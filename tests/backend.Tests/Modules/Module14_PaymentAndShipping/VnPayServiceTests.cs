using backend.Data;
using backend.DTOs.PaymentDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module14_PaymentAndShipping
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 14: PAYMENT & 3PL LOGISTICS
    /// 🧪 UNIT TEST: VnPayService (Cổng Thanh Toán VNPay Sandbox, Callback & IPN)
    /// ============================================================================
    /// </summary>
    public class VnPayServiceTests
    {
        private readonly IConfiguration _config;
        private const string HashSecret = "YCQMIXWYVEDZEKAJQPYIYOIXKQHVROPE";

        public VnPayServiceTests()
        {
            var myConfiguration = new Dictionary<string, string?>
            {
                { "VnPaySettings:TmnCode", "VVRIW1BA" },
                { "VnPaySettings:HashSecret", HashSecret },
                { "VnPaySettings:BaseUrl", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html" },
                { "VnPaySettings:ReturnUrl", "https://www.solaris-os.io.vn/thanh-toan/ket-qua" },
                { "VnPaySettings:Version", "2.1.0" },
                { "VnPaySettings:Command", "pay" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        #region TC01: TẠO URL THANH TOÁN VNPAY SANDBOX THÀNH CÔNG
        [Fact]
        public async Task CreatePaymentUrlAsync_WithValidOrder_ShouldGenerateCorrectUrl()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260830-001",
                TotalAmount = 250000,
                PaymentStatus = PaymentStatus.Unpaid,
                IsDeleted = false
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new VnPayService(_config, context);
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            var request = new VnPayPaymentRequestDto
            {
                OrderCode = "ORD-20260830-001",
                OrderDescription = "Thanh toan don hang ORD-20260830-001"
            };

            // Act
            var result = await service.CreatePaymentUrlAsync(request, httpContext);

            // Assert
            result.Should().NotBeNull();
            result.OrderCode.Should().Be("ORD-20260830-001");
            result.PaymentUrl.Should().StartWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?");
            result.PaymentUrl.Should().Contain("vnp_TmnCode=VVRIW1BA");
            result.PaymentUrl.Should().Contain("vnp_Amount=25000000"); // 250,000 * 100
            result.PaymentUrl.Should().Contain("vnp_SecureHash=");
        }
        #endregion

        #region TC02: TẠO URL THANH TOÁN VỚI ĐƠN HÀNG KHÔNG TỒN TẠI -> THROW EXCEPTION
        [Fact]
        public async Task CreatePaymentUrlAsync_WithNonExistentOrder_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new VnPayService(_config, context);
            var httpContext = new DefaultHttpContext();

            var request = new VnPayPaymentRequestDto
            {
                OrderCode = "ORD-NOT-FOUND",
                OrderDescription = "Thanh toan"
            };

            // Act & Assert
            var act = () => service.CreatePaymentUrlAsync(request, httpContext);
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy đơn hàng mã 'ORD-NOT-FOUND'*");
        }
        #endregion

        #region TC03: XỬ LÝ RETURN URL CALLBACK VNPAY THÀNH CÔNG (MÃ 00)
        [Fact]
        public void ProcessCallback_WithValidSignatureAndSuccessCode_ShouldReturnIsSuccessTrue()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new VnPayService(_config, context);

            var rawData = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Amount", "25000000" },
                { "vnp_BankCode", "NCB" },
                { "vnp_OrderInfo", "Thanh toan don hang" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TmnCode", "VVRIW1BA" },
                { "vnp_TransactionNo", "14567890" },
                { "vnp_TxnRef", "ORD-20260830-001" }
            };

            var signData = new StringBuilder();
            foreach (var (k, v) in rawData)
            {
                signData.Append(System.Net.WebUtility.UrlEncode(k) + "=" + System.Net.WebUtility.UrlEncode(v) + "&");
            }
            if (signData.Length > 0) signData.Remove(signData.Length - 1, 1);

            string secureHash = ComputeHmacSha512(HashSecret, signData.ToString());

            var queryDict = new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "25000000" },
                { "vnp_BankCode", "NCB" },
                { "vnp_OrderInfo", "Thanh toan don hang" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TmnCode", "VVRIW1BA" },
                { "vnp_TransactionNo", "14567890" },
                { "vnp_TxnRef", "ORD-20260830-001" },
                { "vnp_SecureHash", secureHash }
            };

            var query = new QueryCollection(queryDict);

            // Act
            var result = service.ProcessCallback(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.OrderCode.Should().Be("ORD-20260830-001");
            result.Amount.Should().Be(250000);
            result.TransactionNo.Should().Be("14567890");
            result.BankCode.Should().Be("NCB");
        }
        #endregion

        #region TC04: XỬ LÝ RETURN URL CALLBACK VỚI CHỮ KÝ SAI -> IS SUCCESS = FALSE
        [Fact]
        public void ProcessCallback_WithInvalidSignature_ShouldReturnIsSuccessFalse()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new VnPayService(_config, context);

            var queryDict = new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "25000000" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TxnRef", "ORD-20260830-001" },
                { "vnp_SecureHash", "INVALID_HASH_VALUE_HERE" }
            };

            var query = new QueryCollection(queryDict);

            // Act
            var result = service.ProcessCallback(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }
        #endregion

        #region TC05: XỬ LÝ IPN WEBHOOK HỢP LỆ -> CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG SANG PAID
        [Fact]
        public async Task ProcessIpnAsync_WithValidSignatureAndMatchingAmount_ShouldUpdateOrderToPaid()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260830-001",
                TotalAmount = 300000,
                PaymentStatus = PaymentStatus.Unpaid,
                IsDeleted = false
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new VnPayService(_config, context);

            var rawData = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Amount", "30000000" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TmnCode", "VVRIW1BA" },
                { "vnp_TransactionNo", "99887766" },
                { "vnp_TxnRef", "ORD-20260830-001" }
            };

            var signData = new StringBuilder();
            foreach (var (k, v) in rawData)
            {
                signData.Append(System.Net.WebUtility.UrlEncode(k) + "=" + System.Net.WebUtility.UrlEncode(v) + "&");
            }
            if (signData.Length > 0) signData.Remove(signData.Length - 1, 1);

            string secureHash = ComputeHmacSha512(HashSecret, signData.ToString());

            var queryDict = new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "30000000" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TmnCode", "VVRIW1BA" },
                { "vnp_TransactionNo", "99887766" },
                { "vnp_TxnRef", "ORD-20260830-001" },
                { "vnp_SecureHash", secureHash }
            };

            var query = new QueryCollection(queryDict);

            // Act
            var ipnResponse = await service.ProcessIpnAsync(query);

            // Assert
            ipnResponse.RspCode.Should().Be("00");
            ipnResponse.Message.Should().Be("Confirm Success");

            var updatedOrder = await context.Orders.FindAsync(1);
            updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Paid);
            updatedOrder.PaymentMethod.Should().Be(PaymentMethod.EWallet);
            updatedOrder.PaymentTransactionNo.Should().Be("99887766");
        }
        #endregion

        #region TC06: XỬ LÝ IPN VỚI SỐ TIỀN SAI LỆCH -> TRẢ VỀ MÃ LỖI 04 (INVALID AMOUNT)
        [Fact]
        public async Task ProcessIpnAsync_WithMismatchedAmount_ShouldReturnRspCode04()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260830-001",
                TotalAmount = 500000, // Đơn hàng 500k nhưng IPN báo 100k
                PaymentStatus = PaymentStatus.Unpaid,
                IsDeleted = false
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new VnPayService(_config, context);

            var rawData = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Amount", "10000000" }, // 100,000 * 100
                { "vnp_ResponseCode", "00" },
                { "vnp_TxnRef", "ORD-20260830-001" }
            };

            var signData = new StringBuilder();
            foreach (var (k, v) in rawData)
            {
                signData.Append(System.Net.WebUtility.UrlEncode(k) + "=" + System.Net.WebUtility.UrlEncode(v) + "&");
            }
            if (signData.Length > 0) signData.Remove(signData.Length - 1, 1);

            string secureHash = ComputeHmacSha512(HashSecret, signData.ToString());

            var queryDict = new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "10000000" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TxnRef", "ORD-20260830-001" },
                { "vnp_SecureHash", secureHash }
            };

            var query = new QueryCollection(queryDict);

            // Act
            var ipnResponse = await service.ProcessIpnAsync(query);

            // Assert
            ipnResponse.RspCode.Should().Be("04");
            ipnResponse.Message.Should().Be("Invalid amount");
        }
        #endregion

        #region Helper
        private static string ComputeHmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (byte b in hashValue)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }
        #endregion
    }
}
