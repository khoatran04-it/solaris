using backend.DTOs.PaymentDTOs;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Tích hợp Cổng thanh toán Điện tử VNPay.
    /// Đóng vai trò làm cầu nối trung gian xử lý luồng tiền tệ trực tuyến (Online Payment Gateway): 
    /// Khởi tạo đường dẫn thanh toán mã hóa, kiểm tra tính toàn vẹn dữ liệu (Checksum/HMAC SHA512), 
    /// và xử lý cơ chế Webhook/IPN cập nhật trạng thái đơn hàng.
    /// </summary>
    public interface IVnPayService
    {
        #region 1. Khởi tạo Giao dịch (Payment Initialization)
        /// <summary>
        /// Tạo URL thanh toán VNPay có gắn chữ ký số bảo mật.
        /// LUỒNG XỬ LÝ LÕI:
        /// 1. Truy vấn thông tin đơn hàng từ mã [request.OrderCode] để lấy số tiền thực tế (TotalAmount).
        /// 2. Thu thập địa chỉ IP của Client thông qua [httpContext] (Bắt buộc theo chuẩn VNPay: vnp_IpAddr).
        /// 3. Sắp xếp toàn bộ tham số theo thứ tự alphabet (a-z) và tạo chuỗi băm HMAC SHA512 với bí mật (HashSecret).
        /// 4. Trả về URL hoàn chỉnh để Frontend thực hiện chuyển hướng khách hàng sang cổng thanh toán.
        /// </summary>
        /// <param name="request">Thông tin yêu cầu thanh toán (Mã đơn hàng, nội dung, mã ngân hàng tùy chọn).</param>
        /// <param name="httpContext">Ngữ cảnh HTTP hiện tại để trích xuất IP Client (vnp_IpAddr).</param>
        /// <returns>Đối tượng chứa đường dẫn thanh toán (PaymentUrl) và mã đơn hàng.</returns>
        Task<VnPayPaymentResponseDto> CreatePaymentUrlAsync(VnPayPaymentRequestDto request, HttpContext httpContext);
        #endregion

        #region 2. Phản hồi Giao diện Khách hàng (Return URL / Callback)
        /// <summary>
        /// Xử lý và xác thực dữ liệu trả về trên trình duyệt khi khách hàng hoàn tất giao dịch tại VNPay (Return URL).
        /// NGUYÊN TẮC BẢO MẬT:
        /// Hàm này kiểm tra chữ ký số (vnp_SecureHash) trên tập tham số [query].
        /// Kết quả chỉ dùng để hiển thị giao diện UI kết quả (Thành công / Thất bại) cho người dùng cuối.
        /// TUYỆT ĐỐI KHÔNG cập nhật trạng thái đơn hàng (Order.PaymentStatus = Paid) tại hàm này 
        /// để phòng chống rủi ro giả mạo URL hoặc người dùng tắt trình duyệt đột ngột.
        /// </summary>
        /// <param name="query">Tập hợp các tham số Query String mà VNPay đẩy về trên URL trình duyệt.</param>
        /// <returns>Kết quả đối soát giao diện (Thành công, Mã giao dịch, Số tiền, Thông báo lỗi nếu có).</returns>
        VnPayCallbackResultDto ProcessCallback(IQueryCollection query);
        #endregion

        #region 3. Webhook Máy chủ - Máy chủ (Instant Payment Notification - IPN)
        /// <summary>
        /// Xử lý thông báo thanh toán tự động ngầm giữa Server VNPay và Server Backend (IPN Webhook).
        /// LUỒNG ĐỐI SOÁT TÀI CHÍNH LÕI:
        /// 1. Kiểm tra tính hợp lệ của chữ ký điện tử (vnp_SecureHash). Nếu sai trả về RspCode = "97".
        /// 2. Tìm kiếm đơn hàng theo vnp_TxnRef. Nếu không tồn tại trả về RspCode = "01".
        /// 3. Kiểm tra số tiền khớp nối (vnp_Amount / 100 == Order.TotalAmount). Nếu lệch trả về RspCode = "04".
        /// 4. Kiểm tra trạng thái hiện tại. Nếu đơn đã được xác nhận thanh toán trước đó trả về RspCode = "02".
        /// 5. Nếu vnp_ResponseCode == "00" (Thành công): Cập nhật Order.PaymentStatus = Paid, kích hoạt nghiệp vụ kho.
        /// 6. Trả về JSON chuẩn (RspCode = "00", Message = "Confirm Success") để máy chủ VNPay dừng gửi lại thông báo (Retry loop).
        /// </summary>
        /// <param name="query">Tập hợp các tham số do máy chủ VNPay truyền ngầm qua giao thức HTTP GET/POST.</param>
        /// <returns>Đối tượng phản hồi tiêu chuẩn theo giao thức IPN của VNPay.</returns>
        Task<VnPayIpnResponseDto> ProcessIpnAsync(IQueryCollection query);
        #endregion
    }
}