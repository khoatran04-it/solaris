using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Lô Hàng Nông Sản (Product Batch / Lot).
    /// Hạt nhân quản lý chất lượng của ngành thực phẩm & nông sản: Yêu cầu khắt khe về Truy xuất nguồn gốc (Traceability), 
    /// quản lý Ngày sản xuất (NSX) và Hạn sử dụng (HSD) để phục vụ thuật toán xuất kho FEFO (First Expired, First Out).
    /// </summary>
    public class ProductBatch : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã lô hàng duy nhất (Ví dụ: BATCH-20260817-CACHUA-01). Dùng để in mã vạch QR và tra cứu nhanh khi có sự cố an toàn thực phẩm.</summary>
        public required string BatchCode { get; set; }
        #endregion

        #region Thời hạn & Vòng đời (Shelf-life)
        /// <summary>Ngày sản xuất / Ngày thu hoạch, sơ chế, đóng gói từ Nông hộ hoặc Nhà máy.</summary>
        public DateTime ManufactureDate { get; set; }

        /// <summary>
        /// Hạn sử dụng của lô hàng.
        /// Nghiệp vụ: Cực kỳ quan trọng để hệ thống áp dụng thuật toán FEFO (Lô hàng cận date hơn sẽ được ưu tiên xuất kho trước) 
        /// và kích hoạt các cảnh báo xả hàng tồn khi sắp hết hạn.
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái lưu thông (true: Đang kinh doanh bình thường, false: Lô hàng đang bị phong tỏa/đình chỉ để kiểm dịch hoặc thu hồi).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết & Truy xuất nguồn gốc (Traceability)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) cấu thành nên lô hàng này.</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã định danh của Nhà cung cấp / Hợp tác xã / Nông hộ đã cung ứng lô hàng này.</summary>
        public int SupplierId { get; set; }

        /// <summary>
        /// Thực thể Nhà cung cấp.
        /// Nghiệp vụ: Là chốt chặn bắt buộc để truy vết ngược (Backward Traceability) nhằm tìm ra nguồn gốc lô đất/nông trại nếu phát hiện dư lượng hóa chất vượt mức cho phép.
        /// </summary>
        public virtual Supplier? Supplier { get; set; }
        #endregion
    }
}