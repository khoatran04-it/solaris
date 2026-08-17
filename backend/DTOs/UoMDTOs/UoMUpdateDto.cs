using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.UoMDTOs
{
    /// <summary>
    /// DTO cập nhật thông tin Đơn vị tính.
    /// </summary>
    public class UoMUpdateDto
    {
        [Required(ErrorMessage = "Mã ĐVT không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã ĐVT tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên ĐVT không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên ĐVT tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public string? Synonyms { get; set; }
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn Nhóm ĐVT.")]
        public int CategoryId { get; set; }
    }
}
