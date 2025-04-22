using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Models.DTO;
using OrderManagement.Repositories;
using AutoMapper;
using OrderManagement.Common;
using OrderManagement.ExternalServices;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using OrderManagement.Logger.interfaces;
using System.Security.Claims;


namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
   
    public class OrderManagementController : ControllerBase
    {

        private readonly AppDbContext dbContext;
        private readonly IOrderRepository orderRepository;
        private readonly IOrderDetailsRepository orderDetailsRepository;
        private readonly IShoppingCartRepository shoppingCartRepository;
        private readonly IUserRepository userRepository;
        private readonly IProductServiceClient productServiceClient;
        private readonly IProductRepository productRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IAppLogger<OrderManagementController> _logger;
        private readonly IKafkaProducer _kafkaProducer;
        public OrderManagementController(AppDbContext appDbContext, IOrderRepository orderRepo, IOrderDetailsRepository orderDetailsRepo, IShoppingCartRepository shoppingCartRepo, IMapper mapper, IProductServiceClient productServiceClient, IProductRepository productRepo, IUserRepository userRepo
              , IConfiguration configuration
            , IAppLogger<OrderManagementController> logger, IKafkaProducer kafkaProducer)
        {
            this.dbContext = appDbContext;
            this.orderRepository = orderRepo;
            this.orderDetailsRepository = orderDetailsRepo;
            this.shoppingCartRepository = shoppingCartRepo;
            this.productServiceClient = productServiceClient;
            this.productRepository = productRepo;
            this.userRepository = userRepo;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
        }

        private (Guid? UserId, int? Role) GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            Guid? userId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
            int? role = int.TryParse(roleClaim, out var parsedRole) ? parsedRole : null;

            return (userId, role);
        }


        [HttpGet("GetOrdersAndOrderDetails")]
        public async Task<IActionResult> GetOrdersAndOrderDetails(
        [FromQuery] Guid? orderId,
        [FromQuery] Guid? retailerId,
        [FromQuery] string? retailerName,
        [FromQuery] Guid? manufacturerId,
        [FromQuery] string? manufacturerName,
        [FromQuery] string? productName,
        [FromQuery] Guid? deliveryPersonnelId,
        [FromQuery] int? orderStatus,
        [FromQuery] int? orderItemStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {

            _logger.LogInformation("Entering GetOrdersAndOrderDetails...");

            var (currentUserId, role) = GetCurrentUser();
            if (currentUserId == null || role == null)
                return Unauthorized("Invalid or missing user credentials.");

            // Apply filters based on role
            switch (role)
            {
                case 1: // Admin – No filters
                    break;
                case 2: // Retailer
                    retailerId = currentUserId;
                    break;
                case 3: // Manufacturer
                    manufacturerId = currentUserId;
                    break;
                case 4: // DeliveryPersonnel
                    deliveryPersonnelId = currentUserId;
                    break;
                default:
                    return Forbid("Unauthorized role.");
            }

            // Fetch orders
            var (orders, totalPages) = await orderRepository.GetFilteredOrdersAsync(
                orderId, retailerId, deliveryPersonnelId, orderStatus, manufacturerId, orderItemStatus, retailerName, manufacturerName, productName, pageNumber, pageSize);

            _logger.LogInformation("Fetched {OrderCount} orders with {TotalPages} total pages.", orders?.Count() ?? 0, totalPages);

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);

            if (orderDtos == null)
            {
                _logger.LogError("Order is NULL! Returning empty result");

                return Ok(new
                {
                    Message = "Orders retrieved successfully.",
                    ErrorMessage = string.Empty,
                    Orders = new List<object>(), // ✅ Return an empty list instead of null
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }

            return Ok(new
            {
                Message = "Orders retrieved successfully.",
                ErrorMessage = string.Empty,
                Orders = orderDtos.Select(order => new
                {
                    OrderID = order.OrderID,
                    RetailerID = order.RetailerID,
                    RetailerName = order.RetailerName,
                    DeliveryPersonnelID = order.DeliveryPersonnelID,
                    OrderStatus = order.OrderStatusValue,
                    TotalPrice = order.TotalPrice,
                    PaymentMode = order.PaymentModeValue,
                    PaymentCurrency = order.PaymentCurrency,
                    ShippingCost = order.ShippingCost,
                    ShippingCurrency = order.ShippingCurrency,
                    ShippingAddress = order.ShippingAddress,
                    OrderDetails = order.OrderDetails.Select(detail => new
                    {
                        OrderDetailID = detail.OrderDetailID,
                        ProductID = detail.ProductID,
                        ProductName = detail.ProductName,
                        ManufacturerID = detail.ManufacturerID,
                        ManufacturerName = detail.ManufacturerName,
                        Quantity = detail.Quantity,
                        OrderItemStatus = detail.OrderItemStatusValue,
                        ProductPrice = detail.ProductPrice
                    }).ToList()
                }).ToList(),
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO orderRequestDto)
        {
            _logger.LogInformation("Initiating order creation process.");

            if (orderRequestDto == null || orderRequestDto.OrderDetails == null || orderRequestDto.OrderDetails.Count == 0)
            {
                _logger.LogError("Received an invalid order request: Missing order details or null payload.");
                return BadRequest(new { Message = "Invalid Order Data.", ErrorMessage = "Order details are missing." });
            }
            _logger.LogInformation("Processing order request for Retailer ID: {RetailerID}", orderRequestDto.RetailerID);
            var retailer = await userRepository.GetUserInfoByRetailerIdAsync(new List<Guid> { orderRequestDto.RetailerID });

            if (retailer == null || !retailer.ContainsKey(orderRequestDto.RetailerID))
            {
                _logger.LogWarning("Order request failed: Retailer ID {RetailerID} not found in the system.", orderRequestDto.RetailerID);
                return BadRequest(new { Message = "Invalid Retailer ID.", ErrorMessage = "The provided Retailer ID does not exist." });
            }

            _logger.LogInformation("Retailer ID {RetailerID} found: {RetailerName}", orderRequestDto.RetailerID, retailer[orderRequestDto.RetailerID].UserName);
            try
            {
                var totalPrice = CalculateTotalCost(orderRequestDto.OrderDetails);
                _logger.LogInformation("Total price calculation completed: ${TotalPrice}", totalPrice);

                foreach (var detail in orderRequestDto.OrderDetails ?? new List<CreateOrderDetailsDTO>())
                {
                    _logger.LogDebug("Order contains Product ID: {ProductID} with Quantity: {Quantity}", detail.ProductID, detail.Quantity);
                    Console.WriteLine($"[TRACE] Product ID: {detail.ProductID}, Quantity: {detail.Quantity}");
                }

                _logger.LogDebug("Transforming order request DTO into the Order entity.");
                var orderModel = _mapper.Map<CreateOrderDTO, Order>(orderRequestDto);
                orderModel.TotalPrice = totalPrice;

                orderModel = await orderRepository.CreateOrderAsync(orderModel);
                _logger.LogInformation("Saving new order to the database.");

                if (orderModel == null)
                {
                    _logger.LogError("Database operation failed: Order could not be created.");
                    return StatusCode(500, new { Message = "Order creation failed.", ErrorMessage = "Repository returned null." });
                }

                _logger.LogInformation("Order successfully created with Order ID: {OrderID}", orderModel.OrderID);
                var cartIDs = orderRequestDto.OrderDetails.Select(detail => detail.CartID).ToList();

                _logger.LogInformation("Starting cleanup: Deactivating {CartCount} shopping cart items.", cartIDs.Count);
                foreach (var cartID in cartIDs)
                {
                    await DeactivateShoppingCartItemById(cartID);
                }

                //Send notification to Kafka
                _logger.LogInformation("Send notification to Kafka");
                try
                {
                    var notification = new Notification
                    {
                        NotificationID = Guid.NewGuid(),
                        UserID = orderModel.RetailerID,
                        Subject = "Tradeport Notification Service - Order Created",
                        Message = $"Your order {orderModel.OrderID} has been placed successfully.",
                        FromEmail = "noreply@tradeport.com",
                        RecipientEmail = retailer[orderRequestDto.RetailerID].LoginID, // Assuming you have this
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = orderModel.CreatedBy,
                        //FailureReason = null,
                        //SentTime = DateTime.UtcNow,
                        //EmailSend = true
                    };

                    await _kafkaProducer.SendNotificationAsync("tradeport-notify", notification);
                    _logger.LogInformation("Send notification to Kafka successful");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Kafka notification failed: {Message}", ex.Message);
                    _logger.LogError(ex, "Kafka notification failed.");
                }

                _logger.LogInformation("Order creation process completed successfully.");
                var response = new
                {
                    Message = "Order created successfully.",
                    ErrorMessage = ""
                };



                return Ok(new { orderID = orderModel.OrderID, response });
            }
            catch (Exception ex)
            {
                var response = new
                {
                    Message = "Order creation failed.",
                    ErrorMessage = ex.Message + Environment.NewLine + (ex.InnerException?.Message ?? "")
                };

                return StatusCode(500, response);
            }
        }


        [HttpPost("CreateShoppingCart")]
        public async Task<IActionResult> CreateShoppingCartItemAsync([FromBody] CreateShoppingCartDTO shoppingCartDto)
        {
            _logger.LogInformation("Entering CreateShoppingCartItemAsync method.");

            if (shoppingCartDto == null)
            {
                _logger.LogError("Received null shopping cart DTO because of invalid cart item id or cart item id not longer exist in database.");
                return BadRequest(new { Message = "Invalid Cart Item.", ErrorMessage = "Cart items are missing." });
            }

            _logger.LogInformation("Fetching retailer information for RetailerID: {RetailerID}", shoppingCartDto.RetailerID);

            var retailer = await userRepository.GetUserInfoByRetailerIdAsync(new List<Guid> { shoppingCartDto.RetailerID });
            if (retailer == null || !retailer.ContainsKey(shoppingCartDto.RetailerID))
            {
                _logger.LogWarning("Invalid Retailer ID: {RetailerID}", shoppingCartDto.RetailerID);
                return BadRequest(new { Message = "Invalid Retailer ID.", ErrorMessage = "The provided Retailer ID does not exist." });
            }

            try
            {
                _logger.LogInformation("Mapping DTO to ShoppingCart model.");
                var orderModel = _mapper.Map<CreateShoppingCartDTO, ShoppingCart>(shoppingCartDto);
                orderModel.Status = (int)OrderStatus.Save;
                orderModel.OrderQuantity = orderModel.OrderQuantity;
                orderModel.ProductID = orderModel.ProductID;
                orderModel.ProductPrice = orderModel.ProductPrice;
                orderModel.ManufacturerID = orderModel.ManufacturerID;
                orderModel.CreatedBy = orderModel.RetailerID;
                orderModel.CreatedOn = DateTime.UtcNow;
                orderModel.IsActive = true;

                // Use Repository to create Product
                _logger.LogInformation("Creating shopping cart item in the repository.");
                orderModel = await shoppingCartRepository.CreateShoppingCartItemsAsync(orderModel);

                _logger.LogInformation("Shopping cart item created successfully with CartID: {CartID}", orderModel.CartID);

                var response = new
                {
                    Message = "Item added to the cart successfully.",
                    ErrorMessage = ""
                };
                return Ok(new { cartID = orderModel.CartID, response });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating shopping cart item.", ex.Message);
                var response = new
                {
                    Message = "Item creation failed.",
                    ShoppingCartID = string.Empty,
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);
            }
        }


        [HttpGet("GetShoppingCart/{retailerID}")]
        public async Task<IActionResult> GetShoppingCartByRetailerId(Guid retailerID)
        {
            _logger.LogInformation("Entering GetShoppingCartByRetailerId method for RetailerID: {RetailerID}", retailerID);
            if (retailerID == Guid.Empty)
            {
                _logger.LogWarning("Invalid Retailer ID: {RetailerID}", retailerID);
                return BadRequest(new { Message = "Invalid Retailer ID.", ErrorMessage = "The provided Retailer ID does not exist." });
            }
            try
            {
                Console.WriteLine($"[TRACE] Entering GetShoppingCartByRetailerId for RetailerID: {retailerID}");
                _logger.LogInformation("Fetching shopping cart items for RetailerID: {RetailerID}", retailerID);
                var shoppingCart = await shoppingCartRepository.GetShoppingCartByRetailerIdAsync(retailerID, (int)OrderStatus.Save);
                if (shoppingCart == null || shoppingCart.Count == 0)
                {
                    _logger.LogError($"No shopping cart items found for RetailerID:{retailerID}");
                    return new ObjectResult(new
                    {
                        Message = $"No cart items found for the provided Retailer. {retailerID}",
                        ErrorMessage = ""
                    })
                    {
                        StatusCode = 404
                    };
                }

                 _logger.LogInformation("Shopping cart items found. Number of items: {ItemCount}", shoppingCart.Count);
                var shoppingCartDTO = _mapper.Map<List<ShoppingCartDTO>>(shoppingCart);
                var retailerIDs = shoppingCart.Select(sc => sc.RetailerID).Distinct().ToList();
                _logger.LogInformation("Retrieved retailer IDs: {RetailerIDs}", string.Join(", ", retailerIDs));
                var retailers = await GetUserInfoByRetailerIdAsync(retailerIDs);

                foreach (var cart in shoppingCartDTO)
                {
                    _logger.LogInformation("Processing cart item for Retailer ID: {RetailerID}", cart.RetailerID);
                    if (retailers.ContainsKey(cart.RetailerID))
                    {
                        var retailer = retailers[cart.RetailerID];
                        cart.RetailerName = retailer.UserName;
                        cart.PhoneNumber = retailer.PhoneNo;
                        cart.Address = retailer.Address;
                        Console.WriteLine($"[TRACE] Retrieved retailer details: {cart.RetailerName}");
                        _logger.LogInformation("Retrieved retailer details: {RetailerName}", cart.RetailerName);
                    }
                    else
                    {
                        _logger.LogWarning("No retailer details found for Retailer ID: {RetailerID}", cart.RetailerID);
                        Console.WriteLine($"[WARNING] No retailer details found for Retailer ID: {cart.RetailerID}");
                    }

                    var product = await GetProductByProductID(cart.ProductID);
                    if (product != null)
                    {
                        cart.ProductName = product.ProductName;
                        cart.IsOutOfStock = product.Quantity < cart.OrderQuantity;
                        cart.ManufacturerID = product.ManufacturerID;
                    }
                    else
                    {
                        cart.ProductName = string.Empty;
                        cart.IsOutOfStock = false;
                        cart.ManufacturerID = Guid.Empty;
                    }

                    cart.ProductImagePath = string.Empty;
                    cart.TotalPrice = cart.OrderQuantity * cart.ProductPrice;
                }

                _logger.LogInformation("Returning shopping cart details. Total items: {ItemCount}", shoppingCartDTO.Count);
                return Ok(new
                {
                    Message = "Cart Items fetched successfully.",
                    ErrorMessage = string.Empty,
                    NumberOfOrderItems = shoppingCartDTO.Count,
                    CartDetails = shoppingCartDTO
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching cart items for RetailerID: {RetailerID}", retailerID);
                return StatusCode(500, new
                {
                    Message = $"An error occurred while retrieving the cart items for RetailerID: {retailerID}",
                    ErrorMessage = ex.Message
                });
            }
        }


        [HttpPut("DeleteCartItemByID")]
        public async Task<IActionResult> DeleteShoppingCartItemByCardID(Guid cartID)
        {
            _logger.LogInformation("Entering DeleteShoppingCartItemByCardID method for CartID: {CartID}", cartID);

            if (cartID == Guid.Empty)
            {
                _logger.LogError($"Invalid Cart ID: {cartID}");
                return BadRequest(new { Message = "", ErrorMessage = "Invalid Cart ID." });
            }

            try
            {
                _logger.LogInformation("Deactivating shopping cart item with CartID: {CartID}", cartID);
                bool result = await DeactivateShoppingCartItemById(cartID);

                if (!result)
                {
                    _logger.LogError($"Failed to remove item from the cart with CartID: {cartID}");
                    return StatusCode(500, new { Message = "Failed to remove item from the cart.", ErrorMessage = "Internal server error." });
                }
                _logger.LogInformation("Item removed from the cart successfully with CartID: {CartID}", cartID);
                return Ok(new
                {
                    Message = "Item removed from the cart successfully.",
                    ErrorMessage = string.Empty
                });

                //return Ok(new { Message = "Order updated successfully.", OrderID = updatedOrder.OrderID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing item from the cart with CartID: {CartID}", cartID);
                var response = new
                {
                    Message = "An error occurred while removing item from the cart.",
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);

            }
        }

        [HttpPut("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderDTO updateOrderDto)
        {
            _logger.LogInformation("Entering UpdateOrder method..");

            if (updateOrderDto == null || updateOrderDto.OrderID == Guid.Empty || string.IsNullOrEmpty(updateOrderDto.OrderStatus))
            {
                _logger.LogError("Invalid order update data as Order ID or status is missing.");
                return BadRequest(new { Message = "Invalid order update data.", ErrorMessage = "Order ID or status is missing." });
            }

            _logger.LogInformation("Entering UpdateOrder method for OrderID: {OrderID}", updateOrderDto.OrderID);

            try
            {
                _logger.LogInformation("Fetching existing order for OrderID: {OrderID}", updateOrderDto.OrderID);
                var existingOrder = await orderRepository.GetOrderByIdAsync(updateOrderDto.OrderID);

                if (existingOrder == null)
                {
                    _logger.LogWarning("Order not found for OrderID: {OrderID}", updateOrderDto.OrderID);
                    return NotFound(new { Message = "Order not found.", ErrorMessage = "Invalid Order ID." });
                }
                _logger.LogInformation("Updating order with OrderID: {OrderID}{OrderStatus}", updateOrderDto.OrderID, updateOrderDto.OrderStatus);
                // Use AutoMapper to update the existing order
                _mapper.Map(updateOrderDto, existingOrder);

                if (EnumHelper.GetEnumValueFromDisplayName<OrderStatus>(updateOrderDto.OrderStatus) == (int)OrderStatus.Shipped)
                {
                    _logger.LogInformation("Setting order with DeliveryPersonnelID....");
                    existingOrder.DeliveryPersonnelID = GetDeliveryPersonalID(existingOrder.OrderStatus);
                }

                var updatedOrder = await orderRepository.UpdateOrderAsync(existingOrder);

                if (updatedOrder == null)
                {
                    _logger.LogError($"Failed to update order with OrderID :{existingOrder.OrderID}");
                    return StatusCode(500, new { Message = "Failed to update order.", OrderId = existingOrder.OrderID, ErrorMessage = "Internal server error." });
                }
                _logger.LogInformation("Order updated successfully with OrderID: {OrderID}", updatedOrder.OrderID);
                return Ok(new
                {
                    Message = "Order updated successfully.",
                    OrderId = updatedOrder.OrderID,
                    ErrorMessage = string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the order with OrderID: {OrderID}", updateOrderDto.OrderID);
                var response = new
                {
                    Message = "An error occurred while updating the order.",
                    OrderId = updateOrderDto.OrderID,
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);
            }
        }

        [HttpPut("AcceptRejectOrder")]
        public async Task<IActionResult> AcceptRejectOrder([FromBody] AcceptOrderDTO acceptOrderDto)
        {
            _logger.LogInformation("Entering AcceptRejectOrder method..");

            if (acceptOrderDto == null || acceptOrderDto.OrderID == Guid.Empty || acceptOrderDto.OrderItems == null || !acceptOrderDto.OrderItems.Any())
            {
                _logger.LogError("Invalid request. OrderID and OrderItems are required.");
                return BadRequest(new { Message = "Invalid request.", ErrorMessage = "OrderID and OrderItems are required." });
            }
            _logger.LogInformation("Entering AcceptRejectOrder method for OrderID: {OrderID}", acceptOrderDto.OrderID);

            try
            {
                _logger.LogInformation("Fetching existing order for OrderID: {OrderID}", acceptOrderDto.OrderID);
                var existingOrder = await orderRepository.GetOrderByIdAsync(acceptOrderDto.OrderID);
                if (existingOrder == null)
                {
                    _logger.LogWarning("Order not found for OrderID: {OrderID}", acceptOrderDto.OrderID);
                    return NotFound(new { Message = "Order not found.", ErrorMessage = "Invalid Order ID." });
                }
                _logger.LogInformation("Fetching order details for OrderID: {OrderID}", acceptOrderDto.OrderID);
                var orderDetails = await orderRepository.GetOrderDetailsByOrderIdAsync(acceptOrderDto.OrderID);
                if (orderDetails == null || !orderDetails.Any())
                {
                    _logger.LogError($"[ERROR] No order details found for OrderID: {acceptOrderDto.OrderID}");
                    return BadRequest(new { Message = "No order details found.", ErrorMessage = "Cannot process order without items." });
                }

                // Retrieve previously accepted items from the database
                var previouslyAcceptedItems = orderDetails
                    .Where(od => od.OrderItemStatus == (int)OrderStatus.Accepted)
                    .ToList();

                bool isAnyRejected = false;
                bool isAllAccepted = true;
                List<OrderDetails> newlyAcceptedItems = new List<OrderDetails>(); // Track newly accepted items
                List<OrderDetails> newlyRejectedItems = new List<OrderDetails>(); // Track newly rejected items

                foreach (var itemDto in acceptOrderDto.OrderItems)
                {
                    var orderItem = orderDetails.FirstOrDefault(od => od.OrderDetailID == itemDto.OrderDetailID);
                    if (orderItem == null)
                    {
                        _logger.LogError($"Order item not found for OrderDetailID: {itemDto.OrderDetailID}");
                        return BadRequest(new { Message = "Order item not found.", ErrorMessage = $"OrderDetailID {itemDto.OrderDetailID} does not exist." });
                    }

                    var product = await GetProductByProductID(orderItem.ProductID);
                    if (product == null)
                    {
                        _logger.LogError($"[ERROR] Product not found for ProductID: {orderItem.ProductID}");
                        return BadRequest(new { Message = "Product not found.", ErrorMessage = $"ProductID {orderItem.ProductID} does not exist." });
                    }

                    if (itemDto.IsAccepted)
                    {
                        // Accepting the order item
                        orderItem.OrderItemStatus = (int)OrderStatus.Accepted;
                        newlyAcceptedItems.Add(orderItem);

                        // Reduce quantity from Product Table
                        if (product.Quantity < orderItem.Quantity)
                        {
                            _logger.LogError($"[ERROR] Insufficient stock for ProductID: {product.ProductID}");
                            return BadRequest(new { Message = "Insufficient stock.", ErrorMessage = $"Product {product.ProductID} has insufficient quantity." });
                        }
                        int updatedQuantity = product.Quantity.GetValueOrDefault() - orderItem.Quantity;
                        bool productUpdated = await UpdateProductQuantityAsync(orderItem.ProductID, updatedQuantity);

                        if (!productUpdated)
                        {
                            _logger.LogError($"[ERROR] Failed to update product quantity for ProductID: {orderItem.ProductID}");
                            return StatusCode(500, new { Message = "Failed to update product quantity.", ErrorMessage = $"Could not update ProductID {product.ProductID}." });
                        }
                    }
                    else
                    {
                        // Rejecting the order item
                        orderItem.OrderItemStatus = (int)OrderStatus.Rejected;
                        newlyRejectedItems.Add(orderItem);
                        isAnyRejected = true;
                        isAllAccepted = false;
                    }

                    // ✅ Use `UpdateOrderItemStatusAsync` instead of `UpdateAsync`
                    await orderDetailsRepository.UpdateOrderItemStatusAsync(orderItem.OrderDetailID, orderItem.OrderItemStatus);
                }

                // ✅ Update Order Status using `UpdateOrderStatusAsync`
                if (isAnyRejected)
                {
                    _logger.LogInformation("Update Order Status using `UpdateOrderStatusAsync by Order ID" + existingOrder.OrderID);
                    existingOrder = await orderRepository.UpdateOrderStatusAsync(existingOrder.OrderID, (int)OrderStatus.Rejected);

                    // ✅ Restore quantity for ALL previously accepted items
                    var allAcceptedItems = previouslyAcceptedItems.Concat(newlyAcceptedItems).ToList();
                    foreach (var item in allAcceptedItems)
                    {
                        var product = await GetProductByProductID(item.ProductID);
                        if (product != null)
                        {
                            int restoredQuantity = product.Quantity.GetValueOrDefault() + item.Quantity;
                            await UpdateProductQuantityAsync(item.ProductID, restoredQuantity);
                        }
                    }
                }
                else if (isAllAccepted)
                {
                    _logger.LogInformation("if isAllAccepted, update Order Status using `UpdateOrderStatusAsync by Order ID" + existingOrder.OrderID);
                    existingOrder = await orderRepository.UpdateOrderStatusAsync(existingOrder.OrderID, (int)OrderStatus.Accepted);
                }
                _logger.LogInformation($"[TRACE] Order status updated successfully for OrderID: {existingOrder.OrderID}");

                // Send notification to Kafka
                _logger.LogInformation("Send notification to Kafka");
                try
                {
                    var retailer = await userRepository.GetUserInfoByRetailerIdAsync(new List<Guid> { existingOrder.RetailerID });

                    if (retailer == null)
                    {
                        _logger.LogInformation("Retailer ID {RetailerID} not found in the system.", existingOrder.RetailerID);
                        _logger.LogInformation("Retailer ID and email is not found. Kafka notification failed in AcceptRejectOrder");
                    }
                    else
                    {
                        var notification = new Notification
                        {
                            NotificationID = Guid.NewGuid(),
                            UserID = existingOrder.RetailerID,
                            Subject = "Tradeport Notification Service - Order Status Updated",
                            Message = $"Your order ({existingOrder.OrderID}) status has been updated to {(isAllAccepted ? "Accepted" : "Rejected")}.",
                            FromEmail = "noreply@tradeport.com",
                            RecipientEmail = retailer[existingOrder.RetailerID].LoginID,
                            //FailureReason = null,
                            //SentTime = DateTime.UtcNow,
                            //EmailSend = true
                            CreatedOn = DateTime.UtcNow,
                            CreatedBy = existingOrder.UpdatedBy
                        };

                        await _kafkaProducer.SendNotificationAsync("tradeport-notify", notification);
                        _logger.LogInformation("Send notification to Kafka successful.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka notification failed in AcceptRejectOrder");
                    _logger.LogInformation("Kafka notification failed in AcceptRejectOrder: {Message}", ex.Message);
                }

                return Ok(new { Message = "Order status updated successfully.", OrderID = existingOrder.OrderID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing the order.", ErrorMessage = ex.Message });
            }
        }

        #region "Helper Methods"

        private Guid GetDeliveryPersonalID(int status)
        {
            return new Guid(_configuration["OrderService:DeliveryPersonnelID"]);
        }

        //private async Task<string> SetProductImagePath(Guid productID)
        //{
        //    var product = await GetProductByProductID(productID);
        //    return string.Empty;
        //}

        private async Task<Dictionary<Guid, User>> GetUserInfoByRetailerIdAsync(List<Guid> retailerIDs)
        {
            return await userRepository.GetUserInfoByRetailerIdAsync(retailerIDs);
        }

        private async Task<bool> DeactivateShoppingCartItemById(Guid cartID)
        {
            bool result = false;

            var existingOrder = await shoppingCartRepository.GetShoppingCartItemByCartID(cartID);
            if (existingOrder == null)
            {
                throw new Exception($"There is no shopping cart item with cart ID: {cartID}");
            }
            existingOrder.IsActive = false;
            existingOrder.UpdatedOn = DateTime.UtcNow;
            existingOrder.UpdatedBy = Guid.Empty;
            result = await shoppingCartRepository.UpdateShoppingCartItemByCartIdAsync(existingOrder);

            return result;
        }

        //private decimal CalculateShippingCost(decimal totalPrice)
        //{
        //    decimal shippingCost = totalPrice * 0.03m;
        //    return shippingCost;
        //}

        private decimal CalculateTotalCost(List<CreateOrderDetailsDTO> items)
        {
            decimal totalPrice = 0;
            foreach (var item in items)
            {

                // Calculate total price (price * quantity)
                totalPrice += item.ProductPrice * item.Quantity;
            }
            return totalPrice;
        }

        private async Task<ProductDTO> GetProductByProductID(Guid productID)
        {
            var product = await productRepository.GetProductByProductIdAsync(productID);

            if (product == null)
            {
                throw new Exception($"ProductID {productID} does not exist.");
            }
            var productDTO = _mapper.Map<ProductDTO>(product);
            return productDTO;
        }

        private async Task<bool> UpdateProductQuantityAsync(Guid productID, int quantity)
        {
            _logger.LogInformation("Attempting to update product quantity. ProductID: {ProductID}, Quantity: {Quantity}", productID, quantity);

            var product = await productRepository.UpdateProductQuantityAsync(productID, quantity);

            if (product == null)
            {
                _logger.LogWarning("Failed to update product quantity. ProductID: {ProductID} might not exist.", productID);
                return false;
            }

            _logger.LogInformation("Product quantity updated successfully. ProductID: {ProductID}, New Quantity: {Quantity}", productID, quantity);
            return true;
        }
        #endregion
    }
}

