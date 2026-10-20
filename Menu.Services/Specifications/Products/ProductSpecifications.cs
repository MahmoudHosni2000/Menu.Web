using Menu.Domain.Entities.Products;
using Menu.Shared.Dtos.Products;

namespace Menu.Services.Specifications.Products
{
    public class ProductSpecifications : BaseSpecifications<int, Product>
    {
        public ProductSpecifications(ProductQueryParameters queryParameters) 
            : base(
                product =>
                (!queryParameters.BrandId.HasValue ||
                product.BrandId == queryParameters.BrandId.Value) &&
                (!queryParameters.TypeId.HasValue ||
                product.TypeId == queryParameters.TypeId.Value) &&
                (string.IsNullOrWhiteSpace(queryParameters.Search) ||
                product.Name.Contains(queryParameters.Search))
            ) 
        {
            AddInclude(p => p.Brand);
            AddInclude(p => p.Type);

            ApplyPagination((queryParameters.PageIndex - 1) * queryParameters.PageSize, queryParameters.PageSize);

            switch(queryParameters.Sort)
            {
                case "priceAsc":
                    AddOrderBy(p => p.Price);
                    break;
                case "priceDesc":
                    AddOrderByDescending(p => p.Price);
                    break;
                case "nameAsc":
                    AddOrderBy(p => p.Name);
                    break;
                case "nameDesc":
                    AddOrderByDescending(p => p.Name);
                    break;
                default:
                    AddOrderBy(p => p.Name);
                    break;
            }
        }

        public ProductSpecifications()
        {
            AddInclude(p => p.Brand);
            AddInclude(p => p.Type);
        }

        public ProductSpecifications(int id) : base(p => p.Id == id)
        {
            AddInclude(p => p.Brand);
            AddInclude(p => p.Type);
        }
    }
}
