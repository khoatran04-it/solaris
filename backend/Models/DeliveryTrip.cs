using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Chuyến Xe Giao Hàng Nội Bộ (Delivery Trip).
    /// Quản lý một lộ trình xuất bến của xe máy thùng lạnh (gom đơn B2C) hoặc xe tải lạnh (chuyển kho B2B).
    /// </summary>
    public class DeliveryTrip : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã chuyến xe duy nhất (Ví dụ: TRIP-20260907-001).</summary>
        public required string TripCode { get; set; }

        /// <summary>Loại chuyến: "B2C_Delivery" (Giao đơn khách lẻ) hoặc "B2B_Transfer" (Chuyển kho liên chi nhánh).</summary>
        public required string TripType { get; set; }

        /// <summary>Trạng thái chuyến: "Preparing" (Chuẩn bị/Xếp hàng), "InTransit" (Đang đi đường), "Completed" (Hoàn tất), "Cancelled" (Đã hủy).</summary>
        public string Status { get; set; } = "Preparing";
        #endregion

        #region Phương tiện & Tài xế Snapshot
        /// <summary>Mã định danh phương tiện thực hiện chuyến.</summary>
        public int VehicleId { get; set; }
        public virtual DeliveryVehicle? Vehicle { get; set; }

        /// <summary>Biển số xe tại thời điểm khởi hành (Snapshot).</summary>
        public required string LicensePlate { get; set; }

        /// <summary>Tên tài xế điều khiển phương tiện (Snapshot).</summary>
        public required string DriverName { get; set; }

        /// <summary>Số điện thoại tài xế (Snapshot).</summary>
        public required string DriverPhone { get; set; }
        #endregion

        #region Điểm xuất phát & Lịch trình
        /// <summary>Kho hàng xuất phát của chuyến xe.</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Thời điểm chuyến xe chính thức lăn bánh rời kho.</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>Thời điểm toàn bộ lộ trình hoàn tất (tất cả đơn đã giao xong hoặc xe tải cập bến kho đích).</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Ghi chú lộ trình (Ví dụ: Tuyến Quận 1 - Bình Thạnh, giữ nhiệt thùng 2 độ C...).</summary>
        public string? Note { get; set; }
        #endregion

        #region Chuyển kho liên kết (Nếu TripType == B2B_Transfer)
        /// <summary>Mã phiếu chuyển kho liên kết nếu chuyến này là xe tải chở hàng liên kho.</summary>
        public int? InventoryTransferId { get; set; }
        public virtual InventoryTransfer? InventoryTransfer { get; set; }
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

        #region Danh sách Đơn hàng thuộc chuyến (Nếu TripType == B2C_Delivery)
        public virtual ICollection<DeliveryTripOrder> TripOrders { get; set; } = new List<DeliveryTripOrder>();
        #endregion

        #region Danh sách Phiếu Trả Hàng thu hồi trong chuyến (Nếu TripType == B2C_Return)
        public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();
        #endregion
    }
}
