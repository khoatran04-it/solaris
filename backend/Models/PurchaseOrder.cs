using backend.Models.Enums;

namespace backend.Models
{
    public class PurchaseOrder : ISoftDelete
    {
        public int Id { get; set; }
        public required string OrderCode { get; set; } // VD: PO-20260814-001

        public DateTime OrderDate { get; set; } // Ngày lên đơn
        public DateTime? ExpectedDeliveryDate { get; set; } // Ngày hẹn giao hàng dự kiến

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

        public decimal TotalAmount { get; set; } // Tổng giá trị đơn hàng (Recalculate khi Detail thay đổi)
        public string? Note { get; set; } // Ghi chú chung (VD: Giao trước 9h sáng)
        public string? CancellationReason { get; set; } // Lý do hủy (Chỉ có giá trị khi Status = Cancelled)

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        // Đơn mua hàng này đặt từ Nhà cung cấp nào?
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        // Nhân viên Thu mua nào lập đơn?
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        // 1 PO chứa nhiều dòng chi tiết (Mặt hàng A bao nhiêu, Mặt hàng B bao nhiêu...)
        public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
    }
}
