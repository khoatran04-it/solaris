using backend.Helpers;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Modules.Module08_Warehouse
{
    /// <summary>
    /// TEST SUITE: GeoHelperTests
    /// Kiểm thử chuẩn hóa địa giới hành chính và so khớp địa danh linh hoạt giữa Kho hàng và Địa chỉ giao hàng.
    /// </summary>
    public class GeoHelperTests
    {
        [Theory]
        [InlineData("Thành Phố Thủ Đức", "thu-duc")]
        [InlineData("Thủ Đức", "thu-duc")]
        [InlineData("TP. Thủ Đức", "thu-duc")]
        [InlineData("TP Thủ Đức", "thu-duc")]
        [InlineData("Quận Thủ Đức", "thu-duc")]
        [InlineData("Quận 7", "7")]
        [InlineData("Q.7", "7")]
        [InlineData("Q7", "7")]
        [InlineData("Quận 4", "4")]
        [InlineData("Quận 1", "1")]
        [InlineData("Quận 10", "10")]
        [InlineData("Huyện Bình Chánh", "binh-chanh")]
        [InlineData("Huyện Nhà Bè", "nha-be")]
        public void NormalizeLocation_ShouldStandardizeAdministrativeNames(string input, string expected)
        {
            string actual = GeoHelper.NormalizeLocation(input);
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData("Thành Phố Thủ Đức", "Thủ Đức", true)]
        [InlineData("Thành Phố Thủ Đức", "TP. Thủ Đức", true)]
        [InlineData("Thành Phố Thủ Đức", "Quận Thủ Đức", true)]
        [InlineData("Quận 7", "Quận 7", true)]
        [InlineData("Quận 4", "Quận 4", true)]
        [InlineData("TP. Hồ Chí Minh", "Hồ Chí Minh", true)]
        [InlineData("Quận 1", "Quận 10", false)] // Chống lỗi nhận nhầm Q10 là Q1
        [InlineData("Quận 1", "Quận 11", false)]
        [InlineData("Quận 1", "Quận 12", false)]
        [InlineData("Quận 7", "Quận 4", false)]
        public void IsSameLocation_ShouldMatchCorrectly(string loc1, string loc2, bool expected)
        {
            bool actual = GeoHelper.IsSameLocation(loc1, loc2);
            actual.Should().Be(expected);
        }
    }
}
