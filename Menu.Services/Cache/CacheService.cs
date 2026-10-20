using Menu.Domain.Contracts;
using Menu.Services.Abstraction.Cache;

namespace Menu.Services.Cache
{
    public class CacheService(
        ICacheRepository cacheRepository) : ICacheService
    {
        private readonly ICacheRepository _cacheRepository =
            cacheRepository;

        public Task<string?> GetAsync(string cacheKey)
        {
            return _cacheRepository.GetAsync(cacheKey);
        }

        public Task SetAsync(
            string cacheKey,
            object response,
            TimeSpan timeToLive)
        {
            return _cacheRepository.SetAsync(cacheKey, response, timeToLive);
        }
    }
}