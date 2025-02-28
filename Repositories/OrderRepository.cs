using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;
using Xunit.Abstractions;


namespace OrderManagement.Repositories
{
    public class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        private readonly AppDbContext dbContext;
        public OrderRepository(AppDbContext dbContextRepo) : base(dbContextRepo)
        {
            this.dbContext = dbContextRepo;
        }


        public async Task<Order> CreateOrderAsync(Order order)
        {
            await dbContext.Order.AddAsync(order);
            await dbContext.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await dbContext.Order.FindAsync(orderId);
        }

        public async Task<IEnumerable<OrderDetails>> GetOrderDetailsByOrderIdAsync(Guid orderId)
        {
            return await dbContext.OrderDetails
                .Where(od => od.OrderID == orderId)
                .ToListAsync();
        }

        public async Task<Order?> UpdateOrderAsync(Order order)
        {
            var existingOrder = await dbContext.Order.FindAsync(order.OrderID);

            if (existingOrder == null)
            {
                return null; // Order not found
            }

            // Only update the fields that are allowed to change
            existingOrder.OrderStatus = order.OrderStatus;
            existingOrder.DeliveryPersonnelID = order.DeliveryPersonnelID ?? null;
            existingOrder.UpdatedOn = DateTime.UtcNow; // Ensure UpdatedOn timestamp is recorded

            dbContext.Order.Update(existingOrder);
            await dbContext.SaveChangesAsync();
            return existingOrder;
        }
        public async Task<List<Order>> GetOrderByManufacturerIdAsync(Guid manufacturerID)
        {
            return await FindByCondition(order => order.ManufacturerID == manufacturerID).ToListAsync();
        }
        
            public async Task<List<Order>> GetOrderByOrderIdAsync(Guid orderId)
        {
            return await FindByCondition(order => order.OrderID == orderId).ToListAsync();
        }

        //public async Task<Product?> UpdateProductAsync(Guid id, Product product)
        //{
        //    var productObj = await dbContext.Products.FindAsync(id);

        //    if (productObj == null)
        //    {
        //        return null;
        //    }
        //    productObj.ManufacturerID = product.ManufacturerID;
        //    productObj.ProductName = product.ProductName;
        //    productObj.Description = product.Description;
        //    productObj.Category = product.Category;
        //    productObj.WholesalePrice = product.WholesalePrice;
        //    productObj.RetailPrice = product.RetailPrice;
        //    productObj.Quantity = product.Quantity;
        //    productObj.RetailCurrency = product.RetailCurrency;
        //    productObj.WholeSaleCurrency = product.WholeSaleCurrency;
        //    productObj.ShippingCost = product.ShippingCost;
        //    productObj.CreatedOn = product.CreatedOn;
        //    productObj.UpdatedOn = product.UpdatedOn;
        //    productObj.IsActive = product.IsActive;

        //    await dbContext.SaveChangesAsync();
        //    return productObj;
        //}

        //public async Task<Guid?> DeleteAysnc(Guid Id)
        //{
        //    var productObj = await dbContext.Products.FindAsync(Id);

        //    if (productObj == null)
        //    {
        //        return null;

        //    }

        //    dbContext.Products.Remove(productObj);
        //    await dbContext.SaveChangesAsync();
        //    return productObj.ProductID;
        //}

        //public async Task<string> GetProductCodeAsync()
        //{
        //    var lastProductCode = await FindAll().OrderByDescending(x => x.CreatedOn).Select(x => x.ProductCode).FirstOrDefaultAsync();

        //    int nextNumber = 1;
        //    if (!string.IsNullOrEmpty(lastProductCode))
        //    {
        //        var match = Regex.Match(lastProductCode, @"P(\d+)");
        //        if (match.Success)
        //        {
        //            nextNumber = int.Parse(match.Groups[1].Value) + 1;
        //        }

        //    }
        //    return $"P{nextNumber:D3}";
        //}
    }
}
