using System;

namespace backend.DTOs.InventoryDTOs
{
    #region DTO Truy vấn (Read)
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Kho hàng.
    /// Dữ liệu địa chỉ được "làm phẳng" (Flatten) trực tiếp vào DTO này để Frontend dễ dàng bind data lên bảng.
    /// </summary>
    public class WarehouseReadDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }

        public int? ManagerId { get; set; }
        /// <summary>Tên đầy đủ của Quản lý kho (Được ánh xạ từ IAUser.FullName).</summary>
        public string? ManagerName { get; set; }

        // --- Địa chỉ (Flattened) ---
        public int AddressId { get; set; }
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        /// <summary>Địa chỉ đầy đủ đã được ghép nối.</summary>
        public string FullAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // --- Thông số Sức chứa Vật lý ---
        public decimal? TotalAreaSqm { get; set; }
        public decimal? TotalCapacityCbm { get; set; }
        public decimal? MaxWeightCapacityKg { get; set; }
        public int? MaxPalletPositions { get; set; }
        public int WarningThresholdPercent { get; set; } = 85;

        // --- Trạng thái ---
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    #endregion

    #region DTO Phụ trợ (Payload)
    /// <summary>
    /// DTO phụ trợ (Nested Payload) chứa dữ liệu địa chỉ khi thực hiện thao tác Tạo mới hoặc Cập nhật.
    /// </summary>
    public class WarehouseAddressPayload
    {
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
    #endregion

    #region DTO Thêm mới (Create)
    /// <summary>
    /// DTO yêu cầu Tạo mới Kho hàng.
    /// Bắt buộc khởi tạo kèm theo một bộ dữ liệu địa chỉ vật lý đầy đủ.
    /// </summary>
    public class WarehouseCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }

        public decimal? TotalAreaSqm { get; set; }
        public decimal? TotalCapacityCbm { get; set; }
        public decimal? MaxWeightCapacityKg { get; set; }
        public int? MaxPalletPositions { get; set; }
        public int WarningThresholdPercent { get; set; } = 85;

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Khối dữ liệu địa lý bắt buộc đi kèm khi tạo kho.</summary>
        public required WarehouseAddressPayload Address { get; set; }
    }
    #endregion

    #region DTO Cập nhật (Update)
    /// <summary>
    /// DTO yêu cầu Cập nhật thông tin Kho hàng.
    /// Lưu ý: Thuộc tính "Code" (Mã kho) không xuất hiện ở đây để ngăn chặn việc thay đổi mã định danh.
    /// </summary>
    public class WarehouseUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }

        public decimal? TotalAreaSqm { get; set; }
        public decimal? TotalCapacityCbm { get; set; }
        public decimal? MaxWeightCapacityKg { get; set; }
        public int? MaxPalletPositions { get; set; }
        public int WarningThresholdPercent { get; set; } = 85;

        public int? ManagerId { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Thông tin địa chỉ cập nhật để ghi đè lên bản ghi cũ.</summary>
        public required WarehouseAddressPayload Address { get; set; }
    }
    #endregion

    #region DTO Báo cáo Sức chứa (Capacity Status)
    /// <summary>
    /// DTO cung cấp trạng thái sức chứa tức thời (CBM, Tải trọng kg) của một kho hàng.
    /// </summary>
    public class WarehouseCapacityStatusDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;

        // Thể tích
        public decimal TotalCapacityCbm { get; set; }
        public decimal OccupiedCbm { get; set; }
        public decimal AvailableCbm { get; set; }
        public decimal OccupancyRateCbm { get; set; }

        // Tải trọng
        public decimal MaxWeightCapacityKg { get; set; }
        public decimal OccupiedWeightKg { get; set; }
        public decimal AvailableWeightKg { get; set; }
        public decimal OccupancyRateWeight { get; set; }

        // Đánh giá
        public int WarningThresholdPercent { get; set; } = 85;
        public string Status { get; set; } = "Safe"; // "Safe", "Warning", "Critical"
    }
    #endregion
}