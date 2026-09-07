namespace backend.Models
{
    /// <summary>
    /// Thực thể Kho Hàng Vật Lý (Physical Warehouse).
    /// Đơn vị quản lý lưu trữ hàng hóa, kiểm soát số dư tồn kho thực tế và là cứ điểm phục vụ thuật toán định tuyến giao hàng (Smart Routing).
    /// </summary>
    public class Warehouse : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Tên kho hàng (Ví dụ: Tổng Kho Hà Nội, Kho Nông Sản Đà Lạt).</summary>
        public required string Name { get; set; }

        /// <summary>Mã kho duy nhất trên toàn hệ thống (Ví dụ: WH-HN-01, WH-DL-02). Dùng để in mã vạch, nhãn dán hoặc tra cứu nhanh bằng máy quét.</summary>
        public required string Code { get; set; }

        /// <summary>Phân loại vai trò của kho (Ví dụ: Kho tổng - Mega Hub, Kho trung chuyển - Sorting Center, Cửa hàng vật lý - Retail Store).</summary>
        public string? WarehouseType { get; set; }
        #endregion

        #region Sức chứa Vật lý & Cảnh báo (Physical Capacity & Constraints)
        /// <summary>Diện tích mặt sàn hữu dụng (m2).</summary>
        public decimal? TotalAreaSqm { get; set; }

        /// <summary>Sức chứa thể tích tối đa của kho tính bằng mét khối (CBM - m3).</summary>
        public decimal? TotalCapacityCbm { get; set; }

        /// <summary>Tải trọng sàn tối đa cho phép của kho tính bằng Kilogram (Kg).</summary>
        public decimal? MaxWeightCapacityKg { get; set; }

        /// <summary>Số lượng vị trí Pallet tiêu chuẩn tối đa.</summary>
        public int? MaxPalletPositions { get; set; }

        /// <summary>Ngưỡng cảnh báo lấp đầy (Phần trăm %, mặc định 85%).</summary>
        public int WarningThresholdPercent { get; set; } = 85;

        /// <summary>Bán kính tối đa phục vụ giao hàng chuỗi lạnh hỏa tốc của kho (Km, mặc định 15.0 km).</summary>
        public double MaxColdChainRadiusKm { get; set; } = 15.0;
        #endregion

        #region Liên kết Địa lý & Nhân sự
        /// <summary>Mã định danh hồ sơ địa chỉ của kho hàng.</summary>
        public int AddressId { get; set; }

        /// <summary>Thực thể chứa địa chỉ vật lý chi tiết và tọa độ GPS của kho, phục vụ việc đo lường khoảng cách đến Sổ địa chỉ của Khách hàng.</summary>
        public virtual WarehouseAddress? Address { get; set; }

        /// <summary>Mã định danh (User ID) của Quản lý / Trưởng kho chịu trách nhiệm chính về thất thoát, vận hành.</summary>
        public int? ManagerId { get; set; }

        /// <summary>Thực thể Nhân viên (Trưởng kho).</summary>
        public virtual IAUser? Manager { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang vận hành và nhận đơn, false: Tạm đóng cửa / Ngừng xuất nhập hàng).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Phân quyền & Liên kết
        /// <summary>
        /// Danh sách các nhân viên được cấp quyền truy cập làm việc tại kho này.
        /// Ứng dụng Data-Level Authorization (Bảo mật cấp dữ liệu): Nhân viên thuộc kho nào thì chỉ được phép nhìn thấy Phiếu xuất/nhập, Tồn kho và Đơn hàng của kho đó.
        /// </summary>
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();
        #endregion
    }
}