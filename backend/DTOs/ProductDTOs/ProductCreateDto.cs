using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.ProductDTOs
{
    /// <summary>
    /// DTO tạo mới Sản Phẩm Gốc (Product Master).
    /// </summary>
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã sản phẩm tối đa 50 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [StringLength(1000, ErrorMessage = "Đường dẫn ảnh tối đa 1000 ký tự.")]
        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính cơ sở.")]
        [Range(1, int.MaxValue, ErrorMessage = "Đơn vị tính cơ sở không hợp lệ.")]
        public int BaseUoMId { get; set; }
    }
}
