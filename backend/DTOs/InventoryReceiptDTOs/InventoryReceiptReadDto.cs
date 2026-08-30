using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryReceiptDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Phiếu Nhập Kho (Inventory Receipt / GRN).
    /// Dữ liệu đã được "làm phẳng" (Flatten) với các bảng liên kết để tối ưu hóa hiệu suất hiển thị trên Giao diện (UI).
    /// </summary>
    public class InventoryReceiptReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã phiếu nhập kho (Dùng để đối chiếu và in mã vạch).</summary>
        public string ReceiptCode { get; set; } = string.Empty;

        /// <summary>Trạng thái vòng đời của phiếu (Pending, QC, Completed, Cancelled).</summary>
        public InventoryReceiptStatus Status { get; set; }
        #endregion

        #region Thời gian & Ghi chú
        /// <summary>Thời điểm xe tải cập bến và bốc dỡ hàng.</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>Ghi chú chung về đợt nhận hàng.</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy phiếu (Chỉ xuất hiện khi Status = Cancelled).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Đối tượng liên quan (Flattened Data)
        public int WarehouseId { get; set; }
        /// <summary>Tên Kho hàng tiếp nhận (Hiển thị UI).</summary>
        public string WarehouseName { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
        /// <summary>Tên Nhà cung cấp giao hàng (Hiển thị UI).</summary>
        public string? SupplierName { get; set; }

        public int? ReceivedById { get; set; }
        /// <summary>Tên Thủ kho / Nhân viên QC thực hiện kiểm đếm (Hiển thị UI).</summary>
        public string? ReceivedByName { get; set; }
        #endregion

        #region Hệ thống
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Danh sách Kiểm đếm
        /// <summary>Danh sách chi tiết kết quả kiểm đếm chất lượng từng mặt hàng.</summary>
        public List<InventoryReceiptDetailReadDto> Details { get; set; } = new List<InventoryReceiptDetailReadDto>();
        #endregion
    }

    /// <summary>
    /// DTO hiển thị kết quả kiểm đếm của một dòng mặt hàng trong Phiếu nhập kho.
    /// </summary>
    public class InventoryReceiptDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Nguồn gốc (Flattened)
        public int VariantId { get; set; }
        /// <summary>Mã SKU của Biến thể sản phẩm.</summary>
        public string VariantCode { get; set; } = string.Empty;
        /// <summary>Tên hiển thị của Biến thể sản phẩm.</summary>
        public string VariantName { get; set; } = string.Empty;

        public int BatchId { get; set; }
        /// <summary>Mã Lô hàng (Rất quan trọng để nhân viên dán tem QR Code Truy xuất nguồn gốc lên thùng hàng).</summary>
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        /// <summary>Tên Đơn vị tính (Ví dụ: Thùng, Pallet, Tấn).</summary>
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Đối soát Chứng từ
        /// <summary>ID dòng chi tiết Đơn mua hàng (PO) tương ứng (Hỗ trợ truy vết ngược).</summary>
        public int? PurchaseOrderDetailId { get; set; }
        #endregion

        #region Số liệu Kiểm đếm (QC Metrics)
        /// <summary>Số lượng dự kiến giao ban đầu.</summary>
        public decimal ExpectedQuantity { get; set; }

        /// <summary>Số lượng thực tế đạt chuẩn đã được nhập vào Tồn kho.</summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>Số lượng bị từ chối/hỏng.</summary>
        public decimal RejectedQuantity { get; set; }

        /// <summary>Lý do từ chối (bắt buộc nếu RejectedQuantity > 0).</summary>
        public string? RejectReason { get; set; }
        #endregion
    }
}