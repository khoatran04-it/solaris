using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Models;
using System.Linq;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Phân hệ Giỏ Hàng Mua Sắm (Module 12 - Shopping Cart).
    /// Hỗ trợ làm phẳng (Flattening) các thuộc tính liên kết từ Product, Variant, UoM và AttributeDefinition sang DTO.
    /// </summary>
    public class ShopCartProfile : Profile
    {
        public ShopCartProfile()
        {
            #region 1. ShoppingCartItem -> ShopCartItemDto
            CreateMap<ShoppingCartItem, ShopCartItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.ProductSlug, opt => opt.MapFrom(src => src.Variant != null && src.Variant.Product != null ? src.Variant.Product.Slug : null))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.Variant != null ? (src.Variant.ImagePath ?? (src.Variant.Product != null ? src.Variant.Product.ImagePath : null)) : null))
                .ForMember(dest => dest.UoMId, opt => opt.MapFrom(src => src.UoMId))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Attributes != null
                        ? src.Variant.Attributes
                            .Where(a => a.AttributeDefinition != null &&
                                       (a.AttributeDefinition.Name.ToLower().Contains("xuất xứ") || a.AttributeDefinition.Name.ToLower().Contains("vùng trồng")))
                            .Select(a => a.AttributeValue)
                            .FirstOrDefault()
                        : null))
                // Các trường động (Dynamic calculated properties) được tính toán theo thời gian thực tại Service
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.DiscountAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.AvailableStock, opt => opt.Ignore())
                .ForMember(dest => dest.IsOutOfStock, opt => opt.Ignore());
            #endregion

            #region 2. ShoppingCart -> ShopCartDto
            CreateMap<ShoppingCart, ShopCartDto>()
                .ForMember(dest => dest.CartId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TotalDiscount, opt => opt.Ignore())
                .ForMember(dest => dest.EstimatedTotal, opt => opt.Ignore());
            #endregion

            #region 3. ShopCartAddDto -> ShoppingCartItem
            CreateMap<ShopCartAddDto, ShoppingCartItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CartId, opt => opt.Ignore())
                .ForMember(dest => dest.Cart, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            #endregion

            #region 4. ShopGuestCartItemDto -> ShoppingCartItem
            CreateMap<ShopGuestCartItemDto, ShoppingCartItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CartId, opt => opt.Ignore())
                .ForMember(dest => dest.Cart, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore())
                .ForMember(dest => dest.AddedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            #endregion
        }
    }
}
