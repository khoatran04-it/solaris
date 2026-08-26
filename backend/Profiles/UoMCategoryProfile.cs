using AutoMapper;
using backend.DTOs.UoMCategoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Nhóm Đơn vị tính (UoMCategory).
    /// </summary>
    public class UoMCategoryProfile : Profile
    {
        public UoMCategoryProfile()
        {
            #region Entity -> Read DTO
            CreateMap<UoMCategory, UoMCategoryReadDto>()
                .ForMember(dest => dest.BaseUoMCode, opt => opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Code : null))
                .ForMember(dest => dest.BaseUoMName, opt => opt.MapFrom(src => src.BaseUoM != null ? src.BaseUoM.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<UoMCategoryCreateDto, UoMCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BaseUoM, opt => opt.Ignore())
                .ForMember(dest => dest.UoMs, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));
            #endregion

            #region Update DTO -> Entity
            CreateMap<UoMCategoryUpdateDto, UoMCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BaseUoM, opt => opt.Ignore())
                .ForMember(dest => dest.UoMs, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));
            #endregion
        }
    }
}