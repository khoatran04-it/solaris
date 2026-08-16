using AutoMapper;
using backend.DTOs.SupplierProductDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class SupplierProductProfile : Profile
    {
        public SupplierProductProfile()
        {
            //GET
            CreateMap<SupplierProduct, SupplierProductReadDto>();

            //POST
            CreateMap<SupplierProductCreateDto, SupplierProduct>();

            //PUT
            CreateMap<SupplierProductUpdateDto, SupplierProduct>();

        }
    }
}
