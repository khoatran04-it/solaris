using AutoMapper;
using backend.DTOs.CustomerAddressDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Sổ Địa Chỉ Khách Hàng (Customer Address).
    /// </summary>
    public class CustomerAddressProfile : Profile
    {
        public CustomerAddressProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            // Ánh xạ dữ liệu từ Database ra DTO.
            // Thuộc tính FullAddress tự động được ánh xạ thành công nhờ Computed Property đã khai báo bên trong Model.
            CreateMap<CustomerAddress, CustomerAddressReadDto>();
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<CustomerAddressCreateDto, CustomerAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<CustomerAddressUpdateDto, CustomerAddress>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore());
            #endregion
        }
    }
}