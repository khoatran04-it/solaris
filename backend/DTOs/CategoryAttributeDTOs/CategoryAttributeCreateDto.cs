using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CategoryAttributeDTOs
{
    public class CategoryAttributeCreateDto
    {
        [Required(ErrorMessage = "Danh mục sản phẩm không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Thuộc tính không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Thuộc tính không hợp lệ.")]
        public int? AttributeDefinitionId { get; set; }

        public bool IsRequired { get; set; } = false;
    }
}
