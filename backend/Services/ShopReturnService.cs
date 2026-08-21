using backend.Data;
using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ShopReturnService : IShopReturnService
    {
        private readonly SolarisDbContext _context;

        public ShopReturnService(SolarisDbContext context)
        {
            _context = context;
        }

        public async Task<ShopReturnReadDto> CreateReturnRequestAsync(int customerId, ShopReturnCreateRequestDto request)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                .Include(o => o.InventoryIssues.Where(i => i.Status == InventoryIssueStatus.Completed))
                    .ThenInclude(i => i.Details)
                        .ThenInclude(id => id.Batch)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderCode == request.OrderCode.Trim() && !o.IsDeleted);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng tương ứng.");

            if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Shipping)
                throw new InvalidOperationException("Chỉ có thể yêu cầu trả hàng đối với đơn hàng đã hoặc đang giao.");

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Vui lòng chọn ít nhất một sản phẩm cần đổi/trả.");

            var now = DateTime.UtcNow;
            string dateStr = now.ToString("yyyyMMdd");
            string randStr = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            string returnCode = $"RET-{dateStr}-{randStr}";

            var returnDetails = new List<CustomerReturnDetail>();
            decimal totalRefund = 0;

            foreach (var item in request.Items)
            {
                if (item.ReturnedQuantity <= 0) continue;

                var orderDetail = order.Details.FirstOrDefault(d => d.VariantId == item.VariantId);
                decimal unitPrice = orderDetail?.UnitPrice ?? 0;

                // Tìm BatchId từ các phiếu xuất kho đã hoàn tất
                int resolvedBatchId = item.BatchId;
                if (resolvedBatchId <= 0)
                {
                    var issuedDetail = order.InventoryIssues
                        .SelectMany(i => i.Details)
                        .FirstOrDefault(id => id.VariantId == item.VariantId);

                    resolvedBatchId = issuedDetail?.BatchId ?? 0;
                }

                decimal refundAmount = item.ReturnedQuantity * unitPrice;
                totalRefund += refundAmount;

                returnDetails.Add(new CustomerReturnDetail
                {
                    VariantId = item.VariantId,
                    BatchId = resolvedBatchId,
                    UoMId = item.UoMId,
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
                WarehouseId = order.WarehouseId ?? 1,
                ReturnDate = now,
                Status = CustomerReturnStatus.Pending,
                RefundAmount = totalRefund,
                Reason = request.Reason?.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                Details = returnDetails
            };

            _context.CustomerReturns.Add(customerReturn);
            await _context.SaveChangesAsync();

            return await GetReturnByCodeInternalAsync(customerReturn.Id);
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
                .AsQueryable();

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var returns = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = returns.Select(r => MapToShopReturnDto(r)).ToList();

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
                .FirstOrDefaultAsync(r => r.CustomerId == customerId && r.ReturnCode == returnCode.Trim() && !r.IsDeleted);

            if (ret == null)
                return null;

            return MapToShopReturnDto(ret);
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
                .FirstAsync(r => r.Id == returnId);

            return MapToShopReturnDto(ret);
        }

        private ShopReturnReadDto MapToShopReturnDto(CustomerReturn r)
        {
            return new ShopReturnReadDto
            {
                Id = r.Id,
                ReturnCode = r.ReturnCode,
                OrderCode = r.Order?.OrderCode ?? string.Empty,
                ReturnDate = r.ReturnDate,
                Status = r.Status,
                StatusName = GetReturnStatusName(r.Status),
                RefundAmount = r.RefundAmount,
                Reason = r.Reason,
                InspectionNotes = r.InspectionNotes,
                Details = r.Details.Select(d => new ShopReturnItemReadDto
                {
                    VariantId = d.VariantId,
                    VariantName = d.Variant?.Name ?? string.Empty,
                    VariantCode = d.Variant?.Code ?? string.Empty,
                    BatchCode = d.Batch?.BatchCode,
                    UoMName = d.UoM?.Name ?? string.Empty,
                    ReturnedQuantity = d.ReturnedQuantity,
                    AcceptedQuantity = d.AcceptedQuantity,
                    DamagedQuantity = d.DamagedQuantity,
                    RefundAmount = d.RefundAmount,
                    RejectReason = d.RejectReason
                }).ToList()
            };
        }

        private string GetReturnStatusName(CustomerReturnStatus status) => status switch
        {
            CustomerReturnStatus.Pending => "Chờ tiếp nhận",
            CustomerReturnStatus.Inspecting => "Đang kiểm định QC",
            CustomerReturnStatus.Completed => "Đã hoàn tất & hoàn tiền",
            CustomerReturnStatus.Rejected => "Từ chối trả hàng",
            _ => status.ToString()
        };
    }
}
