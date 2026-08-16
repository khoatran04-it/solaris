using AutoMapper;
using backend.DTOs.CategoryAttributeDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class CategoryAttributeProfile : Profile
    {
        public CategoryAttributeProfile()
        {
            //GET
            CreateMap<CategoryAttribute, CategoryAttributeReadDto>()
                .ForMember(dest => dest.CategoryName, opt =>
                    opt.MapFrom(src => src.Category != null ? src.Category.Name : null))

                .ForMember(dest => dest.AttributeDefinitionName, opt =>
                    opt.MapFrom(src => src.AttributeDefinition != null ? src.AttributeDefinition.Name : null));

            //POST
            CreateMap<CategoryAttributeCreateDto, CategoryAttribute>();

            //PUT
            CreateMap<CategoryAttributeUpdateDto, CategoryAttribute>();

        }
    }
}
