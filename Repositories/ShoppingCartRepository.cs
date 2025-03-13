using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public class ShoppingCartRepository : RepositoryBase<ShoppingCart>, IShoppingCartRepository
    {
        private readonly AppDbContext dbContext;
        public ShoppingCartRepository(AppDbContext dbContextRepo) : base(dbContextRepo)
        {
            this.dbContext = dbContextRepo;
        }

        public async Task<List<ShoppingCart>> GetShoppingCartByRetailerIdAsync(Guid retailerID, int status)
        {
            return await FindByCondition(order => order.RetailerID == retailerID && order.IsActive && order.Status == status).ToListAsync();
        }

        public async Task<ShoppingCart> GetShoppingCartItemByCartID(Guid cartID)
        {
            return await FindByCondition(shoppingCart => shoppingCart.CartID == cartID).FirstAsync();

        }

        public async Task<bool> UpdateShoppingCartItemByCartIdAsync(ShoppingCart updatedItem)
        {
            dbContext.ShoppingCart.Update(updatedItem);
            int result = await dbContext.SaveChangesAsync();
            if (result <= 0)
            {
                throw new Exception("Failed to update changes to the database.");
            }
            return result == 1 ? true : false;
        }

        public async Task<ShoppingCart> CreateShoppingCartItemsAsync(ShoppingCart item)
        {
            await dbContext.ShoppingCart.AddAsync(item);
            int result = await dbContext.SaveChangesAsync();
            if (result > 0)
            {
                // Changes were successfully saved
                return item;
            }
            else
            {
                // Handle the case where no changes were saved
                throw new Exception("Failed to save changes to the database.");
            }
        }


    }
}
