namespace backend.DTOs.CustomerTypeDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Phân loại Khách hàng.
    /// </summary>
    public class CustomerTypeUpdateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã phân loại khách hàng (Ví dụ: SI, LE, HORECA).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của phân loại (Ví dụ: Khách sỉ, Khách lẻ, Đại lý cấp 1).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về đặc điểm hoặc điều kiện để được xếp vào phân loại này.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang sử dụng, false: Tạm ngưng áp dụng).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}