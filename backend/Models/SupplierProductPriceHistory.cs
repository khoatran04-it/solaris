namespace backend.Models
{
    /// <summary>
    /// Thực thể Lịch Sử Biến Động Giá Nhập Từ Nhà Cung Cấp.
    /// Lưu vết từng lần thiết lập hoặc điều chỉnh đơn giá nhập theo mốc thời gian để phục vụ báo cáo thống kê biến động giá.
    /// </summary>
    public class SupplierProductPriceHistory
    {
        public int Id { get; set; }

        /// <summary>Mã liên kết cấu hình bảng giá NCC (SupplierProduct).</summary>
        public int SupplierProductId { get; set; }
        public virtual SupplierProduct? SupplierProduct { get; set; }

        /// <summary>Mã nhà cung cấp.</summary>
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Mã biến thể sản phẩm (SKU).</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã đơn vị tính mua hàng.</summary>
        public int PurchaseUoMId { get; set; }
        public virtual UoM? PurchaseUoM { get; set; }

        /// <summary>Đơn giá nhập cũ trước khi điều chỉnh.</summary>
        public decimal OldPrice { get; set; }

        /// <summary>Đơn giá nhập mới sau khi điều chỉnh.</summary>
        public decimal NewPrice { get; set; }

        /// <summary>Chênh lệch giá (NewPrice - OldPrice).</summary>
        public decimal PriceChange { get; set; }

        /// <summary>Loại thay đổi: INITIAL (Tạo mới), MANUAL_UPDATE (Người dùng sửa), AUTO_SYNC (Đồng bộ từ PO/GRN).</summary>
        public string ChangeType { get; set; } = "MANUAL_UPDATE";

        /// <summary>Ghi chú hoặc lý do thay đổi giá.</summary>
        public string? Note { get; set; }

        /// <summary>Thời điểm ghi nhận biến động giá (mốc thời gian đưa vào báo cáo thống kê).</summary>
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

        /// <summary>Mã người dùng thực hiện thay đổi giá (nếu có).</summary>
        public int? CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }
    }
}
