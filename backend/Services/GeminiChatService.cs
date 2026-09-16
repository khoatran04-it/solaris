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
                        .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && (wi.Batch == null || wi.Batch.ExpiryDate > now));

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
                        .Where(wi => wi.VariantId == variant.Id && wi.QuantityAvailable >= baseQty && (wi.Batch == null || wi.Batch.ExpiryDate > now));
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
                decimal shippingFee = netSubTotal >= 300000 ? 0 : (request.ShippingFee > 0 ? request.ShippingFee : 25000);
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
            bool isFree = netSubTotal >= 300000;
            decimal shippingFee = isFree ? 0 : 25000;

            // Kiểm tra rào chắn khoảng cách chuỗi lạnh (Cold-Chain Delivery Feasibility)
            bool isColdChainFeasible = true;
            var ineligibleColdItems = new List<string>();
            string? coldChainWarning = null;

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

            // Kiểm tra rào chắn khoảng cách chuỗi lạnh (Cold-Chain Delivery Feasibility)
            bool isColdChainFeasible = true;
            var ineligibleColdItems = new List<string>();
            string? coldChainWarning = null;

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
                SuggestedReceiverPhone = phone,
                IsColdChainFeasible = isColdChainFeasible,
                IneligibleColdChainItems = ineligibleColdItems,
                ColdChainWarning = coldChainWarning
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
                            sb.AppendLine($"       + SKU [ID:{v.Id}]: {v.Name} (Mã: {v.Code}) {modelTag} | Bảng giá: {pricingStr} | Tồn kho khả dụng: {(inStock ? $"{availableQty:G29} {baseUom} (Còn hàng)" : "0 (Tạm hết)")} | Xuất xứ: {origin ?? "Lâm Đồng"} | Tiêu chuẩn: {cert ?? "VietGAP"} | Độ ngọt: {(string.IsNullOrEmpty(brix) ? "Chuẩn vị" : $"{brix}°Bx")}{coldTag}{descSnippet}");
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
                "tìm", "kiếm", "muốn", "xem", "tư", "vấn", "mua", "lấy", "đặt", "chốt",
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

KỊCH BẢN MẪU (FEW-SHOT EXAMPLES):
- Khách: 'Tư vấn bơ sáp cho tôi'
  -> AI: 'Dạ Solaris có dòng Bơ Sáp 034 Đắk Lắk chuẩn VietGAP với các biến thể: Biến thể Bơ Sáp Loại 1 (Trái 300-500g). Về quy cách bán: Solaris áp dụng Bảng giá đa đơn vị (Cách 2): Bán lẻ 50.000 ₫/Kg, mua Hộp 3Kg giá ưu đãi 140.000 ₫/Hộp, mua Thùng 10Kg giá 450.000 ₫/Thùng. Tồn kho khả dụng hiện còn 50 Kg. Bạn muốn chọn quy cách nào ạ?'
- Khách: 'Shop có bán mì gói không?'
  -> AI: 'Dạ Solaris có dòng Mì Hảo Hảo với các biến thể tách SKU độc lập theo quy cách đóng gói (Cách 1): SKU Mì Hảo Hảo Gói 75g (4.500 ₫/gói, tồn kho: 120 gói) và SKU Mì Hảo Hảo Thùng 30 gói (130.000 ₫/thùng, tồn kho: 15 thùng). Bạn muốn lấy thùng hay gói lẻ ạ?'
- Khách: 'Kiểm tra đơn ORD-20260908-001 giúp tôi'
  -> AI: 'Dạ đơn hàng ORD-20260908-001 đang trên chuyến xe giao hàng lạnh TMS mã TRIP-001. Tài xế: Nguyễn Văn A (SĐT: 0912345678), xe thùng lạnh biển số 59A-12345. Nhiệt độ bảo quản xe duy trì 2-8°C. Quý khách vui lòng để ý điện thoại để nhận hàng nhé!'
- Khách: 'Tại sao đơn hàng ORD-20260908-002 của tôi bị hủy?'
  -> AI: 'Dạ đơn hàng ORD-20260908-002 đã bị kho từ chối/hủy với lý do: ""Địa chỉ giao hàng vượt quá bán kính bảo quản lạnh 15km của kho xe lạnh, không đảm bảo dải nhiệt độ 2-8°C"". Solaris rất xin lỗi quý khách về sự bất tiện này. Quý khách có thể đổi địa chỉ giao hàng gần kho hơn hoặc tham khảo các sản phẩm đồ khô không yêu cầu xe lạnh ạ.'

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
                extraContext = "\n(Dữ liệu sản phẩm thực tế trong kho Solaris: " + string.Join("; ", prods.Select(p => $"{p.Name} (Mã biến thể {p.VariantId}): Giá {p.Price:N0}đ/{p.UoMName}, Khuyến mãi: {(p.DiscountedPrice < p.Price ? $"{p.DiscountedPrice:N0}đ" : "Không")}, Tồn kho: {(p.IsInStock ? "Còn hàng" : "Hết hàng")}, Xuất xứ: {p.Origin ?? "Lâm Đồng"}, Chứng nhận: {p.Certification ?? "VietGAP"}")) + ". Tuân thủ luồng tư vấn 2 bước: hỏi dòng SP -> đề xuất biến thể -> trình bày quy cách bán Cách 1 hoặc Cách 2 dựa trên dữ liệu. Trả lời thẳng thắn, ngắn gọn).";
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
