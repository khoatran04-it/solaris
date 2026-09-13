using System.Text.RegularExpressions;

namespace backend.Helpers
{
    public static class GeoHelper
    {
        /// <summary>
        /// Chuẩn hóa chuỗi địa giới hành chính tiếng Việt để so khớp (bỏ dấu, tiền tố tỉnh/tp/quận/huyện/thị xã)
        /// </summary>
        public static string NormalizeLocation(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string slug = SlugHelper.GenerateSlug(text);

            // Loại bỏ tiền tố hành chính cấp tỉnh
            slug = Regex.Replace(slug, @"^(thanh-pho|tinh|tp)-", "");

            // Loại bỏ tiền tố hành chính cấp quận / huyện / thị xã / phường
            slug = Regex.Replace(slug, @"^(quan|q|huyen|h|thi-xa|tx|phuong|p)-", "");

            // Chuẩn hóa q7, q1 -> 7, 1
            slug = Regex.Replace(slug, @"^q(\d+)$", "$1");

            return slug;
        }

        /// <summary>
        /// Kiểm tra xem 2 địa danh hành chính có tương đồng không (ví dụ "Thành Phố Thủ Đức" tương đồng với "Thủ Đức")
        /// </summary>
        public static bool IsSameLocation(string? loc1, string? loc2)
        {
            if (string.IsNullOrWhiteSpace(loc1) || string.IsNullOrWhiteSpace(loc2)) return false;
            string n1 = NormalizeLocation(loc1);
            string n2 = NormalizeLocation(loc2);
            if (string.IsNullOrEmpty(n1) || string.IsNullOrEmpty(n2)) return false;

            if (n1 == n2) return true;

            // Nếu là quận số (ví dụ Quận 1 vs Quận 10), KHÔNG được so khớp Contains!
            bool isN1Numeric = int.TryParse(n1, out _);
            bool isN2Numeric = int.TryParse(n2, out _);
            if (isN1Numeric || isN2Numeric)
            {
                return n1 == n2;
            }

            // Với chuỗi chữ (ví dụ "thu-duc" vs "tp-thu-duc"), cho phép so khớp hai chiều
            return n1.Contains(n2) || n2.Contains(n1);
        }
    }
}
