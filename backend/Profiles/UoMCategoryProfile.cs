using AutoMapper;
using backend.DTOs.UoMCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMCategoryProfile : Profile
    {
        public UoMCategoryProfile() 
        {
            // GET
            CreateMap<UoMCategory, UoMCategoryReadDto>()
                .ForMember(dest => dest.BaseUoMName, opt => opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Name : null));

            // POST
            CreateMap<UoMCategoryCreateDto, UoMCategory>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<UoMCategoryUpdateDto, UoMCategory>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
