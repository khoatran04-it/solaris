using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CustomerTierDTOs
{
    /// <summary>
    /// DTO tạo mới Bậc hạng khách hàng.
    /// </summary>
    public class CustomerTierCreateDto
    {
        [Required(ErrorMessage = "Mã bậc hạng không được để trống.")]
        [StringLength(20, ErrorMessage = "Mã bậc hạng tối đa 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên bậc hạng không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên bậc hạng tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Phần trăm chiết khấu phải từ 0 đến 100.")]
        public decimal DiscountPercent { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Mức chi tiêu tối thiểu không được âm.")]
        public decimal MinSpending { get; set; } = 0;

        /// <summary>Trạng thái hoạt động (mặc định: true - Hoạt động)</summary>
        public bool IsActive { get; set; } = true;
    }
}
