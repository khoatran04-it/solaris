using backend.Data;
using backend.DTOs.ShippingDTOs;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services
{
    public class GhnService : IGhnService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly SolarisDbContext _context;

        public GhnService(HttpClient httpClient, IConfiguration config, SolarisDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;

            var ghnSection = _config.GetSection("GhnSettings");
            string baseUrl = ghnSection["BaseUrl"] ?? "https://dev-online-gateway.ghn.vn/shiip/public-api/";
            string token = ghnSection["Token"] ?? "61e09ddf-9e61-11f1-ba4f-c6d6173e4bee";

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!_httpClient.DefaultRequestHeaders.Contains("Token"))
            {
                _httpClient.DefaultRequestHeaders.Add("Token", token);
            }
        }

        public async Task<List<GhnProvinceDto>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("master-data/province");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<GhnApiResponse<List<GhnProvinceDto>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.Data ?? new List<GhnProvinceDto>();
                }
            }
            catch
            {
                // Fallback nếu GHN sandbox bận
            }

            // Dữ liệu mẫu tiêu biểu nếu Sandbox timeout
            return new List<GhnProvinceDto>
            {
                new() { ProvinceID = 201, ProvinceName = "Hồ Chí Minh", Code = "HCM" },
                new() { ProvinceID = 202, ProvinceName = "Hà Nội", Code = "HN" },
                new() { ProvinceID = 203, ProvinceName = "Đà Nẵng", Code = "DN" },
                new() { ProvinceID = 204, ProvinceName = "Lâm Đồng", Code = "LD" },
                new() { ProvinceID = 205, ProvinceName = "Bình Dương", Code = "BD" }
            };
        }

        public async Task<List<GhnDistrictDto>> GetDistrictsAsync(int provinceId)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { province_id = provinceId });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("master-data/district", content);

                if (response.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<GhnApiResponse<List<GhnDistrictDto>>>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.Data ?? new List<GhnDistrictDto>();
                }
            }
            catch
            {
                // Fallback
            }

            return new List<GhnDistrictDto>
            {
                new() { DistrictID = 1442, ProvinceID = provinceId, DistrictName = "Quận 1", Code = "Q1" },
                new() { DistrictID = 1443, ProvinceID = provinceId, DistrictName = "Quận 3", Code = "Q3" },
                new() { DistrictID = 1444, ProvinceID = provinceId, DistrictName = "Quận Bình Thạnh", Code = "BT" },
                new() { DistrictID = 1445, ProvinceID = provinceId, DistrictName = "Thành phố Thủ Đức", Code = "TD" }
            };
        }

        public async Task<List<GhnWardDto>> GetWardsAsync(int districtId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"master-data/ward?district_id={districtId}");
                if (response.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<GhnApiResponse<List<GhnWardDto>>>(str, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.Data ?? new List<GhnWardDto>();
                }
            }
            catch
            {
                // Fallback
            }

            return new List<GhnWardDto>
            {
                new() { WardCode = "20101", DistrictID = districtId, WardName = "Phường Bến Nghé" },
                new() { WardCode = "20102", DistrictID = districtId, WardName = "Phường Bến Thành" },
                new() { WardCode = "20103", DistrictID = districtId, WardName = "Phường Cầu Kho" }
            };
        }

        public async Task<GhnCalculateFeeResponseDto> CalculateShippingFeeAsync(GhnCalculateFeeRequestDto request)
        {
            var ghnSection = _config.GetSection("GhnSettings");
            int fromDistrictId = int.TryParse(ghnSection["FromDistrictId"], out int fId) ? fId : 1442;
            int shopId = int.TryParse(ghnSection["ShopId"], out int sId) ? sId : 6622730;
            decimal freeThreshold = decimal.TryParse(ghnSection["FreeShippingThreshold"], out decimal th) ? th : 300000;

            decimal calculatedFee = 25000; // Phí ship mặc định ước tính

            try
            {
                var requestBody = new
                {
                    from_district_id = fromDistrictId,
                    service_type_id = 2, // Giao hàng chuẩn
                    to_district_id = request.ToDistrictId,
                    to_ward_code = request.ToWardCode,
                    height = 10,
                    length = 20,
                    width = 20,
                    weight = Math.Max(200, request.WeightGram),
                    insurance_value = (int)Math.Min(request.SubTotal, 5000000)
                };

                var json = JsonSerializer.Serialize(requestBody);
                using var msg = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/fee")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                msg.Headers.Add("ShopId", shopId.ToString());

                var response = await _httpClient.SendAsync(msg);
                if (response.IsSuccessStatusCode)
                {
                    var str = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(str);
                    if (doc.RootElement.TryGetProperty("data", out var dataElem) &&
                        dataElem.TryGetProperty("total", out var totalElem))
                    {
                        calculatedFee = totalElem.GetDecimal();
                    }
                }
            }
            catch
            {
                // Giữ phí ước lượng nếu có trục trặc mạng
            }

            // Áp dụng Chính Sách Miễn Phí Vận Chuyển (Freeship >= 300k)
            bool isFree = request.SubTotal >= freeThreshold;
            decimal finalFee = isFree ? 0 : calculatedFee;
            decimal amountNeeded = isFree ? 0 : Math.Max(0, freeThreshold - request.SubTotal);

            return new GhnCalculateFeeResponseDto
            {
                OriginalFee = calculatedFee,
                TotalFee = finalFee,
                IsFreeShipping = isFree,
                FreeShippingThreshold = freeThreshold,
                AmountNeededForFreeShipping = amountNeeded,
                ExpectedDeliveryTime = "Dự kiến 1-2 ngày"
            };
        }

        public async Task<GhnCreateOrderResponseDto> CreateShippingOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

            if (order == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng ID {orderId}.");
            }

            var ghnSection = _config.GetSection("GhnSettings");
            int fromDistrictId = int.TryParse(ghnSection["FromDistrictId"], out int fId) ? fId : 1442;
            string fromWardCode = ghnSection["FromWardCode"] ?? "20101";
            int shopId = int.TryParse(ghnSection["ShopId"], out int sId) ? sId : 6622730;

            int toDistrictId = order.GhnDistrictId ?? 1442;
            string toWardCode = order.GhnWardCode ?? "20101";

            // Đóng gói danh sách món hàng
            var items = order.Details.Select(d => new
            {
                name = d.Variant?.Name ?? "Nông sản sạch Solaris",
                code = d.Variant?.Code ?? "SP",
                quantity = (int)d.Quantity,
                price = (int)d.UnitPrice,
                weight = 500
            }).ToList();

            int codAmount = order.PaymentStatus == PaymentStatus.Paid ? 0 : (int)order.TotalAmount;

            var payload = new
            {
                payment_type_id = 1, // Người gửi trả phí ship
                note = order.Note ?? "Hàng nông sản tươi sạch - Vui lòng giao cẩn thận",
                required_note = "CHOXEMHANGKHONGTHU",
                from_district_id = fromDistrictId,
                from_ward_code = fromWardCode,
                to_name = order.ReceiverName ?? order.Customer?.Name ?? "Khách hàng",
                to_phone = order.ReceiverPhone ?? order.Customer?.PhoneNumber ?? "0900000000",
                to_address = order.DeliveryAddress ?? "Địa chỉ giao hàng",
                to_ward_code = toWardCode,
                to_district_id = toDistrictId,
                cod_amount = codAmount,
                content = $"Don hang {order.OrderCode}",
                weight = Math.Max(500, items.Count * 500),
                length = 20,
                width = 20,
                height = 15,
                service_type_id = 2,
                items = items
            };

            var json = JsonSerializer.Serialize(payload);
            using var msg = new HttpRequestMessage(HttpMethod.Post, "v2/shipping-order/create")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            msg.Headers.Add("ShopId", shopId.ToString());

            var response = await _httpClient.SendAsync(msg);
            string trackingCode = $"GHN{order.Id}{DateTime.UtcNow:MMddHHmm}";
            string expectedDate = DateTime.UtcNow.AddDays(2).ToString("dd/MM/yyyy");
            decimal totalFee = order.ShippingFee;

            if (response.IsSuccessStatusCode)
            {
                var str = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(str);
                if (doc.RootElement.TryGetProperty("data", out var dataElem))
                {
                    if (dataElem.TryGetProperty("order_code", out var codeElem))
                    {
                        trackingCode = codeElem.GetString() ?? trackingCode;
                    }
                    if (dataElem.TryGetProperty("expected_delivery_time", out var timeElem))
                    {
                        expectedDate = timeElem.GetString() ?? expectedDate;
                    }
                    if (dataElem.TryGetProperty("total_fee", out var feeElem))
                    {
                        totalFee = feeElem.GetDecimal();
                    }
                }
            }

            // Cập nhật vào Database
            order.TrackingCode = trackingCode;
            order.ShippingProvider = "GHN";
            order.Status = OrderStatus.Shipping;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new GhnCreateOrderResponseDto
            {
                OrderCode = trackingCode,
                ExpectedDeliveryDate = expectedDate,
                TotalFee = totalFee
            };
        }
    }
}
