using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.ShopDTOs
{
    #region 1. DTO Khách Hàng Yêu Cầu Trả Hàng (Self-Service RMA Create)
    /// <summary>
    /// DTO Chi tiết mặt hàng khách muốn hoàn trả từ giao diện Storefront.
    /// </summary>
    public class ShopReturnItemRequestDto
    {
        #region Hàng hóa & Nguồn gốc
        public int VariantId { get; set; }

        /// <summary>
        /// Mã Lô hàng gốc.
        /// NGHIỆP VỤ TRUY VẾT: Khi khách chọn trả hàng trên App/Web, Frontend sẽ tự động trích xuất BatchId 
        /// từ Lịch sử giao hàng (OrderIssuedItem) truyền lên đây để hệ thống kho biết chính xác phải thu hồi lô nào.
        /// </summary>
        public int BatchId { get; set; }

        public int UoMId { get; set; }
        #endregion

        #region Khối lượng & Giải trình
        /// <summary>Số lượng khách khai báo trả lại.</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>Lý do khách trả riêng cho mặt hàng này (Ví dụ: "Trái cây bị dập", "Hết hạn sử dụng").</summary>
        public string? Reason { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO Yêu cầu Khách hàng tự tạo Phiếu Trả Hàng (Self-Service RMA Request).
    /// BƯỚC 1 CỦA LUỒNG B2C: Khách hàng chủ động vào Lịch sử đơn hàng và bấm "Yêu cầu Trả hàng". 
    /// DTO này sẽ tạo ra một phiếu CustomerReturn trạng thái Pending chờ bộ phận CSKH/Kho tiếp nhận.
    /// </summary>
    public class ShopReturnCreateRequestDto
    {
        /// <summary>
        /// Mã đơn hàng gốc.
        /// Dùng OrderCode thay vì OrderId để tăng tính bảo mật (Tránh ID Insecure Direct Object Reference) 
        /// trên các Public API dành cho Khách hàng.
        /// </summary>
        public required string OrderCode { get; set; }

        /// <summary>Lý do tổng quan của toàn bộ phiếu trả hàng.</summary>
        public string? Reason { get; set; }

        /// <summary>Danh sách các món hàng khách chọn để trả lại.</summary>
        public List<ShopReturnItemRequestDto> Items { get; set; } = new List<ShopReturnItemRequestDto>();
    }
    #endregion

    #region 2. DTO Hiển Thị Kết Quả Trả Hàng Cho Khách (Storefront RMA Read)
    /// <summary>
    /// DTO Hiển thị chi tiết mặt hàng trả lại và Kết quả Kiểm định (QC) cho khách hàng xem.
    /// Tính minh bạch cực cao: Khách hàng thấy rõ hàng của mình bị từ chối hoàn tiền bao nhiêu và vì sao.
    /// </summary>
    public class ShopReturnItemReadDto
    {
        #region Hàng hóa (Flattened)
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }
        public string? BatchCode { get; set; }
        public required string UoMName { get; set; }
        #endregion

        #region Kết quả QC & Trải nghiệm (Transparency)
        /// <summary>Số lượng khách gửi trả ban đầu.</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>Số lượng đạt chuẩn được kho chấp nhận hoàn tiền.</summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>Số lượng bị hỏng/lỗi (Tùy chính sách mà phần này có được hoàn tiền hay không).</summary>
        public decimal DamagedQuantity { get; set; }
        #endregion

        #region Tài chính & Giải trình
        /// <summary>Số tiền thực tế khách được nhận lại cho dòng sản phẩm này.</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Lý do bộ phận QC từ chối hoàn tiền (Ví dụ: "Hàng hỏng do khách bảo quản sai cách").</summary>
        public string? RejectReason { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO Tổng quan Lịch sử Trả hàng dành cho Khách hàng (Storefront View).
    /// Đã ẩn đi toàn bộ các thông tin nội bộ của ERP (WarehouseId, ReceivedById) 
    /// chỉ giữ lại những thông tin mà khách hàng quan tâm: Trạng thái và Tiền hoàn.
    /// </summary>
    public class ShopReturnReadDto
    {
        #region Định danh & Đối soát
        public int Id { get; set; }
        public required string ReturnCode { get; set; }
        public required string OrderCode { get; set; }
        public DateTime ReturnDate { get; set; }
        #endregion

        #region Trạng thái (UI Localized)
        public CustomerReturnStatus Status { get; set; }

        /// <summary>
        /// Tên trạng thái đã được dịch vụ đa ngôn ngữ/UI xử lý (Ví dụ: "Đang kiểm định", "Đã hoàn tiền").
        /// </summary>
        public string StatusName { get; set; } = string.Empty;
        #endregion

        #region Tài chính & Giải trình (Financials & Notes)
        /// <summary>
        /// Tổng số tiền sẽ hoàn lại vào tài khoản/ví của khách hàng.
        /// Chỉ số quan trọng nhất trên màn hình chi tiết đổi trả của App.
        /// </summary>
        public decimal RefundAmount { get; set; }

        public string? Reason { get; set; }

        /// <summary>
        /// Biên bản kết luận cuối cùng từ kho báo về cho khách 
        /// (Ví dụ: "Chúng tôi đã nhận được hàng và đang tiến hành thủ tục hoàn tiền qua thẻ tín dụng").
        /// </summary>
        public string? InspectionNotes { get; set; }
        #endregion

        #region Chi tiết
        /// <summary>Danh sách mặt hàng đi kèm tiến độ kiểm định và số tiền hoàn của từng món.</summary>
        public List<ShopReturnItemReadDto> Details { get; set; } = new List<ShopReturnItemReadDto>();
        #endregion
    }
    #endregion
}