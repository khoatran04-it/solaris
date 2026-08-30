using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryAuditDTOs
{
    #region 1. DTO Khởi tạo Kiểm kê (Create & Snapshot)
    /// <summary>
    /// DTO Yêu cầu Tạo đợt Kiểm kê mới.
    /// Khi gọi API với DTO này, Service sẽ tự động "Chụp ảnh số dư" (Snapshot) 
    /// của các mặt hàng trong kho để làm mốc đối soát.
    /// </summary>
    public class InventoryAuditCreateDto
    {
        /// <summary>ID Kho hàng cần kiểm kê.</summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Hình thức: Toàn bộ (Full), Cuốn chiếu/Theo khu (Cycle), Đột xuất (Spot).
        /// Nghiệp vụ: Nếu là Full, hệ thống sẽ tự động quét toàn bộ kho. 
        /// Nếu là Cycle/Spot, người dùng phải truyền vào danh sách SpecificItems.
        /// </summary>
        public InventoryAuditType AuditType { get; set; } = InventoryAuditType.Full;

        /// <summary>ID Nhân viên được giao nhiệm vụ đi kiểm đếm (Auditor).</summary>
        public int? AuditorId { get; set; }

        /// <summary>Chỉ đạo/Ghi chú cho đợt kiểm kê.</summary>
        public string? Note { get; set; }

        /// <summary>
        /// Tùy chọn kiểm kê một phần (Dành cho AuditType = Cycle hoặc Spot).
        /// Khai báo đích danh các mặt hàng / lô hàng cần kiểm tra.
        /// </summary>
        public List<InventoryAuditDetailCreateDto>? SpecificItems { get; set; }
    }

    /// <summary>
    /// DTO phụ trợ khai báo mặt hàng khi kiểm kê cuốn chiếu/đột xuất.
    /// </summary>
    public class InventoryAuditDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
    }
    #endregion

    #region 2. DTO Nộp kết quả đếm thực tế (Blind Count Submit)
    /// <summary>
    /// DTO Nộp kết quả đếm thực tế (Dành cho Mobile App / Máy quét mã vạch của nhân viên).
    /// </summary>
    public class InventoryAuditSubmitCountDto
    {
        /// <summary>Danh sách kết quả đếm được ngoài thực tế.</summary>
        public List<InventoryAuditItemCountDto> Items { get; set; } = new List<InventoryAuditItemCountDto>();

        /// <summary>Ghi chú chung của nhân viên sau khi đếm xong.</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO phụ trợ chứa con số thực tế nhân viên đếm được.
    /// NGHIỆP VỤ ĐẾM MÙ (Blind Count): Nhân viên chỉ truyền lên con số Actual, 
    /// Backend sẽ tự lấy Actual trừ đi System (Snapshot) để ra chênh lệch.
    /// </summary>
    public class InventoryAuditItemCountDto
    {
        /// <summary>ID của dòng chi tiết kiểm kê (InventoryAuditDetail.Id).</summary>
        public int DetailId { get; set; }

        /// <summary>Số lượng thực tế đếm được bằng mắt/máy quét ngoài kho.</summary>
        public decimal ActualQuantity { get; set; }

        /// <summary>Lý do/Giải trình nếu nhân viên thấy sự bất thường (Ví dụ: "Hàng bị mốc").</summary>
        public string? ReasonNote { get; set; }
    }
    #endregion

    #region 3. DTO Truy vấn & Hiển thị (Read / Flattened)
    /// <summary>
    /// DTO Hiển thị chi tiết Đợt kiểm kê. 
    /// Dữ liệu đã được "làm phẳng" (Flatten) phục vụ màn hình Đối soát của Kế toán/Quản lý.
    /// </summary>
    public class InventoryAuditReadDto
    {
        public int Id { get; set; }

        #region Định danh & Phân loại
        public string AuditCode { get; set; } = string.Empty;
        public InventoryAuditType AuditType { get; set; }
        public InventoryAuditStatus Status { get; set; }
        #endregion

        #region Địa điểm & Nhân sự (Flattened)
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int AuditorId { get; set; }
        public string AuditorName { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        #endregion

        #region Thời gian
        public DateTime AuditDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        #endregion

        #region Tổng hợp Chênh lệch (Variance Summary)
        /// <summary>Tổng số lượng sổ sách hệ thống.</summary>
        public decimal TotalSystemQty { get; set; }

        /// <summary>Tổng số lượng đếm thực tế.</summary>
        public decimal TotalActualQty { get; set; }

        /// <summary>Tổng số lượng chênh lệch (Thừa/Thiếu).</summary>
        public decimal TotalVarianceQty { get; set; }

        /// <summary>Tổng giá trị chênh lệch (VND).</summary>
        public decimal TotalVarianceAmount { get; set; }
        #endregion

        #region Ghi chú & Hệ thống
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        public List<InventoryAuditDetailReadDto> Details { get; set; } = new List<InventoryAuditDetailReadDto>();
    }

    /// <summary>
    /// DTO Hiển thị kết quả đối soát của một dòng mặt hàng.
    /// </summary>
    public class InventoryAuditDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Lô hàng (Flattened)
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>Hạn sử dụng của Lô (Giúp kiểm toán đánh giá hàng có bị hết Date không).</summary>
        public DateTime? ExpiryDate { get; set; }

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Đối soát Tồn kho (Variance Metrics)
        /// <summary>Tồn kho sổ sách (Snapshot).</summary>
        public decimal SystemQuantity { get; set; }

        /// <summary>Tồn kho thực tế (Auditor nộp lên).</summary>
        public decimal ActualQuantity { get; set; }

        /// <summary>Chênh lệch (Actual - System).</summary>
        public decimal VarianceQuantity { get; set; }
        #endregion

        #region Tài chính & Giải trình
        /// <summary>Đơn giá vốn.</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền chênh lệch (VarianceQuantity * UnitPrice).</summary>
        public decimal VarianceAmount { get; set; }

        public string? ReasonNote { get; set; }
        #endregion
    }
    #endregion
}