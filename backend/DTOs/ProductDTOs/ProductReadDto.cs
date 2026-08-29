namespace backend.DTOs.ProductDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Sản Phẩm Cha (Product Master).
    /// Dùng để trả dữ liệu về cho Front-end (Web/App) hiển thị danh sách hoặc chi tiết sản phẩm gốc.
    /// </summary>
    public class ProductReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã sản phẩm chung (Ví dụ: PROD-APPLE-ENVY).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của dòng sản phẩm (Ví dụ: Táo Envy New Zealand).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện chung cho dòng sản phẩm.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả tổng quan, chi tiết về dòng sản phẩm.</summary>
        public string? Description { get; set; }
        #endregion

        #region Liên kết dữ liệu & Mở rộng (UI Render)
        /// <summary>Mã định danh Danh mục trực thuộc.</summary>
        public int? CategoryId { get; set; }

        /// <summary>Tên hiển thị Danh mục trực thuộc (Lấy từ ProductCategory - dùng để hiển thị trên UI).</summary>
        public string? CategoryName { get; set; }

        /// <summary>Mã định danh Đơn vị tính cơ sở chuẩn của sản phẩm.</summary>
        public int BaseUoMId { get; set; }

        /// <summary>Tên hiển thị Đơn vị tính cơ sở (Lấy từ bảng UoM - Ví dụ: Kg, Quả, Thùng).</summary>
        public string? BaseUoMName { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép kinh doanh, false: Tạm ngưng).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}