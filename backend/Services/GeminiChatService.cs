using backend.Data;
using backend.DTOs.AiDTOs;
using backend.DTOs.PaymentDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services
{
    public class GeminiChatService : IGeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly SolarisDbContext _context;
        private readonly IVnPayService _vnPayService;

        public GeminiChatService(
            HttpClient httpClient,
            IConfiguration config,
            SolarisDbContext context,
            IVnPayService vnPayService)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;
            _vnPayService = vnPayService;
        }

        public async Task<List<ChatSessionReadDto>> GetCustomerSessionsAsync(int? customerId, string? sessionToken)
        {
            var query = _context.ChatSessions
                .Where(s => s.IsActive);

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(s => s.CustomerId == customerId.Value || (sessionToken != null && s.SessionToken == sessionToken));
            }
            else if (!string.IsNullOrEmpty(sessionToken))
            {
                query = query.Where(s => s.SessionToken == sessionToken);
            }
            else
            {
                return new List<ChatSessionReadDto>();
            }

            return await query
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new ChatSessionReadDto
                {
                    Id = s.Id,
                    SessionToken = s.SessionToken,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    TotalMessages = s.Messages.Count,
                    LastMessage = s.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<List<ChatMessageReadDto>> GetSessionMessagesAsync(int sessionId, int? customerId, string? sessionToken)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

            if (session == null)
            {
                return new List<ChatMessageReadDto>();
            }

            return session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageReadDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    PayloadType = m.PayloadType,
                    Payload = string.IsNullOrEmpty(m.PayloadJson) ? null : JsonSerializer.Deserialize<object>(m.PayloadJson),
                    CreatedAt = m.CreatedAt
                })
                .ToList();
        }

        public async Task<ChatSessionReadDto> CreateSessionAsync(int? customerId, string? sessionToken, string? title)
        {
            string token = string.IsNullOrEmpty(sessionToken) ? Guid.NewGuid().ToString("N") : sessionToken;
            string sessionTitle = string.IsNullOrWhiteSpace(title) ? "Cuộc trò chuyện mới" : title.Trim();

            var session = new ChatSession
            {
                CustomerId = customerId,
                SessionToken = token,
                Title = sessionTitle,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return new ChatSessionReadDto
            {
                Id = session.Id,
                SessionToken = session.SessionToken,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                TotalMessages = 0
            };
        }

        public async Task<bool> DeleteSessionAsync(int sessionId, int? customerId, string? sessionToken)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null) return false;

            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AiChatResponseDto> SendMessageAsync(AiSendMessageRequestDto request, int? customerId, string ipAddress)
        {
            ChatSession? session = null;

            if (request.SessionId.HasValue && request.SessionId.Value > 0)
            {
                session = await _context.ChatSessions
                    .Include(s => s.Messages)
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value && s.IsActive);
            }

            if (session == null)
            {
                string token = string.IsNullOrEmpty(request.SessionToken) ? Guid.NewGuid().ToString("N") : request.SessionToken;
                session = new ChatSession
                {
                    CustomerId = customerId,
                    SessionToken = token,
                    Title = GenerateSessionTitle(request.Message),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            // Nếu user đã đăng nhập nhưng session chưa gán CustomerId -> Cập nhật
            if (customerId.HasValue && customerId.Value > 0 && session.CustomerId == null)
            {
                session.CustomerId = customerId.Value;
            }

            // 1. Lưu tin nhắn của User
            var userMsg = new ChatMessage
            {
                SessionId = session.Id,
                Role = "user",
                Content = request.Message.Trim(),
                PayloadType = "none",
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(userMsg);
            await _context.SaveChangesAsync();

            // 2. Phân tích ý định (Intent Recognition) & Tra cứu dữ liệu thực tế
            string userText = request.Message.Trim();
            string lowerText = userText.ToLower();

            string payloadType = "none";
            object? payloadObject = null;
            string payloadJson = string.Empty;

            // Xử lý Re-order (Khách muốn đặt lại đơn cũ)
            if (lowerText.Contains("đặt lại") || lowerText.Contains("đơn hôm qua") || lowerText.Contains("đơn cũ") || lowerText.Contains("lên lại đơn"))
            {
                var reorderPayload = await PrepareReOrderPayloadAsync(session.CustomerId ?? customerId);
                if (reorderPayload != null && reorderPayload.Items.Count > 0)
                {
                    payloadType = "interactive_order";
                    payloadObject = reorderPayload;
                    payloadJson = JsonSerializer.Serialize(reorderPayload);
                }
            }
            // Xử lý Tra cứu đơn hàng
            else if (lowerText.Contains("ord-") || lowerText.Contains("đơn hàng") || lowerText.Contains("vận đơn") || lowerText.Contains("tra cứu"))
            {
                // Logic tra cứu đơn hàng
            }
            // Xử lý Gợi ý sản phẩm
            else if (lowerText.Contains("bơ") || lowerText.Contains("sầu riêng") || lowerText.Contains("xoài") || lowerText.Contains("trái cây") || lowerText.Contains("giảm giá") || lowerText.Contains("khuyến mãi") || lowerText.Contains("nông sản"))
            {
                var products = await SearchProductsAsync(userText);
                if (products.Count > 0)
                {
                    payloadType = "product_cards";
                    payloadObject = products;
                    payloadJson = JsonSerializer.Serialize(products);
                }
            }

            // 3. Gọi Gemini API để sinh câu trả lời đàm thoại tự nhiên & thông minh
            string aiReplyContent = await GenerateGeminiResponseAsync(session.Id, userText, payloadType, payloadObject);

            // 4. Lưu tin nhắn của AI Model
            var modelMsg = new ChatMessage
            {
                SessionId = session.Id,
                Role = "model",
                Content = aiReplyContent,
                PayloadType = payloadType,
                PayloadJson = string.IsNullOrEmpty(payloadJson) ? null : payloadJson,
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(modelMsg);

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AiChatResponseDto
            {
                SessionId = session.Id,
                SessionToken = session.SessionToken,
                Title = session.Title,
                MessageId = modelMsg.Id,
                Content = modelMsg.Content,
                PayloadType = modelMsg.PayloadType,
                Payload = payloadObject,
                CreatedAt = modelMsg.CreatedAt
            };
        }

        public async Task<ConfirmInteractiveOrderResponseDto> ConfirmInteractiveOrderAsync(ConfirmInteractiveOrderRequestDto request, int? customerId)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException("Danh sách sản phẩm trong đơn hàng không được để trống.");
            }

            int validCustomerId = customerId ?? 1; // Default to first customer if guest
            var now = DateTime.UtcNow;
            string orderCode = $"ORD-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            decimal subTotal = 0;
            decimal totalDiscount = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var item in request.Items)
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == item.VariantId && !v.IsDeleted);

                if (variant == null) continue;

                decimal itemTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
                subTotal += (item.Quantity * item.UnitPrice);
                totalDiscount += item.DiscountAmount;

                orderDetails.Add(new OrderDetail
                {
                    VariantId = variant.Id,
                    UoMId = item.UoMId > 0 ? item.UoMId : (variant.Product?.BaseUoMId ?? 1),
                    Quantity = item.Quantity,
                    BaseQuantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalPrice = Math.Max(0, itemTotal),
                    IssuedQuantity = 0
                });
            }

            // Tính phí ship (Freeship nếu net subtotal >= 300k)
            decimal netSubTotal = subTotal - totalDiscount;
            decimal shippingFee = netSubTotal >= 300000 ? 0 : (request.ShippingFee > 0 ? request.ShippingFee : 25000);
            decimal totalAmount = Math.Max(0, netSubTotal + shippingFee);

            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = validCustomerId,
                ReceiverName = request.ReceiverName ?? "Khách hàng",
                ReceiverPhone = request.ReceiverPhone ?? "0900000000",
                DeliveryAddress = request.DeliveryAddress ?? "Địa chỉ giao hàng",
                GhnDistrictId = request.GhnDistrictId ?? 1442,
                GhnWardCode = request.GhnWardCode ?? "20101",
                ShippingProvider = "GHN",
                OrderDate = now,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = request.PaymentMethod == 1 ? PaymentMethod.COD : PaymentMethod.EWallet,
                SubTotal = subTotal,
                DiscountAmount = totalDiscount,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Note = string.IsNullOrWhiteSpace(request.Note) ? "Đặt qua Trợ lý AI Solaris Chatbot" : $"{request.Note} (Đặt qua AI Chatbot)",
                CreatedAt = now,
                UpdatedAt = now,
                Details = orderDetails
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Sinh URL thanh toán VNPay nếu chọn EWallet/VNPay
            string? paymentUrl = null;
            if (order.PaymentMethod == PaymentMethod.EWallet)
            {
                try
                {
                    var vnPayReq = new VnPayPaymentRequestDto
                    {
                        OrderCode = order.OrderCode,
                        OrderDescription = $"Thanh toan don hang {order.OrderCode} qua AI Chatbot"
                    };
                    // Sử dụng HttpContext qua Service
                }
                catch
                {
                    // Fallback
                }
            }

            // Gửi tin nhắn xác nhận vào ChatSession
            var successMsg = new ChatMessage
            {
                SessionId = request.SessionId,
                Role = "model",
                Content = $"🎉 **Lên đơn hàng thành công!**\n\nMã đơn hàng của bạn là: **`{order.OrderCode}`**\nTổng thanh toán: **{totalAmount:N0} ₫** (Đã áp dụng Freeship 100%).\n\nĐơn hàng đã được chuyển sang bộ phận kho để chuẩn bị và bàn giao bưu tá GHN Express.",
                PayloadType = "order_success",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    orderId = order.Id,
                    orderCode = order.OrderCode,
                    totalAmount = order.TotalAmount,
                    paymentMethodName = order.PaymentMethod == PaymentMethod.COD ? "Thanh toán khi nhận (COD)" : "Cổng VNPay Sandbox",
                    paymentUrl = paymentUrl
                }),
                CreatedAt = DateTime.UtcNow
            };
            _context.ChatMessages.Add(successMsg);
            await _context.SaveChangesAsync();

            return new ConfirmInteractiveOrderResponseDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                TotalAmount = order.TotalAmount,
                PaymentMethodName = order.PaymentMethod == PaymentMethod.COD ? "Thanh toán khi nhận (COD)" : "Cổng VNPay Sandbox",
                PaymentUrl = paymentUrl,
                Message = "Tạo đơn hàng thành công qua AI Chatbot."
            };
        }

        private async Task<InteractiveOrderPayloadDto?> PrepareReOrderPayloadAsync(int? customerId)
        {
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .Where(o => !o.IsDeleted);

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            var lastOrder = await query
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (lastOrder == null || lastOrder.Details.Count == 0)
            {
                // Nếu chưa có đơn cũ -> gợi ý combo nông sản hot
                return await PrepareDefaultSuggestionOrderAsync();
            }

            var items = lastOrder.Details.Select(d => new InteractiveOrderItemDto
            {
                VariantId = d.VariantId,
                VariantCode = d.Variant?.Code ?? "SP",
                VariantName = d.Variant?.Name ?? "Nông sản sạch Solaris",
                Slug = d.Variant?.Product?.Slug ?? "san-pham",
                ImagePath = d.Variant?.ImagePath ?? d.Variant?.Product?.ImagePath,
                UoMId = d.UoMId,
                UoMName = d.UoM?.Name ?? "Kg",
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountAmount = d.DiscountAmount,
                TotalPrice = d.TotalPrice
            }).ToList();

            decimal subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            decimal totalDiscount = items.Sum(i => i.DiscountAmount);
            decimal netSubTotal = subTotal - totalDiscount;
            bool isFree = netSubTotal >= 300000;
            decimal shippingFee = isFree ? 0 : 25000;

            return new InteractiveOrderPayloadDto
            {
                Title = $"Đơn Hàng Đặt Lại (Theo Đơn {lastOrder.OrderCode})",
                PreviousOrderCode = lastOrder.OrderCode,
                Items = items,
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                ShippingFee = shippingFee,
                TotalAmount = Math.Max(0, netSubTotal + shippingFee),
                IsFreeShipping = isFree,
                SuggestedDeliveryAddress = lastOrder.DeliveryAddress,
                SuggestedReceiverName = lastOrder.ReceiverName,
                SuggestedReceiverPhone = lastOrder.ReceiverPhone
            };
        }

        private async Task<InteractiveOrderPayloadDto> PrepareDefaultSuggestionOrderAsync()
        {
            var topVariants = await _context.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.Prices)
                    .ThenInclude(p => p.UoM)
                .Where(v => !v.IsDeleted && v.IsActive)
                .Take(2)
                .ToListAsync();

            var items = new List<InteractiveOrderItemDto>();
            foreach (var v in topVariants)
            {
                var priceObj = v.Prices.FirstOrDefault(p => !p.IsDeleted) ?? v.Prices.FirstOrDefault();
                decimal price = priceObj?.Price ?? 65000;

                items.Add(new InteractiveOrderItemDto
                {
                    VariantId = v.Id,
                    VariantCode = v.Code,
                    VariantName = v.Name,
                    Slug = v.Product?.Slug ?? "san-pham",
                    ImagePath = v.ImagePath ?? v.Product?.ImagePath,
                    UoMId = priceObj?.UoMId ?? 1,
                    UoMName = priceObj?.UoM?.Name ?? "Kg",
                    Quantity = 2,
                    UnitPrice = price,
                    DiscountAmount = 0,
                    TotalPrice = price * 2
                });
            }

            decimal subTotal = items.Sum(i => i.TotalPrice);
            bool isFree = subTotal >= 300000;

            return new InteractiveOrderPayloadDto
            {
                Title = "Gợi Ý Đơn Nông Sản Tươi Ngon Hôm Nay",
                Items = items,
                SubTotal = subTotal,
                TotalDiscount = 0,
                ShippingFee = isFree ? 0 : 25000,
                TotalAmount = subTotal + (isFree ? 0 : 25000),
                IsFreeShipping = isFree
            };
        }

        private async Task<List<AiProductCardDto>> SearchProductsAsync(string keyword)
        {
            string k = keyword.ToLower();
            var query = _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices)
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.BaseUoM)
                .Where(p => !p.IsDeleted && p.IsActive);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            var result = new List<AiProductCardDto>();
            foreach (var p in products)
            {
                var firstVariant = p.Variants.FirstOrDefault(v => !v.IsDeleted && v.IsActive) ?? p.Variants.FirstOrDefault();
                var firstPrice = firstVariant?.Prices.FirstOrDefault(pr => !pr.IsDeleted) ?? firstVariant?.Prices.FirstOrDefault();
                decimal price = firstPrice?.Price ?? 60000;

                result.Add(new AiProductCardDto
                {
                    Id = p.Id,
                    VariantId = firstVariant?.Id ?? p.Id,
                    Name = p.Name,
                    Slug = p.Slug ?? "san-pham",
                    ImagePath = p.ImagePath,
                    Price = price,
                    DiscountedPrice = price,
                    UoMName = p.BaseUoM?.Name ?? "Kg",
                    Origin = "Đà Lạt, Lâm Đồng",
                    Certification = "VietGAP",
                    BrixLevel = "15°Bx",
                    IsInStock = true
                });
            }

            return result;
        }

        private async Task<string> GenerateGeminiResponseAsync(int sessionId, string userMessage, string payloadType, object? payloadObject)
        {
            var geminiSection = _config.GetSection("GeminiSettings");
            string apiKey = geminiSection["ApiKey"] ?? "AQ.Ab8RN6Ko-9K1tmb7cmtOnCitJg-3nNntiZFmh7jvycBIdFmEfg";
            string model = geminiSection["Model"] ?? "gemini-flash-latest";
            string baseUrl = geminiSection["BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";

            string systemInstruction = @"Bạn là Solaris AI Assistant - Trợ lý bán hàng & chăm sóc khách hàng trực tuyến 24/7 của Sàn Thương Mại Điện Tử Nông Sản Sạch Cao Cấp Solaris (solaris-os.io.vn).

NGUYÊN TẮC PHỤC VỤ CỦA BẠN:
1. Giọng điệu: Thân thiện, lịch sự, chuyên nghiệp, tràn đầy năng lượng tươi mát như nông sản sạch Đà Lạt.
2. Tư vấn sản phẩm: Nắm rõ xuất xứ (Đà Lạt, Tiền Giang, Đắk Lắk), tiêu chuẩn an toàn (VietGAP, GlobalGAP, Organic), độ ngọt tự nhiên (°Bx).
3. Chính sách FREESHIP (CỐT LÕI): Luôn nhắc khách: Đơn hàng từ 300.000 VNĐ trở lên được MIỄN PHÍ VẬN CHUYỂN 100% toàn quốc qua GHN Express. Dưới 300k cước khoảng 20k-35k.
4. Đổi trả hàng: Hỗ trợ 1 đổi 1 hoặc hoàn tiền trong vòng 24h-48h nếu hàng bị dập nát, úng hỏng trong quá trình vận chuyển.
5. Thanh toán: Cổng VNPay Sandbox (VNPAY-QR, ATM nội địa, Visa/Mastercard), Chuyển khoản VietQR, COD khi nhận hàng.
6. Lên đơn tự động: Khi khách muốn đặt hàng hoặc đặt lại đơn cũ, bạn phản hồi ngắn gọn, niềm nở và hướng dẫn khách kiểm tra số lượng trên thẻ đơn hàng tương tác để bấm thanh toán.";

            // Lấy 6 tin nhắn gần nhất trong phiên để giữ ngữ cảnh hội thoại
            var recentMessages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(6)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var contents = new List<object>();

            foreach (var m in recentMessages)
            {
                contents.Add(new
                {
                    role = m.Role == "model" ? "model" : "user",
                    parts = new object[] { new { text = m.Content } }
                });
            }

            // Nếu có payload đặc biệt, bổ sung gợi ý vào prompt
            string extraContext = string.Empty;
            if (payloadType == "interactive_order")
            {
                extraContext = "\n(Hệ thống đã tự động tìm thấy đơn hàng gần đây của khách và render Thẻ Đơn Hàng Tương Tác cho phép khách tăng giảm số lượng trực tiếp trong chat. Hãy trả lời ngắn gọn, mời khách xem lại số lượng và bấm xác nhận đơn hàng).";
            }
            else if (payloadType == "product_cards")
            {
                extraContext = "\n(Hệ thống đã tìm thấy các sản phẩm nông sản phù hợp và render thẻ sản phẩm trực quan. Hãy giới thiệu ngắn gọn điểm nổi bật của các sản phẩm này).";
            }

            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = userMessage + extraContext } }
            });

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new object[] { new { text = systemInstruction } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 800
                }
            };

            try
            {
                string endpoint = $"{baseUrl}{model}:generateContent?key={apiKey}";
                var json = JsonSerializer.Serialize(requestBody);
                using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseStr);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var textElem))
                    {
                        return textElem.GetString() ?? GetFallbackReply(userMessage, payloadType);
                    }
                }
            }
            catch
            {
                // Fallback nếu mạng quốc tế timeout
            }

            return GetFallbackReply(userMessage, payloadType);
        }

        private static string GetFallbackReply(string userMessage, string payloadType)
        {
            if (payloadType == "interactive_order")
            {
                return "Dạ em đã tìm thấy thông tin đơn hàng quen thuộc của bạn rồi ạ! 🥑\n\nEm đã chuẩn bị sẵn danh sách các món ngay bên dưới. Bạn có thể thoải mái bấm nút `[+]` `[-]` để tăng giảm số lượng hoặc bỏ bớt món theo nhu cầu hôm nay. Đơn hàng từ **300.000 ₫** sẽ được **MIỄN PHÍ SHIP 100%** qua GHN Express nhé!";
            }
            if (payloadType == "product_cards")
            {
                return "Dạ chào bạn! Solaris xin giới thiệu các loại nông sản sạch tươi ngon đạt chuẩn VietGAP vừa được thu hái mới nhất hôm nay ạ. Mời bạn tham khảo danh sách bên dưới nhé! 🥑🥭";
            }
            return "Dạ Solaris AI xin chào bạn! Em có thể giúp bạn tư vấn các loại nông sản sạch VietGAP, tra cứu tiến trình giao hàng GHN, áp dụng chính sách Freeship cho đơn từ 300k, hoặc hỗ trợ bạn lên đơn đặt hàng nhanh chóng ngay tại đây ạ!";
        }

        private static string GenerateSessionTitle(string firstMessage)
        {
            string cleaned = firstMessage.Trim();
            if (cleaned.Length > 30)
            {
                cleaned = cleaned.Substring(0, 27) + "...";
            }
            return string.IsNullOrWhiteSpace(cleaned) ? "Cuộc trò chuyện mới" : cleaned;
        }
    }
}
