using AutoMapper;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile()
        {
            //GET
            CreateMap<ProductCategory, ProductCategoryReadDto>();

            //POST
            CreateMap<ProductCategoryCreateDto, ProductCategory>();

            //PUT
            CreateMap<ProductCategoryUpdateDto, ProductCategory>();

        }
    }
}
