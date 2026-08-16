using AutoMapper;
using backend.DTOs.ProductBatchDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductBatchProfile : Profile
    {
        public ProductBatchProfile()
        {
            //GET
            CreateMap<ProductBatch, ProductBatchReadDto>();

            //POST
            CreateMap<ProductBatchCreateDto, ProductBatch>();

            //PUT
            CreateMap<ProductBatchUpdateDto, ProductBatch>();

        }
    }
}
