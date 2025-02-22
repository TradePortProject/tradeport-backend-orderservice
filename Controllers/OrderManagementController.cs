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


namespace OrderManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderManagementController : ControllerBase
    {

        private readonly AppDbContext dbContext;
        private readonly IOrderRepository orderRepository;
        private readonly IOrderDetailsRepository orderDetailsRepository;
        private readonly IMapper _mapper;
        public OrderManagementController(AppDbContext appDbContext, IOrderRepository orderRepo, IOrderDetailsRepository orderDetailsRepo, IMapper mapper)
        {
            this.dbContext = appDbContext;
            this.orderRepository = orderRepo;
            this.orderDetailsRepository = orderDetailsRepo;
            _mapper = mapper;
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllOrders()
        //{
        //    try
        //    {
        //        var orderModel = await orderRepository.GetAllOrdersAsync();

        //        if (orderModel == null || !orderModel.Any())
        //        {
        //            return NotFound(new
        //            {
        //                Message = "No orders found.",
        //                ErrorMessage = "No data available."
        //            });
        //        }

        //        // Use AutoMapper to map the list of Product entities to ProductDTOs.
        //        var productDTOs = _mapper.Map<List<OrderDTO>>(orderModel);

        //        return Ok(new
        //        {
        //            Message = "Orders retrieved successfully.",
        //            Products = productDTOs,
        //            ErrorMessage = string.Empty
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            Message = "An error occurred while retrieving the products.",
        //            ErrorMessage = ex.Message
        //        });
        //    }
        //}


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


        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO orderRequestDto)
        {

            if (orderRequestDto == null || orderRequestDto.OrderDetails == null || orderRequestDto.OrderDetails.Count == 0)
            {
                return BadRequest(new { Message = "Invalid order data.", ErrorMessage = "Order details are missing." });
            }

            try
            {
                var totalPrice = CalculateTotalCost(orderRequestDto.OrderDetails);  // Add logic to calculate total price
                //var shippingCost = CalculateShippingCost(totalPrice);  // Calculate shipping cost

                // Map the incoming CreateProductDTO to a Product entity.
                var orderModel = _mapper.Map<CreateOrderDTO, Order>(orderRequestDto);
                orderModel.OrderStatus = (int)OrderStatus.New;
                orderModel.DeliveryPersonnelID = null;
                orderModel.PaymentMode = (int)PaymentMode.Cash;
                orderModel.TotalPrice = totalPrice;
                //orderModel.ShippingCost = shippingCost;
                orderModel.CreatedOn = DateTime.UtcNow;
                orderModel.IsActive = true;

                // Use Repository to create Product
                orderModel = await orderRepository.CreateOrderAsync(orderModel);

                //Loop through the list of OrderDetails from the request
                foreach (var detailDto in orderRequestDto.OrderDetails)
                {
                    // Map CreateOrderDetailRequestDto to OrderDetail
                    var orderDetail = _mapper.Map<OrderDetails>(detailDto);
                    
                    // Set the OrderID and other fields for the OrderDetail
                    orderDetail.OrderID = orderModel.OrderID;
                    orderDetail.CreatedBy = orderModel.CreatedBy;
                     orderDetail.CreatedOn = DateTime.UtcNow;
                    orderDetail.IsActive = true;

                    // Save each OrderDetail one by one to the database
                    await orderDetailsRepository.CreateOrderDetailsAsync(orderDetail);
                }

                var response = new
                {
                    Message = "Order created successfully.",
                    OrderId = orderModel.OrderID,
                    ErrorMessage = ""
                };
                return Ok(new { id = orderModel.OrderID, response });
            }

            catch (Exception ex)
            {
                var response = new
                {
                    Message = "Order creation failed.",
                    ProductCode = string.Empty,
                    ErrorMessage = ex.Message
                };

                return StatusCode(500, response);
            }
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

