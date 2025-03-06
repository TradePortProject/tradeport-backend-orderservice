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

        public async Task<List<ShoppingCart>> GetShoppingCartByRetailerIdAsync(Guid retailerID,int status)
        {
            return await FindByCondition(order => order.RetailerID == retailerID && order.IsActive && order.Status == status).ToListAsync();
        }

        public async Task<ShoppingCart> CreateShoppingCartItemsAsync(ShoppingCart item)
        {
            item.CreatedOn = DateTime.Now;
            await dbContext.ShoppingCart.AddAsync(item);
            await dbContext.SaveChangesAsync();
            return item;
        }

     
    }
}
