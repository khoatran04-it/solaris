namespace backend.Models
{
    /// <summary>
    /// Thực thể Bảng Giá & Danh Mục Mặt Hàng Của Nhà Cung Cấp.
    /// Xác định đơn giá nhập mặc định, số lượng đặt tối thiểu (MOQ) và thời gian giao hàng (Lead Time) cho từng mặt hàng.
    /// </summary>
    public class SupplierProduct : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Mua hàng
        /// <summary>Mã SKU nội bộ của Nhà Cung Cấp (nếu có, để tiện đối soát đơn đặt hàng).</summary>
        public string? SupplierSKU { get; set; }

        /// <summary>Đơn giá nhập tham chiếu gần nhất (hoặc giá thỏa thuận theo hợp đồng).</summary>
        public decimal LastImportPrice { get; set; }

        /// <summary>Số lượng đặt hàng tối thiểu (MOQ - Minimum Order Quantity).</summary>
        public decimal MinimumOrderQuantity { get; set; } = 1;

        /// <summary>Thời gian chuẩn bị và giao hàng ước tính tính bằng ngày (Lead Time Days).</summary>
        public int LeadTimeDays { get; set; } = 0;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cung cấp, false: Ngừng cung cấp mặt hàng này).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh Biến thể sản phẩm (SKU hệ thống).</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã định danh Nhà cung cấp.</summary>
        public int SupplierId { get; set; }

        /// <summary>Thực thể Nhà cung cấp.</summary>
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Mã định danh Đơn vị tính mua hàng đặc thù từ Nhà cung cấp này (Ví dụ: Mua theo Thùng, Bao, Két).</summary>
        public int PurchaseUoMId { get; set; }

        /// <summary>Thực thể Đơn vị tính mua hàng.</summary>
        public virtual UoM? PurchaseUoM { get; set; }
        #endregion
    }
}