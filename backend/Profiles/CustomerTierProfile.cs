using AutoMapper;
using backend.DTOs.CustomerTierDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Hạng / Bậc Khách Hàng (Customer Tier).
    /// </summary>
    public class CustomerTierProfile : Profile
    {
        public CustomerTierProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            CreateMap<CustomerTier, CustomerTierReadDto>();
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<CustomerTierCreateDto, CustomerTier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Customers, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<CustomerTierUpdateDto, CustomerTier>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Customers, opt => opt.Ignore());
            #endregion
        }
    }
}