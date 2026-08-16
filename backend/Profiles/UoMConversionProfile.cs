using AutoMapper;
using backend.DTOs.UoMConversionDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMConversionProfile : Profile
    {
        public UoMConversionProfile() 
        {
            //GET
            CreateMap<UoMConversion, UoMConversionReadDto>()
                .ForMember(dest => dest.FromUoMName, opt => opt.MapFrom(src => src.FromUoM.Name))
                .ForMember(dest => dest.ToUoMName, opt => opt.MapFrom(src => src.ToUoM.Name))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : null));

            //POST
            CreateMap<UoMConversionCreateDto, UoMConversion>();

            //PUT
            CreateMap<UoMConversionUpdateDto, UoMConversion>();

        }
    }
}
