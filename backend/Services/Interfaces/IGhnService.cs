using backend.DTOs.ShippingDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Tích hợp Đối tác Vận chuyển (Giao Hàng Nhanh - GHN).
    /// Đóng vai trò là cầu nối (3PL Integration) giữa hệ thống ERP nội bộ và API của GHN.
    /// Xử lý 3 luồng nghiệp vụ chính: Đồng bộ dữ liệu hành chính, Tính cước phí thời gian thực (Real-time Quoting), 
    /// và Đẩy lệnh giao hàng (Fulfillment/Push Order).
    /// </summary>
    public interface IGhnService
    {
        #region 1. Đồng bộ Dữ liệu Hành chính (Master Data Synchronization)
        /// <summary>
        /// Lấy danh sách Tỉnh/Thành phố từ hệ thống GHN.
        /// NGHIỆP VỤ MAPPING: Để tạo đơn thành công, hệ thống nội bộ phải sử dụng chính xác bộ ID 
        /// (ProvinceID) do GHN định nghĩa, thay vì dùng ID Tỉnh/Thành tự generate.
        /// </summary>
        /// <returns>Danh sách Tỉnh/Thành phố chuẩn GHN.</returns>
        Task<List<GhnProvinceDto>> GetProvincesAsync();

        /// <summary>
        /// Lấy danh sách Quận/Huyện trực thuộc một Tỉnh/Thành phố.
        /// Thường được gọi qua API/AJAX khi khách hàng chọn xong Tỉnh/Thành ở màn hình Checkout.
        /// </summary>
        /// <param name="provinceId">Mã Tỉnh/Thành (ProvinceID của GHN).</param>
        /// <returns>Danh sách Quận/Huyện chuẩn GHN.</returns>
        Task<List<GhnDistrictDto>> GetDistrictsAsync(int provinceId);

        /// <summary>
        /// Lấy danh sách Phường/Xã trực thuộc một Quận/Huyện.
        /// LƯU Ý KỸ THUẬT: GHN quy ước mã Phường/Xã (WardCode) là kiểu chuỗi (String), không phải số nguyên (Int).
        /// </summary>
        /// <param name="districtId">Mã Quận/Huyện (DistrictID của GHN).</param>
        /// <returns>Danh sách Phường/Xã chuẩn GHN.</returns>
        Task<List<GhnWardDto>> GetWardsAsync(int districtId);
        #endregion

        #region 2. Vận hành & Tính cước phí (Logistics Operations & Quoting)
        /// <summary>
        /// Tính toán cước phí vận chuyển dự kiến (Real-time Shipping Fee).
        /// NGHIỆP VỤ CHECKOUT: Gọi API sang GHN dựa trên địa chỉ nhận (District/Ward), khối lượng (Weight) 
        /// và giá trị bảo hiểm (SubTotal). Service sẽ kết hợp kết quả trả về với Chính sách Freeship nội bộ 
        /// để ra được số tiền cuối cùng khách phải trả.
        /// </summary>
        /// <param name="request">Thông tin điểm đến, khối lượng và giá trị đơn hàng.</param>
        /// <returns>Chi tiết cước phí gốc, phí thực thu và thông tin Freeship (nếu có).</returns>
        Task<GhnCalculateFeeResponseDto> CalculateShippingFeeAsync(GhnCalculateFeeRequestDto request);

        /// <summary>
        /// Tạo lệnh vận chuyển (Push Order) sang hệ thống GHN.
        /// BƯỚC ĐIỀU PHỐI (DISPATCH): Được kích hoạt khi Admin chốt đơn hoặc Kho hoàn tất đóng gói. 
        /// Service sẽ gom thông tin Sản phẩm, Người nhận, Kho xuất (Hub) để bắn sang GHN.
        /// Sau khi thành công, GHN sẽ trả về Mã vận đơn (Tracking Code) để lưu vào Database, 
        /// phục vụ việc tra cứu lộ trình cho khách hàng.
        /// </summary>
        /// <param name="orderId">Mã định danh của Đơn bán hàng trong hệ thống (Order.Id).</param>
        /// <returns>Mã vận đơn (OrderCode của GHN), thời gian dự kiến giao và tổng cước phí hãng chốt.</returns>
        Task<GhnCreateOrderResponseDto> CreateShippingOrderAsync(int orderId);
        #endregion
    }
}