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


namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderManagementController : ControllerBase
    {

        private readonly AppDbContext dbContext;
        private readonly IOrderRepository orderRepository;
        private readonly IOrderDetailsRepository orderDetailsRepository;
        private readonly IShoppingCartRepository shoppingCartRepository;
        private readonly IProductServiceClient productServiceClient;
        private readonly IUserRepository userRepository;
        private readonly IMapper _mapper;
        public OrderManagementController(AppDbContext appDbContext, IOrderRepository orderRepo, IOrderDetailsRepository orderDetailsRepo, IShoppingCartRepository shoppingCartRepo, IMapper mapper, IProductServiceClient productServiceClient, IUserRepository userRepo)
        {
            this.dbContext = appDbContext;
            this.orderRepository = orderRepo;
            this.orderDetailsRepository = orderDetailsRepo;
            this.shoppingCartRepository = shoppingCartRepo;
            this.productServiceClient = productServiceClient;
            this.userRepository = userRepo;
            _mapper = mapper;
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
            var (orders, totalPages) = await orderRepository.GetFilteredOrdersAsync(
                orderId, retailerId, deliveryPersonnelId, orderStatus, manufacturerId, orderItemStatus, retailerName, manufacturerName, productName, pageNumber, pageSize);

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);

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




        //[HttpGet]
        //[Route("{id}")]
        //public async Task<IActionResult> GetOrderById(Guid id)
        //{
        //    try
        //    {
        //        var orderById = await orderRepository.GetOrderByIdAsync(id);

        //        if (orderById == null)
        //        {
        //            return NotFound(new
        //            {
        //                Message = "Order not found.",
        //                ProductCode = string.Empty,
        //                ErrorMessage = "Invalid Order ID."
        //            });
        //        }

        //        // Use AutoMapper to map the Product entity to ProductDTO.
        //        var productDto = _mapper.Map<OrderDTO>(orderById);

        //        return Ok(new
        //        {
        //            Message = "Product retrieved successfully.",
        //            Product = productDto,
        //            ErrorMessage = string.Empty
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Message = "An error occurred while retrieving the product.",
        //            ProductCode = string.Empty,
        //            ErrorMessage = ex.Message
        //        });
        //    }
        //}


        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO orderRequestDto)
        {

            if (orderRequestDto == null || orderRequestDto.OrderDetails == null || orderRequestDto.OrderDetails.Count == 0)
            {
                return BadRequest(new { Message = "Invalid Order Data.", ErrorMessage = "Order details are missing." });
            }

            var retailer = await userRepository.GetUserInfoByRetailerIdAsync(new List<Guid> { orderRequestDto.RetailerID });
            if (retailer == null || !retailer.ContainsKey(orderRequestDto.RetailerID))
            {
                return BadRequest(new { Message = "Invalid Retailer ID.", ErrorMessage = "The provided Retailer ID does not exist." });
            }


            try
            {
                var totalPrice = CalculateTotalCost(orderRequestDto.OrderDetails);  // Add logic to calculate total price
                //var shippingCost = CalculateShippingCost(totalPrice);  // Calculate shipping cost

                // Map the incoming CreateProductDTO to a Product entity.
                var orderModel = _mapper.Map<CreateOrderDTO, Order>(orderRequestDto);
                orderModel.TotalPrice = totalPrice;
                orderModel = await orderRepository.CreateOrderAsync(orderModel);

                var cartIDs = orderRequestDto.OrderDetails.Select(detail => detail.CartID).ToList();
                foreach (var cartID in cartIDs)
                {
                    await DeactivateShoppingCartItemById(cartID);
                }

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
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);
            }
        }

        [HttpPost("CreateShoppingCart")]
        public async Task<IActionResult> CreateShoppingCartItemAsync([FromBody] CreateShoppingCartDTO shoppingCartDto)
        {

            if (shoppingCartDto == null)
            {
                return BadRequest(new { Message = "Invalid Cart Item.", ErrorMessage = "Cart items are missing." });
            }

            var retailer = await userRepository.GetUserInfoByRetailerIdAsync(new List<Guid> { shoppingCartDto.RetailerID });
            if (retailer == null || !retailer.ContainsKey(shoppingCartDto.RetailerID))
            {
                return BadRequest(new { Message = "Invalid Retailer ID.", ErrorMessage = "The provided Retailer ID does not exist." });
            }

            try
            {
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
                orderModel = await shoppingCartRepository.CreateShoppingCartItemsAsync(orderModel);

                var response = new
                {
                    Message = "Item added to the cart successfully.",
                    ErrorMessage = ""
                };
                return Ok(new { cartID = orderModel.CartID, response });
            }

            catch (Exception ex)
            {
                var response = new
                {
                    Message = "Item creation failed.",
                    ShoppingCartID = string.Empty,
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);
            }
        }



        private async Task<string> SetProductImagePath(Guid productID)
        {
            var product = await GetProductByProductID(productID);
            return string.Empty;
        }

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


        private decimal CalculateShippingCost(decimal totalPrice)
        {
            decimal shippingCost = totalPrice * 0.03m;
            return shippingCost;
        }

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

        [HttpGet("GetShoppingCart/{retailerID}")]
        public async Task<IActionResult> GetShoppingCartByRetailerId(Guid retailerID)
        {
            try
            {
                // Get orders by RetailerID
                var shoppingCart = await shoppingCartRepository.GetShoppingCartByRetailerIdAsync(retailerID, (int)OrderStatus.Save);

                if (shoppingCart == null || shoppingCart.Count == 0)
                {
                    return StatusCode(404, new
                    {
                        Message = "No cart items found for the provided Retailer." + retailerID,
                        ErrorMessage = ""
                    });
                }

                // Map entities to DTOs
                var shoppingCartDTO = _mapper.Map<List<ShoppingCartDTO>>(shoppingCart);

                // Get user information by retailer IDs
                var retailerIDs = shoppingCart.Select(sc => sc.RetailerID).Distinct().ToList();


                var retailers = await GetUserInfoByRetailerIdAsync(retailerIDs);

                foreach (var cart in shoppingCartDTO)
                {

                    if (retailers.ContainsKey(cart.RetailerID))
                    {
                        var retailer = retailers[cart.RetailerID];
                        cart.RetailerName = retailer.UserName;
                        cart.PhoneNumber = retailer.PhoneNo;
                        cart.Address = retailer.Address;
                    }

                    var product = await GetProductByProductID(cart.ProductID);
                    cart.ProductName = product != null ? product.ProductName : string.Empty;
                    cart.IsOutOfStock = product != null ? product.Quantity < cart.OrderQuantity : false;
                    cart.ManufacturerID = product != null ? product.ManufacturerID : Guid.Empty;
                    cart.ProductImagePath = string.Empty;
                    cart.TotalPrice = cart.OrderQuantity * cart.ProductPrice;
                    cart.OrderQuantity = cart.OrderQuantity;

                }
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
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving the cart items for - " + retailerID,
                    ErrorMessage = ex.Message
                });
            }
        }


        [HttpPut("DeleteCartItemByID")]
        public async Task<IActionResult> DeleteShoppingCartItemByCardID(Guid cartID)
        {
            if (cartID == Guid.Empty)
            {
                return BadRequest(new { Message = "", ErrorMessage = "Invalid Cart ID." });
            }

            try
            {
                bool result = await DeactivateShoppingCartItemById(cartID);

                if (!result)
                {
                    return StatusCode(500, new { Message = "Failed to remove item from the cart.", ErrorMessage = "Internal server error." });
                }

                return Ok(new
                {
                    Message = "Item removed from the cart successfully.",
                    ErrorMessage = string.Empty
                });

                //return Ok(new { Message = "Order updated successfully.", OrderID = updatedOrder.OrderID });
            }
            catch (Exception ex)
            {
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
            if (updateOrderDto == null || updateOrderDto.OrderID == Guid.Empty || string.IsNullOrEmpty(updateOrderDto.OrderStatus))
            {
                return BadRequest(new { Message = "Invalid order update data.", ErrorMessage = "Order ID or status is missing." });
            }

            try
            {
                var existingOrder = await orderRepository.GetOrderByIdAsync(updateOrderDto.OrderID);

                if (existingOrder == null)
                {
                    return NotFound(new { Message = "Order not found.", ErrorMessage = "Invalid Order ID." });
                }

                // Use AutoMapper to update the existing order
                _mapper.Map(updateOrderDto, existingOrder);

                var updatedOrder = await orderRepository.UpdateOrderAsync(existingOrder);

                if (updatedOrder == null)
                {
                    return StatusCode(500, new { Message = "Failed to update order.", OrderId = existingOrder.OrderID, ErrorMessage = "Internal server error." });
                }

                return Ok(new
                {
                    Message = "Order updated successfully.",
                    OrderId = updatedOrder.OrderID,
                    ErrorMessage = string.Empty
                });

                //return Ok(new { Message = "Order updated successfully.", OrderID = updatedOrder.OrderID });
            }
            catch (Exception ex)
            {
                var response = new
                {
                    Message = "An error occurred while updating the order.",
                    OrderId = updateOrderDto.OrderID,
                    ErrorMessage = ex.Message + Environment.NewLine + ex.InnerException
                };

                return StatusCode(500, response);

            }
        }

        //[HttpPut("AcceptOrder")]
        //public async Task<IActionResult> AcceptOrder([FromBody] AcceptOrderDTO acceptOrderDto, [FromServices] IProductServiceClient productServiceClient)
        //{
        //    if (acceptOrderDto == null || acceptOrderDto.OrderID == Guid.Empty || string.IsNullOrEmpty(acceptOrderDto.OrderStatus))
        //    {
        //        return BadRequest(new { Message = "Invalid request.", ErrorMessage = "OrderID and OrderStatus are required." });
        //    }

        //    try
        //    {
        //        var existingOrder = await orderRepository.GetOrderByIdAsync(acceptOrderDto.OrderID);
        //        if (existingOrder == null)
        //        {
        //            return NotFound(new { Message = "Order not found.", ErrorMessage = "Invalid Order ID." });
        //        }

        //        if (existingOrder.OrderStatus != (int)OrderStatus.Accepted)
        //        {
        //            return BadRequest(new { Message = "Order cannot be accepted.", ErrorMessage = "Only 'New' orders can be accepted." });
        //        }

        //        _mapper.Map(acceptOrderDto, existingOrder);

        //        var orderDetails = await orderRepository.GetOrderDetailsByOrderIdAsync(acceptOrderDto.OrderID);
        //        if (orderDetails == null || !orderDetails.Any())
        //        {
        //            return BadRequest(new { Message = "No order details found.", ErrorMessage = "Cannot process order without items." });
        //        }

        //        foreach (var item in orderDetails)
        //        {
        //            var product = await productServiceClient.GetProductByIdAsync(item.ProductID);

        //            if (product == null)
        //            {
        //                return BadRequest(new { Message = "Product not found.", ErrorMessage = $"ProductID {item.ProductID} does not exist." });
        //            }

        //            if (product.Quantity == null || product.Quantity < item.Quantity)
        //            {
        //                return BadRequest(new { Message = "Insufficient stock.", ErrorMessage = $"Product {product.ProductID} has insufficient quantity." });
        //            }

        //            int updatedQuantity = (product.Quantity ?? 0) - item.Quantity;
        //            bool productUpdated = await productServiceClient.UpdateProductQuantityAsync(item.ProductID, updatedQuantity);

        //            if (!productUpdated)
        //            {
        //                return StatusCode(500, new { Message = "Failed to update product quantity.", ErrorMessage = $"Could not update ProductID {product.ProductID}." });
        //            }
        //        }

        //        var updatedOrder = await orderRepository.UpdateOrderAsync(existingOrder);
        //        if (updatedOrder == null)
        //        {
        //            return StatusCode(500, new { Message = "Failed to update order.", ErrorMessage = "Internal server error." });
        //        }

        //        return Ok(new { Message = "Order accepted successfully.", OrderID = updatedOrder.OrderID });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { Message = "An error occurred while accepting the order.", ErrorMessage = ex.Message });
        //    }
        //}

        [HttpPut("AcceptRejectOrder")]
        public async Task<IActionResult> AcceptRejectOrder([FromBody] AcceptOrderDTO acceptOrderDto)
        {
            if (acceptOrderDto == null || acceptOrderDto.OrderID == Guid.Empty || acceptOrderDto.OrderItems == null || !acceptOrderDto.OrderItems.Any())
            {
                return BadRequest(new { Message = "Invalid request.", ErrorMessage = "OrderID and OrderItems are required." });
            }

            try
            {
                var existingOrder = await orderRepository.GetOrderByIdAsync(acceptOrderDto.OrderID);
                if (existingOrder == null)
                {
                    return NotFound(new { Message = "Order not found.", ErrorMessage = "Invalid Order ID." });
                }

                var orderDetails = await orderRepository.GetOrderDetailsByOrderIdAsync(acceptOrderDto.OrderID);
                if (orderDetails == null || !orderDetails.Any())
                {
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
                        return BadRequest(new { Message = "Order item not found.", ErrorMessage = $"OrderDetailID {itemDto.OrderDetailID} does not exist." });
                    }

                    var product = await productServiceClient.GetProductByIdAsync(orderItem.ProductID);
                    if (product == null)
                    {
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
                            return BadRequest(new { Message = "Insufficient stock.", ErrorMessage = $"Product {product.ProductID} has insufficient quantity." });
                        }
                        int updatedQuantity = product.Quantity.GetValueOrDefault() - orderItem.Quantity;
                        bool productUpdated = await productServiceClient.UpdateProductQuantityAsync(orderItem.ProductID, updatedQuantity);

                        if (!productUpdated)
                        {
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
                    existingOrder = await orderRepository.UpdateOrderStatusAsync(existingOrder.OrderID, (int)OrderStatus.Rejected);

                    // ✅ Restore quantity for ALL previously accepted items
                    var allAcceptedItems = previouslyAcceptedItems.Concat(newlyAcceptedItems).ToList();
                    foreach (var item in allAcceptedItems)
                    {
                        var product = await productServiceClient.GetProductByIdAsync(item.ProductID);
                        if (product != null)
                        {
                            int restoredQuantity = product.Quantity.GetValueOrDefault() + item.Quantity;
                            await productServiceClient.UpdateProductQuantityAsync(item.ProductID, restoredQuantity);
                        }
                    }
                }
                else if (isAllAccepted)
                {
                    existingOrder = await orderRepository.UpdateOrderStatusAsync(existingOrder.OrderID, (int)OrderStatus.Accepted);
                }

                return Ok(new { Message = "Order status updated successfully.", OrderID = existingOrder.OrderID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing the order.", ErrorMessage = ex.Message });
            }
        }

        private async Task<ProductDTO> GetProductByProductID(Guid productID)
        {
            var product = await productServiceClient.GetProductByIdAsync(productID);

            if (product == null)
            {
                throw new Exception($"ProductID {productID} does not exist.");
            }

            return product;
        }
        //GetOrders by Manufacturer id 
        [HttpGet("GetOrdersByManufacturerId/{manufacturerId}")]
        public async Task<IActionResult> GetOrdersByManufacturerId(Guid manufacturerId)
        {
            try
            {
                // Get orders by ManufacturerID
                var orders = await orderRepository.GetOrderByManufacturerIdAsync(manufacturerId);
                if (orders == null || !orders.Any())
                {
                    return Ok(new
                    {
                        Message = "Failed",
                        ErrorMessage = "No orders found for the provided Manufacturer ID."
                    });
                }

                // Get related order details for each order
                var orderDetails = await orderDetailsRepository.FindByCondition(od => orders.Select(o => o.OrderID).Contains(od.OrderID)).ToListAsync();
                // Map entities to DTOs
                var ordersDto = _mapper.Map<List<CreateOrderDTO>>(orders);
                var orderDetailsDto = _mapper.Map<List<CreateOrderDetailsDTO>>(orderDetails);
                return Ok(new
                {

                    Message = "Orders fetched successfully.",
                    ErrorMessage = string.Empty,
                    Data = ordersDto.Select(order => new
                    {

                        retailerID = order.RetailerID,
                        paymentMode = order.PaymentMode,
                        paymentCurrency = order.PaymentCurrency,
                        shippingCost = order.ShippingCost,
                        shippingCurrency = order.ShippingCurrency,
                        shippingAddress = order.ShippingAddress,
                        createdBy = order.CreatedBy,
                        orderDetails = orderDetailsDto.Select(detail => new
                        {
                            productID = detail.ProductID,
                            manufacturerId = detail.ManufacturerID,
                            quantity = detail.Quantity,
                            productPrice = detail.ProductPrice
                        }).ToList()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving the orders for - ",
                    ManufacturerId = manufacturerId,
                    ErrorMessage = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            try
            {
                // Get orders by ManufacturerID
                var order = await orderRepository.GetOrderByOrderIdAsync(id);
                if (order == null || !order.Any())
                {
                    return Ok(new
                    {
                        Message = "Failed",
                        ErrorMessage = "No order found for the provided order ID."
                    });
                }

                // Get related order details for each order
                var orderDetails = await orderDetailsRepository.FindByCondition(od => order.Select(o => o.OrderID).Contains(od.OrderID)).ToListAsync();

                // Map entities to DTOs
                var orderDto = _mapper.Map<List<GetOrderDTO>>(order);
                var orderDetailsDto = _mapper.Map<List<GetOrderDetailsDTO>>(orderDetails);
                return Ok(new
                {

                    Message = "Order fetched successfully.",
                    ErrorMessage = string.Empty,
                    Data = orderDto.Select(order => new
                    {
                        orderID = order.OrderID,
                        retailerID = order.RetailerID,
                        manufacturerID = order.ManufacturerID,
                        deliveryPersonnelId = order.DeliveryPersonnelID,
                        orderStatus = order.OrderStatus,
                        totalPrice = order.TotalPrice,
                        paymentMode = order.PaymentMode,
                        paymentCurrency = order.PaymentCurrency,
                        shippingCost = order.ShippingCost,
                        shippingCurrency = order.ShippingCurrency,
                        shippingAddress = order.ShippingAddress,
                        createdOn = order.CreatedOn,
                        createdBy = order.CreatedBy,
                        updatedOn = order.UpdatedOn,
                        updatedBy = order.UpdatedBy,
                        orderDetails = orderDetailsDto.Select(detail => new
                        {
                            orderDetailid = detail.OrderDetailID,
                            productID = detail.ProductID,
                            quantity = detail.Quantity,
                            productPrice = detail.ProductPrice
                        }).ToList()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving the orders for - ",
                    ManufacturerId = id,
                    ErrorMessage = ex.Message
                });
            }
        }

        //[HttpPut]
        //[Route("{id}")]
        //public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDTO updateProductRequestDto)
        //{
        //    try
        //    {
        //        // Check if the product exists
        //        var existingProduct = await orderRepository.GetProductByIdAsync(id);
        //        if (existingProduct == null)
        //        {
        //            return NotFound(new
        //            {
        //                Message = "Product not found.",
        //                ProductCode = string.Empty,
        //                ErrorMessage = "Invalid product ID."
        //            });
        //        }

        //        // Use AutoMapper to update the existing product with values from the DTO.
        //        _mapper.Map(updateProductRequestDto, existingProduct);

        //        // Optionally update properties that aren’t handled by AutoMapper.
        //        existingProduct.UpdatedOn = DateTime.UtcNow;

        //        // Update the product in the repository
        //        var updatedProduct = await orderRepository.UpdateProductAsync(id, existingProduct);

        //        if (updatedProduct == null)
        //        {
        //            return StatusCode(500, new
        //            {
        //                Message = "Failed to update product.",
        //                ProductCode = string.Empty,
        //                ErrorMessage = "Internal server error."
        //            });
        //        }

        //        return Ok(new
        //        {
        //            Message = "Product updated successfully.",
        //            ProductCode = updatedProduct.ProductCode,
        //            ErrorMessage = string.Empty
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Message = "An error occurred while updating the product.",
        //            ProductCode = string.Empty,
        //            ErrorMessage = ex.Message
        //        });
        //    }
        //}


        //[HttpDelete]
        //[Route("{id}")]
        //public async Task<IActionResult> DeleteProduct(Guid id)
        //{
        //    try
        //    {
        //        // Check if the product exists
        //        var productById = await orderRepository.GetProductByIdAsync(id);
        //        if (productById == null)
        //        {
        //            return NotFound(new
        //            {
        //                Message = "Product not found.",
        //                ProductCode = string.Empty,
        //                ErrorMessage = "Invalid product ID."
        //            });
        //        }

        //        productById.IsActive = false;

        //        await orderRepository.UpdateProductAsync(id, productById);
        //        return Ok(new
        //        {
        //            Message = "Product deleted successfully.",
        //            ProductCode = productById.ProductCode,
        //            ErrorMessage = string.Empty
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Message = "An error occurred while deleting the product.",
        //            ProductCode = string.Empty,
        //            ErrorMessage = ex.Message
        //        });
        //    }
        //}


    }
}

