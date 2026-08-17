using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.SupplierTypeDTOs
{
    /// <summary>
    /// DTO cập nhật Loại nhà cung cấp.
    /// </summary>
    public class SupplierTypeUpdateDto
    {
        [Required(ErrorMessage = "Mã phân loại không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã phân loại tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên phân loại không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên phân loại tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động (true: Hoạt động, false: Tạm khóa)</summary>
        public bool IsActive { get; set; } = true;
    }
}
