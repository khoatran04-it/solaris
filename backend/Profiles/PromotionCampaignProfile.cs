using AutoMapper;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class PromotionCampaignProfile : Profile
    {
        public PromotionCampaignProfile()
        {
            // Map Create/Update (Giữ nguyên)
            CreateMap<PromotionCampaignCreateDto, PromotionCampaign>();
            CreateMap<PromotionCampaignUpdateDto, PromotionCampaign>();

            // Map Read (Giữ nguyên)
            CreateMap<PromotionCampaign, PromotionCampaignReadDto>();

            // 🔥 SỬA CHỖ NÀY: Dạy AutoMapper cách moi Giá Mặc Định từ bảng ProductVariantPrice
            CreateMap<PromotionVariant, CampaignAppliedVariantDto>()
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Name : string.Empty))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.ImagePath : string.Empty))

                // Trích xuất Giá của quy cách IsDefault = true (Nếu ko có thì lấy dòng đầu tiên)
                .ForMember(dest => dest.DefaultPrice, opt => opt.MapFrom(src =>
                    src.Variant!.Prices.FirstOrDefault(p => p.IsDefault) != null
                        ? src.Variant.Prices.FirstOrDefault(p => p.IsDefault)!.Price
                        : (src.Variant.Prices.FirstOrDefault() != null ? src.Variant.Prices.FirstOrDefault()!.Price : 0)))

                // Trích xuất Tên Đơn vị tính (VD: Kg, Thùng...)
                .ForMember(dest => dest.DefaultUoMName, opt => opt.MapFrom(src =>
                    src.Variant!.Prices.FirstOrDefault(p => p.IsDefault) != null
                        ? src.Variant.Prices.FirstOrDefault(p => p.IsDefault)!.UoM!.Name
                        : (src.Variant.Prices.FirstOrDefault() != null ? src.Variant.Prices.FirstOrDefault()!.UoM!.Name : null)));
        }
    }
}