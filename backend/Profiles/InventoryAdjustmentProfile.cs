using AutoMapper;
using backend.DTOs.InventoryAdjustmentDTOs;
using backend.Models;
using System;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper cho phân hệ Điều chỉnh & Xuất hủy tồn kho (Inventory Adjustment).
    /// Chịu trách nhiệm chuyển đổi (Map) các Thực thể (Entities) sang DTO hiển thị (Read DTO).
    /// Tối ưu hóa hiệu suất API bằng cách "làm phẳng" (Flatten) dữ liệu từ các bảng liên kết.
    /// </summary>
    public class InventoryAdjustmentProfile : Profile
    {
        public InventoryAdjustmentProfile()
        {
            #region 1. Map Phiếu Điều Chỉnh Tổng Quan (Adjustment Header)
            /// <summary>
            /// Ánh xạ chứng từ gốc.
            /// Kéo các thông tin định danh của Kho, Nhân sự và đặc biệt là Mã đợt Kiểm kê gốc (AuditCode) 
            /// ra ngoài cùng để Frontend dễ dàng tạo link truy vết (Traceability) mà không bị N+1 Query.
            /// </summary>
            CreateMap<InventoryAdjustment, InventoryAdjustmentReadDto>()
                // Flatten Thông tin Địa điểm
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))

                // Flatten Đối soát Chứng từ (Truy vết về đợt Kiểm kê gốc nếu có)
                .ForMember(dest => dest.AuditCode, opt => opt.MapFrom(src => src.Audit != null ? src.Audit.AuditCode : null))

                // Flatten Thông tin Nhân sự
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : string.Empty))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))

                // Ánh xạ đệ quy danh sách các dòng biến động
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details));
            #endregion

            #region 2. Map Chi tiết Điều Chỉnh (Adjustment Line Items)
            /// <summary>
            /// Ánh xạ từng dòng mặt hàng điều chỉnh.
            /// Gắn kèm toàn bộ Tên SP, Mã Lô, Hạn sử dụng (ExpiryDate) để Kế toán kho 
            /// dễ dàng đánh giá xem việc xuất hủy/điều chỉnh này có hợp lý hay không.
            /// </summary>
            CreateMap<InventoryAdjustmentDetail, InventoryAdjustmentDetailReadDto>()
                // Flatten Hàng hóa (Product Variant)
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))

                // Flatten Truy xuất nguồn gốc (Batch & Expiry)
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.Batch != null ? (DateTime?)src.Batch.ExpiryDate : null))

                // Flatten Đơn vị tính (UoM)
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
            #endregion

            #region 3. Map DTO Tạo Mới (Create DTOs)
            CreateMap<InventoryAdjustmentCreateDto, InventoryAdjustment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AdjustmentCode, opt => opt.Ignore())
                .ForMember(dest => dest.AdjustmentDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.TotalVarianceAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Audit, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Details, opt => opt.Ignore());

            CreateMap<InventoryAdjustmentDetailCreateDto, InventoryAdjustmentDetail>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.AdjustmentId, opt => opt.Ignore())
                .ForMember(dest => dest.Adjustment, opt => opt.Ignore())
                .ForMember(dest => dest.Variant, opt => opt.Ignore())
                .ForMember(dest => dest.Batch, opt => opt.Ignore())
                .ForMember(dest => dest.UoM, opt => opt.Ignore());
            #endregion
        }
    }
}