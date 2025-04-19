using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Repositories;
using Xunit;

namespace OrderManagement.Tests.Repositories
{
    [ExcludeFromCodeCoverage]
    public class OrderDetailsRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        public OrderDetailsRepositoryTests()
        {
            //// Configure SQLite in-memory database
            //_dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            //    .UseSqlite("Filename=:memory:") // Use SQLite in-memory database
            //    .Options;
            //// Ensure the database schema is created
            //using var context = new AppDbContext(_dbContextOptions);
            //context.Database.OpenConnection(); // Open the connection
            //context.Database.EnsureCreated(); // Create the schema

            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database for each test
                .Options;
        }
        [Fact]
        public async Task CreateOrderDetailsAsync_ShouldAddOrderDetails()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetails = new OrderDetails
            {
                OrderDetailID = Guid.NewGuid(),
                OrderID = Guid.NewGuid(),
                ProductID = Guid.NewGuid(),
                ManufacturerID = Guid.NewGuid(),
                Quantity = 10,
                OrderItemStatus = 1,
                ProductPrice = 100.50m
            };
            // Act
            var result = await repository.CreateOrderDetailsAsync(orderDetails);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderDetails.OrderDetailID, result.OrderDetailID);
            Assert.Equal(orderDetails.OrderItemStatus, result.OrderItemStatus);
            var savedOrderDetails = await context.OrderDetails.FindAsync(orderDetails.OrderDetailID);
            Assert.NotNull(savedOrderDetails);
        }

        [Fact]
        public async Task UpdateOrderItemStatusAsync_ShouldUpdateStatus_WhenOrderDetailExists()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetailId = Guid.NewGuid();
            var orderDetails = new OrderDetails
            {
                OrderDetailID = orderDetailId,
                OrderItemStatus = 1
            };
            context.OrderDetails.Add(orderDetails);
            await context.SaveChangesAsync();
            var newStatus = 2;
            // Act
            var result = await repository.UpdateOrderItemStatusAsync(orderDetailId, newStatus);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(newStatus, result.OrderItemStatus);
            var updatedOrderDetails = await context.OrderDetails.FindAsync(orderDetailId);
            Assert.Equal(newStatus, updatedOrderDetails.OrderItemStatus);
        }
        [Fact]
        public async Task UpdateOrderItemStatusAsync_ShouldReturnNull_WhenOrderDetailDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetailId = Guid.NewGuid();
            var newStatus = 2;
            // Act
            var result = await repository.UpdateOrderItemStatusAsync(orderDetailId, newStatus);
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderDetailsAsync_ShouldThrowException_WhenOrderDetailsIsNull()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CreateOrderDetailsAsync(null));
        }
        [Fact]
        public async Task CreateOrderDetailsAsync_ShouldThrowException_WhenDuplicateOrderDetailID()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetailId = Guid.NewGuid();
            var orderDetails1 = new OrderDetails
            {
                OrderDetailID = orderDetailId,
                OrderItemStatus = 1
            };
            var orderDetails2 = new OrderDetails
            {
                OrderDetailID = orderDetailId,
                OrderItemStatus = 2
            };
            context.OrderDetails.Add(orderDetails1);
            await context.SaveChangesAsync();
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateOrderDetailsAsync(orderDetails2));
        }

        [Fact]
        public async Task UpdateOrderItemStatusAsync_ShouldThrowException_WhenInvalidStatus()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetailId = Guid.NewGuid();
            var orderDetails = new OrderDetails
            {
                OrderDetailID = orderDetailId,
                OrderItemStatus = 1
            };
            context.OrderDetails.Add(orderDetails);
            await context.SaveChangesAsync();
            var invalidStatus = -1;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => repository.UpdateOrderItemStatusAsync(orderDetailId, invalidStatus));
        }
        [Fact]
        public async Task UpdateOrderItemStatusAsync_ShouldHandleConcurrentUpdates()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new OrderDetailsRepository(context);
            var orderDetailId = Guid.NewGuid();
            var orderDetails = new OrderDetails
            {
                OrderDetailID = orderDetailId,
                OrderItemStatus = 1
            };
            context.OrderDetails.Add(orderDetails);
            await context.SaveChangesAsync();
            var newStatus1 = 2;
            var newStatus2 = 3;
            // Act
            var task1 = repository.UpdateOrderItemStatusAsync(orderDetailId, newStatus1);
            var task2 = repository.UpdateOrderItemStatusAsync(orderDetailId, newStatus2);
            await Task.WhenAll(task1, task2);
            // Assert
            var updatedOrderDetails = await context.OrderDetails.FindAsync(orderDetailId);
            Assert.True(updatedOrderDetails.OrderItemStatus == newStatus1 || updatedOrderDetails.OrderItemStatus == newStatus2);
        }
        //[Fact]
        //public async Task CreateOrderDetailsAsync_ShouldThrowException_WhenRequiredFieldsAreMissing()
        //{
        //    // Arrange
        //    using var context = new AppDbContext(_dbContextOptions);
        //    var repository = new OrderDetailsRepository(context);
        //    var orderDetails = new OrderDetails
        //    {
        //        OrderDetailID = Guid.NewGuid(),
        //        // Missing required fields like OrderID, ProductID, ManufacturerID, Quantity, etc.
        //    };
        //    // Act & Assert
        //    await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateOrderDetailsAsync(orderDetails));
        //}

    }

}
