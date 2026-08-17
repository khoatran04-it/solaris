using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Xuất Kho Giao Hàng (Inventory Issue).
    /// Đóng gói hàng từ kho theo lô FEFO và trừ số lượng giữ chỗ (QuantityReserved) chuyển sang bàn giao vận chuyển.
    /// </summary>
    public class InventoryIssue : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã phiếu xuất kho (Ví dụ: OUT-20260817-001)</summary>
        public required string IssueCode { get; set; }

        /// <summary>Đơn bán hàng tham chiếu (hoặc null nếu xuất hàng mẫu/nội bộ)</summary>
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        /// <summary>Kho thực hiện xuất hàng</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Thủ kho / Nhân viên thực hiện xuất kho</summary>
        public int IssuedById { get; set; }
        public virtual IAUser? IssuedBy { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public InventoryIssueStatus Status { get; set; } = InventoryIssueStatus.Pending;

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<InventoryIssueDetail> Details { get; set; } = new List<InventoryIssueDetail>();
    }
}
