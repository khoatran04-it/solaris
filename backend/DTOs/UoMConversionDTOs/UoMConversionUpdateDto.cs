using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.UoMConversionDTOs
{
    /// <summary>
    /// DTO cập nhật quy tắc Quy đổi Đơn vị tính.
    /// </summary>
    public class UoMConversionUpdateDto
    {
        public bool IsActive { get; set; } = true;

        /// <summary>ID sản phẩm (để trống nếu là quy đổi tiêu chuẩn toàn hệ thống)</summary>
        public int? ProductId { get; set; }

        /// <summary>ID đơn vị tính nguồn</summary>
        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính gốc.")]
        public int FromUoMId { get; set; }

        /// <summary>ID đơn vị tính đích / cơ sở</summary>
        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính đích.")]
        public int ToUoMId { get; set; }

        /// <summary>Hệ số quy đổi dương (> 0)</summary>
        [Required(ErrorMessage = "Vui lòng nhập hệ số quy đổi.")]
        [Range(0.000001, 999999999, ErrorMessage = "Hệ số quy đổi phải lớn hơn 0.")]
        public decimal ConversionFactor { get; set; }

        public bool IsStandard => ProductId == null;
    }
}
