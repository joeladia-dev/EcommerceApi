using AutoMapper;
using EcommerceApi.Dtos;
using EcommerceApi.Models;

namespace EcommerceApi.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : null));

        CreateMap<ProductCreateDto, Product>();
        CreateMap<ProductUpdateDto, Product>();

        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.ProductsCount, opt => opt.MapFrom(s => s.Products.Count));
        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();
    }
}
