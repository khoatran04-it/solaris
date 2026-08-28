namespace backend.DTOs.SupplierAddressDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Địa chỉ giao nhận / kho của Nhà cung cấp.
    /// </summary>
    public class SupplierAddressReadDto
    {
        public int Id { get; set; }

        /// <summary>Mã định danh Nhà cung cấp trực thuộc.</summary>
        public int SupplierId { get; set; }

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

        /// <summary>Địa chỉ đầy đủ (Tự động ghép từ các cấp hành chính).</summary>
        public string? FullAddress { get; set; }

        /// <summary>Cờ đánh dấu đây là địa chỉ lấy hàng mặc định của nhà cung cấp.</summary>
        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}