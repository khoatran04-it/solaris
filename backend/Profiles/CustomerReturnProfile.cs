using AutoMapper;
using backend.DTOs.CustomerReturnDTOs;
using backend.Models;
using backend.Models.Enums;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper cho phân hệ Trả hàng & Hoàn tiền (Customer Return / RMA).
    /// Đóng vai trò chuyển đổi dữ liệu giữa Database Entity và DTO, hỗ trợ đắc lực 
    /// việc tối ưu hiệu năng hiển thị (Làm phẳng dữ liệu) cho màn hình của Kế toán và Thủ kho.
    /// </summary>
    public class CustomerReturnProfile : Profile
    {
        public CustomerReturnProfile()
        {
            #region 1. Map Hiển thị Tổng quan Phiếu Trả Hàng (RMA Header Read DTO)
            /// <summary>
            /// Ánh xạ từ Thực thể sang DTO hiển thị danh sách hoặc chi tiết phiếu trả hàng.
            /// NGHIỆP VỤ LÀM PHẲNG (FLATTENING): Tự động trích xuất các thông tin đối soát cực kỳ quan trọng 
            /// (Mã đơn bán gốc, Tên khách hàng, Kho tiếp nhận, Nhân viên QC) lên cùng một mặt phẳng dữ liệu. 
            /// Đảm bảo Frontend render nhanh chóng, không dính lỗi N+1 Query.
            /// </summary>
            CreateMap<CustomerReturn, CustomerReturnReadDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedBy != null ? src.ReceivedBy.FullName : null))
                .ForMember(dest => dest.ReturnTypeName, opt => opt.MapFrom(src => src.ReturnType == CustomerReturnType.DoorstepRefusal ? "Thu hồi trực tiếp khi giao" : "Thu hồi tại nhà khách"));
            #endregion

            #region 2. Map Khởi tạo Phiếu Trả Hàng (RMA Create Command)
            /// <summary>
            /// Ánh xạ dữ liệu yêu cầu tạo phiếu hoàn trả từ Frontend xuống Entity.
            /// Phục vụ cho BƯỚC 1: Tiếp nhận yêu cầu (Status = Pending), lúc này chưa diễn ra thao tác cộng/trừ Tồn kho.
            /// </summary>
            CreateMap<CustomerReturnCreateDto, CustomerReturn>();
            #endregion

            #region 3. Map Chi tiết Phiếu Trả Hàng (RMA Detail Read DTO)
            /// <summary>
            /// Ánh xạ dòng chi tiết các mặt hàng trả lại để hiển thị cho đối soát.
            /// NGHIỆP VỤ TRUY VẾT: Làm phẳng BatchCode (Mã Lô). Dữ liệu sống còn để Kế toán và Quản lý 
            /// nhìn vào biết ngay lô hàng nào đang bị khách hàng "chê" nhiều nhất để làm việc lại với Nhà cung cấp.
            /// </summary>
            CreateMap<CustomerReturnDetail, CustomerReturnDetailReadDto>()
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
            #endregion

            #region 4. Map Khởi tạo Chi tiết mặt hàng (Detail Create Command)
            /// <summary>
            /// Ánh xạ dữ liệu khởi tạo cho từng dòng sản phẩm khách yêu cầu trả lại.
            /// </summary>
            CreateMap<CustomerReturnDetailCreateDto, CustomerReturnDetail>();
            #endregion
        }
    }
}