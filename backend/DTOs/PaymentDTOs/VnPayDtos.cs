namespace backend.DTOs.PaymentDTOs
{
    #region 1. Khởi tạo Giao dịch (Payment Initialization)
    /// <summary>
    /// DTO Yêu cầu Khởi tạo URL Thanh toán qua cổng VNPay.
    /// Khách hàng bấm "Thanh toán", Backend sẽ dùng DTO này để băm dữ liệu (Checksum/Hash) 
    /// và gửi sang server VNPay để xin cấp một đường link thanh toán bảo mật.
    /// </summary>
    public class VnPayPaymentRequestDto
    {
        /// <summary>
        /// Mã đơn hàng gốc của hệ thống (Ví dụ: ORD-20260817-001).
        /// Cực kỳ quan trọng để làm tham chiếu đối soát (TxnRef) gửi sang VNPay.
        /// </summary>
        public required string OrderCode { get; set; }

        /// <summary>Nội dung hiển thị trên App ngân hàng của khách (Ví dụ: "Thanh toan don hang ORD...").</summary>
        public string? OrderDescription { get; set; }

        /// <summary>
        /// Mã ngân hàng chỉ định (Ví dụ: VCB, NCB, VNPAYQR).
        /// Nếu truyền lên, khách sẽ được điều hướng thẳng vào trang thanh toán của Bank đó. 
        /// Nếu để null, khách sẽ thấy trang chọn Phương thức thanh toán chung của VNPay.
        /// </summary>
        public string? BankCode { get; set; }
    }

    /// <summary>
    /// DTO Phản hồi chứa URL Thanh toán để Frontend điều hướng khách hàng.
    /// </summary>
    public class VnPayPaymentResponseDto
    {
        /// <summary>
        /// Đường link mã hóa từ VNPay. Frontend sẽ dùng lệnh redirect (window.location.href) 
        /// đẩy khách sang trang của VNPay để nhập thẻ/quét mã QR.
        /// </summary>
        public required string PaymentUrl { get; set; }

        public required string OrderCode { get; set; }
    }
    #endregion

    #region 2. Nhận kết quả Giao dịch (Callback & IPN Webhook)
    /// <summary>
    /// DTO Hứng dữ liệu từ Frontend Return URL (Sau khi khách thanh toán xong trên VNPay).
    /// NGHIỆP VỤ BẢO MẬT: Dữ liệu này chỉ dùng để hiển thị giao diện "Thanh toán thành công/Thất bại" 
    /// cho khách xem. TUYỆT ĐỐI KHÔNG dùng DTO này để update trạng thái PaymentStatus trong Database 
    /// nhằm phòng tránh rủi ro Hacker can thiệp sửa URL trên trình duyệt.
    /// </summary>
    public class VnPayCallbackResultDto
    {
        /// <summary>Kết quả giao dịch (True nếu vnp_ResponseCode == "00").</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Mã đơn hàng tham chiếu.</summary>
        public string? OrderCode { get; set; }

        /// <summary>
        /// Mã giao dịch đối soát nội bộ của VNPay (vnp_TransactionNo).
        /// Kế toán dùng mã này để khiếu nại hoặc tra soát dòng tiền với VNPay vào cuối tháng.
        /// </summary>
        public string? TransactionNo { get; set; }

        /// <summary>Mã lỗi/Trạng thái trả về từ VNPay (Ví dụ: 00 = Thành công, 24 = Khách hủy giao dịch).</summary>
        public string? ResponseCode { get; set; }

        public string? BankCode { get; set; }

        /// <summary>Số tiền thực tế khách đã thanh toán (Đã chia 100 theo chuẩn VNPay).</summary>
        public decimal Amount { get; set; }

        public string? OrderInfo { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// DTO Phản hồi chuẩn cho IPN Webhook (Instant Payment Notification) của VNPay.
    /// XƯƠNG SỐNG TÀI CHÍNH: Đây mới là luồng Server-to-Server gọi ngầm để update Trạng thái thanh toán (PaymentStatus = Paid). 
    /// Định dạng RspCode và Message bắt buộc phải tuân thủ nghiêm ngặt 100% tài liệu API của VNPay 
    /// (Ví dụ: {"RspCode": "00", "Message": "Confirm Success"}).
    /// </summary>
    public class VnPayIpnResponseDto
    {
        /// <summary>
        /// Mã phản hồi báo cáo lại cho VNPay biết hệ thống mình đã ghi nhận hay chưa 
        /// (Ví dụ: 00 = Success, 01 = Order not found, 02 = Order already confirmed, 97 = Invalid checksum).
        /// </summary>
        public required string RspCode { get; set; }

        /// <summary>Thông điệp mô tả chi tiết cho RspCode tương ứng.</summary>
        public required string Message { get; set; }
    }
    #endregion
}