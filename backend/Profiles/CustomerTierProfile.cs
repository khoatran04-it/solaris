using AutoMapper;
using backend.DTOs.CustomerTierDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class CustomerTierProfile : Profile
    {
        public CustomerTierProfile() 
        {
            // GET
            CreateMap<CustomerTier, CustomerTierReadDto>();

            // POST
            CreateMap<CustomerTierCreateDto, CustomerTier>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<CustomerTierUpdateDto, CustomerTier>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
