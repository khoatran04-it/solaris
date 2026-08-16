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

            // 🔥 BỔ SUNG: Map Bảng giá từ Model sang DTO
            CreateMap<ProductVariantPrice, VariantPriceReadDto>()
                .ForMember(dest => dest.UoMName, opt =>
                    opt.MapFrom(src => src.UoM != null ? src.UoM.Name : null))
                // Bỏ qua giá KM vì mình sẽ tự viết code tính toán ở Service
                .ForMember(dest => dest.PromotionalPrice, opt => opt.Ignore());

            // Map vỏ Variant
            CreateMap<ProductVariant, ProductVariantReadDto>()
                .ForMember(dest => dest.ProductName, opt =>
                    opt.MapFrom(src => src.Product != null ? src.Product.Name : null));
            // Note: Không cần Ignore PromotionalPrice ở vỏ Variant nữa vì ta đã xóa field đó khỏi ReadDto rồi.


            // ==========================================
            // 2. MAP CHO DỮ LIỆU ĐẦU VÀO (CREATE / UPDATE)
            // ==========================================

            // Dạy AutoMapper cách chuyển cái InputDto thành Model
            CreateMap<AttributeInputDto, ProductAttribute>();

            // 🔥 BỔ SUNG: Map Input Bảng giá thành Model
            CreateMap<VariantPriceInputDto, ProductVariantPrice>();

            // Lúc Create: Chấp nhận ánh xạ luôn mảng Attributes và Prices để EF Core tự Insert 1 cục bằng Transaction
            CreateMap<ProductVariantCreateDto, ProductVariant>();

            // Lúc Update: Cực kỳ quan trọng -> Phải Ignore mảng Attributes VÀ Prices
            // Vì logic update mảng của chúng ta ở Service là: Xóa sạch dòng cũ -> Insert dòng mới. 
            // Nếu để AutoMapper tự map nó sẽ gây lỗi conflict tracking của Entity Framework.
            CreateMap<ProductVariantUpdateDto, ProductVariant>()
                .ForMember(dest => dest.Attributes, opt => opt.Ignore())
                .ForMember(dest => dest.Prices, opt => opt.Ignore()); // 🔥 BỔ SUNG
        }
    }
}