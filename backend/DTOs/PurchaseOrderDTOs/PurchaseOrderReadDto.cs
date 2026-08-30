using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.PurchaseOrderDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Đơn Đặt Mua Hàng (Purchase Order - PO).
    /// Đã được làm phẳng (Flatten) các thông tin liên kết như Tên Nhà cung cấp, Tên Nhân viên 
    /// để Frontend có thể hiển thị trực tiếp lên Giao diện mà không cần gọi API phụ.
    /// </summary>
    public class PurchaseOrderReadDto
    {
        public int Id { get; set; }

        #region Thông tin Chứng từ & Lịch trình
        /// <summary>Mã chứng từ (Ví dụ: PO-20260817-001).</summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>Ngày lập đơn đặt hàng.</summary>
        public DateTime OrderDate { get; set; }

        /// <summary>Ngày hẹn giao hàng dự kiến từ nhà cung cấp.</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }
        #endregion

        #region Trạng thái & Tài chính
        /// <summary>Trạng thái hiện tại của đơn hàng (Nháp, Đang xử lý, Đã duyệt, Hoàn tất, Đã hủy).</summary>
        public PurchaseOrderStatus Status { get; set; }

        /// <summary>Tổng giá trị cuối cùng của đơn đặt hàng (VND).</summary>
        public decimal TotalAmount { get; set; }
        #endregion

        #region Ghi chú & Yêu cầu
        /// <summary>Ghi chú hoặc yêu cầu vận hành đặc biệt gửi đối tác.</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy đơn (Chỉ xuất hiện nếu Status = Cancelled).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Đối tác & Nhân sự (Flattened Data)
        public int SupplierId { get; set; }

        /// <summary>Tên nhà cung cấp (Được ánh xạ từ Supplier.Name).</summary>
        public string SupplierName { get; set; } = string.Empty;

        public int CreatedById { get; set; }

        /// <summary>Tên nhân viên thu mua (Được ánh xạ từ IAUser.FullName).</summary>
        public string CreatedByName { get; set; } = string.Empty;
        #endregion

        #region Hệ thống
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Danh sách Sản phẩm
        /// <summary>Danh sách chi tiết các mặt hàng cùng tiến độ giao nhận của chúng.</summary>
        public List<PurchaseOrderDetailReadDto> Details { get; set; } = new List<PurchaseOrderDetailReadDto>();
        #endregion
    }

    /// <summary>
    /// DTO hiển thị dòng chi tiết mặt hàng trong Đơn mua (PO Line Item).
    /// </summary>
    public class PurchaseOrderDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Quy cách (Flattened)
        public int VariantId { get; set; }

        /// <summary>Tên hiển thị của Biến thể (SKU) (Ví dụ: Cà chua Cherry Đỏ 500g).</summary>
        public string VariantName { get; set; } = string.Empty;

        /// <summary>Mã SKU của Biến thể.</summary>
        public string VariantCode { get; set; } = string.Empty;

        public int UoMId { get; set; }

        /// <summary>Tên Đơn vị tính thu mua (Ví dụ: Thùng, Két, Tấn).</summary>
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Tiến độ Nhập kho
        /// <summary>Số lượng yêu cầu đặt mua ban đầu.</summary>
        public decimal OrderQuantity { get; set; }

        /// <summary>
        /// Số lượng thực tế đã nhập kho thành công (Lũy kế).
        /// Nghiệp vụ: Dựa vào đây, Frontend có thể vẽ thanh Progress Bar (Ví dụ: Đã nhận 50/100 Thùng) cho từng mặt hàng.
        /// </summary>
        public decimal ReceivedQuantity { get; set; }
        #endregion

        #region Tài chính
        /// <summary>Đơn giá thỏa thuận (VND).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền (OrderQuantity * UnitPrice).</summary>
        public decimal TotalPrice { get; set; }
        #endregion
    }
}