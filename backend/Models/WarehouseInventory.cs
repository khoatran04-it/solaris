namespace backend.Models
{
    /// <summary>
    /// Thực thể Két Sắt Tồn Kho 4 Ngăn (Warehouse Inventory 4-Bucket Ledger).
    /// Quản lý số dư tồn kho theo từng cặp [Kho - Biến thể SKU - Lô hàng] theo Đơn vị tính cơ sở (Base UoM).
    /// </summary>
    public class WarehouseInventory
    {
        public int Id { get; set; }

        /// <summary>ID Kho hàng</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>ID Biến thể sản phẩm (SKU)</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>ID Lô hàng nông sản</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        // --- 4 NGĂN TRẠNG THÁI TỒN KHO ---
        /// <summary>Ngăn 1: Số lượng khả dụng sẵn sàng để bán hoặc chuyển kho</summary>
        public decimal QuantityAvailable { get; set; } = 0;

        /// <summary>Ngăn 2: Số lượng đang giữ chỗ cho các đơn hàng chưa giao (Reserved)</summary>
        public decimal QuantityReserved { get; set; } = 0;

        /// <summary>Ngăn 3: Số lượng đang chờ kiểm định chất lượng / cách ly (Quality Control)</summary>
        public decimal QuantityQC { get; set; } = 0;

        /// <summary>Ngăn 4: Số lượng hàng hỏng, dập nát, quá hạn chờ thanh lý hoặc xuất hủy (Damaged)</summary>
        public decimal QuantityDamaged { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}