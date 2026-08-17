using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đơn Đặt Mua Hàng Nhà Cung Cấp (Purchase Order - PO).
    /// Quản lý hợp đồng/đơn hàng thu mua nông sản, tiến độ giao nhận và phân đợt nhập kho.
    /// </summary>
    public class PurchaseOrder : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã đơn mua hàng (Ví dụ: PO-20260817-001)</summary>
        public required string OrderCode { get; set; }

        /// <summary>Ngày lập đơn đặt hàng</summary>
        public DateTime OrderDate { get; set; }

        /// <summary>Ngày hẹn giao hàng dự kiến từ nhà cung cấp</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>Trạng thái đơn: Nháp, Đang xử lý, Đã duyệt, Hoàn tất, Đã hủy</summary>
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        /// <summary>Tổng giá trị đơn đặt hàng (VND)</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Ghi chú đơn hàng (Ví dụ: Giao trước 8h sáng, giữ lạnh...)</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy đơn (nếu trạng thái là Cancelled)</summary>
        public string? CancellationReason { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>Nhà cung cấp tiếp nhận đơn đặt</summary>
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Nhân viên thu mua lập đơn</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Danh sách các mặt hàng đặt mua</summary>
        public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
    }
}
