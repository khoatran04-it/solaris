using backend.Models.Enums;

namespace backend.Models
{
    public class InventoryTransfer : ISoftDelete
    {
        public int Id { get; set; }

        public required string TransferCode { get; set; }

        public int FromWarehouseId { get; set; }
        public virtual Warehouse? FromWarehouse { get; set; }

        public int ToWarehouseId { get; set; }
        public virtual Warehouse? ToWarehouse { get; set; }

        // Nullable nếu là chuyển định kỳ/tái cân bằng tồn kho; Có giá trị nếu sinh tự động phục vụ Order
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public InventoryTransferStatus Status { get; set; } = InventoryTransferStatus.Draft;

        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        // Lúc xe xuất phát từ kho nguồn
        public int? DispatchedById { get; set; }
        public virtual IAUser? DispatchedBy { get; set; }
        public DateTime? DispatchedDate { get; set; }

        // Lúc xe tới kho đích và được nhận vào
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual ICollection<InventoryTransferDetail> Details { get; set; } = new List<InventoryTransferDetail>();
    }
}
