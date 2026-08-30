using AutoMapper;
using backend.DTOs.InventoryReceiptDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryReceiptProfile : Profile
    {
        public InventoryReceiptProfile()
        {
            // Receipt Read DTO Mapping
            CreateMap<InventoryReceipt, InventoryReceiptReadDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null));

            // Receipt Create DTO Mapping
            CreateMap<InventoryReceiptCreateDto, InventoryReceipt>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiptCode, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiptDate, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedById, opt => opt.Ignore())
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.ReceivedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

            // Detail Read DTO Mapping
            CreateMap<InventoryReceiptDetail, InventoryReceiptDetailReadDto>()
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            // Detail Create DTO Mapping
            CreateMap<InventoryReceiptDetailCreateDto, InventoryReceiptDetail>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryReceiptId, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryReceipt, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseOrderDetail, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Batch, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore());
        }
    }
}
