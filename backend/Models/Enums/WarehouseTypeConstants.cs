namespace backend.Models.Enums
{
    /// <summary>
    /// Các hằng số chuẩn định danh Loại Kho Hàng (Warehouse Type) trong hệ thống chuỗi cung ứng Solaris.
    /// Đảm bảo tính nhất quán giữa Backend, Database, Frontend và AI Chatbot.
    /// </summary>
    public static class WarehouseTypeConstants
    {
        /// <summary>Kho Tổng (Master Hub) - Trung tâm tiếp nhận hàng trực tiếp từ Nhà cung cấp (PO/GRN).</summary>
        public const string MasterHub = "Kho Tổng";

        /// <summary>Kho Bán Lẻ (Retail Store / Fulfillment Center) - Nơi duy nhất được phép xuất bán trực tiếp cho khách hàng.</summary>
        public const string Retail = "Kho Bán Lẻ";

        /// <summary>Trạm Trung Chuyển (Transit Station / Cross-docking Hub) - Điểm trung gian điều phối và gom hàng.</summary>
        public const string Transit = "Trạm Trung Chuyển";

        /// <summary>Kho Hàng Lỗi (Damaged / Return Warehouse) - Nơi lưu trữ hàng hỏng, hàng lỗi, hàng hết hạn chờ tiêu hủy.</summary>
        public const string Damaged = "Kho Hàng Lỗi";

        /// <summary>Danh sách tất cả các loại kho hợp lệ trong hệ thống.</summary>
        public static readonly string[] ValidTypes = new[]
        {
            MasterHub,
            Retail,
            Transit,
            Damaged
        };

        /// <summary>Kiểm tra xem chuỗi loại kho có hợp lệ trong hệ thống hay không.</summary>
        public static bool IsValid(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return false;
            return ValidTypes.Contains(type.Trim());
        }
    }
}
