using AutoMapper;
using backend.DTOs.ProductBatchDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Lô Hàng Nông Sản (Product Batch / Lot).
    /// Hỗ trợ làm phẳng dữ liệu Biến thể sản phẩm (Variant) và Nhà cung cấp (Supplier) để phục vụ truy xuất nguồn gốc.
    /// </summary>
    public class ProductBatchProfile : Profile
    {
        public ProductBatchProfile()
        {
            #region Lô hàng: Entity -> Read DTO (Truy vấn)
            CreateMap<ProductBatch, ProductBatchReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));
            #endregion

            #region Lô hàng: Create DTO -> Entity (Thêm mới)
            CreateMap<ProductBatchCreateDto, ProductBatch>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore());
            #endregion

            #region Lô hàng: Update DTO -> Entity (Cập nhật)
            CreateMap<ProductBatchUpdateDto, ProductBatch>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.VariantId, opt => opt.Ignore()) // Khóa cứng VariantId không cho phép sửa
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore());
            #endregion
        }
    }
}
