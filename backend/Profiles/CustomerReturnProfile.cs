using AutoMapper;
using backend.DTOs.CustomerReturnDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class CustomerReturnProfile : Profile
    {
        public CustomerReturnProfile()
        {
            CreateMap<CustomerReturn, CustomerReturnReadDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null));

            CreateMap<CustomerReturnCreateDto, CustomerReturn>();

            CreateMap<CustomerReturnDetail, CustomerReturnDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            CreateMap<CustomerReturnDetailCreateDto, CustomerReturnDetail>();
        }
    }
}
