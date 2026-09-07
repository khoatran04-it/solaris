using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.ShopDTOs
{
    #region 1. DTO Checkout (Khách hàng Đặt đơn)
    /// <summary>
    /// DTO Yêu cầu Thanh toán / Đặt hàng (Checkout Request).
    /// Giao diện dành riêng cho Storefront (App/Web B2C). Khởi chạy luồng nghiệp vụ lõi: 
    /// Chuyển đổi Giỏ hàng (Cart) thành Đơn hàng (Order), giữ chỗ Tồn kho (Reserve), 
    /// và kích hoạt thuật toán Định tuyến kho thông minh (Smart Routing).
    /// </summary>
    public class ShopCheckoutRequestDto
    {
        #region Sổ địa chỉ & Snapshot Giao hàng
        /// <summary>
        /// ID Địa chỉ trong Sổ địa chỉ (Address Book) của khách.
        /// Có thể chọn từ Sổ địa chỉ đã lưu hoặc nhập trực tiếp (để null).
        /// </summary>
        public int? CustomerAddressId { get; set; }

        /// <summary>Tên người nhận (Sẽ được Snapshot cứng vào Đơn hàng).</summary>
        public string? ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ giao hàng (Snapshot).</summary>
        public string? ReceiverPhone { get; set; }

        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }

        /// <summary>Số nhà, Tên đường chi tiết.</summary>
        public string? StreetAddress { get; set; }
        #endregion

        #region API Vận chuyển & Định tuyến (Logistics & Routing)
        /// <summary>Mã Quận/Huyện map theo chuẩn API của Giao Hàng Nhanh (GHN).</summary>
        public int? GhnDistrictId { get; set; }

        /// <summary>Mã Phường/Xã map theo chuẩn API của Giao Hàng Nhanh (GHN).</summary>
        public string? GhnWardCode { get; set; }

        /// <summary>Phí vận chuyển dự kiến (Tính từ API bên thứ 3 hoặc rule hệ thống).</summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Vĩ độ GPS của địa chỉ nhận hàng.
        /// NGHIỆP VỤ SMART ROUTING: Backend sẽ dùng tọa độ này để quét bán kính, tìm ra 
        /// Kho hàng gần khách nhất MÀ CÓ ĐỦ TỒN KHO để xuất hàng, giúp tối ưu chi phí Ship.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS của địa chỉ nhận hàng.</summary>
        public double Longitude { get; set; }
        #endregion

        #region Thanh toán & Ghi chú
        /// <summary>
        /// Phương thức khách chọn thanh toán (COD, MoMo, VNPay...).
        /// Mặc định là COD (Cash on Delivery).
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        /// <summary>Ghi chú của khách (Ví dụ: "Giao sau 5h chiều").</summary>
        public string? Note { get; set; }
        #endregion
    }
    #endregion

    #region 2. DTO Hiển thị Chi tiết Item trong Đơn hàng B2C
    /// <summary>
    /// DTO Hiển thị Dòng sản phẩm trong Đơn hàng (Customer-facing).
    /// Đã được làm phẳng dữ liệu để tối ưu UI lịch sử mua hàng trên App/Web.
    /// </summary>
    public class ShopOrderItemDto
    {
        #region Hàng hóa & Hình ảnh
        public int DetailId { get; set; }
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }

        /// <summary>Hình ảnh sản phẩm hiển thị trên UI Lịch sử đơn hàng.</summary>
        public string? ImagePath { get; set; }

        public int UoMId { get; set; }
        public required string UoMName { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Tiến độ giao hàng lũy kế.
        /// UX/UI: Giúp khách hàng xem được dòng sản phẩm này đã được kho đóng gói/xuất đi hay chưa 
        /// (Dành cho các đơn hàng phải giao làm nhiều đợt).
        /// </summary>
        public decimal IssuedQuantity { get; set; }
        #endregion
    }
    #endregion

    #region 3. DTO Hiển thị Tổng quan Đơn hàng B2C (Order History)
    /// <summary>
    /// DTO Tổng quan Đơn hàng dành cho Khách hàng (Storefront View).
    /// Ẩn đi các thông tin nhạy cảm của hệ thống ERP (như Id Kho xuất, Mã nhân viên duyệt) 
    /// chỉ tập trung vào Trạng thái, Tiền bạc và Tiến độ giao hàng.
    /// </summary>
    public class ShopOrderReadDto
    {
        #region Định danh & Thời gian
        public int Id { get; set; }
        public required string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        #endregion

        #region Trạng thái & Thanh toán (UI Localized)
        public OrderStatus Status { get; set; }

        /// <summary>Tên trạng thái đơn hàng đã được dịch sang ngôn ngữ hiển thị (Ví dụ: "Đang giao").</summary>
        public string StatusName { get; set; } = string.Empty;

        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>Tên trạng thái thanh toán (Ví dụ: "Đã thanh toán").</summary>
        public string PaymentStatusName { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        #endregion

        #region Tài chính
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }

        /// <summary>Số tiền cuối cùng khách phải thanh toán (SubTotal - Discount + ShippingFee).</summary>
        public decimal TotalAmount { get; set; }
        #endregion

        #region Giao hàng & Vận chuyển (Fulfillment Info)
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        /// <summary>Mã tra cứu vận đơn (Giúp khách bấm vào link để xem lộ trình shipper).</summary>
        public string? TrackingCode { get; set; }

        /// <summary>Tên đơn vị vận chuyển (Ví dụ: Giao Hàng Nhanh, ViettelPost).</summary>
        public string? ShippingProvider { get; set; }

        /// <summary>Ngày giờ dự kiến hàng tới tay khách (Hiển thị dạng Text thân thiện UI).</summary>
        public string? ExpectedDeliveryDate { get; set; }
        #endregion

        #region Ghi chú & Chi tiết
        public string? Note { get; set; }

        /// <summary>Lý do hủy đơn (Nếu đơn đã bị hủy).</summary>
        public string? CancellationReason { get; set; }

        /// <summary>Danh sách các mặt hàng trong đơn.</summary>
        public List<ShopOrderItemDto> Items { get; set; } = new List<ShopOrderItemDto>();
        #endregion
    }
    #endregion

    #region 4. DTO Khách hàng Yêu cầu Hủy Đơn (Cancel Request)
    /// <summary>
    /// DTO Khách hàng tự thao tác Hủy đơn hàng trên App/Web.
    /// NGHIỆP VỤ BẢO VỆ TỒN KHO: Khi gọi API này, hệ thống sẽ kiểm tra trạng thái đơn. 
    /// Nếu kho chưa đóng gói (Status = Pending/Approved), đơn sẽ bị hủy và Tồn kho Reserve được nhả ra ngay lập tức.
    /// </summary>
    public class ShopOrderCancelRequestDto
    {
        /// <summary>
        /// Lý do khách hàng muốn hủy.
        /// Dữ liệu cực kỳ quan trọng cho đội Marketing/Operations để phân tích tỷ lệ rớt đơn (Drop-off Rate).
        /// </summary>
        public required string Reason { get; set; }
    }
    #endregion
}