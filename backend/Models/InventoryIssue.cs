using backend.Models.Enums;

namespace backend.Models
{
    public class InventoryIssue : ISoftDelete
    {
        public int Id { get; set; }

        public required string IssueCode { get; set; }

        // Nullable nếu là xuất hàng mẫu / tiêu hủy / xuất nội bộ không qua Order
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

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

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual ICollection<InventoryIssueDetail> Details { get; set; } = new List<InventoryIssueDetail>();
    }
}
