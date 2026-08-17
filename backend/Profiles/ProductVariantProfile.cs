using AutoMapper;
using backend.DTOs.ProductVariantDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class ProductVariantProfile : Profile
    {
        public ProductVariantProfile()
        {
            // ==========================================
            // 1. MAP CHO DỮ LIỆU ĐẦU RA (READ)
            // ==========================================

            // Map từ bảng trung gian ProductAttribute sang DTO con
            CreateMap<ProductAttribute, VariantAttributeDto>()
                .ForMember(dest => dest.AttributeDefinitionName, opt =>
                    opt.MapFrom(src => src.AttributeDefinition != null ? src.AttributeDefinition.Name : null));

            // Map Bảng giá từ Model sang DTO
            CreateMap<ProductVariantPrice, VariantPriceReadDto>()
                .ForMember(dest => dest.UoMName, opt =>
                    opt.MapFrom(src => src.UoM != null ? src.UoM.Name : null))
                .ForMember(dest => dest.PromotionalPrice, opt => opt.Ignore());

            // Map vỏ Variant
            CreateMap<ProductVariant, ProductVariantReadDto>()
                .ForMember(dest => dest.ProductName, opt =>
                    opt.MapFrom(src => src.Product != null ? src.Product.Name : null));

            // ==========================================
            // 2. MAP CHO DỮ LIỆU ĐẦU VÀO (CREATE / UPDATE)
            // ==========================================

            // Dạy AutoMapper cách chuyển cái InputDto thành Model
            CreateMap<AttributeInputDto, ProductAttribute>();

            // Map Input Bảng giá thành Model
            CreateMap<VariantPriceInputDto, ProductVariantPrice>();

            // Lúc Create: Chấp nhận ánh xạ luôn mảng Attributes và Prices để EF Core tự Insert 1 cục bằng Transaction
            CreateMap<ProductVariantCreateDto, ProductVariant>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // Lúc Update: Cực kỳ quan trọng -> Phải Ignore mảng Attributes VÀ Prices
            CreateMap<ProductVariantUpdateDto, ProductVariant>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Attributes, opt => opt.Ignore())
                .ForMember(dest => dest.Prices, opt => opt.Ignore());
        }
    }
}