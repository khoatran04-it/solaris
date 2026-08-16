using AutoMapper;
using backend.DTOs.SupplierAddressDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class SupplierAddressProfile : Profile
    {
        public SupplierAddressProfile()
        {
            //GET
            CreateMap<SupplierAddress, SupplierAddressReadDto>();

            //POST
            CreateMap<SupplierAddressCreateDto, SupplierAddress>();

            //PUT
            CreateMap<SupplierAddressUpdateDto, SupplierAddress>();
        }
    }
}
