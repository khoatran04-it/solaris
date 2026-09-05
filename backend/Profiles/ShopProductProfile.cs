using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Helpers;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho phân hệ Cửa Hàng (Shop Product B2C/B2B).
    /// </summary>
    public class ShopProductProfile : Profile
    {
        public ShopProductProfile()
        {
            #region Promotion Campaign -> Shop Promotion Badge DTO
            CreateMap<PromotionCampaign, ShopPromotionBadgeDto>()
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Slug) ? src.Slug : SlugHelper.GenerateSlug(src.Name)));
            #endregion

            #region Category Group -> Shop Category Tree DTO
            CreateMap<ProductCategoryGroup, ShopCategoryTreeDto>()
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.GroupSlug, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Slug) ? src.Slug : SlugHelper.GenerateSlug(src.Name)))
                .ForMember(dest => dest.GroupImage, opt => opt.MapFrom(src => src.ImagePath))
                .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.Categories.Where(c => c.IsActive && !c.IsDeleted)));
            #endregion

            #region Category -> Shop Category Item DTO
            CreateMap<ProductCategory, ShopCategoryItemDto>()
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.CategorySlug, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Slug) ? src.Slug : SlugHelper.GenerateSlug(src.Name)))
                .ForMember(dest => dest.CategoryImage, opt => opt.MapFrom(src => src.ImagePath))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count(p => p.IsActive && !p.IsDeleted)));
            #endregion

            #region Promotion Campaign -> Shop Promotion Detail DTO
            CreateMap<PromotionCampaign, ShopPromotionDetailDto>()
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Slug) ? src.Slug : SlugHelper.GenerateSlug(src.Name)))
                .ForMember(dest => dest.Products, opt => opt.Ignore());
            #endregion
        }
    }
}
