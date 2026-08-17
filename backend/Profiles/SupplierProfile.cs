using AutoMapper;
using backend.DTOs.SupplierDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class SupplierProfile : Profile
    {
        public SupplierProfile() 
        {
            // GET
            CreateMap<Supplier, SupplierReadDto>()
                .ForMember(dest => dest.SupplierTypeName, opt => opt.MapFrom(src => src.SupplierType != null ? src.SupplierType.Name : null))
                .ForMember(dest => dest.SupplierTypeId, opt => opt.MapFrom(src => src.SupplierTypeId ?? 0))
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            // POST
            CreateMap<SupplierCreateDto, Supplier>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));

            // PUT
            CreateMap<SupplierUpdateDto, Supplier>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
