using AutoMapper;
using backend.DTOs.SupplierTypeDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Loại Nhà Cung Cấp (SupplierType).
    /// Tự động chuẩn hóa chuỗi dữ liệu (Trim, ToUpper Code) theo nguyên lý Defense-in-Depth.
    /// </summary>
    public class SupplierTypeProfile : Profile
    {
        public SupplierTypeProfile()
        {
            #region Entity -> Read DTO
            CreateMap<SupplierType, SupplierTypeReadDto>();
            #endregion

            #region Create DTO -> Entity
            CreateMap<SupplierTypeCreateDto, SupplierType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Description) ? null : src.Description.Trim()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Suppliers, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<SupplierTypeUpdateDto, SupplierType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Description) ? null : src.Description.Trim()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Suppliers, opt => opt.Ignore());
            #endregion
        }
    }
}