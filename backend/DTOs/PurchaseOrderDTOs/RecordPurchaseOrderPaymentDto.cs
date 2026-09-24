using System;
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.PurchaseOrderDTOs
{
    /// <summary>
    /// DTO tiếp nhận thông tin ghi nhận thanh toán công nợ cho Đơn mua hàng (PO).
    /// </summary>
    public class RecordPurchaseOrderPaymentDto
    {
        /// <summary>Số tiền thanh toán (VND). Phải lớn hơn 0.</summary>
        [Range(1, double.MaxValue, ErrorMessage = "Số tiền thanh toán phải lớn hơn 0.")]
        public decimal Amount { get; set; }

        /// <summary>Ngày giờ thực hiện chuyển khoản / thanh toán tiền mặt.</summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>Mã tham chiếu ngân hàng / Ủy nhiệm chi / Số giao dịch chuyển khoản.</summary>
        [MaxLength(100)]
        public string? ReferenceCode { get; set; }

        /// <summary>Ghi chú thanh toán.</summary>
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
