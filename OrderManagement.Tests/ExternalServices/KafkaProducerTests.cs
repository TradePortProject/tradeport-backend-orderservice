using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Moq;
using OrderManagement.ExternalServices;
using OrderManagement.Logger.interfaces;
using OrderManagement.Models;
using Xunit;

namespace OrderManagement.Tests.ExternalServices
{
    [ExcludeFromCodeCoverage]
    public class KafkaProducerTests
    {
        private readonly Mock<IAppLogger<KafkaProducer>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IProducer<string, string>> _producerMock;

        public KafkaProducerTests()
        {
            _loggerMock = new Mock<IAppLogger<KafkaProducer>>();
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Kafka:BootstrapServers"]).Returns("localhost:9092");

            _producerMock = new Mock<IProducer<string, string>>();
        }

        [Fact]
        public async Task SendNotificationAsync_Should_Log_Information_On_Success()
        {
            // Arrange
            var notification = new Notification
            {
                NotificationID = Guid.NewGuid(),
                UserID = Guid.NewGuid(),
                Subject = "Test Subject",
                Message = "Test Message",
                FromEmail = "test@domain.com",
                RecipientEmail = "user@domain.com",
                CreatedOn = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };

            var mockProducer = new Mock<IProducer<string, string>>();
            mockProducer
                .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), default))
                .ReturnsAsync(new DeliveryResult<string, string>());

            var kafkaProducer = new KafkaProducerForTest(_loggerMock.Object, _configurationMock.Object, mockProducer.Object);

            // Act
            await kafkaProducer.SendNotificationAsync("test-topic", notification);

            // Assert
            _loggerMock.Verify(l => l.LogInformation("Notification sent to Kafka: {Subject}", notification.Subject), Times.Once);
        }

        // Helper class to inject mock producer
        private class KafkaProducerForTest : KafkaProducer
        {
            public KafkaProducerForTest(IAppLogger<KafkaProducer> logger, IConfiguration config, IProducer<string, string> producer)
                : base(logger, config)
            {
                SetProducer(producer);
            }

            public void SetProducer(IProducer<string, string> producer)
            {
                typeof(KafkaProducer)
                    .GetField("_producer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(this, producer);
            }
        }
    }
}
