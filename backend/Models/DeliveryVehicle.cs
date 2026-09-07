using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phương Tiện Vận Tải Nội Bộ (Delivery Vehicle).
    /// Quản lý danh mục xe máy thùng lạnh chặng cuối (B2C) và xe tải lạnh điều chuyển liên kho (B2B).
    /// </summary>
    public class DeliveryVehicle : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Kỹ thuật
        /// <summary>Mã phương tiện (Ví dụ: VEH-BIKE-01, VEH-TRUCK-02).</summary>
        public required string Code { get; set; }

        /// <summary>Biển kiểm soát xe (Ví dụ: 59A-123.45, 50H-987.65).</summary>
        public required string LicensePlate { get; set; }

        /// <summary>Phân loại phương tiện: "Motorbike" (Xe máy thùng lạnh) hoặc "RefrigeratedTruck" (Xe tải lạnh).</summary>
        public required string VehicleType { get; set; }

        /// <summary>Tải trọng chuyên chở tối đa (Kg).</summary>
        public decimal MaxWeightKg { get; set; }

        /// <summary>Phương tiện có trang bị thùng lạnh / mút giữ nhiệt đạt chuẩn không.</summary>
        public bool IsColdChainEquipped { get; set; } = true;

        /// <summary>Trạng thái phương tiện: "Available" (Rảnh rỗi), "OnTrip" (Đang chạy chuyến), "Maintenance" (Bảo dưỡng).</summary>
        public string Status { get; set; } = "Available";
        #endregion

        #region Thông tin Tài xế mặc định
        /// <summary>Họ tên tài xế phụ trách chính.</summary>
        public string? DriverName { get; set; }

        /// <summary>Số điện thoại liên lạc của tài xế.</summary>
        public string? DriverPhone { get; set; }
        #endregion

        #region Kho đỗ / Trực thuộc
        /// <summary>Kho hàng cứ điểm xe đỗ và nhận hàng điều phối.</summary>
        public int? HomeWarehouseId { get; set; }
        public virtual Warehouse? HomeWarehouse { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Navigation
        public virtual ICollection<DeliveryTrip> Trips { get; set; } = new List<DeliveryTrip>();
        #endregion
    }
}
