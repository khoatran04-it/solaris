namespace backend.DTOs.UoMDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Đơn vị tính.
    /// </summary>
    public class UoMCreateDto
    {
        /// <summary>Mã viết tắt của ĐVT (Ví dụ: KG, G, TON, BOX, PCS).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị đầy đủ của ĐVT (Ví dụ: Kilogram, Hộp, Thùng, Cái).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Từ khóa tìm kiếm hoặc tên gọi đồng nghĩa (Ví dụ: "ký, cân, kilogam").</summary>
        public string? Synonyms { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Mã định danh của Nhóm ĐVT trực thuộc (Ví dụ: Nhóm Khối lượng, Nhóm Đóng gói).</summary>
        public int CategoryId { get; set; }
    }
}