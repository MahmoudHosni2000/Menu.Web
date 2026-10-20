using AutoMapper;
using Menu.Domain.Entities.Baskets;
using Menu.Shared.Dtos.Baskets;

namespace Menu.Services.Mapping.Baskets
{
    public class BasketProfile : Profile
    {
        public BasketProfile()
        {
            CreateMap<BasketDto, CustomerBasket>()
                .ReverseMap();

            CreateMap<BasketItemDto, BasketItem>()
                .ReverseMap();
        }
    }
}