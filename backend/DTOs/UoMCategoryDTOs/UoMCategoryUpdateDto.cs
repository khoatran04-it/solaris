namespace backend.DTOs.UoMCategoryDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Nhóm Đơn vị tính.
    /// </summary>
    public class UoMCategoryUpdateDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>Mã định danh của Đơn vị tính cơ sở làm mốc quy chiếu mới trong nhóm.</summary>
        public int? BaseUoMId { get; set; }
    }
}