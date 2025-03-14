

using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;  // Import the Product model's namespace

namespace OrderManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        // Define your DbSets here
        public DbSet<Order> Order { get; set; }  
        public DbSet<OrderDetails> OrderDetails { get; set; }  
        public DbSet<ShoppingCart> ShoppingCart { get; set; }
        public DbSet<User> Users { get; set; }  // ✅ Added Users table
        public DbSet<Product> Products { get; set; } // ✅ Added Products table
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .ToTable("Orders");
            modelBuilder.Entity<OrderDetails>()
               .ToTable("OrderDetails");
            modelBuilder.Entity<ShoppingCart>()
              .ToTable("ShoppingCart");
            modelBuilder.Entity<User>().ToTable("Users"); //Map Users Table
            modelBuilder.Entity<Product>().ToTable("Products"); //Map Products Table

            //modelBuilder.Entity<Order>()
            //    .Property(b => b.CreatedOn)
            //    .HasDefaultValueSql("getdate()");
        }
    }
}
