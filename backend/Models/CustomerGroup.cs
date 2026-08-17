namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Khách Hàng (Customer Group / Marketing Tag).
    /// Hỗ trợ phân loại khách hàng theo nhóm tiếp thị, phân khúc chiến dịch.
    /// </summary>
    public class CustomerGroup : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã nhóm khách hàng (Ví dụ: VIP, WHOLESALE, RETAIL, LOYAL)</summary>
        public required string Code { get; set; }

        /// <summary>Tên nhóm khách hàng</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết về tiêu chí của nhóm</summary>
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<CustomerGroupLink> GroupLinks { get; set; } = new List<CustomerGroupLink>();
    }
}