using AutoMapper;
using Menu.Domain.Entities.Products;
using Menu.Shared.Dtos.Products;
using Microsoft.AspNetCore.Http;

// This class is used to resolve the PictureUrl property of the ProductDto when mapping from Product entity to ProductDto using AutoMapper

namespace Menu.Services.Mapping.Products
{
    public class ProductPicUrlResolver : IValueResolver<Product, ProductDto, string>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductPicUrlResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public string Resolve(Product source, ProductDto destination, string destMember, ResolutionContext context)
        {
            var request = _httpContextAccessor.HttpContext?.Request;

            // If the ImageUrl is null or whitespace, return an empty string
            if (string.IsNullOrWhiteSpace(source.ImageUrl))
            {
                return string.Empty;
            }

            // If the request is null, return the ImageUrl as is
            if (request is null)
            {
                return source.ImageUrl;
            }

            // Construct the full URL using the request's scheme and host, and ensure the ImageUrl does not start with a leading slash
            return $"{request.Scheme}://{request.Host}/{source.ImageUrl.TrimStart('/')}";
        }
    }
}
