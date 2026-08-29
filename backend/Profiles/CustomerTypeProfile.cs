using AutoMapper;
using backend.DTOs.CustomerTypeDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Phân loại khách hàng (Customer Type).
    /// </summary>
    public class CustomerTypeProfile : Profile
    {
        public CustomerTypeProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            // Ánh xạ dữ liệu từ Database ra DTO để trả về cho Client
            CreateMap<CustomerType, CustomerTypeReadDto>();
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<CustomerTypeCreateDto, CustomerType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Customers, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<CustomerTypeUpdateDto, CustomerType>()
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