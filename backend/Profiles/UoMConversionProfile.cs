using AutoMapper;
using backend.DTOs.UoMConversionDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMConversionProfile : Profile
    {
        public UoMConversionProfile() 
        {
            // GET
            CreateMap<UoMConversion, UoMConversionReadDto>()
                .ForMember(dest => dest.FromUoMName, opt => opt.MapFrom(src => src.FromUoM != null ? src.FromUoM.Name : string.Empty))
                .ForMember(dest => dest.ToUoMName, opt => opt.MapFrom(src => src.ToUoM != null ? src.ToUoM.Name : string.Empty))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : null));

            // POST
            CreateMap<UoMConversionCreateDto, UoMConversion>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<UoMConversionUpdateDto, UoMConversion>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
