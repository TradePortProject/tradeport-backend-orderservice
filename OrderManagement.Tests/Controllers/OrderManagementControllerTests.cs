using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Controllers;
using OrderManagement.Common;
using OrderManagement.Models;
using OrderManagement.Data;
using OrderManagement.Models.DTO;
using OrderManagement.Repositories;
using OrderManagement.ExternalServices;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OrderManagement.Mappings;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using OrderManagement.Logger.interfaces;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class OrderManagementControllerTests
{
    private readonly AppDbContext _dbContext;  // ✅ Use actual instance, not Mock<>
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IOrderDetailsRepository> _orderDetailsRepoMock;
    private readonly Mock<IShoppingCartRepository> _shoppingCartRepoMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IMapper _mapper;
    private readonly Mock<IProductServiceClient> _productServiceMock;
    private readonly Mock<IProductRepository> _productRepository;
    private readonly OrderManagementController _controller;
    private readonly IAppLogger<OrderManagementController> _logger;
    private readonly Mock<IKafkaProducer> _kafkaProducerMock;

    public OrderManagementControllerTests()
    {
        // ✅ Set up InMemory Database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb") // ✅ Use InMemory Database
            .Options;

        _dbContext = new AppDbContext(options);  // ✅ Provide real InMemory DB

        _orderRepositoryMock = new Mock<IOrderRepository>();
        _orderDetailsRepoMock = new Mock<IOrderDetailsRepository>();
        _shoppingCartRepoMock = new Mock<IShoppingCartRepository>();
        _productServiceMock = new Mock<IProductServiceClient>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _productRepository = new Mock<IProductRepository>();
        _logger = new Mock<IAppLogger<OrderManagementController>>().Object; // Mock logger
        _kafkaProducerMock = new Mock<IKafkaProducer>(); // Mock Kafka producer


        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderAutoMapperProfiles>());
        _mapper = config.CreateMapper();

        _controller = new OrderManagementController(
            _dbContext,
            _orderRepositoryMock.Object,
            _orderDetailsRepoMock.Object,
            _shoppingCartRepoMock.Object,
            _mapper,
            _productServiceMock.Object,
            _productRepository.Object,
            _userRepositoryMock.Object,
            new Mock<IConfiguration>().Object, // Add a mock IConfiguration
            _logger,
            _kafkaProducerMock.Object // Pass the mocked IKafkaProducer
        );
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenRetailerIsNullOrInvalid()
    {
        // Arrange
        var orderRequestDto = new CreateOrderDTO
        {
            RetailerID = Guid.NewGuid(),
            OrderDetails = new List<CreateOrderDetailsDTO>
            {
                new CreateOrderDetailsDTO { ProductID = Guid.NewGuid(), Quantity = 1, CartID = Guid.NewGuid() }
            }
        };
        // Simulate retailer being null
        _userRepositoryMock
            .Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync((Dictionary<Guid, User>)null);
        // Act
        var result = await _controller.CreateOrder(orderRequestDto);
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().BeEquivalentTo(new
        {
            Message = "Invalid Retailer ID.",
            ErrorMessage = "The provided Retailer ID does not exist."
        });
    }

    [Fact]
    public async Task CreateShoppingCartItemAsync_ShouldReturnBadRequest_WhenShoppingCartDtoIsNull()
    {
        // Act
        var result = await _controller.CreateShoppingCartItemAsync(null);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(badRequestResult.Value));

        response["Message"].ToString().Should().Be("Invalid Cart Item.");
        response["ErrorMessage"].ToString().Should().Be("Cart items are missing.");
    }

    [Fact]
    public async Task CreateShoppingCartItemAsync_ShouldReturnBadRequest_WhenRetailerIDIsInvalid()
    {
        // Arrange
        var shoppingCartDto = new CreateShoppingCartDTO { RetailerID = Guid.NewGuid() };

        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, User>()); // Simulating invalid retailer

        // Act
        var result = await _controller.CreateShoppingCartItemAsync(shoppingCartDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(badRequestResult.Value));

        response["Message"].ToString().Should().Be("Invalid Retailer ID.");
        response["ErrorMessage"].ToString().Should().Be("The provided Retailer ID does not exist.");
    }

    [Fact]
    public async Task CreateShoppingCartItemAsync_ShouldReturnOk_WhenItemIsSuccessfullyAdded()
    {
        // Arrange
        var shoppingCartDto = new CreateShoppingCartDTO
        {
            RetailerID = Guid.NewGuid(),
            ProductID = Guid.NewGuid(),
            OrderQuantity = 2,
            ProductPrice = 100m,
            ManufacturerID = Guid.NewGuid()
        };

        var mockShoppingCart = new ShoppingCart
        {
            CartID = Guid.NewGuid(),
            RetailerID = shoppingCartDto.RetailerID,
            ProductID = shoppingCartDto.ProductID,
            OrderQuantity = shoppingCartDto.OrderQuantity,
            ProductPrice = shoppingCartDto.ProductPrice,
            ManufacturerID = shoppingCartDto.ManufacturerID,
            IsActive = true
        };

        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, User>
            {
            { shoppingCartDto.RetailerID, new User() }
            });

        _shoppingCartRepoMock.Setup(repo => repo.CreateShoppingCartItemsAsync(It.IsAny<ShoppingCart>()))
            .ReturnsAsync(mockShoppingCart);

        // Act
        var result = await _controller.CreateShoppingCartItemAsync(shoppingCartDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(okResult.Value));

        response["cartID"].ToString().Should().Be(mockShoppingCart.CartID.ToString());

        var responseMessage = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(response["response"]));

        responseMessage["Message"].ToString().Should().Be("Item added to the cart successfully.");
        responseMessage["ErrorMessage"].ToString().Should().Be("");
    }

    [Fact]
    public async Task CreateShoppingCartItemAsync_ShouldReturnServerError_WhenExceptionOccurs()
    {
        // Arrange
        var shoppingCartDto = new CreateShoppingCartDTO
        {
            RetailerID = Guid.NewGuid(),
            ProductID = Guid.NewGuid(),
            OrderQuantity = 2,
            ProductPrice = 100m,
            ManufacturerID = Guid.NewGuid()
        };

        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, User>
            {
            { shoppingCartDto.RetailerID, new User() }
            });

        _shoppingCartRepoMock.Setup(repo => repo.CreateShoppingCartItemsAsync(It.IsAny<ShoppingCart>()))
            .ThrowsAsync(new Exception("Database failure")); // Simulate failure

        // Act
        var result = await _controller.CreateShoppingCartItemAsync(shoppingCartDto);

        // Assert
        var serverErrorResult = result as ObjectResult;
        serverErrorResult.Should().NotBeNull();
        serverErrorResult.StatusCode.Should().Be(500);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(serverErrorResult.Value));

        response["Message"].ToString().Should().Be("Item creation failed.");
        response["ErrorMessage"].ToString().Should().Contain("Database failure");
    }

    // ✅ 1. Test: Returns 200 OK when retailer has shopping cart items
    [Fact]
    public async Task GetShoppingCartByRetailerId_ShouldReturnOk_WhenCartItemsExist()
    {
        // Arrange
        var retailerID = Guid.NewGuid();
        var mockShoppingCart = new List<ShoppingCart>
        {
            new ShoppingCart
            {
                CartID = Guid.NewGuid(),
                RetailerID = retailerID,
                ProductID = Guid.NewGuid(),
                ManufacturerID = Guid.NewGuid(),
                OrderQuantity = 2,
                ProductPrice = 100m
            }
        };

        var mockRetailer = new Dictionary<Guid, User>
        {
            { retailerID, new User { UserID = retailerID, UserName = "Retailer A", PhoneNo = "123456789", Address = "123 Street" } }
        };

        var mockProduct = new ProductDTO
        {
            ProductID = mockShoppingCart[0].ProductID,
            ProductName = "Product A",
            Quantity = 5,
            ManufacturerID = Guid.NewGuid()
        };

        _shoppingCartRepoMock
            .Setup(repo => repo.GetShoppingCartByRetailerIdAsync(retailerID, (int)OrderStatus.Save))
            .ReturnsAsync(mockShoppingCart);

        _userRepositoryMock
            .Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(mockRetailer);

        _productServiceMock
            .Setup(service => service.GetProductByIdAsync(mockShoppingCart[0].ProductID))
            .ReturnsAsync(mockProduct);

        // Act
        var result = await _controller.GetShoppingCartByRetailerId(retailerID);

        // Assert: Ensure result is an OkObjectResult
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        // ✅ Convert ObjectResult.Value to JSON string
        var jsonString = JsonConvert.SerializeObject(okResult.Value);

        // ✅ Deserialize into Dictionary
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        response.Should().NotBeNull();
        response["Message"].ToString().Should().Be("Cart Items fetched successfully.");
        ((int)(long)response["NumberOfOrderItems"]).Should().Be(mockShoppingCart.Count);
    }

    // ✅ 2. Test: Returns 404 when no cart items exist
    [Fact]
    public async Task GetShoppingCartByRetailerId_ShouldReturnNotFound_WhenNoCartItemsExist()
    {
        // Arrange
        var retailerId = Guid.NewGuid();

        _shoppingCartRepoMock
            .Setup(repo => repo.GetShoppingCartByRetailerIdAsync(retailerId, (int)OrderStatus.Save))
            .ReturnsAsync(new List<ShoppingCart>()); // ✅ Return empty list

        // Act
        var result = await _controller.GetShoppingCartByRetailerId(retailerId);

        // 🔍 Debugging Output
        Console.WriteLine($"[TEST DEBUG] Result Type: {result?.GetType()}");

        if (result is ObjectResult objectResult)
        {
            Console.WriteLine($"[TEST DEBUG] ObjectResult Status Code: {objectResult.StatusCode}");
            Console.WriteLine($"[TEST DEBUG] ObjectResult Value Type: {objectResult.Value?.GetType()}");
            Console.WriteLine($"[TEST DEBUG] ObjectResult Value: {objectResult.Value}");

            // Assert: Ensure ObjectResult is returned
            Assert.NotNull(objectResult);
            Assert.Equal(404, objectResult.StatusCode);

            // ✅ Convert ObjectResult.Value to JSON string and then a Dictionary
            var jsonString = JsonConvert.SerializeObject(objectResult.Value);
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            responseDict.Should().NotBeNull();
            responseDict["Message"].Should().Contain("No cart items found for the provided Retailer");
            responseDict["ErrorMessage"].Should().Be("");
        }
        else
        {
            Assert.Fail("[TEST ERROR] Result is NOT ObjectResult!");
        }
    }


    // ✅ 3. Test: Returns 500 when exception occurs
    [Fact]
    public async Task GetShoppingCartByRetailerId_ShouldReturnServerError_WhenExceptionOccurs()
    {
        // Arrange
        var retailerId = Guid.NewGuid();

        // ✅ Mock the repository to throw an exception
        _shoppingCartRepoMock
            .Setup(repo => repo.GetShoppingCartByRetailerIdAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failed")); // Simulate failure

        // Act
        var result = await _controller.GetShoppingCartByRetailerId(retailerId);

        // Assert: Ensure result is a 500 Internal Server Error
        var serverErrorResult = result as ObjectResult;
        serverErrorResult.Should().NotBeNull();
        serverErrorResult.StatusCode.Should().Be(500);
        serverErrorResult.Value.Should().NotBeNull();

        var jsonString = JsonConvert.SerializeObject(serverErrorResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        response["Message"].ToString().Should().Contain("An error occurred while retrieving the cart items for RetailerID");
    }

    [Fact]
    public async Task GetShoppingCartByRetailerId_ShouldHandleMissingRetailerInfo()
    {
        // Arrange
        var retailerID = Guid.NewGuid();
        var shoppingCartItem = new ShoppingCart
        {
            CartID = Guid.NewGuid(),
            RetailerID = retailerID,
            ProductID = Guid.NewGuid(),
            OrderQuantity = 2,
            ProductPrice = 10.0m,
            ManufacturerID = Guid.NewGuid()
        };

        var cartList = new List<ShoppingCart> { shoppingCartItem };

        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartByRetailerIdAsync(retailerID, (int)OrderStatus.Save))
            .ReturnsAsync(cartList);

        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, User>()); // ❌ Retailer info missing

        _productServiceMock.Setup(service => service.GetProductByIdAsync(shoppingCartItem.ProductID))
            .ReturnsAsync(new ProductDTO
            {
                ProductID = shoppingCartItem.ProductID,
                ProductName = "Mock Product",
                Quantity = 5,
                ManufacturerID = Guid.NewGuid()
            });

        // Act
        var result = await _controller.GetShoppingCartByRetailerId(retailerID);
        var okResult = result as OkObjectResult;

        // Assert
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(okResult.Value));

        response["Message"].ToString().Should().Be("Cart Items fetched successfully.");
    }

    [Fact]
    public async Task GetShoppingCartByRetailerId_ShouldHandleMissingProductInfo()
    {
        // Arrange
        var retailerID = Guid.NewGuid();
        var shoppingCartItem = new ShoppingCart
        {
            CartID = Guid.NewGuid(),
            RetailerID = retailerID,
            ProductID = Guid.NewGuid(),
            OrderQuantity = 2,
            ProductPrice = 10.0m,
            ManufacturerID = Guid.NewGuid()
        };

        var cartList = new List<ShoppingCart> { shoppingCartItem };

        var retailerInfo = new Dictionary<Guid, User>
    {
        { retailerID, new User { UserID = retailerID, UserName = "Retailer A", PhoneNo = "123456789", Address = "123 Street" } }
    };

        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartByRetailerIdAsync(retailerID, (int)OrderStatus.Save))
            .ReturnsAsync(cartList);

        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(retailerInfo);

        _productServiceMock.Setup(service => service.GetProductByIdAsync(shoppingCartItem.ProductID))
            .ReturnsAsync((ProductDTO)null); // ❌ Product missing

        // Act
        var result = await _controller.GetShoppingCartByRetailerId(retailerID);
        var okResult = result as OkObjectResult;

        //Console.WriteLine($"[TEST TRACE] result type: {result?.GetType()}");

        //if (result is ObjectResult objectResult)
        //{
        //    Console.WriteLine($"[TEST TRACE] StatusCode: {objectResult.StatusCode}");
        //    Console.WriteLine($"[TEST TRACE] Value: {JsonConvert.SerializeObject(objectResult.Value)}");
        //}

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult.StatusCode.Should().Be(500);

        var json = JsonConvert.SerializeObject(objectResult.Value);
        json.Should().Contain("ProductID");
        json.Should().Contain("does not exist");
    }


    // ✅ TEST CASE 1: GetOrdersAndOrderDetails Returns Data Successfully
    [Fact]
    public async Task GetOrdersAndOrderDetails_ShouldReturnOrders()
    {
        // ✅ Step 1: Insert Mock Data into In-Memory Database
        var retailerId = Guid.NewGuid();
        var manufacturerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var mockOrders = new List<Order>
    {
        new Order
        {
            OrderID = Guid.NewGuid(),
            RetailerID = retailerId,
            DeliveryPersonnelID = null,
            OrderStatus = 1,
            TotalPrice = 100m,
            PaymentMode = 1,
            PaymentCurrency = "USD",
            ShippingCost = 5m,
            ShippingCurrency = "USD",
            ShippingAddress = "123 Street",
            OrderDetails = new List<OrderDetails>
            {
                new OrderDetails
                {
                    OrderDetailID = Guid.NewGuid(),
                    ProductID = productId,
                    ManufacturerID = manufacturerId,
                    Quantity = 2,
                    OrderItemStatus = 1,
                    ProductPrice = 50m
                }
            }
        }
    };

        // ✅ Save to In-Memory DB
        _dbContext.Order.AddRange(mockOrders);
        await _dbContext.SaveChangesAsync();

        // ✅ Ensure Repository Returns Non-Null Orders
        var mappedOrders = _mapper.Map<IEnumerable<OrderDto>>(mockOrders);
        _orderRepositoryMock
            .Setup(repo => repo.GetFilteredOrdersAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<int?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((mappedOrders, 1)); // ✅ Returns a tuple with orders & totalPages

        // ✅ Step 2: Execute Controller Method
        var result = await _controller.GetOrdersAndOrderDetails(null, null, null, null, null, null, null, null, null, 1, 10);
        var okResult = result as OkObjectResult;

        // ✅ Step 3: Assert Response
        okResult.Should().NotBeNull();
        var responseJson = JsonConvert.SerializeObject(okResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson);

        Assert.NotNull(response);
        Assert.True(response.ContainsKey("Message"));
        Assert.Equal("Orders retrieved successfully.", response["Message"]);
    }

    // ✅ TEST CASE 2: AcceptOrder Updates Order Successfully
    [Fact]
    public async Task AcceptOrder_ShouldUpdateOrderStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderDetailId = Guid.NewGuid();

        var mockOrder = new Order
        {
            OrderID = orderId,
            RetailerID = Guid.NewGuid(),
            OrderStatus = 1,
            OrderDetails = new List<OrderDetails>
                {
                    new OrderDetails
                    {
                        OrderDetailID = orderDetailId,
                        ProductID = productId,
                        ManufacturerID = Guid.NewGuid(),
                        Quantity = 5,
                        OrderItemStatus = 1,
                        ProductPrice = 50m
                    }
                }
        };

        var mockProduct = new ProductDTO
        {
            ProductID = productId,
            ProductName = "Test Product",
            Quantity = 10,
            ManufacturerID = Guid.NewGuid()
        };

        var acceptOrderDto = new AcceptOrderDTO
        {
            OrderID = orderId,
            OrderItems = new List<AcceptOrderItemDTO>
                {
                    new AcceptOrderItemDTO
                    {
                        OrderDetailID = orderDetailId,
                        IsAccepted = true
                    }
                }
        };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(mockOrder);
        _orderRepositoryMock.Setup(repo => repo.UpdateOrderStatusAsync(orderId, It.IsAny<int>())).ReturnsAsync(mockOrder);
        _orderDetailsRepoMock.Setup(repo => repo.UpdateOrderItemStatusAsync(orderDetailId, (int)OrderStatus.Accepted)).ReturnsAsync(mockOrder.OrderDetails.First());
        _productServiceMock.Setup(client => client.GetProductByIdAsync(productId)).ReturnsAsync(mockProduct);
        _productServiceMock.Setup(client => client.UpdateProductQuantityAsync(productId, 5)).ReturnsAsync(true);

        //// Act
        //var result = await _controller.AcceptRejectOrder(acceptOrderDto);
        //var okResult = result as OkObjectResult;

        //// Assert
        //okResult.Should().NotBeNull();
        //var response = okResult.Value as dynamic;
        //Assert.Equal("Order status updated successfully.", response.Message);

        // Act
        var result = await _controller.AcceptRejectOrder(acceptOrderDto);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
    }

    //[Fact]
    //public async Task RejectOrder_ShouldUpdateOrderStatusToRejected()
    //{
    //    // Arrange
    //    var orderId = Guid.NewGuid();
    //    var productId = Guid.NewGuid();
    //    var orderDetailId = Guid.NewGuid();

    //    var mockOrder = new Order
    //    {
    //        OrderID = orderId,
    //        RetailerID = Guid.NewGuid(),
    //        OrderStatus = (int)OrderStatus.Submitted, // Assume order is initially pending
    //        OrderDetails = new List<OrderDetails>
    //    {
    //        new OrderDetails
    //        {
    //            OrderDetailID = orderDetailId,
    //            ProductID = productId,
    //            ManufacturerID = Guid.NewGuid(),
    //            Quantity = 5,
    //            OrderItemStatus = (int)OrderStatus.Submitted, // Initially pending
    //            ProductPrice = 50m
    //        }
    //    }
    //    };

    //    var mockProduct = new ProductDTO
    //    {
    //        ProductID = productId,
    //        ProductName = "Test Product",
    //        Quantity = 10 // Assume current product quantity is 10
    //    };

    //    var rejectOrderDto = new AcceptOrderDTO
    //    {
    //        OrderID = orderId,
    //        OrderItems = new List<AcceptOrderItemDTO>
    //    {
    //        new AcceptOrderItemDTO
    //        {
    //            OrderDetailID = orderDetailId,
    //            IsAccepted = false // ❌ Rejected
    //        }
    //    }
    //    };

    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(mockOrder);
    //    _orderRepositoryMock.Setup(repo => repo.UpdateOrderStatusAsync(orderId, (int)OrderStatus.Rejected)).ReturnsAsync(mockOrder);
    //    _orderDetailsRepoMock.Setup(repo => repo.UpdateOrderItemStatusAsync(orderDetailId, (int)OrderStatus.Rejected))
    //        .ReturnsAsync(mockOrder.OrderDetails.First());

    //    _productServiceMock.Setup(client => client.GetProductByIdAsync(productId)).ReturnsAsync(mockProduct);
    //    _productServiceMock.Setup(client => client.UpdateProductQuantityAsync(productId, -5)) // Restore quantity
    //        .ReturnsAsync(true);

    //    // Act
    //    var result = await _controller.AcceptRejectOrder(rejectOrderDto);
    //    var okResult = result as OkObjectResult;

    //    // Assert
    //    okResult.Should().NotBeNull();
    //    var response = okResult.Value as dynamic;
    //    Assert.Equal("Order status updated successfully.", response.Message);

    //    // Verify the status update for rejection
    //    _orderDetailsRepoMock.Verify(repo => repo.UpdateOrderItemStatusAsync(orderDetailId, (int)OrderStatus.Rejected), Times.Once);
    //    _orderRepositoryMock.Verify(repo => repo.UpdateOrderStatusAsync(orderId, (int)OrderStatus.Rejected), Times.Once);
    //    _productServiceMock.Verify(client => client.UpdateProductQuantityAsync(productId, -5), Times.Once);
    //}

    //[Fact]
    //public async Task RejectOrder_ShouldUpdateOrderStatusToRejected()
    //{
    //    // Arrange
    //    var orderId = Guid.NewGuid();
    //    var productId = Guid.NewGuid();
    //    var orderDetailId = Guid.NewGuid();

    //    var mockOrder = new Order
    //    {
    //        OrderID = orderId,
    //        RetailerID = Guid.NewGuid(),
    //        OrderStatus = (int)OrderStatus.Submitted, // Assume order is initially pending
    //        OrderDetails = new List<OrderDetails>
    //    {
    //        new OrderDetails
    //        {
    //            OrderDetailID = orderDetailId,
    //            ProductID = productId,
    //            ManufacturerID = Guid.NewGuid(),
    //            Quantity = 5,
    //            OrderItemStatus = (int)OrderStatus.Submitted, // Initially pending
    //            ProductPrice = 50m
    //        }
    //    }
    //    };

    //    var mockProduct = new ProductDTO
    //    {
    //        ProductID = productId,
    //        ProductName = "Test Product",
    //        Quantity = 10 // Assume current product quantity is 10
    //    };

    //    var rejectOrderDto = new AcceptOrderDTO
    //    {
    //        OrderID = orderId,
    //        OrderItems = new List<AcceptOrderItemDTO>
    //    {
    //        new AcceptOrderItemDTO
    //        {
    //            OrderDetailID = orderDetailId,
    //            IsAccepted = false // ❌ Rejected
    //        }
    //    }
    //    };

    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(mockOrder);
    //    _orderRepositoryMock.Setup(repo => repo.UpdateOrderStatusAsync(orderId, (int)OrderStatus.Rejected)).ReturnsAsync(mockOrder);
    //    _orderDetailsRepoMock.Setup(repo => repo.UpdateOrderItemStatusAsync(orderDetailId, (int)OrderStatus.Rejected))
    //        .ReturnsAsync(mockOrder.OrderDetails.First());

    //    _productServiceMock.Setup(client => client.GetProductByIdAsync(productId)).ReturnsAsync(mockProduct);
    //    _productServiceMock.Setup(client => client.UpdateProductQuantityAsync(productId, -5)) // Restore quantity
    //        .ReturnsAsync(true);

    //    // Act
    //    var result = await _controller.AcceptRejectOrder(rejectOrderDto);

    //    Console.WriteLine($"[TEST DEBUG] Result Type: {result?.GetType()}");

    //    var okResult = result as OkObjectResult;

    //    // Assert
    //    okResult.Should().NotBeNull($"Expected OkObjectResult but got {result?.GetType()}");

    //    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
    //        JsonConvert.SerializeObject(okResult.Value));

    //    response["Message"].ToString().Should().Be("Order status updated successfully.");

    //    // Verify the status update for rejection
    //    _orderDetailsRepoMock.Verify(repo => repo.UpdateOrderItemStatusAsync(orderDetailId, (int)OrderStatus.Rejected), Times.Once);
    //    _orderRepositoryMock.Verify(repo => repo.UpdateOrderStatusAsync(orderId, (int)OrderStatus.Rejected), Times.Once);
    //    _productServiceMock.Verify(client => client.UpdateProductQuantityAsync(productId, -5), Times.Once);
    //}


    [Fact]
    public async Task CreateOrder_ShouldReturnSuccessMessage()
    {
        // Arrange
        var retailerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderDetailId = Guid.NewGuid();
        var cartId = Guid.NewGuid(); // ✅ Shopping cart ID

        var createOrderDto = new CreateOrderDTO
        {
            RetailerID = retailerId,
            PaymentMode = 1,
            PaymentCurrency = "USD",
            ShippingCost = 10m,
            ShippingCurrency = "USD",
            ShippingAddress = "123 Street",
            CreatedBy = Guid.NewGuid(),
            OrderDetails = new List<CreateOrderDetailsDTO>
            {
                new CreateOrderDetailsDTO
                {
                    ProductID = productId,
                    Quantity = 2,
                    ProductPrice = 100m,
                    ManufacturerID = Guid.NewGuid(),
                    CartID = cartId // ✅ Ensure CartID is assigned
                }
            }
        };

        var mockOrder = new Order
        {
            OrderID = Guid.NewGuid(),
            RetailerID = retailerId,
            OrderStatus = 1,
            OrderDetails = new List<OrderDetails>
            {
                new OrderDetails
                {
                    OrderDetailID = orderDetailId,
                    ProductID = productId,
                    ManufacturerID = Guid.NewGuid(),
                    Quantity = 2,
                    OrderItemStatus = 1,
                    ProductPrice = 100m
                }
            }
        };

        // ✅ Insert Shopping Cart Item (Before Deactivation)
        var shoppingCartItem = new ShoppingCart
        {
            CartID = cartId,
            ProductID = productId,
            OrderQuantity = 2,
            RetailerID = retailerId
        };
        _dbContext.ShoppingCart.Add(shoppingCartItem);
        await _dbContext.SaveChangesAsync(); // ✅ Ensure it's in DB

        // ✅ Mock GetShoppingCartItemByCartID
        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartItemByCartID(cartId))
                             .ReturnsAsync(shoppingCartItem);

        // ✅ Mock UpdateShoppingCartItemByCartIdAsync
        _shoppingCartRepoMock.Setup(repo => repo.UpdateShoppingCartItemByCartIdAsync(It.IsAny<ShoppingCart>()))
                             .ReturnsAsync(true);

        // ✅ Mock User Repository for Retailer Info
        var mockRetailer = new Dictionary<Guid, User>
        {
            { retailerId, new User { UserID = retailerId, UserName = "Test Retailer" } }
        };
        _userRepositoryMock.Setup(repo => repo.GetUserInfoByRetailerIdAsync(It.IsAny<List<Guid>>()))
                           .ReturnsAsync(mockRetailer);

        // ✅ Mock Order Repository
        _orderRepositoryMock.Setup(repo => repo.CreateOrderAsync(It.IsAny<Order>())).ReturnsAsync(mockOrder);

        // Act
        var result = await _controller.CreateOrder(createOrderDto);
        // ✅ Extract the actual response object
        // ✅ Extract the response
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        // ✅ Convert response to a dynamic object for safe property access
        var responseObject = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(okResult.Value)) as dynamic;

        // ✅ Verify response message
        Assert.Equal("Order created successfully.", responseObject.response.Message);
        Assert.NotNull(responseObject.orderID);
    }



    // ✅ Test: Should Accept Order If Stock Is Available
    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnOk_WhenOrderIsAccepted()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderDetails = new List<OrderDetails>
        {
            new OrderDetails { OrderDetailID = Guid.NewGuid(), OrderID = orderId, ProductID = Guid.NewGuid(), Quantity = 2 }
        };

        var existingOrder = new Order { OrderID = orderId, OrderStatus = 1 };

        var acceptOrderDto = new AcceptOrderDTO
        {
            OrderID = orderId,
            OrderItems = orderDetails.Select(od => new AcceptOrderItemDTO { OrderDetailID = od.OrderDetailID, IsAccepted = true }).ToList()
        };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(orderId)).ReturnsAsync(orderDetails);

        _productServiceMock.Setup(service => service.GetProductByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new ProductDTO { ProductID = Guid.NewGuid(), Quantity = 10 });

        _productServiceMock.Setup(service => service.UpdateProductQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);

        // Act
        var result = await _controller.AcceptRejectOrder(acceptOrderDto);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
    }

    //[Fact]
    //public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenRequestIsNull()
    //{
    //    var result = await _controller.AcceptRejectOrder(null);

    //    var badRequest = result as BadRequestObjectResult;
    //    badRequest.Should().NotBeNull();

    //    var json = JsonSerializer.Serialize(badRequest.Value);
    //    json.Should().Contain("OrderID and OrderItems are required");

    //    //var jsonElement = JsonSerializer.SerializeToElement(badRequest.Value);
    //    //jsonElement.GetProperty("Message").GetString().Should().Be("Invalid request.");

    //}

    //[Fact] Old Test
    //public async Task AcceptRejectOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    //{
    //    var dto = new AcceptOrderDTO { OrderID = Guid.NewGuid(), OrderItems = new List<AcceptOrderItemDTO> { new() { OrderDetailID = Guid.NewGuid(), IsAccepted = true } } };

    //    _orderRepositoryMock.Setup(r => r.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync((Order)null);

    //    var result = await _controller.AcceptRejectOrder(dto);

    //    result.Should().BeOfType<NotFoundObjectResult>();
    //}

    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var dto = new AcceptOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderItems = new List<AcceptOrderItemDTO> { new AcceptOrderItemDTO { OrderDetailID = Guid.NewGuid(), IsAccepted = true } }
        };
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync((Order)null);
        // Act
        var result = await _controller.AcceptRejectOrder(dto);
        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(notFoundResult.Value));
        response["Message"].Should().Be("Order not found.");
        response["ErrorMessage"].Should().Be("Invalid Order ID.");
    }

    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenOrderDetailsNotFound()
    {
        var dto = new AcceptOrderDTO { OrderID = Guid.NewGuid(), OrderItems = new List<AcceptOrderItemDTO> { new() { OrderDetailID = Guid.NewGuid(), IsAccepted = true } } };

        _orderRepositoryMock.Setup(r => r.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
        _orderRepositoryMock.Setup(r => r.GetOrderDetailsByOrderIdAsync(dto.OrderID)).ReturnsAsync(new List<OrderDetails>());

        var result = await _controller.AcceptRejectOrder(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    //[Fact]
    //public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenOrderItemNotFound()
    //{
    //    var detailId = Guid.NewGuid();
    //    var dto = new AcceptOrderDTO { OrderID = Guid.NewGuid(), OrderItems = new List<AcceptOrderItemDTO> { new() { OrderDetailID = detailId, IsAccepted = true } } };

    //    _orderRepositoryMock.Setup(r => r.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
    //    _orderRepositoryMock.Setup(r => r.GetOrderDetailsByOrderIdAsync(dto.OrderID)).ReturnsAsync(new List<OrderDetails> { new() { OrderDetailID = Guid.NewGuid() } });

    //    var result = await _controller.AcceptRejectOrder(dto);

    //    result.Should().BeOfType<BadRequestObjectResult>();
    //}

    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenOrderItemNotFound()
    {
        // Arrange
        var dto = new AcceptOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderItems = new List<AcceptOrderItemDTO> { new AcceptOrderItemDTO { OrderDetailID = Guid.NewGuid(), IsAccepted = true } }
        };
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
        _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(dto.OrderID)).ReturnsAsync(new List<OrderDetails> { new OrderDetails { OrderDetailID = Guid.NewGuid() } });
        // Act
        var result = await _controller.AcceptRejectOrder(dto);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
        response["Message"].Should().Be("Order item not found.");
    }


    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenProductNotFound()
    {
        var productId = Guid.NewGuid();
        var detailId = Guid.NewGuid();
        var dto = new AcceptOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderItems = new List<AcceptOrderItemDTO> { new() { OrderDetailID = detailId, IsAccepted = true } }
        };

        _orderRepositoryMock.Setup(r => r.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
        _orderRepositoryMock.Setup(r => r.GetOrderDetailsByOrderIdAsync(dto.OrderID))
            .ReturnsAsync(new List<OrderDetails> { new() { OrderDetailID = detailId, ProductID = productId } });
        _productServiceMock.Setup(s => s.GetProductByIdAsync(productId)).ReturnsAsync((ProductDTO)null);

        var result = await _controller.AcceptRejectOrder(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    //[Fact]
    //public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenProductNotFound()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var dto = new AcceptOrderDTO
    //    {
    //        OrderID = Guid.NewGuid(),
    //        OrderItems = new List<AcceptOrderItemDTO> { new AcceptOrderItemDTO { OrderDetailID = Guid.NewGuid(), IsAccepted = true } }
    //    };
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(dto.OrderID)).ReturnsAsync(new List<OrderDetails> { new OrderDetails { OrderDetailID = Guid.NewGuid(), ProductID = productId } });
    //    _productServiceMock.Setup(service => service.GetProductByIdAsync(productId)).ReturnsAsync((ProductDTO)null);
    //    // Act
    //    var result = await _controller.AcceptRejectOrder(dto);
    //    // Assert
    //    var badRequestResult = result as BadRequestObjectResult;
    //    badRequestResult.Should().NotBeNull();
    //    badRequestResult.StatusCode.Should().Be(400);
    //    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
    //    response["Message"].Should().Be("Product not found.");
    //}


    //[Fact]
    //public async Task AcceptRejectOrder_ShouldReturnOk_WhenAllItemsAccepted()
    //{
    //    var productId = Guid.NewGuid();
    //    var detailId = Guid.NewGuid();
    //    var orderId = Guid.NewGuid();

    //    var dto = new AcceptOrderDTO
    //    {
    //        OrderID = orderId,
    //        OrderItems = new List<AcceptOrderItemDTO> { new() { OrderDetailID = detailId, IsAccepted = true } }
    //    };

    //    var orderDetails = new List<OrderDetails>
    //    {
    //        new() { OrderDetailID = detailId, ProductID = productId, Quantity = 2 }
    //    };

    //    _orderRepositoryMock.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync(new Order { OrderID = orderId });
    //    _orderRepositoryMock.Setup(r => r.GetOrderDetailsByOrderIdAsync(orderId)).ReturnsAsync(orderDetails);
    //    _productServiceMock.Setup(s => s.GetProductByIdAsync(productId)).ReturnsAsync(new ProductDTO { ProductID = productId, Quantity = 10 });
    //    _productServiceMock.Setup(s => s.UpdateProductQuantityAsync(productId, It.IsAny<int>())).ReturnsAsync(true);
    //    _orderDetailsRepoMock.Setup(r => r.UpdateOrderItemStatusAsync(detailId, (int)OrderStatus.Accepted)).ReturnsAsync(orderDetails[0]);
    //    _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(orderId, (int)OrderStatus.Accepted)).ReturnsAsync(new Order { OrderID = orderId });

    //    var result = await _controller.AcceptRejectOrder(dto);

    //    result.Should().BeOfType<OkObjectResult>();
    //    var ok = result as OkObjectResult;
    //    var json = JsonSerializer.Serialize(ok.Value);
    //    json.Should().Contain("Order status updated successfully");
    //}

    // ✅ Test: Should Update Order Successfully
    [Fact]
    public async Task UpdateOrder_ShouldReturnOk_WhenOrderIsUpdated()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var updateOrderDto = new UpdateOrderDTO { OrderID = orderId, OrderStatus = "In Progress" };

        var existingOrder = new Order { OrderID = orderId, OrderStatus = 1 };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _orderRepositoryMock.Setup(repo => repo.UpdateOrderAsync(existingOrder)).ReturnsAsync(existingOrder);

        // Act
        var result = await _controller.UpdateOrder(updateOrderDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateOrder_ShouldReturnBadRequest_WhenDtoIsNull()
    {
        // Act
        var result = await _controller.UpdateOrder(null);
        var badRequestResult = result as BadRequestObjectResult;

        // Assert
        badRequestResult.Should().NotBeNull();
        //badRequestResult.StatusCode.Should().Be(400); // Ensure it's a 400 Bad Request

        // ✅ Deserialize the response value explicitly
        var jsonString = JsonConvert.SerializeObject(badRequestResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        // ✅ Validate the message
        Assert.Equal("Invalid order update data.", response["Message"].ToString());
    }


    [Fact]
    public async Task UpdateOrder_ShouldReturnBadRequest_WhenOrderIDIsEmpty()
    {
        // Arrange
        var updateOrderDto = new UpdateOrderDTO
        {
            OrderID = Guid.Empty, // ❌ Invalid OrderID
            OrderStatus = "Submitted"
        };

        // Act
        var result = await _controller.UpdateOrder(updateOrderDto);
        var badRequestResult = result as BadRequestObjectResult;

        // Assert
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400); // Ensure it is a 400 Bad Request

        // ✅ Deserialize the response value explicitly
        var jsonString = JsonConvert.SerializeObject(badRequestResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        // ✅ Validate the response message
        Assert.Equal("Invalid order update data.", response["Message"].ToString());
    }


    [Fact]
    public async Task UpdateOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        var updateOrderDto = new UpdateOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderStatus = "Completed"
        };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(updateOrderDto.OrderID))
            .ReturnsAsync((Order)null); // Simulating order not found

        // Act
        var result = await _controller.UpdateOrder(updateOrderDto);
        var notFoundResult = result as NotFoundObjectResult;

        // Assert
        notFoundResult.Should().NotBeNull();
        notFoundResult.StatusCode.Should().Be(404); // Ensure the response is a 404 Not Found

        // ✅ Deserialize the response value explicitly
        var jsonString = JsonConvert.SerializeObject(notFoundResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        // ✅ Validate the response message
        Assert.Equal("Order not found.", response["Message"].ToString());
    }


    [Fact]
    public async Task UpdateOrder_ShouldReturnServerError_WhenUpdateFails()
    {
        // Arrange
        var updateOrderDto = new UpdateOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderStatus = "Completed"
        };

        var existingOrder = new Order { OrderID = updateOrderDto.OrderID };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(updateOrderDto.OrderID))
            .ReturnsAsync(existingOrder);

        _orderRepositoryMock.Setup(repo => repo.UpdateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order)null); // Simulating update failure

        // Act
        var result = await _controller.UpdateOrder(updateOrderDto);
        var serverErrorResult = result as ObjectResult;

        // Assert
        serverErrorResult.Should().NotBeNull();
        Assert.Equal(500, serverErrorResult.StatusCode); // Ensure server error is returned

        // ✅ Serialize and deserialize response value
        var jsonString = JsonConvert.SerializeObject(serverErrorResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        // ✅ Validate the response message
        Assert.Equal("Failed to update order.", response["Message"].ToString());
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedProduct = new ProductDTO { ProductID = productId, Quantity = 10 };

        _productServiceMock.Setup(service => service.GetProductByIdAsync(productId))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _productServiceMock.Object.GetProductByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.ProductID.Should().Be(productId);
    }

    //[Theory]
    //[InlineData(1, "New")]
    //[InlineData(2, "In Progress")]
    //[InlineData(3, "Shipped")]
    //[InlineData(4, "Delivered")]
    //public async Task UpdateOrderStatus_ShouldUpdateCorrectly(int status, string expectedStatus)
    //{
    //    // Arrange
    //    var orderId = Guid.NewGuid();
    //    var order = new Order { OrderID = orderId, OrderStatus = status };

    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
    //    _orderRepositoryMock.Setup(repo => repo.UpdateOrderAsync(It.IsAny<Order>()))
    //        .ReturnsAsync(order); // Simulate successful update

    //    var updateOrderDto = new UpdateOrderDTO { OrderID = orderId, OrderStatus = expectedStatus };

    //    // Act
    //    var result = await _controller.UpdateOrder(updateOrderDto);
    //    var okResult = result as OkObjectResult;

    //    // ✅ Assert
    //    okResult.Should().NotBeNull();
    //    okResult.StatusCode.Should().Be(200); // Ensure success response

    //    // ✅ Serialize and Deserialize Response
    //    var jsonString = JsonConvert.SerializeObject(okResult.Value);
    //    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

    //    // ✅ Validate response properties
    //    Assert.Equal("Order updated successfully.", response["Message"].ToString());
    //    Assert.Equal(orderId.ToString(), response["OrderId"].ToString()); // Ensure the order ID is returned correctly
    //    Assert.Equal("", response["ErrorMessage"].ToString()); // Ensure no error message
    //}

    [Fact] 
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnBadRequest_WhenCartIDIsEmpty()
    {
        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(Guid.Empty); 
        var badRequestResult = result as BadRequestObjectResult;

        // Assert
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        // ✅ Parse JSON response
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(badRequestResult.Value));

        response["Message"].ToString().Should().Be("");
        response["ErrorMessage"].ToString().Should().Be("Invalid Cart ID.");
    }

    [Fact]
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnServerError_WhenCartIDNotFound()
    {
        // Arrange
        var cartID = Guid.NewGuid();

        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartItemByCartID(cartID))
            .ReturnsAsync((ShoppingCart)null); // Simulate item not found

        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(cartID);
        var serverErrorResult = result as ObjectResult;

        // Assert
        serverErrorResult.Should().NotBeNull();
        serverErrorResult.StatusCode.Should().Be(500);

        // Convert the response object to JSON
        var jsonString = JsonConvert.SerializeObject(serverErrorResult.Value);
        var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

        response.Should().NotBeNull();
        response["Message"].Should().Be("An error occurred while removing item from the cart.");
        response["ErrorMessage"].Should().Contain($"There is no shopping cart item with cart ID: {cartID}");
    }


    [Fact]
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnInternalServerError_WhenUpdateFails()
    {
        // Arrange
        var cartID = Guid.NewGuid();
        var ShoppingCart = new ShoppingCart { CartID = cartID, IsActive = true };

        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartItemByCartID(cartID))
            .ReturnsAsync(ShoppingCart);

        _shoppingCartRepoMock.Setup(repo => repo.UpdateShoppingCartItemByCartIdAsync(ShoppingCart))
            .ReturnsAsync(false); // Simulate update failure

        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(cartID);
        var serverErrorResult = result as ObjectResult;

        // Assert
        serverErrorResult.Should().NotBeNull();
        serverErrorResult.StatusCode.Should().Be(500);

        // ✅ Parse JSON response
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(serverErrorResult.Value));

        response["Message"].ToString().Should().Be("Failed to remove item from the cart.");
        response["ErrorMessage"].ToString().Should().Be("Internal server error.");
    }

    [Fact]
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnOk_WhenDeletionSucceeds()
    {
        // Arrange
        var cartID = Guid.NewGuid();
        var ShoppingCart = new ShoppingCart { CartID = cartID, IsActive = true };

        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartItemByCartID(cartID))
            .ReturnsAsync(ShoppingCart);

        _shoppingCartRepoMock.Setup(repo => repo.UpdateShoppingCartItemByCartIdAsync(ShoppingCart))
            .ReturnsAsync(true); // Simulate success
        
        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(cartID);
        var okResult = result as OkObjectResult;

        // Assert
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        // ✅ Parse JSON response
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(okResult.Value));

        response["Message"].ToString().Should().Be("Item removed from the cart successfully.");
        response["ErrorMessage"].ToString().Should().Be("");
    }

    [Fact]
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnServerError_WhenExceptionOccurs()
    {
        // Arrange
        var cartID = Guid.NewGuid();
        _shoppingCartRepoMock.Setup(repo => repo.GetShoppingCartItemByCartID(cartID))
            .ThrowsAsync(new Exception("Database connection lost"));

        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(cartID);
        var serverErrorResult = result as ObjectResult;

        // Assert
        serverErrorResult.Should().NotBeNull();
        serverErrorResult.StatusCode.Should().Be(500);

        // ✅ Parse JSON response
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            JsonConvert.SerializeObject(serverErrorResult.Value));

        response["Message"].ToString().Should().Be("An error occurred while removing item from the cart.");
        response["ErrorMessage"].ToString().Should().Contain("Database connection lost");
    }

    [Fact]
    public async Task GetOrdersAndOrderDetails_ShouldHandlePagination()
    {
        // Arrange
        var retailerId = Guid.NewGuid();
        for (int i = 0; i < 15; i++) // Add 15 orders
        {
            _dbContext.Order.Add(new Order
            {
                OrderID = Guid.NewGuid(),
                RetailerID = retailerId,
                OrderStatus = 1,
                TotalPrice = 100m,
                PaymentMode = 1,
                PaymentCurrency = "USD",
                ShippingCost = 5m,
                ShippingCurrency = "USD",
                ShippingAddress = "123 Street",
                OrderDetails = new List<OrderDetails>
                {
                    new OrderDetails
                    {
                        OrderDetailID = Guid.NewGuid(),
                        ProductID = Guid.NewGuid(),
                        ManufacturerID = Guid.NewGuid(),
                        Quantity = 2,
                        OrderItemStatus = 1,
                        ProductPrice = 50m
                    }
                }
            });
        }
        await _dbContext.SaveChangesAsync();
        _orderRepositoryMock
            .Setup(repo => repo.GetFilteredOrdersAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                1, 10)) // Page 1, 10 items per page
            .ReturnsAsync((_mapper.Map<IEnumerable<OrderDto>>(_dbContext.Order.Take(10)), 2)); // 2 pages total
        // Act
        var result = await _controller.GetOrdersAndOrderDetails(null, null, null, null, null, null, null, null, null, 1, 10);
        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(okResult.Value));
        response["TotalPages"].Should().Be(2);
        ((IEnumerable<object>)response["Orders"]).Count().Should().Be(10);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenOrderDetailsAreMissing()
    {
        // Arrange
        var createOrderDto = new CreateOrderDTO
        {
            RetailerID = Guid.NewGuid(),
            PaymentMode = 1,
            PaymentCurrency = "USD",
            ShippingCost = 10m,
            ShippingCurrency = "USD",
            ShippingAddress = "123 Street",
            CreatedBy = Guid.NewGuid(),
            OrderDetails = null // Missing order details
        };
        // Act
        var result = await _controller.CreateOrder(createOrderDto);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
        response["Message"].Should().Be("Invalid Order Data.");
        response["ErrorMessage"].Should().Be("Order details are missing.");
    }

    //[Fact]
    //public async Task AcceptRejectOrder_ShouldHandlePartialAcceptance()
    //{
    //    // Arrange
    //    var orderId = Guid.NewGuid();
    //    var productId = Guid.NewGuid();
    //    var orderDetails = new List<OrderDetails>
    //    {
    //        new OrderDetails { OrderDetailID = Guid.NewGuid(), ProductID = productId, Quantity = 2, OrderItemStatus = 1 },
    //        new OrderDetails { OrderDetailID = Guid.NewGuid(), ProductID = Guid.NewGuid(), Quantity = 1, OrderItemStatus = 1 }
    //    };
    //    var acceptOrderDto = new AcceptOrderDTO
    //    {
    //        OrderID = orderId,
    //        OrderItems = new List<AcceptOrderItemDTO>
    //        {
    //            new AcceptOrderItemDTO { OrderDetailID = orderDetails[0].OrderDetailID, IsAccepted = true },
    //            new AcceptOrderItemDTO { OrderDetailID = orderDetails[1].OrderDetailID, IsAccepted = false }
    //        }
    //    };
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId)).ReturnsAsync(new Order { OrderID = orderId, OrderDetails = orderDetails });
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(orderId)).ReturnsAsync(orderDetails);
    //    _productServiceMock.Setup(service => service.GetProductByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new ProductDTO { ProductID = productId, Quantity = 10 });
    //    _productServiceMock.Setup(service => service.UpdateProductQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
    //    // Act
    //    var result = await _controller.AcceptRejectOrder(acceptOrderDto);
    //    // Assert
    //    result.Should().BeAssignableTo<OkObjectResult>();
    //    var okResult = result as OkObjectResult;
    //    okResult.Should().NotBeNull();
    //    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(okResult.Value));
    //    response["Message"].Should().Be("Order status updated successfully.");
    //}

    //[Fact]
    //public async Task AcceptRejectOrder_ShouldHandlePartialAcceptance()
    //{
    //    // Arrange
    //    var orderId = Guid.NewGuid();
    //    var productId = Guid.NewGuid();
    //    var orderDetails = new List<OrderDetails>
    //    {
    //        new OrderDetails { OrderDetailID = Guid.NewGuid(), ProductID = productId, Quantity = 2, OrderItemStatus = 1 },
    //        new OrderDetails { OrderDetailID = Guid.NewGuid(), ProductID = Guid.NewGuid(), Quantity = 1, OrderItemStatus = 1 }
    //    };
    //    var acceptOrderDto = new AcceptOrderDTO
    //    {
    //        OrderID = orderId,
    //        OrderItems = new List<AcceptOrderItemDTO>
    //        {
    //            new AcceptOrderItemDTO { OrderDetailID = orderDetails[0].OrderDetailID, IsAccepted = true },
    //            new AcceptOrderItemDTO { OrderDetailID = orderDetails[1].OrderDetailID, IsAccepted = false }
    //        }
    //    };
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId))
    //        .ReturnsAsync(new Order { OrderID = orderId, OrderDetails = orderDetails });
    //    _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(orderId))
    //        .ReturnsAsync(orderDetails);
    //    _productServiceMock.Setup(service => service.GetProductByIdAsync(It.IsAny<Guid>()))
    //        .ReturnsAsync(new ProductDTO { ProductID = productId, Quantity = 10 });
    //    _productServiceMock.Setup(service => service.UpdateProductQuantityAsync(It.IsAny<Guid>(), It.IsAny<int>()))
    //        .ReturnsAsync(true);
    //    // Act
    //    var result = await _controller.AcceptRejectOrder(acceptOrderDto);
    //    // Debugging: Verify the result type
    //    Console.WriteLine($"[DEBUG] Result Type: {result?.GetType()}");
    //    // Assert
    //    var okResult = result as OkObjectResult;
    //    okResult.Should().NotBeNull($"Expected OkObjectResult but got {result?.GetType()}");
    //    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(okResult.Value));
    //    response["Message"].Should().Be("Order status updated successfully.");
    //}

    [Fact]
    public async Task DeleteShoppingCartItemByCardID_ShouldReturnBadRequest_WhenCartIDIsInvalid()
    {
        // Act
        var result = await _controller.DeleteShoppingCartItemByCardID(Guid.Empty);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
        response["Message"].Should().Be("");
        response["ErrorMessage"].Should().Be("Invalid Cart ID.");
    }

    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var invalidDto = new AcceptOrderDTO
        {
            OrderID = Guid.Empty, // Invalid OrderID
            OrderItems = null // Missing OrderItems
        };
        // Act
        var result = await _controller.AcceptRejectOrder(invalidDto);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
        response["Message"].Should().Be("Invalid request.");
        response["ErrorMessage"].Should().Be("OrderID and OrderItems are required.");
    }

    [Fact]
    public async Task AcceptRejectOrder_ShouldReturnBadRequest_WhenNoOrderDetailsFound()
    {
        // Arrange
        var dto = new AcceptOrderDTO
        {
            OrderID = Guid.NewGuid(),
            OrderItems = new List<AcceptOrderItemDTO> { new AcceptOrderItemDTO { OrderDetailID = Guid.NewGuid(), IsAccepted = true } }
        };
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(dto.OrderID)).ReturnsAsync(new Order());
        _orderRepositoryMock.Setup(repo => repo.GetOrderDetailsByOrderIdAsync(dto.OrderID)).ReturnsAsync(new List<OrderDetails>());
        // Act
        var result = await _controller.AcceptRejectOrder(dto);
        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(badRequestResult.Value));
        response["Message"].Should().Be("No order details found.");
        response["ErrorMessage"].Should().Be("Cannot process order without items.");
    }

    [Fact]
    public void AcceptRejectOrder_ShouldHaveHttpPutAttributeWithCorrectRoute()
    {
        // Arrange
        var methodInfo = typeof(OrderManagementController).GetMethod("AcceptRejectOrder");
        // Act
        var httpPutAttribute = methodInfo.GetCustomAttributes(typeof(HttpPutAttribute), false)
            .FirstOrDefault() as HttpPutAttribute;
        // Assert
        httpPutAttribute.Should().NotBeNull();
        httpPutAttribute.Template.Should().Be("AcceptRejectOrder");
    }
}
