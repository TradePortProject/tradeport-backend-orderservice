
using System.Threading.Tasks;
using OrderManagement.Data;
using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public interface IOrderDetailsRepository : IRepositoryBase<OrderDetails>
    {
        Task<OrderDetails> CreateOrderDetailsAsync(OrderDetails orderDetails);
        Task<OrderDetails?> UpdateOrderItemStatusAsync(Guid orderDetailId, int newStatus);

    }
}
