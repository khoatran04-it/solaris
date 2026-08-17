using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.CustomerAddressDTOs
{
    /// <summary>
    /// DTO tạo mới Địa chỉ khách hàng.
    /// </summary>
    public class CustomerAddressCreateDto
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Tên người nhận không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên người nhận tối đa 100 ký tự.")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại nhận hàng không được để trống.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string Phone { get; set; } = string.Empty;

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

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
