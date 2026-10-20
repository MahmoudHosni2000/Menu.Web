using Menu.Shared.Dtos.Pagination;
using Menu.Shared.Dtos.Products;

namespace Menu.Services.Abstraction.Products
{
    public interface IProductService
    {
        // Get all products
        Task<PaginatedResponse<ProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters);

        // Get product by id
        Task<ProductDto> GetProductByIdAsync(int id);

        // Get all types
        Task<IEnumerable<TypeDto>> GetAllTypesAsync();

        // Get all brands
        Task<IEnumerable<BrandDto>> GetAllBrandsAsync();
    }
}
