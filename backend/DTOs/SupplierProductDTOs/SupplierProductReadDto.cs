namespace backend.DTOs.SupplierProductDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Bảng giá & Danh mục mặt hàng từ Nhà cung cấp.
    /// Chứa các trường thông tin mở rộng (Enriched Properties) để giao diện (UI) render nhanh chóng mà không cần gọi thêm API phụ.
    /// </summary>
    public class SupplierProductReadDto
    {
        public int Id { get; set; }

        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Biến thể sản phẩm (SKU hệ thống).</summary>
        public int VariantId { get; set; }

        /// <summary>Mã định danh Nhà cung cấp.</summary>
        public int SupplierId { get; set; }

        /// <summary>Mã định danh Đơn vị tính mua hàng đặc thù từ NCC này.</summary>
        public int PurchaseUoMId { get; set; }
        #endregion

        #region Thông tin Định danh & Mua hàng
        /// <summary>Mã SKU nội bộ của Nhà cung cấp (dùng để đối soát khi đặt hàng).</summary>
        public string? SupplierSKU { get; set; }

        /// <summary>Đơn giá nhập tham chiếu hoặc giá thỏa thuận theo hợp đồng.</summary>
        public decimal LastImportPrice { get; set; }

        /// <summary>Số lượng đặt hàng tối thiểu (MOQ - Minimum Order Quantity).</summary>
        public decimal MinimumOrderQuantity { get; set; } = 1m;

        /// <summary>Thời gian chuẩn bị và giao hàng ước tính tính bằng ngày (Lead Time Days).</summary>
        public int LeadTimeDays { get; set; } = 0;
        #endregion

        #region Thông tin Mở rộng (UI Render Properties)
        // --- Của Biến thể sản phẩm ---
        /// <summary>Mã biến thể sản phẩm trong hệ thống.</summary>
        public string? VariantCode { get; set; }

        /// <summary>Tên hiển thị biến thể sản phẩm.</summary>
        public string? VariantName { get; set; }

        /// <summary>Đường dẫn hình ảnh của biến thể sản phẩm.</summary>
        public string? VariantImagePath { get; set; }

        // --- Của Nhà cung cấp ---
        /// <summary>Mã hệ thống của nhà cung cấp.</summary>
        public string? SupplierCode { get; set; }

        /// <summary>Tên hiển thị công ty/nhà cung cấp.</summary>
        public string? SupplierName { get; set; }

        // --- Của Đơn vị tính ---
        /// <summary>Tên hiển thị đơn vị tính mua hàng.</summary>
        public string? PurchaseUoMName { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cung cấp, false: Ngừng cung cấp).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}