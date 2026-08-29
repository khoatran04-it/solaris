namespace backend.DTOs.CustomerGroupDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Nhóm Khách Hàng (Marketing Tag).
    /// </summary>
    public class CustomerGroupUpdateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã nhóm khách hàng (Ví dụ: GRP-VIP, GRP-WHOLESALE, GRP-PROMO-HUNTER).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của nhóm khách hàng (Ví dụ: Khách VIP, Khách săn sale, Đối tác chiến lược).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về tiêu chí hoặc mục đích tạo nhóm này.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang sử dụng nhóm này, false: Tạm khóa/Ẩn đi).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}