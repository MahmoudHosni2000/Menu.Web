namespace Menu.Domain.Contracts
{
    public interface ICacheRepository
    {
        Task<string?> GetAsync(string cacheKey);

        Task SetAsync(
            string cacheKey,
            object response,
            TimeSpan timeToLive);
    }
}