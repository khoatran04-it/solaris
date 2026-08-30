namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Phiếu Xuất Kho (Inventory Issue Detail).
    /// Đại diện cho một dòng mặt hàng cụ thể được nhặt ra khỏi kho. 
    /// Đóng vai trò quyết định trong việc trừ chính xác số dư Tồn kho của từng Lô hàng (Batch) 
    /// và ghi nhận Giá vốn hàng bán (COGS).
    /// </summary>
    public class InventoryIssueDetail
    {
        public int Id { get; set; }

        #region Hàng hóa & Truy xuất nguồn gốc (Traceability)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) được xuất kho.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP: Mã định danh của Lô hàng nông sản (Product Batch).
        /// Nghiệp vụ cốt lõi: Khác với các ngành hàng điện tử (không quan tâm lô), đối với nông sản, 
        /// mọi dòng xuất kho đều BẮT BUỘC phải chỉ đích danh hàng được nhặt từ Lô (Batch) nào. 
        /// Việc này do hệ thống tự động chỉ định thông qua thuật toán FEFO (Hết hạn trước xuất trước) 
        /// nhằm đảm bảo vòng đời sản phẩm và Truy xuất nguồn gốc (Traceability).
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) lúc xuất kho.</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>
        /// Số lượng thực tế được xuất đi (Theo Đơn vị tính UoM).
        /// Nghiệp vụ: Con số này sẽ được dùng để trừ thẳng vào Tồn kho thực tế (QuantityAvailable) của Batch tương ứng.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá xuất kho (VND).
        /// Nghiệp vụ: Thường được tính toán tự động dựa trên phương pháp tính Giá vốn (Bình quân gia quyền, FIFO, hoặc giá đích danh của Batch) 
        /// để phục vụ cho báo cáo Lợi nhuận gộp.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Thành tiền xuất kho (Quantity * UnitPrice).
        /// </summary>
        public decimal TotalPrice { get; set; }
        #endregion

        #region Đối soát Chứng từ (Issue & Order)
        /// <summary>Mã định danh của Phiếu xuất kho chủ quản.</summary>
        public int InventoryIssueId { get; set; }
        public virtual InventoryIssue? InventoryIssue { get; set; }

        /// <summary>
        /// Mã định danh dòng chi tiết Đơn bán hàng (Order Line Item) tham chiếu.
        /// Nghiệp vụ: Giúp hệ thống tự động đồng bộ tiến độ giao hàng, đánh dấu dòng Đơn hàng này 
        /// là "Đã hoàn tất xuất kho" (Fulfilled). (Có thể null nếu xuất hủy, xuất tiêu dùng nội bộ).
        /// </summary>
        public int? OrderDetailId { get; set; }
        public virtual OrderDetail? OrderDetail { get; set; }
        #endregion
    }
}