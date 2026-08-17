using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Điều Chuyển Liên Kho (Inventory Transfer).
    /// Quy trình 2 bước: Xuất chuyển đi (Dispatched) $\to$ Vận chuyển đường dài $\to$ Nhập nhận tại kho đích (Received).
    /// </summary>
    public class InventoryTransfer : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã phiếu điều chuyển (Ví dụ: TRF-20260817-001)</summary>
        public required string TransferCode { get; set; }

        /// <summary>Kho nguồn (Kho xuất đi)</summary>
        public int FromWarehouseId { get; set; }
        public virtual Warehouse? FromWarehouse { get; set; }

        /// <summary>Kho đích (Kho tiếp nhận)</summary>
        public int ToWarehouseId { get; set; }
        public virtual Warehouse? ToWarehouse { get; set; }

        /// <summary>Đơn hàng liên kết (nếu sinh tự động do kho đích thiếu hàng phục vụ Order)</summary>
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        /// <summary>Trạng thái: Nháp, Đang vận chuyển, Hoàn tất nhập nhận, Đã hủy</summary>
        public InventoryTransferStatus Status { get; set; } = InventoryTransferStatus.Draft;

        /// <summary>Người tạo phiếu điều chuyển</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Thủ kho xuất phát hàng tại kho nguồn</summary>
        public int? DispatchedById { get; set; }
        public virtual IAUser? DispatchedBy { get; set; }
        public DateTime? DispatchedDate { get; set; }

        /// <summary>Thủ kho tiếp nhận hàng tại kho đích</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<InventoryTransferDetail> Details { get; set; } = new List<InventoryTransferDetail>();
    }
}
