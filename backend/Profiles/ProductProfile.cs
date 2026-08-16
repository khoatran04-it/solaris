using AutoMapper;
using backend.Models;
using backend.DTOs.ProductDTOs;

namespace backend.Profiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            //GET
            CreateMap<Product, ProductReadDto>();

            //POST
            CreateMap<ProductCreateDto, Product>();

            //PUR
            CreateMap<ProductUpdateDto, Product>();
        }
    }
}
