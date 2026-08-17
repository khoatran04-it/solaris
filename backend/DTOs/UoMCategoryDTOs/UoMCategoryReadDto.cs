namespace backend.DTOs.UoMCategoryDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Nhóm Đơn vị tính.
    /// </summary>
    public class UoMCategoryReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int? BaseUoMId { get; set; }
        public string? BaseUoMName { get; set; }
    }
}
