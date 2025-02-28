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
            CreateMap<CreateOrderDTO, Order>();
            CreateMap<OrderDetails, CreateOrderDetailsDTO>();
            CreateMap<CreateOrderDetailsDTO, OrderDetails>();

            CreateMap<Order, GetOrderDTO>();

            CreateMap<GetOrderDTO, Order>();

            CreateMap<OrderDetails, GetOrderDetailsDTO>();

            CreateMap<GetOrderDetailsDTO, OrderDetails>();


            // Convert string to int when mapping DTO → Entity
            CreateMap<UpdateOrderDTO, Order>()
                .ForMember(dest => dest.OrderStatus,
                    opt => opt.MapFrom(src => GetEnumValueFromDisplayName<OrderStatus>(src.OrderStatus) ?? (int)OrderStatus.New))
                .ForMember(dest => dest.DeliveryPersonnelID,
                    opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.DeliveryPersonnelID)
                        ? (Guid?)null : Guid.Parse(src.DeliveryPersonnelID)))
                .ForMember(dest => dest.UpdatedOn, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Convert int to string when mapping Entity → DTO
            CreateMap<Order, UpdateOrderDTO>()
                .ForMember(dest => dest.OrderStatus,
                    opt => opt.MapFrom(src => GetEnumDisplayName((OrderStatus)src.OrderStatus)))
                .ForMember(dest => dest.DeliveryPersonnelID,
                    opt => opt.MapFrom(src => src.DeliveryPersonnelID.HasValue
                        ? src.DeliveryPersonnelID.ToString() : ""));


        }

        // Helper to get enum int value from display name
        private static int? GetEnumValueFromDisplayName<TEnum>(string displayName) where TEnum : struct, Enum
        {
            foreach (var field in typeof(TEnum).GetFields())
            {
                var attribute = field.GetCustomAttribute<DisplayAttribute>();
                if (attribute != null && attribute.Name == displayName)
                {
                    return (int)field.GetValue(null);
                }
            }
            return null;
        }

        // Helper to get display name from enum value
        private static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : Enum
        {
            var field = typeof(TEnum).GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? value.ToString();
        }
    }
}



