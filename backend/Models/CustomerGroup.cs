namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Khách Hàng (Customer Group / Marketing Tag).
    /// Hỗ trợ phân loại, gắn thẻ (tag) khách hàng phục vụ cho các chiến dịch tiếp thị, phân khúc khách hàng (Segmentation).
    /// Ghi chú: Một khách hàng có thể thuộc nhiều Nhóm (thông qua bảng trung gian CustomerGroupLink).
    /// </summary>
    public class CustomerGroup : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã nhóm khách hàng (Ví dụ: GRP-VIP, GRP-WHOLESALE, GRP-PROMO-HUNTER).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của nhóm khách hàng (Ví dụ: Khách VIP, Khách săn sale, Đối tác chiến lược).</summary>
        public required string Name { get; set; }
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về tiêu chí hoặc mục đích tạo nhóm này (Ví dụ: Nhóm dành cho khách hàng thường xuyên mua hàng vào dịp Lễ).</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang sử dụng nhóm này, false: Tạm khóa/Ẩn đi).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Danh sách liên kết đa chiều (Many-to-Many) tới các Khách hàng nằm trong nhóm này.</summary>
        public virtual ICollection<CustomerGroupLink> GroupLinks { get; set; } = new List<CustomerGroupLink>();
        #endregion
    }
}