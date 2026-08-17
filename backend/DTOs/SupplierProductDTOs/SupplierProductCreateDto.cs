using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.SupplierProductDTOs
{
    public class SupplierProductCreateDto
    {
        [StringLength(100, ErrorMessage = "Mã SKU của NCC tối đa 100 ký tự.")]
        public string? SupplierSKU { get; set; }

        [Required(ErrorMessage = "Đơn giá nhập không được để trống.")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá nhập phải lớn hơn hoặc bằng 0.")]
        public decimal LastImportPrice { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng đặt tối thiểu (MOQ) phải lớn hơn 0.")]
        public decimal MinimumOrderQuantity { get; set; } = 1m;

        [Range(0, 365, ErrorMessage = "Thời gian giao hàng (Lead Time) phải từ 0 đến 365 ngày.")]
        public int LeadTimeDays { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn biến thể sản phẩm.")]
        public int VariantId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính mua hàng.")]
        public int PurchaseUoMId { get; set; }
    }
}
