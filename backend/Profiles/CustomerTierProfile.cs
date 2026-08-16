using AutoMapper;
using backend.DTOs.CustomerTierDTOs;
using backend.Models;
namespace backend.Profiles
{
    public class CustomerTierProfile : Profile
    {
        public CustomerTierProfile() 
        {
            //GET
            CreateMap<CustomerTier, CustomerTierReadDto>();

            //POST
            CreateMap<CustomerTierCreateDto, CustomerTier>();

            //PUT
            CreateMap<CustomerTierUpdateDto, CustomerTier>();
        }
    }
}
