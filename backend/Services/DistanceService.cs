using backend.Services.Interfaces;

namespace backend.Services
{
    public class DistanceService : IDistanceService
    {
        private const double EarthRadiusKm = 6371.0;

        /// <summary>
        /// Thuật toán Haversine tính khoảng cách đại hình cầu giữa 2 tọa độ địa lý
        /// </summary>
        public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == 0 && lon1 == 0 || lat2 == 0 && lon2 == 0)
            {
                return double.MaxValue; // Tọa độ không hợp lệ -> Xem như ở xa vô cực
            }

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return Math.Round(EarthRadiusKm * c, 2);
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }
}
