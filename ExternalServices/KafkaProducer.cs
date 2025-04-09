using System.Text.Json;
using Confluent.Kafka;
using OrderManagement.Models;
using OrderManagement.Logger.interfaces;

namespace OrderManagement.ExternalServices
{
    public class KafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly IAppLogger<KafkaProducer> _logger;

        public KafkaProducer(IAppLogger<KafkaProducer> logger, IConfiguration configuration)
        {
            _logger = logger;
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendNotificationAsync(string topic, Notification notification)
        {
            try
            {
                var message = JsonSerializer.Serialize(notification);

                //_logger.LogInformation("Sending message to topic {Topic}: {Message}", topic, message);

                await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = notification.NotificationID.ToString(),
                    Value = message
                });

                _logger.LogInformation("Notification sent to Kafka: {Subject}", notification.Subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Kafka message.");
            }
        }
    }
}





