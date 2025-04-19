using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Moq;
using OrderManagement.Logger;
using Serilog;
using Xunit;

namespace OrderManagement.Tests.Logger
{
    [ExcludeFromCodeCoverage]
    public class AppLoggerTests
    {
        private readonly Mock<IConfiguration> _configurationMock;

        public AppLoggerTests()
        {
            _configurationMock = new Mock<IConfiguration>();
        }

        [Fact]
        public void LogInformation_Should_Not_Throw()
        {
            // Arrange
            var logger = new AppLogger<AppLoggerTests>(_configurationMock.Object);

            // Act & Assert
            logger.LogInformation("This is a test log");
        }

        [Fact]
        public void LogError_With_Exception_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_configurationMock.Object);

            // Act & Assert
            logger.LogError(new Exception("Test exception"), "Error occurred");
        }

        [Fact]
        public void LogWarning_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_configurationMock.Object);

            // Act & Assert
            logger.LogWarning("This is a warning");
        }

        [Fact]
        public void LogDebug_Should_Not_Throw()
        {
            var logger = new AppLogger<AppLoggerTests>(_configurationMock.Object);

            // Act & Assert
            logger.LogDebug("This is a debug message");
        }
    }
}