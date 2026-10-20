using Menu.Services.Abstraction;
using Menu.Services.Abstraction.Products;
using Menu.Shared.Dtos.Errors;
using Menu.Shared.Dtos.Pagination;
using Menu.Shared.Dtos.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Menu.Presentation.Attributes;

namespace Menu.Presentation
{
    // API controller for managing product-related endpoints. This controller will handle HTTP requests related to products, such as retrieving product information, brands, and types.
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController(IServiceManager serviceManager) : ControllerBase
    {
        private readonly IProductService _productService = serviceManager.ProductService;

        [HttpGet] // GET: api/products
        [Cached(300)] // Custom attribute that indicates the response from this endpoint should be cached for 300 seconds (5 minutes). This can help improve performance by reducing the number of requests to the server for frequently accessed data.
        [ProducesResponseType(
            typeof(PaginatedResponse<ProductDto>),
            StatusCodes.Status200OK)] // Indicates that a successful response will return a PaginatedResponse<ProductDto> object with a 200 OK status code.
        [ProducesResponseType(
            typeof(ValidationErrorResponse),
            StatusCodes.Status400BadRequest)] // Indicates that if the request is invalid, a 400 Bad Request status code will be returned along with a ValidationErrorResponse object.
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryParameters queryParameters)
        {
            var products = await _productService.GetAllProductsAsync(queryParameters);
            return Ok(products);
        }

        [HttpGet("{id:int}")] // GET: api/products/12
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)] // Indicates that a successful response will return a ProductDto object with a 200 OK status code.
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)] // Indicates that if the product is not found, a 404 Not Found status code will be returned along with an ErrorResponse object.
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return Ok(product);
        }

        [HttpGet("brands")] // GET: api/products/brands
        [ProducesResponseType(
            typeof(IEnumerable<BrandDto>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllBrandsAsync()
        {
            var brands = await _productService.GetAllBrandsAsync();
            return Ok(brands);
        }

        [HttpGet("types")] // GET: api/products/types
        [ProducesResponseType(
            typeof(IEnumerable<TypeDto>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllTypesAsync()
        {
            var types = await _productService.GetAllTypesAsync();
            return Ok(types);
        }
    }
}
