using AutoMapper;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile()
        {
            // GET
            CreateMap<ProductCategory, ProductCategoryReadDto>()
                .ForMember(dest => dest.CategoryGroupName, opt =>
                    opt.MapFrom(src => src.CategoryGroup != null ? src.CategoryGroup.Name : null));

            // POST
            CreateMap<ProductCategoryCreateDto, ProductCategory>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<ProductCategoryUpdateDto, ProductCategory>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
