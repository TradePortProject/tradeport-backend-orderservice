using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;


namespace OrderManagement.Repositories
{
    public class OrderDetailsRepository : RepositoryBase<OrderDetails>, IOrderDetailsRepository
    {
        private readonly AppDbContext dbContext;
        public OrderDetailsRepository(AppDbContext dbContextRepo) : base(dbContextRepo)
        {
            this.dbContext = dbContextRepo;
        }


        public async Task<OrderDetails> CreateOrderDetailsAsync(OrderDetails items)
        {
            items.CreatedOn = DateTime.Now;
            await dbContext.OrderDetails.AddAsync(items);
            await dbContext.SaveChangesAsync();
            return items;
        }

        public async Task<OrderDetails?> UpdateOrderItemStatusAsync(Guid orderDetailId, int newStatus)
        {
            var orderItem = await dbContext.OrderDetails.FindAsync(orderDetailId);
            if (orderItem == null)
            {
                return null; // Not found
            }

            orderItem.OrderItemStatus = newStatus;
            await dbContext.SaveChangesAsync();
            return orderItem;
        }
    }
}
