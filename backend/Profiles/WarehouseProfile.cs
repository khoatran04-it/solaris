using AutoMapper;
using backend.DTOs.InventoryDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class WarehouseProfile : Profile
    {
        public WarehouseProfile()
        {
            // ==========================================
            // 1. MAPPING CHO ADDRESS (Dùng chung)
            // ==========================================
            CreateMap<WarehouseAddressPayload, WarehouseAddress>();

            // ==========================================
            // 2. MAPPING KHI ĐỌC (READ DTO)
            // ==========================================
            CreateMap<Warehouse, WarehouseReadDto>()
                // Lấy tên Trưởng kho từ Navigation Property Manager
                .ForMember(dest => dest.ManagerName,
                           opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))

                // Trải phẳng (Flatten) object Address ra thành các thuộc tính độc lập của DTO
                .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Address != null ? src.Address.Province : string.Empty))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.Address != null ? src.Address.District : string.Empty))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Address != null ? src.Address.Ward : string.Empty))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.Address != null ? src.Address.StreetAddress : string.Empty))
                .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.Address != null ? src.Address.FullAddress : string.Empty))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : 0))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : 0));

            // ==========================================
            // 3. MAPPING KHI TẠO (CREATE)
            // ==========================================
            CreateMap<WarehouseCreateDto, Warehouse>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                // Bỏ qua AddressId vì Entity Framework sẽ tự động sinh khi lưu đồng thời cả 2 bảng
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                // Tự động map cái AddressPayload vào bảng WarehouseAddress
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));

            // ==========================================
            // 4. MAPPING KHI CẬP NHẬT (UPDATE)
            // ==========================================
            CreateMap<WarehouseUpdateDto, Warehouse>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                // Không cho phép sửa Id và Code (Mã kho là cố định)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())

                // Bỏ qua map tự động Address ở đây để tránh lỗi Tracking của EF Core
                .ForMember(dest => dest.Address, opt => opt.Ignore());
        }
    }
}