using AutoMapper;
using backend.DTOs.CustomerGroupDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Nhóm Khách Hàng (Customer Group / Marketing Tag).
    /// </summary>
    public class CustomerGroupProfile : Profile
    {
        public CustomerGroupProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            // Ánh xạ dữ liệu từ Database ra DTO để trả về cho Client
            CreateMap<CustomerGroup, CustomerGroupReadDto>();
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<CustomerGroupCreateDto, CustomerGroup>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<CustomerGroupUpdateDto, CustomerGroup>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());
            #endregion
        }
    }
}