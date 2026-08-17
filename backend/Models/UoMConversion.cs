namespace backend.Models
{
    /// <summary>
    /// Thực thể Quy tắc Quy đổi Đơn vị tính (UoM Conversion).
    /// Hỗ trợ 2 chế độ:
    /// 1. Quy đổi tiêu chuẩn toàn hệ thống: Khi ProductId = null (Ví dụ: 1 Tấn = 1000 Kg).
    /// 2. Quy đổi đặc thù theo từng sản phẩm: Khi ProductId != null (Ví dụ: 1 Thùng Táo = 24 Quả, 1 Thùng Sữa = 48 Hộp).
    /// </summary>
    public class UoMConversion : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Hệ số nhân quy đổi: [Số lượng Đơn vị Gốc (FromUoM)] * [ConversionFactor] = [Số lượng Đơn vị Đích (ToUoM)]</summary>
        public decimal ConversionFactor { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY ---
        /// <summary>ID sản phẩm nếu là quy đổi đặc thù (null = quy đổi tiêu chuẩn toàn hệ thống)</summary>
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        /// <summary>Đơn vị tính nguồn / gốc</summary>
        public int? FromUoMId { get; set; }
        public virtual UoM? FromUoM { get; set; }

        /// <summary>Đơn vị tính đích / cơ sở</summary>
        public int? ToUoMId { get; set; }
        public virtual UoM? ToUoM { get; set; }

        /// <summary>Thuộc tính logic: True nếu là quy đổi tiêu chuẩn chung của hệ thống</summary>
        public bool IsStandard => ProductId == null;
    }
}