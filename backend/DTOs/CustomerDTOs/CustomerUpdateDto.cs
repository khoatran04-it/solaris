using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CustomerDTOs
{
    /// <summary>
    /// DTO cập nhật Khách Hàng.
    /// </summary>
    public class CustomerUpdateDto
    {
        [Required(ErrorMessage = "Mã khách hàng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã khách hàng tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên khách hàng tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "Mã số thuế tối đa 20 ký tự.")]
        public string? TaxCode { get; set; }

        public string? AvatarPath { get; set; }
        public DateTime? Birthday { get; set; }
        public bool? Gender { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        public int? CustomerTypeId { get; set; }
        public int? CustomerTierId { get; set; }

        // Danh sách nhóm mới (sẽ ghi đè danh sách cũ khi Update)
        public List<int> GroupIds { get; set; } = new List<int>();
    }
}
