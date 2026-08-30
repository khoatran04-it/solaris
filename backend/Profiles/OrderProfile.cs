using AutoMapper;
using backend.DTOs.OrderDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper cho phân hệ Quản lý Đơn Bán Hàng (Sales Order).
    /// Đóng vai trò là "Trình phiên dịch" giữa tầng Database Entity và tầng Giao diện (DTO).
    /// </summary>
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            #region 1. Map Hiển thị Tổng quan Đơn Hàng (Read DTO)
            /// <summary>
            /// Ánh xạ từ Thực thể sang DTO hiển thị danh sách / chi tiết cho Admin.
            /// NGHIỆP VỤ LÀM PHẲNG (FLATTENING): Tự động kéo Tên khách hàng, Số điện thoại và Tên Kho xuất 
            /// lên cùng một mặt phẳng dữ liệu. Giúp Frontend render DataGrid siêu mượt mà không cần gọi API phụ (Tránh N+1 Query).
            /// </summary>
            CreateMap<Order, OrderReadDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.PhoneNumber : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : null))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.Details != null ? src.Details.Sum(d => d.DiscountAmount) : 0))
                .ForMember(dest => dest.IssuedItems, opt => opt.Ignore());
            #endregion

            #region 2. Map Thao tác Khởi tạo & Cập nhật (Create & Update Command)
            /// <summary>
            /// Ánh xạ dữ liệu khởi tạo đơn hàng từ Frontend bơm xuống Entity để lưu vào DB.
            /// (Các trường như Status, TotalAmount sẽ được IOrderService tự động tính toán sau khi map).
            /// </summary>
            CreateMap<OrderCreateDto, Order>();

            /// <summary>
            /// Ánh xạ dữ liệu cập nhật đơn hàng.
            /// NGHIỆP VỤ CẬP NHẬT TỪNG PHẦN (PARTIAL UPDATE / HTTP PATCH):
            /// Dòng code 'opts.Condition(...)' cực kỳ đắt giá. Nó ra lệnh cho AutoMapper: 
            /// "Chỉ ghi đè những trường có dữ liệu (khác null). Nếu trường nào Frontend truyền lên là null, 
            /// hãy giữ nguyên giá trị cũ trong Database". Giúp tránh việc lỡ tay xóa mất dữ liệu cũ của đơn hàng.
            /// </summary>
            CreateMap<OrderUpdateDto, Order>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            #endregion

            #region 3. Map Chi tiết Đơn Hàng (Order Detail)
            /// <summary>
            /// Ánh xạ các dòng sản phẩm trong đơn hàng để hiển thị.
            /// Làm phẳng Tên SP, Mã SP và Đơn vị tính để hóa đơn hiển thị đầy đủ thông tin nhất cho kế toán/kho đối chiếu.
            /// </summary>
            CreateMap<OrderDetail, OrderDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));

            /// <summary>
            /// Ánh xạ dòng chi tiết sản phẩm khi khách hàng đang tiến hành Checkout.
            /// </summary>
            CreateMap<OrderDetailCreateDto, OrderDetail>();
            #endregion
        }
    }
}