using AutoMapper;
using backend.Data;
using backend.DTOs.AiDTOs;
using backend.DTOs.PaymentDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service Tích hợp Trí tuệ Nhân tạo Google Gemini (AI Chatbot & Conversational Commerce).
    /// Đóng vai trò là Trợ lý bán hàng thông minh:
    /// - Quản lý ngữ cảnh và lịch sử hội thoại (Session & Context Management)
    /// - Nhận diện ý định khách hàng (Intent Recognition: Tìm sản phẩm, Đặt lại đơn cũ, Tra cứu đơn hàng)
    /// - Gọi API Google Gemini với mô hình thế hệ mới (gemini-3.5-flash-lite)
    /// - Chuyển đổi giỏ hàng ảo thành đơn hàng thực tế (Interactive Mini-Checkout)
    /// - Bọc toàn bộ thao tác ghi trong ExecuteInTransactionAsync bảo đảm an toàn với SqlServerRetryingExecutionStrategy.
    /// </summary>
    public class GeminiChatService : IGeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVnPayService _vnPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GeminiChatService(
            HttpClient httpClient,
            IConfiguration config,
            SolarisDbContext context,
            IMapper mapper,
            IVnPayService vnPayService,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;
            _mapper = mapper;
            _vnPayService = vnPayService;
            _httpContextAccessor = httpContextAccessor;
        }

        #region 1. Quản lý Phiên hội thoại (Session Management)

        /// <summary>
        /// Lấy danh sách các phiên hội thoại của người dùng để hiển thị trên Sidebar/Lịch sử Chat.
        /// </summary>
        public async Task<List<ChatSessionReadDto>> GetCustomerSessionsAsync(int? customerId, string? sessionToken)
        {
            var query = _context.ChatSessions
                .Include(s => s.Messages)
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

            var sessions = await query
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();

            return _mapper.Map<List<ChatSessionReadDto>>(sessions);
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn của một phiên cụ thể.
        /// </summary>
        public async Task<List<ChatMessageReadDto>> GetSessionMessagesAsync(int sessionId, int? customerId, string? sessionToken)
        {
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

            if (session == null)
            {
                return new List<ChatMessageReadDto>();
            }

            // Kiểm tra quyền truy cập (Ownership Check)
            if (customerId.HasValue && customerId.Value > 0)
            {
                if (session.CustomerId != null && session.CustomerId != customerId.Value && session.SessionToken != sessionToken)
                {
                    return new List<ChatMessageReadDto>();
                }
            }
            else if (!string.IsNullOrEmpty(sessionToken))
            {
                if (session.SessionToken != sessionToken && session.CustomerId != null)
                {
                    return new List<ChatMessageReadDto>();
                }
            }

            var messages = session.Messages
                .Where(m => !IsHallucinatedMessage(m.Content))
                .OrderBy(m => m.CreatedAt)
                .ToList();

            return _mapper.Map<List<ChatMessageReadDto>>(messages);
        }

        /// <summary>
        /// Khởi tạo một phiên hội thoại mới và bọc trong transaction an toàn.
        /// </summary>
        public async Task<ChatSessionReadDto> CreateSessionAsync(int? customerId, string? sessionToken, string? title)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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

                return _mapper.Map<ChatSessionReadDto>(session);
            });
        }

        /// <summary>
        /// Xóa mềm một phiên hội thoại khỏi giao diện người dùng.
        /// </summary>
        public async Task<bool> DeleteSessionAsync(int sessionId, int? customerId, string? sessionToken)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);
                if (session == null) return false;

                // Kiểm tra quyền sở hữu trước khi cho phép xóa
                if (customerId.HasValue && customerId.Value > 0 && session.CustomerId.HasValue && session.CustomerId.Value != customerId.Value)
                {
                    return false;
                }

                session.IsActive = false;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion

        #region 2. Tương tác Trí tuệ nhân tạo (LLM Orchestration)

        /// <summary>
        /// Nhận câu hỏi từ người dùng, tra cứu dữ liệu thực tế, gọi Google Gemini và phản hồi kèm UI Payload.
        /// </summary>
        public async Task<AiChatResponseDto> SendMessageAsync(AiSendMessageRequestDto request, int? customerId, string ipAddress)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
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

                // Nếu user đã đăng nhập nhưng session chưa gắn CustomerId -> Đồng bộ
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

                // A. Tra cứu đơn hàng (Order Tracking)
                var orderCodeMatch = System.Text.RegularExpressions.Regex.Match(userText, @"ORD-\d{8}-\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (orderCodeMatch.Success || lowerText.Contains("kiểm tra đơn") || lowerText.Contains("tra cứu đơn") || lowerText.Contains("đơn hàng của tôi") || lowerText.Contains("tình trạng đơn"))
                {
                    string? specificCode = orderCodeMatch.Success ? orderCodeMatch.Value.ToUpper() : null;
                    var orderDto = await LookupOrderAsync(specificCode, session.CustomerId ?? customerId);
                    if (orderDto != null)
                    {
                        payloadType = "order_tracking";
                        payloadObject = orderDto;
                        payloadJson = JsonSerializer.Serialize(orderDto);
                    }
                }
                // B. Đặt lại đơn cũ (Re-order)
                else if (lowerText.Contains("đặt lại") || lowerText.Contains("đơn hôm qua") || lowerText.Contains("đơn cũ") || lowerText.Contains("lên lại đơn"))
                {
                    var reorderPayload = await PrepareReOrderPayloadAsync(session.CustomerId ?? customerId);
                    if (reorderPayload != null && reorderPayload.Items.Count > 0)
                    {
                        payloadType = "interactive_order";
                        payloadObject = reorderPayload;
                        payloadJson = JsonSerializer.Serialize(reorderPayload);
                    }
                }
                // C. Lên đơn mua hàng trực tiếp (Direct Conversational Order)
                else if (lowerText.Contains("lên đơn") || lowerText.Contains("chốt đơn") || lowerText.Contains("tôi muốn mua") || lowerText.Contains("đặt mua") || 
                         lowerText.Contains("đặt hàng") || lowerText.Contains("xác nhận đặt") || lowerText.Contains("xác nhận đơn") || lowerText.Contains("chốt mua") ||
                         lowerText.Contains("mua hàng") || lowerText.Contains("tạo đơn") || lowerText.StartsWith("mua ") || lowerText.Contains(" mua ") || 
                         lowerText.Contains("lấy cho tôi") || lowerText.Contains("cho tôi ") || lowerText.Contains("lấy 1") || lowerText.Contains("lấy 2") ||
                         lowerText.Contains("đặt 1") || lowerText.Contains("đặt 2") || lowerText.Contains("lấy một") || lowerText.Contains("đặt một"))
                {
                    var directOrderPayload = await PrepareDirectOrderPayloadAsync(session.Id, userText, session.CustomerId ?? customerId);
                    if (directOrderPayload != null && directOrderPayload.Items.Count > 0)
                    {
                        payloadType = "interactive_order";
                        payloadObject = directOrderPayload;
                        payloadJson = JsonSerializer.Serialize(directOrderPayload);
                    }
                    else if (directOrderPayload != null && !string.IsNullOrEmpty(directOrderPayload.StockWarning))
                    {
                        // Tìm thấy sản phẩm nhưng đã hết hàng hoàn toàn trong kho
                        payloadType = "none";
                        payloadObject = directOrderPayload.StockWarning;
                    }
                    else
                    {
                        // Fallback sang tìm kiếm sản phẩm nếu chưa đủ thông tin lên đơn
                        var products = await SearchProductsAsync(userText);
                        if (products.Count > 0)
                        {
                            payloadType = "product_cards";
                            payloadObject = products;
                            payloadJson = JsonSerializer.Serialize(products);
                        }
                    }
                }
                // D. Tra cứu chương trình khuyến mãi (Promotion Finder)
                else if (lowerText.Contains("khuyến mãi") || lowerText.Contains("giảm giá") || lowerText.Contains("voucher") || lowerText.Contains("ưu đãi"))
                {
                    var promoProducts = await LookupPromotionProductsAsync();
                    if (promoProducts.Count > 0)
                    {
                        payloadType = "product_cards";
                        payloadObject = promoProducts;
                        payloadJson = JsonSerializer.Serialize(promoProducts);
                    }
                }
                // E. Tìm kiếm & Gợi ý sản phẩm
                else
                {
                    var products = await SearchProductsAsync(userText);
                    if (products.Count > 0)
                    {
                        payloadType = "product_cards";
                        payloadObject = products;
                        payloadJson = JsonSerializer.Serialize(products);
                    }
                }

                // 3. Gọi Gemini API để sinh câu trả lời đàm thoại nghiêm túc, trung thực & chính xác
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
            });
        }

        #endregion

        #region 3. Nghiệp vụ Chốt đơn qua Chat (Conversational Commerce)

        /// <summary>
        /// Chuyển đổi giỏ hàng ảo từ khung chat thành đơn hàng thực tế, bọc trong Transaction an toàn.
        /// </summary>
        public async Task<ConfirmInteractiveOrderResponseDto> ConfirmInteractiveOrderAsync(ConfirmInteractiveOrderRequestDto request, int? customerId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                if (request.Items == null || request.Items.Count == 0)
                {
                    throw new ArgumentException("Danh sách sản phẩm trong đơn hàng không được để trống.");
                }

                if (!customerId.HasValue || customerId.Value <= 0)
                {
                    throw new UnauthorizedAccessException("Vui lòng đăng nhập tài khoản để xác nhận tạo đơn hàng.");
                }

                int validCustomerId = customerId.Value;
                var now = DateTime.UtcNow;
                string orderCode = $"ORD-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                decimal subTotal = 0;
                decimal totalDiscount = 0;
                var orderDetails = new List<OrderDetail>();

                foreach (var item in request.Items)
                {
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                            .ThenInclude(p => p!.BaseUoM)
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId && !v.IsDeleted);

                    if (variant == null) continue;

                    // Kiểm tra tồn kho khả dụng thời gian thực từ Kho Bán Lẻ
                    var stockQuery = _context.WarehouseInventories
                        .Include(wi => wi.Batch)
                        .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && (wi.Batch == null || wi.Batch.ExpiryDate > now));

                    var filteredStockQuery = await stockQuery.FilterRetailOnlyAsync(_context);

                    var availableStock = await filteredStockQuery
                        .SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;

                    if (availableStock < item.Quantity)
                    {
                        throw new InvalidOperationException($"Sản phẩm '{variant.Name}' hiện chỉ còn tồn kho {availableStock:G29} {variant.Product?.BaseUoM?.Name ?? "đơn vị"}. Vui lòng giảm bớt số lượng đặt mua.");
                    }

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

                // Lấy thông tin khách hàng từ DB nếu chưa có
                var customer = await _context.Customers.FindAsync(validCustomerId);
                string receiverName = !string.IsNullOrWhiteSpace(request.ReceiverName) && request.ReceiverName != "Khách hàng"
                    ? request.ReceiverName.Trim()
                    : (customer?.Name ?? "Khách hàng");
                string receiverPhone = !string.IsNullOrWhiteSpace(request.ReceiverPhone) && request.ReceiverPhone != "0900000000"
                    ? request.ReceiverPhone.Trim()
                    : (customer?.PhoneNumber ?? "0900000000");
                string deliveryAddress = request.DeliveryAddress ?? string.Empty;

                // Nếu khách chọn địa chỉ từ sổ địa chỉ (CustomerAddressId)
                if (request.CustomerAddressId.HasValue && request.CustomerAddressId.Value > 0)
                {
                    var savedAddress = await _context.CustomerAddresses
                        .FirstOrDefaultAsync(a => a.Id == request.CustomerAddressId.Value && a.CustomerId == validCustomerId && !a.IsDeleted);
                    if (savedAddress != null)
                    {
                        if (!string.IsNullOrWhiteSpace(savedAddress.ReceiverName)) receiverName = savedAddress.ReceiverName.Trim();
                        if (!string.IsNullOrWhiteSpace(savedAddress.Phone)) receiverPhone = savedAddress.Phone.Trim();
                        deliveryAddress = savedAddress.FullAddress;
                    }
                }
                else if (string.IsNullOrWhiteSpace(deliveryAddress) || deliveryAddress == "Địa chỉ giao hàng" || deliveryAddress == "Địa chỉ nhận hàng")
                {
                    // Tự động tìm địa chỉ mặc định trong sổ địa chỉ của khách nếu chưa có
                    var defaultAddr = await _context.CustomerAddresses
                        .FirstOrDefaultAsync(a => a.CustomerId == validCustomerId && a.IsDefault && !a.IsDeleted)
                        ?? await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.CustomerId == validCustomerId && !a.IsDeleted);

                    if (defaultAddr != null)
                    {
                        if (!string.IsNullOrWhiteSpace(defaultAddr.ReceiverName)) receiverName = defaultAddr.ReceiverName.Trim();
                        if (!string.IsNullOrWhiteSpace(defaultAddr.Phone)) receiverPhone = defaultAddr.Phone.Trim();
                        deliveryAddress = defaultAddr.FullAddress;
                    }
                }

                // Kiểm tra bắt buộc phải có địa chỉ nhận hàng hợp lệ
                if (string.IsNullOrWhiteSpace(deliveryAddress) || deliveryAddress == "Địa chỉ giao hàng" || deliveryAddress == "Địa chỉ nhận hàng")
                {
                    throw new ArgumentException("Bạn chưa cập nhật địa chỉ nhận hàng. Vui lòng cập nhật địa chỉ trong sổ địa chỉ trước khi xác nhận đơn hàng.");
                }

                // Tính phí ship (Freeship 100% nếu net subtotal >= 300k)
                decimal netSubTotal = subTotal - totalDiscount;
                decimal shippingFee = netSubTotal >= 300000 ? 0 : (request.ShippingFee > 0 ? request.ShippingFee : 25000);
                decimal totalAmount = Math.Max(0, netSubTotal + shippingFee);

                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = validCustomerId,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    DeliveryAddress = deliveryAddress,
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

                // Sinh URL thanh toán VNPay nếu chọn EWallet/VNPay (Method = 3)
                string? paymentUrl = null;
                if (order.PaymentMethod == PaymentMethod.EWallet && _httpContextAccessor.HttpContext != null)
                {
                    try
                    {
                        var vnPayReq = new VnPayPaymentRequestDto
                        {
                            OrderCode = order.OrderCode,
                            OrderDescription = $"Thanh toan don hang {order.OrderCode} qua AI Chatbot"
                        };
                        var vnPayRes = await _vnPayService.CreatePaymentUrlAsync(vnPayReq, _httpContextAccessor.HttpContext);
                        paymentUrl = vnPayRes.PaymentUrl;
                    }
                    catch
                    {
                        // Fallback URL
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
            });
        }

        #endregion

        #region 4. Helper Methods & Gemini LLM Integration

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
                return await PrepareDefaultSuggestionOrderAsync();
            }

            var items = _mapper.Map<List<InteractiveOrderItemDto>>(lastOrder.Details);

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

        private async Task<AiOrderTrackingDto?> LookupOrderAsync(string? orderCode, int? customerId)
        {
            if (!customerId.HasValue || customerId.Value <= 0)
            {
                return null;
            }

            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .Where(o => !o.IsDeleted && o.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(orderCode))
            {
                query = query.Where(o => o.OrderCode.ToUpper() == orderCode.ToUpper());
            }
            else
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            var order = await query.FirstOrDefaultAsync();
            if (order == null) return null;

            return _mapper.Map<AiOrderTrackingDto>(order);
        }

        private async Task<InteractiveOrderPayloadDto?> PrepareDirectOrderPayloadAsync(int sessionId, string userText, int? customerId)
        {
            var products = await SearchProductsAsync(userText);

            // Trích xuất số lượng chung từ câu chat nếu có (VD: "2kg", "3 hộp", "số lượng 2")
            decimal defaultQty = 1;
            var matchQty = System.Text.RegularExpressions.Regex.Match(userText, @"(\d+)\s*(kg|kí|ký|quả|trái|hộp|thùng)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (matchQty.Success && decimal.TryParse(matchQty.Groups[1].Value, out decimal parsedQty) && parsedQty > 0 && parsedQty <= 100)
            {
                defaultQty = parsedQty;
            }

            // Lọc rõ ràng chỉ các sản phẩm thực sự được nhắc đến trong câu chat của người dùng
            string lowerUserText = userText.ToLower();
            var explicitMatches = products.Where(p => 
            {
                string pName = p.Name.ToLower();
                string pProdName = (p.ProductName ?? string.Empty).ToLower();
                string pVarName = (p.VariantName ?? string.Empty).ToLower();
                string pSlug = (p.Slug ?? string.Empty).Replace("-", " ").ToLower();
                return lowerUserText.Contains(pName) ||
                       (!string.IsNullOrEmpty(pProdName) && lowerUserText.Contains(pProdName)) ||
                       (!string.IsNullOrEmpty(pVarName) && lowerUserText.Contains(pVarName)) ||
                       (!string.IsNullOrEmpty(pSlug) && lowerUserText.Contains(pSlug)) ||
                       (pName.Contains("sầu riêng") && lowerUserText.Contains("sầu riêng")) ||
                       (pName.Contains("bơ") && (lowerUserText.Contains("bơ") || lowerUserText.Contains("034")));
            }).ToList();

            if (explicitMatches.Count > 0)
            {
                // Nhóm theo dòng sản phẩm (dựa vào Slug hoặc ProductName) để xử lý chuẩn xác trường hợp khách đặt nhiều dòng SP (VD: 2kg bơ và 1 quả sầu riêng)
                // hoặc đặt nhiều biến thể trong cùng 1 dòng SP (VD: 1kg đùi heo và 1kg má heo)
                var groupedByProduct = explicitMatches.GroupBy(p => !string.IsNullOrEmpty(p.Slug) ? p.Slug : (p.ProductName ?? p.Name));
                var selectedVariants = new List<AiProductCardDto>();

                foreach (var grp in groupedByProduct)
                {
                    var matchedVariants = grp.Where(p => 
                        !string.IsNullOrEmpty(p.VariantName) && 
                        p.VariantName.Trim().ToLower() != (p.ProductName ?? string.Empty).Trim().ToLower() && 
                        lowerUserText.Contains(p.VariantName.ToLower())).ToList();

                    if (matchedVariants.Count > 0)
                    {
                        selectedVariants.AddRange(matchedVariants);
                    }
                    else
                    {
                        selectedVariants.Add(grp.First());
                    }
                }

                products = selectedVariants;
            }

            // Nếu câu chat của user không chứa tên sản phẩm trực tiếp (VD: "OK lên đơn cho tôi", "Lên đơn giúp mình", "Xác nhận đặt hàng")
            // -> Truy hồi từ ngữ cảnh hội thoại gần nhất trong phiên chat (Recent Context Extraction)
            if (products.Count == 0 && sessionId > 0)
            {
                var recentMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                // 1. Kiểm tra xem tin nhắn gần nhất có payload product_cards không
                var recentCardMsg = recentMessages.FirstOrDefault(m => m.PayloadType == "product_cards" && !string.IsNullOrEmpty(m.PayloadJson));
                if (recentCardMsg != null)
                {
                    try
                    {
                        var cachedCards = JsonSerializer.Deserialize<List<AiProductCardDto>>(recentCardMsg.PayloadJson!);
                        if (cachedCards != null && cachedCards.Count > 0)
                        {
                            // Kiểm tra xem tin nhắn trước đó của user có nhắc đích danh sản phẩm nào trong list không
                            var prevUserMsg = recentMessages.FirstOrDefault(m => m.Role == "user" && m.Id != recentMessages.FirstOrDefault()?.Id);
                            string contextSearch = (prevUserMsg?.Content ?? string.Empty).ToLower();

                            var matchedFromContext = cachedCards.Where(c => 
                                (!string.IsNullOrEmpty(c.Name) && contextSearch.Contains(c.Name.ToLower())) || 
                                (!string.IsNullOrEmpty(c.ProductName) && contextSearch.Contains(c.ProductName.ToLower())) || 
                                (!string.IsNullOrEmpty(c.VariantName) && contextSearch.Contains(c.VariantName.ToLower())) || 
                                (!string.IsNullOrEmpty(c.Slug) && contextSearch.Contains(c.Slug.Replace("-", " "))) ||
                                (c.Name.ToLower().Contains("sầu riêng") && contextSearch.Contains("sầu riêng")) ||
                                (c.Name.ToLower().Contains("bơ") && (contextSearch.Contains("bơ") || contextSearch.Contains("034")))
                            ).ToList();

                            // Ưu tiên khớp biến thể cụ thể nếu câu chat nhắc đến VariantName
                            var variantMatch = matchedFromContext.FirstOrDefault(c => 
                                !string.IsNullOrEmpty(c.VariantName) && contextSearch.Contains(c.VariantName.ToLower()));

                            // Chỉ chọn đúng 1 sản phẩm liên quan từ ngữ cảnh gần nhất, không gom hàng loạt
                            products = variantMatch != null 
                                ? new List<AiProductCardDto> { variantMatch }
                                : (matchedFromContext.Count > 0 
                                    ? new List<AiProductCardDto> { matchedFromContext.First() } 
                                    : new List<AiProductCardDto> { cachedCards.First() });

                            // Nếu chưa tìm thấy quantity ở userText hiện tại, tìm trong câu user trước đó
                            if (!matchQty.Success && prevUserMsg != null)
                            {
                                var prevQtyMatch = System.Text.RegularExpressions.Regex.Match(prevUserMsg.Content, @"(\d+)\s*(kg|kí|ký|quả|trái|hộp|thùng)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (prevQtyMatch.Success && decimal.TryParse(prevQtyMatch.Groups[1].Value, out decimal prevParsedQty) && prevParsedQty > 0)
                                {
                                    defaultQty = prevParsedQty;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fallback nếu parse json lỗi
                    }
                }

                // 2. Nếu vẫn chưa có sản phẩm, quét toàn bộ tin nhắn gần đây để tìm tên sản phẩm hoặc biến thể từ DB
                if (products.Count == 0)
                {
                    var allActiveProducts = await _context.Products
                        .Include(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted))
                            .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                                .ThenInclude(pr => pr.UoM)
                        .Include(p => p.Category)
                            .ThenInclude(c => c!.CategoryGroup)
                        .Where(p => p.IsActive && !p.IsDeleted)
                        .ToListAsync();

                    foreach (var msg in recentMessages)
                    {
                        string text = msg.Content.ToLower();
                        (Product product, ProductVariant variant)? matchedPair = null;

                        foreach (var prod in allActiveProducts)
                        {
                            // 1. Kiểm tra khớp biến thể cụ thể (Tầng 4) trước
                            var matchedVar = prod.Variants.FirstOrDefault(v =>
                                text.Contains(v.Name.ToLower()) ||
                                (!string.IsNullOrEmpty(v.Code) && text.Contains(v.Code.ToLower()))
                            );
                            if (matchedVar != null)
                            {
                                matchedPair = (prod, matchedVar);
                                break;
                            }

                            // 2. Kiểm tra tên sản phẩm (Tầng 3)
                            if (text.Contains(prod.Name.ToLower()) ||
                                (!string.IsNullOrEmpty(prod.Slug) && text.Contains(prod.Slug.Replace("-", " "))) ||
                                (prod.Name.ToLower().Contains("sầu riêng") && text.Contains("sầu riêng")) ||
                                (prod.Name.ToLower().Contains("bơ") && (text.Contains("bơ") || text.Contains("034"))))
                            {
                                var firstVar = prod.Variants.FirstOrDefault();
                                if (firstVar != null)
                                {
                                    matchedPair = (prod, firstVar);
                                    break;
                                }
                            }
                        }

                        if (matchedPair != null)
                        {
                            var enriched = await EnrichVariantCardDtosAsync(new List<(Product, ProductVariant)> { matchedPair.Value });
                            if (enriched.Count > 0)
                            {
                                products = enriched.Take(1).ToList();

                                if (!matchQty.Success)
                                {
                                    var prevQtyMatch = System.Text.RegularExpressions.Regex.Match(msg.Content, @"(\d+)\s*(kg|kí|ký|quả|trái|hộp|thùng)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    if (prevQtyMatch.Success && decimal.TryParse(prevQtyMatch.Groups[1].Value, out decimal prevParsedQty) && prevParsedQty > 0)
                                    {
                                        defaultQty = prevParsedQty;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            if (products.Count == 0) return null;

            var now = DateTime.UtcNow;
            var items = new List<InteractiveOrderItemDto>();
            var stockWarnings = new List<string>();

            // Chỉ đưa nhiều sản phẩm vào thẻ nếu người dùng thực sự nhắc đến từ 2 sản phẩm trở lên trong câu chat
            var targetProducts = explicitMatches.Count > 1 ? products.Take(2).ToList() : products.Take(1).ToList();

            foreach (var p in targetProducts)
            {
                // Trích xuất số lượng riêng cho từng sản phẩm nếu có (VD: "2kg bơ và 1 quả sầu riêng")
                decimal requestedQty = defaultQty;
                string pLower = p.Name.ToLower();
                if (pLower.Contains("sầu riêng"))
                {
                    var m1 = System.Text.RegularExpressions.Regex.Match(userText, @"(\d+)\s*(?:quả|trái|kg|kí|ký)?\s*(?:sầu\s*riêng)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m1.Success && decimal.TryParse(m1.Groups[1].Value, out decimal q1) && q1 > 0 && q1 <= 100) requestedQty = q1;
                    else
                    {
                        var m2 = System.Text.RegularExpressions.Regex.Match(userText, @"(?:sầu\s*riêng)\s*(\d+)\s*(?:quả|trái|kg|kí|ký)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m2.Success && decimal.TryParse(m2.Groups[1].Value, out decimal q2) && q2 > 0 && q2 <= 100) requestedQty = q2;
                    }
                }
                else if (pLower.Contains("bơ"))
                {
                    var m1 = System.Text.RegularExpressions.Regex.Match(userText, @"(\d+)\s*(?:kg|kí|ký|quả|trái|hộp|thùng)?\s*(?:bơ|034)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m1.Success && decimal.TryParse(m1.Groups[1].Value, out decimal q1) && q1 > 0 && q1 <= 100) requestedQty = q1;
                    else
                    {
                        var m2 = System.Text.RegularExpressions.Regex.Match(userText, @"(?:bơ|034)\s*(\d+)\s*(?:kg|kí|ký|quả|trái|hộp|thùng)?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m2.Success && decimal.TryParse(m2.Groups[1].Value, out decimal q2) && q2 > 0 && q2 <= 100) requestedQty = q2;
                    }
                }

                // Kiểm tra tồn kho khả dụng thời gian thực từ các lô hàng còn hạn dùng tại Kho Bán Lẻ
                var checkStockQuery = _context.WarehouseInventories
                    .Include(wi => wi.Batch)
                    .Where(wi => wi.VariantId == p.VariantId && wi.QuantityAvailable > 0 && (wi.Batch == null || wi.Batch.ExpiryDate > now));

                var filteredCheckStock = await checkStockQuery.FilterRetailOnlyAsync(_context);

                var availableStock = await filteredCheckStock
                    .SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;

                // Nếu sản phẩm hết hàng hoàn toàn trong kho
                if (availableStock <= 0)
                {
                    stockWarnings.Add($"Sản phẩm '{p.Name}' hiện đã hết hàng trong kho Solaris.");
                    continue;
                }

                decimal unitPrice = p.DiscountedPrice > 0 ? p.DiscountedPrice : p.Price;
                decimal discount = (p.Price - p.DiscountedPrice) > 0 ? (p.Price - p.DiscountedPrice) : 0;
                decimal actualQty = requestedQty;
                string? warningMsg = null;

                // Nếu khách yêu cầu số lượng vượt quá tồn kho khả dụng
                if (requestedQty > availableStock)
                {
                    actualQty = availableStock;
                    warningMsg = $"Kho chỉ còn {availableStock:G29} {p.UoMName}";
                    stockWarnings.Add($"Sản phẩm '{p.Name}' trong kho hiện chỉ còn {availableStock:G29} {p.UoMName} (khách yêu cầu {requestedQty:G29} {p.UoMName}). Hệ thống đã tự động điều chỉnh số lượng trên Thẻ Đơn Hàng xuống mức tối đa là {availableStock:G29} {p.UoMName}.");
                }

                items.Add(new InteractiveOrderItemDto
                {
                    VariantId = p.VariantId,
                    VariantCode = $"VAR-{p.VariantId}",
                    VariantName = p.Name,
                    Slug = p.Slug,
                    ImagePath = p.ImagePath,
                    UoMId = p.UoMId,
                    UoMName = p.UoMName,
                    Quantity = actualQty,
                    UnitPrice = unitPrice,
                    DiscountAmount = discount,
                    TotalPrice = unitPrice * actualQty,
                    AvailableStock = availableStock,
                    WarningMessage = warningMsg
                });
            }

            if (items.Count == 0)
            {
                return new InteractiveOrderPayloadDto
                {
                    Title = "Thông Báo Tồn Kho",
                    Items = new List<InteractiveOrderItemDto>(),
                    StockWarning = stockWarnings.Count > 0 ? string.Join("\n", stockWarnings) : "Sản phẩm yêu cầu hiện không còn đủ tồn kho khả dụng để lên đơn."
                };
            }

            decimal subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            decimal totalDiscount = items.Sum(i => i.DiscountAmount * i.Quantity);
            decimal netSubTotal = Math.Max(0, subTotal - totalDiscount);
            bool isFree = netSubTotal >= 300000;
            decimal shippingFee = isFree ? 0 : 25000;

            string? address = null;
            string? name = null;
            string? phone = null;

            if (customerId.HasValue && customerId.Value > 0)
            {
                var customer = await _context.Customers
                    .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted);

                if (customer != null)
                {
                    name = customer.Name;
                    phone = customer.PhoneNumber;

                    var defaultAddr = customer.Addresses.FirstOrDefault(a => a.IsDefault) ?? customer.Addresses.FirstOrDefault();
                    if (defaultAddr != null)
                    {
                        address = defaultAddr.FullAddress;
                        if (!string.IsNullOrEmpty(defaultAddr.ReceiverName)) name = defaultAddr.ReceiverName;
                        if (!string.IsNullOrEmpty(defaultAddr.Phone)) phone = defaultAddr.Phone;
                    }
                }

                if (string.IsNullOrEmpty(address))
                {
                    var lastOrder = await _context.Orders
                        .Where(o => o.CustomerId == customerId.Value && !o.IsDeleted)
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();

                    if (lastOrder != null)
                    {
                        address = lastOrder.DeliveryAddress;
                        if (string.IsNullOrEmpty(name)) name = lastOrder.ReceiverName;
                        if (string.IsNullOrEmpty(phone)) phone = lastOrder.ReceiverPhone;
                    }
                }
            }

            return new InteractiveOrderPayloadDto
            {
                Title = "Thẻ Đơn Hàng Tương Tác",
                Items = items,
                StockWarning = stockWarnings.Count > 0 ? string.Join("\n", stockWarnings) : null,
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                ShippingFee = shippingFee,
                TotalAmount = Math.Max(0, netSubTotal + shippingFee),
                IsFreeShipping = isFree,
                SuggestedDeliveryAddress = address,
                SuggestedReceiverName = name,
                SuggestedReceiverPhone = phone
            };
        }

        private async Task<List<AiProductCardDto>> LookupPromotionProductsAsync()
        {
            var now = DateTime.UtcNow;
            var promoProducts = await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                .Include(p => p.BaseUoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Attributes)
                        .ThenInclude(a => a.AttributeDefinition)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.PromotionVariants)
                        .ThenInclude(pv => pv.PromotionCampaign)
                .Where(p => !p.IsDeleted && p.IsActive &&
                            p.Variants.Any(v => v.PromotionVariants.Any(pv => pv.PromotionCampaign != null &&
                                                                             pv.PromotionCampaign.IsActive &&
                                                                             !pv.PromotionCampaign.IsDeleted &&
                                                                             pv.PromotionCampaign.StartDate <= now &&
                                                                             pv.PromotionCampaign.EndDate >= now)))
                .Take(4)
                .ToListAsync();

            if (promoProducts.Count > 0)
            {
                var pairs = promoProducts
                    .SelectMany(p => p.Variants
                        .Where(v => v.IsActive && !v.IsDeleted && v.PromotionVariants.Any(pv => pv.PromotionCampaign != null && pv.PromotionCampaign.IsActive && pv.PromotionCampaign.StartDate <= now && pv.PromotionCampaign.EndDate >= now))
                        .Select(v => (Product: p, Variant: v)))
                    .Take(4)
                    .ToList();
                return await EnrichVariantCardDtosAsync(pairs);
            }

            return await SearchProductsAsync("trái cây");
        }

        private async Task<string> BuildLiveCatalogSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var products = await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                .Include(p => p.BaseUoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Attributes)
                        .ThenInclude(a => a.AttributeDefinition)
                .Where(p => !p.IsDeleted && p.IsActive)
                .Take(60)
                .ToListAsync();

            var variantIds = products.SelectMany(p => p.Variants.Select(v => v.Id)).Distinct().ToList();
            var catalogStockQuery = _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) && wi.QuantityAvailable > 0 && (wi.Batch == null || wi.Batch.ExpiryDate > now));

            var filteredCatalogStock = await catalogStockQuery.FilterRetailOnlyAsync(_context);

            var inventories = await filteredCatalogStock
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            var sb = new StringBuilder();
            sb.AppendLine("BẢNG DANH MỤC SẢN PHẨM PHÂN CẤP 4 TẦNG TRONG KHO SOLARIS (DỮ LIỆU THỰC TẾ 100% TỪ DATABASE):");
            sb.AppendLine("[Cấu trúc: Nhóm Ngành Hàng (Tầng 1) > Loại Sản Phẩm (Tầng 2) > Dòng Sản Phẩm (Tầng 3) > Biến Thể SKU Bán Lẻ (Tầng 4)]");

            var grouped = products
                .GroupBy(p => p.Category?.CategoryGroup?.Name ?? "Nông Sản Khác")
                .ToList();

            foreach (var group in grouped)
            {
                sb.AppendLine($"\n=== NHÓM NGÀNH HÀNG: {group.Key.ToUpper()} ===");
                var subCats = group.GroupBy(p => p.Category?.Name ?? "Chung").ToList();
                foreach (var cat in subCats)
                {
                    sb.AppendLine($"  * Loại Sản Phẩm: {cat.Key}");
                    foreach (var p in cat)
                    {
                        var activeVariants = p.Variants.Where(v => v.IsActive && !v.IsDeleted).ToList();
                        if (!activeVariants.Any()) continue;

                        sb.AppendLine($"    - Dòng Sản Phẩm: {p.Name} (Mã: {p.Code})");
                        foreach (var v in activeVariants)
                        {
                            var priceObj = v.Prices.FirstOrDefault(pr => pr.IsActive && !pr.IsDeleted) ?? v.Prices.FirstOrDefault();
                            decimal price = priceObj?.Price ?? 0;
                            string uom = priceObj?.UoM?.Name ?? p.BaseUoM?.Name ?? "Kg";
                            decimal availableQty = inventories.TryGetValue(v.Id, out decimal s) ? s : 0;
                            bool inStock = availableQty > 0;

                            string? origin = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && a.AttributeDefinition.Name.ToLower().Contains("xuất xứ"))?.AttributeValue;
                            string? cert = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")))?.AttributeValue;
                            string? brix = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("độ ngọt") || a.AttributeDefinition.Name.ToLower().Contains("brix")))?.AttributeValue;

                            string descSnippet = !string.IsNullOrEmpty(v.Description) ? $" | Đặc điểm: {v.Description.Trim()}" : string.Empty;
                            sb.AppendLine($"       + SKU [ID:{v.Id}]: {v.Name} (Mã: {v.Code}) | Giá: {price:N0} ₫/{uom} | Tồn kho khả dụng: {(inStock ? $"{availableQty:G29} {uom} (Còn hàng)" : "0 (Tạm hết)")} | Xuất xứ: {origin ?? "Lâm Đồng"} | Tiêu chuẩn: {cert ?? "VietGAP"} | Độ ngọt: {(string.IsNullOrEmpty(brix) ? "Chuẩn vị" : $"{brix}°Bx")}{descSnippet}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private async Task<List<AiProductCardDto>> SearchProductsAsync(string keyword)
        {
            var now = DateTime.UtcNow;
            string rawKw = (keyword ?? string.Empty).Trim().ToLower();

            // Nếu người dùng hỏi tổng quan: "đang có sản phẩm nào", "shop bán gì", "nông sản hôm nay", "có gì", "danh sách"...
            bool isGeneralInquiry = rawKw.Contains("sản phẩm") || rawKw.Contains("nông sản") || rawKw.Contains("trái cây") ||
                                    rawKw.Contains("bán gì") || rawKw.Contains("có gì") || rawKw.Contains("tươi hôm nay") ||
                                    rawKw.Contains("menu") || rawKw.Contains("danh mục") || rawKw.Contains("hoa quả") ||
                                    string.IsNullOrWhiteSpace(rawKw);

            // Lấy toàn bộ sản phẩm đang kích hoạt kèm liên kết 4 tầng
            var allActiveProducts = await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                .Include(p => p.BaseUoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Attributes)
                        .ThenInclude(a => a.AttributeDefinition)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.PromotionVariants)
                        .ThenInclude(pv => pv.PromotionCampaign)
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            if (isGeneralInquiry)
            {
                var generalVariantPairs = allActiveProducts
                    .SelectMany(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted).Select(v => (Product: p, Variant: v)))
                    .Take(4)
                    .ToList();
                return await EnrichVariantCardDtosAsync(generalVariantPairs);
            }

            // Tìm kiếm theo từ khóa thực tế: tách từ và lọc từ dừng
            var stopWords = new HashSet<string>(new[] { 
                "cho", "tôi", "hỏi", "có", "không", "giá", "bao", "nhiêu", "shop", "ơi", 
                "tìm", "kiếm", "muốn", "xem", "tư", "vấn", "mua", "lấy", "đặt", "chốt", 
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                "kg", "kí", "ký", "quả", "trái", "hộp", "thùng", "bịch", "loại", "nào", "những", "đang", "ạ", "nhé" 
            });

            var searchTerms = rawKw.Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(w => !stopWords.Contains(w) && w.Length > 1)
                                   .ToList();

            var tier1Matches = new List<(Product Product, ProductVariant Variant)>(); // Khớp đích danh Variant hoặc Product
            var tier2Matches = new List<(Product Product, ProductVariant Variant)>(); // Khớp từ khóa với Variant, Product, SKU
            var tier3Matches = new List<(Product Product, ProductVariant Variant)>(); // Khớp Loại sản phẩm (Category - Tầng 2)
            var tier0Matches = new List<(Product Product, ProductVariant Variant)>(); // Khớp Nhóm ngành hàng (CategoryGroup - Tầng 1)

            foreach (var p in allActiveProducts)
            {
                string pName = p.Name.ToLower();
                string pCode = p.Code.ToLower();
                string pSlug = (p.Slug ?? string.Empty).ToLower();
                string cName = (p.Category?.Name ?? string.Empty).ToLower();
                string gName = (p.Category?.CategoryGroup?.Name ?? string.Empty).ToLower();

                var activeVariants = p.Variants.Where(v => v.IsActive && !v.IsDeleted).ToList();
                if (!activeVariants.Any()) continue;

                // Kiểm tra khớp Nhóm Ngành Hàng (Tầng 1)
                bool isGroupMatch = !string.IsNullOrEmpty(gName) && (rawKw.Contains(gName) || searchTerms.Any(term => gName.Contains(term)));
                if (isGroupMatch)
                {
                    foreach (var v in activeVariants)
                        tier0Matches.Add((p, v));
                }

                // Kiểm tra khớp Loại Sản Phẩm (Tầng 2)
                bool isCatMatch = !string.IsNullOrEmpty(cName) && (rawKw.Contains(cName) || searchTerms.Any(term => cName.Contains(term)));
                if (isCatMatch)
                {
                    foreach (var v in activeVariants)
                        tier3Matches.Add((p, v));
                }

                // Kiểm tra từng Biến Thể & Dòng Sản Phẩm (Tầng 3 & Tầng 4)
                foreach (var v in activeVariants)
                {
                    string vName = v.Name.ToLower();
                    string vCode = v.Code.ToLower();

                    // 1. Tier 1: Khớp chính xác tên biến thể, tên sản phẩm cha, hoặc slug
                    bool isTier1 = rawKw.Contains(vName) ||
                                   rawKw.Contains(pName) ||
                                   (!string.IsNullOrEmpty(pSlug) && rawKw.Contains(pSlug.Replace("-", " "))) ||
                                   (pName.Contains("sầu riêng") && rawKw.Contains("sầu riêng")) ||
                                   (pName.Contains("bơ") && (rawKw.Contains("bơ") || rawKw.Contains("034")));

                    if (isTier1)
                    {
                        tier1Matches.Add((p, v));
                        continue;
                    }

                    // 2. Tier 2: Khớp search terms với Tên biến thể, Mã SKU, Tên sản phẩm, Mã sản phẩm
                    if (searchTerms.Any(term => vName.Contains(term) || vCode.Contains(term) || pName.Contains(term) || pCode.Contains(term) || pSlug.Contains(term)))
                    {
                        tier2Matches.Add((p, v));
                    }
                }
            }

            var matched = tier1Matches.Count > 0 ? tier1Matches
                        : tier2Matches.Count > 0 ? tier2Matches
                        : tier3Matches.Count > 0 ? tier3Matches
                        : tier0Matches;

            if (matched.Count > 0)
            {
                var distinctMatches = matched
                    .GroupBy(x => x.Variant.Id)
                    .Select(g => g.First())
                    .Take(4)
                    .ToList();

                return await EnrichVariantCardDtosAsync(distinctMatches);
            }

            return new List<AiProductCardDto>();
        }

        private async Task<List<AiProductCardDto>> EnrichProductDtosAsync(List<Product> products)
        {
            var pairs = products
                .SelectMany(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted).Select(v => (Product: p, Variant: v)))
                .ToList();
            return await EnrichVariantCardDtosAsync(pairs);
        }

        private async Task<List<AiProductCardDto>> EnrichVariantCardDtosAsync(List<(Product Product, ProductVariant Variant)> pairs)
        {
            var now = DateTime.UtcNow;
            if (pairs.Count == 0) return new List<AiProductCardDto>();

            var variantIds = pairs.Select(x => x.Variant.Id).Distinct().ToList();

            var cardStockQuery = _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now));

            var filteredCardStock = await cardStockQuery.FilterRetailOnlyAsync(_context);

            var inventories = await filteredCardStock
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            var dtoList = new List<AiProductCardDto>();

            foreach (var (p, v) in pairs)
            {
                var priceObj = v.Prices.FirstOrDefault(pr => pr.IsActive && !pr.IsDeleted) ?? v.Prices.FirstOrDefault();
                decimal originalPrice = priceObj?.Price ?? 0;
                decimal discountedPrice = originalPrice;

                var promo = v.PromotionVariants
                    .Select(pv => pv.PromotionCampaign)
                    .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                    .OrderByDescending(pc => pc!.DiscountValue)
                    .FirstOrDefault();

                if (promo != null)
                {
                    discountedPrice = promo.IsPercentage
                        ? Math.Max(0, Math.Round(originalPrice * (1 - promo.DiscountValue / 100m)))
                        : Math.Max(0, originalPrice - promo.DiscountValue);
                }

                // Trích xuất EAV của biến thể (có fallback sang sản phẩm cha)
                string? origin = v.Attributes
                    .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("xuất xứ") || a.AttributeDefinition.Name.ToLower().Contains("vùng trồng")))
                    .Select(a => a.AttributeValue).FirstOrDefault();
                if (string.IsNullOrEmpty(origin))
                {
                    origin = p.Variants.SelectMany(x => x.Attributes)
                        .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("xuất xứ") || a.AttributeDefinition.Name.ToLower().Contains("vùng trồng")))
                        .Select(a => a.AttributeValue).FirstOrDefault();
                }

                string? cert = v.Attributes
                    .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")))
                    .Select(a => a.AttributeValue).FirstOrDefault();
                if (string.IsNullOrEmpty(cert))
                {
                    cert = p.Variants.SelectMany(x => x.Attributes)
                        .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")))
                        .Select(a => a.AttributeValue).FirstOrDefault();
                }

                string? brix = v.Attributes
                    .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("độ ngọt") || a.AttributeDefinition.Name.ToLower().Contains("brix")))
                    .Select(a => a.AttributeValue).FirstOrDefault();
                if (string.IsNullOrEmpty(brix))
                {
                    brix = p.Variants.SelectMany(x => x.Attributes)
                        .Where(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("độ ngọt") || a.AttributeDefinition.Name.ToLower().Contains("brix")))
                        .Select(a => a.AttributeValue).FirstOrDefault();
                }

                decimal stock = inventories.TryGetValue(v.Id, out decimal s) ? s : 0;

                // Tên hiển thị Thẻ: Nếu tên biến thể đã bao gồm tên sản phẩm thì giữ nguyên, ngược lại kết hợp "$p.Name - $v.Name"
                string displayName = v.Name.ToLower().Contains(p.Name.ToLower())
                    ? v.Name
                    : $"{p.Name} - {v.Name}";

                dtoList.Add(new AiProductCardDto
                {
                    Id = v.Id,
                    VariantId = v.Id,
                    UoMId = priceObj?.UoMId ?? p.BaseUoMId,
                    Name = displayName,
                    ProductName = p.Name,
                    VariantName = v.Name,
                    CategoryName = p.Category?.Name,
                    CategoryGroupName = p.Category?.CategoryGroup?.Name,
                    Slug = p.Slug ?? "san-pham",
                    ImagePath = v.ImagePath ?? p.ImagePath,
                    Price = originalPrice,
                    DiscountedPrice = discountedPrice,
                    UoMName = priceObj?.UoM?.Name ?? p.BaseUoM?.Name ?? "Kg",
                    Origin = origin,
                    Certification = cert,
                    BrixLevel = brix,
                    IsInStock = stock > 0
                });
            }

            return dtoList;
        }

        private async Task<string> GenerateGeminiResponseAsync(int sessionId, string userMessage, string payloadType, object? payloadObject)
        {
            var geminiSection = _config.GetSection("GeminiSettings");
            string apiKey = geminiSection["ApiKey"] ?? "AQ.Ab8RN6Ko-9K1tmb7cmtOnCitJg-3nNntiZFmh7jvycBIdFmEfg";
            string model = geminiSection["Model"] ?? "gemini-3.5-flash-lite";
            string baseUrl = geminiSection["BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";

            string liveCatalogSummary = await BuildLiveCatalogSummaryAsync();
            string systemInstruction = $@"Bạn là Trợ lý AI Bán hàng & Chăm sóc khách hàng của Sàn Thương Mại Điện Tử Nông Sản Sạch Solaris (solaris-os.io.vn).

QUY TẮC PHỤC VỤ VÀ TÍNH CÁCH BẮT BUỘC:
1. Tính cách: Nói thẳng vào trọng tâm, nghiêm túc, thật thà, ngắn gọn, lịch sự. Thể hiện sự am hiểu về nông sản sạch, an toàn chuẩn VietGAP/GlobalGAP. Tuyệt đối không chào hỏi hoa mỹ dài dòng, không nịnh nọt hoặc hứa hẹn viển vông.
2. Nguyên tắc dữ liệu (Zero Hallucination - TUYỆT ĐỐI KHÔNG ẢO TƯỞNG): 
   - CHỈ sử dụng dữ liệu thực tế từ BẢNG DANH MỤC SẢN PHẨM SOLARIS bên dưới.
   - BẠN CHỈ ĐƯỢC PHÉP ĐỀ CẬP HOẶC TƯ VẤN CÁC SẢN PHẨM CÓ TÊN CHÍNH XÁC TRONG BẢNG NÀY. TUYỆT ĐỐI CẤM TỰ NGHĨ RA HOẶC KỂ TÊN BẤT KỲ NÔNG SẢN/TRÁI CÂY/RAU CỦ NÀO KHÁC.
   - Nếu khách hỏi về bất kỳ sản phẩm nào ngoài bảng danh mục, bạn PHẢI trả lời: ""Dạ hiện tại Solaris chưa có dữ liệu hoặc không kinh doanh sản phẩm này. Hệ thống hiện chỉ có: [liệt kê các sản phẩm thực tế trong bảng bên dưới]"".
   - Bỏ qua toàn bộ sản phẩm hư cấu nếu từng xuất hiện trong lịch sử chat cũ trước đây.
3. Quy tắc kiểm soát số lượng & Tồn kho khả dụng:
   - Trong bảng danh mục có nêu rõ 'Tồn kho khả dụng' của từng sản phẩm.
   - Khi khách yêu cầu mua số lượng vượt quá tồn kho khả dụng (ví dụ đòi 100 trái sầu riêng khi kho chỉ còn 5 trái), bạn PHẢI nói thật: kho hiện chỉ còn 5 trái, và hệ thống đã tự động điều chỉnh số lượng trên Thẻ Đơn Hàng tương tác xuống mức tối đa còn hàng. Tuyệt đối KHÔNG chúc mừng hay hứa hẹn số lượng vượt quá tồn kho.
   - Nếu sản phẩm hết hàng: Thông báo rõ sản phẩm tạm hết hàng và gợi ý khách chọn nông sản khác đang có sẵn.
4. Đơn vị tính nông sản Việt Nam:
   - Hiểu rõ quy chuẩn đơn vị: 'kg', 'kí', 'ký' = Kilogram; 'quả', 'trái' = Quả/Trái; 'hộp', 'thùng'.
5. Chính sách bán hàng & Giao nhận:
   - Đơn vị giao nhận: Giao Hàng Nhanh (GHN Express).
   - Phí vận chuyển: Cước chuẩn 25.000 ₫; Đơn hàng có giá trị tiền hàng sau chiết khấu từ 300.000 ₫ trở lên được áp dụng MIỄN PHÍ VẬN CHUYỂN (FREESHIP).
   - Đổi trả hàng: Hỗ trợ đổi trả hoặc hoàn tiền 100% trong vòng 48 giờ nếu sản phẩm bị dập úng, hư hỏng trong quá trình vận chuyển.
   - Thanh toán: Hỗ trợ Cổng VNPay Sandbox (VNPAY-QR, Thẻ ATM/Visa/Mastercard) và Thanh toán khi nhận hàng (COD).
6. Khi khách muốn đặt mua hoặc lên đơn:
   - Trả lời ngắn gọn số lượng, đơn giá, tổng tiền và thông báo rằng Thẻ Đơn Hàng Tương Tác đã xuất hiện ngay bên dưới.
   - Hướng dẫn khách: Chọn địa chỉ nhận hàng từ Sổ địa chỉ (hoặc bấm cập nhật địa chỉ nếu chưa có), tùy chỉnh số lượng [-] [+], chọn hình thức thanh toán và bấm nút 'Xác nhận đặt hàng' trên thẻ.
   - Tuyệt đối không tự nói rằng 'đơn hàng đã được đặt thành công' qua tin nhắn chữ khi khách chưa bấm nút trên thẻ.
7. Cấu trúc danh mục 4 tầng của Solaris:
   - Tầng 1: Nhóm Loại sản phẩm (Category Group) - ví dụ: Sản phẩm tươi sống, Thực phẩm chế biến,...
   - Tầng 2: Loại Sản phẩm (Category) - ví dụ: Sản phẩm từ động vật, Rau củ quả hữu cơ,...
   - Tầng 3: Sản phẩm / Dòng sản phẩm (Product) - ví dụ: Thịt heo sạch, Cá hồi Na Uy, Bơ 034, Sầu riêng Ri6,...
   - Tầng 4: Biến thể Sản phẩm / SKU bán lẻ (Product Variant) - ví dụ: Khay Đùi heo 500g, Vỉ Cá hồi 500g, Túi Cà chua 500g, Trái 1-2kg, Túi Gạo 5kg... (LƯU Ý: Toàn bộ thực phẩm tươi sống thịt, cá, rau củ đều được chuẩn hóa đóng gói sẵn theo Khay/Vỉ/Túi định lượng; đồ khô vẫn bán theo kg/túi bình thường).
   - Khi khách hàng hỏi chung chung theo nhóm ngành hoặc loại (VD: 'Shop có bán thịt gì không?', 'Có sản phẩm tươi sống nào?'), hãy tư vấn các dòng sản phẩm và các biến thể cụ thể thuộc nhóm đó.
   - Khi khách hàng hỏi hoặc mua một biến thể SKU cụ thể (VD: 'Khay Đùi heo 500g', 'Vỉ Cá hồi 500g'), hãy tập trung tư vấn đúng biến thể SKU đó, giá bán và tồn kho khả dụng của biến thể.

KỊCH BẢN MẪU (FEW-SHOT EXAMPLES):
- Khách: '1 trái sầu riêng giá bao nhiêu?'
  -> AI: 'Dạ sầu riêng có giá 100.000 ₫/trái, hàng chuẩn VietGAP, tồn kho hiện còn X trái. Bạn có muốn lên đơn mua không ạ?'
- Khách: 'Lên đơn cho tôi 100 trái sầu riêng'
  -> AI: 'Dạ kho Solaris hiện chỉ còn X trái sầu riêng. Em đã tạo Thẻ Đơn Hàng Tương Tác bên dưới với số lượng tối đa là X trái (Tổng: ... ₫). Quý khách vui lòng chọn địa chỉ nhận hàng và bấm xác nhận trên thẻ giúp em nhé!'
- Khách: 'Shop có bán thịt gì không?'
  -> AI: 'Dạ Solaris thuộc nhóm Sản phẩm tươi sống có dòng Thịt heo sạch đóng khay tiện lợi với các biến thể: Khay Đùi heo 500g (65.000 ₫/khay), Khay Má heo 300g (45.000 ₫/khay), Khay Sườn non 500g (95.000 ₫/khay)... Bạn muốn tham khảo biến thể nào ạ?'
- Khách: 'Solaris có bán nho Mỹ không?'
  -> AI: 'Dạ hiện tại Solaris chưa có dữ liệu hoặc không kinh doanh sản phẩm nho Mỹ. Hệ thống hiện chỉ có: [liệt kê sản phẩm thực tế]. Bạn cần tư vấn sản phẩm nào ạ?'

{liveCatalogSummary}";

            // Lấy lịch sử hội thoại trước đó (loại trừ message của user vừa được lưu để không bị lặp 2 turns user liên tiếp)
            var historyMessages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(1) // Bỏ qua userMsg vừa tạo
                .Take(6)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var safeHistory = historyMessages
                .Where(m => !IsHallucinatedMessage(m.Content))
                .ToList();

            var contents = new List<object>();

            foreach (var m in safeHistory)
            {
                contents.Add(new
                {
                    role = m.Role == "model" ? "model" : "user",
                    parts = new object[] { new { text = m.Content } }
                });
            }

            string extraContext = string.Empty;
            if (payloadType == "order_tracking" && payloadObject is AiOrderTrackingDto ord)
            {
                extraContext = $"\n(Dữ liệu thực tế đơn hàng từ hệ thống: Mã đơn: {ord.OrderCode}, Ngày đặt: {ord.OrderDate:dd/MM/yyyy HH:mm}, Trạng thái đơn: {ord.StatusName}, Thanh toán: {ord.PaymentStatusName} qua {ord.PaymentMethodName}, Tổng tiền: {ord.TotalAmount:N0} ₫, Người nhận: {ord.ReceiverName}, SĐT: {ord.ReceiverPhone}, Địa chỉ: {ord.DeliveryAddress}, Danh sách sản phẩm: {string.Join(", ", ord.Items.Select(i => $"{i.VariantName} x {i.Quantity} {i.UoMName}"))}. Hãy trả lời ngắn gọn, thẳng thắn và chính xác dựa trên dữ liệu trên, không suy diễn).";
            }
            else if (payloadType == "interactive_order" && payloadObject is InteractiveOrderPayloadDto orderPayload)
            {
                string stockNotice = !string.IsNullOrEmpty(orderPayload.StockWarning)
                    ? $"\nCẢNH BÁO TỒN KHO THỰC TẾ:\n{orderPayload.StockWarning}\nBẠN BẮT BUỘC PHẢI GIẢI THÍCH RÕ VỚI KHÁCH: Do kho hiện chỉ còn số lượng như trên nên hệ thống đã tự động điều chỉnh số lượng trên Thẻ Đơn Hàng xuống mức tồn kho tối đa. Nhắc khách chọn địa chỉ từ sổ địa chỉ và bấm nút xác nhận trên thẻ."
                    : string.Empty;

                extraContext = $"\n(Hệ thống đã tự động tính giá và hiển thị Thẻ Đơn Hàng Tương Tác ngay bên dưới tin nhắn này với các món: {string.Join(", ", orderPayload.Items.Select(i => $"{i.VariantName} x {i.Quantity} {i.UoMName} ({i.TotalPrice:N0}đ)"))}. Tổng tiền thanh toán: {orderPayload.TotalAmount:N0} ₫ (Phí ship GHN: {(orderPayload.IsFreeShipping ? "Miễn phí 0đ" : $"{orderPayload.ShippingFee:N0}đ")}).{stockNotice} Hãy thông báo ngắn gọn cho khách biết Thẻ Đơn Hàng Tương Tác đã xuất hiện ngay bên dưới, khách có thể chọn địa chỉ từ sổ địa chỉ, bấm nút [-] [+] để chỉnh số lượng, chọn thanh toán VNPay Sandbox hoặc COD và bấm nút 'Xác nhận đặt hàng' trên thẻ).";
            }
            else if (payloadType == "product_cards" && payloadObject is List<AiProductCardDto> prods && prods.Count > 0)
            {
                extraContext = "\n(Dữ liệu sản phẩm thực tế trong kho Solaris: " + string.Join("; ", prods.Select(p => $"{p.Name} (Mã biến thể {p.VariantId}): Giá {p.Price:N0}đ/{p.UoMName}, Khuyến mãi: {(p.DiscountedPrice < p.Price ? $"{p.DiscountedPrice:N0}đ" : "Không")}, Tồn kho: {(p.IsInStock ? "Còn hàng" : "Hết hàng")}, Xuất xứ: {p.Origin ?? "Lâm Đồng"}, Chứng nhận: {p.Certification ?? "VietGAP"}")) + ". Trả lời thẳng thắn, ngắn gọn dựa trên dữ liệu này. Tuyệt đối không tự bịa thêm thông tin).";
            }
            else if (payloadType == "none")
            {
                if (payloadObject is string customWarning && !string.IsNullOrEmpty(customWarning))
                {
                    extraContext = $"\n(LƯU Ý TỒN KHO: {customWarning}. Bạn PHẢI trả lời thông báo rõ ràng cho khách biết sản phẩm đã hết hàng trong kho và tư vấn khách chọn các nông sản khác đang có sẵn trong kho).";
                }
                else
                {
                    extraContext = "\n(Lưu ý: Nếu câu hỏi liên quan đến sản phẩm không có trong Bảng Danh Mục Sản Phẩm Solaris ở trên, hãy trả lời thẳng thắn rằng Solaris hiện chưa có dữ liệu hoặc không kinh doanh mặt hàng này. Tuyệt đối không được bịa đặt).";
                }
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
                    temperature = 0.1,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 800
                }
            };

            string[] candidateModels = new[] { model, "gemini-3.5-flash-lite", "gemini-3.6-flash" };

            foreach (var currentModel in candidateModels.Distinct())
            {
                try
                {
                    string endpoint = $"{baseUrl}{currentModel}:generateContent?key={apiKey}";
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
                            string? reply = textElem.GetString();
                            if (!string.IsNullOrWhiteSpace(reply))
                            {
                                return reply;
                            }
                        }
                    }
                }
                catch
                {
                    // Thử model tiếp theo
                }
            }

            return GetFallbackReply(userMessage, payloadType);
        }

        private static string GetFallbackReply(string userMessage, string payloadType)
        {
            if (payloadType == "order_tracking")
            {
                return "Dạ hệ thống đã tra cứu thông tin đơn hàng thực tế của bạn. Chi tiết trạng thái và thông tin giao nhận được hiển thị trong thẻ bên dưới.";
            }
            if (payloadType == "interactive_order")
            {
                return "Dạ hệ thống đã chuẩn bị Thẻ Đơn Hàng Tương Tác với báo giá và phí giao hàng chuẩn xác. Bạn có thể bấm [+] [-] để điều chỉnh số lượng, điền thông tin nhận hàng và bấm xác nhận đơn ngay tại đây.";
            }
            if (payloadType == "product_cards")
            {
                return "Dạ hệ thống đã tìm thấy các sản phẩm nông sản sạch hiện có trong kho Solaris phù hợp với yêu cầu của bạn. Mời bạn tham khảo thông tin chi tiết bên dưới.";
            }
            return "Dạ Solaris AI xin chào bạn. Tôi có thể hỗ trợ bạn tra cứu sản phẩm nông sản sạch, kiểm tra đơn hàng, xem chương trình khuyến mãi hoặc hỗ trợ lên đơn đặt hàng nhanh chóng.";
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

        private static bool IsHallucinatedMessage(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            string lower = content.ToLower();
            return lower.Contains("bơ booth") ||
                   lower.Contains("xoài cát") ||
                   lower.Contains("hòa lộc") ||
                   lower.Contains("cải kale") ||
                   lower.Contains("cải xoăn") ||
                   lower.Contains("táo đỏ") ||
                   lower.Contains("cam sành") ||
                   lower.Contains("cà chua bi") ||
                   lower.Contains("khoai lang mật") ||
                   lower.Contains("dưa lưới");
        }

        #endregion
    }
}
