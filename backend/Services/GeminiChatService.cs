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
using System.Text.RegularExpressions;
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
        private decimal GetStandardShippingFee()
        {
            return _config.GetValue<decimal>("ShippingSettings:StandardFee", 25000m);
        }

        private decimal GetFreeShippingThreshold()
        {
            return _config.GetValue<decimal>("ShippingSettings:FreeShippingThreshold", 300000m);
        }

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVnPayService _vnPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUoMConversionService? _uomConversionService;
        private readonly IOrderRoutingService? _routingService;

        public GeminiChatService(
            HttpClient httpClient,
            IConfiguration config,
            SolarisDbContext context,
            IMapper mapper,
            IVnPayService vnPayService,
            IHttpContextAccessor httpContextAccessor,
            IUoMConversionService? uomConversionService = null,
            IOrderRoutingService? routingService = null)
        {
            _httpClient = httpClient;
            _config = config;
            _context = context;
            _mapper = mapper;
            _vnPayService = vnPayService;
            _httpContextAccessor = httpContextAccessor;
            _uomConversionService = uomConversionService;
            _routingService = routingService;
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

                // Thử dispatch qua Gemini Native Tool Calling (nếu AI trả về functionCall trực tiếp)
                var (geminiToolHandled, geminiPayloadType, geminiPayloadObject) = await TryDispatchGeminiToolsAsync(session, userText, session.CustomerId ?? customerId);
                if (geminiToolHandled)
                {
                    payloadType = geminiPayloadType;
                    payloadObject = geminiPayloadObject;
                    if (payloadObject != null && payloadType != "none")
                    {
                        payloadJson = JsonSerializer.Serialize(payloadObject);
                    }
                }
                else
                {
                    // Fallback sang Engine Deterministic chuẩn xác từ Database
                    // A. Tra cứu đơn hàng (Order Tracking)
                var orderCodeMatch = System.Text.RegularExpressions.Regex.Match(userText, @"ORD-[A-Za-z0-9\-]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                bool isOrderTrackingInquiry = orderCodeMatch.Success ||
                    lowerText.Contains("kiểm tra đơn") || lowerText.Contains("tra cứu đơn") ||
                    lowerText.Contains("đơn hàng của tôi") || lowerText.Contains("tình trạng đơn") ||
                    lowerText.Contains("đơn của tôi") || lowerText.Contains("đơn hàng ở đâu") ||
                    lowerText.Contains("thanh toán thành công") || lowerText.Contains("vừa thanh toán") ||
                    lowerText.Contains("trạng thái đơn") || lowerText.Contains("kiểm tra trạng thái") ||
                    lowerText.Contains("theo dõi đơn") || lowerText.Contains("xem đơn") ||
                    lowerText.Contains("đơn đến đâu") || lowerText.Contains("vận chuyển đến đâu") ||
                    ((lowerText.Contains("kiểm tra") || lowerText.Contains("tra cứu") || lowerText.Contains("theo dõi") || lowerText.Contains("trạng thái") || lowerText.Contains("tình trạng")) && (lowerText.Contains("đơn") || lowerText.Contains("order")));

                if (isOrderTrackingInquiry)
                {
                    string? specificCode = orderCodeMatch.Success ? orderCodeMatch.Value.ToUpper() : null;
                    var orderDto = await LookupOrderAsync(specificCode, session.CustomerId ?? customerId);
                    if (orderDto != null)
                    {
                        payloadType = "order_tracking";
                        payloadObject = orderDto;
                        payloadJson = JsonSerializer.Serialize(orderDto);
                    }
                    else
                    {
                        payloadType = "none";
                        payloadObject = "(HỆ THỐNG KHÔNG TÌM THẤY ĐƠN HÀNG NÀO: Khách hàng đang hỏi tra cứu hoặc kiểm tra trạng thái đơn hàng nhưng hệ thống chưa ghi nhận đơn hàng tương ứng. Bạn PHẢI giải thích lịch sự: Quý khách vui lòng cung cấp đúng Mã đơn hàng dạng ORD-... hoặc đăng nhập đúng tài khoản đã đặt hàng để hệ thống hỗ trợ tra cứu tiến trình giao xe lạnh TMS ngay).";
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
                // C. Lên đơn mua hàng trực tiếp & Quản lý Giỏ hàng hội thoại Draft Order (Direct Conversational Order)
                else if (!((lowerText.Contains("tư vấn") || lowerText.Contains("hướng dẫn") || lowerText.Contains("giải thích") || lowerText.Contains("giới thiệu") || lowerText.Contains("cho tôi xem") || lowerText.Contains("cho mình xem")) && !lowerText.Contains("lên đơn") && !lowerText.Contains("chốt đơn") && !lowerText.Contains("đặt mua") && !lowerText.Contains("tạo đơn")) &&
                         (lowerText.Contains("lên đơn") || lowerText.Contains("chốt đơn") || lowerText.Contains("tôi muốn mua") || lowerText.Contains("đặt mua") ||
                          lowerText.Contains("đặt hàng") || lowerText.Contains("xác nhận đặt") || lowerText.Contains("xác nhận đơn") || lowerText.Contains("chốt mua") ||
                          lowerText.Contains("mua hàng") || lowerText.Contains("tạo đơn") || lowerText.StartsWith("mua ") || lowerText.Contains(" mua ") ||
                          lowerText.Contains("lấy cho tôi") || lowerText.Contains("cho tôi") || lowerText.Contains("cho mình") || lowerText.Contains("lấy giúp") ||
                          lowerText.Contains("bán cho tôi") || lowerText.Contains("bán tôi") || lowerText.Contains("bán cho") ||
                          lowerText.Contains("thêm") || lowerText.Contains("cho thêm") || lowerText.Contains("bỏ ") || lowerText.Contains("xóa ") || lowerText.Contains("hủy giỏ") || lowerText.Contains("xóa giỏ") || lowerText.Contains("hủy đơn") ||
                          System.Text.RegularExpressions.Regex.IsMatch(lowerText, @"\b(cho|lấy|đặt|mua|bán|giao|ship|thêm|bỏ|xóa|cần)\s+(\d+|một|hai|ba|bốn|năm|sáu|bảy|tám|chín|mười)\b") ||
                          System.Text.RegularExpressions.Regex.IsMatch(lowerText, @"\b(cho|lấy|đặt|mua|bán|giao|ship|thêm|bỏ|xóa|cần)\s+(\d+)\s*(kg|kí|ký|kilo|kí\s*lô|kí\s*lô\s*gam|kilogram|gói|hộp|thùng|quả|trái|bịch|túi|vỉ|chai|lon)\b")))
                {
                    string orderAction = "add";
                    if (lowerText.Contains("xóa hết") || lowerText.Contains("hủy đơn") || lowerText.Contains("hủy giỏ") || lowerText.Contains("xóa giỏ"))
                    {
                        orderAction = "clear";
                    }
                    else if (lowerText.Contains("xóa") || lowerText.Contains("bỏ") || lowerText.Contains("bớt"))
                    {
                        orderAction = "remove";
                    }
                    else if (lowerText.Contains("sửa") || lowerText.Contains("đổi thành") || lowerText.Contains("thay đổi"))
                    {
                        orderAction = "update";
                    }

                    var extractedItems = await ExtractOrderItemsFromTextAsync(userText, session.Id);
                    if (extractedItems.Count > 0 || orderAction == "clear")
                    {
                        var toolArgs = new ManageDraftOrderToolArgs
                        {
                            Action = orderAction,
                            Items = extractedItems
                        };
                        var directOrderPayload = await ExecuteManageDraftOrderAsync(session, toolArgs, session.CustomerId ?? customerId);
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
                            var products = await SearchProductsAsync(userText);
                            if (products.Count > 0)
                            {
                                payloadType = "product_cards";
                                payloadObject = products;
                                payloadJson = JsonSerializer.Serialize(products);
                            }
                        }
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

                // Multi-layer fallback: Tự động trích xuất danh tính khách hàng nếu tham số customerId bị khuyết hoặc chưa đồng bộ từ claim
                if (!customerId.HasValue || customerId.Value <= 0)
                {
                    // Fallback 1: Trích xuất từ phiên hội thoại chat hiện tại (ChatSession)
                    if (request.SessionId > 0)
                    {
                        var currentSession = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId);
                        if (currentSession != null && currentSession.CustomerId.HasValue && currentSession.CustomerId.Value > 0)
                        {
                            customerId = currentSession.CustomerId.Value;
                        }
                    }

                    // Fallback 2: Trích xuất từ sổ địa chỉ khách hàng đã chọn (CustomerAddress)
                    if ((!customerId.HasValue || customerId.Value <= 0) && request.CustomerAddressId.HasValue && request.CustomerAddressId.Value > 0)
                    {
                        var addr = await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == request.CustomerAddressId.Value && !a.IsDeleted);
                        if (addr != null && addr.CustomerId > 0)
                        {
                            customerId = addr.CustomerId;
                        }
                    }

                    // Fallback 3: Khớp số điện thoại người nhận với tài khoản khách hàng thực tế (Customers)
                    if ((!customerId.HasValue || customerId.Value <= 0) && !string.IsNullOrWhiteSpace(request.ReceiverPhone))
                    {
                        string phone = request.ReceiverPhone.Trim();
                        var customerByPhone = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phone && !c.IsDeleted);
                        if (customerByPhone != null && customerByPhone.Id > 0)
                        {
                            customerId = customerByPhone.Id;
                        }
                    }
                }

                if (!customerId.HasValue || customerId.Value <= 0)
                {
                    throw new UnauthorizedAccessException("Vui lòng đăng nhập tài khoản để xác nhận tạo đơn hàng.");
                }

                int validCustomerId = customerId.Value;
                var now = DateTime.UtcNow;
                string dateStr = DateTimeHelper.VietnamDateString;
                string randStr = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                string orderCode = $"ORD-{dateStr}-{randStr}";

                var systemUser = await _context.IAUsers.FirstOrDefaultAsync(u => u.IsActive && !u.IsDeleted)
                                 ?? await _context.IAUsers.FirstOrDefaultAsync();
                int systemUserId = systemUser?.Id ?? 2;

                decimal subTotal = 0;
                decimal totalDiscount = 0;
                var orderDetails = new List<OrderDetail>();
                int? assignedWarehouseId = null;

                foreach (var item in request.Items)
                {
                    var variant = await _context.ProductVariants
                        .Include(v => v.Product)
                            .ThenInclude(p => p!.BaseUoM)
                        .FirstOrDefaultAsync(v => v.Id == item.VariantId && !v.IsDeleted);

                    if (variant == null) continue;

                    int itemUoMId = item.UoMId > 0 ? item.UoMId : (variant.Product?.BaseUoMId ?? 1);
                    decimal baseQty = item.Quantity;
                    if (_uomConversionService != null)
                    {
                        baseQty = await _uomConversionService.ConvertToBaseQuantityAsync(variant.Id, itemUoMId, item.Quantity);
                    }

                    // Kiểm tra tồn kho khả dụng thời gian thực từ Kho Bán Lẻ
                    var stockQuery = _context.WarehouseInventories
                        .Include(wi => wi.Batch)
                        .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && (wi.BatchId == 0 || wi.Batch == null || wi.Batch.ExpiryDate > now));

                    var filteredStockQuery = await stockQuery.FilterRetailOnlyAsync(_context);

                    var availableStock = await filteredStockQuery
                        .SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;

                    if (availableStock < baseQty)
                    {
                        throw new InvalidOperationException($"Sản phẩm '{variant.Name}' hiện chỉ còn tồn kho {availableStock:G29} {variant.Product?.BaseUoM?.Name ?? "đơn vị"}. Vui lòng giảm bớt số lượng đặt mua.");
                    }

                    // Giữ chỗ tồn kho khả dụng (Reserve)
                    var invQuery = _context.WarehouseInventories
                        .Include(wi => wi.Batch)
                        .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable >= baseQty && (wi.BatchId == 0 || wi.Batch == null || wi.Batch.ExpiryDate > now));
                    var filteredInv = await invQuery.FilterRetailOnlyAsync(_context);
                    var inventory = await filteredInv
                        .OrderBy(wi => wi.Batch != null ? wi.Batch.ExpiryDate : DateTime.MaxValue)
                        .FirstOrDefaultAsync();

                    if (inventory == null)
                    {
                        var anyStockQuery = _context.WarehouseInventories
                            .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable >= baseQty);
                        var filteredAny = await anyStockQuery.FilterRetailOnlyAsync(_context);
                        inventory = await filteredAny.FirstOrDefaultAsync();
                    }

                    if (inventory != null)
                    {
                        inventory.QuantityAvailable -= baseQty;
                        inventory.QuantityReserved += baseQty;
                        inventory.UpdatedAt = now;

                        if (assignedWarehouseId == null)
                        {
                            assignedWarehouseId = inventory.WarehouseId;
                        }

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionCode = $"TX-{now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpperInvariant(),
                            Type = TransactionType.Reserve,
                            WarehouseId = inventory.WarehouseId,
                            VariantId = variant.Id,
                            BatchId = inventory.BatchId,
                            Quantity = baseQty,
                            ReferenceCode = orderCode,
                            Note = $"Khách hàng đặt hàng qua AI Chatbot ({item.Quantity} ĐVT -> {baseQty} Base UoM, Giữ hàng tại kho #{inventory.WarehouseId})",
                            CreatedById = systemUserId,
                            CreatedAt = now
                        });
                    }

                    decimal itemTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
                    subTotal += (item.Quantity * item.UnitPrice);
                    totalDiscount += item.DiscountAmount;

                    orderDetails.Add(new OrderDetail
                    {
                        VariantId = variant.Id,
                        UoMId = itemUoMId,
                        Quantity = item.Quantity,
                        BaseQuantity = baseQty,
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
                if (request.CustomerAddressId.HasValue && request.CustomerAddressId.Value >= 0)
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

                CustomerAddress? currentCustAddr = null;
                if (request.CustomerAddressId.HasValue && request.CustomerAddressId.Value >= 0)
                {
                    currentCustAddr = await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == request.CustomerAddressId.Value);
                }
                else
                {
                    currentCustAddr = await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.CustomerId == validCustomerId && a.IsDefault && !a.IsDeleted)
                        ?? await _context.CustomerAddresses.FirstOrDefaultAsync(a => a.CustomerId == validCustomerId && !a.IsDeleted);
                }

                // 2. Chạy Smart Order Routing (Thuật toán Haversine) để tự động định vị Kho đích tối ưu gần khách hàng nhất
                double deliveryDistanceKm = 0;
                string selectedWarehouseName = string.Empty;

                if (_routingService != null)
                {
                    var routingItems = orderDetails.Select(d => new backend.DTOs.OrderDTOs.OrderDetailCreateDto
                    {
                        VariantId = d.VariantId,
                        Quantity = d.Quantity
                    }).ToList();

                    var routing = await _routingService.DetermineOptimalWarehouseAsync(
                        request.CustomerAddressId,
                        routingItems,
                        currentCustAddr?.Latitude ?? 0,
                        currentCustAddr?.Longitude ?? 0,
                        currentCustAddr?.Province,
                        currentCustAddr?.District);

                    if (routing.OptimalWarehouseId > 0)
                    {
                        assignedWarehouseId = routing.OptimalWarehouseId;
                        deliveryDistanceKm = routing.DistanceKm;
                        selectedWarehouseName = routing.WarehouseName;
                    }
                }

                // 3. Kiểm tra rào chắn khoảng cách chuỗi lạnh nếu đơn có sản phẩm tươi sống
                var orderedVariantIds = orderDetails.Select(d => d.VariantId).ToList();
                var coldVariantsInOrder = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p!.Category)
                            .ThenInclude(c => c!.CategoryGroup)
                    .Where(v => orderedVariantIds.Contains(v.Id))
                    .ToListAsync();

                var coldItemNames = coldVariantsInOrder.Where(v =>
                    v.Product?.Category?.RequiresColdChain == true ||
                    (v.Product?.Category?.CategoryGroup?.Code == "FRESH_PRODUCE") ||
                    (v.Product?.Category?.Name?.ToLower().Contains("tươi") == true) ||
                    (v.Product?.Category?.Name?.ToLower().Contains("thịt") == true) ||
                    (v.Product?.Category?.Name?.ToLower().Contains("cá") == true)
                ).Select(v => v.Name).ToList();

                if (coldItemNames.Count > 0)
                {
                    var targetWh = await _context.Warehouses
                        .Include(w => w.Address)
                        .FirstOrDefaultAsync(w => w.Id == (assignedWarehouseId ?? 1) && w.IsActive && !w.IsDeleted);

                    if (targetWh?.Address != null)
                    {
                        double dist = deliveryDistanceKm > 0 ? deliveryDistanceKm : 0;
                        if (dist <= 0 && currentCustAddr != null && currentCustAddr.Latitude != 0 && currentCustAddr.Longitude != 0 &&
                            targetWh.Address.Latitude != 0 && targetWh.Address.Longitude != 0)
                        {
                            var distService = new DistanceService();
                            dist = distService.CalculateDistanceKm(currentCustAddr.Latitude, currentCustAddr.Longitude, targetWh.Address.Latitude, targetWh.Address.Longitude);
                        }
                        else if (dist <= 0 && currentCustAddr != null && !string.IsNullOrEmpty(currentCustAddr.Province) && !string.IsNullOrEmpty(targetWh.Address.Province))
                        {
                            bool sameProv = currentCustAddr.Province.Trim().ToLower().Contains(targetWh.Address.Province.Trim().ToLower()) ||
                                            targetWh.Address.Province.Trim().ToLower().Contains(currentCustAddr.Province.Trim().ToLower());
                            dist = sameProv ? 8.0 : 100.0;
                        }

                        double maxRad = targetWh.MaxColdChainRadiusKm > 0 ? targetWh.MaxColdChainRadiusKm : 15.0;
                        if (dist > maxRad)
                        {
                            throw new InvalidOperationException($"Khoảng cách giao hàng ({dist:F1} km) vượt quá bán kính phục vụ xe thùng lạnh tối đa ({maxRad} km) của kho {targetWh.Name}. Các sản phẩm tươi sống sau không thể đảm bảo dải nhiệt độ mát 2°C - 8°C: {string.Join(", ", coldItemNames)}. Quý khách vui lòng chọn địa chỉ nhận hàng gần hơn hoặc loại bỏ các sản phẩm tươi sống này để đặt hàng.");
                        }
                    }
                }

                // Tính phí ship (Freeship 100% nếu net subtotal >= 300k)
                decimal netSubTotal = subTotal - totalDiscount;
                decimal shippingFee = netSubTotal >= GetFreeShippingThreshold() ? 0 : (request.ShippingFee > 0 ? request.ShippingFee : GetStandardShippingFee());
                decimal totalAmount = Math.Max(0, netSubTotal + shippingFee);

                var order = new Order
                {
                    OrderCode = orderCode,
                    CustomerId = validCustomerId,
                    WarehouseId = assignedWarehouseId,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    DeliveryAddress = deliveryAddress,
                    GhnDistrictId = request.GhnDistrictId ?? 1442,
                    GhnWardCode = request.GhnWardCode ?? "20101",
                    ShippingProvider = coldItemNames.Count > 0 ? "Solaris Cold-Chain Express (TMS)" : "Solaris Express",
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

                string shipFreeText = order.ShippingFee == 0 ? " (Áp dụng Freeship 100% xe thùng lạnh)" : $" (Phí ship xe lạnh: {order.ShippingFee:N0} ₫)";
                string payMethodText = order.PaymentMethod == PaymentMethod.COD ? "Thanh toán khi nhận hàng (COD)" : "Cổng VNPay Sandbox";

                string successMsgContent = order.PaymentMethod == PaymentMethod.COD
                    ? $"🎉 **Đặt hàng thành công!**\n\n" +
                      $"Mã đơn hàng: **`{order.OrderCode}`**\n" +
                      $"Tổng thanh toán: **{totalAmount:N0} ₫**{shipFreeText}\n" +
                      $"Hình thức: **{payMethodText}**\n\n" +
                      $"Đơn hàng đã được đặt hàng thành công, trạng thái: **Chờ xử lý**. Bộ phận kho Solaris sẽ soạn hàng theo nguyên tắc FEFO và đóng gói vào xe máy thùng lạnh chuyên dụng TMS (duy trì 2°C - 8°C) để giao tới bạn sớm nhất!"
                    : $"🎉 **Lên đơn hàng thành công!**\n\n" +
                      $"Mã đơn hàng: **`{order.OrderCode}`**\n" +
                      $"Tổng thanh toán: **{totalAmount:N0} ₫**{shipFreeText}\n" +
                      $"Hình thức: **{payMethodText}**\n\n" +
                      $"Hệ thống đang chuyển hướng bạn sang cổng VNPay Sandbox để thanh toán. Sau khi thanh toán thành công, đơn hàng sẽ chuyển sang trạng thái **Chờ xử lý** để kho tiến hành soạn hàng theo chuẩn FEFO và giao xe lạnh TMS tới bạn!";

                // Đảm bảo ChatSession tồn tại hợp lệ trước khi ghi ChatMessage (Tránh lỗi Foreign Key Constraint)
                int targetSessionId = request.SessionId;
                var session = targetSessionId > 0
                    ? await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == targetSessionId)
                    : null;

                if (session == null)
                {
                    session = await _context.ChatSessions
                        .Where(s => s.CustomerId == validCustomerId && s.IsActive)
                        .OrderByDescending(s => s.UpdatedAt)
                        .FirstOrDefaultAsync();

                    if (session == null)
                    {
                        session = new ChatSession
                        {
                            CustomerId = validCustomerId,
                            Title = "Đơn hàng AI Chatbot",
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsActive = true
                        };
                        _context.ChatSessions.Add(session);
                        await _context.SaveChangesAsync();
                    }
                    targetSessionId = session.Id;
                }

                session.DraftOrderJson = null;
                session.UpdatedAt = now;

                // Gửi tin nhắn xác nhận vào ChatSession
                var successMsg = new ChatMessage
                {
                    SessionId = targetSessionId,
                    Role = "model",
                    Content = successMsgContent,
                    PayloadType = "order_success",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        orderId = order.Id,
                        orderCode = order.OrderCode,
                        totalAmount = order.TotalAmount,
                        paymentMethodName = payMethodText,
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

            var now = DateTime.UtcNow;
            var items = new List<InteractiveOrderItemDto>();
            var stockWarnings = new List<string>();

            foreach (var detail in lastOrder.Details)
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p!.Category)
                            .ThenInclude(c => c!.CategoryGroup)
                    .Include(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                    .Include(v => v.PromotionVariants)
                        .ThenInclude(pv => pv.PromotionCampaign)
                    .FirstOrDefaultAsync(v => v.Id == detail.VariantId && !v.IsDeleted && v.IsActive);

                if (variant == null) continue;

                // Kiểm tra tồn kho khả dụng thời gian thực từ các lô hàng còn hạn dùng tại Kho Bán Lẻ
                var checkStockQuery = _context.WarehouseInventories
                    .Include(wi => wi.Batch)
                    .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && (wi.Batch == null || wi.Batch.ExpiryDate > now));

                var filteredCheckStock = await checkStockQuery.FilterRetailOnlyAsync(_context);

                var availableStock = await filteredCheckStock
                    .SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;

                bool hasStockTracking = await _context.WarehouseInventories.AnyAsync(wi => wi.VariantId == variant.Id);
                if (hasStockTracking && availableStock <= 0)
                {
                    stockWarnings.Add($"Sản phẩm '{variant.Name}' trong đơn cũ hiện đã tạm hết hàng.");
                    continue;
                }

                if (!hasStockTracking)
                {
                    availableStock = detail.Quantity;
                }

                // Lấy đơn giá hiện hành từ bảng giá hoặc chương trình khuyến mại đang chạy
                var priceObj = variant.Prices.FirstOrDefault(p => p.UoMId == detail.UoMId)
                               ?? variant.Prices.FirstOrDefault();
                decimal basePrice = priceObj?.Price ?? detail.UnitPrice;
                decimal unitPrice = basePrice;
                decimal discount = 0;

                var activePromo = variant.PromotionVariants
                    .Select(pv => pv.PromotionCampaign)
                    .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                    .OrderByDescending(pc => pc!.DiscountValue)
                    .FirstOrDefault();

                if (activePromo != null)
                {
                    if (activePromo.IsPercentage)
                    {
                        discount = basePrice * (activePromo.DiscountValue / 100m);
                    }
                    else
                    {
                        discount = activePromo.DiscountValue;
                    }
                    unitPrice = Math.Max(0, basePrice - discount);
                }
                else
                {
                    unitPrice = basePrice;
                    discount = 0;
                }

                decimal actualQty = detail.Quantity;
                string? warningMsg = null;

                if (detail.Quantity > availableStock)
                {
                    actualQty = availableStock;
                    warningMsg = $"Kho chỉ còn {availableStock:G29} {detail.UoM?.Name ?? "ĐVT"}";
                    stockWarnings.Add($"Sản phẩm '{variant.Name}' trong kho hiện chỉ còn {availableStock:G29} {detail.UoM?.Name ?? "ĐVT"} (đơn cũ: {detail.Quantity:G29}). Hệ thống đã tự động điều chỉnh số lượng.");
                }

                items.Add(new InteractiveOrderItemDto
                {
                    VariantId = variant.Id,
                    VariantCode = variant.Code,
                    VariantName = variant.Name,
                    Slug = variant.Product?.Slug ?? "san-pham",
                    ImagePath = variant.ImagePath ?? variant.Product?.ImagePath,
                    UoMId = detail.UoMId,
                    UoMName = detail.UoM?.Name ?? "Kg",
                    Quantity = actualQty,
                    UnitPrice = unitPrice,
                    DiscountAmount = discount,
                    TotalPrice = unitPrice * actualQty,
                    AvailableStock = availableStock,
                    WarningMessage = warningMsg
                });
            }

            if (items.Count == 0 && lastOrder.Details.Count > 0)
            {
                return new InteractiveOrderPayloadDto
                {
                    Title = $"Đơn Hàng Đặt Lại (Theo Đơn {lastOrder.OrderCode})",
                    PreviousOrderCode = lastOrder.OrderCode,
                    Items = new List<InteractiveOrderItemDto>(),
                    StockWarning = stockWarnings.Count > 0 ? string.Join("\n", stockWarnings) : "Các sản phẩm trong đơn hàng cũ hiện đã tạm hết hàng.",
                    SubTotal = 0,
                    TotalDiscount = 0,
                    ShippingFee = 0,
                    TotalAmount = 0,
                    IsFreeShipping = false,
                    SuggestedDeliveryAddress = lastOrder.DeliveryAddress,
                    SuggestedReceiverName = lastOrder.ReceiverName,
                    SuggestedReceiverPhone = lastOrder.ReceiverPhone
                };
            }

            if (items.Count == 0)
            {
                return await PrepareDefaultSuggestionOrderAsync();
            }

            decimal subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            decimal totalDiscount = items.Sum(i => i.DiscountAmount * i.Quantity);
            decimal netSubTotal = Math.Max(0, subTotal - totalDiscount);
            decimal freeThreshold = GetFreeShippingThreshold();
            decimal standardFee = GetStandardShippingFee();
            bool isFree = netSubTotal >= freeThreshold;
            decimal shippingFee = isFree ? 0 : standardFee;

            // Kiểm tra rào chắn khoảng cách chuỗi lạnh (Cold-Chain Delivery Feasibility)
            var (isColdChainFeasible, ineligibleColdItems, coldChainWarning) = await CheckColdChainFeasibilityAsync(items, customerId);

            return new InteractiveOrderPayloadDto
            {
                Title = $"Đơn Hàng Đặt Lại (Theo Đơn {lastOrder.OrderCode})",
                PreviousOrderCode = lastOrder.OrderCode,
                Items = items,
                StockWarning = stockWarnings.Count > 0 ? string.Join("\n", stockWarnings) : null,
                SubTotal = subTotal,
                TotalDiscount = totalDiscount,
                ShippingFee = shippingFee,
                TotalAmount = Math.Max(0, netSubTotal + shippingFee),
                IsFreeShipping = isFree,
                SuggestedDeliveryAddress = lastOrder.DeliveryAddress,
                SuggestedReceiverName = lastOrder.ReceiverName,
                SuggestedReceiverPhone = lastOrder.ReceiverPhone,
                IsColdChainFeasible = isColdChainFeasible,
                IneligibleColdChainItems = ineligibleColdItems,
                ColdChainWarning = coldChainWarning
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
            var query = _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .Include(o => o.DeliveryTrip)
                    .ThenInclude(t => t!.Vehicle)
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(orderCode))
            {
                query = query.Where(o => o.OrderCode.ToUpper() == orderCode.ToUpper());
            }
            else if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(o => o.CustomerId == customerId.Value)
                             .OrderByDescending(o => o.OrderDate);
            }
            else
            {
                return null;
            }

            var order = await query.FirstOrDefaultAsync();
            if (order == null) return null;

            var dto = _mapper.Map<AiOrderTrackingDto>(order);

            // Chi tiết Chuyến xe TMS & Phương tiện giao hàng
            if (order.DeliveryTrip != null)
            {
                dto.DeliveryTripCode = order.DeliveryTrip.TripCode;
                dto.LicensePlate = order.DeliveryTrip.LicensePlate;
                dto.DriverName = order.DeliveryTrip.DriverName;
                dto.DriverPhone = order.DeliveryTrip.DriverPhone;
                dto.TripStatus = order.DeliveryTrip.Status;
                dto.TripStatusName = order.DeliveryTrip.Status switch
                {
                    "Preparing" => "Chuẩn bị / Xếp hàng",
                    "InTransit" => "Đang đi đường",
                    "Completed" => "Đã hoàn tất",
                    "Cancelled" => "Đã hủy",
                    _ => order.DeliveryTrip.Status
                };
                dto.StartedAt = order.DeliveryTrip.StartedAt;
                if (order.DeliveryTrip.Vehicle != null)
                {
                    dto.VehicleType = order.DeliveryTrip.Vehicle.VehicleType == "Motorbike" ? "Xe máy thùng lạnh" : "Xe tải lạnh";
                    dto.IsColdChainVehicle = order.DeliveryTrip.Vehicle.IsColdChainEquipped;
                }
            }

            // Lý do hủy / kho từ chối nếu đơn ở trạng thái Cancelled
            if (order.Status == OrderStatus.Cancelled)
            {
                dto.CancellationReason = !string.IsNullOrEmpty(order.CancellationReason)
                    ? order.CancellationReason
                    : "Kho hàng đơn phương từ chối / hủy đơn do sản phẩm tươi sống không đạt kiểm định FEFO hoặc địa chỉ ngoài bán kính giao xe lạnh.";
            }

            // Tra cứu yêu cầu Đổi trả hàng (RMA - CustomerReturn) nếu có
            var rma = await _context.CustomerReturns
                .Where(r => r.OrderId == order.Id && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (rma != null)
            {
                dto.ReturnCode = rma.ReturnCode;
                dto.ReturnStatus = rma.Status switch
                {
                    CustomerReturnStatus.Pending => "Chờ tiếp nhận yêu cầu",
                    CustomerReturnStatus.Approved => "Đã duyệt yêu cầu",
                    CustomerReturnStatus.PickingUp => "Tài xế đang đi thu hồi",
                    CustomerReturnStatus.Inspecting => "Đang kiểm định chất lượng QC",
                    CustomerReturnStatus.Completed => "Hoàn tất & Hoàn tiền",
                    CustomerReturnStatus.Rejected => "Từ chối trả hàng",
                    _ => rma.Status.ToString()
                };
                dto.RefundAmount = rma.RefundAmount;
            }

            return dto;
        }

        private async Task<(bool IsColdChainFeasible, List<string> IneligibleColdItems, string? ColdChainWarning)> CheckColdChainFeasibilityAsync(List<InteractiveOrderItemDto> items, int? customerId)
        {
            bool isColdChainFeasible = true;
            var ineligibleColdItems = new List<string>();
            string? coldChainWarning = null;

            if (items == null || items.Count == 0)
                return (true, ineligibleColdItems, null);

            var targetVariantIds = items.Select(i => i.VariantId).ToList();
            var coldVariants = await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p!.Category)
                        .ThenInclude(c => c!.CategoryGroup)
                .Where(v => targetVariantIds.Contains(v.Id))
                .ToListAsync();

            var itemsNeedingCold = coldVariants.Where(v =>
                v.Product?.Category?.RequiresColdChain == true ||
                (v.Product?.Category?.CategoryGroup?.Code == "FRESH_PRODUCE") ||
                (v.Product?.Category?.Name?.ToLower().Contains("tươi") == true) ||
                (v.Product?.Category?.Name?.ToLower().Contains("thịt") == true) ||
                (v.Product?.Category?.Name?.ToLower().Contains("cá") == true)
            ).Select(v => v.Name).Distinct().ToList();

            if (itemsNeedingCold.Count > 0 && customerId.HasValue && customerId.Value > 0)
            {
                var custWithAddr = await _context.Customers
                    .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted);

                var targetAddr = custWithAddr?.Addresses.FirstOrDefault(a => a.IsDefault) ?? custWithAddr?.Addresses.FirstOrDefault();
                if (targetAddr != null)
                {
                    double distKm = 0;
                    double maxRadius = 15.0;
                    string whName = "Solaris Cold Storage";

                    if (_routingService != null)
                    {
                        var routingItems = items.Select(i => new backend.DTOs.OrderDTOs.OrderDetailCreateDto
                        {
                            VariantId = i.VariantId,
                            Quantity = i.Quantity
                        }).ToList();

                        var routing = await _routingService.DetermineOptimalWarehouseAsync(
                            targetAddr.Id,
                            routingItems,
                            targetAddr.Latitude,
                            targetAddr.Longitude,
                            targetAddr.Province,
                            targetAddr.District);

                        distKm = routing.DistanceKm;
                        whName = routing.WarehouseName;

                        var wh = await _context.Warehouses
                            .FirstOrDefaultAsync(w => w.Id == routing.OptimalWarehouseId);
                        if (wh != null && wh.MaxColdChainRadiusKm > 0)
                        {
                            maxRadius = wh.MaxColdChainRadiusKm;
                        }
                    }
                    else
                    {
                        var wh = await _context.Warehouses
                            .Include(w => w.Address)
                            .FirstOrDefaultAsync(w => w.IsActive && !w.IsDeleted && w.Address != null);

                        if (wh?.Address != null)
                        {
                            whName = wh.Name;
                            if (targetAddr.Latitude != 0 && targetAddr.Longitude != 0 && wh.Address.Latitude != 0 && wh.Address.Longitude != 0)
                            {
                                var distanceService = new DistanceService();
                                distKm = distanceService.CalculateDistanceKm(targetAddr.Latitude, targetAddr.Longitude, wh.Address.Latitude, wh.Address.Longitude);
                            }
                            else if (!string.IsNullOrEmpty(targetAddr.Province) && !string.IsNullOrEmpty(wh.Address.Province))
                            {
                                bool sameProv = targetAddr.Province.Trim().ToLower().Contains(wh.Address.Province.Trim().ToLower()) ||
                                                wh.Address.Province.Trim().ToLower().Contains(targetAddr.Province.Trim().ToLower());
                                distKm = sameProv ? 8.0 : 100.0;
                            }

                            maxRadius = wh.MaxColdChainRadiusKm > 0 ? wh.MaxColdChainRadiusKm : 15.0;
                        }
                    }

                    if (distKm > maxRadius)
                    {
                        isColdChainFeasible = false;
                        ineligibleColdItems = itemsNeedingCold;
                        coldChainWarning = $"Địa chỉ giao hàng ({targetAddr.FullAddress}) cách kho xe lạnh {whName} khoảng {distKm:F1} km, vượt quá bán kính bảo quản tối đa ({maxRadius} km) của xe máy thùng lạnh chuyên dụng TMS. Các sản phẩm tươi sống sau không đảm bảo dải nhiệt độ mát 2°C - 8°C: {string.Join(", ", ineligibleColdItems)}. Vui lòng đổi địa chỉ nhận hàng gần hơn hoặc chỉ đặt các sản phẩm đồ khô/nhiệt độ thường.";
                    }
                }
            }

            return (isColdChainFeasible, ineligibleColdItems, coldChainWarning);
        }

        private async Task<(ProductVariant Variant, ProductVariantPrice SelectedPrice, List<AiProductCardPriceDto> AvailablePrices)?> FindProductAndPriceMatchAsync(
            string productNameOrVariant,
            string? requestedUom)
        {
            string term = (productNameOrVariant ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term)) return null;

            var activeVariants = await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p!.BaseUoM)
                .Include(v => v.Product)
                    .ThenInclude(p => p!.Category)
                .Include(v => v.Prices)
                    .ThenInclude(pr => pr.UoM)
                .Include(v => v.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .Where(v => v.IsActive && !v.IsDeleted && v.Product != null && v.Product.IsActive && !v.Product.IsDeleted)
                .ToListAsync();

            if (activeVariants.Count == 0) return null;

            var searchTokens = term.Split(new[] { ' ', ',', '.', '-', '_', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1).ToList();

            ProductVariant? bestVariant = null;
            int bestScore = -1;

            foreach (var v in activeVariants)
            {
                var p = v.Product!;
                string vName = v.Name.ToLower();
                string pName = p.Name.ToLower();
                string slug = (p.Slug ?? string.Empty).Replace("-", " ").ToLower();

                int score = 0;

                if (vName == term) score += 100;
                else if (pName == term) score += 90;
                else if (slug == term) score += 85;
                else if (vName.Contains(term)) score += 70;
                else if (pName.Contains(term)) score += 60;
                else if (slug.Contains(term)) score += 55;
                else if (term.Contains(vName)) score += 65;
                else if (term.Contains(pName)) score += 50;

                if (searchTokens.Count > 0)
                {
                    int tokenMatches = searchTokens.Count(t => vName.Contains(t) || pName.Contains(t) || slug.Contains(t));
                    score += tokenMatches * 20;
                }

                // Cách 1 (Tách SKU): Nếu người dùng chỉ định ĐVT (ví dụ: Thùng, Hộp, Gói), ưu tiên biến thể có tên chứa ĐVT này
                if (!string.IsNullOrEmpty(requestedUom))
                {
                    string reqUomLower = requestedUom.ToLower();
                    if (vName.Contains(reqUomLower))
                    {
                        score += 50;
                    }
                }

                if (score > bestScore && score >= 20)
                {
                    bestScore = score;
                    bestVariant = v;
                }
            }

            if (bestVariant == null) return null;

            var prices = bestVariant.Prices.Where(pr => pr.IsActive && !pr.IsDeleted).ToList();
            if (!prices.Any() && bestVariant.Prices.Any())
            {
                prices = bestVariant.Prices.ToList();
            }

            var allUoms = await _context.UoMs.ToListAsync();
            foreach (var pr in prices)
            {
                if (pr.UoM == null && pr.UoMId > 0)
                {
                    pr.UoM = allUoms.FirstOrDefault(u => u.Id == pr.UoMId);
                }
            }
            ProductVariantPrice? matchedPrice = null;

            // Cách 2 (Bảng giá đa ĐVT trên cùng SKU)
            if (!string.IsNullOrEmpty(requestedUom) && prices.Count > 0)
            {
                string reqUomLower = requestedUom.ToLower().Trim();

                // 1. Ưu tiên tuyệt đối: Khớp chính xác hoàn toàn (Exact Match)
                matchedPrice = prices.FirstOrDefault(pr =>
                {
                    string uName = (pr.UoM?.Name ?? string.Empty).ToLower().Trim();
                    string uCode = (pr.UoM?.Code ?? string.Empty).ToLower().Trim();
                    return uName == reqUomLower || uCode == reqUomLower;
                });

                // 2. Ưu tiên quy cách đóng gói đặc thù (Hộp, Thùng, Gói, Bịch, Quả/Trái, Kg)
                if (matchedPrice == null)
                {
                    if (reqUomLower.Contains("hộp"))
                        matchedPrice = prices.FirstOrDefault(pr => (pr.UoM?.Name ?? string.Empty).ToLower().Contains("hộp"));
                    else if (reqUomLower.Contains("thùng"))
                        matchedPrice = prices.FirstOrDefault(pr => (pr.UoM?.Name ?? string.Empty).ToLower().Contains("thùng"));
                    else if (reqUomLower.Contains("gói"))
                        matchedPrice = prices.FirstOrDefault(pr => (pr.UoM?.Name ?? string.Empty).ToLower().Contains("gói"));
                    else if (reqUomLower.Contains("quả") || reqUomLower.Contains("trái"))
                        matchedPrice = prices.FirstOrDefault(pr =>
                        {
                            string u = (pr.UoM?.Name ?? string.Empty).ToLower();
                            return u.Contains("quả") || u.Contains("trái") || u.Contains("củ");
                        });
                    else if (reqUomLower == "kg" || reqUomLower == "kí" || reqUomLower == "ký" || reqUomLower == "kilo")
                        matchedPrice = prices.FirstOrDefault(pr =>
                        {
                            string u = (pr.UoM?.Name ?? string.Empty).ToLower();
                            return (u.Contains("kg") || u.Contains("kilo")) && !u.Contains("hộp") && !u.Contains("thùng");
                        });
                }

                // 3. Fallback: contains nếu chưa tìm thấy
                if (matchedPrice == null)
                {
                    matchedPrice = prices.FirstOrDefault(pr =>
                    {
                        string uName = (pr.UoM?.Name ?? string.Empty).ToLower();
                        return uName.Contains(reqUomLower);
                    });
                }
            }

            if (matchedPrice == null)
            {
                matchedPrice = prices.FirstOrDefault(pr => pr.IsDefault) ?? prices.FirstOrDefault();
            }

            if (matchedPrice == null)
            {
                matchedPrice = new ProductVariantPrice
                {
                    VariantId = bestVariant.Id,
                    UoMId = bestVariant.Product?.BaseUoMId ?? 1,
                    Price = 50000,
                    IsDefault = true
                };
            }

            var now = DateTime.UtcNow;
            var promo = bestVariant.PromotionVariants
                .Select(pv => pv.PromotionCampaign)
                .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                .OrderByDescending(pc => pc!.DiscountValue)
                .FirstOrDefault();

            var availablePrices = prices.Select(pr => new AiProductCardPriceDto
            {
                UoMId = pr.UoMId,
                UoMName = pr.UoM?.Name ?? bestVariant.Product?.BaseUoM?.Name ?? "Kg",
                Price = pr.Price,
                DiscountedPrice = promo != null
                    ? (promo.IsPercentage ? Math.Max(0, Math.Round(pr.Price * (1 - promo.DiscountValue / 100m))) : Math.Max(0, pr.Price - promo.DiscountValue))
                    : pr.Price,
                IsDefault = pr.IsDefault
            }).ToList();

            return (bestVariant, matchedPrice, availablePrices);
        }

        private async Task<List<ManageDraftOrderItemArg>> ExtractOrderItemsFromTextAsync(string userText, int sessionId)
        {
            var items = new List<ManageDraftOrderItemArg>();
            if (string.IsNullOrWhiteSpace(userText)) return items;

            // A. Thử tách mệnh đề bằng các liên từ tự nhiên: "và", ",", "+", "với", "cùng", "thêm", dấu xuống dòng
            var parts = Regex.Split(userText, @"\s*(?:và|,|\+|\bvới\b|\bcùng\b|\bthêm\b|\r?\n)\s*", RegexOptions.IgnoreCase)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (parts.Count > 1)
            {
                foreach (var part in parts)
                {
                    var match = Regex.Match(part, @"(\d+(?:[.,]\d+)?)\s*(kg|kí|ký|kilo|kí\s*lô|kí\s*lô\s*gam|kilogram|gói|hộp|thùng|quả|trái|bịch|túi|vỉ|chai|lon|cái)?", RegexOptions.IgnoreCase);
                    decimal qty = 1;
                    string? uom = null;
                    string keyword = part;

                    if (match.Success)
                    {
                        if (decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal q) && q > 0)
                        {
                            qty = q;
                        }
                        if (match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            uom = match.Groups[2].Value;
                        }
                        keyword = part.Remove(match.Index, match.Length);
                    }

                    keyword = Regex.Replace(keyword, @"\b(cho\s*tôi|cho\s*mình|lấy\s*giúp|lấy|mua|bán|đặt|thêm|giúp|cho|ơi|nhé|nha|với|tôi|mình|em|anh|chị)\b", "", RegexOptions.IgnoreCase).Trim();
                    keyword = Regex.Replace(keyword, @"^[,\.\-\+\s]+|[,\.\-\+\s]+$", "");

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        items.Add(new ManageDraftOrderItemArg
                        {
                            ProductNameOrVariant = keyword,
                            Quantity = qty,
                            RequestedUom = uom
                        });
                    }
                }
            }

            // B. Nếu parts.Count <= 1, quét xem có nhiều cụm [số lượng + ĐVT + tên sản phẩm] không
            if (items.Count == 0)
            {
                var matches = Regex.Matches(userText, @"(\d+(?:[.,]\d+)?)\s*(kg|kí|ký|kilo|kí\s*lô|kí\s*lô\s*gam|kilogram|gói|hộp|thùng|quả|trái|bịch|túi|vỉ|chai|lon|cái)?\s+([^\d,;+\r\n]+)", RegexOptions.IgnoreCase);
                if (matches.Count > 1)
                {
                    foreach (Match m in matches)
                    {
                        decimal qty = 1;
                        if (decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal q) && q > 0)
                        {
                            qty = q;
                        }
                        string? uom = m.Groups[2].Success ? m.Groups[2].Value : null;
                        string prod = Regex.Replace(m.Groups[3].Value, @"\b(cho\s*tôi|cho\s*mình|lấy\s*giúp|lấy|mua|bán|đặt|thêm|giúp|cho|ơi|nhé|nha|với|tôi|mình|em|anh|chị)\b", "", RegexOptions.IgnoreCase).Trim();
                        prod = Regex.Replace(prod, @"^[,\.\-\+\s]+|[,\.\-\+\s]+$", "");
                        if (!string.IsNullOrWhiteSpace(prod))
                        {
                            items.Add(new ManageDraftOrderItemArg
                            {
                                ProductNameOrVariant = prod,
                                Quantity = qty,
                                RequestedUom = uom
                            });
                        }
                    }
                }
                else if (matches.Count == 1)
                {
                    var m = matches[0];
                    decimal qty = 1;
                    if (decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal q) && q > 0)
                    {
                        qty = q;
                    }
                    string? uom = m.Groups[2].Success ? m.Groups[2].Value : null;
                    string prod = Regex.Replace(m.Groups[3].Value, @"\b(cho\s*tôi|cho\s*mình|lấy\s*giúp|lấy|mua|bán|đặt|thêm|giúp|cho|ơi|nhé|nha|với|tôi|mình|em|anh|chị)\b", "", RegexOptions.IgnoreCase).Trim();
                    prod = Regex.Replace(prod, @"^[,\.\-\+\s]+|[,\.\-\+\s]+$", "");
                    if (!string.IsNullOrWhiteSpace(prod))
                    {
                        items.Add(new ManageDraftOrderItemArg
                        {
                            ProductNameOrVariant = prod,
                            Quantity = qty,
                            RequestedUom = uom
                        });
                    }
                }
            }

            // C. Fallback: Trích xuất số lượng và từ khóa đơn
            if (items.Count == 0)
            {
                var singleQtyMatch = Regex.Match(userText, @"(\d+(?:[.,]\d+)?)\s*(kg|kí|ký|kilo|kí\s*lô|kí\s*lô\s*gam|kilogram|gói|hộp|thùng|quả|trái|bịch|túi|vỉ|chai|lon|cái)?", RegexOptions.IgnoreCase);
                decimal qty = 1;
                string? uom = null;
                string keyword = userText;

                if (singleQtyMatch.Success)
                {
                    if (decimal.TryParse(singleQtyMatch.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal q) && q > 0)
                    {
                        qty = q;
                    }
                    if (singleQtyMatch.Groups[2].Success && !string.IsNullOrWhiteSpace(singleQtyMatch.Groups[2].Value))
                    {
                        uom = singleQtyMatch.Groups[2].Value;
                    }
                    keyword = userText.Remove(singleQtyMatch.Index, singleQtyMatch.Length);
                }

                keyword = Regex.Replace(keyword, @"\b(cho\s*tôi|cho\s*mình|lấy\s*giúp|lấy|mua|bán|đặt|thêm|giúp|cho|ơi|nhé|nha|với|tôi|mình|em|anh|chị|lên\s*đơn|chốt\s*đơn|tạo\s*đơn|xóa|bỏ|bớt)\b", "", RegexOptions.IgnoreCase).Trim();
                keyword = Regex.Replace(keyword, @"^[,\.\-\+\s]+|[,\.\-\+\s]+$", "");

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    items.Add(new ManageDraftOrderItemArg
                    {
                        ProductNameOrVariant = keyword,
                        Quantity = qty,
                        RequestedUom = uom
                    });
                }
            }

            // D. Nếu không có tên sản phẩm trực tiếp (VD: "Lên đơn cho tôi", "Chốt đơn giúp mình") -> Truy hồi từ ngữ cảnh gần nhất trong phiên
            if ((items.Count == 0 || items.All(i => string.IsNullOrWhiteSpace(i.ProductNameOrVariant))) && sessionId > 0)
            {
                items.Clear();
                var recentMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                var recentCardMsg = recentMessages.FirstOrDefault(m => m.PayloadType == "product_cards" && !string.IsNullOrEmpty(m.PayloadJson));
                if (recentCardMsg != null)
                {
                    try
                    {
                        var cachedCards = JsonSerializer.Deserialize<List<AiProductCardDto>>(recentCardMsg.PayloadJson!);
                        if (cachedCards != null && cachedCards.Count > 0)
                        {
                            var card = cachedCards.First();
                            items.Add(new ManageDraftOrderItemArg
                            {
                                ProductNameOrVariant = card.Name,
                                Quantity = 1,
                                RequestedUom = card.UoMName
                            });
                        }
                    }
                    catch { }
                }
            }

            return items;
        }


        public async Task<InteractiveOrderPayloadDto?> ExecuteManageDraftOrderAsync(
            ChatSession session,
            ManageDraftOrderToolArgs args,
            int? customerId)
        {
            var now = DateTime.UtcNow;

            InteractiveOrderPayloadDto? draftOrder = null;
            if (!string.IsNullOrEmpty(session.DraftOrderJson))
            {
                try
                {
                    draftOrder = JsonSerializer.Deserialize<InteractiveOrderPayloadDto>(session.DraftOrderJson);
                }
                catch
                {
                    draftOrder = null;
                }
            }

            if (draftOrder == null)
            {
                draftOrder = new InteractiveOrderPayloadDto
                {
                    Title = "Thẻ Đơn Hàng Tương Tác",
                    Items = new List<InteractiveOrderItemDto>()
                };
            }

            string action = (args.Action ?? "add").ToLower();

            if (action == "clear")
            {
                draftOrder.Items.Clear();
                session.DraftOrderJson = null;
                return draftOrder;
            }

            var stockWarnings = new List<string>();

            foreach (var itemArg in args.Items)
            {
                if (string.IsNullOrWhiteSpace(itemArg.ProductNameOrVariant)) continue;

                if (action == "remove")
                {
                    draftOrder.Items.RemoveAll(i =>
                        i.VariantName.ToLower().Contains(itemArg.ProductNameOrVariant.ToLower()) ||
                        i.Slug.ToLower().Contains(itemArg.ProductNameOrVariant.ToLower()));
                    continue;
                }

                var matchResult = await FindProductAndPriceMatchAsync(itemArg.ProductNameOrVariant, itemArg.RequestedUom);
                if (matchResult == null) continue;

                var (variant, selectedPrice, availablePrices) = matchResult.Value;

                // Kiểm tra tồn kho khả dụng từ các lô hàng còn hạn dùng tại kho bán lẻ
                var checkStockQuery = _context.WarehouseInventories
                    .Include(wi => wi.Batch)
                    .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && (wi.BatchId == 0 || wi.Batch == null || wi.Batch.ExpiryDate > now));
                var filteredCheckStock = await checkStockQuery.FilterRetailOnlyAsync(_context);
                var availableStock = await filteredCheckStock.SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;

                if (availableStock <= 0)
                {
                    stockWarnings.Add($"Sản phẩm '{variant.Name}' hiện đã hết hàng trong kho Solaris.");
                    continue;
                }

                decimal requestedQty = itemArg.Quantity > 0 ? itemArg.Quantity : 1;
                decimal clampedQty = requestedQty;
                if (requestedQty > availableStock)
                {
                    clampedQty = availableStock;
                    stockWarnings.Add($"Kho hiện chỉ còn {availableStock:G29} {selectedPrice.UoM?.Name ?? "đơn vị"} {variant.Name} (bạn đã yêu cầu {requestedQty:G29}), hệ thống đã điều chỉnh số lượng trên đơn.");
                }

                decimal unitPrice = selectedPrice.Price;
                decimal discountAmount = 0;

                var promo = variant.PromotionVariants
                    .Select(pv => pv.PromotionCampaign)
                    .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                    .OrderByDescending(pc => pc!.DiscountValue)
                    .FirstOrDefault();

                if (promo != null)
                {
                    decimal discounted = promo.IsPercentage
                        ? Math.Max(0, Math.Round(unitPrice * (1 - promo.DiscountValue / 100m)))
                        : Math.Max(0, unitPrice - promo.DiscountValue);
                    discountAmount = Math.Max(0, unitPrice - discounted);
                }

                var existingItem = draftOrder.Items.FirstOrDefault(i => i.VariantId == variant.Id && i.UoMId == selectedPrice.UoMId);
                if (existingItem != null)
                {
                    if (action == "update")
                    {
                        existingItem.Quantity = clampedQty;
                    }
                    else // "add" -> Cộng dồn theo phiên
                    {
                        decimal totalQty = existingItem.Quantity + clampedQty;
                        if (totalQty > availableStock)
                        {
                            existingItem.Quantity = availableStock;
                            stockWarnings.Add($"Kho hiện chỉ còn {availableStock:G29} {existingItem.UoMName} {variant.Name}, hệ thống đã giới hạn số lượng tối đa trên đơn.");
                        }
                        else
                        {
                            existingItem.Quantity = totalQty;
                        }
                    }
                    existingItem.AvailableStock = availableStock;
                    existingItem.UnitPrice = unitPrice;
                    existingItem.DiscountAmount = discountAmount;
                    existingItem.TotalPrice = (unitPrice - discountAmount) * existingItem.Quantity;
                    existingItem.AvailablePrices = availablePrices;
                }
                else
                {
                    draftOrder.Items.Add(new InteractiveOrderItemDto
                    {
                        VariantId = variant.Id,
                        VariantCode = $"VAR-{variant.Id}",
                        VariantName = variant.Name,
                        Slug = variant.Product?.Slug ?? "san-pham",
                        ImagePath = variant.ImagePath ?? variant.Product?.ImagePath,
                        UoMId = selectedPrice.UoMId,
                        UoMName = selectedPrice.UoM?.Name ?? variant.Product?.BaseUoM?.Name ?? "Kg",
                        Quantity = clampedQty,
                        UnitPrice = unitPrice,
                        DiscountAmount = discountAmount,
                        TotalPrice = (unitPrice - discountAmount) * clampedQty,
                        AvailableStock = availableStock,
                        AvailablePrices = availablePrices
                    });
                }
            }

            if (draftOrder.Items.Count == 0 && stockWarnings.Count > 0)
            {
                return new InteractiveOrderPayloadDto
                {
                    Title = "Thẻ Đơn Hàng Tương Tác",
                    Items = new List<InteractiveOrderItemDto>(),
                    StockWarning = string.Join("\n", stockWarnings)
                };
            }

            if (draftOrder.Items.Count == 0) return null;

            decimal subTotal = draftOrder.Items.Sum(i => i.UnitPrice * i.Quantity);
            decimal totalDiscount = draftOrder.Items.Sum(i => i.DiscountAmount * i.Quantity);
            decimal netSubTotal = Math.Max(0, subTotal - totalDiscount);
            decimal freeThreshold = GetFreeShippingThreshold();
            decimal standardFee = GetStandardShippingFee();
            bool isFree = netSubTotal >= freeThreshold;
            decimal shippingFee = isFree ? 0 : standardFee;

            draftOrder.SubTotal = subTotal;
            draftOrder.TotalDiscount = totalDiscount;
            draftOrder.ShippingFee = shippingFee;
            draftOrder.TotalAmount = Math.Max(0, netSubTotal + shippingFee);
            draftOrder.IsFreeShipping = isFree;
            if (stockWarnings.Count > 0)
            {
                draftOrder.StockWarning = string.Join("\n", stockWarnings);
            }

            // Gợi ý địa chỉ từ tài khoản hoặc đơn trước
            string? address = null;
            string? name = null;
            string? phone = null;

            if (customerId.HasValue && customerId.Value > 0)
            {
                var custWithAddr = await _context.Customers
                    .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                    .FirstOrDefaultAsync(c => c.Id == customerId.Value && !c.IsDeleted);

                if (custWithAddr != null)
                {
                    var defAddr = custWithAddr.Addresses.FirstOrDefault(a => a.IsDefault) ?? custWithAddr.Addresses.FirstOrDefault();
                    if (defAddr != null)
                    {
                        address = defAddr.FullAddress;
                        name = !string.IsNullOrEmpty(defAddr.ReceiverName) ? defAddr.ReceiverName : custWithAddr.Name;
                        phone = !string.IsNullOrEmpty(defAddr.Phone) ? defAddr.Phone : custWithAddr.PhoneNumber;
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

            draftOrder.SuggestedDeliveryAddress = address;
            draftOrder.SuggestedReceiverName = name;
            draftOrder.SuggestedReceiverPhone = phone;

            // Rào chắn khoảng cách chuỗi lạnh (Cold-Chain Delivery Feasibility)
            var (isFeasible, ineligibleItems, coldWarning) = await CheckColdChainFeasibilityAsync(draftOrder.Items, customerId);
            draftOrder.IsColdChainFeasible = isFeasible;
            draftOrder.IneligibleColdChainItems = ineligibleItems;
            draftOrder.ColdChainWarning = coldWarning;

            // Lưu vào ChatSession DraftOrderJson
            session.DraftOrderJson = JsonSerializer.Serialize(draftOrder);

            return draftOrder;
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
                            var activePrices = v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted).ToList();
                            if (!activePrices.Any() && v.Prices.Any())
                            {
                                activePrices = v.Prices.ToList();
                            }

                            string baseUom = p.BaseUoM?.Name ?? "Kg";
                            decimal availableQty = inventories.TryGetValue(v.Id, out decimal s) ? s : 0;
                            bool inStock = availableQty > 0;

                            // Phân loại Mô hình Quy cách bán:
                            // Cách 2 (Một biến thể có nhiều quy cách bán / Quy đổi ĐVT): 1 SKU duy nhất nhưng có nhiều mức giá ĐVT
                            // Cách 1 (Một biến thể = Một quy cách bán / Tách SKU riêng): Mỗi quy cách là 1 mã SKU độc lập
                            string modelTag = activePrices.Count > 1
                                ? "[Cách 2: Đa quy cách bán / Quy đổi ĐVT]"
                                : "[Cách 1: Tách SKU riêng / 1 Quy cách bán]";

                            var priceDetails = new List<string>();
                            foreach (var pr in activePrices)
                            {
                                string prUom = pr.UoM?.Name ?? baseUom;
                                string defTag = pr.IsDefault ? " [Mặc định]" : string.Empty;
                                priceDetails.Add($"{pr.Price:N0} ₫/{prUom}{defTag}");
                            }
                            string pricingStr = priceDetails.Count > 0 ? string.Join(", ", priceDetails) : "Liên hệ";

                            // Nhận diện bảo quản lạnh TMS (2-8°C)
                            bool isCold = p.Category?.RequiresColdChain == true ||
                                          (p.Category?.CategoryGroup?.Code == "FRESH_PRODUCE") ||
                                          (p.Category?.Name?.ToLower().Contains("tươi") == true) ||
                                          (p.Category?.Name?.ToLower().Contains("thịt") == true) ||
                                          (p.Category?.Name?.ToLower().Contains("cá") == true);
                            string coldTag = isCold ? " | ❄️ Bảo quản lạnh TMS (2-8°C)" : " | 📦 Giao thường";

                            string? origin = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && a.AttributeDefinition.Name.ToLower().Contains("xuất xứ"))?.AttributeValue;
                            string? cert = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")))?.AttributeValue;
                            string? brix = v.Attributes.FirstOrDefault(a => a.AttributeDefinition != null && (a.AttributeDefinition.Name.ToLower().Contains("độ ngọt") || a.AttributeDefinition.Name.ToLower().Contains("brix")))?.AttributeValue;

                            string descSnippet = !string.IsNullOrEmpty(v.Description) ? $" | Đặc điểm: {v.Description.Trim()}" : string.Empty;
                            string originStr = !string.IsNullOrEmpty(origin) ? $" | Xuất xứ: {origin}" : string.Empty;
                            string certStr = !string.IsNullOrEmpty(cert) ? $" | Tiêu chuẩn: {cert}" : string.Empty;
                            string brixStr = !string.IsNullOrEmpty(brix) ? $" | Độ ngọt: {brix}°Bx" : string.Empty;
                            sb.AppendLine($"       + SKU [ID:{v.Id}]: {v.Name} (Mã: {v.Code}) {modelTag} | Bảng giá: {pricingStr} | Tồn kho khả dụng: {(inStock ? $"{availableQty:G29} {baseUom} (Còn hàng)" : "0 (Tạm hết)")}{originStr}{certStr}{brixStr}{coldTag}{descSnippet}");
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

            // Nếu người dùng hỏi tổng quan: "đang có sản phẩm nào", "shop bán gì", "nông sản hôm nay", "có gì", "danh sách", "tư vấn", "gợi ý"...
            bool isGeneralInquiry = rawKw.Contains("sản phẩm") || rawKw.Contains("nông sản") || rawKw.Contains("trái cây") ||
                                    rawKw.Contains("bán gì") || rawKw.Contains("có gì") || rawKw.Contains("tươi hôm nay") ||
                                    rawKw.Contains("menu") || rawKw.Contains("danh mục") || rawKw.Contains("hoa quả") ||
                                    rawKw.Contains("tư vấn") || rawKw.Contains("tu van") || rawKw.Contains("gợi ý") ||
                                    rawKw.Contains("goi y") || rawKw.Contains("đề xuất") || rawKw.Contains("de xuat") ||
                                    rawKw.Contains("recommend") ||
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
                "tìm", "kiếm", "muốn", "xem", "tư", "vấn", "mua", "lấy", "đặt", "chốt", "bán", "solaris", "hàng", "cửa",
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                "kg", "kí", "ký", "quả", "trái", "hộp", "thùng", "bịch", "loại", "nào", "những", "đang", "ạ", "nhé"
            });

            var searchTerms = rawKw.Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(w => !stopWords.Contains(w) && w.Length > 1)
                                   .ToList();

            if (searchTerms.Count == 0)
            {
                var fallbackPairs = allActiveProducts
                    .SelectMany(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted).Select(v => (Product: p, Variant: v)))
                    .Take(4)
                    .ToList();
                return await EnrichVariantCardDtosAsync(fallbackPairs);
            }

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
                                   (!string.IsNullOrEmpty(pSlug) && rawKw.Contains(pSlug.Replace("-", " ")));

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

                var availablePrices = v.Prices
                    .Where(pr => pr.IsActive && !pr.IsDeleted)
                    .Select(pr => new AiProductCardPriceDto
                    {
                        UoMId = pr.UoMId,
                        UoMName = pr.UoM?.Name ?? p.BaseUoM?.Name ?? "Kg",
                        Price = pr.Price,
                        DiscountedPrice = promo != null
                            ? (promo.IsPercentage ? Math.Max(0, Math.Round(pr.Price * (1 - promo.DiscountValue / 100m))) : Math.Max(0, pr.Price - promo.DiscountValue))
                            : pr.Price,
                        IsDefault = pr.IsDefault
                    })
                    .ToList();

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
                    IsInStock = stock > 0,
                    AvailablePrices = availablePrices
                });
            }

            return dtoList;
        }

        
        private static object GetGeminiToolsDeclaration()
        {
            return new object[]
            {
                new
                {
                    function_declarations = new object[]
                    {
                        new
                        {
                            name = "manage_draft_order",
                            description = "Thao tác trên giỏ hàng hội thoại: thêm mới, cộng dồn, sửa số lượng, đổi quy cách, hoặc xóa sản phẩm.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    action = new
                                    {
                                        type = "STRING",
                                        @enum = new[] { "add", "update", "remove", "clear" },
                                        description = "Hành động: add (thêm/cộng dồn), update (cập nhật số lượng), remove (xóa sản phẩm), clear (xóa toàn bộ giỏ hàng)"
                                    },
                                    items = new
                                    {
                                        type = "ARRAY",
                                        description = "Danh sách sản phẩm cần thao tác",
                                        items = new
                                        {
                                            type = "OBJECT",
                                            properties = new
                                            {
                                                productName = new { type = "STRING", description = "Tên sản phẩm hoặc biến thể nông sản khách muốn mua" },
                                                quantity = new { type = "NUMBER", description = "Số lượng khách yêu cầu" },
                                                uom = new { type = "STRING", description = "Đơn vị tính quy cách đóng gói (kg, hộp, thùng, gói, bịch, trái...)" }
                                            },
                                            required = new[] { "productName", "quantity" }
                                        }
                                    }
                                },
                                required = new[] { "action", "items" }
                            }
                        },
                        new
                        {
                            name = "search_products",
                            description = "Tìm kiếm sản phẩm nông sản sạch trong danh mục kho Solaris theo từ khóa.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    keyword = new { type = "STRING", description = "Từ khóa tìm kiếm (tên sản phẩm, chủng loại...)" },
                                    categoryGroup = new { type = "STRING", description = "Tên hoặc mã nhóm danh mục nếu có" }
                                },
                                required = new[] { "keyword" }
                            }
                        },
                        new
                        {
                            name = "lookup_order",
                            description = "Tra cứu tiến trình xử lý, trạng thái đơn hàng và thông tin chuyến xe giao hàng thùng lạnh TMS.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    orderCode = new { type = "STRING", description = "Mã đơn hàng dạng ORD-... nếu có" }
                                }
                            }
                        },
                        new
                        {
                            name = "reorder_previous_order",
                            description = "Lấy lại danh sách sản phẩm từ đơn hàng trước đây của khách hàng để tạo giỏ hàng đặt lại.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new { }
                            }
                        },
                        new
                        {
                            name = "get_active_promotions",
                            description = "Tra cứu các sản phẩm đang có chương trình khuyến mãi, giảm giá trong hệ thống.",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new { }
                            }
                        }
                    }
                }
            };
        }

        private async Task<(bool Handled, string PayloadType, object? PayloadObject)> TryDispatchGeminiToolsAsync(
            ChatSession session,
            string userText,
            int? customerId)
        {
            var geminiSection = _config.GetSection("GeminiSettings");
            string apiKey = geminiSection["ApiKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("TEST_")) return (false, "none", null);

            string model = geminiSection["Model"] ?? "gemini-3.5-flash-lite";
            string baseUrl = geminiSection["BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/";

            try
            {
                var toolsPayload = GetGeminiToolsDeclaration();
                var contents = new List<object>
                {
                    new
                    {
                        role = "user",
                        parts = new object[] { new { text = userText } }
                    }
                };

                var requestBody = new
                {
                    contents = contents,
                    tools = toolsPayload,
                    tool_config = new
                    {
                        function_calling_config = new
                        {
                            mode = "AUTO"
                        }
                    }
                };

                string endpoint = $"{baseUrl}{model}:generateContent?key={apiKey}";
                var json = JsonSerializer.Serialize(requestBody);
                using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var response = await _httpClient.PostAsync(endpoint, httpContent, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var responseStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseStr);
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0 &&
                        candidates[0].TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var parts))
                    {
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("functionCall", out var funcCall))
                            {
                                string funcName = funcCall.GetProperty("name").GetString() ?? "";
                                var argsElem = funcCall.GetProperty("args");

                                switch (funcName)
                                {
                                    case "manage_draft_order":
                                    {
                                        var toolArgs = JsonSerializer.Deserialize<ManageDraftOrderToolArgs>(argsElem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        if (toolArgs != null && (toolArgs.Items.Count > 0 || toolArgs.Action == "clear"))
                                        {
                                            var payload = await ExecuteManageDraftOrderAsync(session, toolArgs, session.CustomerId ?? customerId);
                                            if (payload != null && payload.Items.Count > 0)
                                                return (true, "interactive_order", payload);
                                            if (payload != null && !string.IsNullOrEmpty(payload.StockWarning))
                                                return (true, "none", payload.StockWarning);
                                        }
                                        break;
                                    }
                                    case "lookup_order":
                                    {
                                        var toolArgs = JsonSerializer.Deserialize<LookupOrderToolArgs>(argsElem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        var ordDto = await LookupOrderAsync(toolArgs?.OrderCode, session.CustomerId ?? customerId);
                                        if (ordDto != null)
                                            return (true, "order_tracking", ordDto);
                                        break;
                                    }
                                    case "reorder_previous_order":
                                    {
                                        var reorder = await PrepareReOrderPayloadAsync(session.CustomerId ?? customerId);
                                        if (reorder != null && reorder.Items.Count > 0)
                                            return (true, "interactive_order", reorder);
                                        break;
                                    }
                                    case "get_active_promotions":
                                    {
                                        var promos = await LookupPromotionProductsAsync();
                                        if (promos.Count > 0)
                                            return (true, "product_cards", promos);
                                        break;
                                    }
                                    case "search_products":
                                    {
                                        var toolArgs = JsonSerializer.Deserialize<SearchProductsToolArgs>(argsElem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        if (!string.IsNullOrEmpty(toolArgs?.Keyword))
                                        {
                                            var prods = await SearchProductsAsync(toolArgs.Keyword);
                                            if (prods.Count > 0)
                                                return (true, "product_cards", prods);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback sang deterministic resolution
            }

            return (false, "none", null);
        }

        private async Task<string> GenerateGeminiResponseAsync(int sessionId, string userMessage, string payloadType, object? payloadObject)
        {
            var geminiSection = _config.GetSection("GeminiSettings");
            string apiKey = geminiSection["ApiKey"] ?? string.Empty;
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
   - Hiểu rõ quy chuẩn đơn vị: 'kg', 'kí', 'ký' = Kilogram; 'quả', 'trái' = Quả/Trái; 'hộp', 'thùng', 'gói', 'khay', 'vỉ'.
5. Vận chuyển Chuỗi Lạnh Chuyên Dụng (Solaris Cold-Chain Express - TMS 2°C - 8°C):
   - Đơn vị vận chuyển: Đội xe máy & xe tải thùng lạnh chuyên dụng của Solaris (Solaris Cold-Chain Express - TMS), cam kết duy trì dải nhiệt độ mát kiểm soát nghiêm ngặt 2°C - 8°C từ kho lạnh đến tận tay khách hàng. Tuyệt đối không nhắc đến GHN hay các đơn vị giao hàng thông thường khác.
   - Cước phí: Cước chuẩn 25.000 ₫; Đơn hàng có giá trị tiền hàng sau chiết khấu từ 300.000 ₫ trở lên được áp dụng MIỄN PHÍ VẬN CHUYỂN XE LẠNH (FREESHIP).
   - Rào chắn khoảng cách chuỗi lạnh (Cold-Chain Feasibility): Sản phẩm bảo quản lạnh (thịt, cá, hải sản, rau củ quả tươi sống) chỉ được giao trong bán kính tối đa của kho xe lạnh (thường là 15 km). Nếu địa chỉ nhận hàng của khách vượt quá bán kính này, hệ thống sẽ cảnh báo không giao được. Bạn BẮT BUỘC PHẢI thông báo rõ cho khách biết đích danh sản phẩm nào không giao được và nêu rõ lý do: ""Không đảm bảo dải nhiệt độ mát 2°C - 8°C trong thời gian vận chuyển xe máy/xe tải thùng lạnh dẫn đến nguy cơ suy giảm độ tươi ngon, an toàn vệ sinh thực phẩm"". Hướng dẫn khách đổi địa chỉ gần kho hơn hoặc chỉ mua sản phẩm đồ khô/bình thường.
   - Đổi trả hàng: Hỗ trợ đổi trả hoặc hoàn tiền 100% trong vòng 48 giờ nếu sản phẩm bị dập úng, hư hỏng trong quá trình vận chuyển.
   - Thanh toán: Hỗ trợ Cổng VNPay Sandbox (VNPAY-QR, Thẻ ATM/Visa/Mastercard) và Thanh toán khi nhận hàng (COD).
6. Khi khách muốn đặt mua hoặc lên đơn:
   - Trả lời ngắn gọn số lượng, đơn giá, tổng tiền, phí vận chuyển xe thùng lạnh TMS và thông báo rằng Thẻ Đơn Hàng Tương Tác đã xuất hiện ngay bên dưới.
   - Hướng dẫn khách: Chọn địa chỉ nhận hàng từ Sổ địa chỉ (hoặc bấm cập nhật địa chỉ nếu chưa có), tùy chỉnh số lượng [-] [+], chọn hình thức thanh toán (COD hoặc VNPay) và bấm nút 'Xác nhận đặt hàng' trên thẻ.
   - Sau khi khách hàng đã đặt hàng thành công (qua COD) hoặc thanh toán VNPay Sandbox thành công, bạn PHẢI gửi tin nhắn xác nhận: Mã đơn hàng ORD-... đã được đặt hàng thành công, trạng thái hiện tại là 'Chờ xử lý'. Đội ngũ kho đang chuẩn bị soạn hàng theo chuẩn FEFO (First-Expired, First-Out) và đóng gói vào xe máy/xe tải thùng lạnh chuyên dụng TMS (2°C - 8°C).
7. Cấu trúc danh mục 4 tầng & Luồng tư vấn 2 bước chuẩn:
   - Tầng 1: Nhóm Loại sản phẩm (Category Group) - ví dụ: Sản phẩm tươi sống, Thực phẩm chế biến,...
   - Tầng 2: Loại Sản phẩm (Category) - ví dụ: Sản phẩm từ động vật, Rau củ quả hữu cơ,...
   - Tầng 3: Sản phẩm khung (Product) - ví dụ: Thịt heo sạch, Cá hồi Na Uy, Bơ Sáp 034, Mì ăn liền,...
   - Tầng 4: Biến thể SKU (Product Variant) & Bảng giá quy cách bán (Product Variant Price).
   - LUỒNG TƯ VẤN 2 BƯỚC BẮT BUỘC:
     * Bước 1: Khi khách hỏi về Dòng sản phẩm (Tầng 3), hãy đề xuất các Biến thể hiện có kèm đặc điểm.
     * Bước 2: Khi khách chọn một Biến thể, hãy trình bày Quy cách bán và bảng giá theo 2 trường hợp cụ thể:
       + Cách 1 (Một biến thể = Một quy cách bán / Tách SKU riêng): Mỗi quy cách đóng gói là một mã tồn kho (SKU) độc lập. Ví dụ: Mì Hảo Hảo - Thùng (tồn kho đếm theo Thùng) và Mì Hảo Hảo - Gói (tồn kho đếm theo Gói). Thùng ra thùng, gói ra gói.
       + Cách 2 (Một biến thể có nhiều quy cách bán / Quy đổi ĐVT): Chỉ tạo một mã tồn kho (SKU) duy nhất lưu theo Đơn vị cơ sở (như Kg), nhưng thiết lập bảng giá cho phép bán theo nhiều đơn vị khác nhau (ví dụ: mua lẻ Kg giá gốc, mua Hộp 3kg hoặc Thùng 10kg có giá ưu đãi theo tỷ lệ quy đổi).
8. Tra cứu & kiểm tra đơn hàng realtime (kèm Chuyến xe TMS & Lý do kho từ chối / hủy):
   - Cho phép khách tra cứu bằng mã đơn hàng (ORD-...) công khai mà không bắt buộc đăng nhập.
   - Báo cáo tiến trình đơn hàng: Chờ duyệt -> Soạn hàng FEFO -> Đang trên chuyến xe giao hàng TMS -> Đã giao.
   - Nếu đơn hàng đang trên chuyến xe giao: Thông báo mã chuyến xe TMS, biển số xe, tên tài xế và số điện thoại liên hệ để khách tiện gọi nhận hàng.
   - NẾU ĐƠN HÀNG BỊ TỪ CHỐI HOẶC BỊ HỦY BỞI KHO: Trích xuất và giải thích rõ ràng lý do hủy của kho (Cancellation Reason), thể hiện sự thông cảm, xin lỗi chân thành và hướng dẫn khách giải pháp thay thế.
   - Nếu đơn hàng có yêu cầu đổi trả (RMA): Báo cáo mã đổi trả và số tiền hoàn (nếu có).
9. TẬP TRUNG TRẢ LỜI LƯỢT CHAT HIỆN TẠI (SINGLE-TURN FOCUS):
   - Bạn CHỈ ĐƯỢC PHÉP trả lời đúng yêu cầu mới nhất trong lượt chat hiện tại của khách hàng.
   - TUYỆT ĐỐI KHÔNG lặp lại câu từ chối, giải thích hoặc liệt kê danh mục cho các sản phẩm không kinh doanh (như xi măng, gạch đá, vật liệu xây dựng...) đã từng xuất hiện ở các lượt chat trước trong lịch sử hội thoại. Lượt trước đã giải thích xong là kết thúc; trong lượt hiện tại, nếu khách hỏi mua nông sản thực tế thì chỉ phục vụ và trả lời đúng món nông sản đó.

10. QUY TẮC PHẢN HỒI THỰC TẾ:
    - Khi khách hỏi tư vấn dòng sản phẩm, hãy dựa vào bảng danh mục thực tế bên dưới để trình bày biến thể và quy cách bán.
    - Tuyệt đối không tự bịa đặt giá tiền, số điện thoại, biển số xe hay tên tài xế ngoài dữ liệu do hệ thống cung cấp.

{liveCatalogSummary}";

            // Lấy lịch sử hội thoại trước đó (loại trừ message của user vừa được lưu để không bị lặp 2 turns user liên tiếp)
            var historyMessages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(1) // Bỏ qua userMsg vừa tạo
                .Take(6)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var contents = new List<object>();

            foreach (var m in historyMessages)
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
                string tripInfo = !string.IsNullOrEmpty(ord.DeliveryTripCode)
                    ? $" Chuyến xe TMS: {ord.DeliveryTripCode}, Xe: {ord.LicensePlate} ({(ord.IsColdChainVehicle == true ? "Thùng lạnh 2-8°C" : "Xe tiêu chuẩn")}), Tài xế: {ord.DriverName} (SĐT: {ord.DriverPhone}), Trạng thái chuyến: {ord.TripStatusName}."
                    : string.Empty;

                string cancelInfo = !string.IsNullOrEmpty(ord.CancellationReason)
                    ? $" ĐƠN BỊ TỪ CHỐI/HỦY BỞI KHO VỚI LÝ DO: \"{ord.CancellationReason}\". BẠN BẮT BUỘC PHẢI GIẢI THÍCH RÕ LÝ DO NÀY VÀ XIN LỖI CHÂN THÀNH."
                    : string.Empty;

                string rmaInfo = !string.IsNullOrEmpty(ord.ReturnCode)
                    ? $" Có yêu cầu đổi trả RMA: {ord.ReturnCode}, Hoàn tiền: {ord.RefundAmount:N0} ₫."
                    : string.Empty;

                extraContext = $"\n(Dữ liệu thực tế đơn hàng từ hệ thống: Mã đơn: {ord.OrderCode}, Ngày đặt: {ord.OrderDate:dd/MM/yyyy HH:mm}, Trạng thái đơn: {ord.StatusName}, Thanh toán: {ord.PaymentStatusName} qua {ord.PaymentMethodName}, Đơn vị vận chuyển: {ord.ShippingProvider}, Tổng tiền: {ord.TotalAmount:N0} ₫, Người nhận: {ord.ReceiverName}, SĐT: {ord.ReceiverPhone}, Địa chỉ: {ord.DeliveryAddress}, Danh sách sản phẩm: {string.Join(", ", ord.Items.Select(i => $"{i.VariantName} x {i.Quantity} {i.UoMName}"))}.{tripInfo}{cancelInfo}{rmaInfo} Hãy trả lời ngắn gọn, thẳng thắn và chính xác dựa trên dữ liệu trên, không suy diễn).";
            }
            else if (payloadType == "interactive_order" && payloadObject is InteractiveOrderPayloadDto orderPayload)
            {
                string stockNotice = !string.IsNullOrEmpty(orderPayload.StockWarning)
                    ? $"\nCẢNH BÁO TỒN KHO THỰC TẾ:\n{orderPayload.StockWarning}\nBẠN BẮT BUỘC PHẢI GIẢI THÍCH RÕ VỚI KHÁCH: Do kho hiện chỉ còn số lượng như trên nên hệ thống đã tự động điều chỉnh số lượng trên Thẻ Đơn Hàng xuống mức tồn kho tối đa."
                    : string.Empty;

                string coldChainNotice = !orderPayload.IsColdChainFeasible
                    ? $"\nCẢNH BÁO KHOẢNG CÁCH XE LẠNH TMS:\n{orderPayload.ColdChainWarning}\nSản phẩm không đủ điều kiện giao: {string.Join(", ", orderPayload.IneligibleColdChainItems)}\nBẠN BẮT BUỘC PHẢI NÊU ĐÍCH DANH CÁC SẢN PHẨM NÀY VÀ GIẢI THÍCH RÕ LÝ DO: Khoảng cách giao hàng vượt quá bán kính xe lạnh của kho, không đảm bảo dải nhiệt độ 2-8°C gây nguy cơ hỏng nông sản. Hướng dẫn khách đổi địa chỉ gần kho hơn hoặc bỏ sản phẩm lạnh khỏi đơn."
                    : string.Empty;

                extraContext = $"\n(Hệ thống đã tự động tính giá và hiển thị Thẻ Đơn Hàng Tương Tác ngay bên dưới tin nhắn này với các món: {string.Join(", ", orderPayload.Items.Select(i => $"{i.VariantName} x {i.Quantity} {i.UoMName} ({i.TotalPrice:N0}đ)"))}. Tổng tiền thanh toán: {orderPayload.TotalAmount:N0} ₫ (Phí vận chuyển xe thùng lạnh TMS: {(orderPayload.IsFreeShipping ? "Miễn phí 0đ" : $"{orderPayload.ShippingFee:N0}đ")}).{stockNotice}{coldChainNotice} Hãy thông báo ngắn gọn cho khách biết Thẻ Đơn Hàng Tương Tác đã xuất hiện ngay bên dưới, khách có thể chọn địa chỉ từ sổ địa chỉ, bấm nút [-] [+] để chỉnh số lượng, chọn thanh toán VNPay Sandbox hoặc COD và bấm nút 'Xác nhận đặt hàng' trên thẻ).";
            }
            else if (payloadType == "product_cards" && payloadObject is List<AiProductCardDto> prods && prods.Count > 0)
            {
                extraContext = "\n(Dữ liệu sản phẩm thực tế trong kho Solaris: " + string.Join("; ", prods.Select(p => $"{p.Name} (Mã biến thể {p.VariantId}): Giá {p.Price:N0}đ/{p.UoMName}, Khuyến mãi: {(p.DiscountedPrice < p.Price ? $"{p.DiscountedPrice:N0}đ" : "Không")}, Tồn kho: {(p.IsInStock ? "Còn hàng" : "Hết hàng")}{(!string.IsNullOrEmpty(p.Origin) ? $", Xuất xứ: {p.Origin}" : "")}{(!string.IsNullOrEmpty(p.Certification) ? $", Chứng nhận: {p.Certification}" : "")}")) + ". Tuân thủ luồng tư vấn 2 bước: hỏi dòng SP -> đề xuất biến thể -> trình bày quy cách bán Cách 1 hoặc Cách 2 dựa trên dữ liệu. Trả lời thẳng thắn, ngắn gọn. LƯU Ý: Đây là giao diện Thẻ Sản Phẩm (product_cards) để khách xem thông tin, KHÔNG PHẢI Thẻ Đơn Hàng Tương Tác. Tuyệt đối KHÔNG nói câu 'Thẻ Đơn Hàng Tương Tác đã xuất hiện' ở đây).";
            }
            else if (payloadType == "none")
            {
                if (payloadObject is string customWarning && !string.IsNullOrEmpty(customWarning))
                {
                    extraContext = customWarning.StartsWith("(HỆ THỐNG")
                        ? $"\n{customWarning}"
                        : $"\n(LƯU Ý TỒN KHO: {customWarning}. Bạn PHẢI trả lời thông báo rõ ràng cho khách biết sản phẩm đã hết hàng trong kho và tư vấn khách chọn các nông sản khác đang có sẵn trong kho).";
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

        #endregion
    }
}
