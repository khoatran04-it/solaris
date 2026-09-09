using System.Text.RegularExpressions;

namespace backend.Helpers
{
    public static class GeoHelper
    {
        /// <summary>
        /// Chuẩn hóa chuỗi địa giới hành chính tiếng Việt để so khớp (bỏ dấu, tiền tố tỉnh/tp/quận/huyện)
        /// </summary>
        public static string NormalizeLocation(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string slug = SlugHelper.GenerateSlug(text);

            // Loại bỏ tiền tố hành chính cấp tỉnh
            slug = Regex.Replace(slug, @"^(thanh-pho|tinh|tp)-", "");

            // Chuẩn hóa tiền tố quận / huyện
            if (slug.StartsWith("quan-") || slug.StartsWith("q-"))
            {
                slug = "q" + slug.Substring(slug.IndexOf('-') + 1);
            }
            else if (slug.StartsWith("huyen-") || slug.StartsWith("h-"))
            {
                slug = "h" + slug.Substring(slug.IndexOf('-') + 1);
            }
            else if (slug.StartsWith("phuong-") || slug.StartsWith("p-"))
            {
                slug = "p" + slug.Substring(slug.IndexOf('-') + 1);
            }

            return slug;
        }

        /// <summary>
        /// Kiểm tra xem 2 địa danh hành chính có tương đồng không (ví dụ "TP. Hồ Chí Minh" tương đồng với "Ho Chi Minh")
        /// </summary>
        public static bool IsSameLocation(string? loc1, string? loc2)
        {
            if (string.IsNullOrWhiteSpace(loc1) || string.IsNullOrWhiteSpace(loc2)) return false;
            string n1 = NormalizeLocation(loc1);
            string n2 = NormalizeLocation(loc2);
            if (string.IsNullOrEmpty(n1) || string.IsNullOrEmpty(n2)) return false;
            return n1 == n2 || n1.Contains(n2) || n2.Contains(n1);
        }
    }
}
