namespace backend.DTOs.UoMCategoryDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Nhóm Đơn vị tính.
    /// </summary>
    public class UoMCategoryCreateDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>Mã định danh của Đơn vị tính cơ sở làm mốc quy chiếu trong nhóm (có thể cập nhật sau).</summary>
        public int? BaseUoMId { get; set; }
    }
}