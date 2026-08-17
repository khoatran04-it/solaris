using AutoMapper;
using backend.DTOs.InventoryReceiptDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class InventoryReceiptProfile : Profile
    {
        public InventoryReceiptProfile()
        {
            // Receipt
            CreateMap<InventoryReceipt, InventoryReceiptReadDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null));
            CreateMap<InventoryReceiptCreateDto, InventoryReceipt>();

            // Detail
            CreateMap<InventoryReceiptDetail, InventoryReceiptDetailReadDto>()
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
            CreateMap<InventoryReceiptDetailCreateDto, InventoryReceiptDetail>();
        }
    }
}
