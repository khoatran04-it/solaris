using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.AttributeDefinitionDTOs
{
    public class AttributeDefinitionUpdateDto
    {
        [Required(ErrorMessage = "Tên thuộc tính không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên thuộc tính tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kiểu dữ liệu không được để trống.")]
        [StringLength(50, ErrorMessage = "Kiểu dữ liệu tối đa 50 ký tự.")]
        public string DataType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
