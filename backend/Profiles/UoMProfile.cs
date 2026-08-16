using AutoMapper;
using backend.DTOs.UoMDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMProfile : Profile
    {
        public UoMProfile() 
        {
            //GET
            CreateMap<UoM, UoMReadDto>();

            //POST
            CreateMap<UoMCreateDto, UoM>();

            //PUT
            CreateMap<UoMUpdateDto, UoM>();
        }
    }
}
