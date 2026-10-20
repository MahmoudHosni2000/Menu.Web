using AutoMapper;
using Menu.Domain.Entities.Products;
using Menu.Shared.Dtos.Products;

namespace Menu.Services.Mapping.Products
{
    // this class is used to map the Product entity to the ProductDto and vice versa using AutoMapper
    // "map" means to convert one object to another object, in this case, we are converting Product entity to ProductDto and vice versa
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.Name))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand.Name))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom<ProductPicUrlResolver>());

            CreateMap<ProductBrand, BrandDto>();
            CreateMap<ProductType, TypeDto>();
        }
    }
}
