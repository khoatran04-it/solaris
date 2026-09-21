using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Đơn Đặt Mua Hàng (Purchase Order Detail).
    /// Đại diện cho một dòng mặt hàng (SKU) cụ thể được yêu cầu trong Đơn mua hàng (PO).
    /// Hỗ trợ theo dõi tiến độ nhập kho thực tế so với số lượng đã đặt mua ban đầu.
    /// </summary>
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        #region Chỉ tiêu Khối lượng & Tiến độ
        /// <summary>Số lượng hàng hóa yêu cầu đặt mua (Dựa theo Đơn vị tính UoM chỉ định).</summary>
        public decimal OrderQuantity { get; set; }

        /// <summary>
        /// Số lượng hàng hóa thực tế đã nhập kho thành công (Lũy kế).
        /// Nghiệp vụ: Cực kỳ quan trọng để xử lý bài toán Giao hàng từng phần (Partial Receipt) khi Nhà cung cấp giao thiếu hoặc chia làm nhiều đợt. 
        /// Đơn hàng (PO) chỉ hoàn tất khi ReceivedQuantity == OrderQuantity của tất cả các dòng.
        /// </summary>
        public decimal ReceivedQuantity { get; set; } = 0;

        /// <summary>
        /// Số lượng hàng hóa bị từ chối / trả lại ngay tại cửa kho (Lũy kế).
        /// Dùng làm căn cứ đối soát giảm trừ công nợ với Nhà cung cấp.
        /// </summary>
        public decimal RejectedQuantity { get; set; } = 0;
        #endregion

        #region Chỉ tiêu Tài chính
        /// <summary>Đơn giá đặt mua (VND) (Giá đã thỏa thuận chốt với Nhà cung cấp tại thời điểm lập đơn).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Thành tiền của dòng sản phẩm này.
        /// Thường được tính bằng: OrderQuantity * UnitPrice (Được lưu cứng vào CSDL để phục vụ đối soát công nợ và báo cáo, tránh sai lệch khi giá gốc thay đổi).
        /// </summary>
        public decimal TotalPrice { get; set; }
        #endregion

        #region Liên kết Chứng từ (PO)
        /// <summary>Mã định danh của Đơn mua hàng (PO) chủ quản.</summary>
        public int PurchaseOrderId { get; set; }

        /// <summary>Thực thể Đơn mua hàng chứa dòng chi tiết này.</summary>
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        #endregion

        #region Liên kết Hàng hóa & Quy cách
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) cần mua.</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM).</summary>
        public int UoMId { get; set; }

        /// <summary>
        /// Thực thể Đơn vị tính. 
        /// Nghiệp vụ: Đơn vị tính thu mua (Ví dụ: Tấn, Thùng, Két) có thể khác với đơn vị tính bán lẻ (Ví dụ: Kg, Gói, Hộp). Hệ thống quy đổi sẽ xử lý việc này lúc nhập kho.
        /// </summary>
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Đối soát Nhập kho (Receipt Tracking)
        /// <summary>
        /// Danh sách các dòng Chi tiết Phiếu Nhập Kho (Goods Receipt Note Details) được sinh ra từ dòng PO này.
        /// Hỗ trợ truy vết 1 dòng đặt mua đã được nhập vào kho những ngày nào, mỗi ngày bao nhiêu lượng.
        /// </summary>
        public virtual ICollection<InventoryReceiptDetail> ReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();
        #endregion
    }
}