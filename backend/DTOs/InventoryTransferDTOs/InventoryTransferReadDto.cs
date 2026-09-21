using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryTransferDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Phiếu Điều Chuyển Liên Kho (Inventory Transfer).
    /// Dữ liệu đã được "làm phẳng" (Flatten) từ 4-5 bảng khác nhau để Frontend có thể dễ dàng vẽ 
    /// sơ đồ luồng luân chuyển (Từ kho A -> Kho B) và dòng thời gian thực hiện (Timeline).
    /// </summary>
    public class InventoryTransferReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã chứng từ điều chuyển (Ví dụ: TRF-20260817-001).</summary>
        public string TransferCode { get; set; } = string.Empty;

        /// <summary>Trạng thái vòng đời luân chuyển (Draft: Nháp, Dispatched: Đang đi đường, Received: Đã nhận, Cancelled: Đã hủy).</summary>
        public InventoryTransferStatus Status { get; set; }
        #endregion

        #region Định tuyến (Routing - Flattened)
        public int FromWarehouseId { get; set; }
        /// <summary>Tên Kho nguồn - Nơi xuất hàng đi.</summary>
        public string FromWarehouseName { get; set; } = string.Empty;

        public int ToWarehouseId { get; set; }
        /// <summary>Tên Kho đích - Nơi dự kiến cập bến.</summary>
        public string ToWarehouseName { get; set; } = string.Empty;
        #endregion

        #region Quy trình 2 bước: Xuất & Nhập (Flattened)
        public int CreatedById { get; set; }
        /// <summary>Tên người khởi tạo lệnh điều chuyển.</summary>
        public string CreatedByName { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        /// <summary>Tên Quản lý đã phê duyệt lệnh chuyển kho.</summary>
        public string? ApprovedByName { get; set; }
        /// <summary>Thời điểm lệnh chuyển được duyệt.</summary>
        public DateTime? ApprovedDate { get; set; }
        /// <summary>Ghi chú của người phê duyệt.</summary>
        public string? ApprovalNote { get; set; }

        public int? DispatchedById { get; set; }
        /// <summary>Tên Thủ kho đã thực hiện xuất hàng lên xe tải (Bước 1).</summary>
        public string? DispatchedByName { get; set; }
        /// <summary>Thời điểm hàng chính thức rời khỏi Kho nguồn.</summary>
        public DateTime? DispatchedDate { get; set; }

        public int? ReceivedById { get; set; }
        /// <summary>Tên Thủ kho đã xác nhận nhận hàng tại bến (Bước 2).</summary>
        public string? ReceivedByName { get; set; }
        /// <summary>Thời điểm hàng chính thức được nhập vào Kho đích.</summary>
        public DateTime? ReceivedDate { get; set; }

        public int? InspectedById { get; set; }
        public string? InspectedByName { get; set; }
        public DateTime? InspectedDate { get; set; }
        #endregion

        #region Vận Tải & Chuyến Xe Liên Kho
        public int? DeliveryTripId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public string? LicensePlate { get; set; }
        #endregion

        #region Đối soát Chứng từ & Ghi chú (Flattened)
        public int? OrderId { get; set; }
        /// <summary>Mã Đơn bán hàng tham chiếu (Nếu có).</summary>
        public string? OrderCode { get; set; }

        /// <summary>Ghi chú vận hành, yêu cầu vận tải.</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy chứng từ (Chỉ hiển thị khi Status = Cancelled).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Hệ thống
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Danh sách Điều chuyển
        /// <summary>Danh sách chi tiết các mặt hàng và lô hàng đang được luân chuyển.</summary>
        public List<InventoryTransferDetailReadDto> Details { get; set; } = new List<InventoryTransferDetailReadDto>();
        #endregion
    }

    /// <summary>
    /// DTO hiển thị chi tiết của một dòng mặt hàng trong Phiếu điều chuyển.
    /// </summary>
    public class InventoryTransferDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Nguồn gốc (Flattened)
        public int VariantId { get; set; }
        /// <summary>Tên hiển thị của Biến thể sản phẩm (SKU).</summary>
        public string VariantName { get; set; } = string.Empty;
        /// <summary>Mã SKU của Biến thể.</summary>
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        /// <summary>Mã Lô hàng (Bảo toàn Truy xuất nguồn gốc giữa các kho).</summary>
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        /// <summary>Tên Đơn vị tính vận chuyển (Ví dụ: Thùng, Pallet).</summary>
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Khối lượng & Kiểm đếm
        /// <summary>Số lượng hàng hóa được xuất đi từ Kho Nguồn.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Số lượng hàng hóa thực nhận nguyên vẹn tại Kho Đích (cộng vào Available).</summary>
        public decimal ActualReceivedQuantity { get; set; }

        /// <summary>Số lượng hàng hóa bị dập nát, hư hỏng trong quá trình vận chuyển (cộng vào Damaged).</summary>
        public decimal DamagedQuantity { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO tiếp nhận kiểm đếm hàng chuyển kho tại Kho đích.
    /// </summary>
    public class InventoryTransferInspectReceiveDto
    {
        public List<InventoryTransferItemInspectDto> Items { get; set; } = new();
        public string? Note { get; set; }
    }

    public class InventoryTransferItemInspectDto
    {
        public int DetailId { get; set; }
        public decimal ActualReceivedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }
    }
}