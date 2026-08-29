using AutoMapper;
using backend.DTOs.ProductVariantDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Biến Thể Sản Phẩm (SKU) và các đối tượng liên quan (Giá, Thuộc tính).
    /// </summary>
    public class ProductVariantProfile : Profile
    {
        public ProductVariantProfile()
        {
            #region Entity -> Read DTO (Dữ liệu đầu ra)
            // 1. Ánh xạ Thuộc tính EAV của biến thể
            CreateMap<ProductAttribute, VariantAttributeDto>()
                .ForMember(dest => dest.AttributeDefinitionName, opt => opt.MapFrom(src => src.AttributeDefinition != null ? src.AttributeDefinition.Name : null));

            // 2. Ánh xạ Bảng giá theo ĐVT của biến thể
            CreateMap<ProductVariantPrice, VariantPriceReadDto>()
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : null))
                // PromotionalPrice sẽ được tầng Service tính toán riêng (dựa trên các Promotion đang chạy), không map trực tiếp từ DB
                .ForMember(dest => dest.PromotionalPrice, opt => opt.Ignore());

            // 3. Ánh xạ Biến thể tổng (gộp Giá và Thuộc tính)
            CreateMap<ProductVariant, ProductVariantReadDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null));
            #endregion

            #region DTO Đầu vào -> Entity (Dữ liệu thành phần)
            // Cấu hình map DTO con sang Model để EF Core có thể tự động Insert
            CreateMap<AttributeInputDto, ProductAttribute>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.VariantId, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeDefinition, opt => opt.Ignore());

            CreateMap<VariantPriceInputDto, ProductVariantPrice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.VariantId, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                // IsActive cho giá luôn set mặc định là true khi tạo mới
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<ProductVariantCreateDto, ProductVariant>()
                // Bỏ qua ID và các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                // Bỏ qua Navigation Properties bên ngoài
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.Batches, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierProducts, opt => opt.Ignore())
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore());
            // LƯU Ý KHI CREATE: Cố tình KHÔNG ignore Attributes và Prices để EF Core thực hiện Cascade Insert (Thêm biến thể + thêm giá + thêm thuộc tính trong cùng 1 Transaction ngầm).
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<ProductVariantUpdateDto, ProductVariant>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.Batches, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierProducts, opt => opt.Ignore())
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore())
                // LƯU Ý KHI UPDATE: Cực kỳ quan trọng -> Phải Ignore Collections để tầng Service tự cập nhật (Xóa cũ - Thêm mới) nhằm tránh lỗi Tracking Identity của EF Core.
                .ForMember(dest => dest.Attributes, opt => opt.Ignore())
                .ForMember(dest => dest.Prices, opt => opt.Ignore());
            #endregion
        }
    }
}