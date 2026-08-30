using AutoMapper;
using backend.DTOs.AiDTOs;
using backend.Models;
using System.Linq;
using System.Text.Json;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Module 15: AI Chatbot Assistant.
    /// Chuẩn hóa chuyển đổi giữa các thực thể ChatSession, ChatMessage, OrderDetail, Product/Variant và các DTOs giao tiếp với Frontend.
    /// </summary>
    public class AiChatProfile : Profile
    {
        public AiChatProfile()
        {
            #region ChatSession -> ChatSessionReadDto
            CreateMap<ChatSession, ChatSessionReadDto>()
                .ForMember(dest => dest.TotalMessages, opt => opt.MapFrom(src => src.Messages.Count))
                .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => 
                    src.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault()));
            #endregion

            #region ChatMessage -> ChatMessageReadDto
            CreateMap<ChatMessage, ChatMessageReadDto>()
                .ForMember(dest => dest.Payload, opt => opt.MapFrom(src => 
                    string.IsNullOrEmpty(src.PayloadJson) ? null : JsonSerializer.Deserialize<object>(src.PayloadJson, (JsonSerializerOptions?)null)));
            #endregion

            #region OrderDetail -> InteractiveOrderItemDto
            CreateMap<OrderDetail, InteractiveOrderItemDto>()
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : "SP"))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : "Nông sản sạch Solaris"))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Slug : "san-pham"))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => 
                    src.Variant != null && src.Variant.ImagePath != null ? src.Variant.ImagePath : (src.Variant != null && src.Variant.Product != null ? src.Variant.Product.ImagePath : null)))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : "Kg"));
            #endregion

            #region Product / ProductVariant -> AiProductCardDto
            CreateMap<Product, AiProductCardDto>()
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => 
                    src.Variants.Where(v => !v.IsDeleted && v.IsActive).Select(v => v.Id).FirstOrDefault() != 0 
                        ? src.Variants.Where(v => !v.IsDeleted && v.IsActive).Select(v => v.Id).FirstOrDefault() 
                        : src.Id))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug ?? "san-pham"))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Name : "Kg"))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => 
                    src.Variants.Where(v => !v.IsDeleted && v.IsActive)
                        .SelectMany(v => v.Prices.Where(p => !p.IsDeleted))
                        .Select(p => p.Price)
                        .FirstOrDefault() != 0 
                            ? src.Variants.Where(v => !v.IsDeleted && v.IsActive)
                                .SelectMany(v => v.Prices.Where(p => !p.IsDeleted))
                                .Select(p => p.Price)
                                .FirstOrDefault() 
                            : 60000m))
                .ForMember(dest => dest.DiscountedPrice, opt => opt.MapFrom(src => 
                    src.Variants.Where(v => !v.IsDeleted && v.IsActive)
                        .SelectMany(v => v.Prices.Where(p => !p.IsDeleted))
                        .Select(p => p.Price)
                        .FirstOrDefault() != 0 
                            ? src.Variants.Where(v => !v.IsDeleted && v.IsActive)
                                .SelectMany(v => v.Prices.Where(p => !p.IsDeleted))
                                .Select(p => p.Price)
                                .FirstOrDefault() 
                            : 60000m))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => "Đà Lạt, Lâm Đồng"))
                .ForMember(dest => dest.Certification, opt => opt.MapFrom(src => "VietGAP"))
                .ForMember(dest => dest.BrixLevel, opt => opt.MapFrom(src => "15°Bx"))
                .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => true));
            #endregion
        }
    }
}
