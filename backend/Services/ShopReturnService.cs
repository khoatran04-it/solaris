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
    }
}
