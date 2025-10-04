using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Mappings;
using OrderManagement.Repositories;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using OrderManagement.ExternalServices;
using OrderManagement.Logger.interfaces;
using OrderManagement.Logger;
using Microsoft.OpenApi.Models;
using System.Text;
// NEW: AWS + JSON
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // trust k8s/ingress proxies
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

/* 
   1) Load config from AWS Secrets Manager (same secrets as User Mgmt)
   - tradeport/dev/user-mgmt/mssql-eks -> ConnectionStrings:tradeportdb
   - tradeport/dev/user-mgmt/jwt       -> Jwt:Key, Jwt:Issuer, Jwt:Audience
*/
var smClient = new AmazonSecretsManagerClient(RegionEndpoint.APSoutheast1);

async Task LoadSecret(string name)
{
    var resp = await smClient.GetSecretValueAsync(new GetSecretValueRequest { SecretId = name });
    if (resp.SecretString is null) return;

    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(resp.SecretString);
    if (dict is null) return;

    foreach (var kv in dict)
    {
        if (kv.Value is JsonElement el && el.ValueKind == JsonValueKind.Object)
        {
            foreach (var inner in el.EnumerateObject())
                builder.Configuration[$"{kv.Key}:{inner.Name}"] = inner.Value.ToString();
        }
        else
        {
            builder.Configuration[kv.Key] = kv.Value?.ToString();
        }
    }
}

// Load the secrets 
await LoadSecret("tradeport/dev/user-mgmt/mssql-eks");
await LoadSecret("tradeport/dev/user-mgmt/jwt");

/* 2) Database (RDS SQL Server)  Uses ConnectionStrings:tradeportdb from Secrets Manager */
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("tradeportdb"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));
});

// Handy raw ADO connection for quick checks
builder.Services.AddScoped<System.Data.Common.DbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("tradeportdb")));

/* 3) App services, repos, mapping, external clients */
builder.Services.AddAutoMapper(typeof(OrderAutoMapperProfiles));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT auth header
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token here}'"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme{ Reference = new OpenApiReference{ Type = ReferenceType.SecurityScheme, Id = "Bearer"}}, Array.Empty<string>() }
    });
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailsRepository, OrderDetailsRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Serilog from appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Filter.ByIncludingOnly(Matching.FromSource("OrderManagement.Controllers.OrderManagementController"))
    .CreateLogger();
builder.Host.UseSerilog();

// Register IAppLogger<T>
builder.Services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

// CORS: support cloud + localhost for dev/test
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://tradeport.cloud", "https://tradeport.cloud")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Ports 
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3017);
    //options.ListenAnyIP(3018);
});

// External clients
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>();
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();

//4) JWT Auth (values come from Secrets Manager)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // set true in prod behind HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
                Console.WriteLine($"Authorization header: {authHeader}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"message\": \"Token is missing or invalid\"}");
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"Authentication failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.UseForwardedHeaders();

// Quick DB health endpoints (kept)
app.MapGet("/db-health", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return Results.Ok(new { connected = ok });
});

app.MapGet("/db-ping", async (System.Data.Common.DbConnection conn) =>
{
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT TOP 1 name FROM sys.tables;";
    var result = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { anyTable = result?.ToString() ?? "(none)" });
});

// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API v1");
    c.RoutePrefix = "swagger";
});

// Enable CORS early
app.UseCors("AllowSpecificOrigins");

// Handle OPTIONS quickly
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = 200; return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
