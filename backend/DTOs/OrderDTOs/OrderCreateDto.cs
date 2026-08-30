using backend.Models.Enums;
using System.Collections.Generic;

namespace backend.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO Yêu cầu Tạo Đơn Bán Hàng mới (Checkout Request).
    /// Được gọi từ giao diện Frontend khi khách hàng bấm nút "Đặt hàng".
    /// DTO này sẽ kích hoạt luồng nghiệp vụ lõi: Giữ chỗ tồn kho (Reserve Stock) 
    /// và Định tuyến kho thông minh (Smart Routing).
    /// </summary>
    public class OrderCreateDto
    {
        #region Khách hàng & Địa chỉ Giao hàng
        /// <summary>
        /// Mã định danh của Khách hàng thực hiện đặt đơn.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Mã Địa chỉ trong Sổ địa chỉ (Address Book) của khách.
        /// Dùng để tham chiếu, có thể null nếu khách nhập địa chỉ giao hàng tùy chỉnh một lần.
        /// </summary>
        public int? CustomerAddressId { get; set; }

        /// <summary>
        /// Tên người nhận hàng (Snapshot).
        /// Lưu cứng thông tin tại thời điểm đặt để in bill không bị ảnh hưởng nếu sau này khách đổi tên.
        /// </summary>
        public string? ReceiverName { get; set; }

        /// <summary>
        /// Số điện thoại liên hệ giao hàng (Snapshot).
        /// </summary>
        public string? ReceiverPhone { get; set; }

        /// <summary>
        /// Địa chỉ giao hàng chi tiết (Snapshot).
        /// </summary>
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Vận hành & Thanh toán
        /// <summary>
        /// Kho xuất hàng. 
        /// NGHIỆP VỤ SMART ROUTING: Có thể truyền cứng ID Kho nếu khách chọn lấy tại quầy/chọn chi nhánh. 
        /// Nếu để trống (null), Service sẽ tự động chạy thuật toán tìm Kho gần khách nhất MÀ CÓ ĐỦ TỒN KHO.
        /// </summary>
        public int? WarehouseId { get; set; }

        /// <summary>
        /// Phương thức thanh toán (COD, VNPay, MoMo...).
        /// Mặc định là COD (Thanh toán khi nhận hàng).
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        /// <summary>
        /// Phí vận chuyển áp dụng cho đơn hàng (Tính từ API bên thứ 3 như GHN hoặc rule Freeship của hệ thống).
        /// </summary>
        public decimal ShippingFee { get; set; } = 0;

        /// <summary>
        /// Ghi chú của khách cho đơn hàng (Ví dụ: "Giao giờ hành chính").
        /// </summary>
        public string? Note { get; set; }
        #endregion

        #region Chi tiết Đơn hàng
        /// <summary>
        /// Danh sách các mặt hàng khách muốn chốt mua.
        /// </summary>
        public List<OrderDetailCreateDto> Details { get; set; } = new();
        #endregion
    }

    /// <summary>
    /// DTO Chi tiết từng mặt hàng trong Yêu cầu Tạo Đơn Hàng.
    /// </summary>
    public class OrderDetailCreateDto
    {
        #region Hàng hóa & Phân loại
        /// <summary>
        /// Mã SKU (Biến thể sản phẩm) khách chọn mua.
        /// </summary>
        public int VariantId { get; set; }

        /// <summary>
        /// Đơn vị tính khách đang chọn trên giao diện (Ví dụ: Kg, Thùng, Hộp).
        /// </summary>
        public int UoMId { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>
        /// Số lượng khách đặt mua.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá mặt hàng.
        /// BẢO MẬT KẾ TOÁN: Cho phép null. Nếu null (hoặc không truyền lên), Service TẮT BUỘC phải tự truy vấn DB 
        /// để lấy [VariantPrice] nhằm ngăn chặn tình trạng Frontend bị hack/sửa giá truyền lên Server.
        /// Nếu có truyền lên, Service vẫn phải có logic Cross-check (Đối chiếu) xem giá này có khớp với DB hay không.
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Số tiền giảm giá được phân bổ cho dòng sản phẩm này (Từ Voucher hoặc CTKM).
        /// </summary>
        public decimal DiscountAmount { get; set; } = 0;
        #endregion
    }
}