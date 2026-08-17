namespace backend.DTOs.SupplierTypeDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Loại nhà cung cấp.
    /// </summary>
    public class SupplierTypeReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
