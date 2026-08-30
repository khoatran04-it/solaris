using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Đơn Bán Hàng (Sales Order Detail).
    /// Chứa thông tin về các mặt hàng khách đã chốt mua. 
    /// Đóng vai trò là cầu nối quan trọng để chuyển đổi từ Đơn vị mua hàng (UoM) 
    /// sang Đơn vị lưu kho cơ sở (Base UoM) nhằm đồng bộ với Core Engine Tồn kho.
    /// </summary>
    public class OrderDetail
    {
        public int Id { get; set; }

        #region Liên kết Đơn hàng gốc
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }
        #endregion

        #region Hàng hóa & Phân loại (Product Variant)
        /// <summary>Mã định danh Biến thể sản phẩm (SKU) khách hàng chọn mua.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// Đơn vị tính khách chọn mua lúc đặt hàng (Ví dụ: Thùng, Hộp, Bó, Kg).
        /// </summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Khối lượng & Quy đổi Kho (Inventory Mapping)
        /// <summary>
        /// Số lượng khách đặt theo Đơn vị tính (UoM) hiển thị trên Website. 
        /// (Ví dụ: Khách đặt 2 "Thùng" -> Quantity = 2).
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// NGHIỆP VỤ LÕI: Số lượng đã quy đổi về Đơn vị tính cơ sở (Base UoM).
        /// (Ví dụ: 2 Thùng x 10 Kg/Thùng = 20 Kg -> BaseQuantity = 20).
        /// Trái tim hệ thống (IInventoryService) bắt buộc phải dùng biến này để gọi hàm ReserveStock (Giữ chỗ) 
        /// nhằm tránh tuyệt đối tình trạng sai lệch số dư sổ cái Kế toán.
        /// </summary>
        public decimal BaseQuantity { get; set; }
        #endregion

        #region Tài chính & Snapshot Giá (Financials & Immutability)
        /// <summary>
        /// Đơn giá bán tại chính xác thời điểm khách chốt đơn (Mẫu thiết kế Snapshot).
        /// Đảm bảo tính bất biến: Nếu ngày mai Admin tăng giá sản phẩm trên hệ thống, 
        /// hóa đơn của đơn hàng cũ này vẫn phải giữ nguyên mức giá cũ lúc khách mua.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Số tiền giảm giá áp dụng riêng cho dòng sản phẩm này (Flash sale, Khuyến mãi trên từng món).</summary>
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>
        /// Thành tiền của dòng này. 
        /// Công thức bắt buộc: (Quantity * UnitPrice) - DiscountAmount.
        /// </summary>
        public decimal TotalPrice { get; set; }
        #endregion

        #region Tiến độ Giao hàng (Fulfillment Tracking)
        /// <summary>
        /// Tiến độ xuất kho lũy kế (Số lượng đã thực tế bàn giao cho Shipper).
        /// NGHIỆP VỤ NÂNG CAO: Hỗ trợ tính năng Giao hàng từng phần (Partial Fulfillment / Split Shipment).
        /// (Ví dụ: Khách đặt 100 kg, nhưng kho chi nhánh A chỉ còn 60 kg. Hệ thống xuất trước 60 kg 
        /// -> IssuedQuantity = 60. Phần 40 kg còn nợ sẽ được hệ thống sinh lệnh chuyển từ kho B sang bù vào sau).
        /// </summary>
        public decimal IssuedQuantity { get; set; } = 0;
        #endregion
    }
}