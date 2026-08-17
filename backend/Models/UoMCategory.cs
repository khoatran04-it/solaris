namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Đơn vị tính (Ví dụ: Nhóm Khối lượng, Nhóm Dung tích, Nhóm Đếm bao gói).
    /// Mỗi nhóm sở hữu một Đơn vị tính cơ sở (Base UoM) dùng làm mốc quy chiếu toán học.
    /// </summary>
    public class UoMCategory : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã nhóm đơn vị tính (Ví dụ: WEIGHT, VOLUME, PACKAGING)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị nhóm đơn vị tính (Ví dụ: Nhóm Khối Lượng, Nhóm Thể Tích)</summary>
        public required string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY ---
        /// <summary>ID của Đơn vị tính cơ sở làm mốc trong nhóm (Ví dụ: Kg trong nhóm Khối lượng)</summary>
        public int? BaseUoMId { get; set; }
        public virtual UoM? BaseUoM { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<UoM> UoMs { get; set; } = new List<UoM>();
    }
}