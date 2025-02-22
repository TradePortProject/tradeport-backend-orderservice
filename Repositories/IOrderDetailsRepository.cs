
using System.Threading.Tasks;
using OrderManagement.Data;
using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public interface IOrderDetailsRepository : IRepositoryBase<OrderDetails>
    {
        //Task<List<Order>> GetAllOrdersAsync();

        //Task<Order?> GetOrderByIdAsync(Guid Id);

        Task<OrderDetails> CreateOrderDetailsAsync(OrderDetails orderDetails);

        //Task<Order?> UpdateProductAsync(Guid Id, Order order);

        //Task<string> GetProductCodeAsync();

    }
}
