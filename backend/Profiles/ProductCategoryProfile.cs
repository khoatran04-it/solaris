using AutoMapper;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public class ProductCategoryProfile : Profile
    {
        public ProductCategoryProfile()
        {
            #region Entity -> Read DTO
            CreateMap<ProductCategory, ProductCategoryReadDto>()
                // Lấy tên hiển thị từ bảng liên kết Nhóm ngành hàng (Enriched Property)
                .ForMember(dest => dest.CategoryGroupName, opt => opt.MapFrom(src => src.CategoryGroup != null ? src.CategoryGroup.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<ProductCategoryCreateDto, ProductCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryGroup, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeTemplates, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<ProductCategoryUpdateDto, ProductCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryGroup, opt => opt.Ignore())
                .ForMember(dest => dest.Products, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeTemplates, opt => opt.Ignore());
            #endregion
        }
    }
}