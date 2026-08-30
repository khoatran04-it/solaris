using System;

namespace backend.DTOs.ProductBatchDTOs
{
    /// <summary>
    /// DTO yêu cầu Cập nhật thông tin Lô Hàng Nông Sản (Product Batch).
    /// Ràng buộc nghiệp vụ: Tuyệt đối KHÔNG CHO PHÉP cập nhật Mã lô (BatchCode), Sản phẩm (VariantId) 
    /// và Nguồn gốc (SupplierId) để bảo toàn tính minh bạch của dữ liệu Truy xuất nguồn gốc (Traceability).
    /// </summary>
    public class ProductBatchUpdateDto
    {
        #region Thời hạn & Vòng đời (Shelf-life)
        /// <summary>Ngày sản xuất (Cho phép điều chỉnh nếu có sai sót do nhân viên kho nhập liệu ban đầu).</summary>
        public DateTime ManufactureDate { get; set; }

        /// <summary>
        /// Hạn sử dụng của lô hàng.
        /// Nghiệp vụ: Trong thực tế, bộ phận Kiểm soát chất lượng (QC/QA) có quyền đánh giá lại 
        /// và gia hạn (Extend) hoặc rút ngắn HSD dựa trên tình trạng bảo quản thực tế của nông sản tại kho.
        /// </summary>
        public DateTime ExpiryDate { get; set; }
        #endregion

        #region Trạng thái & Quản lý rủi ro
        /// <summary>
        /// Trạng thái lưu thông của lô hàng.
        /// Nghiệp vụ cốt lõi: Cho phép Quản lý kho / QC chuyển trạng thái thành 'false' (Quarantine - Phong tỏa) 
        /// để đình chỉ khẩn cấp nếu phát hiện lô hàng bị lỗi, nhiễm khuẩn hoặc chờ xét nghiệm dư lượng, 
        /// ngăn chặn triệt để việc thuật toán chia đơn (Smart Routing) gắp nhầm hàng lỗi đi giao.
        /// </summary>
        public bool IsActive { get; set; }
        #endregion
    }
}