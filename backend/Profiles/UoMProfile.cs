using AutoMapper;
using backend.DTOs.UoMDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Đơn vị tính (UoM).
    /// </summary>
    public class UoMProfile : Profile
    {
        public UoMProfile()
        {
            #region Entity -> Read DTO
            CreateMap<UoM, UoMReadDto>()
                .ForMember(dest => dest.CategoryCode, opt => opt.MapFrom(src => src.Category != null ? src.Category.Code : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<UoMCreateDto, UoM>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Synonyms, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Synonyms) ? null : src.Synonyms.Trim()));
            #endregion

            #region Update DTO -> Entity
            CreateMap<UoMUpdateDto, UoM>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Synonyms, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Synonyms) ? null : src.Synonyms.Trim()));
            #endregion
        }
    }
}