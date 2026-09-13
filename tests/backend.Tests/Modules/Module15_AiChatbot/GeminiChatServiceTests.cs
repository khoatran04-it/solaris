using AutoMapper;
using backend.Data;
using backend.DTOs.AiDTOs;
using backend.DTOs.PaymentDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Services.Interfaces;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module15_AiChatbot
{
    /// <summary>
    /// ============================================================================
    /// MODULE 15: AI CHATBOT & CONVERSATIONAL COMMERCE
    /// UNIT TEST: GeminiChatService (Quản lý Phiên, LLM Agentic Workflow & Chốt Đơn Chat)
    /// ============================================================================
    /// </summary>
    public class GeminiChatServiceTests
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly IVnPayService _mockVnPayService;
        private readonly IHttpContextAccessor _mockHttpContextAccessor;

        public GeminiChatServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();

            var myConfiguration = new Dictionary<string, string?>
            {
                { "GeminiSettings:ApiKey", "TEST_API_KEY_123" },
                { "GeminiSettings:Model", "gemini-3.5-flash-lite" },
                { "GeminiSettings:BaseUrl", "https://generativelanguage.googleapis.com/v1beta/models/" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            _mockVnPayService = Substitute.For<IVnPayService>();
            _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();

            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            _mockHttpContextAccessor.HttpContext.Returns(httpContext);
        }

        private static HttpClient CreateMockHttpClient(string replyText = "Dạ Solaris AI xin chào bạn! Nông sản sạch Đà Lạt luôn sẵn sàng phục vụ.")
        {
            var geminiResponseObj = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new { text = replyText }
                            }
                        }
                    }
                }
            };

            var responseJson = JsonSerializer.Serialize(geminiResponseObj);
            return new HttpClient(new FakeHttpMessageHandler(responseJson));
        }

        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseContent;
            private readonly HttpStatusCode _statusCode;

            public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                _responseContent = responseContent;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        #region TC01: LẤY DANH SÁCH PHIÊN CHAT CỦA KHÁCH HÀNG ĐÃ ĐĂNG NHẬP
        [Fact]
        public async Task GetCustomerSessionsAsync_WithCustomerId_ShouldReturnOrderedSessions()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session1 = new ChatSession
            {
                Id = 1,
                CustomerId = 10,
                SessionToken = "token-1",
                Title = "Tư vấn bơ 034",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
                IsActive = true,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Id = 1, Content = "Bơ có ngon không?", Role = "user", CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
                    new ChatMessage { Id = 2, Content = "Dạ bơ sáp 034 dẻo béo ạ!", Role = "model", CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
                }
            };
            var session2 = new ChatSession
            {
                Id = 2,
                CustomerId = 10,
                SessionToken = "token-2",
                Title = "Tư vấn sầu riêng",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-1),
                IsActive = true
            };
            var otherSession = new ChatSession
            {
                Id = 3,
                CustomerId = 99,
                SessionToken = "token-other",
                Title = "Phiên của người khác",
                IsActive = true
            };

            context.ChatSessions.AddRange(session1, session2, otherSession);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act
            var result = await service.GetCustomerSessionsAsync(10, null);

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(2); // UpdatedAt mới hơn nên nổi lên đầu
            result[1].Id.Should().Be(1);
            result[1].TotalMessages.Should().Be(2);
            result[1].LastMessage.Should().Be("Dạ bơ sáp 034 dẻo béo ạ!");
        }
        #endregion

        #region TC02: LẤY DANH SÁCH PHIÊN CHAT CỦA KHÁCH VÃNG LAI QUA SESSION TOKEN
        [Fact]
        public async Task GetCustomerSessionsAsync_WithGuestSessionToken_ShouldReturnMatchingSessions()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session = new ChatSession
            {
                Id = 1,
                CustomerId = null,
                SessionToken = "guest_token_abc123",
                Title = "Khách vãng lai hỏi dưa lưới",
                IsActive = true
            };
            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act
            var result = await service.GetCustomerSessionsAsync(null, "guest_token_abc123");

            // Assert
            result.Should().HaveCount(1);
            result[0].SessionToken.Should().Be("guest_token_abc123");
            result[0].Title.Should().Be("Khách vãng lai hỏi dưa lưới");
        }
        #endregion

        #region TC03: LẤY LỊCH SỬ TIN NHẮN TRONG PHIÊN VỚI QUYỀN TRUY CẬP HỢP LỆ
        [Fact]
        public async Task GetSessionMessagesAsync_WithValidOwnership_ShouldReturnMessagesInAscendingOrder()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session = new ChatSession
            {
                Id = 1,
                CustomerId = 5,
                SessionToken = "tok-5",
                IsActive = true,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Id = 10, Content = "Xin chào", Role = "user", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                    new ChatMessage { Id = 11, Content = "Dạ chào bạn!", Role = "model", CreatedAt = DateTime.UtcNow.AddMinutes(-4) }
                }
            };
            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act
            var result = await service.GetSessionMessagesAsync(1, 5, "tok-5");

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(10);
            result[0].Role.Should().Be("user");
            result[1].Id.Should().Be(11);
            result[1].Role.Should().Be("model");
        }
        #endregion

        #region TC04: LẤY LỊCH SỬ TIN NHẮN KHI KHÔNG CÓ QUYỀN SỞ HỮU -> TRẢ VỀ RỖNG
        [Fact]
        public async Task GetSessionMessagesAsync_WithInvalidOwnership_ShouldReturnEmptyList()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session = new ChatSession
            {
                Id = 1,
                CustomerId = 99,
                SessionToken = "tok-secret",
                IsActive = true
            };
            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act: Khách hàng id = 1 cố tình xem trộm phiên của khách hàng id = 99
            var result = await service.GetSessionMessagesAsync(1, 1, "tok-stranger");

            // Assert
            result.Should().BeEmpty();
        }
        #endregion

        #region TC05: TẠO PHIÊN HỘI THOẠI MỚI THÀNH CÔNG
        [Fact]
        public async Task CreateSessionAsync_WithValidParams_ShouldPersistAndReturnDto()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act
            var sessionDto = await service.CreateSessionAsync(15, "my_custom_token", "Hỏi đáp xuất xứ VietGAP");

            // Assert
            sessionDto.Should().NotBeNull();
            sessionDto.Id.Should().BeGreaterThan(0);
            sessionDto.SessionToken.Should().Be("my_custom_token");
            sessionDto.Title.Should().Be("Hỏi đáp xuất xứ VietGAP");

            var inDb = await context.ChatSessions.FindAsync(sessionDto.Id);
            inDb.Should().NotBeNull();
            inDb!.CustomerId.Should().Be(15);
            inDb.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC06: XÓA MỀM PHIÊN HỘI THOẠI
        [Fact]
        public async Task DeleteSessionAsync_WithValidOwner_ShouldSetIsActiveFalse()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session = new ChatSession
            {
                Id = 1,
                CustomerId = 7,
                SessionToken = "token-7",
                IsActive = true
            };
            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act
            var success = await service.DeleteSessionAsync(1, 7, "token-7");

            // Assert
            success.Should().BeTrue();
            var inDb = await context.ChatSessions.FindAsync(1);
            inDb!.IsActive.Should().BeFalse();
        }
        #endregion

        #region TC07: GỬI TIN NHẮN TÌM KIẾM SẢN PHẨM -> TRẢ VỀ PRODUCT_CARDS PAYLOAD
        [Fact]
        public async Task SendMessageAsync_WithProductSearchKeyword_ShouldReturnProductCardsPayload()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product
            {
                Id = 1,
                Name = "Bơ Sáp 034 Đặc Sản Lâm Đồng",
                Code = "PRD-BO034",
                Slug = "bo-sap-034-lam-dong",
                ImagePath = "/images/bo034.jpg",
                BaseUoMId = 1,
                BaseUoM = uom,
                IsActive = true,
                IsDeleted = false
            };
            var variant = new ProductVariant
            {
                Id = 101,
                ProductId = 1,
                Product = product,
                Name = "Bơ Sáp 034 Size VIP (2-3 quả/kg)",
                Code = "VAR-BO034-VIP",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 101, UoMId = 1, Price = 85000, IsDeleted = false }
                }
            };
            product.Variants.Add(variant);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ Solaris có sẵn Bơ Sáp 034 Lâm Đồng tươi ngon hái tận vườn đây ạ!");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Cửa hàng có bơ 034 ngon không em ơi?"
            };

            // Act
            var result = await service.SendMessageAsync(request, 10, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.SessionId.Should().BeGreaterThan(0);
            result.PayloadType.Should().Be("product_cards");
            result.Payload.Should().NotBeNull();
            result.Content.Should().Contain("Bơ Sáp 034");

            var messagesInDb = await context.ChatMessages.ToListAsync();
            messagesInDb.Should().HaveCount(2); // 1 User + 1 Model
        }
        #endregion

        #region TC08: GỬI TIN NHẮN ĐẶT LẠI ĐƠN CŨ -> TRẢ VỀ INTERACTIVE_ORDER PAYLOAD
        [Fact]
        public async Task SendMessageAsync_WithReorderIntent_ShouldExtractPreviousOrderAndReturnInteractiveOrderPayload()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-DUA", Name = "Dưa Lưới Huỳnh Long", Slug = "dua-luoi", BaseUoMId = 1, BaseUoM = uom };
            var variant = new ProductVariant { Id = 201, ProductId = 1, Product = product, Name = "Dưa Lưới Huỳnh Long Trái 1.5kg", Code = "VAR-DUA-01" };

            var pastOrder = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260825-111",
                CustomerId = 10,
                ReceiverName = "Khoa Tran",
                ReceiverPhone = "0987654321",
                DeliveryAddress = "456 Đường Lê Văn Sỹ, Phường 12, Quận 3, TP.HCM",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                SubTotal = 350000,
                TotalAmount = 350000,
                IsDeleted = false,
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        Id = 1,
                        VariantId = 201,
                        Variant = variant,
                        UoMId = 1,
                        UoM = uom,
                        Quantity = 2,
                        UnitPrice = 175000,
                        TotalPrice = 350000
                    }
                }
            };

            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.Orders.Add(pastOrder);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ em đã chuẩn bị lại đơn hàng dưa lưới quen thuộc của anh rồi ạ!");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Lên lại đơn cũ hôm qua giúp anh với"
            };

            // Act
            var result = await service.SendMessageAsync(request, 10, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("interactive_order");
            result.Payload.Should().BeOfType<InteractiveOrderPayloadDto>();

            var payload = (InteractiveOrderPayloadDto)result.Payload!;
            payload.PreviousOrderCode.Should().Be("ORD-20260825-111");
            payload.Items.Should().HaveCount(1);
            payload.Items[0].VariantId.Should().Be(201);
            payload.IsFreeShipping.Should().BeTrue(); // 350,000 >= 300,000
            payload.SuggestedReceiverName.Should().Be("Khoa Tran");
        }
        #endregion

        #region TC09: CHỐT ĐƠN HÀNG QUA CHAT VỚI PHƯƠNG THỨC COD -> TẠO ORDER & FREESHIP THÀNH CÔNG
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WithCodPayment_ShouldCreateOrderAndApplyFreeship()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var session = new ChatSession { Id = 1, CustomerId = 10, IsActive = true };
            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            context.ChatSessions.Add(session);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-SR-01", VariantId = 301, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                BatchId = 1,
                QuantityAvailable = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                ReceiverName = "Khoa Tran",
                ReceiverPhone = "0912345678",
                DeliveryAddress = "123 Đường Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM",
                GhnDistrictId = 1442,
                GhnWardCode = "20101",
                PaymentMethod = 1, // COD
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 2,
                        UnitPrice = 200000,
                        DiscountAmount = 0,
                        TotalPrice = 400000
                    }
                }
            };

            // Act
            var result = await service.ConfirmInteractiveOrderAsync(request, 10);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().BeGreaterThan(0);
            result.OrderCode.Should().StartWith("ORD-");
            result.TotalAmount.Should().Be(400000); // 400k >= 300k -> Freeship 100%
            result.PaymentMethodName.Should().Contain("COD");

            var createdOrder = await context.Orders.FindAsync(result.OrderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.Status.Should().Be(OrderStatus.Confirmed);
            createdOrder.ShippingFee.Should().Be(0); // Free ship
            createdOrder.PaymentMethod.Should().Be(PaymentMethod.COD);
            createdOrder.PaymentStatus.Should().Be(PaymentStatus.Unpaid);

            var lastMessage = await context.ChatMessages.OrderByDescending(m => m.CreatedAt).FirstOrDefaultAsync();
            lastMessage!.PayloadType.Should().Be("order_success");
        }
        #endregion

        #region TC10: CHỐT ĐƠN HÀNG QUA CHAT VỚI VNPAY -> GỌI VNPAY SERVICE SINH PAYMENT URL
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WithVnPayPayment_ShouldGeneratePaymentUrl()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var session = new ChatSession { Id = 1, CustomerId = 10, IsActive = true };
            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-CAM", Name = "Cam Sành Bến Tre", Slug = "cam-sanh", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 401, ProductId = 1, Product = product, Name = "Cam Sành Túi 2kg", Code = "VAR-CAM-01", IsActive = true, IsDeleted = false };

            context.ChatSessions.Add(session);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            var batch = new ProductBatch { Id = 2, BatchCode = "BATCH-CAM-01", VariantId = 401, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 2,
                WarehouseId = 1,
                VariantId = 401,
                BatchId = 2,
                QuantityAvailable = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            _mockVnPayService
                .CreatePaymentUrlAsync(Arg.Any<VnPayPaymentRequestDto>(), Arg.Any<HttpContext>())
                .Returns(Task.FromResult(new VnPayPaymentResponseDto
                {
                    OrderCode = "ORD-TEST-VNPAY",
                    PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=150000"
                }));

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                ReceiverName = "Nguyen Van B",
                ReceiverPhone = "0933333333",
                DeliveryAddress = "789 Đường CMT8, Phường 5, Tân Bình, TP.HCM",
                PaymentMethod = 3, // VNPay
                ShippingFee = 25000,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 401,
                        UoMId = 1,
                        Quantity = 2,
                        UnitPrice = 50000,
                        DiscountAmount = 0,
                        TotalPrice = 100000
                    }
                }
            };

            // Act
            var result = await service.ConfirmInteractiveOrderAsync(request, 10);

            // Assert
            result.Should().NotBeNull();
            result.PaymentMethodName.Should().Contain("VNPay");
            result.PaymentUrl.Should().StartWith("https://sandbox.vnpayment.vn/");
            result.TotalAmount.Should().Be(125000); // 100k + 25k ship (do < 300k)

            var createdOrder = await context.Orders.FindAsync(result.OrderId);
            createdOrder!.PaymentMethod.Should().Be(PaymentMethod.EWallet);
        }
        #endregion

        #region TC11: CHỐT ĐƠN VỚI DANH SÁCH SẢN PHẨM RỖNG -> NÉM ARGUMENT EXCEPTION
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WithEmptyItems_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                Items = new List<InteractiveOrderItemDto>()
            };

            // Act & Assert
            var act = () => service.ConfirmInteractiveOrderAsync(request, 10);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Danh sách sản phẩm trong đơn hàng không được để trống*");
        }
        #endregion

        #region TC12: THỰC THI REJECT KHI XÓA PHIÊN CỦA KHÁCH HÀNG KHÁC
        [Fact]
        public async Task DeleteSessionAsync_WithDifferentCustomerId_ShouldReturnFalse()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var session = new ChatSession
            {
                Id = 1,
                CustomerId = 88, // Thuộc khách hàng 88
                SessionToken = "tok-88",
                IsActive = true
            };
            context.ChatSessions.Add(session);
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            // Act: Khách hàng id = 1 cố tình xóa phiên của khách hàng id = 88
            var result = await service.DeleteSessionAsync(1, 1, "tok-stranger");

            // Assert
            result.Should().BeFalse();
            var inDb = await context.ChatSessions.FindAsync(1);
            inDb!.IsActive.Should().BeTrue(); // Không bị xóa
        }
        #endregion

        #region TC14: LÊN ĐƠN TRỰC TIẾP CHO 1 SẢN PHẨM CỤ THỂ -> THẺ ĐƠN HÀNG CHỈ CHỨA ĐÚNG SẢN PHẨM ĐÓ
        [Fact]
        public async Task SendMessageAsync_WhenOrderingSpecificProductInSharedCategory_ShouldOnlyIncludeRequestedProduct()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var cat = new ProductCategory { Id = 1, Name = "Bơ Sáp & Sầu Riêng", Code = "CAT-BO-SR", IsActive = true, IsDeleted = false };
            var uomKg = new UoM { Id = 1, Name = "Kilogram", Code = "KG" };
            var uomTrai = new UoM { Id = 5, Name = "Trái/Quả/Củ", Code = "TRAI" };

            var boSap = new Product
            {
                Id = 1,
                Name = "Bơ Sáp 034 Lâm Đồng (Loại 1)",
                Code = "SP-BO-034",
                Slug = "bo-sap-034-lam-dong-loai-1",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };
            var varBo = new ProductVariant
            {
                Id = 3,
                ProductId = 1,
                Product = boSap,
                Name = "Bơ Sáp 034 - Loại 1 (Size VIP 2-3 trái/kg)",
                Code = "SKU-BO034-VIP",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 3, UoMId = 1, Price = 90000, IsActive = true, IsDeleted = false }
                }
            };
            boSap.Variants.Add(varBo);

            var sauRieng = new Product
            {
                Id = 2,
                Name = "Sầu Riêng",
                Code = "SP-BO-035",
                Slug = "sau-rieng",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 5,
                BaseUoM = uomTrai,
                IsActive = true,
                IsDeleted = false
            };
            var varSR = new ProductVariant
            {
                Id = 4,
                ProductId = 2,
                Product = sauRieng,
                Name = "Sầu Riêng Loại 1 Trái",
                Code = "SR-1",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 2, VariantId = 4, UoMId = 5, Price = 100000, IsActive = true, IsDeleted = false }
                }
            };
            sauRieng.Variants.Add(varSR);

            context.ProductCategories.Add(cat);
            context.UoMs.AddRange(uomKg, uomTrai);
            context.Products.AddRange(boSap, sauRieng);
            context.ProductVariants.AddRange(varBo, varSR);

            var batchBo = new ProductBatch { Id = 1, BatchCode = "BATCH-BO-01", VariantId = 3, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            var batchSR = new ProductBatch { Id = 2, BatchCode = "BATCH-SR-01", VariantId = 4, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.AddRange(batchBo, batchSR);

            // Tồn kho: Bơ có 34kg, Sầu riêng chỉ còn 8 trái
            context.WarehouseInventories.AddRange(
                new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 3, BatchId = 1, QuantityAvailable = 34, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new WarehouseInventory { Id = 2, WarehouseId = 1, VariantId = 4, BatchId = 2, QuantityAvailable = 8, QuantityReserved = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ kho Solaris hiện chỉ còn 8 trái sầu riêng, hệ thống đã điều chỉnh số lượng.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Cho tôi 10 quả sầu riêng"
            };

            // Act
            var result = await service.SendMessageAsync(request, 10, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("interactive_order");
            result.Payload.Should().BeOfType<InteractiveOrderPayloadDto>();

            var order = (InteractiveOrderPayloadDto)result.Payload!;
            // CHỈ CÓ ĐÚNG 1 SẢN PHẨM LÀ SẦU RIÊNG, TUYỆT ĐỐI KHÔNG CÓ BƠ SÁP
            order.Items.Should().HaveCount(1);
            order.Items[0].VariantId.Should().Be(4);
            order.Items[0].VariantName.Should().Be("Sầu Riêng Loại 1 Trái");
            order.Items[0].Quantity.Should().Be(8); // Capped at available stock (8)
            order.Items[0].UnitPrice.Should().Be(100000);
            order.Items[0].TotalPrice.Should().Be(800000);
            order.StockWarning.Should().Contain("chỉ còn 8");
            order.StockWarning.Should().Contain("10");

            // Đảm bảo không có Bơ Sáp trong đơn
            order.Items.Any(i => i.VariantName.Contains("Bơ")).Should().BeFalse();
        }
        #endregion

        #region TC15: LÊN ĐƠN KHI NGƯỜI DÙNG YÊU CẦU NHIỀU SẢN PHẨM KHÁC NHAU
        [Fact]
        public async Task SendMessageAsync_WhenOrderingMultipleProductsExplicitly_ShouldExtractCorrectQuantities()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var cat = new ProductCategory { Id = 1, Name = "Bơ Sáp & Sầu Riêng", Code = "CAT-BO-SR", IsActive = true, IsDeleted = false };
            var uomKg = new UoM { Id = 1, Name = "Kilogram", Code = "KG" };
            var uomTrai = new UoM { Id = 5, Name = "Trái/Quả/Củ", Code = "TRAI" };

            var boSap = new Product
            {
                Id = 1,
                Name = "Bơ Sáp 034 Lâm Đồng (Loại 1)",
                Code = "SP-BO-034",
                Slug = "bo-sap-034-lam-dong-loai-1",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };
            var varBo = new ProductVariant
            {
                Id = 3,
                ProductId = 1,
                Product = boSap,
                Name = "Bơ Sáp 034",
                Code = "SKU-BO034",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 3, UoMId = 1, Price = 90000, IsActive = true, IsDeleted = false }
                }
            };
            boSap.Variants.Add(varBo);

            var sauRieng = new Product
            {
                Id = 2,
                Name = "Sầu Riêng",
                Code = "SP-BO-035",
                Slug = "sau-rieng",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 5,
                BaseUoM = uomTrai,
                IsActive = true,
                IsDeleted = false
            };
            var varSR = new ProductVariant
            {
                Id = 4,
                ProductId = 2,
                Product = sauRieng,
                Name = "Sầu Riêng",
                Code = "SR-1",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 2, VariantId = 4, UoMId = 5, Price = 100000, IsActive = true, IsDeleted = false }
                }
            };
            sauRieng.Variants.Add(varSR);

            context.ProductCategories.Add(cat);
            context.UoMs.AddRange(uomKg, uomTrai);
            context.Products.AddRange(boSap, sauRieng);
            context.ProductVariants.AddRange(varBo, varSR);

            var batchBo = new ProductBatch { Id = 10, BatchCode = "BATCH-BO-02", VariantId = 3, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            var batchSR = new ProductBatch { Id = 20, BatchCode = "BATCH-SR-02", VariantId = 4, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.AddRange(batchBo, batchSR);

            context.WarehouseInventories.AddRange(
                new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 3, BatchId = 10, QuantityAvailable = 50, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new WarehouseInventory { Id = 2, WarehouseId = 1, VariantId = 4, BatchId = 20, QuantityAvailable = 50, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );

            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ em đã chuẩn bị đơn gồm 2kg bơ và 1 quả sầu riêng cho anh rồi ạ!");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Cho tôi 2kg bơ và 1 quả sầu riêng"
            };

            // Act
            var result = await service.SendMessageAsync(request, 10, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("interactive_order");
            var order = (InteractiveOrderPayloadDto)result.Payload!;

            order.Items.Should().HaveCount(2);

            var boItem = order.Items.FirstOrDefault(i => i.VariantId == 3);
            boItem.Should().NotBeNull();
            boItem!.Quantity.Should().Be(2);

            var srItem = order.Items.FirstOrDefault(i => i.VariantId == 4);
            srItem.Should().NotBeNull();
            srItem!.Quantity.Should().Be(1);
        }
        #endregion

        #region TC16: RÀO CHẮN KHOẢNG CÁCH CHUỖI LẠNH KHI ĐẶT HÀNG QUA CHAT
        [Fact]
        public async Task SendMessageAsync_WhenColdChainProductExceedsRadius_ShouldMarkIsColdChainFeasibleFalse()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var catCold = new ProductCategory
            {
                Id = 1,
                Name = "Thực Phẩm Tươi Sống",
                Code = "FRESH_PRODUCE",
                RequiresColdChain = true,
                IsActive = true,
                IsDeleted = false
            };
            var uomKg = new UoM { Id = 1, Name = "Kilogram", Code = "KG" };

            var pork = new Product
            {
                Id = 1,
                Name = "Thịt Heo Sạch Sinh Học",
                Code = "SP-THIT-01",
                Slug = "thit-heo-sach",
                CategoryId = 1,
                Category = catCold,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };
            var varPork = new ProductVariant
            {
                Id = 10,
                ProductId = 1,
                Product = pork,
                Name = "Thịt Heo Sạch Sinh Học",
                Code = "SKU-PORK",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 120000, IsActive = true, IsDeleted = false }
                }
            };
            pork.Variants.Add(varPork);

            // Kho tại Đà Lạt (Lat: 11.94, Lon: 108.45), bán kính xe lạnh 15km
            var wh = new Warehouse
            {
                Id = 1,
                Code = "WH-DL",
                Name = "Kho Trung Tâm Đà Lạt",
                IsActive = true,
                IsDeleted = false,
                MaxColdChainRadiusKm = 15.0,
                Address = new WarehouseAddress
                {
                    Id = 1,
                    StreetAddress = "123 Phù Đổng Thiên Vương",
                    Ward = "Phường 8",
                    District = "Thành phố Đà Lạt",
                    Province = "Lâm Đồng",
                    Latitude = 11.9404,
                    Longitude = 108.4583
                }
            };

            // Khách hàng tại TP.HCM (Lat: 10.77, Lon: 106.69) cách Đà Lạt > 200km
            var customer = new Customer
            {
                Id = 100,
                Code = "CUST-100",
                Name = "Khách Hàng Ở Xa",
                PhoneNumber = "0912345678",
                IsActive = true,
                IsDeleted = false,
                Addresses = new List<CustomerAddress>
                {
                    new CustomerAddress
                    {
                        Id = 50,
                        CustomerId = 100,
                        ReceiverName = "Khách Hàng Ở Xa",
                        Phone = "0912345678",
                        StreetAddress = "1 Lê Duẩn",
                        Ward = "Phường Bến Nghé",
                        District = "Quận 1",
                        Province = "Hồ Chí Minh",
                        Latitude = 10.7769,
                        Longitude = 106.6951,
                        IsDefault = true,
                        IsDeleted = false
                    }
                }
            };

            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-PORK-01", VariantId = 10, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            var inv = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 50, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            context.ProductCategories.Add(catCold);
            context.UoMs.Add(uomKg);
            context.Products.Add(pork);
            context.ProductVariants.Add(varPork);
            context.Warehouses.Add(wh);
            context.Customers.Add(customer);
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inv);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ đơn hàng thịt heo đã được chuẩn bị.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Cho tôi 2kg thịt heo"
            };

            // Act
            var result = await service.SendMessageAsync(request, 100, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("interactive_order");
            var order = (InteractiveOrderPayloadDto)result.Payload!;
            order.IsColdChainFeasible.Should().BeFalse();
            order.IneligibleColdChainItems.Should().Contain("Thịt Heo Sạch Sinh Học");
            order.ColdChainWarning.Should().NotBeNullOrEmpty();
            order.ColdChainWarning.Should().Contain("2°C - 8°C");
        }
        #endregion

        #region TC17: TRA CỨU ĐƠN HÀNG THỜI GIAN THỰC KÈM THÔNG TIN CHUYẾN XE LẠNH TMS (KHÁCH VÃNG LAI)
        [Fact]
        public async Task LookupOrderAsync_WhenGuestCustomerQueriesOrderCode_ShouldReturnTrackingWithTMSVehicleAndDriver()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var vehicle = new DeliveryVehicle
            {
                Id = 1,
                Code = "VEH-BIKE-01",
                LicensePlate = "49-B1 999.88",
                VehicleType = "Motorbike",
                IsColdChainEquipped = true,
                MaxWeightKg = 80
            };

            var trip = new DeliveryTrip
            {
                Id = 10,
                TripCode = "TRIP-20260909-001",
                TripType = "B2C_Delivery",
                Status = "InTransit",
                VehicleId = 1,
                Vehicle = vehicle,
                LicensePlate = "49-B1 999.88",
                DriverName = "Nguyễn Văn Shipper",
                DriverPhone = "0909112233",
                WarehouseId = 1,
                StartedAt = DateTime.UtcNow.AddMinutes(-30)
            };

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260909-001",
                CustomerId = 20,
                OrderDate = DateTime.UtcNow.AddHours(-1),
                Status = OrderStatus.Shipping,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.EWallet,
                SubTotal = 350000,
                DiscountAmount = 0,
                ShippingFee = 0,
                TotalAmount = 350000,
                ReceiverName = "Trần Thị Khách",
                ReceiverPhone = "0988776655",
                DeliveryAddress = "123 Phường 1, Đà Lạt",
                DeliveryTripId = 10,
                DeliveryTrip = trip,
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        Id = 1,
                        OrderId = 1,
                        VariantId = 1,
                        Variant = new ProductVariant { Id = 1, Name = "Bơ Sáp 034", Code = "BO-034" },
                        Quantity = 3,
                        UnitPrice = 90000,
                        TotalPrice = 270000,
                        UoM = new UoM { Id = 1, Name = "Kg", Code = "KG" }
                    }
                }
            };

            context.DeliveryVehicles.Add(vehicle);
            context.DeliveryTrips.Add(trip);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ đơn hàng ORD-20260909-001 đang trên chuyến xe giao hàng lạnh TMS.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Kiểm tra đơn ORD-20260909-001"
            };

            // Act: Khách vãng lai (customerId = null)
            var result = await service.SendMessageAsync(request, null, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("order_tracking");
            var tracking = (AiOrderTrackingDto)result.Payload!;
            tracking.OrderCode.Should().Be("ORD-20260909-001");
            tracking.DeliveryTripCode.Should().Be("TRIP-20260909-001");
            tracking.LicensePlate.Should().Be("49-B1 999.88");
            tracking.DriverName.Should().Be("Nguyễn Văn Shipper");
            tracking.DriverPhone.Should().Be("0909112233");
            tracking.IsColdChainVehicle.Should().BeTrue();
            tracking.ShippingProvider.Should().Contain("Solaris Cold-Chain Express");
        }
        #endregion

        #region TC18: TRA CỨU ĐƠN HÀNG BỊ TỪ CHỐI / HỦY BỞI KHO KÈM LÝ DO HỦY
        [Fact]
        public async Task LookupOrderAsync_WhenOrderCancelledWithReason_ShouldExtractCancellationReason()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var order = new Order
            {
                Id = 2,
                OrderCode = "ORD-20260909-CANCEL",
                CustomerId = 20,
                OrderDate = DateTime.UtcNow.AddHours(-2),
                Status = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Refunded,
                PaymentMethod = PaymentMethod.EWallet,
                SubTotal = 200000,
                DiscountAmount = 0,
                ShippingFee = 25000,
                TotalAmount = 225000,
                ReceiverName = "Khách Bị Hủy",
                ReceiverPhone = "0988000111",
                DeliveryAddress = "Huyện Cát Tiên, Lâm Đồng",
                CancellationReason = "Kho từ chối do địa chỉ vượt quá bán kính bảo quản lạnh 15km của kho xe lạnh"
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ đơn hàng đã bị hủy do vượt quá bán kính xe lạnh.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Tra cứu đơn ORD-20260909-CANCEL"
            };

            // Act
            var result = await service.SendMessageAsync(request, 20, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("order_tracking");
            var tracking = (AiOrderTrackingDto)result.Payload!;
            tracking.OrderCode.Should().Be("ORD-20260909-CANCEL");
            tracking.Status.Should().Be(7); // Cancelled
            tracking.CancellationReason.Should().Be("Kho từ chối do địa chỉ vượt quá bán kính bảo quản lạnh 15km của kho xe lạnh");
        }
        #endregion

        #region TC19: XÁC NHẬN ĐẶT HÀNG VƯỢT BÁN KÍNH XE LẠNH NÉM INVALIDOPERATIONEXCEPTION
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenAddressExceedsColdChainRadius_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var catCold = new ProductCategory
            {
                Id = 2,
                Name = "Hải Sản Tươi Sống",
                Code = "SEAFOOD",
                RequiresColdChain = true,
                IsActive = true,
                IsDeleted = false
            };
            var uomKg = new UoM { Id = 1, Name = "Kilogram", Code = "KG" };

            var salmon = new Product
            {
                Id = 5,
                Name = "Cá Hồi Tươi Na Uy",
                Code = "SP-SALMON",
                CategoryId = 2,
                Category = catCold,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };
            var varSalmon = new ProductVariant
            {
                Id = 50,
                ProductId = 5,
                Product = salmon,
                Name = "Cá Hồi Tươi Na Uy (Fillet)",
                Code = "SKU-SALMON",
                IsActive = true,
                IsDeleted = false
            };
            salmon.Variants.Add(varSalmon);

            var wh = new Warehouse
            {
                Id = 1,
                Code = "WH-HCM",
                Name = "Kho Lạnh Quận 7",
                IsActive = true,
                IsDeleted = false,
                MaxColdChainRadiusKm = 10.0,
                Address = new WarehouseAddress
                {
                    Id = 1,
                    StreetAddress = "456 Nguyễn Lương Bằng",
                    Ward = "Phường Tân Phú",
                    District = "Quận 7",
                    Province = "Hồ Chí Minh",
                    Latitude = 10.7327,
                    Longitude = 106.7158
                }
            };

            // Khách hàng có địa chỉ tại Bình Dương (cách Q7 ~35km > 10km)
            var customer = new Customer
            {
                Id = 30,
                Code = "CUST-30",
                Name = "Khách Hàng Bình Dương",
                PhoneNumber = "0933445566",
                IsActive = true,
                IsDeleted = false,
                Addresses = new List<CustomerAddress>
                {
                    new CustomerAddress
                    {
                        Id = 88,
                        CustomerId = 30,
                        ReceiverName = "Khách Bình Dương",
                        Phone = "0933445566",
                        StreetAddress = "789 Đại Lộ Bình Dương",
                        Ward = "Phường Phú Hòa",
                        District = "TP. Thủ Dầu Một",
                        Province = "Bình Dương",
                        Latitude = 10.9805,
                        Longitude = 106.6519,
                        IsDefault = true,
                        IsDeleted = false
                    }
                }
            };

            var batch = new ProductBatch { Id = 5, BatchCode = "BATCH-SALMON-01", VariantId = 50, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            var inv = new WarehouseInventory { Id = 5, WarehouseId = 1, VariantId = 50, BatchId = 5, QuantityAvailable = 20, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

            context.ProductCategories.Add(catCold);
            context.UoMs.Add(uomKg);
            context.Products.Add(salmon);
            context.ProductVariants.Add(varSalmon);
            context.Warehouses.Add(wh);
            context.Customers.Add(customer);
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inv);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient();
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var confirmRequest = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 1,
                CustomerAddressId = 88,
                PaymentMethod = 1, // COD
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 50,
                        Quantity = 2,
                        UnitPrice = 250000,
                        DiscountAmount = 0,
                        TotalPrice = 500000
                    }
                }
            };

            // Act & Assert
            var act = async () => await service.ConfirmInteractiveOrderAsync(confirmRequest, 30);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vượt quá bán kính phục vụ xe thùng lạnh*");
        }
        #endregion

        #region TC20: TƯ VẤN SẢN PHẨM KHUNG VÀ BIẾN THỂ VỚI GIÁ CÁCH 1 VÀ CÁCH 2
        [Fact]
        public async Task SendMessageAsync_WhenCustomerAsksForProductGuidance_ShouldReturnProductCardsWithAvailablePrices()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var catGroup = new ProductCategoryGroup { Id = 1, Name = "Nông Sản Tươi", Code = "FRESH_PRODUCE" };
            var cat = new ProductCategory { Id = 1, Name = "Trái Cây Đặc Sản", Code = "FRUITS", CategoryGroupId = 1, CategoryGroup = catGroup, IsActive = true, IsDeleted = false };
            var uomKg = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var uomHop = new UoM { Id = 2, Name = "Hộp 3kg", Code = "BOX3" };

            var boSap = new Product
            {
                Id = 1,
                Name = "Bơ Sáp 034",
                Code = "SP-BO-034",
                Slug = "bo-sap-034",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };

            var varBo = new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Product = boSap,
                Name = "Bơ Sáp 034 Loại 1",
                Code = "SKU-BO034-L1",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 1, UoMId = 1, Price = 50000, IsDefault = true, IsActive = true, IsDeleted = false },
                    new ProductVariantPrice { Id = 2, VariantId = 1, UoMId = 2, Price = 140000, IsDefault = false, IsActive = true, IsDeleted = false }
                }
            };
            boSap.Variants.Add(varBo);

            context.ProductCategoryGroups.Add(catGroup);
            context.ProductCategories.Add(cat);
            context.UoMs.AddRange(uomKg, uomHop);
            context.Products.Add(boSap);
            context.ProductVariants.Add(varBo);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ Solaris có Bơ Sáp 034 với bảng giá quy cách lẻ và hộp ưu đãi.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Tư vấn bơ sáp cho tôi"
            };

            // Act
            var result = await service.SendMessageAsync(request, null, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("product_cards");
            var prods = (List<AiProductCardDto>)result.Payload!;
            prods.Should().NotBeEmpty();
            var card = prods.First();
            card.AvailablePrices.Should().NotBeNull();
            card.AvailablePrices.Should().HaveCount(2);
            card.AvailablePrices!.Any(p => p.Price == 50000).Should().BeTrue();
            card.AvailablePrices!.Any(p => p.Price == 140000).Should().BeTrue();
        }
        #endregion

        #region TC21: XÁC NHẬN ĐƠN HÀNG KHI SESSION ID = 0 (TRÁNH LỖI FOREIGN KEY CONSTRAINT)
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenSessionIdIsZero_ShouldResolveValidSessionAndCreateOrderSuccessfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            var customer = new Customer
            {
                Id = 10,
                Code = "KH-0010",
                Name = "Nguyễn Văn Test",
                PhoneNumber = "0912345678"
            };

            context.Customers.Add(customer);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-SR-01", VariantId = 301, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 0, // Mô phỏng frontend truyền 0 khi chưa nạp xong session
                ReceiverName = "Nguyễn Văn Test",
                ReceiverPhone = "0912345678",
                DeliveryAddress = "123 Đường Nguyễn Huệ, Quận 1, TP.HCM",
                PaymentMethod = 1, // COD
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 2,
                        UnitPrice = 200000,
                        DiscountAmount = 0,
                        TotalPrice = 400000
                    }
                }
            };

            // Act
            var result = await service.ConfirmInteractiveOrderAsync(request, 10);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().BeGreaterThan(0);
            result.OrderCode.Should().StartWith("ORD-");

            var createdOrder = await context.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == result.OrderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.WarehouseId.Should().Be(1);
            createdOrder.Status.Should().Be(OrderStatus.Confirmed);

            // Tồn kho phải được chuyển từ Available sang Reserved
            var inventory = await context.WarehouseInventories.FindAsync(1);
            inventory.Should().NotBeNull();
            inventory!.QuantityAvailable.Should().Be(48);
            inventory.QuantityReserved.Should().Be(2);

            // ChatMessage phải được lưu thành công vào ChatSession mới tạo mà không văng lỗi FK
            var savedMessage = await context.ChatMessages.FirstOrDefaultAsync(m => m.PayloadType == "order_success");
            savedMessage.Should().NotBeNull();
            savedMessage!.SessionId.Should().BeGreaterThan(0);
        }
        #endregion

        #region TC22: TƯ VẤN SẢN PHẨM KHI NGƯỜI DÙNG CHỈ GÕ "TƯ VẤN" HOẶC "TƯ VẤN GIÚP TÔI"
        [Fact]
        public async Task SendMessageAsync_WhenUserAsksGeneralConsultation_ShouldReturnProductCards()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var catGroup = new ProductCategoryGroup { Id = 1, Name = "Nông Sản Tươi", Code = "FRESH_PRODUCE" };
            var cat = new ProductCategory { Id = 1, Name = "Trái Cây Đặc Sản", Code = "FRUITS", CategoryGroupId = 1, CategoryGroup = catGroup, IsActive = true, IsDeleted = false };
            var uomKg = new UoM { Id = 1, Name = "Kg", Code = "KG" };

            var product = new Product
            {
                Id = 1,
                Name = "Dưa Lưới Huỳnh Long",
                Code = "SP-DUA-LUOI",
                Slug = "dua-luoi-huynh-long",
                CategoryId = 1,
                Category = cat,
                BaseUoMId = 1,
                BaseUoM = uomKg,
                IsActive = true,
                IsDeleted = false
            };

            var variant = new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Product = product,
                Name = "Dưa Lưới Huỳnh Long Loại 1",
                Code = "SKU-DUA-01",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 1, UoMId = 1, Price = 85000, IsDefault = true, IsActive = true, IsDeleted = false }
                }
            };
            product.Variants.Add(variant);

            context.ProductCategoryGroups.Add(catGroup);
            context.ProductCategories.Add(cat);
            context.UoMs.Add(uomKg);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ Solaris xin tư vấn dưa lưới Huỳnh Long giòn ngọt thanh mát.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Tư vấn giúp tôi"
            };

            // Act
            var result = await service.SendMessageAsync(request, null, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("product_cards");
            var prods = (List<AiProductCardDto>)result.Payload!;
            prods.Should().NotBeEmpty();
            prods.First().Name.Should().Contain("Dưa Lưới");
        }
        #endregion

        #region TC23: KIỂM TRA TRẠNG THÁI ĐƠN HÀNG VỚI CÁC CÂU HỎI ĐA DẠNG
        [Fact]
        public async Task SendMessageAsync_WhenUserAsksOrderStatus_ShouldRecognizeTrackingIntent()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var customer = new Customer { Id = 15, Code = "KH-0015", Name = "Trần Thị B", PhoneNumber = "0987654321" };
            context.Customers.Add(customer);

            var order = new Order
            {
                Id = 101,
                OrderCode = "ORD-20260913-ABC123",
                CustomerId = 15,
                ReceiverName = "Trần Thị B",
                ReceiverPhone = "0987654321",
                DeliveryAddress = "456 Lê Duẩn, Quận 1, TP.HCM",
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = PaymentMethod.COD,
                TotalAmount = 250000,
                OrderDate = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-2)
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ đơn hàng ORD-20260913-ABC123 của bạn đang ở trạng thái Chờ xử lý.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Kiểm tra trạng thái đơn hàng giúp tôi luôn"
            };

            // Act
            var result = await service.SendMessageAsync(request, 15, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("order_tracking");
            var tracking = (AiOrderTrackingDto)result.Payload!;
            tracking.Should().NotBeNull();
            tracking.OrderCode.Should().Be("ORD-20260913-ABC123");
            tracking.StatusName.Should().Be("Đã xác nhận");
        }
        #endregion

        #region TC24: ĐẶT LẠI ĐƠN CŨ VỚI TỒN KHO KHẢ DỤNG THỜI GIAN THỰC
        [Fact]
        public async Task SendMessageAsync_WhenUserAsksReOrder_ShouldPopulateRealtimeStockAndAdjustQuantity()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var customer = new Customer { Id = 20, Code = "KH-0020", Name = "Lê Văn C", PhoneNumber = "0933333333" };
            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 10, Code = "PRD-CAM", Name = "Cam Sành Tiền Giang", Slug = "cam-sanh", BaseUoMId = 1, IsActive = true, IsDeleted = false };
            var variant = new ProductVariant
            {
                Id = 501,
                ProductId = 10,
                Product = product,
                Name = "Cam Sành Loại 1",
                Code = "VAR-CAM-01",
                IsActive = true,
                IsDeleted = false,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 501, UoMId = 1, Price = 35000, IsDefault = true, IsActive = true, IsDeleted = false }
                }
            };
            product.Variants.Add(variant);

            context.Customers.Add(customer);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);

            // Đơn cũ khách từng mua 10kg
            var pastOrder = new Order
            {
                Id = 202,
                OrderCode = "ORD-PAST-001",
                CustomerId = 20,
                ReceiverName = "Lê Văn C",
                ReceiverPhone = "0933333333",
                DeliveryAddress = "789 Điện Biên Phủ, Bình Thạnh, TP.HCM",
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.COD,
                TotalAmount = 350000,
                OrderDate = DateTime.UtcNow.AddDays(-7),
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        VariantId = 501,
                        Variant = variant,
                        UoMId = 1,
                        UoM = uom,
                        Quantity = 10,
                        UnitPrice = 35000,
                        TotalPrice = 350000
                    }
                }
            };
            context.Orders.Add(pastOrder);

            // Hiện tại kho chỉ còn 4kg
            var batch = new ProductBatch { Id = 10, BatchCode = "BATCH-CAM", VariantId = 501, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 10,
                WarehouseId = 1,
                VariantId = 501,
                BatchId = 10,
                QuantityAvailable = 4, // Chỉ còn 4kg
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var client = CreateMockHttpClient("Dạ đơn cũ có Cam Sành, kho hiện còn 4kg nên em đã điều chỉnh số lượng.");
            var service = new GeminiChatService(client, _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new AiSendMessageRequestDto
            {
                Message = "Đặt lại đơn cũ giúp tôi"
            };

            // Act
            var result = await service.SendMessageAsync(request, 20, "127.0.0.1");

            // Assert
            result.Should().NotBeNull();
            result.PayloadType.Should().Be("interactive_order");
            var orderPayload = (InteractiveOrderPayloadDto)result.Payload!;
            orderPayload.Should().NotBeNull();
            orderPayload.PreviousOrderCode.Should().Be("ORD-PAST-001");
            orderPayload.Items.Should().HaveCount(1);
            var item = orderPayload.Items.First();
            item.AvailableStock.Should().Be(4);
            item.Quantity.Should().Be(4); // Đã được điều chỉnh xuống tồn kho tối đa
            item.WarningMessage.Should().NotBeNull();
            orderPayload.StockWarning.Should().NotBeNull();
        }
        #endregion

        #region TC25: XÁC THỰC VÀ MULTI-LAYER FALLBACK KHI XÁC NHẬN ĐƠN HÀNG (CUSTOMER ID RESOLUTION)
        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenCustomerIdIsNull_ShouldResolveFromSessionId()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            var customer = new Customer
            {
                Id = 10,
                Code = "KH-0010",
                Name = "Khách Hàng Session",
                PhoneNumber = "0912345678"
            };

            var session = new ChatSession
            {
                Id = 99,
                CustomerId = 10,
                SessionToken = "sess-token-99",
                Title = "Tư vấn hoa quả",
                CreatedAt = DateTime.UtcNow
            };

            context.Customers.Add(customer);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ChatSessions.Add(session);
            var batch1 = new ProductBatch { Id = 1, BatchCode = "BATCH-SR-01", VariantId = 301, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch1);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 99,
                ReceiverName = "Khách Hàng Session",
                ReceiverPhone = "0912345678",
                DeliveryAddress = "123 Đường Lê Lợi, Q1, TP.HCM",
                PaymentMethod = 1,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 1,
                        UnitPrice = 150000,
                        TotalPrice = 150000
                    }
                }
            };

            // Act - customerId là null nhưng có SessionId gắn với Customer 10
            var result = await service.ConfirmInteractiveOrderAsync(request, null);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().BeGreaterThan(0);
            var createdOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == result.OrderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.CustomerId.Should().Be(10);
        }

        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenCustomerIdIsNull_ShouldResolveFromCustomerAddressId()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            var customer = new Customer
            {
                Id = 25,
                Code = "KH-0025",
                Name = "Khách Hàng Địa Chỉ",
                PhoneNumber = "0988776655"
            };

            var address = new CustomerAddress
            {
                Id = 77,
                CustomerId = 25,
                ReceiverName = "Khách Hàng Địa Chỉ",
                Phone = "0988776655",
                Province = "Hồ Chí Minh",
                District = "Quận 5",
                Ward = "Phường 2",
                StreetAddress = "456 Đường Nguyễn Trãi",
                IsDefault = true,
                IsDeleted = false
            };

            context.Customers.Add(customer);
            context.CustomerAddresses.Add(address);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            var batch2 = new ProductBatch { Id = 1, BatchCode = "BATCH-SR-01", VariantId = 301, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch2);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 0,
                CustomerAddressId = 77,
                PaymentMethod = 1,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 1,
                        UnitPrice = 150000,
                        TotalPrice = 150000
                    }
                }
            };

            // Act - customerId là null nhưng có CustomerAddressId thuộc Customer 25
            var result = await service.ConfirmInteractiveOrderAsync(request, null);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().BeGreaterThan(0);
            var createdOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == result.OrderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.CustomerId.Should().Be(25);
        }

        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenCustomerIdIsNull_ShouldResolveFromReceiverPhone()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            var customer = new Customer
            {
                Id = 33,
                Code = "KH-0033",
                Name = "Khách Hàng SĐT",
                PhoneNumber = "0933445566"
            };

            context.Customers.Add(customer);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            var batch3 = new ProductBatch { Id = 1, BatchCode = "BATCH-SR-01", VariantId = 301, ExpiryDate = DateTime.UtcNow.AddMonths(1) };
            context.ProductBatches.Add(batch3);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 0,
                ReceiverName = "Khách Hàng SĐT",
                ReceiverPhone = "0933445566",
                DeliveryAddress = "789 Đường Nam Kỳ Khởi Nghĩa, Q3, TP.HCM",
                PaymentMethod = 1,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 1,
                        UnitPrice = 150000,
                        TotalPrice = 150000
                    }
                }
            };

            // Act - customerId là null nhưng có ReceiverPhone khớp Customer 33
            var result = await service.ConfirmInteractiveOrderAsync(request, null);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().BeGreaterThan(0);
            var createdOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == result.OrderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.CustomerId.Should().Be(33);
        }

        [Fact]
        public async Task ConfirmInteractiveOrderAsync_WhenCustomerIdIsNullAndNoFallbackMatches_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var product = new Product { Id = 1, Code = "PRD-SR", Name = "Sầu Riêng Ri6", Slug = "sau-rieng-ri6", BaseUoMId = 1 };
            var variant = new ProductVariant { Id = 301, ProductId = 1, Product = product, Name = "Sầu Riêng Ri6 Trái 3kg", Code = "VAR-SR-01", IsActive = true, IsDeleted = false };

            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 301,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new GeminiChatService(CreateMockHttpClient(), _config, context, _mapper, _mockVnPayService, _mockHttpContextAccessor);

            var request = new ConfirmInteractiveOrderRequestDto
            {
                SessionId = 0,
                ReceiverName = "Khách Lạ Vãng Lai",
                ReceiverPhone = "0999999999",
                DeliveryAddress = "Vãng lai",
                PaymentMethod = 1,
                Items = new List<InteractiveOrderItemDto>
                {
                    new InteractiveOrderItemDto
                    {
                        VariantId = 301,
                        UoMId = 1,
                        Quantity = 1,
                        UnitPrice = 150000,
                        TotalPrice = 150000
                    }
                }
            };

            // Act & Assert
            var act = async () => await service.ConfirmInteractiveOrderAsync(request, null);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Vui lòng đăng nhập tài khoản để xác nhận tạo đơn hàng.");
        }
        #endregion
    }
}
