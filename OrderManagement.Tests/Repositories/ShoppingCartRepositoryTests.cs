using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Repositories;
using Xunit;
using OrderManagement.Tests.TestUtilities;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Tests.Repositories
{
    [ExcludeFromCodeCoverage]
    public class ShoppingCartRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        public ShoppingCartRepositoryTests()
        {
            // Configure the in-memory database
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database for each test
                .Options;
        }

        //[Fact]
        //public async Task UpdateShoppingCartItemByCartIdAsync_ShouldThrowException_WhenSaveFails()
        //{
        //    // Arrange
        //    var options = new DbContextOptionsBuilder<AppDbContext>()
        //        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
        //        .Options;
        //    await using var context = new AppDbContext(options);
        //    var repository = new ShoppingCartRepository(context);
        //    var shoppingCartItem = new ShoppingCart
        //    {
        //        CartID = Guid.NewGuid(),
        //        RetailerID = Guid.NewGuid(),
        //        ProductID = Guid.NewGuid(),
        //        OrderQuantity = 2,
        //        ProductPrice = 100m,
        //        ManufacturerID = Guid.NewGuid(),
        //        IsActive = true
        //    };
        //    // Add the item to the database first
        //    context.ShoppingCart.Add(shoppingCartItem);
        //    await context.SaveChangesAsync();
        //    // Simulate failure by overriding SaveChangesAsync to return 0
        //    var mockContext = new Mock<AppDbContext>(options);
        //    mockContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
        //        .ReturnsAsync(0);
        //    var mockRepository = new ShoppingCartRepository(mockContext.Object);
        //    // Act & Assert
        //    var exception = await Assert.ThrowsAsync<Exception>(() => mockRepository.UpdateShoppingCartItemByCartIdAsync(shoppingCartItem));
        //    Assert.Equal("Failed to update changes to the database.", exception.Message);
        //}

        [Fact]
        public async Task UpdateShoppingCartItemByCartIdAsync_ShouldThrowException_WhenSaveFails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
                .Options;
            await using var context = new TestDbContext(options); // Use the derived TestAppDbContext
            var repository = new ShoppingCartRepository(context);
            var shoppingCartItem = new ShoppingCart
            {
                CartID = Guid.NewGuid(),
                RetailerID = Guid.NewGuid(),
                ProductID = Guid.NewGuid(),
                OrderQuantity = 2,
                ProductPrice = 100m,
                ManufacturerID = Guid.NewGuid(),
                IsActive = true
            };
            // Add the item to the database first
            context.ShoppingCart.Add(shoppingCartItem);
            await context.SaveChangesAsync();
            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => repository.UpdateShoppingCartItemByCartIdAsync(shoppingCartItem));
            Assert.Equal("Failed to update changes to the database.", exception.Message);
        }


        [Fact]
        public async Task CreateShoppingCartItemsAsync_ShouldThrowException_WhenSaveFails()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
                .Options;
            await using var context = new TestDbContext(options); // Use the derived TestAppDbContext
            var repository = new ShoppingCartRepository(context);
            var shoppingCartItem = new ShoppingCart
            {
                CartID = Guid.NewGuid(),
                RetailerID = Guid.NewGuid(),
                ProductID = Guid.NewGuid(),
                OrderQuantity = 2,
                ProductPrice = 100m,
                ManufacturerID = Guid.NewGuid(),
                IsActive = true
            };
            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => repository.CreateShoppingCartItemsAsync(shoppingCartItem));
            Assert.Equal("Failed to save changes to the database.", exception.Message);
        }




        //[Fact]
        //public async Task UpdateShoppingCartItemByCartIdAsync_ShouldThrowException_WhenSaveFails()
        //{
        //    // Arrange
        //    var options = new DbContextOptionsBuilder<AppDbContext>()
        //        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
        //        .Options;
        //    await using var context = new AppDbContext(options);
        //    var repository = new ShoppingCartRepository(context);
        //    var shoppingCartItem = new ShoppingCart
        //    {
        //        CartID = Guid.NewGuid(),
        //        RetailerID = Guid.NewGuid(),
        //        ProductID = Guid.NewGuid(),
        //        OrderQuantity = 2,
        //        ProductPrice = 100m,
        //        ManufacturerID = Guid.NewGuid(),
        //        IsActive = true
        //    };
        //    // Simulate failure by disposing the context before calling SaveChangesAsync
        //    await context.DisposeAsync();
        //    // Act & Assert
        //    var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() => repository.UpdateShoppingCartItemByCartIdAsync(shoppingCartItem));
        //    Assert.Contains("Cannot access a disposed context instance", exception.Message);
        //}




        [Fact]
        public async Task GetShoppingCartByRetailerIdAsync_ShouldReturnMatchingCarts()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new ShoppingCartRepository(context);
            var retailerId = Guid.NewGuid();
            var shoppingCarts = new List<ShoppingCart>
        {
            new ShoppingCart { CartID = Guid.NewGuid(), RetailerID = retailerId, IsActive = true, Status = 1 },
            new ShoppingCart { CartID = Guid.NewGuid(), RetailerID = retailerId, IsActive = true, Status = 1 },
            new ShoppingCart { CartID = Guid.NewGuid(), RetailerID = Guid.NewGuid(), IsActive = true, Status = 1 } // Different retailer
        };
            context.ShoppingCart.AddRange(shoppingCarts);
            await context.SaveChangesAsync();
            // Act
            var result = await repository.GetShoppingCartByRetailerIdAsync(retailerId, 1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, cart => Assert.Equal(retailerId, cart.RetailerID));
        }
        [Fact]
        public async Task GetShoppingCartItemByCartID_ShouldReturnMatchingCart()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new ShoppingCartRepository(context);
            var cartId = Guid.NewGuid();
            var shoppingCart = new ShoppingCart { CartID = cartId, RetailerID = Guid.NewGuid(), IsActive = true, Status = 1 };
            context.ShoppingCart.Add(shoppingCart);
            await context.SaveChangesAsync();
            // Act
            var result = await repository.GetShoppingCartItemByCartID(cartId);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(cartId, result.CartID);
        }
        [Fact]
        public async Task UpdateShoppingCartItemByCartIdAsync_ShouldUpdateCartItem()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new ShoppingCartRepository(context);
            var cartId = Guid.NewGuid();
            var shoppingCart = new ShoppingCart
            {
                CartID = cartId,
                RetailerID = Guid.NewGuid(),
                IsActive = true,
                Status = 1
            };
            context.ShoppingCart.Add(shoppingCart);
            await context.SaveChangesAsync();
            // Retrieve the existing entity and update its properties
            var updatedCart = await context.ShoppingCart.FindAsync(cartId);
            updatedCart.IsActive = false;
            updatedCart.Status = 2;
            // Act
            var result = await repository.UpdateShoppingCartItemByCartIdAsync(updatedCart);
            // Assert
            Assert.True(result);
            var updatedItem = await context.ShoppingCart.FindAsync(cartId);
            Assert.NotNull(updatedItem);
            Assert.False(updatedItem.IsActive);
            Assert.Equal(2, updatedItem.Status);
        }

        [Fact]
        public async Task UpdateShoppingCartItemByCartIdAsync_ShouldThrowException_WhenUpdateFails()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new ShoppingCartRepository(context);
            var updatedCart = new ShoppingCart
            {
                CartID = Guid.NewGuid(), // Non-existent CartID
                RetailerID = Guid.NewGuid(),
                IsActive = false,
                Status = 2
            };
            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repository.UpdateShoppingCartItemByCartIdAsync(updatedCart));
        }

        [Fact]
        public async Task CreateShoppingCartItemsAsync_ShouldAddNewCartItem()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new ShoppingCartRepository(context);
            var newCart = new ShoppingCart { CartID = Guid.NewGuid(), RetailerID = Guid.NewGuid(), IsActive = true, Status = 1 };
            // Act
            var result = await repository.CreateShoppingCartItemsAsync(newCart);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(newCart.CartID, result.CartID);
            var savedCart = await context.ShoppingCart.FindAsync(newCart.CartID);
            Assert.NotNull(savedCart);
        }
        //[Fact]
        //public async Task CreateShoppingCartItemsAsync_ShouldThrowException_WhenSaveFails()
        //{
        //    // Arrange
        //    using var context = new AppDbContext(_dbContextOptions);
        //    var repository = new ShoppingCartRepository(context);
        //    var newCart = new ShoppingCart
        //    {
        //        CartID = Guid.NewGuid(),
        //        RetailerID = Guid.NewGuid(),
        //        IsActive = true,
        //        Status = 1
        //    };
        //    // Simulate a failure by disposing the context before saving
        //    await context.DisposeAsync();
        //    // Act & Assert
        //    await Assert.ThrowsAsync<ObjectDisposedException>(() => repository.CreateShoppingCartItemsAsync(newCart));
        //}

        //[Fact]
        //public async Task CreateShoppingCartItemsAsync_ShouldThrowException_WhenSaveFails()
        //{
        //    // Arrange
        //    var options = new DbContextOptionsBuilder<AppDbContext>()
        //        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
        //        .Options;
        //    await using var context = new AppDbContext(options);
        //    // Mock SaveChangesAsync to simulate failure
        //    var repository = new ShoppingCartRepository(context);
        //    // Simulate failure by disposing the context before calling SaveChangesAsync
        //    await context.DisposeAsync();
        //    var shoppingCartItem = new ShoppingCart
        //    {
        //        CartID = Guid.NewGuid(),
        //        RetailerID = Guid.NewGuid(),
        //        ProductID = Guid.NewGuid(),
        //        OrderQuantity = 2,
        //        ProductPrice = 100m,
        //        ManufacturerID = Guid.NewGuid(),
        //        IsActive = true
        //    };
        //    // Act & Assert
        //    var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() => repository.CreateShoppingCartItemsAsync(shoppingCartItem));
        //    Assert.Contains("Cannot access a disposed context instance", exception.Message);
        //}

    }
}
