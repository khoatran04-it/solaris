namespace backend.DTOs.UoMConversionDTOs
{
    /// <summary>
    /// DTO chứa thông tin Đơn vị tính hợp lệ của một Biến thể sản phẩm (SKU) kèm tỷ lệ quy đổi về Base UoM.
    /// Dùng để cấp danh sách lựa chọn động (0% hardcode) cho Frontend ở tất cả các form (PO, Nhập, Xuất, Chuyển kho).
    /// </summary>
    public class ValidUoMOptionDto
    {
        /// <summary>Mã định danh ĐVT.</summary>
        public int UoMId { get; set; }

        /// <summary>Tên hiển thị của ĐVT (Ví dụ: Thùng, Bao, Khay, Kg, Gói).</summary>
        public string UoMName { get; set; } = string.Empty;

        /// <summary>Mã viết tắt ĐVT (Ví dụ: CTN, BAG, KG, PACK).</summary>
        public string UoMCode { get; set; } = string.Empty;

        /// <summary>
        /// Hệ số nhân quy đổi về Đơn vị tính cơ sở (Base UoM):
        /// [Số lượng theo ĐVT này] * ConversionFactorToBase = [Số lượng theo Base UoM].
        /// Ví dụ: 1 Thùng = 30 Gói -> ConversionFactorToBase = 30.
        /// </summary>
        public decimal ConversionFactorToBase { get; set; } = 1;

        /// <summary>Cờ đánh dấu đây có phải là ĐVT cơ sở hạt nhân của sản phẩm hay không.</summary>
        public bool IsBaseUoM { get; set; }

        /// <summary>Mô tả trực quan (Ví dụ: "⚡ 1 Thùng = 30 Gói" hoặc "Đơn vị cơ sở").</summary>
        public string Description { get; set; } = string.Empty;
    }
}
