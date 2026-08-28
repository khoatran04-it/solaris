namespace backend.Models
{
    /// <summary>
    /// Thực thể Địa chỉ kho / bến bãi giao nhận hàng của Nhà Cung Cấp.
    /// Hỗ trợ thiết kế một nhà cung cấp có thể có nhiều điểm lấy/giao hàng khác nhau.
    /// </summary>
    public class SupplierAddress : ISoftDelete
    {
        public int Id { get; set; }

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Nhà cung cấp sở hữu địa chỉ này.</summary>
        public int SupplierId { get; set; }

        /// <summary>Thực thể Nhà cung cấp trực thuộc.</summary>
        public virtual Supplier? Supplier { get; set; }
        #endregion

        #region Thông tin liên hệ
        /// <summary>Tên người liên hệ phụ trách tại điểm giao nhận này.</summary>
        public required string ContactName { get; set; }

        /// <summary>Số điện thoại liên lạc của người phụ trách điểm giao nhận.</summary>
        public required string ContactPhone { get; set; }
        #endregion

        #region Cấu trúc địa chỉ hành chính (Address Structure)
        /// <summary>Tỉnh / Thành phố trực thuộc trung ương.</summary>
        public required string Province { get; set; }

        /// <summary>Quận / Huyện / Thị xã / Thành phố thuộc tỉnh.</summary>
        public required string District { get; set; }

        /// <summary>Phường / Xã / Thị trấn.</summary>
        public required string Ward { get; set; }

        /// <summary>Số nhà, tên đường, ngõ hẻm hoặc thôn xóm.</summary>
        public required string StreetAddress { get; set; }

        /// <summary>Địa chỉ đầy đủ (Tự động ghép từ các cấp hành chính để thuận tiện hiển thị).</summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Cờ đánh dấu đây là địa chỉ lấy/giao hàng mặc định của nhà cung cấp.</summary>
        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion
    }
}