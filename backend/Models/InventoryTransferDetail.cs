namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Phiếu Điều Chuyển Liên Kho (Inventory Transfer Detail).
    /// Đại diện cho một dòng mặt hàng cụ thể đang được luân chuyển giữa các kho.
    /// </summary>
    public class InventoryTransferDetail
    {
        public int Id { get; set; }

        #region Hàng hóa & Truy xuất nguồn gốc (Traceability)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) được điều chuyển.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP VỀ NGUỒN GỐC: Mã định danh của Lô hàng nông sản (Product Batch).
        /// Nghiệp vụ cốt lõi: Khi điều chuyển hàng hóa, hệ thống bắt buộc bảo lưu thông tin Lô hàng. 
        /// Xuất đi từ Kho Nguồn bằng Lô nào (Ngày SX, Hạn SD) thì khi cập bến Kho Đích phải nhập đúng vào Lô đó. 
        /// Tuyệt đối không được gộp lô hay làm mất dấu vết (Traceability) giữa đường.
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) dùng trong quá trình đóng gói và vận chuyển.</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Khối lượng
        /// <summary>
        /// Số lượng hàng hóa được điều chuyển.
        /// Nghiệp vụ 2 bước: 
        /// - Khi Dispatched (Xuất đi): Trừ đi [Quantity] tại Kho Nguồn.
        /// - Khi Received (Nhập nhận): Cộng thêm [Quantity] tại Kho Đích.
        /// (Lưu ý: Nếu hệ thống sau này cần quản lý hao hụt đi đường, bạn có thể bổ sung thêm trường ReceivedQuantity thực tế tại đây).
        /// </summary>
        public decimal Quantity { get; set; }
        #endregion

        #region Đối soát Chứng từ (Transfer Note)
        /// <summary>Mã định danh của Phiếu điều chuyển chủ quản.</summary>
        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer? InventoryTransfer { get; set; }
        #endregion
    }
}