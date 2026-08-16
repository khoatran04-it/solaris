using AutoMapper;
using backend.DTOs.UoMCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class UoMCategoryProfile : Profile
    {
        public UoMCategoryProfile() 
        {
            //GET
            CreateMap<UoMCategory, UoMCategoryReadDto>();

            //POST
            CreateMap<UoMCategoryCreateDto, UoMCategory>();

            //PUT
            CreateMap<UoMCategoryUpdateDto, UoMCategory>();
        }
    }
}
