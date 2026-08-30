using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryIssueDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Phiếu Xuất Kho (Inventory Issue / Goods Issue Note).
    /// Dữ liệu đã được "làm phẳng" (Flatten) với các bảng liên kết để tối ưu hóa hiệu suất hiển thị trên Giao diện (UI) 
    /// và hỗ trợ in ấn phiếu xuất/tem vận chuyển nhanh chóng.
    /// </summary>
    public class InventoryIssueReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã phiếu xuất kho (Dùng để in barcode/QR code dán lên kiện hàng).</summary>
        public string IssueCode { get; set; } = string.Empty;

        /// <summary>Trạng thái vòng đời của phiếu (Pending, Processing, Completed, Cancelled).</summary>
        public InventoryIssueStatus Status { get; set; }
        #endregion

        #region Thời gian & Ghi chú
        /// <summary>Thời điểm xuất kho thực tế hoặc dự kiến.</summary>
        public DateTime IssueDate { get; set; }

        /// <summary>Ghi chú đóng gói hoặc lưu ý vận chuyển.</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy phiếu (Chỉ hiển thị khi Status = Cancelled).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Đối soát Chứng từ (Flattened)
        public int? OrderId { get; set; }

        /// <summary>Mã Đơn bán hàng tham chiếu (Giúp nhân viên kho dễ dàng đối chiếu chứng từ giấy).</summary>
        public string? OrderCode { get; set; }
        #endregion

        #region Thông tin Kho & Nhân sự (Flattened)
        public int WarehouseId { get; set; }

        /// <summary>Tên Kho hàng thực hiện xuất (Hiển thị UI).</summary>
        public string WarehouseName { get; set; } = string.Empty;

        public int IssuedById { get; set; }

        /// <summary>Tên Thủ kho / Nhân viên nhặt hàng (Hiển thị UI).</summary>
        public string IssuedByName { get; set; } = string.Empty;
        #endregion

        #region Thông tin Giao nhận (Logistics)
        /// <summary>Tên người nhận hàng cuối cùng hoặc tài xế vận chuyển.</summary>
        public string? ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ giao hàng.</summary>
        public string? ReceiverPhone { get; set; }

        /// <summary>Địa chỉ đích đến của kiện hàng.</summary>
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Hệ thống
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Danh sách Hàng hóa xuất kho
        /// <summary>Danh sách chi tiết các mặt hàng và lô hàng cụ thể được xuất đi.</summary>
        public List<InventoryIssueDetailReadDto> Details { get; set; } = new List<InventoryIssueDetailReadDto>();
        #endregion
    }

    /// <summary>
    /// DTO hiển thị chi tiết của một dòng mặt hàng trong Phiếu xuất kho.
    /// </summary>
    public class InventoryIssueDetailReadDto
    {
        public int Id { get; set; }

        #region Đối soát Đơn hàng
        /// <summary>ID dòng chi tiết Đơn bán hàng (Sales Order) tương ứng.</summary>
        public int? OrderDetailId { get; set; }
        #endregion

        #region Hàng hóa & Nguồn gốc (Flattened)
        public int VariantId { get; set; }
        /// <summary>Tên hiển thị của Biến thể sản phẩm (SKU).</summary>
        public string VariantName { get; set; } = string.Empty;
        /// <summary>Mã SKU của Biến thể.</summary>
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        /// <summary>Mã Lô hàng xuất đi (Dữ liệu đặc biệt quan trọng để đối chiếu Truy xuất nguồn gốc).</summary>
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        /// <summary>Tên Đơn vị tính (Ví dụ: Thùng, Két, Túi).</summary>
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>Số lượng thực tế xuất đi (Theo ĐVT đã định).</summary>
        public decimal Quantity { get; set; }

        /// <summary>Đơn giá xuất kho (VND).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền xuất kho (Quantity * UnitPrice).</summary>
        public decimal TotalPrice { get; set; }
        #endregion
    }
}