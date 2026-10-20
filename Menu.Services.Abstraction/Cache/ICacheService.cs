namespace Menu.Services.Abstraction.Cache
{
    public interface ICacheService
    {
        Task<string?> GetAsync(string cacheKey);

        Task SetAsync(
            string cacheKey,
            object response,
            TimeSpan timeToLive);
    }
}