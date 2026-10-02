using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Sample.Api.Data;
using EFCore.DynamicQuery.Sample.Api.Models;

namespace EFCore.DynamicQuery.Sample.Api.Mapping;

public sealed class ProjectForgeMappingProfile : MappingProfile
{
    public ProjectForgeMappingProfile()
    {
        CreateMap<Customer, CustomerModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true);

        CreateMap<Order, OrderModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true)
            .ForMember(dest => dest.TotalItems, src => src.OrderItems.Count)
            .ForMember(dest => dest.ItemsList, src => string.Join(", ", src.OrderItems.Select(oi => oi.ProductName).Distinct()))
            .ForMember(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null);

        CreateMap<OrderItem, OrderItemModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true)
            // Stored as thousands on this (fictional) schema - clients that ask for transforms
            // see/filter in units instead, with no way to know the raw stored scale.
            .ForMember(dest => dest.Quantity, src => src.Quantity, cfg => cfg.MutableFunction = q => q * 1000);
    }
}
