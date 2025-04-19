using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Models.DTO;
using OrderManagement.Repositories;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Tests.Repositories
{
    [ExcludeFromCodeCoverage]
    public class OrderRepositoryTests
    {
        public OrderRepositoryTests()
        {
           
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByManufacturerId()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var manufacturerId = Guid.NewGuid();
            var orders = new List<Order>
            {
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "123 Test Street", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderDetails = new List<OrderDetails>
                    {
                        new OrderDetails { ManufacturerID = manufacturerId, ProductID = Guid.NewGuid() }
                    }
                },
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "456 Test Avenue", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderDetails = new List<OrderDetails>
                    {
                        new OrderDetails { ManufacturerID = Guid.NewGuid(), ProductID = Guid.NewGuid() }
                    }
                }
            };
            context.Order.AddRange(orders);
            await context.SaveChangesAsync();
            var repository = new OrderRepository(context);
            // Act
            var (result, totalPages) = await repository.GetFilteredOrdersAsync(
                null, null, null, null, manufacturerId, null, null, null, null, 1, 10);
            // Assert
            Assert.Single(result);
            Assert.All(result.SelectMany(o => o.OrderDetails), od => Assert.Equal(manufacturerId, od.ManufacturerID));
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByOrderStatus()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var orderStatus = 1;
            var orders = new List<Order>
            {
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "123 Test Street", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderStatus = orderStatus
                },
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "456 Test Avenue", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderStatus = 2
                }
            };
            context.Order.AddRange(orders);
            await context.SaveChangesAsync();
            var repository = new OrderRepository(context);
            // Act
            var (result, totalPages) = await repository.GetFilteredOrdersAsync(
                null, null, null, orderStatus, null, null, null, null, null, 1, 10);
            // Assert
            Assert.Single(result);
            Assert.All(result, o => Assert.Equal(orderStatus, o.OrderStatus));
        }


        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByOrderItemStatus()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var orderItemStatus = 1;
            var orders = new List<Order>
            {
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "123 Test Street", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderDetails = new List<OrderDetails>
                    {
                        new OrderDetails { OrderItemStatus = orderItemStatus, ProductID = Guid.NewGuid() }
                    }
                },
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "456 Test Avenue", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderDetails = new List<OrderDetails>
                    {
                        new OrderDetails { OrderItemStatus = 2, ProductID = Guid.NewGuid() }
                    }
                }
            };
            context.Order.AddRange(orders);
            await context.SaveChangesAsync();
            var repository = new OrderRepository(context);
            // Act
            var (result, totalPages) = await repository.GetFilteredOrdersAsync(
                null, null, null, null, null, orderItemStatus, null, null, null, 1, 10);
            // Assert
            Assert.Single(result);
            Assert.All(result.SelectMany(o => o.OrderDetails), od => Assert.Equal(orderItemStatus, od.OrderItemStatus));
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByRetailerName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            var retailerId = Guid.NewGuid();
            var retailerName = "Retailer A";
            var users = new List<User>
            {
                new User
                {
                    UserID = retailerId,
                    UserName = retailerName,
                    Address = "123 Test Street", // Required property
                    PhoneNo = "1234567890" // Required property
                },
                new User
                {
                    UserID = Guid.NewGuid(),
                    UserName = "Retailer B",
                    Address = "456 Test Avenue", // Required property
                    PhoneNo = "0987654321" // Required property
                }
            };
            var orders = new List<Order>
            {
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = retailerId,
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "123 Test Street", // Required property
                    ShippingCurrency = "USD", // Required property
                },
                new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = "456 Test Avenue", // Required property
                    ShippingCurrency = "USD", // Required property
                }
            };
            context.Users.AddRange(users);
            context.Order.AddRange(orders);
            await context.SaveChangesAsync();
            var repository = new OrderRepository(context);
            // Act
            var (result, totalPages) = await repository.GetFilteredOrdersAsync(
                null, null, null, null, null, null, retailerName, null, null, 1, 10);
            // Assert
            Assert.Single(result);
            Assert.All(result, o => Assert.Equal(retailerId, o.RetailerID));
        }


        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldHandlePagination()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new AppDbContext(options);
            // Add 15 orders with required properties
            for (int i = 0; i < 15; i++)
            {
                context.Order.Add(new Order
                {
                    OrderID = Guid.NewGuid(),
                    RetailerID = Guid.NewGuid(),
                    PaymentCurrency = "USD", // Required property
                    ShippingAddress = $"123 Test Street {i}", // Required property
                    ShippingCurrency = "USD", // Required property
                    OrderStatus = 1
                });
            }
            await context.SaveChangesAsync();
            var repository = new OrderRepository(context);
            // Act
            var (result, totalPages) = await repository.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, null, 1, 10); // Page 1, 10 items per page
            // Assert
            Assert.Equal(10, result.Count()); // Ensure 10 items are returned for page 1
            Assert.Equal(2, totalPages); // Total pages = 15 items / 10 items per page
        }




        //[Fact]
        //public async Task GetOrderByOrderIdAsync_ShouldReturnOrders_WhenOrderExists()
        //{
        //    // Arrange
        //    var options = new DbContextOptionsBuilder<AppDbContext>()
        //        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
        //        .Options;
        //    await using var context = new AppDbContext(options);
        //    var orderId = Guid.NewGuid();
        //    var orders = new List<Order>
        //    {
        //        new Order { OrderID = orderId, RetailerID = Guid.NewGuid(), OrderStatus = 1, TotalPrice = 100 },
        //        new Order { OrderID = orderId, RetailerID = Guid.NewGuid(), OrderStatus = 2, TotalPrice = 200 }
        //    };
        //    await context.Order.AddRangeAsync(orders);
        //    await context.SaveChangesAsync();
        //    var repository = new OrderRepository(context);
        //    // Act
        //    var result = await repository.GetOrderByOrderIdAsync(orderId);
        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.Equal(2, result.Count);
        //    Assert.All(result, order => Assert.Equal(orderId, order.OrderID));
        //}

        [Fact]
        public async Task GetOrderByOrderIdAsync_ShouldReturnEmptyList_WhenOrderDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use a unique in-memory database
                .Options;
            await using var context = new AppDbContext(options);
            var orderId = Guid.NewGuid(); // Non-existent OrderID
            var repository = new OrderRepository(context);
            // Act
            var result = await repository.GetOrderByOrderIdAsync(orderId);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }



        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnAllOrders_WhenNoFiltersAreApplied()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context); // Seed multiple orders
            await TestDataSeeder.SeedOrderWithDetailsAsync(context);
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, null, 1, 10);
            // Assert
            results.Should().NotBeEmpty();
            totalPages.Should().Be(1); // Assuming all orders fit in one page
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrder_WhenFilteredByOrderId()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                orderId, null, null, null, null, null, null, null, null, 1, 10);
            // Assert
            results.Should().ContainSingle(o => o.OrderID == orderId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByRetailerId()
        {
            // Arrange
            var retailerId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, retailerId: retailerId);
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, retailerId, null, null, null, null, null, null, null, 1, 10);
            // Assert
            results.Should().OnlyContain(o => o.RetailerID == retailerId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByManufacturerName()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, manufacturerName: "Test Manufacturer");
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, "Test Manufacturer", null, 1, 10);
            // Assert
            results.SelectMany(o => o.OrderDetails).Should().OnlyContain(d => d.ManufacturerName == "Test Manufacturer");
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenFilteredByProductName()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, productName: "Special Product");
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, "Special Product", 1, 10);
            // Assert
            results.SelectMany(o => o.OrderDetails).Should().OnlyContain(d => d.ProductName == "Special Product");
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnPaginatedResults()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context); // Seed multiple orders
            await TestDataSeeder.SeedOrderWithDetailsAsync(context);
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, null, 1, 1); // Page size = 1
            // Assert
            results.Should().HaveCount(1); // Only 1 order per page
            totalPages.Should().BeGreaterThan(1); // Multiple pages
        }






        [Fact]
        public async Task CreateOrderAsync_ShouldReturnOrder_WhenSavedSuccessfully()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            var order = new Order
            {
                OrderID = Guid.NewGuid(),
                RetailerID = Guid.NewGuid(),
                OrderStatus = 1,
                TotalPrice = 100,
                PaymentMode = 1,
                PaymentCurrency = "USD",
                ShippingAddress = "123 Test St",
                ShippingCost = 10,
                ShippingCurrency = "USD"
            };
            // Act
            var result = await repo.CreateOrderAsync(order);
            // Assert
            result.Should().NotBeNull();
            result.OrderID.Should().Be(order.OrderID);
            (await context.Order.FindAsync(order.OrderID)).Should().NotBeNull();
        }
        [Fact]
        public async Task CreateOrderAsync_ShouldThrowException_WhenSaveFails()
        {
            // Arrange
            using var context = new FailingSaveChangesContext(TestDbContextFactory.CreateOptions());
            var repo = new OrderRepository(context);
            var order = new Order
            {
                OrderID = Guid.NewGuid(),
                RetailerID = Guid.NewGuid(),
                OrderStatus = 1,
                TotalPrice = 100
            };
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => repo.CreateOrderAsync(order));
        }

        [Fact]
        public async Task GetOrderByIdAsync_ShouldReturnOrder_WhenOrderExists()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var orderId = Guid.NewGuid();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.GetOrderByIdAsync(orderId);
            // Assert
            result.Should().NotBeNull();
            result.OrderID.Should().Be(orderId);
        }
        [Fact]
        public async Task GetOrderByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.GetOrderByIdAsync(Guid.NewGuid());
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOrderDetailsByOrderIdAsync_ShouldReturnOrderDetails_WhenDetailsExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var orderId = Guid.NewGuid();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.GetOrderDetailsByOrderIdAsync(orderId);
            // Assert
            result.Should().NotBeEmpty();
            result.All(od => od.OrderID == orderId).Should().BeTrue();
        }
        [Fact]
        public async Task GetOrderDetailsByOrderIdAsync_ShouldReturnEmptyList_WhenNoDetailsExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.GetOrderDetailsByOrderIdAsync(Guid.NewGuid());
            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldUpdateOrder_WhenOrderExists()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var orderId = Guid.NewGuid();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);
            var repo = new OrderRepository(context);
            var updatedOrder = new Order
            {
                OrderID = orderId,
                OrderStatus = 2,
                DeliveryPersonnelID = Guid.NewGuid()
            };
            // Act
            var result = await repo.UpdateOrderAsync(updatedOrder);
            // Assert
            result.Should().NotBeNull();
            result.OrderStatus.Should().Be(2);
            result.DeliveryPersonnelID.Should().NotBeNull();
        }
        [Fact]
        public async Task UpdateOrderAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            var updatedOrder = new Order
            {
                OrderID = Guid.NewGuid(),
                OrderStatus = 2
            };
            // Act
            var result = await repo.UpdateOrderAsync(updatedOrder);
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ShouldUpdateStatus_WhenOrderExists()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var orderId = Guid.NewGuid();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.UpdateOrderStatusAsync(orderId, 2);
            // Assert
            result.Should().NotBeNull();
            result.OrderStatus.Should().Be(2);
        }
        [Fact]
        public async Task UpdateOrderStatusAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            // Act
            var result = await repo.UpdateOrderStatusAsync(Guid.NewGuid(), 2);
            // Assert
            result.Should().BeNull();
        }

        ///start from here
        // ✅ TEST CASE 3: Get Orders by Manufacturer Name
        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByManufacturerName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"OrderDb_{Guid.NewGuid()}")
                .Options;

            using var context = new AppDbContext(options);

            var manufacturerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var manufacturer = new User { UserID = manufacturerId, UserName = "Test Manufacturer", Address = "123", PhoneNo = "88889999" };
            var product = new Product { ProductID = productId, ProductName = "Test Product", ManufacturerID = manufacturerId };

            var order = new Order
            {
                OrderID = Guid.NewGuid(),
                RetailerID = Guid.NewGuid(),
                PaymentCurrency = "USD",
                PaymentMode = 1,
                ShippingCost = 10,
                ShippingCurrency = "USD",
                ShippingAddress = "123 Test St",
                OrderStatus = 1,
                TotalPrice = 100,
                CreatedBy = Guid.NewGuid(),
                OrderDetails = new List<OrderDetails>
        {
            new OrderDetails
            {
                OrderDetailID = Guid.NewGuid(),
                ProductID = productId,
                ManufacturerID = manufacturerId,
                Quantity = 2,
                OrderItemStatus = 1,
                ProductPrice = 50
            }
        }
            };

            // Seed data
            context.Order.Add(order);
            context.Users.Add(manufacturer);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                orderId: null,
                retailerId: null,
                deliveryPersonnelId: null,
                orderStatus: null,
                manufacturerId: null,
                orderItemStatus: null,
                retailerName: null,
                manufacturerName: "Manufacturer",
                productName: null,
                pageNumber: 1,
                pageSize: 10
            );

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCount(1);
            var result = results.First();
            result.OrderDetails.Should().ContainSingle(detail => detail.ManufacturerName == "Test Manufacturer");
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByRetailerName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var retailerId = Guid.NewGuid();
            var retailer = new User { UserID = retailerId, UserName = "Retailer A", Address = "123", PhoneNo = "88889999" };
            var productId = Guid.NewGuid();
            var product = new Product { ProductID = productId, ProductName = "Product X", ManufacturerID = Guid.NewGuid() };
            var manufacturerId = Guid.NewGuid();
            var manufacturer = new User { UserID = manufacturerId, UserName = "Manufacturer A", Address = "123", PhoneNo = "88889999" };

            var order = new Order
            {
                OrderID = Guid.NewGuid(),
                RetailerID = retailerId,
                PaymentCurrency = "USD",
                PaymentMode = 1,
                ShippingCost = 10,
                ShippingCurrency = "USD",
                ShippingAddress = "123 Test St",
                OrderStatus = 1,
                TotalPrice = 100,
                CreatedBy = Guid.NewGuid(),
                OrderDetails = new List<OrderDetails>
                {
                    new OrderDetails
                    {
                        OrderDetailID = Guid.NewGuid(),
                        ProductID = productId,
                        ManufacturerID = manufacturerId,
                        Quantity = 2,
                        ProductPrice = 50,
                        OrderItemStatus = 1
                    }
                }
            };

            context.Users.AddRange(retailer, manufacturer);
            context.Products.Add(product);
            context.Order.Add(order);
            await context.SaveChangesAsync();

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                orderId: null,
                retailerId: null,
                deliveryPersonnelId: null,
                orderStatus: null,
                manufacturerId: null,
                orderItemStatus: null,
                retailerName: "Retailer A",
                manufacturerName: null,
                productName: null,
                pageNumber: 1,
                pageSize: 10
            );

            // Assert
            results.Should().NotBeNull();
            results.Should().HaveCount(1);
            results.First().RetailerName.Should().Be("Retailer A");
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByOrderId()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderId: orderId);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                orderId, null, null, null, null, null, null, null, null, 1, 10);

            // Assert
            results.Should().ContainSingle(o => o.OrderID == orderId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByRetailerId()
        {
            // Arrange
            var retailerId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, retailerId: retailerId);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, retailerId, null, null, null, null, null, null, null, 1, 10);

            // Assert
            results.Should().OnlyContain(o => o.RetailerID == retailerId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByDeliveryPersonnelId()
        {
            // Arrange
            var deliveryPersonnelId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, deliveryPersonnelId: deliveryPersonnelId);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, deliveryPersonnelId, null, null, null, null, null, null, 1, 10);

            // Assert
            results.Should().OnlyContain(o => o.DeliveryPersonnelID == deliveryPersonnelId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByOrderStatus()
        {
            // Arrange
            int orderStatus = 2;
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderStatus: orderStatus);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, orderStatus, null, null, null, null, null, 1, 10);

            // Assert
            results.Should().OnlyContain(o => o.OrderStatus == orderStatus);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByManufacturerId()
        {
            // Arrange
            var manufacturerId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, manufacturerId: manufacturerId);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, manufacturerId, null, null, null, null, 1, 10);

            // Assert
            results.SelectMany(o => o.OrderDetails).Should().OnlyContain(d => d.ManufacturerID == manufacturerId);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByOrderItemStatus()
        {
            // Arrange
            int itemStatus = 1;
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, orderItemStatus: itemStatus);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, itemStatus, null, null, null, 1, 10);

            // Assert
            results.SelectMany(o => o.OrderDetails).Should().OnlyContain(d => d.OrderItemStatus == itemStatus);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_ByProductName()
        {
            // Arrange
            var productName = "SpecialProduct";
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, productName: productName);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, productName, 1, 10);

            // Assert
            results.SelectMany(o => o.OrderDetails).Should().OnlyContain(d => d.ProductName.Contains("SpecialProduct"));
        }


        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnOrders_WhenNoFiltersAreApplied()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context);

            var repo = new OrderRepository(context);

            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, null, null, null, 1, 10);

            // Assert
            results.SelectMany(o => o.OrderDetails).Should();
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnEmptyResults_WhenNoOrdersMatch()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                Guid.NewGuid(), null, null, null, null, null, null, null, null, 1, 10); // Non-existent OrderID
            // Assert
            results.Should().BeEmpty();
            totalPages.Should().Be(0);
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_WhenMultipleFiltersAreApplied()
        {
            // Arrange
            var retailerId = Guid.NewGuid();
            var manufacturerId = Guid.NewGuid();
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, retailerId: retailerId, manufacturerId: manufacturerId);
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, retailerId, null, null, manufacturerId, null, null, null, null, 1, 10);
            // Assert
            results.Should().OnlyContain(o =>
                o.RetailerID == retailerId &&
                o.OrderDetails.Any(d => d.ManufacturerID == manufacturerId));
        }

        [Fact]
        public async Task GetFilteredOrdersAsync_ShouldReturnFilteredOrders_WhenStringFiltersAreCaseInsensitive()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await TestDataSeeder.SeedOrderWithDetailsAsync(context, retailerName: "Test Retailer");
            var repo = new OrderRepository(context);
            // Act
            var (results, totalPages) = await repo.GetFilteredOrdersAsync(
                null, null, null, null, null, null, "test retailer", null, null, 1, 10); // Lowercase filter
            // Assert
            results.Should().NotBeEmpty();
            results.First().RetailerName.Should().Be("Test Retailer");
        }

        //[Fact]
        //public async Task GetFilteredOrdersAsync_ShouldReturnSortedOrders()
        //{
        //    // Arrange
        //    using var context = TestDbContextFactory.Create();
        //    await TestDataSeeder.SeedOrderWithDetailsAsync(context);
        //    await TestDataSeeder.SeedOrderWithDetailsAsync(context);
        //    var repo = new OrderRepository(context);
        //    // Act
        //    var (results, totalPages) = await repo.GetFilteredOrdersAsync(
        //        null, null, null, null, null, null, null, null, null, 1, 10); // Default sorting
        //    // Assert
        //    results.Should().BeInAscendingOrder(o => o.CreatedDate); // Example: Sorted by CreatedDate
        //}









    }
}

// Utility to create isolated in-memory test DbContext instances
[ExcludeFromCodeCoverage]
public static class TestDbContextFactory
{
    public static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Unique database for each test
            .Options;
    }
    public static AppDbContext Create()
    {
        return new AppDbContext(CreateOptions());
    }
}

// Utility to seed consistent order data with parameters for testing
[ExcludeFromCodeCoverage]
public static class TestDataSeeder
{
    public static async Task SeedOrderWithDetailsAsync(
        AppDbContext context,
        Guid? orderId = null,
        Guid? retailerId = null,
        Guid? deliveryPersonnelId = null,
        int? orderStatus = null,
        Guid? manufacturerId = null,
        int? orderItemStatus = null,
        string productName = "Test Product",
        string manufacturerName = "Test Manufacturer",
        string retailerName = "Test Retailer")
    {
        var actualOrderId = orderId ?? Guid.NewGuid();
        var actualRetailerId = retailerId ?? Guid.NewGuid();
        var actualDeliveryPersonnelId = deliveryPersonnelId ?? Guid.NewGuid();
        var actualManufacturerId = manufacturerId ?? Guid.NewGuid();
        var actualOrderItemStatus = orderItemStatus ?? 1;

        var productId = Guid.NewGuid();

        var order = new Order
        {
            OrderID = actualOrderId,
            RetailerID = actualRetailerId,
            DeliveryPersonnelID = actualDeliveryPersonnelId,
            OrderStatus = orderStatus ?? 1,
            TotalPrice = 100,
            PaymentCurrency = "USD",
            PaymentMode = 1,
            ShippingCost = 10,
            ShippingCurrency = "USD",
            ShippingAddress = "123 Test St",
            CreatedBy = Guid.NewGuid(),
            OrderDetails = new List<OrderDetails>
            {
                new OrderDetails
                {
                    OrderDetailID = Guid.NewGuid(),
                    ProductID = productId,
                    ManufacturerID = actualManufacturerId,
                    Quantity = 5,
                    OrderItemStatus = actualOrderItemStatus,
                    ProductPrice = 20
                }
            }
        };

        await context.Order.AddAsync(order);
        await context.Products.AddAsync(new Product
        {
            ProductID = productId,
            ProductName = productName
        });

        await context.Users.AddRangeAsync(
            new User { UserID = actualManufacturerId, UserName = manufacturerName, Address = "123", PhoneNo = "88889999" },
            new User { UserID = actualRetailerId, UserName = retailerName, Address = "123", PhoneNo = "88889999" }
        );

        await context.SaveChangesAsync();
    }
}

[ExcludeFromCodeCoverage]
public class FailingSaveChangesContext : AppDbContext
{
    public FailingSaveChangesContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0); // Simulate failure
    }
}





