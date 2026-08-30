namespace backend.DTOs.ShippingDTOs
{
    #region 1. Dữ liệu Hành chính chuẩn Giao Hàng Nhanh (GHN Master Data)
    /// <summary>
    /// DTO Đại diện cho Tỉnh/Thành phố theo chuẩn dữ liệu của GHN.
    /// NGHIỆP VỤ ĐỒNG BỘ: Để API của GHN hiểu được địa chỉ giao hàng, hệ thống tuyệt đối không dùng 
    /// ID Tỉnh/Thành nội bộ mà phải map 1-1 với ProvinceID do chính GHN cung cấp.
    /// </summary>
    public class GhnProvinceDto
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;

        /// <summary>Mã code tỉnh (Thường dùng cho các hệ thống ERP đồng bộ chéo).</summary>
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO Đại diện cho Quận/Huyện theo chuẩn GHN.
    /// </summary>
    public class GhnDistrictDto
    {
        public int DistrictID { get; set; }
        public int ProvinceID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO Đại diện cho Phường/Xã theo chuẩn GHN.
    /// LƯU Ý KỸ THUẬT: API của GHN quy định WardCode là kiểu chuỗi (string), khác với ID của Quận/Tỉnh là số (int).
    /// </summary>
    public class GhnWardDto
    {
        public string WardCode { get; set; } = string.Empty;
        public int DistrictID { get; set; }
        public string WardName { get; set; } = string.Empty;
    }
    #endregion

    #region 2. Tính Phí Vận Chuyển & Chính sách Freeship (Shipping Fee & Rules)
    /// <summary>
    /// DTO Yêu cầu Tính phí Vận chuyển (Gửi sang GHN).
    /// Được gọi liên tục (Real-time) mỗi khi khách hàng đổi địa chỉ ở màn hình Checkout.
    /// </summary>
    public class GhnCalculateFeeRequestDto
    {
        /// <summary>Mã Quận/Huyện đích đến.</summary>
        public int ToDistrictId { get; set; }

        /// <summary>Mã Phường/Xã đích đến.</summary>
        public required string ToWardCode { get; set; }

        /// <summary>
        /// Tổng trọng lượng đơn hàng (Tính bằng Gram).
        /// NGHIỆP VỤ ĐÓNG GÓI: GHN tính phí dựa trên khối lượng. Mặc định gán 1000g (1kg) 
        /// nếu hệ thống chưa bóc tách được trọng lượng tịnh của từng mặt hàng.
        /// </summary>
        public int WeightGram { get; set; } = 1000;

        /// <summary>
        /// Tổng giá trị đơn hàng.
        /// BẢO HIỂM HÀNG HÓA: Truyền lên GHN để tính phí bảo hiểm (Insurance Fee) đề phòng mất mát trong quá trình vận chuyển.
        /// </summary>
        public decimal SubTotal { get; set; }
    }

    /// <summary>
    /// DTO Phản hồi Phí vận chuyển (Đã tích hợp Logic Business nội bộ).
    /// Đây là một DTO rất thông minh vì nó kết hợp cả dữ liệu raw từ GHN và chính sách Khuyến mãi (Freeship) của công ty.
    /// </summary>
    public class GhnCalculateFeeResponseDto
    {
        /// <summary>Phí ship thực tế khách phải trả sau khi áp dụng các luật Freeship nội bộ.</summary>
        public decimal TotalFee { get; set; }

        /// <summary>Phí ship gốc do GHN báo giá (Dùng để hiển thị gạch ngang trên UI).</summary>
        public decimal OriginalFee { get; set; }

        /// <summary>Cờ đánh dấu đơn này có đang được Miễn phí vận chuyển hay không.</summary>
        public bool IsFreeShipping { get; set; }

        /// <summary>Mốc tiền hàng tối thiểu để được Freeship (Ví dụ: Mua trên 500k miễn phí giao hàng).</summary>
        public decimal FreeShippingThreshold { get; set; }

        /// <summary>
        /// Số tiền khách CẦN MUA THÊM để đạt mốc Freeship.
        /// CHIẾN LƯỢC MARKETING (Upsell): Giúp Frontend hiển thị thanh tiến trình "Mua thêm 50k nữa để được Freeship!".
        /// </summary>
        public decimal AmountNeededForFreeShipping { get; set; }

        /// <summary>Thời gian dự kiến hàng tới tay khách (Dựa trên SLA của GHN).</summary>
        public string? ExpectedDeliveryTime { get; set; }
    }
    #endregion

    #region 3. Tạo Đơn Giao Hàng (Push Order to Logistics Provider)
    /// <summary>
    /// DTO Phản hồi từ GHN sau khi Hệ thống chốt đơn và "Đẩy" lệnh vận chuyển sang hãng.
    /// Xảy ra tự động khi Admin/Điều phối viên chuyển trạng thái Order sang 'Packing' hoặc 'Shipping'.
    /// </summary>
    public class GhnCreateOrderResponseDto
    {
        /// <summary>
        /// MÃ VẬN ĐƠN (Tracking Code). 
        /// NGHIỆP VỤ THEO DÕI: Cực kỳ quan trọng. Hệ thống sẽ lưu mã này vào cột [TrackingCode] của bảng Order, 
        /// sau đó gửi SMS/Email cho khách để họ lên app GHN tra cứu lộ trình shipper.
        /// </summary>
        public string? OrderCode { get; set; }

        /// <summary>Thời gian dự kiến giao thành công.</summary>
        public string? ExpectedDeliveryDate { get; set; }

        /// <summary>Tổng cước phí cuối cùng hãng chốt thu của cửa hàng (Dùng cho Kế toán đối soát công nợ cước vận chuyển).</summary>
        public decimal TotalFee { get; set; }
    }
    #endregion

    #region 4. Cấu trúc Phản hồi gốc từ API GHN (Generic API Wrapper)
    /// <summary>
    /// DTO Vỏ bọc (Wrapper) tiêu chuẩn để Deserialization dữ liệu HTTP Response từ API của GHN.
    /// Giúp Backend xử lý lỗi (Exception handling) một cách thống nhất khi GHN bảo trì hoặc sập server.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu Model động trả về bên trong Data.</typeparam>
    public class GhnApiResponse<T>
    {
        /// <summary>Mã HTTP Code hoặc Mã lỗi nghiệp vụ riêng của GHN (Ví dụ: 200 = Success).</summary>
        public int Code { get; set; }

        /// <summary>Thông báo lỗi chi tiết từ GHN (Ví dụ: "Khối lượng không hợp lệ").</summary>
        public string? Message { get; set; }

        /// <summary>Khối dữ liệu JSON thực sự chứa thông tin (Tùy thuộc vào từng Endpoint gọi sang GHN).</summary>
        public T? Data { get; set; }
    }
    #endregion
}