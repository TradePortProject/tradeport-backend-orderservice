using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface IShoppingCartRepository : IRepositoryBase<ShoppingCart>
    {
        Task<List<ShoppingCart>> GetShoppingCartByRetailerIdAsync(Guid retailerID,int status);
        Task<ShoppingCart> GetShoppingCartByRetailerIdAndProductIdAsync(Guid retailerID, Guid ProductID);
        Task<ShoppingCart> CreateShoppingCartItemsAsync(ShoppingCart cart);
        Task <ShoppingCart> GetShoppingCartItemByCartID(Guid cartID);
        Task<bool> UpdateShoppingCartItemByCartIdAsync(ShoppingCart cart);
    }
}