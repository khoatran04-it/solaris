using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.CustomerReturnDTOs
{
    #region 1. DTO Khởi tạo Yêu cầu Hoàn trả (Create RMA Request)
    /// <summary>
    /// DTO Yêu cầu Tạo Phiếu Khách Hàng Trả Hàng (RMA - Return Merchandise Authorization).
    /// BƯỚC 1: Ghi nhận yêu cầu từ khách hàng (Có thể do CSKH tạo thay khách).
    /// Việc tạo phiếu ở bước này TUYỆT ĐỐI CHƯA làm thay đổi số dư Tồn kho. Hàng hóa phải chờ 
    /// đi qua bước Kiểm định (Inspection) mới được hạch toán.
    /// </summary>
    public class CustomerReturnCreateDto
    {
        #region Đối soát Chứng từ & Định danh
        /// <summary>Mã Đơn bán hàng gốc (Cơ sở để đối chiếu giá trị hoàn tiền).</summary>
        public int OrderId { get; set; }

        /// <summary>Mã Khách hàng yêu cầu trả hàng.</summary>
        public int CustomerId { get; set; }

        /// <summary>Hình thức hoàn trả (Mặc định là PostDeliveryReturn nếu do khách/CSKH tạo).</summary>
        public CustomerReturnType ReturnType { get; set; } = CustomerReturnType.PostDeliveryReturn;
        #endregion

        #region Vận hành & Tiếp nhận
        /// <summary>Kho được chỉ định để tiếp nhận lô hàng hoàn này.</summary>
        public int WarehouseId { get; set; }

        /// <summary>Nhân viên kho hoặc CSKH trực tiếp tạo/tiếp nhận phiếu.</summary>
        public int? ReceivedById { get; set; }

        /// <summary>Ngày khách gửi yêu cầu hoặc ngày dự kiến hàng về tới kho.</summary>
        public DateTime? ReturnDate { get; set; }
        #endregion

        #region Giải trình & Chi tiết
        /// <summary>Lý do khách trả hàng (Ví dụ: "Hàng giao trễ", "Hàng bị hỏng dập").</summary>
        public string? Reason { get; set; }

        /// <summary>Danh sách các mặt hàng khách muốn trả lại.</summary>
        public List<CustomerReturnDetailCreateDto> Details { get; set; } = new();
        #endregion
    }

    /// <summary>
    /// DTO Chi tiết từng mặt hàng trong Yêu cầu Hoàn trả.
    /// </summary>
    public class CustomerReturnDetailCreateDto
    {
        #region Hàng hóa & Nguồn gốc
        public int VariantId { get; set; }

        /// <summary>
        /// Mã Lô hàng gốc. 
        /// NGHIỆP VỤ TRUY VẾT LÕI: Bắt buộc phải truyền lên để nếu hàng đạt chuẩn nhập lại kho, 
        /// hệ thống biết chính xác phải cộng tồn kho vào [Lô] nào, giữ nguyên HSD của lô đó.
        /// </summary>
        public int BatchId { get; set; }

        public int UoMId { get; set; }
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>Số lượng khách khai báo muốn trả lại.</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>
        /// Đơn giá lúc khách mua. 
        /// Nếu Frontend không truyền (null), Backend sẽ tự động móc nối với OrderDetail gốc để lấy giá, 
        /// tránh trường hợp khách/client tự ý sửa giá hoàn tiền cao hơn giá mua thực tế.
        /// </summary>
        public decimal? UnitPrice { get; set; }
        #endregion
    }
    #endregion

    #region 2. DTO Cập nhật Kết quả Kiểm định Kho (QC Inspection Submit)
    /// <summary>
    /// DTO Nộp kết quả Kiểm định Chất lượng (QC).
    /// BƯỚC 2: Thủ kho nhận hàng vật lý, mở thùng kiểm tra và nộp kết quả.
    /// NGHIỆP VỤ LÕI: Khi DTO này được Submit và Approve, hệ thống sẽ gọi IInventoryService 
    /// để kích hoạt luồng cộng/trừ số dư Kho (Available hoặc Damaged) và chốt số tiền Refund.
    /// </summary>
    public class CustomerReturnInspectionDto
    {
        /// <summary>Biên bản tổng kết tình trạng lô hàng (Ví dụ: "Thùng ướt nhẹ, ruột ổn").</summary>
        public string? InspectionNotes { get; set; }

        /// <summary>Danh sách kết quả kiểm định cho từng dòng sản phẩm.</summary>
        public List<CustomerReturnItemInspectionDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO Kết quả kiểm định chi tiết cho 1 dòng mặt hàng.
    /// Giải quyết bài toán phân luồng tồn kho: Hàng nào bán lại được, hàng nào phải đem vứt.
    /// </summary>
    public class CustomerReturnItemInspectionDto
    {
        /// <summary>Mã ID của dòng chi tiết hoàn trả (CustomerReturnDetail.Id).</summary>
        public int DetailId { get; set; }

        #region Kết luận QC (Điều hướng Tồn kho)
        /// <summary>
        /// TỔN KHO KHẢ DỤNG: Số lượng hàng nguyên vẹn, đạt chuẩn.
        /// Sẽ được hệ thống tự động cộng ngược vào Hàng Khả dụng (QuantityAvailable) để tái xuất bán.
        /// (Khách sẽ được hoàn tiền cho phần này).
        /// </summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>
        /// TỒN KHO HÀNG HỎNG: Số lượng hàng móp méo, dập nát, ôi thiu.
        /// Sẽ bị hệ thống đẩy vào kho Hàng Hỏng (QuantityDamaged), tuyệt đối không cho phép bán.
        /// Tùy chính sách, công ty có thể từ chối hoàn tiền cho số lượng bị hỏng này nếu lỗi do khách.
        /// </summary>
        public decimal DamagedQuantity { get; set; }
        #endregion

        /// <summary>
        /// Ghi chú từ chối (Nếu có). 
        /// Ví dụ: "Từ chối hoàn tiền 2 Kg dâu tây vì hỏng do khách bảo quản sai nhiệt độ".
        /// </summary>
        public string? RejectReason { get; set; }
    }
    #endregion
}