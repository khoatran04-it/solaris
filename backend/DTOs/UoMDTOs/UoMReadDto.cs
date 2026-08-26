namespace backend.DTOs.UoMDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Đơn vị tính kèm thông tin Nhóm trực thuộc.
    /// </summary>
    public class UoMReadDto
    {
        public int Id { get; set; }

        /// <summary>Mã viết tắt của ĐVT (Ví dụ: KG, G, BOX, PCS).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị đầy đủ của ĐVT (Ví dụ: Kilogram, Hộp, Thùng).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Từ khóa tìm kiếm hoặc tên gọi đồng nghĩa (Ví dụ: "ký, cân, kilogam").</summary>
        public string? Synonyms { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        #region Thông tin Nhóm ĐVT (Category)
        /// <summary>Mã định danh Nhóm ĐVT trực thuộc.</summary>
        public int? CategoryId { get; set; }

        /// <summary>Mã viết tắt Nhóm ĐVT (Ví dụ: WEIGHT, VOLUME, PACKAGING).</summary>
        public string? CategoryCode { get; set; }

        /// <summary>Tên hiển thị Nhóm ĐVT (Ví dụ: Nhóm Khối lượng, Nhóm Đóng gói).</summary>
        public string? CategoryName { get; set; }
        #endregion
    }
}