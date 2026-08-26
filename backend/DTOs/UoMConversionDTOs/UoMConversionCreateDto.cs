namespace backend.DTOs.UoMConversionDTOs
{
    /// <summary>
    /// DTO yêu cầu thiết lập quy tắc Quy đổi Đơn vị tính mới.
    /// </summary>
    public class UoMConversionCreateDto
    {
        /// <summary>
        /// Mã định danh sản phẩm nếu là quy đổi đặc thù theo mặt hàng (để trống/null nếu là quy chuẩn toàn hệ thống).
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>Mã định danh Đơn vị tính nguồn / gốc (ĐVT lớn hơn hoặc cần quy đổi).</summary>
        public int FromUoMId { get; set; }

        /// <summary>Mã định danh Đơn vị tính đích / cơ sở (ĐVT chuẩn nhận giá trị quy đổi).</summary>
        public int ToUoMId { get; set; }

        /// <summary>
        /// Hệ số nhân quy đổi toán học: [Số lượng FromUoM] * ConversionFactor = [Số lượng ToUoM].
        /// </summary>
        public decimal ConversionFactor { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Cờ hiệu logic: true nếu là quy đổi tiêu chuẩn chung của hệ thống.</summary>
        public bool IsStandard => ProductId == null;
    }
}