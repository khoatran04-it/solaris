namespace backend.Models
{
    /// <summary>
    /// Thực thể Khách Hàng (B2C & B2B).
    /// Quản lý hồ sơ khách hàng, phân hạng tích lũy (CustomerTier) để tự động chiết khấu, phân loại và địa chỉ nhận hàng.
    /// </summary>
    public class Customer : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã khách hàng (Ví dụ: CUST-2026-001)</summary>
        public required string Code { get; set; }

        /// <summary>Tên khách hàng / Tên doanh nghiệp đối tác</summary>
        public required string Name { get; set; }

        /// <summary>Số điện thoại chính để tra cứu và liên hệ</summary>
        public required string PhoneNumber { get; set; }

        /// <summary>Địa chỉ Email</summary>
        public string? Email { get; set; }

        /// <summary>Mã số thuế (với khách hàng doanh nghiệp B2B)</summary>
        public string? TaxCode { get; set; }

        /// <summary>Ảnh đại diện khách hàng</summary>
        public string? AvatarPath { get; set; }

        /// <summary>Ngày sinh nhật</summary>
        public DateTime? Birthday { get; set; }

        /// <summary>Giới tính (true: Nam, false: Nữ, null: Khác)</summary>
        public bool? Gender { get; set; }

        /// <summary>Ghi chú chăm sóc khách hàng</summary>
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>ID Phân loại khách hàng (Cá nhân, Doanh nghiệp, Đại lý)</summary>
        public int? CustomerTypeId { get; set; }
        public virtual CustomerType? CustomerType { get; set; }

        /// <summary>ID Hạng thành viên (Đồng, Bạc, Vàng, Kim Cương) để tự động áp dụng % chiết khấu</summary>
        public int? CustomerTierId { get; set; }
        public virtual CustomerTier? CustomerTier { get; set; }

        /// <summary>Sổ địa chỉ giao hàng của khách hàng</summary>
        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

        /// <summary>Danh sách nhóm khách hàng tham gia</summary>
        public virtual ICollection<CustomerGroupLink> GroupLinks { get; set; } = new List<CustomerGroupLink>();
    }
}