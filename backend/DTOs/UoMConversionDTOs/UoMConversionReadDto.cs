namespace backend.DTOs.UoMConversionDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết quy tắc quy đổi Đơn vị tính kèm diễn giải ngữ nghĩa trực quan (Semantic Description).
    /// </summary>
    public class UoMConversionReadDto
    {
        public int Id { get; set; }

        public bool IsActive { get; set; }

        public decimal ConversionFactor { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        #region Thông tin Sản phẩm (Áp dụng cho quy đổi đặc thù)
        /// <summary>Mã định danh sản phẩm (null nếu là quy đổi chuẩn hệ thống).</summary>
        public int? ProductId { get; set; }

        /// <summary>Mã sản phẩm (SKU/Code).</summary>
        public string? ProductCode { get; set; }

        /// <summary>Tên hiển thị sản phẩm.</summary>
        public string? ProductName { get; set; }
        #endregion

        #region Thông tin ĐVT Nguồn (From UoM)
        /// <summary>Mã định danh ĐVT nguồn.</summary>
        public int FromUoMId { get; set; }

        /// <summary>Mã viết tắt ĐVT nguồn (Ví dụ: BOX, TON).</summary>
        public string FromUoMCode { get; set; } = string.Empty;

        /// <summary>Tên hiển thị ĐVT nguồn (Ví dụ: Thùng, Tấn).</summary>
        public string FromUoMName { get; set; } = string.Empty;
        #endregion

        #region Thông tin ĐVT Đích (To UoM)
        /// <summary>Mã định danh ĐVT đích.</summary>
        public int ToUoMId { get; set; }

        /// <summary>Mã viết tắt ĐVT đích (Ví dụ: BTL, KG).</summary>
        public string ToUoMCode { get; set; } = string.Empty;

        /// <summary>Tên hiển thị ĐVT đích (Ví dụ: Chai, Kg).</summary>
        public string ToUoMName { get; set; } = string.Empty;
        #endregion

        #region Thuộc tính tính toán (Computed Properties)
        /// <summary>Cờ hiệu: true nếu là quy đổi tiêu chuẩn toàn hệ thống, false nếu là quy đổi theo sản phẩm.</summary>
        public bool IsStandard => ProductId == null;

        /// <summary>Diễn giải công thức trực quan cho người dùng (Ví dụ: "1 Thùng = 24 Chai" hoặc "1 Tấn = 1000 Kg").</summary>
        public string SemanticDescription => $"1 {FromUoMName} = {ConversionFactor:0.######} {ToUoMName}";
        #endregion
    }
}