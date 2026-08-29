using backend.Services;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Modules.Module08_Warehouse
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 08: GEOSPATIAL & DISTANCE CALCULATION SERVICE
    /// 🧪 TEST SUITE: DistanceServiceTests
    /// ============================================================================
    /// Kiểm thử thuật toán Haversine tính toán khoảng cách đại hình cầu giữa 2 tọa độ GPS (Kho Hàng <-> Khách Hàng).
    /// </summary>
    public class DistanceServiceTests
    {
        private readonly DistanceService _distanceService;

        public DistanceServiceTests()
        {
            _distanceService = new DistanceService();
        }

        [Fact]
        public void CalculateDistanceKm_ShouldReturnAccurateDistance_BetweenTwoCoordinates()
        {
            // Arrange: Tọa độ Hồ Hoàn Kiếm (Hà Nội) -> Tọa độ Cầu Rồng (Đà Nẵng) ~625 - 630 km
            double lat1 = 21.0285;
            double lon1 = 105.8542;

            double lat2 = 16.0611;
            double lon2 = 108.2272;

            // Act
            double distanceKm = _distanceService.CalculateDistanceKm(lat1, lon1, lat2, lon2);

            // Assert: Khoảng cách thực tế ~606.32 km
            distanceKm.Should().BeInRange(600.0, 615.0);
        }

        [Fact]
        public void CalculateDistanceKm_ShouldReturnZero_WhenCoordinatesAreIdentical()
        {
            // Arrange
            double lat = 10.7769;
            double lon = 106.7009;

            // Act
            double distanceKm = _distanceService.CalculateDistanceKm(lat, lon, lat, lon);

            // Assert
            distanceKm.Should().Be(0);
        }

        [Fact]
        public void CalculateDistanceKm_ShouldReturnMaxValue_WhenCoordinatesAreInvalidZero()
        {
            // Act 1: Điểm 1 là (0,0)
            double res1 = _distanceService.CalculateDistanceKm(0, 0, 10.5, 106.5);

            // Act 2: Điểm 2 là (0,0)
            double res2 = _distanceService.CalculateDistanceKm(21.0, 105.8, 0, 0);

            // Assert
            res1.Should().Be(double.MaxValue);
            res2.Should().Be(double.MaxValue);
        }
    }
}
