using AutoMapper;
using backend.DTOs.InventoryIssueDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryIssueProfile : Profile
    {
        public InventoryIssueProfile()
        {
            CreateMap<InventoryIssue, InventoryIssueReadDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : null))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.IssuedByName, opt => opt.MapFrom(src => src.IssuedBy != null ? src.IssuedBy.FullName : string.Empty));

            CreateMap<InventoryIssueCreateDto, InventoryIssue>();

            CreateMap<InventoryIssueDetail, InventoryIssueDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            CreateMap<InventoryIssueDetailCreateDto, InventoryIssueDetail>();
        }
    }
}
