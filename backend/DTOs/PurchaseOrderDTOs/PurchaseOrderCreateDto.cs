using System;
using System.Collections.Generic;

namespace backend.DTOs.PurchaseOrderDTOs
{
    /// <summary>
    /// DTO yêu cầu Tạo mới Đơn Đặt Mua Hàng (Purchase Order - PO).
    /// Cung cấp thông tin đối tác, lịch trình giao nhận và danh sách các mặt hàng cần thu mua từ Nhà cung cấp.
    /// </summary>
    public class PurchaseOrderCreateDto
    {
        #region Thông tin Chứng từ & Lịch trình
        /// <summary>Mã chứng từ (Ví dụ: PO-20260817-001). Có thể nhập tay, hoặc để trống nếu muốn hệ thống tự động phát sinh theo quy tắc.</summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>Ngày chính thức lập đơn đặt hàng và chốt thỏa thuận.</summary>
        public DateTime OrderDate { get; set; }

        /// <summary>Ngày hẹn giao hàng dự kiến từ nhà cung cấp (Căn cứ để bộ phận Kho sắp xếp vị trí và nhân sự bốc dỡ).</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }
        #endregion

        #region Ghi chú & Yêu cầu
        /// <summary>Ghi chú hoặc yêu cầu vận hành đặc biệt (Ví dụ: Yêu cầu xe bảo ôn, giao trước 8h sáng, đóng pallet...).</summary>
        public string? Note { get; set; }
        #endregion

        #region Đối tác & Nhân sự
        /// <summary>Mã định danh của Nhà cung cấp (Supplier) tiếp nhận PO này.</summary>
        public int SupplierId { get; set; }

        /// <summary>Mã định danh của Nhân viên Thu mua (Purchasing Staff) chịu trách nhiệm lập và theo dõi đơn.</summary>
        public int CreatedById { get; set; }
        #endregion

        #region Danh sách Sản phẩm
        /// <summary>Danh sách chi tiết các mặt hàng (SKU), số lượng và đơn giá cần đặt mua (Phải có ít nhất 1 dòng chi tiết).</summary>
        public List<PurchaseOrderDetailCreateDto> Details { get; set; } = new List<PurchaseOrderDetailCreateDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ chứa thông tin chi tiết của một dòng mặt hàng (Line Item) trong Đơn đặt mua.
    /// </summary>
    public class PurchaseOrderDetailCreateDto
    {
        #region Hàng hóa & Quy cách
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) cần mua.</summary>
        public int VariantId { get; set; }

        /// <summary>
        /// Mã định danh Đơn vị tính (UoM). 
        /// Nghiệp vụ: Thể hiện quy cách thu mua từ Nhà cung cấp (Ví dụ: Nhập theo Tấn, Thùng, Két thay vì Kg hay Chai).
        /// </summary>
        public int UoMId { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>Số lượng yêu cầu đặt mua (Căn cứ theo Đơn vị tính ở trên).</summary>
        public decimal OrderQuantity { get; set; }

        /// <summary>Đơn giá thỏa thuận (VND) tại thời điểm lập đơn (Chưa tính VAT hoặc đã tính VAT tùy theo cấu hình quy đổi).</summary>
        public decimal UnitPrice { get; set; }
        #endregion
    }
}