using AutoMapper;
using backend.Models;
using backend.DTOs.InventoryDTOs;

namespace backend.Profiles
{
    public class InventoryProfile : Profile
    {
        public InventoryProfile()
        {
            // ==========================================
            // MAPPING: WarehouseInventory -> InventoryReadDto
            // ==========================================
            CreateMap<WarehouseInventory, InventoryReadDto>()
                // 1. Flatten thông tin Kho hàng
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : ""))
                .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Code : ""))

                // 2. Flatten thông tin Biến thể (Variant)
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : ""))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : ""))

                // Ánh xạ Đơn vị tính (UoM) cơ sở từ Product gốc
                .ForMember(dest => dest.BaseUoMName, opt => opt.MapFrom(src =>
                    (src.Variant != null && src.Variant.Product != null && src.Variant.Product.BaseUoM != null)
                    ? src.Variant.Product.BaseUoM.Name : ""))

                // 3. Flatten thông tin Lô hàng & Cảnh báo date
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : ""))
                .ForMember(dest => dest.ManufactureDate, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.ManufactureDate : default))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.ExpiryDate : default))

                // 4. Flatten thông tin Nhà cung cấp từ Batch liên kết
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src =>
                    (src.Batch != null && src.Batch.Supplier != null)
                    ? src.Batch.Supplier.Name : ""));

            // Các cột Quantity (Available, Reserved, QC, Damaged) AutoMapper sẽ tự động map 
            // vì trùng tên chính xác 100% giữa Model và DTO.

            // Các cột tính toán (DaysToExpiry, TotalQuantity) DTO đã tự lo (get only), 
            // AutoMapper sẽ tự động bỏ qua, không gây lỗi.
        }
    }
}