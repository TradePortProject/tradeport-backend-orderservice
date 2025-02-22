// File: Profiles/ProductProfile.cs
using AutoMapper;
using OrderManagement.Models;
using OrderManagement.Models.DTO;


namespace OrderManagement.Mappings
{
    public class OrderAutoMapperProfiles : Profile
    {
        public OrderAutoMapperProfiles()
        {
            // Map from Product entity to ProductDTO.
            CreateMap<Order, CreateOrderDTO>();
            CreateMap<CreateOrderDTO, Order>();
            CreateMap<OrderDetails, CreateOrderDetailsDTO>();
            CreateMap<CreateOrderDetailsDTO, OrderDetails>();
            //.ForMember(
            //    dest => dest.CategoryDescription,
            //    opt => opt.MapFrom(src => EnumHelper.GetDescription<Category>(src.Category))
            //);

            // Map from CreateProductDTO to Product entity.
            //CreateMap<CreateProductDTO, Order>();
            //.ForMember(
            //    dest => dest.Category,
            //    opt => opt.MapFrom(src => EnumHelper.GetEnumFromDescription<Category>(src.CategoryDescription))
            //);

            // Map from UpdateProductDTO to Product entity.
            //CreateMap<UpdateProductDTO, Order>();
            //.ForMember(
            //    dest => dest.Category,
            //    opt => opt.MapFrom(src => EnumHelper.GetEnumFromDescription<Category>(src.CategoryDescription))
            //);
        }
    }
}

