namespace backend.Models
{
    /// <summary>
    /// Thực thể Đơn vị tính (Ví dụ: Kilogram, Gram, Tấn, Thùng, Hộp, Chai, Vỉ, Quả, Bó).
    /// </summary>
    public class UoM : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã viết tắt của ĐVT (Ví dụ: KG, G, TON, BOX, PCS)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của ĐVT (Ví dụ: Kilogram, Thùng, Hộp, Cái)</summary>
        public required string Name { get; set; }

        /// <summary>Các tên gọi đồng nghĩa hoặc từ khóa tìm kiếm (Ví dụ: "ký, cân, kilogam")</summary>
        public string? Synonyms { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY ---
        /// <summary>Thuộc về Nhóm ĐVT nào</summary>
        public int? CategoryId { get; set; }
        public virtual UoMCategory? Category { get; set; }
    }
}