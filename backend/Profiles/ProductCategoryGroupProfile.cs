using AutoMapper;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Nhóm Ngành Hàng lớn (Product Category Group).
    /// </summary>
    public class ProductCategoryGroupProfile : Profile
    {
        public ProductCategoryGroupProfile()
        {
            #region Entity -> Read DTO
            CreateMap<ProductCategoryGroup, ProductCategoryGroupReadDto>();
            #endregion

            #region Create DTO -> Entity
            CreateMap<ProductCategoryGroupCreateDto, ProductCategoryGroup>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Categories, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<ProductCategoryGroupUpdateDto, ProductCategoryGroup>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Categories, opt => opt.Ignore());
            #endregion
        }
    }
}