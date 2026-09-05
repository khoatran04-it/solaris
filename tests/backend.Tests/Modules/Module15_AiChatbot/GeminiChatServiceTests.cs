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
            order.Items[0].VariantName.Should().Be("Sầu Riêng");
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
    }
}
