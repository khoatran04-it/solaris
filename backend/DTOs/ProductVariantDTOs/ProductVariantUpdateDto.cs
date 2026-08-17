using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.ProductVariantDTOs
{
    public class ProductVariantUpdateDto
    {
        [Required(ErrorMessage = "Mã SKU không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên biến thể không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên biến thể tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
        public string? ImagePath { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Mức tồn an toàn phải >= 0.")]
        public int InventoryGuideline { get; set; }

        [Required(ErrorMessage = "Sản phẩm gốc không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Sản phẩm gốc không hợp lệ.")]
        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<AttributeInputDto> Attributes { get; set; } = new();
        public List<VariantPriceInputDto> Prices { get; set; } = new();
    }
}