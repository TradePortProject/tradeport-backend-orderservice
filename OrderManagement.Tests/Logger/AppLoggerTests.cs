using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Moq;
using OrderManagement.Logger;
using Serilog;
using Xunit;

namespace OrderManagement.Tests.Logger
{
    public class AppLoggerTests
    {
        private readonly IConfiguration _realConfig;

        public AppLoggerTests()
        {
            // Build a simple in-memory configuration that Serilog expects
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Serilog:MinimumLevel:Default", "Information" },
                { "Serilog:WriteTo:0:Name", "Console" },
            };

            _realConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void LogInformation_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_realConfig);
            logger.LogInformation("This is a test log");
        }

        [Fact]
        public void LogError_With_Exception_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_realConfig);
            logger.LogError(new Exception("Test exception"), "Error occurred");
        }

        [Fact]
        public void LogWarning_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_realConfig);
            logger.LogWarning("This is a warning");
        }

        [Fact]
        public void LogDebug_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_realConfig);
            logger.LogDebug("This is a debug message");
        }
    }
}