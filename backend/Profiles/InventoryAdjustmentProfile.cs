using AutoMapper;
using backend.DTOs.InventoryAdjustmentDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryAdjustmentProfile : Profile
    {
        public InventoryAdjustmentProfile()
        {
            CreateMap<InventoryAdjustment, InventoryAdjustmentReadDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.AuditCode, opt => opt.MapFrom(src => src.Audit != null ? src.Audit.AuditCode : null))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : string.Empty))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details));

            CreateMap<InventoryAdjustmentDetail, InventoryAdjustmentDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.Batch != null ? (DateTime?)src.Batch.ExpiryDate : null))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
        }
    }
}
