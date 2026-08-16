using backend.Models.Enums;

namespace backend.Models
{
    public class InventoryReceipt : ISoftDelete
    {
        public int Id { get; set; }
        public required string ReceiptCode { get; set; } // VD: IR-20260814-001

        public InventoryReceiptStatus Status { get; set; } = InventoryReceiptStatus.Pending;
        public DateTime? ReceiptDate { get; set; } // Ngày giờ xe chở hàng tới cửa kho thực tế
        public string? Note { get; set; } // Ghi chú kiểm đếm (VD: Trời mưa ướt 2 thùng, đã loại)
        public string? CancellationReason { get; set; } // Lý do hủy phiếu

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        // Nhập vào kho nào?
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        // Nhà cung cấp nào giao? (Nullable: hàng tặng, hàng mẫu không cần NCC)
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        // Trưởng kho / Thủ kho kiểm đếm và xác nhận nhập kho
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        // 1 Phiếu Nhập chứa nhiều dòng chi tiết
        // Mỗi dòng Detail có thể trỏ về PO khác nhau -> 1 IR chứa hàng từ nhiều PO
        public virtual ICollection<InventoryReceiptDetail> Details { get; set; } = new List<InventoryReceiptDetail>();
    }
}
