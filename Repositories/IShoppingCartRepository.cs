using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface IShoppingCartRepository : IRepositoryBase<ShoppingCart>
    {
        Task<List<ShoppingCart>> GetShoppingCartByRetailerIdAsync(Guid retailerID,int status);
        Task<ShoppingCart> CreateShoppingCartItemsAsync(ShoppingCart item);
    }
}