using Menu.Domain.Contracts;
using StackExchange.Redis;
using System.Text.Json;

namespace Menu.Persistence.Repos
{
    public class CacheRepository(
        IConnectionMultiplexer connection) : ICacheRepository
    {
        private readonly IDatabase _database =
            connection.GetDatabase();

        public async Task<string?> GetAsync(string cacheKey)
        {
            var cachedResponse =
                await _database.StringGetAsync(cacheKey);

            return cachedResponse.IsNullOrEmpty
                ? null
                : cachedResponse.ToString();
        }

        public async Task SetAsync(
            string cacheKey,
            object response,
            TimeSpan timeToLive)
        {
            var serializedResponse = JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            await _database.StringSetAsync(
                cacheKey,
                serializedResponse,
                timeToLive);
        }
    }
}