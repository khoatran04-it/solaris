namespace backend.DTOs.SupplierProductDTOs
{
    /// <summary>
    /// DTO yêu cầu thiết lập Bảng giá & Danh mục mặt hàng từ Nhà cung cấp.
    /// </summary>
    public class SupplierProductCreateDto
    {
        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Nhà cung cấp.</summary>
        public int SupplierId { get; set; }

        /// <summary>Mã định danh Biến thể sản phẩm (SKU hệ thống).</summary>
        public int VariantId { get; set; }

        /// <summary>Mã định danh Đơn vị tính mua hàng đặc thù từ Nhà cung cấp này (Ví dụ: Thùng, Bao, Két).</summary>
        public int PurchaseUoMId { get; set; }
        #endregion

        #region Thông tin Định danh & Mua hàng
        /// <summary>Mã SKU nội bộ của Nhà cung cấp (nếu có, để tiện đối soát khi đặt hàng).</summary>
        public string? SupplierSKU { get; set; }

        /// <summary>Đơn giá nhập tham chiếu hoặc đơn giá thỏa thuận theo hợp đồng.</summary>
        public decimal LastImportPrice { get; set; }

        /// <summary>Số lượng đặt hàng tối thiểu (MOQ - Minimum Order Quantity).</summary>
        public decimal MinimumOrderQuantity { get; set; } = 1m;

        /// <summary>Thời gian chuẩn bị và giao hàng ước tính tính bằng ngày (Lead Time Days).</summary>
        public int LeadTimeDays { get; set; } = 0;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cung cấp, false: Ngừng cung cấp mặt hàng này).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}