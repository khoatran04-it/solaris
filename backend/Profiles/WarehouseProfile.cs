using AutoMapper;
using backend.DTOs.InventoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Kho Hàng (Warehouse).
    /// Xử lý việc làm phẳng (Flatten) thông tin địa chỉ từ WarehouseAddress ra DTO 
    /// và ngăn chặn các lỗi Tracking của Entity Framework khi cập nhật dữ liệu.
    /// </summary>
    public class WarehouseProfile : Profile
    {
        public WarehouseProfile()
        {
            #region Payload -> Entity (Dùng chung)
            // Ánh xạ từ Payload địa chỉ sang Entity địa chỉ kho
            CreateMap<WarehouseAddressPayload, WarehouseAddress>()
                // Bảo vệ các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
            #endregion

            #region Entity -> Read DTO (Truy vấn)
            CreateMap<Warehouse, WarehouseReadDto>()
                // 1. Ánh xạ thông tin Trưởng kho từ Navigation Property
                .ForMember(dest => dest.ManagerName, opt =>
                    opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))

                // 2. Trải phẳng (Flatten) dữ liệu từ bảng WarehouseAddress ra các thuộc tính độc lập của DTO
                .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Address != null ? src.Address.Province : string.Empty))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.Address != null ? src.Address.District : string.Empty))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Address != null ? src.Address.Ward : string.Empty))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.Address != null ? src.Address.StreetAddress : string.Empty))
                .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.Address != null ? src.Address.FullAddress : string.Empty))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : 0))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : 0));
            #endregion

            #region Create DTO -> Entity (Thêm mới)
            CreateMap<WarehouseCreateDto, Warehouse>()
                // Bảo vệ các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())

                // Bỏ qua Navigation Properties chống lỗi vòng lặp
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())

                // Xử lý dữ liệu liên kết: Bỏ qua AddressId vì EF Core sẽ tự động sinh và gán khi lưu đồng thời cả 2 bảng
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                // Tự động ánh xạ Nested Object AddressPayload thành thực thể WarehouseAddress
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));
            #endregion

            #region Update DTO -> Entity (Cập nhật)
            CreateMap<WarehouseUpdateDto, Warehouse>()
                // Bảo vệ các trường hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())

                // Không cho phép sửa đổi Mã kho (Code)
                .ForMember(dest => dest.Code, opt => opt.Ignore())

                // Bỏ qua Navigation Properties
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.Address, opt => opt.Ignore());
            #endregion
        }
    }
}