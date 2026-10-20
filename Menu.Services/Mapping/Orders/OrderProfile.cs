using AutoMapper;
using Menu.Domain.Entities.Orders;
using Menu.Shared.Dtos.Orders;

namespace Menu.Services.Mapping.Orders;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // 1. Map between OrderAddressDto and OrderAddress
        CreateMap<OrderAddressDto, OrderAddress>()
            .ReverseMap();

        // 2. Create a mapping between DeliveryMethod and DeliveryMethodResponse
        CreateMap<DeliveryMethod, DeliveryMethodResponse>();

        // 3. Create a mapping between OrderItem and OrderItemDto
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(
                destination => destination.ProductId,
                options => options.MapFrom(
                    source => source.Product.ProductId))
            .ForMember(
                destination => destination.ProductName,
                options => options.MapFrom(
                    source => source.Product.ProductName))
            .ForMember(
                destination => destination.PictureUrl,
                options => options.MapFrom(
                    source => source.Product.PictureUrl));

        // 4. Create a mapping between Order and OrderResponse
        CreateMap<Order, OrderResponse>()
            .ForMember(
                destination => destination.UserEmail,
                options => options.MapFrom(
                    source => source.BuyerEmail))
            .ForMember(
                destination => destination.DeliveryMethod,
                options => options.MapFrom(
                    source => source.DeliveryMethod.ShortName))
            .ForMember(
                destination => destination.Total,
                options => options.MapFrom(
                    source => source.GetTotal()));
    }
}