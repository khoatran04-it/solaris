using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper Profile cho phân hệ Đổi trả hàng Storefront (B2C Shop Customer Return / RMA).
    /// Ánh xạ giữa Database Entity và Storefront DTOs, làm phẳng các trường dữ liệu hiển thị cho khách hàng.
    /// </summary>
    public class ShopReturnProfile : Profile
    {
        public ShopReturnProfile()
        {
            #region 1. CustomerReturnDetail -> ShopReturnItemReadDto
            CreateMap<CustomerReturnDetail, ShopReturnItemReadDto>()
                .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.VariantId))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : null))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty))
                .ForMember(dest => dest.ReturnedQuantity, opt => opt.MapFrom(src => src.ReturnedQuantity))
                .ForMember(dest => dest.AcceptedQuantity, opt => opt.MapFrom(src => src.AcceptedQuantity))
                .ForMember(dest => dest.DamagedQuantity, opt => opt.MapFrom(src => src.DamagedQuantity))
                .ForMember(dest => dest.RefundAmount, opt => opt.MapFrom(src => src.RefundAmount))
                .ForMember(dest => dest.RejectReason, opt => opt.MapFrom(src => src.RejectReason));
            #endregion

            #region 2. CustomerReturn -> ShopReturnReadDto
            CreateMap<CustomerReturn, ShopReturnReadDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ReturnCode, opt => opt.MapFrom(src => src.ReturnCode))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.ReturnDate, opt => opt.MapFrom(src => src.ReturnDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => GetReturnStatusName(src.Status)))
                .ForMember(dest => dest.RefundAmount, opt => opt.MapFrom(src => src.RefundAmount))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
                .ForMember(dest => dest.InspectionNotes, opt => opt.MapFrom(src => src.InspectionNotes))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details));
            #endregion
        }

        private static string GetReturnStatusName(CustomerReturnStatus status) => status switch
        {
            CustomerReturnStatus.Pending => "Chờ tiếp nhận",
            CustomerReturnStatus.Approved => "Đã duyệt — Chờ nhận hàng tại kho",
            CustomerReturnStatus.PickingUp => "Đang thu hồi hàng",
            CustomerReturnStatus.Inspecting => "Đang kiểm định QC",
            CustomerReturnStatus.Completed => "Đã hoàn tất & hoàn tiền",
            CustomerReturnStatus.Rejected => "Từ chối trả hàng",
            _ => status.ToString()
        };
    }
}
