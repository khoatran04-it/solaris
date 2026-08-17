using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Nhập Kho & Kiểm Đếm Chất Lượng Nông Sản (Inventory Receipt - IR).
    /// Ghi nhận quy trình nhận hàng tại cửa kho, phân loại SL Đạt (Accepted) vs SL Hỏng (Rejected) và cập nhật số dư tồn kho.
    /// </summary>
    public class InventoryReceipt : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã phiếu nhập kho (Ví dụ: IR-20260817-001)</summary>
        public required string ReceiptCode { get; set; }

        /// <summary>Trạng thái: Chờ kiểm đếm, Đang kiểm tra QC, Hoàn tất nhập kho, Đã hủy</summary>
        public InventoryReceiptStatus Status { get; set; } = InventoryReceiptStatus.Pending;

        /// <summary>Thời điểm xe giao hàng cập bến bốc dỡ thực tế</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>Ghi chú kiểm đếm hàng hóa</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy phiếu</summary>
        public string? CancellationReason { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>Kho hàng tiếp nhận</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Nhà cung cấp giao hàng</summary>
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Thủ kho / Nhân viên kiểm định chất lượng thực hiện</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        /// <summary>Danh sách các mặt hàng kiểm đếm nhập kho</summary>
        public virtual ICollection<InventoryReceiptDetail> Details { get; set; } = new List<InventoryReceiptDetail>();
    }
}
