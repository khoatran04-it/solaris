using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerReturnDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class CustomerReturnService : ICustomerReturnService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerReturnService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<CustomerReturnReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Customer)
                .Include(r => r.Warehouse)
                .Include(r => r.ReceivedBy)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(r => allowedWarehouseIds.Contains(r.WarehouseId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r => r.ReturnCode.ToLower().Contains(s) ||
                                         r.Order.OrderCode.ToLower().Contains(s) ||
                                         r.Customer.Name.ToLower().Contains(s));
            }

            if (warehouseId.HasValue) query = query.Where(r => r.WarehouseId == warehouseId.Value);
            if (status.HasValue && Enum.IsDefined(typeof(CustomerReturnStatus), status.Value))
                query = query.Where(r => r.Status == (CustomerReturnStatus)status.Value);

            if (startDate.HasValue) query = query.Where(r => r.ReturnDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(r => r.ReturnDate < endDate.Value.Date.AddDays(1));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<CustomerReturnReadDto>
            {
                Items = _mapper.Map<IEnumerable<CustomerReturnReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<CustomerReturnReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.CustomerReturns
                .Include(r => r.Order)
                .Include(r => r.Customer)
                .Include(r => r.Warehouse)
                .Include(r => r.ReceivedBy)
                .Include(r => r.Details).ThenInclude(d => d.Variant)
                .Include(r => r.Details).ThenInclude(d => d.Batch)
                .Include(r => r.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(r => allowedWarehouseIds.Contains(r.WarehouseId));
            }

            var entity = await query.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");

            return _mapper.Map<CustomerReturnReadDto>(entity);
        }

        public async Task<int> CreateAsync(CustomerReturnCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu trả hàng phải có ít nhất 1 dòng chi tiết.");

            var ret = new CustomerReturn
            {
                ReturnCode = $"RET-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}",
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                WarehouseId = dto.WarehouseId,
                ReceivedById = dto.ReceivedById,
                ReturnDate = dto.ReturnDate ?? DateTime.UtcNow,
                Status = CustomerReturnStatus.Pending,
                Reason = dto.Reason
            };

            foreach (var item in dto.Details)
            {
                ret.Details.Add(new CustomerReturnDetail
                {
                    VariantId = item.VariantId,
                    BatchId = item.BatchId,
                    UoMId = item.UoMId,
                    ReturnedQuantity = item.ReturnedQuantity,
                    UnitPrice = item.UnitPrice ?? 0,
                    AcceptedQuantity = 0,
                    DamagedQuantity = 0,
                    RefundAmount = 0
                });
            }

            _context.CustomerReturns.Add(ret);
            await _context.SaveChangesAsync();

            return ret.Id;
        }

        public async Task<bool> InspectAndCompleteAsync(int id, int receivedById, CustomerReturnInspectionDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ret = await _context.CustomerReturns
                    .Include(r => r.Details)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (ret == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");
                if (ret.Status != CustomerReturnStatus.Pending && ret.Status != CustomerReturnStatus.Inspecting)
                    throw new InvalidOperationException("Phiếu trả hàng phải ở trạng thái Pending hoặc Inspecting mới có thể nghiệm thu.");

                decimal totalRefund = 0;

                // 1. Phân loại kiểm định từng dòng: Hàng tốt -> QuantityAvailable, Hàng hỏng -> QuantityDamaged
                foreach (var detail in ret.Details)
                {
                    var itemInspection = dto.Items.FirstOrDefault(i => i.DetailId == detail.Id);
                    if (itemInspection != null)
                    {
                        detail.AcceptedQuantity = itemInspection.AcceptedQuantity;
                        detail.DamagedQuantity = itemInspection.DamagedQuantity;
                        detail.RejectReason = itemInspection.RejectReason;
                        detail.RefundAmount = (detail.AcceptedQuantity + detail.DamagedQuantity) * detail.UnitPrice;
                        totalRefund += detail.RefundAmount;

                        var inv = await _context.WarehouseInventories
                            .FirstOrDefaultAsync(x => x.WarehouseId == ret.WarehouseId &&
                                                      x.VariantId == detail.VariantId &&
                                                      x.BatchId == detail.BatchId);

                        if (inv == null)
                        {
                            inv = new WarehouseInventory
                            {
                                WarehouseId = ret.WarehouseId,
                                VariantId = detail.VariantId,
                                BatchId = detail.BatchId,
                                QuantityAvailable = detail.AcceptedQuantity,
                                QuantityDamaged = detail.DamagedQuantity,
                                QuantityReserved = 0,
                                QuantityQC = 0
                            };
                            _context.WarehouseInventories.Add(inv);
                        }
                        else
                        {
                            inv.QuantityAvailable += detail.AcceptedQuantity;
                            inv.QuantityDamaged += detail.DamagedQuantity;
                        }

                        // Ghi sổ cái CustomerReturn
                        var totalReclaimed = detail.AcceptedQuantity + detail.DamagedQuantity;
                        if (totalReclaimed > 0)
                        {
                            _context.InventoryTransactions.Add(new InventoryTransaction
                            {
                                TransactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                                WarehouseId = ret.WarehouseId,
                                VariantId = detail.VariantId,
                                BatchId = detail.BatchId,
                                Type = TransactionType.CustomerReturn,
                                Quantity = totalReclaimed,
                                ReferenceCode = ret.ReturnCode,
                                Note = $"Khách trả hàng theo phiếu {ret.ReturnCode} (Đạt: {detail.AcceptedQuantity}, Lỗi: {detail.DamagedQuantity})",
                                CreatedById = receivedById
                            });
                        }
                    }
                }

                ret.Status = CustomerReturnStatus.Completed;
                ret.ReceivedById = receivedById;
                ret.RefundAmount = totalRefund;
                ret.InspectionNotes = dto.InspectionNotes;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectReturnAsync(int id, string reason)
        {
            var ret = await _context.CustomerReturns.FindAsync(id);
            if (ret == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");
            if (ret.Status == CustomerReturnStatus.Completed)
                throw new InvalidOperationException("Không thể từ chối Phiếu trả hàng đã hoàn tất.");

            ret.Status = CustomerReturnStatus.Rejected;
            ret.InspectionNotes = $"Từ chối nhận hàng: {reason}";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
