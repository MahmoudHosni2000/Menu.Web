using Menu.Shared.Dtos.Baskets;

namespace Menu.Services.Abstraction.Baskets
{
    public interface IBasketService
    {
        Task<BasketDto?> GetBasketAsync(string basketId);

        Task<BasketDto?> UpdateBasketAsync(BasketDto basket);

        Task<bool> DeleteBasketAsync(string basketId);
    }
}