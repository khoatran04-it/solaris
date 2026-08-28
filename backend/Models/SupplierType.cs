namespace backend.Models
{
    /// <summary>
    /// Thực thể Loại Nhà Cung Cấp (Ví dụ: Nông hộ trực tiếp, Hợp tác xã, Tổng kho phân phối, Nhà máy chế biến).
    /// Dùng để phân loại và quản lý các nhóm nhà cung cấp khác nhau trong hệ thống.
    /// </summary>
    public class SupplierType : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã loại nhà cung cấp viết hoa không dấu (Ví dụ: FARM, DISTRIBUTOR, FACTORY).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của loại nhà cung cấp (Ví dụ: Nông hộ trực tiếp, Hợp tác xã).</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết về phân loại nhà cung cấp này.</summary>
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động (true: Đang sử dụng, false: Tạm khóa).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Danh sách liên kết (Navigation Properties)
        /// <summary>Danh sách các nhà cung cấp thuộc phân loại này.</summary>
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        #endregion
    }
}