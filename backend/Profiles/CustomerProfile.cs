using AutoMapper;
using backend.DTOs.CustomerDTOs;
using backend.Models;
using System.Linq;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho trung tâm Khách Hàng (Customer).
    /// Xử lý các logic ánh xạ phức tạp như bóc tách Tên hạng, Phân loại, Nhóm (Tags) và Sổ địa chỉ.
    /// </summary>
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            #region Entity -> Read DTO (Truy vấn)
            // Ánh xạ dữ liệu từ Database ra DTO.
            CreateMap<Customer, CustomerReadDto>()
                // 1. Ánh xạ Tên Phân Loại (Lấy từ đối tượng CustomerType)
                .ForMember(dest => dest.CustomerTypeName, opt =>
                    opt.MapFrom(src => src.CustomerType != null ? src.CustomerType.Name : null))

                // 2. Ánh xạ Tên Bậc Hạng (Lấy từ đối tượng CustomerTier)
                .ForMember(dest => dest.CustomerTierName, opt =>
                    opt.MapFrom(src => src.CustomerTier != null ? src.CustomerTier.Name : null))

                // 3. Flatten danh sách TÊN NHÓM (Dùng để hiển thị các Tags trên giao diện danh sách)
                .ForMember(dest => dest.Groups, opt =>
                    opt.MapFrom(src => src.GroupLinks != null
                        ? src.GroupLinks
                            .Where(gl => !gl.IsDeleted && gl.CustomerGroup != null)
                            .Select(gl => gl.CustomerGroup!.Name)
                            .ToList()
                        : new List<string>()))

                // 4. Flatten danh sách ID NHÓM (Dùng khi gọi API GetById để bind dữ liệu vào Multi-Select form Edit)
                .ForMember(dest => dest.GroupIds, opt =>
                    opt.MapFrom(src => src.GroupLinks != null
                        ? src.GroupLinks.Where(gl => !gl.IsDeleted).Select(gl => gl.CustomerGroupId).ToList()
                        : new List<int>()))

                // 5. Ánh xạ Sổ địa chỉ (AutoMapper sẽ tự động gọi tiếp CustomerAddressProfile)
                .ForMember(dest => dest.Addresses, opt =>
                    opt.MapFrom(src => src.Addresses != null
                        ? src.Addresses.Where(a => !a.IsDeleted).ToList()
                        : new List<CustomerAddress>()));
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<CustomerCreateDto, Customer>()
                // Bỏ qua các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                // Bỏ qua thông tin Auth (Sẽ được xử lý riêng trong module Authentication)
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                // Bỏ qua Navigation Properties - Bắt buộc bỏ qua để tầng Service tự xử lý thủ công,
                // ngăn chặn Entity Framework Core phát sinh lỗi Tracking vòng lặp
                .ForMember(dest => dest.CustomerType, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerTier, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<CustomerUpdateDto, Customer>()
                // Bảo vệ các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                // Bảo vệ thông tin Auth và mã Khách hàng (Thường không cho đổi mã tự sinh)
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                // Bỏ qua Navigation Properties để tầng Service xử lý xóa cũ/thêm mới
                .ForMember(dest => dest.CustomerType, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerTier, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());
            #endregion
        }
    }
}