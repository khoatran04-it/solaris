using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Điều Chuyển Liên Kho (Inventory Transfer).
    /// Quản lý việc luân chuyển hàng hóa vật lý giữa các kho trong hệ thống.
    /// Nghiệp vụ cốt lõi: Quy trình bắt buộc trải qua 2 bước để phản ánh đúng thời gian thực tế: 
    /// 1. Xuất đi (Dispatched): Trừ tồn kho tại Kho nguồn -> Hàng chuyển sang trạng thái "Đang đi đường" (In Transit).
    /// 2. Nhập nhận (Received): Cộng tồn kho tại Kho đích khi xe tải cập bến an toàn.
    /// </summary>
    public class InventoryTransfer : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã chứng từ điều chuyển duy nhất (Ví dụ: TRF-20260817-001).</summary>
        public required string TransferCode { get; set; }

        /// <summary>
        /// Trạng thái vòng đời của phiếu (Draft: Nháp, Dispatched: Đang đi đường, Received: Đã nhận, Cancelled: Đã hủy).
        /// Nghiệp vụ: Chuyển đổi trạng thái sẽ kích hoạt các Trigger cập nhật số dư kho tương ứng ở nguồn và đích.
        /// </summary>
        public InventoryTransferStatus Status { get; set; } = InventoryTransferStatus.Draft;
        #endregion

        #region Định tuyến (Routing)
        /// <summary>Mã định danh Kho nguồn (Nơi hàng hóa được xuất đi).</summary>
        public int FromWarehouseId { get; set; }
        public virtual Warehouse? FromWarehouse { get; set; }

        /// <summary>Mã định danh Kho đích (Nơi hàng hóa dự kiến cập bến).</summary>
        public int ToWarehouseId { get; set; }
        public virtual Warehouse? ToWarehouse { get; set; }
        #endregion

        #region Quy trình 2 bước: Xuất & Nhập
        /// <summary>Nhân viên điều phối lập phiếu trên hệ thống.</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Quản lý kiểm tra và phê duyệt lệnh chuyển kho.</summary>
        public int? ApprovedById { get; set; }
        public virtual IAUser? ApprovedBy { get; set; }

        /// <summary>Thời điểm lệnh chuyển được phê duyệt.</summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>Ghi chú của người phê duyệt.</summary>
        public string? ApprovalNote { get; set; }

        /// <summary>Thủ kho tại Kho nguồn thực hiện xuất hàng lên xe tải.</summary>
        public int? DispatchedById { get; set; }
        public virtual IAUser? DispatchedBy { get; set; }

        /// <summary>Thời điểm hàng chính thức rời khỏi Kho nguồn.</summary>
        public DateTime? DispatchedDate { get; set; }

        /// <summary>Thủ kho tại Kho đích xác nhận hàng đã cập bến và ký nhận đủ.</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        /// <summary>Thời điểm hàng chính thức được nhập vào Kho đích.</summary>
        public DateTime? ReceivedDate { get; set; }

        /// <summary>Nhân viên kiểm đếm chất lượng và tình trạng hàng hóa khi xe tải cập bến.</summary>
        public int? InspectedById { get; set; }
        public virtual IAUser? InspectedBy { get; set; }

        /// <summary>Thời điểm hoàn tất kiểm đếm hàng chuyển kho.</summary>
        public DateTime? InspectedDate { get; set; }
        #endregion

        #region Vận Tải & Chuyến Xe Liên Kho
        /// <summary>Mã chuyến xe nếu điều chuyển bằng Xe tải lạnh nội bộ của sàn.</summary>
        public int? DeliveryTripId { get; set; }
        public virtual DeliveryTrip? DeliveryTrip { get; set; }

        /// <summary>Họ tên tài xế xe tải lạnh phụ trách chuyên chở.</summary>
        public string? DriverName { get; set; }

        /// <summary>Số điện thoại tài xế xe tải.</summary>
        public string? DriverPhone { get; set; }

        /// <summary>Biển kiểm soát xe tải lạnh.</summary>
        public string? LicensePlate { get; set; }
        #endregion

        #region Đối soát Chứng từ & Ghi chú
        /// <summary>
        /// Mã định danh Đơn bán hàng (Order) tham chiếu.
        /// Nghiệp vụ Smart Routing: Khi Kho A gần khách nhất nhưng lại hết hàng, hệ thống có thể tự động 
        /// sinh ra một phiếu điều chuyển để lấy hàng từ Kho B sang Kho A phục vụ cho Đơn hàng này.
        /// </summary>
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        /// <summary>Ghi chú vận hành (Ví dụ: Xe tải lạnh 2.5T biển 50H-987.65, nhiệt độ thùng 2 độ C...).</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy chứng từ điều chuyển.</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết
        /// <summary>Danh sách chi tiết các mặt hàng và Lô hàng (Batch) được điều chuyển.</summary>
        public virtual ICollection<InventoryTransferDetail> Details { get; set; } = new List<InventoryTransferDetail>();
        #endregion
    }
}