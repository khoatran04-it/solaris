namespace backend.DTOs.SupplierAddressDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật Địa chỉ kho của Nhà cung cấp.
    /// </summary>
    public class SupplierAddressUpdateDto
    {
        /// <summary>Tên người liên hệ tại điểm giao nhận.</summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>Số điện thoại người liên hệ.</summary>
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>Tỉnh / Thành phố trực thuộc trung ương.</summary>
        public string Province { get; set; } = string.Empty;

        /// <summary>Quận / Huyện / Thị xã / Thành phố thuộc tỉnh.</summary>
        public string District { get; set; } = string.Empty;

        /// <summary>Phường / Xã / Thị trấn.</summary>
        public string Ward { get; set; } = string.Empty;

        /// <summary>Số nhà, tên đường, ngõ hẻm hoặc thôn xóm.</summary>
        public string StreetAddress { get; set; } = string.Empty;

        /// <summary>Cờ đánh dấu đây là địa chỉ lấy hàng mặc định của nhà cung cấp.</summary>
        public bool IsDefault { get; set; } = false;
    }
}