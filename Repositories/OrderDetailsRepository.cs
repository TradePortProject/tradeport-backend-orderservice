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
