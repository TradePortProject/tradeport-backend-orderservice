using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public interface IProductRepository
    {
        Task<Product> GetProductByProductIdAsync(Guid productID);

        Task<Product?> UpdateProductQuantityAsync(Guid id, int quantity);
    }
}
