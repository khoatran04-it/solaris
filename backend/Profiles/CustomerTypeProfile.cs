using AutoMapper;
using backend.DTOs.CustomerTypeDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class CustomerTypeProfile : Profile
    {
        public CustomerTypeProfile() 
        {
            // GET
            CreateMap<CustomerType, CustomerTypeReadDto>();

            // POST
            CreateMap<CustomerTypeCreateDto, CustomerType>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<CustomerTypeUpdateDto, CustomerType>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
