using AutoMapper;
using OrderManagement.Models;
using OrderManagement.Models.DTO;
using OrderManagement.Common;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OrderManagement.Mappings
{
    public class OrderAutoMapperProfiles : Profile
    {
        public OrderAutoMapperProfiles()
        {
            CreateMap<Order, CreateOrderDTO>();
            CreateMap<OrderDetails, CreateOrderDetailsDTO>();

            // Mapping from CreateOrderDTO to Order entity
            CreateMap<CreateOrderDTO, Order>()
                .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(_ => (int)OrderStatus.Submitted)) // Set default status
                .ForMember(dest => dest.PaymentMode, opt => opt.MapFrom(_ => (int)PaymentMode.Cash)) // Set default payment mode
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow)) // Set CreatedOn time
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true)) // Ensure IsActive is set
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails)) //Set Order Details
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom((src, dest, destMember, context) =>
                                     src.OrderDetails.Select(detail =>
                                     {
                                         var mappedDetail = context.Mapper.Map<OrderDetails>(detail);
                                         mappedDetail.CreatedBy = src.CreatedBy; // ✅ Set CreatedBy from Order
                                         return mappedDetail;
                                     }).ToList()));

            // Mapping from CreateOrderDetailsDTO to OrderDetails entity
            CreateMap<CreateOrderDetailsDTO, OrderDetails>()
                .ForMember(dest => dest.OrderItemStatus, opt => opt.MapFrom(_ => (int)OrderStatus.Submitted)) // Set default status
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(_ => DateTime.UtcNow)) // Set CreatedOn
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true)); // Set IsActive

            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.OrderStatusValue,
                    opt => opt.MapFrom(src => EnumHelper.GetEnumDisplayName((OrderStatus)src.OrderStatus))) // ✅ Convert OrderStatus to string
                .ForMember(dest => dest.PaymentModeValue,
                    opt => opt.MapFrom(src => EnumHelper.GetEnumDisplayName((PaymentMode)src.PaymentMode))) // ✅ Convert PaymentMode to string
                .ForMember(dest => dest.RetailerName, opt => opt.Ignore());//Ignore because it’s computed


            CreateMap<OrderDetails, OrderDetailsDto>()
                .ForMember(dest => dest.OrderItemStatusValue,
                    opt => opt.MapFrom(src => EnumHelper.GetEnumDisplayName((OrderStatus)src.OrderItemStatus))) // ✅ Convert OrderItemStatus to string
                .ForMember(dest => dest.ProductName, opt => opt.Ignore())//Ignore because it’s computed
                .ForMember(dest => dest.ManufacturerName, opt => opt.Ignore());//Ignore because it’s computed

            CreateMap<ShoppingCart, CreateShoppingCartDTO>();
            CreateMap<CreateShoppingCartDTO, ShoppingCart>();

            CreateMap<ShoppingCart, ShoppingCartDTO>();
            CreateMap<ShoppingCartDTO, ShoppingCart>();


            // Convert string to int when mapping DTO → Entity
            CreateMap<UpdateOrderDTO, Order>()
                .ForMember(dest => dest.OrderStatus,
                    opt => opt.MapFrom(src => EnumHelper.GetEnumValueFromDisplayName<OrderStatus>(src.OrderStatus) ?? (int)OrderStatus.Submitted))                
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Convert int to string when mapping Entity → DTO
            CreateMap<Order, UpdateOrderDTO>()
                .ForMember(dest => dest.OrderStatus,
                    opt => opt.MapFrom(src => EnumHelper.GetEnumDisplayName((OrderStatus)src.OrderStatus)));

            CreateMap<Product, ProductDTO>();
            CreateMap<ProductDTO, Product>();

            // ✅ Mapping for Accepting Order
            CreateMap<AcceptOrderDTO, Order>()
                //.ForMember(dest => dest.OrderStatus,
                //    opt => opt.MapFrom(src => GetEnumValueFromDisplayName<OrderStatus>(src.OrderStatus) ?? (int)OrderStatus.Submitted))
                //.ForMember(dest => dest.DeliveryPersonnelID,
                //    opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.DeliveryPersonnelID)
                //        ? (Guid?)null : Guid.Parse(src.DeliveryPersonnelID)))
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(src => DateTime.UtcNow));

            // ✅ Mapping for Converting Order Back to DTO
            //CreateMap<Order, AcceptOrderDTO>()
            //.ForMember(dest => dest.OrderStatus,
            //    opt => opt.MapFrom(src => ((OrderStatus)src.OrderStatus).ToString()));
            //.ForMember(dest => dest.DeliveryPersonnelID,
            //    opt => opt.MapFrom(src => src.DeliveryPersonnelID.HasValue
            //        ? src.DeliveryPersonnelID.ToString() : ""));
        }

    }
}