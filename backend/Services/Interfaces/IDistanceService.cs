namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service tiện ích xử lý các bài toán Tính toán Địa lý (Geospatial Service).
    /// Đóng vai trò hạt nhân trong các nghiệp vụ Logistic của hệ thống như: 
    /// 1. Định tuyến đơn hàng (Smart Routing): Tìm kho hàng gần khách hàng nhất để xuất hàng.
    /// 2. Tính phí giao hàng (Shipping Fee): Tính cước vận chuyển dựa trên số Kilomet.
    /// 3. Kiểm tra bán kính phục vụ: Xác định khách hàng có nằm trong vùng giao hàng của Cửa hàng/Kho hay không.
    /// </summary>
    public interface IDistanceService
    {
        #region Tính toán Địa lý (Geospatial Calculation)
        /// <summary>
        /// Tính toán khoảng cách tuyến tính ("đường chim bay") giữa 2 điểm tọa độ trên bản đồ Trái Đất.
        /// Thường được cài đặt (Implement) bằng Công thức Haversine (Haversine Formula) để bù đắp độ cong của bề mặt Trái Đất.
        /// </summary>
        /// <param name="lat1">Vĩ độ (Latitude) của điểm xuất phát (Ví dụ: Tọa độ Kho hàng - WarehouseAddress.Latitude).</param>
        /// <param name="lon1">Kinh độ (Longitude) của điểm xuất phát (Ví dụ: Tọa độ Kho hàng - WarehouseAddress.Longitude).</param>
        /// <param name="lat2">Vĩ độ (Latitude) của điểm đích (Ví dụ: Tọa độ Khách hàng - CustomerAddress.Latitude).</param>
        /// <param name="lon2">Kinh độ (Longitude) của điểm đích (Ví dụ: Tọa độ Khách hàng - CustomerAddress.Longitude).</param>
        /// <returns>
        /// Trả về khoảng cách giữa 2 điểm tính bằng Kilomet (km). 
        /// Kết quả thường là kiểu double độ chính xác cao (Ví dụ: 5.43 km).
        /// </returns>
        double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
        #endregion
    }
}