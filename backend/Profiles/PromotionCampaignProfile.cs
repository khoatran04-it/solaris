using AutoMapper;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class PromotionCampaignProfile : Profile
    {
        public PromotionCampaignProfile()
        {
            // Map Create/Update
            CreateMap<PromotionCampaignCreateDto, PromotionCampaign>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore());

            CreateMap<PromotionCampaignUpdateDto, PromotionCampaign>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.PromotionVariants, opt => opt.Ignore());

            // Map Read
            CreateMap<PromotionCampaign, PromotionCampaignReadDto>()
                .ForMember(dest => dest.AppliedVariants, opt => opt.MapFrom(src => src.PromotionVariants));

            // Map Applied Variants
            CreateMap<PromotionVariant, CampaignAppliedVariantDto>()
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : string.Empty))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.ImagePath : string.Empty))

                // Trích xuất Giá của quy cách IsDefault = true (Nếu ko có thì lấy dòng đầu tiên)
                .ForMember(dest => dest.DefaultPrice, opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Prices.Any(p => p.IsDefault)
                        ? src.Variant.Prices.First(p => p.IsDefault).Price
                        : (src.Variant != null && src.Variant.Prices.Any() ? src.Variant.Prices.First().Price : 0)))

                // Trích xuất Tên Đơn vị tính (VD: Kg, Thùng...)
                .ForMember(dest => dest.DefaultUoMName, opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Prices.Any(p => p.IsDefault) && src.Variant.Prices.First(p => p.IsDefault).UoM != null
                        ? src.Variant.Prices.First(p => p.IsDefault).UoM!.Name
                        : (src.Variant != null && src.Variant.Prices.Any() && src.Variant.Prices.First().UoM != null ? src.Variant.Prices.First().UoM!.Name : null)));
        }
    }
}