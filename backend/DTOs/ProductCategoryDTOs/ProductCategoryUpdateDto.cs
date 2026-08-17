using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.ProductCategoryDTOs
{
    /// <summary>
    /// DTO cập nhật Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public class ProductCategoryUpdateDto
    {
        [Required(ErrorMessage = "Mã danh mục không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã danh mục tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
        public string? ImagePath { get; set; }

        public int? CategoryGroupId { get; set; }

        /// <summary>Trạng thái hoạt động</summary>
        public bool IsActive { get; set; } = true;
    }
}
