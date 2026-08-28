using AutoMapper;
using backend.DTOs.SupplierAddressDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Địa chỉ kho / giao nhận của Nhà cung cấp.
    /// Tự động làm sạch chuỗi địa chỉ (Trim) trước khi lưu trữ vào cơ sở dữ liệu.
    /// </summary>
    public class SupplierAddressProfile : Profile
    {
        public SupplierAddressProfile()
        {
            #region Entity -> Read DTO
            CreateMap<SupplierAddress, SupplierAddressReadDto>();
            #endregion

            #region Create DTO -> Entity
            CreateMap<SupplierAddressCreateDto, SupplierAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ContactName, opt => opt.MapFrom(src => src.ContactName.Trim()))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone.Trim()))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.StreetAddress.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Trim()))
                .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Province.Trim()))
                .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<SupplierAddressUpdateDto, SupplierAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ContactName, opt => opt.MapFrom(src => src.ContactName.Trim()))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone.Trim()))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.StreetAddress.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Trim()))
                .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Province.Trim()))
                .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore());
            #endregion
        }
    }
}