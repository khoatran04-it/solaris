namespace backend.DTOs.UoMCategoryDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Nhóm Đơn vị tính kèm thông tin ĐVT cơ sở.
    /// </summary>
    public class UoMCategoryReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        #region Thông tin Đơn vị tính cơ sở (Base UoM)
        /// <summary>Mã định danh ĐVT cơ sở làm mốc của nhóm.</summary>
        public int? BaseUoMId { get; set; }

        /// <summary>Mã viết tắt ĐVT cơ sở (Ví dụ: KG, L, PCS).</summary>
        public string? BaseUoMCode { get; set; }

        /// <summary>Tên hiển thị ĐVT cơ sở (Ví dụ: Kilogram, Lít, Cái).</summary>
        public string? BaseUoMName { get; set; }
        #endregion
    }
}