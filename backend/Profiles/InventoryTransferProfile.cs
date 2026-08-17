using AutoMapper;
using backend.DTOs.InventoryTransferDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryTransferProfile : Profile
    {
        public InventoryTransferProfile()
        {
            CreateMap<InventoryTransfer, InventoryTransferReadDto>()
                .ForMember(dest => dest.FromWarehouseName, opt => opt.MapFrom(src => src.FromWarehouse != null ? src.FromWarehouse.Name : string.Empty))
                .ForMember(dest => dest.ToWarehouseName, opt => opt.MapFrom(src => src.ToWarehouse != null ? src.ToWarehouse.Name : string.Empty))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : null))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : string.Empty))
                .ForMember(dest => dest.DispatchedByName, opt => opt.MapFrom(src => src.DispatchedBy != null ? src.DispatchedBy.FullName : null))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null));

            CreateMap<InventoryTransferCreateDto, InventoryTransfer>();

            CreateMap<InventoryTransferDetail, InventoryTransferDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            CreateMap<InventoryTransferDetailCreateDto, InventoryTransferDetail>();
        }
    }
}
