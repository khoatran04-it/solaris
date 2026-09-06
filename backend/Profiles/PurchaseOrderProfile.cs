using AutoMapper;
using backend.DTOs.PurchaseOrderDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Đơn Đặt Mua Hàng (Purchase Order).
    /// Hỗ trợ làm phẳng dữ liệu hiển thị và bảo vệ nghiêm ngặt các trường tài chính/hệ thống 
    /// chống lại lỗ hổng Over-posting khi Thêm mới/Cập nhật.
    /// </summary>
    public class PurchaseOrderProfile : Profile
    {
        public PurchaseOrderProfile()
        {
            #region Đơn mua hàng: Entity -> Read DTO (Truy vấn)
            CreateMap<PurchaseOrder, PurchaseOrderReadDto>()
                // Làm phẳng dữ liệu từ các Navigation Properties để Frontend dễ hiển thị
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Code : string.Empty));
            #endregion

            #region Đơn mua hàng: Create DTO -> Entity (Thêm mới)
            CreateMap<PurchaseOrderCreateDto, PurchaseOrder>()
                // Bỏ qua các trường Hệ thống
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())

                // Bỏ qua các trường Nghiệp vụ (Sẽ được xử lý bằng logic tại tầng Service)
                .ForMember(dest => dest.Status, opt => opt.Ignore()) // Mặc định luôn là Draft khi tạo mới
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore()) // Service sẽ tự tính tổng từ các dòng Details
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())
                .ForMember(dest => dest.CancellationReason, opt => opt.Ignore())

                // Bỏ qua Navigation Properties chống lỗi EF Core Tracking
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore());
            #endregion

            #region Đơn mua hàng: Update / Status Update DTO -> Entity (Cập nhật)
            CreateMap<PurchaseOrderUpdateDto, PurchaseOrder>()
                // KHÓA CHẶT các trường không cho phép sửa thông qua hàm Update thông thường
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderCode, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.Details, opt => opt.Ignore())

                // Bỏ qua các trường Hệ thống & Navigation Properties
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore());

            CreateMap<PurchaseOrderStatusUpdateDto, PurchaseOrder>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderCode, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDate, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.Details, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
            #endregion

            // =========================================================================

            #region Chi tiết Đơn mua: Entity -> Read DTO (Truy vấn)
            CreateMap<PurchaseOrderDetail, PurchaseOrderDetailReadDto>()
                // Làm phẳng dữ liệu Hàng hóa và Đơn vị tính
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
            #endregion

            #region Chi tiết Đơn mua: Create DTO -> Entity (Thêm mới)
            CreateMap<PurchaseOrderDetailCreateDto, PurchaseOrderDetail>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.Ignore())

                // BẢO MẬT NGHIỆP VỤ:
                // Thành tiền sẽ được Service tự tính (OrderQuantity * UnitPrice), không tin tưởng số do Client gửi lên.
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                // Số lượng đã nhận (ReceivedQuantity) bắt buộc phải là 0 lúc mới tạo, không cho phép gán đè.
                .ForMember(dest => dest.ReceivedQuantity, opt => opt.Ignore())

                // Bỏ qua Navigation Properties
                .ForMember(dest => dest.PurchaseOrder, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiptDetails, opt => opt.Ignore());
            #endregion
        }
    }
}