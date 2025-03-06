
using System.Threading.Tasks;
using OrderManagement.Data;
using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public interface IOrderRepository : IRepositoryBase<Order>
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task<Order?> UpdateOrderAsync(Order order);
        Task<List<Order>> GetOrderByManufacturerIdAsync(Guid manufacturerID);
        Task<IEnumerable<OrderDetails>> GetOrderDetailsByOrderIdAsync(Guid orderId); 

        Task<List<Order>> GetOrderByOrderIdAsync(Guid manufacturerID);

    }
}
