namespace backend.Models
{
    /// <summary>
    /// Thực thể Bảng Trung Gian (Many-to-Many Join Table) liên kết giữa Khách Hàng (Customer) và Nhóm Khách Hàng (CustomerGroup).
    /// Cho phép một khách hàng có thể được gắn nhiều thẻ/nhóm (Tags/Groups) khác nhau phục vụ cho marketing, và ngược lại.
    /// </summary>
    public class CustomerGroupLink : ISoftDelete
    {
        #region Liên kết đối tượng (Foreign Keys)
        /// <summary>Mã định danh của Khách hàng.</summary>
        public int CustomerId { get; set; }

        /// <summary>Thực thể Khách hàng.</summary>
        public virtual Customer? Customer { get; set; }

        /// <summary>Mã định danh của Nhóm khách hàng.</summary>
        public int CustomerGroupId { get; set; }

        /// <summary>Thực thể Nhóm khách hàng.</summary>
        public virtual CustomerGroup? CustomerGroup { get; set; }
        #endregion

        #region Thông tin Gán nhóm
        /// <summary>Thời điểm hệ thống hoặc nhân viên thực hiện gán khách hàng vào nhóm này.</summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion
    }
}