using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đơn Bán Hàng (Customer Sales Order).
    /// Trung tâm của hệ thống Storefront B2C. Giao thoa giữa phân hệ Khách hàng, Thanh toán và Kho hàng.
    /// Tích hợp sẵn cơ sở hạ tầng cho: Định tuyến kho (Smart Routing), Giữ chỗ tồn kho (Stock Reservation),
    /// và kết nối API Vận chuyển bên thứ 3 (GHN - Giao Hàng Nhanh).
    /// </summary>
    public class Order : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái (Identity & Status)
        /// <summary>Mã đơn hàng (Ví dụ: ORD-20260817-001).</summary>
        public required string OrderCode { get; set; }

        /// <summary>
        /// Trạng thái vòng đời Đơn hàng (Pending, Approved, Packing, Shipping, Completed, Cancelled).
        /// NGHIỆP VỤ KHO: Khi trạng thái là Pending/Approved, Core Engine sẽ khóa tồn kho (Reserve). 
        /// Khi trạng thái chuyển sang Completed, tồn kho sẽ bị trừ đi vĩnh viễn (Issue).
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        #endregion

        #region Khách hàng & Địa chỉ Giao hàng (Customer & Snapshot)
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        /// <summary>ID Địa chỉ sổ bưu điện của khách hàng (Dùng để tham chiếu gốc).</summary>
        public int? CustomerAddressId { get; set; }
        public virtual CustomerAddress? CustomerAddress { get; set; }

        // ==============================================================================
        // MẪU THIẾT KẾ SNAPSHOT (CHỤP NHANH DỮ LIỆU)
        // Nghiệp vụ bảo toàn chứng từ: Tại giây phút khách hàng bấm "Thanh toán", 
        // toàn bộ thông tin địa chỉ sẽ được copy chết (hard-copy) vào 3 trường này.
        // Đảm bảo: Dù ngày mai khách có vào sửa Tên/SĐT trong Profile, hóa đơn của 
        // đơn hàng cũ này in ra vẫn giữ nguyên thông tin tại thời điểm mua.
        // ==============================================================================
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Định tuyến Kho & Vận chuyển (Routing & Logistics)
        /// <summary>
        /// Kho xuất hàng. 
        /// TÍNH NĂNG SMART ROUTING: Hệ thống sẽ tự động quét GPS/Mã Vùng để tìm Kho gần khách hàng nhất 
        /// MÀ CÓ ĐỦ SỐ LƯỢNG TỒN KHO nhằm tối ưu chi phí ship. Nếu không có kho nào đủ, trường này có thể null 
        /// để chờ Điều phối viên chia đơn thủ công hoặc sinh lệnh Điều chuyển liên kho.
        /// </summary>
        public int? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Đơn vị vận chuyển (GHN, ViettelPost, Internal - Tự giao, None - Mua tại quầy).</summary>
        public string? ShippingProvider { get; set; }

        /// <summary>Mã vận đơn giao hàng (Ví dụ: Mã Tracking của GHN để khách tra cứu trên App).</summary>
        public string? TrackingCode { get; set; }

        /// <summary>Ngày dự kiến hàng tới tay khách.</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>Mã Quận/Huyện định tuyến theo chuẩn API của GHN.</summary>
        public int? GhnDistrictId { get; set; }

        /// <summary>Mã Phường/Xã định tuyến theo chuẩn API của GHN.</summary>
        public string? GhnWardCode { get; set; }
        #endregion

        #region Tài chính & Thanh toán (Finance & Payment)
        /// <summary>Phương thức thanh toán: COD (Thu hộ), VNPay, MoMo, Thẻ tín dụng...</summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        /// <summary>Trạng thái dòng tiền: Unpaid, Paid, Refunded, Failed.</summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        /// <summary>Tiền hàng gốc (Tổng đơn giá x Số lượng của tất cả sản phẩm).</summary>
        public decimal SubTotal { get; set; } = 0;

        /// <summary>Tổng tiền giảm giá (Cộng gộp từ Hạng thành viên Tier và Mã Voucher).</summary>
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>Phí vận chuyển (Tính theo API GHN hoặc chính sách Freeship của cửa hàng).</summary>
        public decimal ShippingFee { get; set; } = 0;

        /// <summary>
        /// Tổng tiền khách KHÁCH PHẢI TRẢ (Công thức: SubTotal - DiscountAmount + ShippingFee).
        /// </summary>
        public decimal TotalAmount { get; set; } = 0;

        /// <summary>Mã giao dịch đối soát từ Cổng thanh toán (Ví dụ: vnp_TransactionNo).</summary>
        public string? PaymentTransactionNo { get; set; }
        #endregion

        #region Thời gian, Ghi chú & Soft Delete
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>Ghi chú của khách hàng lúc đặt đơn (Ví dụ: "Giao trong giờ hành chính").</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy đơn (Rất quan trọng để phân tích chỉ số Drop-off rate).</summary>
        public string? CancellationReason { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Liên kết phân hệ khác (Navigation Properties)
        /// <summary>Danh sách chi tiết mặt hàng khách mua.</summary>
        public virtual ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();

        /// <summary>
        /// Liên kết 1-N với Phiếu Xuất Kho. 
        /// Tại sao lại là 1-N? Vì một đơn hàng lớn có thể phải xuất ra làm nhiều đợt (Split Shipment) 
        /// hoặc bốc từ 2 kho khác nhau để giao cho khách.
        /// </summary>
        public virtual ICollection<InventoryIssue> InventoryIssues { get; set; } = new List<InventoryIssue>();

        /// <summary>Danh sách các Phiếu yêu cầu Đổi/Trả hàng phát sinh từ đơn hàng này (Nếu khách Boom hoặc hàng lỗi).</summary>
        public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();
        #endregion
    }
}