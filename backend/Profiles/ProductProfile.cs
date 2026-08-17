using AutoMapper;
using backend.DTOs.ProductDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // GET
            CreateMap<Product, ProductReadDto>()
                .ForMember(dest => dest.CategoryName, opt =>
                    opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.BaseUoMName, opt =>
                    opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Name : null));

            // POST
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
