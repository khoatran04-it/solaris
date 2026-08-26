using AutoMapper;
using backend.DTOs.UoMConversionDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Quy tắc Quy đổi Đơn vị tính (UoMConversion).
    /// </summary>
    public class UoMConversionProfile : Profile
    {
        public UoMConversionProfile()
        {
            #region Entity -> Read DTO
            CreateMap<UoMConversion, UoMConversionReadDto>()
                // Thông tin Sản phẩm (nếu là quy đổi đặc thù)
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Code : null))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))

                // Thông tin ĐVT Nguồn (From UoM)
                .ForMember(dest => dest.FromUoMCode, opt => opt.MapFrom(src => src.FromUoM != null ? src.FromUoM.Code : string.Empty))
                .ForMember(dest => dest.FromUoMName, opt => opt.MapFrom(src => src.FromUoM != null ? src.FromUoM.Name : string.Empty))

                // Thông tin ĐVT Đích (To UoM)
                .ForMember(dest => dest.ToUoMCode, opt => opt.MapFrom(src => src.ToUoM != null ? src.ToUoM.Code : string.Empty))
                .ForMember(dest => dest.ToUoMName, opt => opt.MapFrom(src => src.ToUoM != null ? src.ToUoM.Name : string.Empty));
            #endregion

            #region Create DTO -> Entity
            CreateMap<UoMConversionCreateDto, UoMConversion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.FromUoM, opt => opt.Ignore())
                .ForMember(dest => dest.ToUoM, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<UoMConversionUpdateDto, UoMConversion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.FromUoM, opt => opt.Ignore())
                .ForMember(dest => dest.ToUoM, opt => opt.Ignore());
            #endregion
        }
    }
}