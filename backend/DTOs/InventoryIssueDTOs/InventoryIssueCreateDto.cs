using System;
using System.Collections.Generic;

namespace backend.DTOs.InventoryIssueDTOs
{
    /// <summary>
    /// DTO yêu cầu Tạo mới Phiếu Xuất Kho (Inventory Issue / Goods Issue Note).
    /// Đóng vai trò là lệnh nhặt hàng (Pick) và đóng gói (Pack) để bàn giao cho Đơn vị vận chuyển hoặc luân chuyển nội bộ.
    /// </summary>
    public class InventoryIssueCreateDto
    {
        #region Đối soát Chứng từ & Định tuyến
        /// <summary>
        /// Mã định danh Đơn bán hàng (Sales Order) yêu cầu xuất kho.
        /// (Có thể null nếu xuất hàng với mục đích khác như: Xuất mẫu, xuất tiêu hủy, xuất nội bộ).
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>Mã định danh Kho hàng thực hiện xuất lô hàng này.</summary>
        public int WarehouseId { get; set; }

        /// <summary>Mã định danh Thủ kho hoặc Nhân viên kho (Picker) trực tiếp thực hiện nhặt hàng.</summary>
        public int? IssuedById { get; set; }
        #endregion

        #region Thời gian & Ghi chú
        /// <summary>Thời điểm xuất kho dự kiến hoặc thực tế.</summary>
        public DateTime? IssueDate { get; set; }

        /// <summary>Ghi chú đóng gói hoặc lưu ý vận chuyển (Ví dụ: Hàng dễ dập nát, bọc màng co 3 lớp...).</summary>
        public string? Note { get; set; }
        #endregion

        #region Thông tin Giao nhận (Logistics)
        /// <summary>Tên người nhận (Khách hàng hoặc Tên Tài xế/Đơn vị vận chuyển lấy hàng).</summary>
        public string? ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ giao nhận.</summary>
        public string? ReceiverPhone { get; set; }

        /// <summary>Địa chỉ đích đến để in lên phiếu giao/nhãn dán (Shipping Label).</summary>
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Danh sách Hàng hóa xuất kho
        /// <summary>Danh sách chi tiết các mặt hàng cần xuất đi (Phải có ít nhất 1 dòng).</summary>
        public List<InventoryIssueDetailCreateDto> Details { get; set; } = new List<InventoryIssueDetailCreateDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ chứa thông tin chi tiết của một dòng mặt hàng yêu cầu xuất kho.
    /// </summary>
    public class InventoryIssueDetailCreateDto
    {
        #region Hàng hóa & Truy xuất nguồn gốc
        /// <summary>Mã định danh Biến thể sản phẩm (SKU) cần xuất.</summary>
        public int VariantId { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP: Mã định danh của Lô hàng (Batch).
        /// Nghiệp vụ: Lệnh xuất kho bắt buộc phải chỉ đích danh hàng được nhặt từ Lô nào.
        /// Thông thường, Frontend/Client sẽ gọi một API "Suggest Batch" (Gợi ý lô theo thuật toán FEFO) 
        /// để lấy ID Lô cận Date nhất truyền vào trường này.
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM) dùng lúc xuất kho.</summary>
        public int UoMId { get; set; }
        #endregion

        #region Liên kết Đơn bán hàng
        /// <summary>
        /// ID dòng chi tiết của Đơn bán hàng (Order Line Item) tương ứng.
        /// Giúp hệ thống đánh dấu dòng Đơn hàng này đã được "Giao hàng hoàn tất" (Fulfilled).
        /// </summary>
        public int? OrderDetailId { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>Số lượng yêu cầu xuất đi (Sẽ trừ thẳng vào Tồn kho thực tế - QuantityAvailable khi hoàn tất).</summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn giá xuất kho (VND).
        /// Lưu ý nghiệp vụ: Trong một hệ thống ERP chặt chẽ, Giá vốn hàng bán (COGS) thường do Service backend tự tính 
        /// dựa trên giá của Batch hoặc Bình quân gia quyền. Trường này thường chỉ dùng khi có yêu cầu ghi nhận giá thủ công ngoại lệ.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Tổng khối lượng kiện hàng xuất đi tính bằng Kilogram (Kg).</summary>
        public decimal? TotalWeightKg { get; set; }

        /// <summary>Tổng thể tích kiện hàng xuất đi tính bằng mét khối (CBM - m3).</summary>
        public decimal? TotalCbm { get; set; }
        #endregion
    }
}