namespace backend.DTOs.UoMDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Đơn vị tính.
    /// </summary>
    public class UoMReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Synonyms { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
