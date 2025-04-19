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
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items), "Order details cannot be null.");
            }
            items.CreatedOn = DateTime.Now;
            await dbContext.OrderDetails.AddAsync(items);
            await dbContext.SaveChangesAsync();
            return items;
        }


        public async Task<OrderDetails?> UpdateOrderItemStatusAsync(Guid orderDetailId, int newStatus)
        {
            if (newStatus < 0) // Example validation: status must be non-negative
            {
                throw new ArgumentException("Order item status must be a non-negative value.", nameof(newStatus));
            }
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
