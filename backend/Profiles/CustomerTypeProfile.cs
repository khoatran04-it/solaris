using AutoMapper;
using backend.Models;
using backend.DTOs.CustomerTypeDTOs;
namespace backend.Profiles
{
    public class CustomerTypeProfile : Profile
    {
        public CustomerTypeProfile() 
        {
            //GET
            CreateMap<CustomerType, CustomerTypeReadDto>();

            //POST
            CreateMap<CustomerTypeCreateDto, CustomerType>();

            //PUT
            CreateMap<CustomerTypeUpdateDto, CustomerType>();
        }


    }
}
