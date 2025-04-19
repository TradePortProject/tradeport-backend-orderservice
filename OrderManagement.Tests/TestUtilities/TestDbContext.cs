using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Tests.TestEntities; 

namespace OrderManagement.Tests.TestUtilities
{
    [ExcludeFromCodeCoverage]
    public class TestDbContext : AppDbContext
    {
        public TestDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<TestEntity> TestEntities { get; set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Simulate failure by returning 0
            return Task.FromResult(0);
        }
    }
}

