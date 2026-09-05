using System;

namespace backend.Helpers
{
    /// <summary>
    /// Tiện ích xử lý ngày giờ chuẩn múi giờ Việt Nam (UTC+7 / SE Asia Standard Time).
    /// Đảm bảo tất cả mã chứng từ (PO, IR, ORD, ISS, TRF, ADJ, RET...) sinh đúng ngày giờ thực tế tại Việt Nam.
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Windows
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); // Linux / macOS
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("Vietnam Time", TimeSpan.FromHours(7), "Vietnam Time", "Vietnam Time");
                }
            }
        }

        /// <summary>
        /// Lấy thời gian hiện tại theo múi giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime VietnamNow
        {
            get
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
            }
        }

        /// <summary>
        /// Chuỗi ngày tháng năm YYYYMMDD theo giờ Việt Nam (VD: 20260901)
        /// </summary>
        public static string VietnamDateString => VietnamNow.ToString("yyyyMMdd");

        /// <summary>
        /// Chuỗi ngày giờ YYYYMMDDHHmmss theo giờ Việt Nam
        /// </summary>
        public static string VietnamDateTimeString => VietnamNow.ToString("yyyyMMddHHmmss");
    }
}
