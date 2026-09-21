using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryReceiptDTOs
{
    /// <summary>
    /// DTO yêu cầu Tạo mới Phiếu Nhập Kho (Inventory Receipt / GRN).
    /// Ghi nhận thông tin chuyến hàng đến, kho tiếp nhận và danh sách kết quả kiểm đếm chất lượng.
    /// </summary>
    public class InventoryReceiptCreateDto
    {
        #region Định tuyến & Đối tác
        /// <summary>Mã định danh Kho hàng tiếp nhận lô hàng này.</summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Mã định danh Nhà cung cấp giao hàng. 
        /// (Có thể null nếu đây là phiếu nhập từ khách hoàn trả, hoặc nhập nội bộ).
        /// </summary>
        public int? SupplierId { get; set; }
        #endregion

        #region Thông tin Giao nhận & Trách nhiệm
        /// <summary>Mã định danh Thủ kho hoặc Nhân viên QC trực tiếp kiểm đếm và ký nhận.</summary>
        public int? ReceivedById { get; set; }

        /// <summary>Thời điểm thực tế xe tải cập bến và bắt đầu bốc dỡ.</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>Ghi chú chung về đợt nhận hàng (Ví dụ: Bao bì bị móp nhẹ, giao trễ so với kế hoạch...).</summary>
        public string? Note { get; set; }
        #endregion

        #region Danh sách Kiểm đếm
        /// <summary>Danh sách chi tiết kết quả kiểm đếm QC cho từng mặt hàng (Phải có ít nhất 1 dòng).</summary>
        public List<InventoryReceiptDetailCreateDto> Details { get; set; } = new List<InventoryReceiptDetailCreateDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ chứa thông tin kiểm đếm của một dòng mặt hàng trong Phiếu nhập kho.
    /// </summary>
    public class InventoryReceiptDetailCreateDto
    {
        #region Hàng hóa & Truy xuất nguồn gốc
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU).</summary>
        public int VariantId { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP: Mã định danh của Lô hàng (Batch).
        /// Nghiệp vụ: Mọi mặt hàng nhập vào kho bắt buộc phải được gắn vào một Lô để phục vụ Truy xuất nguồn gốc 
        /// và thuật toán xuất kho FEFO (Hết hạn trước, xuất trước). Cho phép null khi hàng bị từ chối 100% tại cửa kho.
        /// </summary>
        public int? BatchId { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) lúc nhập.</summary>
        public int UoMId { get; set; }
        #endregion

        #region Đối soát Chứng từ
        /// <summary>
        /// ID dòng chi tiết của Đơn mua hàng (PO Detail) nếu lô hàng này được nhập dựa trên một PO.
        /// Giúp Service tự động cộng dồn số lượng [AcceptedQuantity] vào tiến độ [ReceivedQuantity] của PO tương ứng.
        /// </summary>
        public int? PurchaseOrderDetailId { get; set; }
        #endregion

        #region Số liệu Kiểm đếm (QC Metrics)
        /// <summary>Số lượng dự kiến giao (Theo chứng từ PO hoặc Hóa đơn của NCC).</summary>
        public decimal ExpectedQuantity { get; set; }

        /// <summary>
        /// Số lượng thực tế đạt chuẩn QC.
        /// Con số sống còn: Chỉ số lượng này mới được cộng vào Tồn kho thực tế (QuantityAvailable).
        /// </summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>Số lượng hàng hỏng, sai quy cách bị từ chối nhận tại cửa kho.</summary>
        public decimal RejectedQuantity { get; set; }

        /// <summary>Lý do từ chối (Bắt buộc phải nhập nếu RejectedQuantity > 0 để làm căn cứ trừ công nợ NCC).</summary>
        public string? RejectReason { get; set; }

        /// <summary>Khối lượng cân thực tế tại cửa kho (Kg).</summary>
        public decimal? ActualWeightKg { get; set; }

        /// <summary>Thể tích tính toán của dòng hàng (CBM - m3).</summary>
        public decimal? CalculatedCbm { get; set; }
        #endregion
    }
}