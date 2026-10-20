using AutoMapper;
using Menu.Domain.Contracts;
using Menu.Shared.Dtos.Baskets;
using Menu.Domain.Entities.Baskets;
using Menu.Services.Abstraction.Baskets;

namespace Menu.Services.Baskets
{
    public class BasketService(
        IBasketRepository basketRepository,
        IMapper mapper) : IBasketService
    {
        private readonly IBasketRepository _basketRepository =
            basketRepository;

        private readonly IMapper _mapper = mapper;


        public async Task<BasketDto?> GetBasketAsync(string basketId)
        {
            var basket = await _basketRepository.GetBasketAsync(basketId);

            return basket is null
                ? null
                : _mapper.Map<BasketDto>(basket);
        }

        public async Task<BasketDto?> UpdateBasketAsync(BasketDto basket)
        {
            var customerBasket =
                _mapper.Map<CustomerBasket>(basket);

            var savedBasket =
                await _basketRepository.UpdateBasketAsync(customerBasket);

            return savedBasket is null
                ? null
                : _mapper.Map<BasketDto>(savedBasket);
        }

        public async Task<bool> DeleteBasketAsync(string basketId)
        {
            return await _basketRepository.DeleteBasketAsync(basketId);
        }
    }
}