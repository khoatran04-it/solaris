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

        #region Khối lượng & Kiểm đếm tiếp nhận
        /// <summary>
        /// Số lượng hàng hóa được xuất đi từ Kho Nguồn.
        /// Nghiệp vụ: Khi Dispatched (Xuất đi), trừ đi [Quantity] tại Kho Nguồn.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Số lượng hàng hóa thực tế còn nguyên vẹn đạt chuẩn khi kiểm đếm tại Kho Đích.
        /// Nghiệp vụ: Khi Received (Nhập nhận), cộng thêm [ActualReceivedQuantity] vào ngăn Available tại Kho Đích.
        /// </summary>
        public decimal ActualReceivedQuantity { get; set; }

        /// <summary>
        /// Số lượng hàng hóa bị hư hỏng, dập nát, rách màng bọc trong quá trình vận chuyển.
        /// Nghiệp vụ: Khi Received (Nhập nhận), cộng thêm [DamagedQuantity] vào ngăn Damaged tại Kho Đích để xử lý hủy/thanh lý.
        /// </summary>
        public decimal DamagedQuantity { get; set; } = 0;
        #endregion

        #region Đối soát Chứng từ (Transfer Note)
        /// <summary>Mã định danh của Phiếu điều chuyển chủ quản.</summary>
        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer? InventoryTransfer { get; set; }
        #endregion
    }
}