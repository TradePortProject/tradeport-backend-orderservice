using OrderManagement.Models;
using System.Threading.Tasks;

namespace OrderManagement.ExternalServices
{
    public interface IKafkaProducer
    {
        Task SendNotificationAsync(string topic, Notification notification);
    }
}

