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
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAutoMapper(typeof(OrderAutoMapperProfiles));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION") ??
                       //builder.Configuration.GetConnectionString("DefaultConnection");

// Add DbContext service
//builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseSqlServer(connectionString));

// Configure SqlClient to ignore certificate validation errors (for testing purposes)
SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));

sqlBuilder.Encrypt = true;
sqlBuilder.TrustServerCertificate = true;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(sqlBuilder.ConnectionString));

// Register repository
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderDetailsRepository, OrderDetailsRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Filter.ByIncludingOnly(Matching.FromSource("OrderManagement.Controllers.OrderManagementController"))
    .CreateLogger();
builder.Host.UseSerilog(); 

// Register IAppLogger<T>
builder.Services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3017); // HTTP port
    options.ListenAnyIP(3018);
    //options.ListenAnyIP(443, listenOptions => listenOptions.UseHttps()); // HTTPS port
});

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>();

builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //options.RequireHttpsMetadata = false; // Set to true in production
        //options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };

        Console.WriteLine($"Issuer: {options.TokenValidationParameters.ValidIssuer}");

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                Console.WriteLine($"Authorization header: {authHeader}");
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"message\": \"Token is missing or invalid\"}");
            },

            OnAuthenticationFailed = context =>
            {
                // Log authentication failure
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();



// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API v1");
    c.RoutePrefix = "swagger"; // Access at http://localhost:3017/swagger
});

// Enable CORS with the specified policy
app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();



app.Run();
