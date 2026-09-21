namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Phiếu Nhập Kho (Inventory Receipt Detail).
    /// Ghi nhận kết quả kiểm đếm thực tế của từng mặt hàng tại cửa kho. 
    /// Đóng vai trò quyết định trong việc tính toán số dư tồn kho, đánh giá tỷ lệ hao hụt/lỗi của Nhà cung cấp 
    /// và đối soát tiến độ giao hàng của Đơn mua (PO).
    /// </summary>
    public class InventoryReceiptDetail
    {
        public int Id { get; set; }

        #region Số liệu Kiểm đếm (QC Metrics)
        /// <summary>
        /// Số lượng dự kiến giao (Dựa trên Đơn đặt hàng PO hoặc Packing List của Nhà cung cấp).
        /// </summary>
        public decimal ExpectedQuantity { get; set; }

        /// <summary>
        /// Số lượng thực tế đạt chuẩn chất lượng (QC Passed).
        /// Nghiệp vụ cốt lõi: Đây là con số DUY NHẤT được dùng để cộng TĂNG số dư Tồn kho thực tế (QuantityAvailable) 
        /// và cộng dồn vào Tiến độ nhận hàng (ReceivedQuantity) của dòng PO tham chiếu khi Phiếu nhập Hoàn tất.
        /// </summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>
        /// Số lượng bị từ chối / trả về ngay tại cửa kho (Ví dụ: Trái cây dập nát, thối rữa, sai quy cách, cận date).
        /// Nghiệp vụ: Dùng để tính toán tỷ lệ lỗi (Defect Rate) và đánh giá độ uy tín (KPI) của Nhà cung cấp.
        /// </summary>
        public decimal RejectedQuantity { get; set; }

        /// <summary>
        /// Lý do từ chối nhận hàng (Bắt buộc phải ghi rõ nếu RejectedQuantity > 0 để làm bằng chứng trừ công nợ).
        /// </summary>
        public string? RejectReason { get; set; }
        #endregion

        #region Cân đo thực tế & Thể tích (Actual Weight & Volume Metrics)
        /// <summary>
        /// Khối lượng cân thực tế tại cửa kho tính bằng Kilogram (Kg).
        /// Nghiệp vụ: Dùng đối chiếu hao hụt thực tế so với barem cân nặng lý thuyết từ nhà cung cấp.
        /// </summary>
        public decimal? ActualWeightKg { get; set; }

        /// <summary>
        /// Thể tích tính toán của dòng hàng nhập kho này tính bằng mét khối (CBM - m3).
        /// Nghiệp vụ: Dùng để cộng vào thể tích kho đã sử dụng (Occupied CBM) khi hoàn tất phiếu nhập.
        /// </summary>
        public decimal? CalculatedCbm { get; set; }
        #endregion

        #region Liên kết Hàng hóa & Lô hàng (Traceability)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) đang được kiểm đếm.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// Mã định danh của Lô hàng nông sản (Product Batch).
        /// Nghiệp vụ khắt khe: Hàng hóa nhập vào kho bắt buộc phải được gán vào một Mã Lô (Batch) cụ thể 
        /// để hệ thống quản lý Truy xuất nguồn gốc và áp dụng thuật toán xuất kho FEFO (Hết hạn trước xuất trước).
        /// </summary>
        public int? BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) lúc nhập kho.</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Đối soát Chứng từ (Receipt & PO)
        /// <summary>Mã định danh Phiếu nhập kho chủ quản.</summary>
        public int InventoryReceiptId { get; set; }
        public virtual InventoryReceipt? InventoryReceipt { get; set; }

        /// <summary>
        /// Mã định danh dòng chi tiết Đơn mua hàng (PO Line Item) tham chiếu.
        /// Nghiệp vụ: Cực kỳ quan trọng để hệ thống biết dòng hàng này đang nhập cho Hợp đồng/PO nào, 
        /// từ đó tự động cập nhật số lượng đã nhận (ReceivedQuantity) sang bảng PurchaseOrderDetail tương ứng.
        /// (Có thể null nếu nhập hàng ngoài PO như nhập hàng hoàn trả).
        /// </summary>
        public int? PurchaseOrderDetailId { get; set; }
        public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
        #endregion
    }
}