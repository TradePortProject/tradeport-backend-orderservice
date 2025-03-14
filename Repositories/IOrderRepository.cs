
using System.Threading.Tasks;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Models.DTO;


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
        Task<(IEnumerable<OrderDto>, int)> GetFilteredOrdersAsync(
        Guid? orderId, Guid? retailerId, Guid? deliveryPersonnelId,
        int? orderStatus, Guid? manufacturerId, int? orderItemStatus,
        int pageNumber, int pageSize);

    }
}
