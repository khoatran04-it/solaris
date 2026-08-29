using AutoMapper;
using backend.DTOs.ProductDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Sản Phẩm Cha (Product Master).
    /// </summary>
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            #region Entity -> Read DTO
            CreateMap<Product, ProductReadDto>()
                // Lấy tên hiển thị từ các bảng liên kết (Enriched Properties)
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.BaseUoMName, opt => opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.BaseUoM, opt => opt.Ignore())
                .ForMember(dest => dest.Variants, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.BaseUoM, opt => opt.Ignore())
                .ForMember(dest => dest.Variants, opt => opt.Ignore());
            #endregion
        }
    }
}