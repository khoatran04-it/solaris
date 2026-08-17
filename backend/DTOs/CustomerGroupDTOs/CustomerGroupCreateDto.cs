using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CustomerGroupDTOs
{
    /// <summary>
    /// DTO tạo mới Nhóm Khách Hàng.
    /// </summary>
    public class CustomerGroupCreateDto
    {
        [Required(ErrorMessage = "Mã nhóm khách hàng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã nhóm tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhóm khách hàng không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhóm tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động (mặc định: true)</summary>
        public bool IsActive { get; set; } = true;
    }
}
