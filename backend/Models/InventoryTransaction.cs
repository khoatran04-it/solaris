using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Sổ Cái Giao Dịch Tồn Kho Bất Biến (Immutable Inventory Transaction Log).
    /// Ghi nhận mọi vết biến động tăng, giảm, giữ chỗ, hoàn trả và điều chỉnh kho theo thời gian thực.
    /// </summary>
    public class InventoryTransaction
    {
        public int Id { get; set; }

        /// <summary>Mã giao dịch phát sinh (Ví dụ: TX-20260817-0001)</summary>
        public required string TransactionCode { get; set; }

        /// <summary>Loại giao dịch (Nhập kho, Xuất kho, Giữ chỗ, Hủy giữ chỗ, Chuyển kho, Điều chỉnh, Trả hàng)</summary>
        public TransactionType Type { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Lô hàng cụ thể xảy ra biến động</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Số lượng biến động (theo Đơn vị tính cơ sở - Base UoM)</summary>
        public decimal Quantity { get; set; }

        /// <summary>Mã chứng từ gốc tham chiếu (Mã PO, IR, Order, Transfer, Audit...)</summary>
        public string? ReferenceCode { get; set; }

        /// <summary>Ghi chú nghiệp vụ chi tiết</summary>
        public string? Note { get; set; }

        /// <summary>Nhân viên thực hiện hoặc phê duyệt giao dịch</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Thời điểm ghi nhận giao dịch vào sổ cái (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}