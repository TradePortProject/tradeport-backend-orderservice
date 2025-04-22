using System.Text.Json;
using Confluent.Kafka;
using OrderManagement.Models;
using OrderManagement.Logger.interfaces;

namespace OrderManagement.ExternalServices
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly IAppLogger<KafkaProducer> _logger;

        public KafkaProducer(IAppLogger<KafkaProducer> logger, IConfiguration configuration)
        {
            _logger = logger;

            var kafkaSection = configuration.GetSection("Kafka");
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSection["BootstrapServers"],
                MessageTimeoutMs = int.Parse(kafkaSection["MessageTimeoutMs"] ?? "5000"),
                SocketTimeoutMs = int.Parse(kafkaSection["SocketTimeoutMs"] ?? "6000"),
                RequestTimeoutMs = int.Parse(kafkaSection["RequestTimeoutMs"] ?? "5000"),
                Acks = Acks.All,
                EnableIdempotence = false, // Default is false, but good to make it explicit
                ClientId = "order-service",
                MaxInFlight = 1,// Add these to stop infinite retries
                RetryBackoffMs = 100,// Time to wait before retrying         
                MessageSendMaxRetries = 1
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendNotificationAsync(string topic, Notification notification)
        {
            var message = JsonSerializer.Serialize(notification);
            _logger.LogDebug("Preparing to send Kafka message: {Message}", message);

            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = notification.NotificationID.ToString(),
                Value = message
            });

            _logger.LogInformation("Kafka message sent successfully: {Subject}", notification.Subject);
        }
    }
}





