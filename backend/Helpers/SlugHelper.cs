using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace backend.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. Chuyển chữ có dấu thành không dấu
            string normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ' || c == 'Đ') sb.Append('d');
                    else sb.Append(c);
                }
            }

            string cleanString = sb.ToString().Normalize(NormalizationForm.FormC);

            // Thay thế đ / Đ nếu còn sót
            cleanString = cleanString.Replace("đ", "d").Replace("Đ", "d");

            // 2. Thay thế ký tự đặc biệt bằng dấu gạch ngang
            cleanString = Regex.Replace(cleanString, @"[^a-z0-9\s-]", "");
            // 3. Thay nhiều khoảng trắng / gạch ngang liên tiếp thành 1 dấu gạch ngang
            cleanString = Regex.Replace(cleanString, @"[\s-]+", "-").Trim('-');

            return cleanString;
        }
    }
}
