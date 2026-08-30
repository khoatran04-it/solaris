using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using System.Linq;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper Profile cho phân hệ Đơn hàng Storefront (B2C Shop Order).
    /// Ánh xạ giữa Database Entity và Storefront DTOs, làm phẳng các trường dữ liệu hiển thị cho khách hàng.
    /// </summary>
    public class ShopOrderProfile : Profile
    {
        public ShopOrderProfile()
        {
            #region 1. OrderDetail -> ShopOrderItemDto
            CreateMap<OrderDetail, ShopOrderItemDto>()
                .ForMember(dest => dest.DetailId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src =>
                    src.Variant != null
                        ? (src.Variant.ImagePath ?? (src.Variant.Product != null ? src.Variant.Product.ImagePath : null))
                        : null))
                .ForMember(dest => dest.UoMId, opt => opt.MapFrom(src => src.UoMId))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.IssuedQuantity, opt => opt.MapFrom(src => src.IssuedQuantity));
            #endregion

            #region 2. Order -> ShopOrderReadDto
            CreateMap<Order, ShopOrderReadDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.OrderCode))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => GetOrderStatusName(src.Status)))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus))
                .ForMember(dest => dest.PaymentStatusName, opt => opt.MapFrom(src => GetPaymentStatusName(src.PaymentStatus)))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
                .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => GetPaymentMethodName(src.PaymentMethod)))
                .ForMember(dest => dest.SubTotal, opt => opt.MapFrom(src => src.SubTotal))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src =>
                    src.DiscountAmount > 0 ? src.DiscountAmount : (src.Details != null ? src.Details.Sum(d => d.DiscountAmount) : 0)))
                .ForMember(dest => dest.ShippingFee, opt => opt.MapFrom(src => src.ShippingFee))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.ReceiverName))
                .ForMember(dest => dest.ReceiverPhone, opt => opt.MapFrom(src => src.ReceiverPhone))
                .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.DeliveryAddress))
                .ForMember(dest => dest.TrackingCode, opt => opt.MapFrom(src => src.TrackingCode))
                .ForMember(dest => dest.ShippingProvider, opt => opt.MapFrom(src => src.ShippingProvider))
                .ForMember(dest => dest.ExpectedDeliveryDate, opt => opt.MapFrom(src => src.ExpectedDeliveryDate))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.CancellationReason, opt => opt.MapFrom(src => src.CancellationReason))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Details));
            #endregion
        }

        private static string GetOrderStatusName(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "Chờ xác nhận",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang chuẩn bị hàng",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Giao thành công",
            OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        private static string GetPaymentStatusName(PaymentStatus status) => status switch
        {
            PaymentStatus.Unpaid => "Chưa thanh toán",
            PaymentStatus.PartiallyPaid => "Thanh toán 1 phần",
            PaymentStatus.Paid => "Đã thanh toán",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => status.ToString()
        };

        private static string GetPaymentMethodName(PaymentMethod method) => method switch
        {
            PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
            PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
            PaymentMethod.EWallet => "Ví điện tử",
            PaymentMethod.CreditCard => "Thẻ tín dụng / Ghi nợ quốc tế",
            _ => method.ToString()
        };
    }
}
