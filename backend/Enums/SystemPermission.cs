using System;
using System.Reflection;

namespace backend.Enums
{
    /// <summary>
    /// Metadata bổ trợ gắn kèm từng phần tử Enum SystemPermission để định nghĩa Nhóm Module và Tên hiển thị Tiếng Việt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PermissionInfoAttribute : Attribute
    {
        public string Module { get; }
        public string Name { get; }

        public PermissionInfoAttribute(string module, string name)
        {
            Module = module;
            Name = name;
        }
    }

    /// <summary>
    /// Danh mục Quyền hạn Hệ thống chuẩn hóa (Single Source of Truth) dạng Enum cho toàn bộ 15 Module của Solaris ERP.
    /// </summary>
    public enum SystemPermission
    {
        // 1. Hệ thống & Phân quyền (System & IAM)
        [PermissionInfo("1. Hệ thống", "Xem danh sách Vai trò")]
        ROLE_VIEW,
        [PermissionInfo("1. Hệ thống", "Thêm mới Vai trò")]
        ROLE_CREATE,
        [PermissionInfo("1. Hệ thống", "Chỉnh sửa Vai trò & Phân quyền")]
        ROLE_UPDATE,
        [PermissionInfo("1. Hệ thống", "Xóa Vai trò")]
        ROLE_DELETE,
        [PermissionInfo("1. Hệ thống", "Xem danh sách Nhân viên")]
        USER_VIEW,
        [PermissionInfo("1. Hệ thống", "Thêm mới Nhân viên")]
        USER_CREATE,
        [PermissionInfo("1. Hệ thống", "Chỉnh sửa Hồ sơ & Phân quyền")]
        USER_UPDATE,
        [PermissionInfo("1. Hệ thống", "Xóa & Khóa Nhân viên")]
        USER_DELETE,

        // 2. Đơn vị tính & Quy đổi (UoM)
        [PermissionInfo("2. Đơn vị tính", "Xem Đơn vị tính & Quy đổi")]
        UOM_VIEW,
        [PermissionInfo("2. Đơn vị tính", "Thêm mới Đơn vị tính")]
        UOM_CREATE,
        [PermissionInfo("2. Đơn vị tính", "Chỉnh sửa ĐVT & Quy đổi")]
        UOM_UPDATE,
        [PermissionInfo("2. Đơn vị tính", "Xóa Đơn vị tính")]
        UOM_DELETE,

        // 3. Nhà vườn & Nhà cung cấp (Suppliers)
        [PermissionInfo("3. Nhà cung cấp", "Xem danh sách Nhà cung cấp")]
        SUPPLIER_VIEW,
        [PermissionInfo("3. Nhà cung cấp", "Thêm mới Nhà cung cấp")]
        SUPPLIER_CREATE,
        [PermissionInfo("3. Nhà cung cấp", "Chỉnh sửa Nhà cung cấp & VietGAP")]
        SUPPLIER_UPDATE,
        [PermissionInfo("3. Nhà cung cấp", "Xóa Nhà cung cấp")]
        SUPPLIER_DELETE,

        // 4. Danh mục & Thuộc tính (Categories & Attributes)
        [PermissionInfo("4. Danh mục & Thuộc tính", "Xem Danh mục sản phẩm")]
        CATEGORY_VIEW,
        [PermissionInfo("4. Danh mục & Thuộc tính", "Tạo mới Danh mục")]
        CATEGORY_CREATE,
        [PermissionInfo("4. Danh mục & Thuộc tính", "Chỉnh sửa Danh mục")]
        CATEGORY_UPDATE,
        [PermissionInfo("4. Danh mục & Thuộc tính", "Xóa Danh mục")]
        CATEGORY_DELETE,
        [PermissionInfo("4. Danh mục & Thuộc tính", "Cấu hình Từ điển thuộc tính")]
        ATTRIBUTE_MANAGE,

        // 5. Sản phẩm & Bảng giá (Products & Pricing)
        [PermissionInfo("5. Sản phẩm & Bảng giá", "Xem Danh sách Sản phẩm")]
        PRODUCT_VIEW,
        [PermissionInfo("5. Sản phẩm & Bảng giá", "Thêm mới Sản phẩm nông sản")]
        PRODUCT_CREATE,
        [PermissionInfo("5. Sản phẩm & Bảng giá", "Chỉnh sửa Sản phẩm & Bảng giá")]
        PRODUCT_UPDATE,
        [PermissionInfo("5. Sản phẩm & Bảng giá", "Xóa Sản phẩm & SKU biến thể")]
        PRODUCT_DELETE,

        // 6. Khách hàng & Hạng thẻ (Customers & Tiers)
        [PermissionInfo("6. Khách hàng & Hạng thẻ", "Xem Danh sách Khách hàng")]
        CUSTOMER_VIEW,
        [PermissionInfo("6. Khách hàng & Hạng thẻ", "Thêm mới Khách hàng")]
        CUSTOMER_CREATE,
        [PermissionInfo("6. Khách hàng & Hạng thẻ", "Chỉnh sửa Khách hàng & Hạng thẻ")]
        CUSTOMER_UPDATE,
        [PermissionInfo("6. Khách hàng & Hạng thẻ", "Xóa Khách hàng")]
        CUSTOMER_DELETE,

        // 7. Khuyến mãi & Flash Sale (Promotions)
        [PermissionInfo("7. Khuyến mãi", "Xem Chiến dịch khuyến mãi")]
        PROMOTION_VIEW,
        [PermissionInfo("7. Khuyến mãi", "Tạo mới Chiến dịch khuyến mãi")]
        PROMOTION_CREATE,
        [PermissionInfo("7. Khuyến mãi", "Chỉnh sửa Chiến dịch khuyến mãi")]
        PROMOTION_UPDATE,
        [PermissionInfo("7. Khuyến mãi", "Hủy / Xóa Chiến dịch khuyến mãi")]
        PROMOTION_DELETE,

        // 8. Mạng lưới Kho hàng (Warehouses)
        [PermissionInfo("8. Kho hàng", "Xem Danh sách Kho hàng")]
        WAREHOUSE_VIEW,
        [PermissionInfo("8. Kho hàng", "Thêm mới Chi nhánh Kho")]
        WAREHOUSE_CREATE,
        [PermissionInfo("8. Kho hàng", "Chỉnh sửa Kho hàng & Tọa độ GPS")]
        WAREHOUSE_UPDATE,
        [PermissionInfo("8. Kho hàng", "Xóa Chi nhánh Kho")]
        WAREHOUSE_DELETE,

        // 9. Mua hàng & Quản lý Lô (Purchases & Batches)
        [PermissionInfo("9. Mua hàng & Lô", "Xem Đơn đặt mua hàng (PO)")]
        PURCHASE_VIEW,
        [PermissionInfo("9. Mua hàng & Lô", "Lập Đơn đặt mua hàng (PO)")]
        PURCHASE_CREATE,
        [PermissionInfo("9. Mua hàng & Lô", "Chỉnh sửa Đơn đặt mua (PO)")]
        PURCHASE_UPDATE,
        [PermissionInfo("9. Mua hàng & Lô", "Phê duyệt Đơn mua hàng (PO Approve)")]
        PURCHASE_APPROVE,
        [PermissionInfo("9. Mua hàng & Lô", "Hủy Đơn đặt mua (PO Cancel)")]
        PURCHASE_CANCEL,

        // 10. Vận hành Kho hàng (GRN, GIN, Transfer)
        [PermissionInfo("10. Vận hành Kho", "Xem Phiếu nhập kho (GRN)")]
        RECEIPT_VIEW,
        [PermissionInfo("10. Vận hành Kho", "Lập Phiếu kiểm đếm nhập kho")]
        RECEIPT_CREATE,
        [PermissionInfo("10. Vận hành Kho", "Chốt sổ Hoàn tất Nhập kho (Cộng tồn)")]
        RECEIPT_CONFIRM,
        [PermissionInfo("10. Vận hành Kho", "Xem Phiếu xuất kho (GIN)")]
        ISSUE_VIEW,
        [PermissionInfo("10. Vận hành Kho", "Lập Phiếu xuất kho (Auto FEFO)")]
        ISSUE_CREATE,
        [PermissionInfo("10. Vận hành Kho", "Xem Phiếu chuyển kho")]
        TRANSFER_VIEW,
        [PermissionInfo("10. Vận hành Kho", "Lập & Chốt chuyển kho")]
        TRANSFER_MANAGE,
        [PermissionInfo("10. Vận hành Kho", "Xem Sổ cái & Lịch sử biến động tồn kho")]
        RECONCILIATION_VIEW,
        [PermissionInfo("10. Vận hành Kho", "Chốt ca & Đối soát chênh lệch tồn kho")]
        RECONCILIATION_MANAGE,

        // 11. Kiểm kê & Điều chỉnh (Inventory Audit & Adjustment)
        [PermissionInfo("11. Kiểm kê & Điều chỉnh", "Xem Phiếu kiểm kê kho")]
        AUDIT_VIEW,
        [PermissionInfo("11. Kiểm kê & Điều chỉnh", "Lập Phiếu kiểm kê thực tế")]
        AUDIT_CREATE,
        [PermissionInfo("11. Kiểm kê & Điều chỉnh", "Xem Phiếu điều chỉnh kho")]
        ADJUSTMENT_VIEW,
        [PermissionInfo("11. Kiểm kê & Điều chỉnh", "Lập Phiếu Cân bằng & Điều chỉnh tồn")]
        ADJUSTMENT_CREATE,

        // 12. Đơn bán hàng & Phân bổ (Sales Orders & Smart Routing)
        [PermissionInfo("12. Đơn bán hàng", "Xem Danh sách Đơn bán hàng")]
        ORDER_VIEW,
        [PermissionInfo("12. Đơn bán hàng", "Tạo Đơn bán hàng trực tiếp")]
        ORDER_CREATE,
        [PermissionInfo("12. Đơn bán hàng", "Duyệt & Chuyển trạng thái Đơn hàng")]
        ORDER_PROCESS,
        [PermissionInfo("12. Đơn bán hàng", "Hủy Đơn bán hàng")]
        ORDER_CANCEL,

        // 13. Đổi trả & Kiểm định QC (Customer RMA & QC)
        [PermissionInfo("13. Đổi trả & QC", "Xem Danh sách Phiếu Đổi trả")]
        RETURN_VIEW,
        [PermissionInfo("13. Đổi trả & QC", "Tiếp nhận Yêu cầu Đổi trả")]
        RETURN_CREATE,
        [PermissionInfo("13. Đổi trả & QC", "Kiểm định QC 2 xô & Hoàn tất Đổi trả")]
        RETURN_INSPECT,

        // 14. Vận tải Chuỗi lạnh TMS & Đội xe (Transportation & Fleet Management)
        [PermissionInfo("14. Vận tải & Đội xe", "Xem Bảng điều khiển Vận tải & Chuyến xe")]
        TRIP_VIEW,
        [PermissionInfo("14. Vận tải & Đội xe", "Khởi tạo Chuyến xe Giao hàng / Điều chuyển")]
        TRIP_CREATE,
        [PermissionInfo("14. Vận tải & Đội xe", "Điều phối, Khởi hành & Hoàn tất Chuyến xe TMS")]
        TRIP_MANAGE,
        [PermissionInfo("14. Vận tải & Đội xe", "Hủy Chuyến xe vận tải")]
        TRIP_CANCEL,
        [PermissionInfo("14. Vận tải & Đội xe", "Xem Danh sách Đội xe & Phương tiện")]
        VEHICLE_VIEW,
        [PermissionInfo("14. Vận tải & Đội xe", "Thêm mới Phương tiện vào Đội xe")]
        VEHICLE_CREATE,
        [PermissionInfo("14. Vận tải & Đội xe", "Cập nhật Phương tiện & Tài xế phụ trách")]
        VEHICLE_UPDATE,
        [PermissionInfo("14. Vận tải & Đội xe", "Xóa / Ngừng khai thác Phương tiện")]
        VEHICLE_DELETE,

        // 15. Báo cáo & Thống kê Quản trị (Dashboard & Analytics)
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Bảng tổng quan Kinh doanh (Overview)")]
        DASHBOARD_OVERVIEW,
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Báo cáo Tài chính, Giá vốn & Dòng tiền")]
        DASHBOARD_FINANCE,
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Báo cáo Doanh số & Phân bổ Khách hàng")]
        DASHBOARD_SALES,
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Báo cáo Tồn kho & Sức chứa Kho hàng")]
        DASHBOARD_INVENTORY,
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Báo cáo Chất lượng & Hạn dùng Nông sản (FEFO)")]
        DASHBOARD_QUALITY,
        [PermissionInfo("15. Báo cáo & Thống kê", "Xem Báo cáo Biến động Giá & Biên lãi SKU")]
        DASHBOARD_PRICE
    }

    /// <summary>
    /// Helper trích xuất danh sách Permission Definition từ Enum SystemPermission.
    /// </summary>
    public static class SystemPermissionExtensions
    {
        public static (string Module, string Code, string Name) GetMetadata(this SystemPermission permission)
        {
            var member = typeof(SystemPermission).GetMember(permission.ToString());
            if (member.Length > 0)
            {
                var attr = member[0].GetCustomAttribute<PermissionInfoAttribute>();
                if (attr != null)
                {
                    return (attr.Module, permission.ToString(), attr.Name);
                }
            }
            return ("Khác", permission.ToString(), permission.ToString());
        }

        public static List<(string Module, string Code, string Name)> GetAllDefinitions()
        {
            var list = new List<(string Module, string Code, string Name)>();
            foreach (SystemPermission p in Enum.GetValues(typeof(SystemPermission)))
            {
                list.Add(p.GetMetadata());
            }
            return list;
        }
    }
}
