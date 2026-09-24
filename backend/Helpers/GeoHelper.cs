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

        /// <summary>
        /// Điểm tọa độ trọng tâm hành chính (GIS Centroid)
        /// </summary>
        public readonly record struct CentroidPoint(double Latitude, double Longitude, string DistrictName, string ProvinceName);

        private static readonly List<CentroidPoint> CentroidList = new()
        {
            // === 1. THÀNH PHỐ HỒ CHÍ MINH (24 Quận / Huyện / TP) ===
            new(10.7756, 106.7004, "Quận 1", "Thành phố Hồ Chí Minh"),
            new(10.7844, 106.6844, "Quận 3", "Thành phố Hồ Chí Minh"),
            new(10.7630, 106.7070, "Quận 4", "Thành phố Hồ Chí Minh"),
            new(10.7540, 106.6630, "Quận 5", "Thành phố Hồ Chí Minh"),
            new(10.7480, 106.6350, "Quận 6", "Thành phố Hồ Chí Minh"),
            new(10.7340, 106.7210, "Quận 7", "Thành phố Hồ Chí Minh"),
            new(10.7240, 106.6280, "Quận 8", "Thành phố Hồ Chí Minh"),
            new(10.7710, 106.6670, "Quận 10", "Thành phố Hồ Chí Minh"),
            new(10.7660, 106.6500, "Quận 11", "Thành phố Hồ Chí Minh"),
            new(10.8670, 106.6410, "Quận 12", "Thành phố Hồ Chí Minh"),
            new(10.8105, 106.7091, "Bình Thạnh", "Thành phố Hồ Chí Minh"),
            new(10.8380, 106.6650, "Gò Vấp", "Thành phố Hồ Chí Minh"),
            new(10.7990, 106.6800, "Phú Nhuận", "Thành phố Hồ Chí Minh"),
            new(10.8010, 106.6540, "Tân Bình", "Thành phố Hồ Chí Minh"),
            new(10.7900, 106.6280, "Tân Phú", "Thành phố Hồ Chí Minh"),
            new(10.7650, 106.6040, "Bình Tân", "Thành phố Hồ Chí Minh"),
            new(10.8500, 106.7720, "Thủ Đức", "Thành phố Hồ Chí Minh"),
            new(10.7870, 106.7490, "Quận 2", "Thành phố Hồ Chí Minh"),
            new(10.8420, 106.8280, "Quận 9", "Thành phố Hồ Chí Minh"),
            new(10.6950, 106.7320, "Nhà Bè", "Thành phố Hồ Chí Minh"),
            new(10.6870, 106.5930, "Bình Chánh", "Thành phố Hồ Chí Minh"),
            new(10.8840, 106.5920, "Hóc Môn", "Thành phố Hồ Chí Minh"),
            new(11.0060, 106.5130, "Củ Chi", "Thành phố Hồ Chí Minh"),
            new(10.4110, 106.9540, "Cần Giờ", "Thành phố Hồ Chí Minh"),

            // === 2. TỈNH BÌNH DƯƠNG (9 TP / Thị xã / Huyện) ===
            new(10.9805, 106.6520, "Thủ Dầu Một", "Bình Dương"),
            new(10.9060, 106.7710, "Dĩ An", "Bình Dương"),
            new(10.9250, 106.6970, "Thuận An", "Bình Dương"),
            new(11.0720, 106.7760, "Tân Uyên", "Bình Dương"),
            new(11.1350, 106.6050, "Bến Cát", "Bình Dương"),
            new(11.2670, 106.6120, "Bàu Bàng", "Bình Dương"),
            new(11.1540, 106.8620, "Bắc Tân Uyên", "Bình Dương"),
            new(11.2910, 106.3710, "Dầu Tiếng", "Bình Dương"),
            new(11.3120, 106.8040, "Phú Giáo", "Bình Dương"),

            // === 3. TỈNH ĐỒNG NAI (11 TP / Huyện) ===
            new(10.9575, 106.8425, "Biên Hòa", "Đồng Nai"),
            new(10.6870, 106.8870, "Nhơn Trạch", "Đồng Nai"),
            new(10.7410, 106.9540, "Long Thành", "Đồng Nai"),
            new(10.9420, 107.2410, "Long Khánh", "Đồng Nai"),
            new(10.9510, 107.0120, "Trảng Bom", "Đồng Nai"),
            new(11.0540, 106.9120, "Vĩnh Cửu", "Đồng Nai"),
            new(10.9820, 107.1350, "Thống Nhất", "Đồng Nai"),
            new(10.8250, 107.2180, "Cẩm Mỹ", "Đồng Nai"),
            new(10.9120, 107.3820, "Xuân Lộc", "Đồng Nai"),
            new(11.2050, 107.2510, "Định Quán", "Đồng Nai"),
            new(11.4120, 107.4120, "Tân Phú", "Đồng Nai"),

            // === 4. TỈNH BÀ RỊA - VŨNG TÀU (8 TP / Thị xã / Huyện) ===
            new(10.3460, 107.0843, "Vũng Tàu", "Bà Rịa - Vũng Tàu"),
            new(10.5010, 107.1720, "Bà Rịa", "Bà Rịa - Vũng Tàu"),
            new(10.5890, 107.0540, "Phú Mỹ", "Bà Rịa - Vũng Tàu"),
            new(10.4670, 107.1920, "Long Điền", "Bà Rịa - Vũng Tàu"),
            new(10.4950, 107.2840, "Đất Đỏ", "Bà Rịa - Vũng Tàu"),
            new(10.5820, 107.4610, "Xuyên Mộc", "Bà Rịa - Vũng Tàu"),
            new(10.6380, 107.2520, "Châu Đức", "Bà Rịa - Vũng Tàu"),
            new(8.6830, 106.6080, "Côn Đảo", "Bà Rịa - Vũng Tàu"),

            // === 5. TỈNH LONG AN (15 TP / Thị xã / Huyện) ===
            new(10.5360, 106.4130, "Tân An", "Long An"),
            new(10.6380, 106.4880, "Bến Lức", "Long An"),
            new(10.6050, 106.6670, "Cần Giuộc", "Long An"),
            new(10.5180, 106.6120, "Cần Đước", "Long An"),
            new(10.8820, 106.4320, "Đức Hòa", "Long An"),
            new(10.9250, 106.2840, "Đức Huệ", "Long An"),
            new(10.4520, 106.4950, "Châu Thành", "Long An"),
            new(10.5120, 106.5120, "Tân Trụ", "Long An"),
            new(10.5980, 106.3820, "Thủ Thừa", "Long An"),
            new(10.7920, 105.9320, "Kiến Tường", "Long An"),
            new(10.7420, 105.9920, "Mộc Hóa", "Long An"),
            new(10.8850, 105.6520, "Tân Hưng", "Long An"),
            new(10.8920, 105.7820, "Vĩnh Hưng", "Long An"),
            new(10.6540, 106.1820, "Thạnh Hóa", "Long An"),
            new(10.5820, 106.0120, "Tân Thạnh", "Long An"),

            // === 6. TỈNH TIỀN GIANG & BẾN TRE (10 TP / Thị xã / Huyện) ===
            new(10.3540, 106.3630, "Mỹ Tho", "Tiền Giang"),
            new(10.3620, 106.6670, "Gò Công", "Tiền Giang"),
            new(10.4120, 106.1150, "Cai Lậy", "Tiền Giang"),
            new(10.3950, 106.3050, "Châu Thành", "Tiền Giang"),
            new(10.3350, 105.9250, "Cái Bè", "Tiền Giang"),
            new(10.3520, 106.4620, "Chợ Gạo", "Tiền Giang"),
            new(10.2410, 106.3750, "Bến Tre", "Bến Tre"),
            new(10.2920, 106.3520, "Châu Thành", "Bến Tre"),
            new(10.2520, 106.1220, "Chợ Lách", "Bến Tre"),
            new(10.0420, 106.5980, "Ba Tri", "Bến Tre"),

            // === 7. CẦN THƠ & MIỀN TÂY (8 TP / Quận) ===
            new(10.0340, 105.7870, "Ninh Kiều", "Cần Thơ"),
            new(9.9980, 105.7530, "Cái Răng", "Cần Thơ"),
            new(10.0720, 105.7420, "Bình Thủy", "Cần Thơ"),
            new(10.1220, 105.6420, "Ô Môn", "Cần Thơ"),
            new(10.3830, 105.4350, "Long Xuyên", "An Giang"),
            new(10.2530, 105.9720, "Vĩnh Long", "Vĩnh Long"),
            new(10.4580, 105.6320, "Cao Lãnh", "Đồng Tháp"),
            new(10.0120, 105.0820, "Rạch Giá", "Kiên Giang"),

            // === 8. TÂY NINH, BÌNH THUẬN, TÂY NGUYÊN & DUYÊN HẢI (8 TP / Thị xã) ===
            new(11.3100, 106.0980, "Tây Ninh", "Tây Ninh"),
            new(11.0320, 106.3650, "Trảng Bàng", "Tây Ninh"),
            new(11.2650, 106.1350, "Hòa Thành", "Tây Ninh"),
            new(10.9320, 108.1020, "Phan Thiết", "Bình Thuận"),
            new(11.9404, 108.4583, "Đà Lạt", "Lâm Đồng"),
            new(11.5470, 107.8080, "Bảo Lộc", "Lâm Đồng"),
            new(12.2388, 109.1967, "Nha Trang", "Khánh Hòa"),
            new(12.6670, 108.0380, "Buôn Ma Thuột", "Đắk Lắk"),

            // === 9. MIỀN TRUNG & MIỀN BẮC (12 TP / Quận Lớn) ===
            new(16.0544, 108.2022, "Hải Châu", "Đà Nẵng"),
            new(16.0820, 108.2430, "Sơn Trà", "Đà Nẵng"),
            new(16.0650, 108.1880, "Thanh Khê", "Đà Nẵng"),
            new(21.0285, 105.8542, "Hoàn Kiếm", "Hà Nội"),
            new(21.0340, 105.8240, "Ba Đình", "Hà Nội"),
            new(21.0180, 105.8270, "Đống Đa", "Hà Nội"),
            new(21.0360, 105.7900, "Cầu Giấy", "Hà Nội"),
            new(21.0080, 105.8540, "Hai Bà Trưng", "Hà Nội"),
            new(20.9720, 105.7740, "Hà Đông", "Hà Nội"),
            new(20.8650, 106.6830, "Hồng Bàng", "Hải Phòng"),
            new(16.4637, 107.5909, "Huế", "Thừa Thiên Huế"),
            new(18.6730, 105.6810, "Vinh", "Nghệ An")
        };

        private static readonly Dictionary<string, CentroidPoint> LookupByKey = new();
        private static readonly Dictionary<string, CentroidPoint> LookupByDistrict = new();
        private static readonly Dictionary<string, CentroidPoint> LookupByProvince = new();

        static GeoHelper()
        {
            foreach (var pt in CentroidList)
            {
                string normProv = NormalizeLocation(pt.ProvinceName);
                string normDist = NormalizeLocation(pt.DistrictName);

                string fullKey = $"{normProv}:{normDist}";
                if (!LookupByKey.ContainsKey(fullKey))
                    LookupByKey[fullKey] = pt;

                if (!LookupByDistrict.ContainsKey(normDist))
                    LookupByDistrict[normDist] = pt;

                if (!LookupByProvince.ContainsKey(normProv))
                    LookupByProvince[normProv] = pt;
            }
        }

        /// <summary>
        /// Tìm kiếm tọa độ trọng tâm GIS dựa theo Tỉnh/Thành và Quận/Huyện ($O(1)$ Memory Lookup)
        /// </summary>
        public static (double Lat, double Lng)? FindCoordinates(string? district, string? province)
        {
            string normDist = NormalizeLocation(district);
            string normProv = NormalizeLocation(province);

            // Ưu tiên 1: So khớp cả Tỉnh/TP và Quận/Huyện
            if (!string.IsNullOrEmpty(normProv) && !string.IsNullOrEmpty(normDist))
            {
                string fullKey = $"{normProv}:{normDist}";
                if (LookupByKey.TryGetValue(fullKey, out var pt))
                    return (pt.Latitude, pt.Longitude);
            }

            // Ưu tiên 2: So khớp theo Quận/Huyện
            if (!string.IsNullOrEmpty(normDist))
            {
                if (LookupByDistrict.TryGetValue(normDist, out var pt))
                    return (pt.Latitude, pt.Longitude);
            }

            // Ưu tiên 3: So khớp theo Tỉnh/TP
            if (!string.IsNullOrEmpty(normProv))
            {
                if (LookupByProvince.TryGetValue(normProv, out var pt))
                    return (pt.Latitude, pt.Longitude);
            }

            return null;
        }
    }
}
