using Menu.Services.Abstraction.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Menu.Presentation.Attributes
{
    public class CachedAttribute(
        int timeToLiveSeconds) : Attribute, IAsyncActionFilter
    {
        private readonly int _timeToLiveSeconds =
            timeToLiveSeconds;

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var cacheService =
                context.HttpContext.RequestServices
                    .GetRequiredService<ICacheService>();

            var request = context.HttpContext.Request;

            var cacheKey =
                $"{request.Path}{request.QueryString}";

            var cachedResponse =
                await cacheService.GetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResponse))
            {
                context.Result = new ContentResult
                {
                    Content = cachedResponse,
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };

                return;
            }

            var executedContext = await next();
            if (executedContext.Result is OkObjectResult okResult &&
                okResult.Value is not null)
            {
                await cacheService.SetAsync(
                    cacheKey,
                    okResult.Value,
                    TimeSpan.FromSeconds(_timeToLiveSeconds));
            }
        }
    }
}