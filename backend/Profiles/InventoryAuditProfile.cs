using AutoMapper;
using backend.DTOs.InventoryAuditDTOs;
using backend.Models;
using System;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình AutoMapper cho phân hệ Kiểm kê kho (Inventory Audit).
    /// Chịu trách nhiệm chuyển đổi (Map) các Thực thể (Entities) sang DTO truy vấn (Read DTO).
    /// Hỗ trợ "làm phẳng" (Flatten) các bảng liên kết để tối ưu hóa hiệu suất hiển thị trên Giao diện (UI).
    /// </summary>
    public class InventoryAuditProfile : Profile
    {
        public InventoryAuditProfile()
        {
            #region 1. Map Phiếu Kiểm Kê Tổng Quan (Audit Header)
            /// <summary>
            /// Ánh xạ chứng từ gốc. 
            /// Giải quyết bài toán N+1 Query trên UI bằng cách nạp sẵn Tên Kho và Tên Nhân viên.
            /// </summary>
            CreateMap<InventoryAudit, InventoryAuditReadDto>()
                // Flatten Thông tin Địa điểm
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))

                // Flatten Thông tin Nhân sự tham gia
                .ForMember(dest => dest.AuditorName, opt => opt.MapFrom(src => src.Auditor != null ? src.Auditor.FullName : string.Empty))
                .ForMember(dest => dest.ApprovedByName, opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null))

                // Map đệ quy danh sách chi tiết kiểm đếm
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details));
            #endregion

            #region 2. Map Chi tiết Kiểm Kê (Audit Line Items)
            /// <summary>
            /// Ánh xạ từng dòng mặt hàng kiểm đếm.
            /// Kéo các dữ liệu quan trọng như Tên SP, Mã Lô, Hạn sử dụng (ExpiryDate) ra ngoài cùng 
            /// để Kế toán dễ dàng đánh giá tình trạng hàng hóa khi đối soát.
            /// </summary>
            CreateMap<InventoryAuditDetail, InventoryAuditDetailReadDto>()
                // Flatten Hàng hóa (Product Variant)
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : string.Empty))
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : string.Empty))

                // Flatten Truy xuất nguồn gốc (Batch Traceability)
                .ForMember(dest => dest.BatchCode, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.BatchCode : string.Empty))
                .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.Batch != null ? (DateTime?)src.Batch.ExpiryDate : null))

                // Flatten Đơn vị tính (UoM)
                .ForMember(dest => dest.UoMName, opt => opt.MapFrom(src => src.UoM != null ? src.UoM.Name : string.Empty));
            #endregion

            #region 3. Map DTO Tạo Mới (Create DTOs)
            CreateMap<InventoryAuditCreateDto, InventoryAudit>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AuditCode, opt => opt.Ignore())
                .ForMember(dest => dest.AuditDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedById, opt => opt.Ignore())
                .ForMember(dest => dest.TotalSystemQty, opt => opt.Ignore())
                .ForMember(dest => dest.TotalActualQty, opt => opt.Ignore())
                .ForMember(dest => dest.TotalVarianceQty, opt => opt.Ignore())
                .ForMember(dest => dest.TotalVarianceAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Auditor, opt => opt.Ignore())
                .ForMember(dest => dest.ApprovedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Details, opt => opt.Ignore());
            #endregion
        }
    }
}