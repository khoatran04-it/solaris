using AutoMapper;
using backend.DTOs.SupplierDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Hồ sơ Nhà cung cấp (Supplier).
    /// Tự động chuẩn hóa chuỗi dữ liệu (Trim, ToUpper Code, ToLower Email) theo nguyên lý Defense-in-Depth.
    /// </summary>
    public class SupplierProfile : Profile
    {
        public SupplierProfile()
        {
            #region Entity -> Read DTO
            CreateMap<Supplier, SupplierReadDto>()
                .ForMember(dest => dest.SupplierTypeName, opt => opt.MapFrom(src => src.SupplierType != null ? src.SupplierType.Name : null));
            #endregion

            #region Create DTO -> Entity
            CreateMap<SupplierCreateDto, Supplier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone.Trim()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLower()))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.TaxCode) ? null : src.TaxCode.Trim()))
                .ForMember(dest => dest.Website, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Website) ? null : src.Website.Trim()))
                .ForMember(dest => dest.SocialLink, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SocialLink) ? null : src.SocialLink.Trim()))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.BankAccount) ? null : src.BankAccount.Trim()))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.BankName) ? null : src.BankName.Trim()))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Note) ? null : src.Note.Trim()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierType, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierProducts, opt => opt.Ignore())
                .ForMember(dest => dest.Batches, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<SupplierUpdateDto, Supplier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone.Trim()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLower()))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.TaxCode) ? null : src.TaxCode.Trim()))
                .ForMember(dest => dest.Website, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Website) ? null : src.Website.Trim()))
                .ForMember(dest => dest.SocialLink, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.SocialLink) ? null : src.SocialLink.Trim()))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.BankAccount) ? null : src.BankAccount.Trim()))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.BankName) ? null : src.BankName.Trim()))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Note) ? null : src.Note.Trim()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierType, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierProducts, opt => opt.Ignore())
                .ForMember(dest => dest.Batches, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore());
            #endregion
        }
    }
}