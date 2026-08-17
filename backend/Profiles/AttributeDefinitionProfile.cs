using AutoMapper;
using backend.DTOs.AttributeDefinitionDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class AttributeDefinitionProfile : Profile
    {
        public AttributeDefinitionProfile()
        {
            // GET
            CreateMap<AttributeDefinition, AttributeDefinitionReadDto>();

            // POST
            CreateMap<AttributeDefinitionCreateDto, AttributeDefinition>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<AttributeDefinitionUpdateDto, AttributeDefinition>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
