namespace backend.Models
{
    public class Customer : ISoftDelete
    {
        public int Id { get; set; }

        public required string Code { get; set; }

        public required string Name { get; set; }

        // Số điện thoại chính để tra cứu/đăng nhập
        public required string PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? TaxCode { get; set; }

        public string? AvatarPath { get; set; }

        public DateTime? Birthday { get; set; }

        public bool? Gender { get; set; } // true: Nam, false: Nữ

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // --- CÁC TRƯỜNG XÓA MỀM ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---

        // Khóa ngoại dạng Nullable (int?) để hỗ trợ Soft Delete từ bảng cha
        public int? CustomerTypeId { get; set; }
        public virtual CustomerType? CustomerType { get; set; }

        public int? CustomerTierId { get; set; }
        public virtual CustomerTier? CustomerTier { get; set; }

        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

        // 🔥 Đã đổi tên thành GroupLinks để sửa dứt điểm lỗi CS1061 ở Service
        public virtual ICollection<CustomerGroupLink> GroupLinks { get; set; } = new List<CustomerGroupLink>();
    }
}