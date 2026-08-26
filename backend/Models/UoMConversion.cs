namespace backend.Models
{
    /// <summary>
    /// Thực thể Quy tắc Quy đổi Đơn vị tính (UoM Conversion).
    /// Hỗ trợ 2 phạm vi quy đổi:
    /// 1. Quy đổi tiêu chuẩn toàn hệ thống (Global/Standard): Áp dụng chung khi ProductId = null (Ví dụ: 1 Tấn = 1.000 Kg, 1 Lít = 1.000 ml).
    /// 2. Quy đổi đặc thù theo sản phẩm (Product-specific): Gắn riêng cho từng mặt hàng khi ProductId != null (Ví dụ: 1 Thùng Táo Envy = 24 Quả, 1 Thùng Sữa TH = 48 Hộp).
    /// </summary>
    public class UoMConversion : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>
        /// Hệ số nhân quy đổi trực tiếp theo công thức toán học:
        /// [Số lượng ĐVT Nguồn (FromUoM)] * ConversionFactor = [Số lượng ĐVT Đích (ToUoM)].
        /// Ví dụ: 1 Tấn (From) = 1.000 (Factor) * 1 Kg (To) -> ConversionFactor = 1000.
        /// </summary>
        public decimal ConversionFactor { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Khóa ngoại & Liên kết dữ liệu (Foreign Keys)
        /// <summary>
        /// Mã định danh sản phẩm áp dụng quy đổi đặc thù (null = quy tắc chuẩn áp dụng toàn hệ thống).
        /// </summary>
        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        /// <summary>Mã định danh Đơn vị tính nguồn / gốc (ĐVT lớn hơn hoặc ĐVT cần quy đổi).</summary>
        public int? FromUoMId { get; set; }
        public virtual UoM? FromUoM { get; set; }

        /// <summary>Mã định danh Đơn vị tính đích / chuẩn (ĐVT nhỏ hơn hoặc ĐVT cơ sở để tính toán).</summary>
        public int? ToUoMId { get; set; }
        public virtual UoM? ToUoM { get; set; }
        #endregion

        #region Thuộc tính tính toán (Computed Properties)
        /// <summary>Cờ hiệu logic: true nếu là quy đổi tiêu chuẩn chung, false nếu là quy đổi riêng theo từng sản phẩm.</summary>
        public bool IsStandard => ProductId == null;
        #endregion
    }
}