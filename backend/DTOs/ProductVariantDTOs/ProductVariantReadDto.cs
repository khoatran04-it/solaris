namespace backend.DTOs.ProductVariantDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Cấu hình Giá bán theo Đơn vị tính của Biến thể.
    /// </summary>
    public class VariantPriceReadDto
    {
        public int Id { get; set; }

        #region Thông tin Đơn vị tính & Mở rộng (UI Render)
        /// <summary>Mã định danh Đơn vị tính.</summary>
        public int UoMId { get; set; }

        /// <summary>Tên hiển thị Đơn vị tính (Ví dụ: Kg, Thùng).</summary>
        public string? UoMName { get; set; }
        #endregion

        #region Thông tin Giá
        /// <summary>Đơn giá bán ra niêm yết (VNĐ).</summary>
        public decimal Price { get; set; }

        /// <summary>Giá khuyến mãi (được tính toán tự động nếu có chương trình Promotion đang áp dụng).</summary>
        public decimal? PromotionalPrice { get; set; }

        /// <summary>Cờ đánh dấu đây là ĐVT và mức giá bán mặc định trên giao diện.</summary>
        public bool IsDefault { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO hiển thị chi tiết Giá trị Thuộc tính động (EAV) của Biến thể.
    /// </summary>
    public class VariantAttributeDto
    {
        public int Id { get; set; }

        #region Thông tin Định nghĩa Thuộc tính & Mở rộng (UI Render)
        /// <summary>Mã định danh của Thuộc tính từ Từ điển hệ thống.</summary>
        public int? AttributeDefinitionId { get; set; }

        /// <summary>Tên hiển thị của Thuộc tính (Ví dụ: Độ Brix, Vùng trồng, Kích cỡ).</summary>
        public string? AttributeDefinitionName { get; set; }
        #endregion

        #region Giá trị Thuộc tính
        /// <summary>Giá trị thực tế của thuộc tính thiết lập cho biến thể này (Ví dụ: "12", "Đà Lạt", "Size L").</summary>
        public string AttributeValue { get; set; } = string.Empty;
        #endregion
    }

    /// <summary>
    /// DTO hiển thị chi tiết Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Gói gọn toàn bộ thông tin cơ bản, bảng giá và các thuộc tính động trả về cho Client.
    /// </summary>
    public class ProductVariantReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã SKU biến thể dùng để quản lý kho (Ví dụ: SKU-ENVY-S-1KG).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị chi tiết của biến thể (Ví dụ: Táo Envy Size Nhỏ 1kg).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết & Quy cách
        /// <summary>Mô tả chi tiết về quy cách đóng gói, ngoại hình của riêng biến thể này.</summary>
        public string? Description { get; set; }

        /// <summary>Đường dẫn ảnh chụp thực tế chi tiết của biến thể.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mức tồn kho an toàn tối thiểu (Safety Stock).</summary>
        public int InventoryGuideline { get; set; }

        /// <summary>Số lượng tồn kho khả dụng thực tế (tính từ các lô hàng còn hạn).</summary>
        public decimal QuantityAvailable { get; set; }

        public decimal? GrossWeightKg { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? UnitCbm { get; set; }
        #endregion

        #region Liên kết dữ liệu & Mở rộng (UI Render)
        /// <summary>Mã định danh Sản phẩm cha (Product Master).</summary>
        public int ProductId { get; set; }

        /// <summary>Tên hiển thị Sản phẩm cha trực thuộc.</summary>
        public string? ProductName { get; set; }
        #endregion

        #region Dữ liệu Liên kết trực thuộc (Collections)
        /// <summary>Danh sách các giá trị thuộc tính động (EAV) của biến thể.</summary>
        public List<VariantAttributeDto> Attributes { get; set; } = new();

        /// <summary>Danh sách bảng giá bán theo các Đơn vị tính khác nhau của biến thể.</summary>
        public List<VariantPriceReadDto> Prices { get; set; } = new();
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang kinh doanh, false: Tạm ngưng).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}