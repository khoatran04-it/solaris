using backend.Data;
using backend.DTOs.ShippingDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module14_PaymentAndShipping
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 14: PAYMENT & 3PL LOGISTICS
    /// 🧪 UNIT TEST: GhnService (Giao Hàng Nhanh Express, Master Data & Freeship 300k)
    /// ============================================================================
    /// </summary>
    public class GhnServiceTests
    {
        private readonly IConfiguration _config;

        public GhnServiceTests()
        {
            var myConfiguration = new Dictionary<string, string?>
            {
                { "GhnSettings:ShopId", "6622730" },
                { "GhnSettings:Token", "test-ghn-token" },
                { "GhnSettings:BaseUrl", "https://dev-online-gateway.ghn.vn/shiip/public-api/" },
                { "GhnSettings:FromDistrictId", "1442" },
                { "GhnSettings:FromWardCode", "20101" },
                { "GhnSettings:FreeShippingThreshold", "300000" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        #region TC01: LẤY DANH SÁCH TỈNH THÀNH (PROVINCES) VỚI HTTP CLIENT
        [Fact]
        public async Task GetProvincesAsync_ShouldReturnProvincesList()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mockResponseData = new GhnApiResponse<List<GhnProvinceDto>>
            {
                Code = 200,
                Message = "Success",
                Data = new List<GhnProvinceDto>
                {
                    new() { ProvinceID = 201, ProvinceName = "Hồ Chí Minh", Code = "HCM" },
                    new() { ProvinceID = 202, ProvinceName = "Hà Nội", Code = "HN" }
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseData);
            var service = new GhnService(httpClient, _config, context);

            // Act
            var result = await service.GetProvincesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].ProvinceName.Should().Be("Hồ Chí Minh");
        }
        #endregion

        #region TC02: LẤY DANH SÁCH QUẬN HUYỆN THEO TỈNH THÀNH
        [Fact]
        public async Task GetDistrictsAsync_ShouldReturnDistrictsForProvince()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mockResponseData = new GhnApiResponse<List<GhnDistrictDto>>
            {
                Code = 200,
                Message = "Success",
                Data = new List<GhnDistrictDto>
                {
                    new() { DistrictID = 1442, ProvinceID = 201, DistrictName = "Quận 1", Code = "Q1" },
                    new() { DistrictID = 1443, ProvinceID = 201, DistrictName = "Quận 3", Code = "Q3" }
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseData);
            var service = new GhnService(httpClient, _config, context);

            // Act
            var result = await service.GetDistrictsAsync(201);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].DistrictName.Should().Be("Quận 1");
        }
        #endregion

        #region TC03: LẤY DANH SÁCH PHƯỜNG XÃ THEO QUẬN HUYỆN
        [Fact]
        public async Task GetWardsAsync_ShouldReturnWardsForDistrict()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mockResponseData = new GhnApiResponse<List<GhnWardDto>>
            {
                Code = 200,
                Message = "Success",
                Data = new List<GhnWardDto>
                {
                    new() { WardCode = "20101", DistrictID = 1442, WardName = "Phường Bến Nghé" },
                    new() { WardCode = "20102", DistrictID = 1442, WardName = "Phường Bến Thành" }
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseData);
            var service = new GhnService(httpClient, _config, context);

            // Act
            var result = await service.GetWardsAsync(1442);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].WardName.Should().Be("Phường Bến Nghé");
        }
        #endregion

        #region TC04: TÍNH PHÍ SHIP VỚI ĐƠN HÀNG DƯỚI 300K -> TÍNH CƯỚC CHUẨN & BÁO SỐ TIỀN CẦN MUA THÊM
        [Fact]
        public async Task CalculateShippingFeeAsync_UnderThreshold_ShouldChargeFeeAndCalculateAmountNeeded()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mockResponseJson = new
            {
                code = 200,
                message = "Success",
                data = new
                {
                    total = 28000,
                    service_fee = 28000
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseJson);
            var service = new GhnService(httpClient, _config, context);

            var request = new GhnCalculateFeeRequestDto
            {
                ToDistrictId = 1442,
                ToWardCode = "20101",
                SubTotal = 200000, // 200k < 300k
                WeightGram = 1000
            };

            // Act
            var result = await service.CalculateShippingFeeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsFreeShipping.Should().BeFalse();
            result.OriginalFee.Should().Be(28000);
            result.TotalFee.Should().Be(28000);
            result.FreeShippingThreshold.Should().Be(300000);
            result.AmountNeededForFreeShipping.Should().Be(100000); // 300k - 200k = 100k
        }
        #endregion

        #region TC05: TÍNH PHÍ SHIP VỚI ĐƠN HÀNG ĐẠT 300K -> ÁP DỤNG FREESHIP 100% (TOTAL FEE = 0)
        [Fact]
        public async Task CalculateShippingFeeAsync_OverThreshold_ShouldApply100PercentFreeShipping()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mockResponseJson = new
            {
                code = 200,
                message = "Success",
                data = new
                {
                    total = 35000,
                    service_fee = 35000
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseJson);
            var service = new GhnService(httpClient, _config, context);

            var request = new GhnCalculateFeeRequestDto
            {
                ToDistrictId = 1442,
                ToWardCode = "20101",
                SubTotal = 350000, // 350k >= 300k
                WeightGram = 1500
            };

            // Act
            var result = await service.CalculateShippingFeeAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsFreeShipping.Should().BeTrue();
            result.OriginalFee.Should().Be(35000);
            result.TotalFee.Should().Be(0); // 100% Miễn phí
            result.AmountNeededForFreeShipping.Should().Be(0);
        }
        #endregion

        #region TC06: TẠO ĐƠN VẬN CHUYỂN GHN -> NHẬN TRACKING CODE VÀ CẬP NHẬT TRẠNG THÁI SHIPPING
        [Fact]
        public async Task CreateShippingOrderAsync_WithValidOrder_ShouldReturnTrackingCodeAndUpdateOrder()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var customer = new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Trần Văn B",
                PhoneNumber = "0912345678",
                IsActive = true
            };
            var variant = new ProductVariant
            {
                Id = 10,
                Code = "SKU-BO-034",
                Name = "Bơ 034 Đắk Lắk",
                IsActive = true
            };
            context.Customers.Add(customer);
            context.ProductVariants.Add(variant);

            var order = new Order
            {
                Id = 100,
                OrderCode = "ORD-20260830-100",
                CustomerId = 1,
                ReceiverName = "Trần Văn B",
                ReceiverPhone = "0912345678",
                DeliveryAddress = "456 Nguyễn Huệ, Quận 1, TP.HCM",
                GhnDistrictId = 1442,
                GhnWardCode = "20101",
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 450000,
                ShippingFee = 0,
                IsDeleted = false,
                Details = new List<OrderDetail>
                {
                    new()
                    {
                        VariantId = 10,
                        Quantity = 3,
                        UnitPrice = 150000,
                        TotalPrice = 450000
                    }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var mockResponseJson = new
            {
                code = 200,
                message = "Success",
                data = new
                {
                    order_code = "GHN-EXPRESS-100830-999",
                    expected_delivery_time = "2026-09-02",
                    total_fee = 25000
                }
            };

            var httpClient = CreateMockHttpClient(mockResponseJson);
            var service = new GhnService(httpClient, _config, context);

            // Act
            var result = await service.CreateShippingOrderAsync(100);

            // Assert
            result.Should().NotBeNull();
            result.OrderCode.Should().Be("GHN-EXPRESS-100830-999");
            result.ExpectedDeliveryDate.Should().Be("2026-09-02");

            var updatedOrder = await context.Orders.FindAsync(100);
            updatedOrder!.TrackingCode.Should().Be("GHN-EXPRESS-100830-999");
            updatedOrder.ShippingProvider.Should().Be("GHN");
            updatedOrder.Status.Should().Be(OrderStatus.Shipping);
        }
        #endregion

        #region Helper Mock HttpClient
        private static HttpClient CreateMockHttpClient(object responseData)
        {
            var json = JsonSerializer.Serialize(responseData);
            var handler = new MockHttpMessageHandler(json);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://dev-online-gateway.ghn.vn/shiip/public-api/")
            };
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseJson;

            public MockHttpMessageHandler(string responseJson)
            {
                _responseJson = responseJson;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
        #endregion
    }
}
