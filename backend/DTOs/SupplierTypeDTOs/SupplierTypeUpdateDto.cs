namespace backend.DTOs.SupplierTypeDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Loại nhà cung cấp.
    /// </summary>
    public class SupplierTypeUpdateDto
    {
        /// <summary>Mã loại nhà cung cấp (Ví dụ: FARM, DISTRIBUTOR).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị phân loại.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết.</summary>
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động (true: Đang sử dụng, false: Tạm khóa).</summary>
        public bool IsActive { get; set; } = true;
    }
}