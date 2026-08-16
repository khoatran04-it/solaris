using AutoMapper;
using backend.Models;
using backend.DTOs.InventoryDTOs;

namespace backend.Mappings
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
                .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Address!.Province))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.Address!.District))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Address!.Ward))
                .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.Address!.StreetAddress))
                .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.Address!.FullAddress))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address!.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address!.Longitude));

            // ==========================================
            // 3. MAPPING KHI TẠO (CREATE)
            // ==========================================
            CreateMap<WarehouseCreateDto, Warehouse>()
                // Bỏ qua AddressId vì Entity Framework sẽ tự động sinh khi lưu đồng thời cả 2 bảng
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                // Tự động map cái AddressPayload vào bảng WarehouseAddress
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));

            // ==========================================
            // 4. MAPPING KHI CẬP NHẬT (UPDATE)
            // ==========================================
            CreateMap<WarehouseUpdateDto, Warehouse>()
                // Không cho phép sửa Id và Code (Mã kho là cố định)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())

                // QUAN TRỌNG: Bỏ qua map tự động Address ở đây để tránh lỗi Tracking của EF Core
                // Chúng ta sẽ map thủ công cục Address trong Service để an toàn.
                .ForMember(dest => dest.Address, opt => opt.Ignore());
        }
    }
}