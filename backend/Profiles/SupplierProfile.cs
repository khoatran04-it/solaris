using AutoMapper;
using backend.Models;
using backend.DTOs.SupplierDTOs;
namespace backend.Profiles
{
    public class SupplierProfile : Profile
    {
        public SupplierProfile() 
        {
            //GET
            CreateMap<Supplier, SupplierReadDto>()
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses)); ;

            //POST
            CreateMap<SupplierCreateDto, Supplier>();

            //PUT
            CreateMap<SupplierUpdateDto, Supplier>();

        }
    }
}
