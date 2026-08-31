using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CategoryAttributeDTOs
{
    /// <summary>
    /// DTO đồng bộ hàng loạt các thuộc tính cho một Danh mục sản phẩm (Bulk Sync Matrix).
    /// </summary>
    public class CategoryAttributeSyncDto
    {
        [Required(ErrorMessage = "Mã danh mục không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã danh mục không hợp lệ.")]
        public int CategoryId { get; set; }

        /// <summary>
        /// Danh sách các thuộc tính được chọn gán kèm cờ Bắt buộc / Tùy chọn.
        /// </summary>
        public List<CategoryAttributeSyncItemDto> Attributes { get; set; } = new();
    }

    public class CategoryAttributeSyncItemDto
    {
        [Required(ErrorMessage = "Mã định nghĩa thuộc tính không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã thuộc tính không hợp lệ.")]
        public int AttributeDefinitionId { get; set; }

        public bool IsRequired { get; set; } = false;
    }
}
