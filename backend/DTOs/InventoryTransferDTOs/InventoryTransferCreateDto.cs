using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryTransferDTOs
{
    /// <summary>
    /// DTO yêu cầu Tạo mới Phiếu Điều Chuyển Liên Kho (Inventory Transfer).
    /// Khởi tạo lệnh luân chuyển hàng hóa vật lý từ Kho này sang Kho khác.
    /// </summary>
    public class InventoryTransferCreateDto
    {
        #region Định tuyến Không gian (Routing)
        /// <summary>
        /// Mã định danh Kho nguồn (Nơi sẽ xuất/trừ hàng).
        /// </summary>
        public int FromWarehouseId { get; set; }

        /// <summary>
        /// Mã định danh Kho đích (Nơi sẽ nhập/cộng hàng).
        /// Nghiệp vụ: FluentValidation phải bắt buộc trường này khác với FromWarehouseId (Không thể tự chuyển cho chính mình).
        /// </summary>
        public int ToWarehouseId { get; set; }
        #endregion

        #region Đối soát Chứng từ & Ghi chú
        /// <summary>
        /// Mã định danh Đơn bán hàng (Order) tham chiếu.
        /// Nghiệp vụ Smart Routing: Được sử dụng khi hệ thống tự động sinh lệnh điều chuyển 
        /// để gom đủ hàng từ các kho khác về kho trung tâm nhằm giao cho khách.
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>Ghi chú vận hành, yêu cầu vận tải (Ví dụ: Chuyển gấp bằng xe lạnh trong đêm...).</summary>
        public string? Note { get; set; }
        #endregion

        #region Nhân sự
        /// <summary>Mã định danh của Điều phối viên hoặc Quản lý kho khởi tạo lệnh điều chuyển này.</summary>
        public int? CreatedById { get; set; }
        #endregion

        #region Danh sách Điều chuyển
        /// <summary>Danh sách chi tiết các mặt hàng và số lượng cần điều chuyển (Phải có ít nhất 1 dòng).</summary>
        public List<InventoryTransferDetailCreateDto> Details { get; set; } = new List<InventoryTransferDetailCreateDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ chứa thông tin chi tiết của một dòng mặt hàng yêu cầu điều chuyển.
    /// </summary>
    public class InventoryTransferDetailCreateDto
    {
        #region Hàng hóa & Truy xuất nguồn gốc
        /// <summary>Mã định danh Biến thể sản phẩm (SKU) cần điều chuyển.</summary>
        public int VariantId { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP VỀ NGUỒN GỐC: Mã định danh của Lô hàng (Batch).
        /// Nghiệp vụ: Khi hàng lên xe tải di chuyển từ Kho A sang Kho B, thông tin Lô (Ngày SX, Hạn SD) 
        /// bắt buộc phải được giữ nguyên để bảo toàn chuỗi Truy xuất nguồn gốc (Traceability).
        /// Không được phép xuất Lô 1 mà sang kho đích lại nhập thành Lô 2.
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) dùng trong quá trình vận chuyển (Ví dụ: Chuyển theo Pallet, Thùng).</summary>
        public int UoMId { get; set; }
        #endregion

        #region Khối lượng
        /// <summary>
        /// Số lượng yêu cầu luân chuyển.
        /// Nghiệp vụ: Số lượng này sẽ bị trừ ở Kho nguồn khi trạng thái là "Dispatched" (Đang đi đường) 
        /// và sẽ được cộng vào Kho đích khi trạng thái chuyển sang "Received" (Đã nhận).
        /// </summary>
        public decimal Quantity { get; set; }
        #endregion
    }
}