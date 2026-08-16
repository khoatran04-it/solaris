using AutoMapper;
using backend.Models;
using backend.DTOs.CustomerGroupDTOs;
namespace backend.Profiles
{
    public class CustomerGroupProfile : Profile
    {
        public CustomerGroupProfile() 
        {
            //GET
            CreateMap<CustomerGroup, CustomerGroupReadDto>();

            //POST
            CreateMap<CustomerGroupCreateDto, CustomerGroup>();

            //PUT
            CreateMap<CustomerGroupUpdateDto, CustomerGroup>();
        }
    }
}
