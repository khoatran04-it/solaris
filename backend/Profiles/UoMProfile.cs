using AutoMapper;
using backend.DTOs.UoMDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMProfile : Profile
    {
        public UoMProfile() 
        {
            // GET
            CreateMap<UoM, UoMReadDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

            // POST
            CreateMap<UoMCreateDto, UoM>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<UoMUpdateDto, UoM>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
