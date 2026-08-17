using AutoMapper;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductCategoryGroupProfile : Profile
    {
        public ProductCategoryGroupProfile() 
        {
            // GET
            CreateMap<ProductCategoryGroup, ProductCategoryGroupReadDto>();

            // POST
            CreateMap<ProductCategoryGroupCreateDto, ProductCategoryGroup>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<ProductCategoryGroupUpdateDto, ProductCategoryGroup>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
