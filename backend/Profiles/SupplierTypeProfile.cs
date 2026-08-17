using AutoMapper;
using backend.Models;
using backend.DTOs.SupplierTypeDTOs;

namespace backend.Profiles
{
    public class SupplierTypeProfile : Profile
    {
        public SupplierTypeProfile() 
        {
            //GET
            CreateMap<SupplierType, SupplierTypeReadDto>();

            //POST
            CreateMap<SupplierTypeCreateDto, SupplierType>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            //PUT
            CreateMap<SupplierTypeUpdateDto, SupplierType>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
