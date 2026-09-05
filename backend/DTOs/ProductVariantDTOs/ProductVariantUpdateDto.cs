namespace backend.DTOs.ProductVariantDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Hỗ trợ cập nhật đồng thời cả thông tin cơ bản, bảng giá (Prices) và các thuộc tính động (Attributes).
    /// </summary>
    public class ProductVariantUpdateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã SKU biến thể dùng để quản lý kho (Ví dụ: SKU-ENVY-S-1KG).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị chi tiết của biến thể (Ví dụ: Táo Envy Size Nhỏ 1kg).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết & Quy cách
        /// <summary>Đường dẫn ảnh chụp thực tế chi tiết của biến thể.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về quy cách đóng gói, ngoại hình của riêng biến thể này.</summary>
        public string? Description { get; set; }

        /// <summary>Mức tồn kho an toàn tối thiểu (Safety Stock - tự động cảnh báo khi tồn xuống dưới mức này).</summary>
        public int InventoryGuideline { get; set; }

        public decimal? GrossWeightKg { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? UnitCbm { get; set; }
        #endregion

        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Sản phẩm cha (Product Master) mà biến thể này trực thuộc.</summary>
        public int ProductId { get; set; }
        #endregion

        #region Dữ liệu Mở rộng (Cập nhật đồng thời)
        /// <summary>Danh sách các giá trị thuộc tính động (EAV) thiết lập lại cho biến thể này.</summary>
        public List<AttributeInputDto> Attributes { get; set; } = new();

        /// <summary>Danh sách bảng giá bán cập nhật theo các Đơn vị tính khác nhau của biến thể này.</summary>
        public List<VariantPriceInputDto> Prices { get; set; } = new();
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép kinh doanh/nhập xuất, false: Tạm khóa).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}