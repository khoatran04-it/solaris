namespace backend.DTOs.ProductDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Sản Phẩm Cha (Product Master).
    /// </summary>
    public class ProductUpdateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã sản phẩm chung (Ví dụ: PROD-APPLE-ENVY, PROD-TOMATO-BEEF).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của dòng sản phẩm (Ví dụ: Táo Envy New Zealand).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện chung cho dòng sản phẩm.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả tổng quan, chi tiết về dòng sản phẩm (chất lượng, nguồn gốc chung).</summary>
        public string? Description { get; set; }
        #endregion

        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Danh mục trực thuộc (Ví dụ: Trái cây nhập khẩu).</summary>
        public int? CategoryId { get; set; }

        /// <summary>Mã định danh Đơn vị tính cơ sở chuẩn của sản phẩm (Base UoM - Ví dụ: Kg, Quả, Thùng).</summary>
        public int BaseUoMId { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép kinh doanh, false: Tạm ngưng).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}