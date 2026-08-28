using AutoMapper;
using backend.DTOs.CategoryAttributeDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Cấu hình Thuộc tính Danh mục (Category Attribute / EAV Template).
    /// </summary>
    public class CategoryAttributeProfile : Profile
    {
        public CategoryAttributeProfile()
        {
            #region Entity -> Read DTO
            CreateMap<CategoryAttribute, CategoryAttributeReadDto>()
                // Lấy tên hiển thị từ các bảng liên kết (Enriched Properties)
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.AttributeDefinitionName, opt => opt.MapFrom(src => src.AttributeDefinition != null ? src.AttributeDefinition.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<CategoryAttributeCreateDto, CategoryAttribute>()
                // Bỏ qua ID (tự tăng) và các đối tượng liên kết (Navigation Properties)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeDefinition, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<CategoryAttributeUpdateDto, CategoryAttribute>()
                // Bảo vệ ID và các đối tượng liên kết khỏi việc bị ghi đè (Over-posting)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeDefinition, opt => opt.Ignore());
            #endregion
        }
    }
}