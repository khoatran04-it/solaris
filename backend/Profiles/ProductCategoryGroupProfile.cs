using AutoMapper;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductCategoryGroupProfile : Profile
    {
        public ProductCategoryGroupProfile() 
        {
            //GET
            CreateMap<ProductCategoryGroup, ProductCategoryGroupReadDto>();

            //POST
            CreateMap<ProductCategoryGroupCreateDto, ProductCategoryGroup>();

            //PUT
            CreateMap<ProductCategoryGroupUpdateDto, ProductCategoryGroup>();

        }
    }
}
