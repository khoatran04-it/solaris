using AutoMapper;
using backend.DTOs.InventoryTransferDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryTransferProfile : Profile
    {
        public InventoryTransferProfile()
        {
            // Transfer Read DTO Mapping
            CreateMap<InventoryTransfer, InventoryTransferReadDto>()
                .ForMember(dest => dest.FromWarehouseName, opt => opt.MapFrom(src => src.FromWarehouse != null ? src.FromWarehouse.Name : string.Empty))
                .ForMember(dest => dest.ToWarehouseName, opt => opt.MapFrom(src => src.ToWarehouse != null ? src.ToWarehouse.Name : string.Empty))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : null))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : string.Empty))
                .ForMember(dest => dest.DispatchedByName, opt => opt.MapFrom(src => src.DispatchedBy != null ? src.DispatchedBy.FullName : null))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null));

            // Transfer Create DTO Mapping
            CreateMap<InventoryTransferCreateDto, InventoryTransfer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TransferCode, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.DispatchedDate, opt => opt.Ignore())
                .ForMember(dest => dest.DispatchedById, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedById, opt => opt.Ignore())
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())
                .ForMember(dest => dest.FromWarehouse, opt => opt.Ignore())
                .ForMember(dest => dest.ToWarehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.DispatchedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

            // Detail Read DTO Mapping
            CreateMap<InventoryTransferDetail, InventoryTransferDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            // Detail Create DTO Mapping
            CreateMap<InventoryTransferDetailCreateDto, InventoryTransferDetail>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryTransferId, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryTransfer, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Batch, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore());
        }
    }
}
