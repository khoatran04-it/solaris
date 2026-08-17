using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.SupplierDTOs
{
    /// <summary>
    /// DTO cập nhật thông tin Nhà cung cấp.
    /// </summary>
    public class SupplierUpdateDto
    {
        [Required(ErrorMessage = "Mã nhà cung cấp không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã nhà cung cấp tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhà cung cấp tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        public string? LogoPath { get; set; }

        [StringLength(20, ErrorMessage = "Mã số thuế tối đa 20 ký tự.")]
        public string? TaxCode { get; set; }

        public string? Website { get; set; }
        public string? SocialLink { get; set; }

        [StringLength(50, ErrorMessage = "Số tài khoản ngân hàng tối đa 50 ký tự.")]
        public string? BankAccount { get; set; }

        [StringLength(100, ErrorMessage = "Tên ngân hàng tối đa 100 ký tự.")]
        public string? BankName { get; set; }

        public string? Note { get; set; }

        /// <summary>Trạng thái hoạt động (true: Hoạt động, false: Tạm khóa)</summary>
        public bool IsActive { get; set; } = true;

        // --- FOREIGN KEY ---
        /// <summary>ID Loại nhà cung cấp</summary>
        public int? SupplierTypeId { get; set; }
    }
}