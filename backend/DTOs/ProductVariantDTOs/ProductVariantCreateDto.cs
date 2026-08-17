using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.ProductVariantDTOs
{
    public class VariantPriceInputDto
    {
        [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Đơn vị tính không hợp lệ.")]
        public int UoMId { get; set; }

        [Required(ErrorMessage = "Giá bán không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        public bool IsDefault { get; set; }
    }

    public class AttributeInputDto
    {
        [Required(ErrorMessage = "Thuộc tính không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Thuộc tính không hợp lệ.")]
        public int AttributeDefinitionId { get; set; }

        [Required(ErrorMessage = "Giá trị thuộc tính không được để trống.")]
        [StringLength(200, ErrorMessage = "Giá trị thuộc tính tối đa 200 ký tự.")]
        public string AttributeValue { get; set; } = string.Empty;
    }

    public class ProductVariantCreateDto
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