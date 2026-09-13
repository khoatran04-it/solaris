using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service xử lý Yêu cầu Đổi trả hàng cho Storefront B2C (Customer Self-Service RMA).
    /// Hỗ trợ khách hàng gửi yêu cầu trả hàng từ lịch sử mua hàng, tự động bóc tách lô xuất gốc,
    /// và tra cứu tiến độ kiểm định QC cùng số tiền hoàn trả.
    /// </summary>
    public class ShopReturnService : IShopReturnService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ShopReturnService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ShopReturnReadDto> CreateReturnRequestAsync(int customerId, ShopReturnCreateRequestDto request)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.InventoryIssues)
                    .ThenInclude(i => i.Details)
                        .ThenInclude(id => id.Batch)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == request.OrderCode.Trim() && !o.IsDeleted);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng tương ứng.");

            if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Shipping)
                throw new InvalidOperationException("Chỉ có thể yêu cầu trả hàng đối với đơn hàng đã hoặc đang giao.");

            // Kiểm tra thời hạn đổi trả trong vòng 12 giờ (Chính sách nông sản tươi sống)
            var referenceTime = order.DeliveredAt ?? order.OrderDate;
            if ((DateTime.UtcNow - referenceTime).TotalHours > 12)
            {
                throw new InvalidOperationException("Chính sách nông sản tươi Solaris chỉ hỗ trợ đổi/trả trong vòng 12 giờ kể từ khi nhận hàng. Đơn hàng này đã quá thời hạn 12 giờ.");
            }

            // Kiểm tra xem đơn hàng đã có yêu cầu đổi/trả đang xử lý hay chưa
            var existingReturn = await _context.CustomerReturns
                .FirstOrDefaultAsync(r => r.OrderId == order.Id && !r.IsDeleted && r.Status != CustomerReturnStatus.Rejected);
            if (existingReturn != null)
            {
                throw new InvalidOperationException($"Đơn hàng này đã có yêu cầu đổi/trả ({existingReturn.ReturnCode}) đang được xử lý.");
            }

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Vui lòng chọn ít nhất một sản phẩm cần đổi/trả.");

            // 1. Xác định kho tiếp nhận an toàn
            int safeWarehouseId = order.WarehouseId ?? 1;
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == safeWarehouseId && !w.IsDeleted);
            if (!warehouseExists)
            {
                var firstWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive && !w.IsDeleted);
                safeWarehouseId = firstWh?.Id ?? 1;
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var now = DateTime.UtcNow;
                string dateStr = DateTimeHelper.VietnamDateString;
                string randStr = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                string returnCode = $"RET-{dateStr}-{randStr}";

                var returnDetails = new List<CustomerReturnDetail>();
                decimal totalRefund = 0;

                foreach (var item in request.Items)
                {
                    if (item.ReturnedQuantity <= 0) continue;

                    var orderDetail = order.Details.FirstOrDefault(d => d.VariantId == item.VariantId);
                    decimal unitPrice = orderDetail?.UnitPrice ?? 0;

                    // Phân giải Lô hàng (BatchId) đa tầng an toàn tuyệt đối
                    int resolvedBatchId = await ResolveBatchIdAsync(order, item.VariantId, safeWarehouseId, item.BatchId);

                    // Phân giải Đơn vị tính (UoMId) an toàn
                    int resolvedUoMId = await ResolveUoMIdAsync(item.VariantId, item.UoMId, orderDetail?.UoMId ?? 0);

                    decimal refundAmount = item.ReturnedQuantity * unitPrice;
                    totalRefund += refundAmount;

                    returnDetails.Add(new CustomerReturnDetail
                    {
                        VariantId = item.VariantId,
                        BatchId = resolvedBatchId,
                        UoMId = resolvedUoMId,
                        ReturnedQuantity = item.ReturnedQuantity,
                        AcceptedQuantity = 0,
                        DamagedQuantity = 0,
                        UnitPrice = unitPrice,
                        RefundAmount = refundAmount,
                        RejectReason = item.Reason
                    });
                }

                var customerReturn = new CustomerReturn
                {
                    ReturnCode = returnCode,
                    OrderId = order.Id,
                    CustomerId = customerId,
                    WarehouseId = safeWarehouseId,
                    ReturnDate = now,
                    Status = CustomerReturnStatus.Pending,
                    ReturnType = CustomerReturnType.PostDeliveryReturn,
                    RefundAmount = totalRefund,
                    Reason = request.Reason?.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now,
                    Details = returnDetails
                };

                _context.CustomerReturns.Add(customerReturn);
                await _context.SaveChangesAsync();

                return await GetReturnByCodeInternalAsync(customerReturn.Id);
            });
        }

        public async Task<PagedResult<ShopReturnReadDto>> GetCustomerReturnsAsync(int customerId, int pageIndex = 1, int pageSize = 10)
        {
            var query = _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Variant)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Batch)
                .Include(r => r.Details)
                    .ThenInclude(d => d.UoM)
                .Where(r => r.CustomerId == customerId && !r.IsDeleted)
                .OrderByDescending(r => r.ReturnDate)
                .AsNoTracking()
                .AsQueryable();

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var returns = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = _mapper.Map<List<ShopReturnReadDto>>(returns);

            return new PagedResult<ShopReturnReadDto>
            {
                Items = items,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ShopReturnReadDto?> GetReturnByCodeAsync(int customerId, string returnCode)
        {
            var ret = await _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Variant)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Batch)
                .Include(r => r.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ReturnCode == returnCode.Trim() && !r.IsDeleted);

            if (ret == null)
                return null;

            return _mapper.Map<ShopReturnReadDto>(ret);
        }

        private async Task<ShopReturnReadDto> GetReturnByCodeInternalAsync(int returnId)
        {
            var ret = await _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Variant)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Batch)
                .Include(r => r.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstAsync(r => r.Id == returnId);

            return _mapper.Map<ShopReturnReadDto>(ret);
        }

        /// <summary>
        /// Phân giải BatchId an toàn đa tầng nhằm đảm bảo thỏa mãn khóa ngoại FK_CustomerReturnDetails_ProductBatches_BatchId:
        /// Tầng 1: Nếu BatchId được truyền lên hợp lệ và tồn tại trong DB, sử dụng trực tiếp.
        /// Tầng 2: Tìm trong InventoryIssues đã xuất kho của chính đơn hàng này.
        /// Tầng 3: Tìm trong InventoryIssueDetails của đơn hàng từ DB.
        /// Tầng 4: Tìm lô có tồn kho khả dụng lớn nhất tại kho tiếp nhận.
        /// Tầng 5: Tìm lô hàng gần nhất của biến thể (Variant).
        /// Tầng 6: Tìm bất kỳ lô hàng nào còn tồn tại trong hệ thống.
        /// Tầng 7: Tự động khởi tạo lô hàng hệ thống (System Fallback Batch) nếu hệ thống chưa từng có lô nào.
        /// </summary>
        private async Task<int> ResolveBatchIdAsync(Order order, int variantId, int safeWarehouseId, int requestedBatchId)
        {
            // Tầng 1: BatchId khách hoặc client truyền lên hợp lệ
            if (requestedBatchId > 0)
            {
                var batchExists = await _context.ProductBatches.AnyAsync(b => b.Id == requestedBatchId && !b.IsDeleted);
                if (batchExists) return requestedBatchId;
            }

            // Tầng 2: Tìm từ InventoryIssues của Order (nếu đã nạp)
            if (order.InventoryIssues != null)
            {
                foreach (var issue in order.InventoryIssues)
                {
                    var matchedDetail = issue.Details?.FirstOrDefault(d => d.VariantId == variantId && d.BatchId > 0);
                    if (matchedDetail != null)
                    {
                        return matchedDetail.BatchId;
                    }
                }
            }

            // Tầng 3: Truy vấn InventoryIssueDetails từ DB theo OrderId và VariantId
            var issueBatchId = await _context.InventoryIssueDetails
                .Where(iid => iid.InventoryIssue != null && iid.InventoryIssue.OrderId == order.Id && iid.VariantId == variantId && iid.BatchId > 0 && !iid.InventoryIssue.IsDeleted)
                .Select(iid => iid.BatchId)
                .FirstOrDefaultAsync();

            if (issueBatchId > 0)
            {
                return issueBatchId;
            }

            // Tầng 4: Tìm từ WarehouseInventories của kho tiếp nhận có tồn kho
            var inventoryBatchId = await _context.WarehouseInventories
                .Where(wi => wi.WarehouseId == safeWarehouseId && wi.VariantId == variantId && wi.BatchId > 0)
                .OrderByDescending(wi => wi.QuantityAvailable)
                .Select(wi => wi.BatchId)
                .FirstOrDefaultAsync();

            if (inventoryBatchId > 0)
            {
                return inventoryBatchId;
            }

            // Tầng 5: Tìm lô hàng của Biến thể sản phẩm (Variant)
            var variantBatchId = await _context.ProductBatches
                .Where(b => b.VariantId == variantId && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            if (variantBatchId > 0)
            {
                return variantBatchId;
            }

            // Tầng 6: Tìm bất kỳ lô hàng nào trong hệ thống
            var anyBatchId = await _context.ProductBatches
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            if (anyBatchId > 0)
            {
                return anyBatchId;
            }

            // Tầng 7: Tự sinh Lô hàng hệ thống (System Fallback Batch) để đảm bảo toàn vẹn dữ liệu
            int supplierId = await _context.Suppliers
                .Where(s => !s.IsDeleted)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (supplierId == 0)
            {
                var newSupplier = new Supplier
                {
                    Code = "SUP-SYSTEM",
                    Name = "Nhà Cung Cấp Hệ Thống",
                    Phone = "0900000000",
                    Email = "system@solaris.vn",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Suppliers.Add(newSupplier);
                await _context.SaveChangesAsync();
                supplierId = newSupplier.Id;
            }

            string dateCode = DateTimeHelper.VietnamDateString;
            string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            var fallbackBatch = new ProductBatch
            {
                BatchCode = $"BATCH-AUTO-{variantId}-{dateCode}-{randomSuffix}",
                VariantId = variantId,
                SupplierId = supplierId,
                ManufactureDate = DateTime.UtcNow.AddDays(-7),
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductBatches.Add(fallbackBatch);
            await _context.SaveChangesAsync();

            return fallbackBatch.Id;
        }

        /// <summary>
        /// Phân giải UoMId an toàn cho dòng trả hàng
        /// </summary>
        private async Task<int> ResolveUoMIdAsync(int variantId, int requestedUoMId, int orderDetailUoMId)
        {
            if (requestedUoMId > 0 && await _context.UoMs.AnyAsync(u => u.Id == requestedUoMId && !u.IsDeleted))
            {
                return requestedUoMId;
            }

            if (orderDetailUoMId > 0 && await _context.UoMs.AnyAsync(u => u.Id == orderDetailUoMId && !u.IsDeleted))
            {
                return orderDetailUoMId;
            }

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted);

            if (variant?.Product?.BaseUoMId > 0 && await _context.UoMs.AnyAsync(u => u.Id == variant.Product.BaseUoMId && !u.IsDeleted))
            {
                return variant.Product.BaseUoMId;
            }

            var firstUoM = await _context.UoMs.FirstOrDefaultAsync(u => !u.IsDeleted);
            return firstUoM?.Id ?? 1;
        }
    }
}
