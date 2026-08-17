using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.UoMCategoryDTOs
{
    /// <summary>
    /// DTO tạo mới Nhóm Đơn vị tính.
    /// </summary>
    public class UoMCategoryCreateDto
    {
        [Required(ErrorMessage = "Mã nhóm ĐVT không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã nhóm ĐVT tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhóm ĐVT không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên nhóm ĐVT tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>ID của Đơn vị tính cơ sở làm mốc trong nhóm (có thể gán sau)</summary>
        public int? BaseUoMId { get; set; }
    }
}
