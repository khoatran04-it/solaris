using AutoMapper;
using backend.DTOs.SupplierProductDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Bảng giá và Danh mục mặt hàng của Nhà cung cấp (SupplierProduct).
    /// </summary>
    public class SupplierProductProfile : Profile
    {
        public SupplierProductProfile()
        {
            #region Entity -> Read DTO
            CreateMap<SupplierProduct, SupplierProductReadDto>()
                // Thông tin Mở rộng (Enriched Properties) từ Biến thể sản phẩm
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : null))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : null))
                .ForMember(dest => dest.VariantImagePath, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.ImagePath : null))

                // Thông tin Mở rộng từ Nhà cung cấp
                .ForMember(dest => dest.SupplierCode, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Code : null))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))

                // Thông tin Mở rộng từ Đơn vị tính mua hàng
                .ForMember(dest => dest.PurchaseUoMName, opt => opt.MapFrom(src => src.PurchaseUoM != null ? src.PurchaseUoM.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<SupplierProductCreateDto, SupplierProduct>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierSKU, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SupplierSKU) ? null : src.SupplierSKU.Trim().ToUpper()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseUoM, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<SupplierProductUpdateDto, SupplierProduct>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierSKU, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SupplierSKU) ? null : src.SupplierSKU.Trim().ToUpper()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseUoM, opt => opt.Ignore());
            #endregion
        }
    }
}