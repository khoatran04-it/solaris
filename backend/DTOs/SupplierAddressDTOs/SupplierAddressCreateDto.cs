using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.SupplierAddressDTOs
{
    /// <summary>
    /// DTO tạo mới Địa chỉ kho của Nhà cung cấp.
    /// </summary>
    public class SupplierAddressCreateDto
    {
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Tên người liên hệ không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên người liên hệ tối đa 100 ký tự.")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại liên hệ không được để trống.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống.")]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố tối đa 100 ký tự.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện không được để trống.")]
        [StringLength(100, ErrorMessage = "Quận/Huyện tối đa 100 ký tự.")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã không được để trống.")]
        [StringLength(100, ErrorMessage = "Phường/Xã tối đa 100 ký tự.")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
        [StringLength(200, ErrorMessage = "Địa chỉ chi tiết tối đa 200 ký tự.")]
        public string StreetAddress { get; set; } = string.Empty;

        /// <summary>Có phải địa chỉ lấy hàng mặc định không</summary>
        public bool IsDefault { get; set; } = false;
    }
}
