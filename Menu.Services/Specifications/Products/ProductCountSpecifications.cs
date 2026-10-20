using Menu.Domain.Entities.Products;
using Menu.Shared.Dtos.Products;

namespace Menu.Services.Specifications.Products
{
    public class ProductCountSpecifications : BaseSpecifications<int, Product>
    {
        public ProductCountSpecifications(ProductQueryParameters queryParameters) : base(
            product =>
            (!queryParameters.BrandId.HasValue ||
            product.BrandId == queryParameters.BrandId.Value) &&
            (!queryParameters.TypeId.HasValue ||
            product.TypeId == queryParameters.TypeId.Value) &&
            (string.IsNullOrWhiteSpace(queryParameters.Search) ||
            product.Name.Contains(queryParameters.Search))
        )
        { }
    }
}
