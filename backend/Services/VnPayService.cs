using backend.Data;
using backend.DTOs.PaymentDTOs;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly SolarisDbContext _context;

        public VnPayService(IConfiguration config, SolarisDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<VnPayPaymentResponseDto> CreatePaymentUrlAsync(VnPayPaymentRequestDto request, HttpContext httpContext)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == request.OrderCode && !o.IsDeleted);

            if (order == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng mã '{request.OrderCode}'.");
            }

            var vnPaySection = _config.GetSection("VnPaySettings");
            string tmnCode = vnPaySection["TmnCode"] ?? "VVRIW1BA";
            string hashSecret = vnPaySection["HashSecret"] ?? "YCQMIXWYVEDZEKAJQPYIYOIXKQHVROPE";
            string baseUrl = vnPaySection["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string returnUrl = vnPaySection["ReturnUrl"] ?? "https://www.solaris-os.io.vn/thanh-toan/ket-qua";
            string version = vnPaySection["Version"] ?? "2.1.0";
            string command = vnPaySection["Command"] ?? "pay";

            var payLib = new VnPayLibrary();
            payLib.AddRequestData("vnp_Version", version);
            payLib.AddRequestData("vnp_Command", command);
            payLib.AddRequestData("vnp_TmnCode", tmnCode);
            // Amount in VND * 100 as required by VNPay
            long amountInCents = (long)(order.TotalAmount * 100);
            payLib.AddRequestData("vnp_Amount", amountInCents.ToString());
            payLib.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"));
            payLib.AddRequestData("vnp_CurrCode", "VND");
            payLib.AddRequestData("vnp_IpAddr", GetIpAddress(httpContext));
            payLib.AddRequestData("vnp_Locale", "vn");
            payLib.AddRequestData("vnp_OrderInfo", string.IsNullOrWhiteSpace(request.OrderDescription)
                ? $"Thanh toan don hang {order.OrderCode}"
                : request.OrderDescription);
            payLib.AddRequestData("vnp_OrderType", "other");
            payLib.AddRequestData("vnp_ReturnUrl", returnUrl);
            payLib.AddRequestData("vnp_TxnRef", order.OrderCode);

            if (!string.IsNullOrEmpty(request.BankCode))
            {
                payLib.AddRequestData("vnp_BankCode", request.BankCode);
            }

            string paymentUrl = payLib.CreateRequestUrl(baseUrl, hashSecret);

            return new VnPayPaymentResponseDto
            {
                PaymentUrl = paymentUrl,
                OrderCode = order.OrderCode
            };
        }

        public VnPayCallbackResultDto ProcessCallback(IQueryCollection query)
        {
            var vnPaySection = _config.GetSection("VnPaySettings");
            string hashSecret = vnPaySection["HashSecret"] ?? "YCQMIXWYVEDZEKAJQPYIYOIXKQHVROPE";

            var payLib = new VnPayLibrary();
            foreach (var (key, value) in query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    payLib.AddResponseData(key, value.ToString());
                }
            }

            string secureHash = query["vnp_SecureHash"].ToString();
            bool isValidSignature = payLib.ValidateSignature(secureHash, hashSecret);

            string orderCode = payLib.GetResponseData("vnp_TxnRef");
            string responseCode = payLib.GetResponseData("vnp_ResponseCode");
            string transactionNo = payLib.GetResponseData("vnp_TransactionNo");
            string bankCode = payLib.GetResponseData("vnp_BankCode");
            string orderInfo = payLib.GetResponseData("vnp_OrderInfo");
            long vnpAmount = 0;
            long.TryParse(payLib.GetResponseData("vnp_Amount"), out vnpAmount);
            decimal amount = vnpAmount / 100m;

            bool isSuccess = isValidSignature && responseCode == "00";

            return new VnPayCallbackResultDto
            {
                IsSuccess = isSuccess,
                OrderCode = orderCode,
                TransactionNo = transactionNo,
                ResponseCode = responseCode,
                BankCode = bankCode,
                Amount = amount,
                OrderInfo = orderInfo,
                Message = isSuccess ? "Giao dịch thanh toán VNPay thành công." : "Giao dịch không thành công hoặc bị hủy."
            };
        }

        public async Task<VnPayIpnResponseDto> ProcessIpnAsync(IQueryCollection query)
        {
            var vnPaySection = _config.GetSection("VnPaySettings");
            string hashSecret = vnPaySection["HashSecret"] ?? "YCQMIXWYVEDZEKAJQPYIYOIXKQHVROPE";

            var payLib = new VnPayLibrary();
            foreach (var (key, value) in query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    payLib.AddResponseData(key, value.ToString());
                }
            }

            string secureHash = query["vnp_SecureHash"].ToString();
            bool isValidSignature = payLib.ValidateSignature(secureHash, hashSecret);

            if (!isValidSignature)
            {
                return new VnPayIpnResponseDto { RspCode = "97", Message = "Invalid signature" };
            }

            string orderCode = payLib.GetResponseData("vnp_TxnRef");
            string responseCode = payLib.GetResponseData("vnp_ResponseCode");
            string transactionNo = payLib.GetResponseData("vnp_TransactionNo");
            long vnpAmount = 0;
            long.TryParse(payLib.GetResponseData("vnp_Amount"), out vnpAmount);
            decimal amount = vnpAmount / 100m;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && !o.IsDeleted);

            if (order == null)
            {
                return new VnPayIpnResponseDto { RspCode = "01", Message = "Order not found" };
            }

            if (order.TotalAmount != amount)
            {
                return new VnPayIpnResponseDto { RspCode = "04", Message = "Invalid amount" };
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return new VnPayIpnResponseDto { RspCode = "02", Message = "Order already confirmed" };
            }

            if (responseCode == "00")
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaymentMethod = PaymentMethod.EWallet;
                order.PaymentTransactionNo = transactionNo;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return new VnPayIpnResponseDto { RspCode = "00", Message = "Confirm Success" };
            }
        }

        private static string GetIpAddress(HttpContext context)
        {
            string ipAddress = string.Empty;
            try
            {
                ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? string.Empty;
                if (string.IsNullOrEmpty(ipAddress) || ipAddress.ToLower() == "unknown")
                {
                    ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }
    }

    /// <summary>
    /// Helper xử lý mã hóa HMAC-SHA512 và sắp xếp tham số VNPay v2.1.0
    /// </summary>
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
        {
            var data = new StringBuilder();
            foreach (var (key, value) in _requestData)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
                }
            }

            string queryString = data.ToString();
            baseUrl += "?" + queryString;
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            string vnpSecureHash = HmacSha512(vnpHashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = HmacSha512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }
            foreach (var (key, value) in _responseData)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
                }
            }
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }
            return data.ToString();
        }

        private static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (byte theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return CompareInfo.GetCompareInfo("en-US").Compare(x, y, CompareOptions.Ordinal);
        }
    }
}
