using backend.DTOs.SupplierAddressDTOs;

namespace backend.DTOs.SupplierDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Hồ sơ Nhà cung cấp (bao gồm cả danh sách địa chỉ giao nhận).
    /// </summary>
    public class SupplierReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã nhà cung cấp (Ví dụ: SUP-DALAT-001).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên đơn vị / Tên công ty cung cấp.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Đường dẫn logo hoặc ảnh đại diện của nhà cung cấp.</summary>
        public string? LogoPath { get; set; }
        #endregion

        #region Thông tin Liên hệ
        /// <summary>Số điện thoại liên hệ chính.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Địa chỉ Email liên hệ chính.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Website chính thức của doanh nghiệp.</summary>
        public string? Website { get; set; }

        /// <summary>Liên kết mạng xã hội hoặc Fanpage.</summary>
        public string? SocialLink { get; set; }
        #endregion

        #region Thông tin Thuế & Thanh toán
        /// <summary>Mã số thuế doanh nghiệp.</summary>
        public string? TaxCode { get; set; }

        /// <summary>Số tài khoản ngân hàng giao dịch.</summary>
        public string? BankAccount { get; set; }

        /// <summary>Tên ngân hàng và chi nhánh.</summary>
        public string? BankName { get; set; }
        #endregion

        #region Phân loại & Ghi chú
        /// <summary>Mã định danh Loại nhà cung cấp.</summary>
        public int? SupplierTypeId { get; set; }

        /// <summary>Tên hiển thị Loại nhà cung cấp (Lấy từ bảng SupplierType).</summary>
        public string? SupplierTypeName { get; set; }

        /// <summary>Ghi chú đặc thù, đánh giá hoặc lưu ý nội bộ về nhà cung cấp này.</summary>
        public string? Note { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hợp tác, false: Ngừng giao dịch).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Liên kết dữ liệu (Navigation Properties)
        /// <summary>Danh sách các địa chỉ lấy/giao hàng của nhà cung cấp.</summary>
        public List<SupplierAddressReadDto> Addresses { get; set; } = new();
        #endregion
    }
}