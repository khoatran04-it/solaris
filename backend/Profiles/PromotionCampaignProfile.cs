using System.Linq;
using AutoMapper;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Chiến Dịch Khuyến Mãi (Promotion Campaign).
    /// Hỗ trợ bóc tách, Flatten (làm phẳng) dữ liệu Giá bán và Đơn vị tính từ các tầng liên kết sâu.
    /// </summary>
    public class PromotionCampaignProfile : Profile
    {
        public PromotionCampaignProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            // 1. Ánh xạ dữ liệu Chiến dịch
            CreateMap<PromotionCampaign, PromotionCampaignReadDto>()
                .ForMember(dest => dest.AppliedVariants, opt => opt.MapFrom(src => src.PromotionVariants));

            // 2. Ánh xạ dữ liệu Phụ trợ: Bảng nối (PromotionVariant) -> DTO hiển thị danh sách sản phẩm Sale
            CreateMap<PromotionVariant, CampaignAppliedVariantDto>()
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : null))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.ImagePath : null))
                .ForMember(dest => dest.DefaultPrice, opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Prices != null
                        ? src.Variant.Prices
                            .OrderByDescending(p => p.IsDefault)
                            .Select(p => p.Price)
                            .FirstOrDefault()
                        : 0))
                .ForMember(dest => dest.DefaultUoMName, opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Prices != null
                        ? src.Variant.Prices
                            .OrderByDescending(p => p.IsDefault)
                            .Select(p => p.UoM != null ? p.UoM.Name : null)
                            .FirstOrDefault()
                        : null));
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<PromotionCampaignCreateDto, PromotionCampaign>()
                // Bỏ qua các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                // Các trường xử lý riêng ở Service (Tạo Slug, Upload hình ảnh...)
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.BannerImagePath, opt => opt.Ignore())
                // Bỏ qua Navigation Property để tầng Service tự xử lý bảng trung gian
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<PromotionCampaignUpdateDto, PromotionCampaign>()
                // Bảo vệ chặt chẽ các trường hệ thống, chống lỗ hổng Over-posting
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.BannerImagePath, opt => opt.Ignore())
                // Danh sách sản phẩm được cập nhật qua 1 API riêng biệt (ApplyVariantsToCampaignDto)
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore());
            #endregion
        }
    }
}