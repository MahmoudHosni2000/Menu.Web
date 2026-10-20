using StackExchange.Redis;
using Menu.Domain.Entities.Baskets;
using System.Text.Json;
using Menu.Domain.Contracts;

namespace Menu.Persistence.Repos
{
    public class BasketRepository(IConnectionMultiplexer connection) : IBasketRepository
    {
        private readonly IDatabase _database = connection.GetDatabase();

        // Retrieves a basket from Redis based on the provided basketId. If the basket is found, it is deserialized from JSON and returned as a CustomerBasket object. If not found, null is returned.
        public async Task<CustomerBasket?> GetBasketAsync(string basketId)
        {
            var basketJson = await _database.StringGetAsync(basketId);

            return basketJson.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<CustomerBasket>(
                    basketJson.ToString());
        }

        // Updates or creates a basket in Redis by serializing the provided CustomerBasket object to JSON and storing it with the specified basketId. Returns the updated CustomerBasket object.
        public async Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket)
        {
            var basketJson = JsonSerializer.Serialize(basket);

            var isSaved = await _database.StringSetAsync(
                basket.Id,
                basketJson,
                TimeSpan.FromDays(30));

            return isSaved ? basket : null;
        }

        // Deletes a basket from Redis based on the provided basketId. Returns true if the deletion was successful, otherwise false.
        public async Task<bool> DeleteBasketAsync(string basketId)
        {
            return await _database.KeyDeleteAsync(basketId);
        }
    }
}