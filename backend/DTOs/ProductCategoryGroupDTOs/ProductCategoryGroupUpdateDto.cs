using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.ProductCategoryGroupDTOs
{
    /// <summary>
    /// DTO cập nhật Nhóm Ngành Hàng (Product Category Group).
    /// </summary>
    public class ProductCategoryGroupUpdateDto
    {
        [Required(ErrorMessage = "Mã nhóm ngành hàng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã nhóm ngành hàng tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhóm ngành hàng không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhóm ngành hàng tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [StringLength(1000, ErrorMessage = "Đường dẫn ảnh tối đa 1000 ký tự.")]
        public string? ImagePath { get; set; }

        /// <summary>Trạng thái hoạt động</summary>
        public bool IsActive { get; set; } = true;
    }
}
