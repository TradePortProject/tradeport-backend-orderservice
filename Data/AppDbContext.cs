

using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;  // Import the Product model's namespace

namespace OrderManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        // Define your DbSets here
        public DbSet<Order> Order { get; set; }  // This is the DbSet for the Product entity

        // Define your DbSets here
        public DbSet<OrderDetails> OrderDetails { get; set; }  // This is the DbSet for the Product entity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .ToTable("Orders");
            modelBuilder.Entity<OrderDetails>()
               .ToTable("OrderDetails");
            //modelBuilder.Entity<Order>()
            //    .Property(b => b.CreatedOn)
            //    .HasDefaultValueSql("getdate()");
        }
    }
}
