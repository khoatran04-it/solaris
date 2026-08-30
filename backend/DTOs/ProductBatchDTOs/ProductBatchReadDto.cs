using System;

namespace backend.DTOs.ProductBatchDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Lô Hàng Nông Sản (Product Batch).
    /// Dữ liệu đã được "làm phẳng" (Flatten) với các bảng liên kết để Frontend tối ưu hóa 
    /// việc hiển thị lưới dữ liệu (DataGrid) và in ấn tem nhãn truy xuất nguồn gốc.
    /// </summary>
    public class ProductBatchReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã lô hàng (Dùng để in mã vạch / QR Code dán lên thùng/pallet).</summary>
        public string BatchCode { get; set; } = string.Empty;
        #endregion

        #region Thời hạn & Vòng đời (Shelf-life)
        /// <summary>Ngày sản xuất / thu hoạch thực tế.</summary>
        public DateTime ManufactureDate { get; set; }

        /// <summary>
        /// Ngày hết hạn của lô hàng. 
        /// Nghiệp vụ: Frontend có thể so sánh trường này với ngày hiện tại để tô màu cảnh báo (Đỏ/Vàng) 
        /// cho các lô hàng cận date cần ưu tiên đẩy Sale hoặc xuất kho gấp (FEFO).
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        #endregion

        #region Nguồn gốc & Hàng hóa (Flattened Data)
        public int VariantId { get; set; }

        /// <summary>Tên Biến thể sản phẩm (SKU) (Ví dụ: Cà chua Cherry Đỏ 500g).</summary>
        public string VariantName { get; set; } = string.Empty;

        /// <summary>Mã định danh SKU (Hỗ trợ đối chiếu khi nhân viên quét máy tít mã vạch).</summary>
        public string VariantCode { get; set; } = string.Empty;

        public int SupplierId { get; set; }

        /// <summary>Tên Nhà cung cấp / Nông hộ (Thông tin quan trọng nhất để minh bạch truy xuất nguồn gốc - Traceability).</summary>
        public string SupplierName { get; set; } = string.Empty;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái (true: Đang lưu thông bình thường, false: Lô hàng đang bị phong tỏa chờ kiểm tra chất lượng).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}