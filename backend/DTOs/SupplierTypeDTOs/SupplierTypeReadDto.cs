namespace backend.DTOs.SupplierTypeDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Loại nhà cung cấp.
    /// </summary>
    public class SupplierTypeReadDto
    {
        public int Id { get; set; }

        /// <summary>Mã loại nhà cung cấp (Ví dụ: FARM, DISTRIBUTOR).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị phân loại.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết.</summary>
        public string? Description { get; set; }

        /// <summary>Trạng thái hoạt động.</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}