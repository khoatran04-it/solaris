namespace backend.Models
{
    /// <summary>
    /// Thực thể Phân loại Khách hàng (Customer Type / Customer Group).
    /// Quản lý các nhóm đối tượng khách hàng khác nhau (Ví dụ: Khách sỉ, Khách lẻ, Nhà hàng/Khách sạn - HORECA, Đại lý).
    /// Hỗ trợ thiết lập các chính sách giá bán, công nợ hoặc chương trình khuyến mãi chuyên biệt cho từng nhóm.
    /// </summary>
    public class CustomerType : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã phân loại khách hàng (Ví dụ: SI, LE, HORECA).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của phân loại (Ví dụ: Khách sỉ, Khách lẻ, Đại lý cấp 1).</summary>
        public required string Name { get; set; }
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Mô tả chi tiết về đặc điểm hoặc điều kiện để được xếp vào nhóm khách hàng này.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang sử dụng, false: Tạm khóa/Ngừng áp dụng).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Danh sách các khách hàng hiện đang trực thuộc phân loại (nhóm) này.</summary>
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        #endregion
    }
}