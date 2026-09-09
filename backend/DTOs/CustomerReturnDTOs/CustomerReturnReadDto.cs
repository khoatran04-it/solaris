using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.CustomerReturnDTOs
{
    /// <summary>
    /// DTO Hiển thị Tổng quan Phiếu Khách Hàng Trả Hàng (RMA Read View).
    /// Dữ liệu đã được "Làm phẳng" (Flatten) từ các bảng liên kết (Order, Customer, Warehouse, User) 
    /// để tối ưu hóa hiệu năng render danh sách trên giao diện Quản lý kho và Kế toán, 
    /// loại bỏ hoàn toàn vấn đề N+1 Query.
    /// </summary>
    public class CustomerReturnReadDto
    {
        #region Định danh & Phân loại
        public int Id { get; set; }

        /// <summary>Mã phiếu trả hàng (Ví dụ: RET-20260817-001).</summary>
        public string ReturnCode { get; set; } = string.Empty;

        /// <summary>
        /// Hình thức hoàn trả: 1 = DoorstepRefusal (Thu hồi trực tiếp khi giao), 2 = PostDeliveryReturn (Thu hồi tại nhà khách)
        /// </summary>
        public CustomerReturnType ReturnType { get; set; } = CustomerReturnType.PostDeliveryReturn;
        public string ReturnTypeName { get; set; } = "Thu hồi tại nhà khách";

        /// <summary>ID chuyến xe thu hồi (nếu có).</summary>
        public int? DeliveryTripId { get; set; }
        #endregion

        #region Đối soát Chứng từ & Khách hàng (Flattened)
        public int OrderId { get; set; }

        /// <summary>Mã Đơn bán hàng gốc bị hoàn trả, giúp Admin bấm vào để mở trực tiếp hóa đơn cũ.</summary>
        public string OrderCode { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        #endregion

        #region Địa điểm & Nhân sự QC (Flattened)
        public int WarehouseId { get; set; }

        /// <summary>Tên Kho tiếp nhận hàng hoàn vật lý.</summary>
        public string WarehouseName { get; set; } = string.Empty;

        public int? ReceivedById { get; set; }

        /// <summary>Tên Thủ kho hoặc Nhân viên QC trực tiếp kiểm định lô hàng này.</summary>
        public string? ReceivedByName { get; set; }
        #endregion

        #region Vận hành & QC (Workflow)
        /// <summary>Ngày tiếp nhận yêu cầu hoặc ngày hàng về tới kho.</summary>
        public DateTime ReturnDate { get; set; }

        /// <summary>
        /// Trạng thái của phiếu trả hàng (Pending, Inspecting, Completed, Rejected).
        /// NGHIỆP VỤ LÕI: Khi trạng thái là Completed, Kế toán hiểu rằng khoản RefundAmount 
        /// đã chốt và Tồn kho đã được hệ thống tự động bù trừ xong.
        /// </summary>
        public CustomerReturnStatus Status { get; set; }
        #endregion

        #region Tài chính & Giải trình (Financials & Notes)
        /// <summary>Tổng tiền hoàn trả thực tế cho khách (VND) sau khi đã cấn trừ các khoản phí/hàng lỗi.</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Lý do khách hàng yêu cầu hoàn trả ban đầu.</summary>
        public string? Reason { get; set; }

        /// <summary>Biên bản ghi nhận tổng quan của bộ phận QC về lô hàng (Ví dụ: "Hàng về bị ẩm mốc").</summary>
        public string? InspectionNotes { get; set; }
        #endregion

        #region Thời gian
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết
        /// <summary>Danh sách chi tiết các mặt hàng trả lại cùng kết quả QC phân luồng tồn kho tương ứng.</summary>
        public List<CustomerReturnDetailReadDto> Details { get; set; } = new();
        #endregion
    }

    /// <summary>
    /// DTO Hiển thị Chi tiết mặt hàng hoàn trả và kết quả Kiểm định (QC).
    /// Cung cấp cái nhìn minh bạch cho cả Kế toán (Tính toán hoàn tiền) và Thủ kho (Phân bổ tồn kho).
    /// </summary>
    public class CustomerReturnDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Nguồn gốc (Traceability - Flattened)
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }

        /// <summary>
        /// Mã Lô hàng nông sản thực tế bị trả lại.
        /// Bằng chứng truy vết (Traceability) để đánh giá chất lượng nhà cung cấp hoặc xử lý thu hồi lô diện rộng.
        /// </summary>
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Kết quả Kiểm định Kho (QC Results)
        /// <summary>Tổng số lượng khách mang trả theo Đơn vị tính.</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>Số lượng nguyên vẹn (Đã được hệ thống tự động cộng lại vào Hàng Khả dụng - Available).</summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>Số lượng móp méo/hỏng (Đã tự động chuyển vào Hàng Hỏng - Damaged, chờ xuất hủy / Write-off).</summary>
        public decimal DamagedQuantity { get; set; }
        #endregion

        #region Tài chính (Financials)
        /// <summary>Đơn giá làm cơ sở tính hoàn tiền (Lấy cứng từ Đơn hàng gốc - Snapshot).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Số tiền hoàn trả cho khách trên riêng dòng sản phẩm này.</summary>
        public decimal RefundAmount { get; set; }
        #endregion

        #region Giải trình
        /// <summary>Lý do từ chối hoàn tiền toàn bộ/một phần hoặc ghi chú tình trạng lỗi cụ thể.</summary>
        public string? RejectReason { get; set; }
        #endregion
    }
}