using AutoMapper;
using backend.DTOs.CustomerDTOs;
using backend.Models;
using System.Collections.Generic;
using System.Linq;

namespace backend.Profiles
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            // --- GET: Từ Model sang DTO ---
            CreateMap<Customer, CustomerReadDto>()
                // 1. Ánh xạ Tên Phân Loại
                .ForMember(dest => dest.CustomerTypeName, opt =>
                    opt.MapFrom(src => src.CustomerType != null ? src.CustomerType.Name : null))

                // 2. Ánh xạ Tên Bậc Hạng
                .ForMember(dest => dest.CustomerTierName, opt =>
                    opt.MapFrom(src => src.CustomerTier != null ? src.CustomerTier.Name : null))

                // 3. Ánh xạ mảng TÊN NHÓM (Dùng cho UI danh sách: string[])
                .ForMember(dest => dest.Groups, opt =>
                    opt.MapFrom(src => src.GroupLinks != null
                        // Lọc bỏ những liên kết bị lỗi (Group bị null) rồi mới lấy Name
                        ? src.GroupLinks
                            .Where(gl => gl.CustomerGroup != null)
                            .Select(gl => gl.CustomerGroup!.Name)
                            .ToList()
                        : new List<string>()))

                // 4. Giữ lại mảng ID NHÓM (Dùng khi UI gọi API GetById để bind vào form Edit: int[])
                .ForMember(dest => dest.GroupIds, opt =>
                    opt.MapFrom(src => src.GroupLinks != null
                        ? src.GroupLinks.Select(gl => gl.CustomerGroupId).ToList()
                        : new List<int>()));

            // --- POST: Từ DTO Create sang Model ---
            CreateMap<CustomerCreateDto, Customer>()
                // Bỏ qua GroupLinks vì chúng ta đã tự xử lý Insert thủ công trong CustomerService
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());

            // --- PUT: Từ DTO Update sang Model ---
            CreateMap<CustomerUpdateDto, Customer>()
                // Bỏ qua GroupLinks vì chúng ta đã tự xử lý Xóa/Thêm thủ công trong CustomerService
                .ForMember(dest => dest.GroupLinks, opt => opt.Ignore());
        }
    }
}