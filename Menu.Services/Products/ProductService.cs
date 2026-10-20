using AutoMapper;
using Menu.Domain.Contracts;
using Menu.Domain.Entities.Products;
using Menu.Domain.Exceptions;
using Menu.Services.Abstraction.Products;
using Menu.Services.Specifications.Products;
using Menu.Shared.Dtos.Pagination;
using Menu.Shared.Dtos.Products;

namespace Menu.Services.Products
{
    // This class implements the IProductService interface and provides methods to retrieve product-related data.
    // Simply it is a service class that interacts with the data layer to fetch product information and maps it to DTOs for use in other parts of the application.
    public class ProductService(IUnitOfWork _unitOfWork, IMapper _mapper) : IProductService
    {

        public async Task<PaginatedResponse<ProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters)
        {

            var countSpecifications = new ProductCountSpecifications(queryParameters);
            var totalCount = await _unitOfWork.GetRepo<int, Product>().CountAsync(countSpecifications);

            var specifications = new ProductSpecifications(queryParameters);
            var products = await _unitOfWork.GetRepo<int, Product>().GetAllAsync(specifications);
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

            // using object initializer instead constructor:
            var paginatedResponse = new PaginatedResponse<ProductDto>
            {
                PageIndex = queryParameters.PageIndex,
                PageSize = queryParameters.PageSize,
                TotalCount = totalCount,
                Data = productDtos
            };

            return paginatedResponse;
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var specifications = new ProductSpecifications(id);
            var product = await _unitOfWork.GetRepo<int, Product>().GetByIdAsync(specifications);
            if (product == null)
            {
                throw new ProductNotFoundException(id);
            }
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync()
        {
            var brand = await _unitOfWork.GetRepo<int, ProductBrand>().GetAllAsync();
            return _mapper.Map<IEnumerable<BrandDto>>(brand);
        }

        public async Task<IEnumerable<TypeDto>> GetAllTypesAsync()
        {
            var type = await _unitOfWork.GetRepo<int, ProductType>().GetAllAsync();
            return _mapper.Map<IEnumerable<TypeDto>>(type);
        }

    }
}
