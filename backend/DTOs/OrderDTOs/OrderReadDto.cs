using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO Hiển thị Tổng quan Đơn Bán Hàng (Sales Order Read View).
    /// Đã được "Làm phẳng" (Flatten) toàn bộ dữ liệu từ các bảng liên kết (Customer, Warehouse)
    /// để tối ưu hóa hiệu năng truy vấn cho màn hình Quản lý đơn hàng của Admin hoặc Lịch sử mua hàng của App.
    /// </summary>
    public class OrderReadDto
    {
        #region Định danh & Phân loại
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        #endregion

        #region Khách hàng & Người nhận (Flattened & Snapshot)
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        /// <summary>
        /// Dữ liệu Snapshot (Chụp nhanh). 
        /// Đảm bảo hóa đơn hiển thị chính xác địa chỉ tại thời điểm đặt, bất chấp việc sau này khách có đổi thông tin.
        /// </summary>
        public int? CustomerAddressId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Định tuyến Kho & Vận hành (Routing)
        /// <summary>
        /// Thông tin kho xuất hàng sau khi đã chạy thuật toán Smart Routing.
        /// </summary>
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        #endregion

        #region Tài chính & Đối soát (Financials)
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }

        /// <summary>Tổng tiền thu của khách = SubTotal - DiscountAmount + ShippingFee.</summary>
        public decimal TotalAmount { get; set; }
        #endregion

        #region Ghi chú & Thời gian
        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết (Ordered vs Fulfilled)
        /// <summary>Danh sách mặt hàng khách "Yêu cầu" đặt mua (Dựa trên OrderDetail).</summary>
        public List<OrderDetailReadDto> Details { get; set; } = new();

        /// <summary>
        /// Danh sách mặt hàng "Thực tế" đã xuất kho giao cho khách (Dựa trên InventoryIssue).
        /// Rất quan trọng để theo dõi tiến độ Giao hàng từng phần (Partial Fulfillment) và Truy vết lô hàng (Batch Traceability).
        /// </summary>
        public List<OrderIssuedItemDto> IssuedItems { get; set; } = new();
        #endregion
    }

    /// <summary>
    /// DTO Hiển thị Chi tiết mặt hàng khách đặt mua.
    /// </summary>
    public class OrderDetailReadDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Phân loại (Flattened)
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Khối lượng, Tài chính & Tiến độ
        /// <summary>Số lượng theo Đơn vị tính hiển thị (Ví dụ: 2 Thùng).</summary>
        public decimal Quantity { get; set; }

        /// <summary>Số lượng quy đổi về Base UoM dùng để đối soát với sổ cái Tồn kho.</summary>
        public decimal BaseQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Tiến độ giao hàng lũy kế. 
        /// Kế toán nhìn vào đây để biết dòng sản phẩm này đã giao đủ (IssuedQuantity == BaseQuantity) 
        /// hay đang nợ khách (IssuedQuantity < BaseQuantity).
        /// </summary>
        public decimal IssuedQuantity { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO Hiển thị Chi tiết Thực xuất (Hàng hóa vật lý đã rời khỏi kho).
    /// Kết xuất từ Phiếu xuất kho (InventoryIssue) nhằm phục vụ bài toán Truy xuất nguồn gốc (Traceability).
    /// </summary>
    public class OrderIssuedItemDto
    {
        #region Hàng hóa & Truy vết Lô (Traceability)
        public int? OrderDetailId { get; set; }

        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        /// <summary>
        /// KỶ LUẬT TRUY VẾT: Thông tin Lô hàng (Batch) thực tế đã giao cho khách.
        /// Sinh tử đối với ngành nông sản: Nếu khách khiếu nại ngộ độc hoặc hàng hỏng, 
        /// Admin sẽ nhìn vào BatchCode này để biết chính xác nhà cung cấp nào phân phối lô đó.
        /// </summary>
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        #endregion

        #region Khối lượng & Tài chính
        /// <summary>Số lượng thực tế xuất ra từ lô này.</summary>
        public decimal QuantityIssued { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        #endregion

        #region Đối soát Chứng từ Kho
        /// <summary>Mã phiếu xuất kho (Ví dụ: ISS-20260817-001). Click vào để xem chi tiết lệnh xuất kho.</summary>
        public string IssueCode { get; set; } = string.Empty;

        /// <summary>Ngày giờ thực xuất.</summary>
        public DateTime IssueDate { get; set; }
        #endregion
    }
}