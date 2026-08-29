namespace backend.Models
{
    /// <summary>
    /// Thực thể Khách Hàng (Dùng chung cho cả B2C & B2B).
    /// Quản lý hồ sơ định danh, thông tin tài khoản đăng nhập (Web/App), phân hạng thành viên (Tier) để tự động chiết khấu, phân loại (Type) và sổ địa chỉ giao hàng.
    /// </summary>
    public class Customer : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Liên hệ
        /// <summary>Mã khách hàng tự sinh (Ví dụ: CUST-2026-001).</summary>
        public required string Code { get; set; }

        /// <summary>Tên đầy đủ của khách hàng cá nhân hoặc Tên doanh nghiệp đối tác.</summary>
        public required string Name { get; set; }

        /// <summary>Số điện thoại liên lạc chính (Dùng để giao hàng, tra cứu đơn hàng hoặc đăng nhập).</summary>
        public required string PhoneNumber { get; set; }

        /// <summary>Địa chỉ Email liên hệ.</summary>
        public string? Email { get; set; }
        #endregion

        #region Thông tin Chi tiết & Cá nhân hóa
        /// <summary>Mã số thuế (Áp dụng chủ yếu cho khách hàng doanh nghiệp B2B/Đại lý để xuất hóa đơn điện tử).</summary>
        public string? TaxCode { get; set; }

        /// <summary>Đường dẫn lưu trữ ảnh đại diện (Avatar) của khách hàng.</summary>
        public string? AvatarPath { get; set; }

        /// <summary>Ngày tháng năm sinh (Sử dụng cho các chiến dịch gửi mã khuyến mãi/chúc mừng dịp sinh nhật).</summary>
        public DateTime? Birthday { get; set; }

        /// <summary>Giới tính (true: Nam, false: Nữ, null: Không xác định hoặc Khác).</summary>
        public bool? Gender { get; set; }

        /// <summary>Ghi chú nội bộ dành riêng cho nhân viên chăm sóc khách hàng (Telesale, CSKH).</summary>
        public string? Note { get; set; }
        #endregion

        #region Thông tin Xác thực (Tài khoản Web/App)
        /// <summary>Tên đăng nhập vào hệ thống Cửa hàng trực tuyến (Thường sử dụng Số điện thoại hoặc Email).</summary>
        public string? Username { get; set; }

        /// <summary>Mật khẩu đăng nhập Cửa hàng đã được mã hóa một chiều (Bcrypt/Argon2).</summary>
        public string? PasswordHash { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Cho phép đăng nhập/mua hàng, false: Khóa tài khoản).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh Phân loại khách hàng (Ví dụ: Khách sỉ, Khách lẻ, HORECA).</summary>
        public int? CustomerTypeId { get; set; }

        /// <summary>Thực thể Phân loại khách hàng.</summary>
        public virtual CustomerType? CustomerType { get; set; }

        /// <summary>Mã định danh Hạng thành viên (Ví dụ: Đồng, Bạc, Vàng, Kim Cương) - Dùng để hệ thống tự động tính % chiết khấu khi lên đơn.</summary>
        public int? CustomerTierId { get; set; }

        /// <summary>Thực thể Hạng thành viên.</summary>
        public virtual CustomerTier? CustomerTier { get; set; }

        /// <summary>Sổ địa chỉ: Danh sách các địa chỉ nhận hàng lưu sẵn của khách hàng này.</summary>
        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

        /// <summary>Danh sách các thẻ/nhóm (Tags/Groups) tiếp thị mà khách hàng này đang được gán vào.</summary>
        public virtual ICollection<CustomerGroupLink> GroupLinks { get; set; } = new List<CustomerGroupLink>();
        #endregion
    }
}