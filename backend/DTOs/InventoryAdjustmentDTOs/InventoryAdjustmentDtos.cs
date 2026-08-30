using backend.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.InventoryAdjustmentDTOs
{
    #region 1. DTO Khởi tạo Điều chỉnh (Create)
    /// <summary>
    /// DTO Yêu cầu Tạo Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho.
    /// Đóng vai trò là "Van an toàn" giúp Thủ kho và Kế toán hợp thức hóa các sai lệch 
    /// (Hao hụt, Mất mát, Hư hỏng) hoặc cân bằng số liệu sau khi có kết quả Kiểm kê.
    /// </summary>
    public class InventoryAdjustmentCreateDto
    {
        #region Định tuyến & Phân loại
        [Required(ErrorMessage = "Vui lòng chọn kho hàng xảy ra biến động")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do điều chỉnh")]
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;
        #endregion

        #region Đối soát Chứng từ
        /// <summary>
        /// ID Đợt kiểm kê gốc (Nếu có).
        /// Giúp truy vết: Phiếu điều chỉnh này được lập ra độc lập hay là kết quả của một đợt kiểm đếm định kỳ.
        /// </summary>
        public int? AuditId { get; set; }
        #endregion

        #region Nhân sự & Ghi chú
        /// <summary>Mã ID của nhân viên lập phiếu đề xuất điều chỉnh.</summary>
        public int? CreatedById { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự")]
        public string? Note { get; set; }
        #endregion

        #region Danh sách Chi tiết
        [Required(ErrorMessage = "Danh sách chi tiết không được để trống")]
        [MinLength(1, ErrorMessage = "Phiếu điều chỉnh phải có ít nhất 1 dòng chi tiết")]
        public List<InventoryAdjustmentDetailCreateDto> Details { get; set; } = new List<InventoryAdjustmentDetailCreateDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ khai báo chi tiết từng mặt hàng cần điều chỉnh.
    /// </summary>
    public class InventoryAdjustmentDetailCreateDto
    {
        #region Hàng hóa & Nguồn gốc
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm biến thể")]
        public int VariantId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lô hàng")]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính")]
        public int UoMId { get; set; }
        #endregion

        #region Số liệu & Loại điều chỉnh
        [Required(ErrorMessage = "Vui lòng chọn loại điều chỉnh")]
        public InventoryAdjustmentType AdjustmentType { get; set; }

        /// <summary>
        /// NGHIỆP VỤ KẾ TOÁN: Số lượng điều chỉnh luôn phải là SỐ DƯƠNG (Absolute value).
        /// Việc cộng/trừ sẽ do trường [AdjustmentType] quyết định. 
        /// Không cho phép nhập số âm để tránh double-negative (trừ đi một số âm thành cộng).
        /// </summary>
        [Range(0.0001, 1000000, ErrorMessage = "Số lượng điều chỉnh phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [Range(0, 100000000000, ErrorMessage = "Đơn giá không được âm")]
        public decimal UnitPrice { get; set; }
        #endregion

        #region Giải trình
        [StringLength(500, ErrorMessage = "Chi tiết lý do tối đa 500 ký tự")]
        public string? ReasonDetail { get; set; }
        #endregion
    }
    #endregion

    #region 2. DTO Truy vấn & Hiển thị (Read / Flattened)
    /// <summary>
    /// DTO Hiển thị chi tiết Phiếu Điều Chỉnh.
    /// Dữ liệu đã được "làm phẳng" (Flatten) để tối ưu cho việc render UI và đối soát của Kế toán.
    /// </summary>
    public class InventoryAdjustmentReadDto
    {
        public int Id { get; set; }

        #region Định danh & Trạng thái
        public string AdjustmentCode { get; set; } = string.Empty;
        public InventoryAdjustmentStatus Status { get; set; }
        public InventoryAdjustmentReason Reason { get; set; }
        #endregion

        #region Đối tượng liên quan (Flattened)
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int? AuditId { get; set; }
        public string? AuditCode { get; set; }

        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        #endregion

        #region Thời gian & Tài chính
        public DateTime AdjustmentDate { get; set; }
        public DateTime? ApprovedDate { get; set; }

        /// <summary>Tổng giá trị hạch toán (VND).</summary>
        public decimal TotalVarianceAmount { get; set; }
        #endregion

        #region Hệ thống & Ghi chú
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        #endregion

        public List<InventoryAdjustmentDetailReadDto> Details { get; set; } = new List<InventoryAdjustmentDetailReadDto>();
    }

    /// <summary>
    /// DTO Hiển thị một dòng mặt hàng đã được điều chỉnh.
    /// </summary>
    public class InventoryAdjustmentDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Lô hàng (Flattened)
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Loại Điều chỉnh & Số liệu
        public InventoryAdjustmentType AdjustmentType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        #endregion

        public string? ReasonDetail { get; set; }
    }
    #endregion
}