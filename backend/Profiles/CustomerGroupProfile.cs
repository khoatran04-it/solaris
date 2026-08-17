using AutoMapper;
using backend.DTOs.CustomerGroupDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class CustomerGroupProfile : Profile
    {
        public CustomerGroupProfile() 
        {
            // GET
            CreateMap<CustomerGroup, CustomerGroupReadDto>();

            // POST
            CreateMap<CustomerGroupCreateDto, CustomerGroup>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<CustomerGroupUpdateDto, CustomerGroup>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
