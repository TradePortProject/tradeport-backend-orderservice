using OrderManagement.Models;
using OrderManagement.Models.DTO;

namespace OrderManagement.ExternalServices
{
    public interface IProductServiceClient
    {
        Task<ProductDTO> GetProductByIdAsync(Guid productId);
        Task<bool> UpdateProductQuantityAsync(Guid productId, int quantity);
    }
}