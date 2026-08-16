using AutoMapper;
using backend.Models;
using backend.DTOs.CustomerAddressDTOs;

namespace backend.Profiles
{
    public class CustomerAddressProfile : Profile
    {
        public CustomerAddressProfile() 
        {
            //GET
            CreateMap<CustomerAddress, CustomerAddressReadDto>();

            //POST
            CreateMap<CustomerAddressCreateDto, CustomerAddress>();

            //PUT
            CreateMap<CustomerAddressUpdateDto, CustomerAddress>();
        }
    }
}
