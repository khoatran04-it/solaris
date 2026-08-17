namespace backend.Models
{
    /// <summary>
    /// Thực thể Phân loại Khách hàng (Customer Type).
    /// Ví dụ: Khách sỉ, Khách lẻ, Nhà hàng/Khách sạn, Đại lý F1...
    /// </summary>
    public class CustomerType : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã phân loại khách hàng (Ví dụ: SI, LE, HORECA)</summary>
        public required string Code { get; set; }

        /// <summary>Tên phân loại khách hàng</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết</summary>
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động (true: Hoạt động, false: Tạm khóa)</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}