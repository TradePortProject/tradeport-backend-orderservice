using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories
{
    public class ProductRepository : RepositoryBase<Product>, IProductRepository
    {
        private readonly AppDbContext dbContext;

        public ProductRepository(AppDbContext dbContextRepo) : base(dbContextRepo)
        {
            this.dbContext = dbContextRepo;
        }
        public async Task<Product> GetProductByProductIdAsync(Guid productID)
        {
            return await FindByCondition(product => product.ProductID == productID && product.IsActive).FirstAsync();
        }
        public async Task<Product?> UpdateProductQuantityAsync(Guid productID, int quantity)
        {
            var existingProduct = await dbContext.Products.FindAsync(productID);
            if (existingProduct == null)
            {
                return null;
            }

            existingProduct.Quantity = quantity;
            existingProduct.UpdatedOn = DateTime.UtcNow;

            dbContext.Products.Update(existingProduct);
            await dbContext.SaveChangesAsync();

            return existingProduct;
        }

    }
}
