using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerReturnDTOs;
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
    /// Service xử lý nghiệp vụ Quản lý Phiếu Khách Hàng Trả Hàng (Customer Return / RMA Engine).
    /// Chịu trách nhiệm toàn bộ quy trình thu hồi: Tiếp nhận yêu cầu, Phân quyền dữ liệu kho (Data Isolation),
    /// Kiểm định chất lượng (QC), Hạch toán hoàn tiền (Refund), và Điều hướng dòng tồn kho (Available vs Damaged).
    /// </summary>
    public class CustomerReturnService : ICustomerReturnService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerReturnService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn & Phân quyền Dữ liệu (Read & Data Isolation)
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
                var s = search.ToLower().Trim();
                query = query.Where(r => r.ReturnCode.ToLower().Contains(s) ||
                                         (r.Order != null && r.Order.OrderCode.ToLower().Contains(s)) ||
                                         (r.Customer != null && r.Customer.Name.ToLower().Contains(s)) ||
                                         (r.Reason != null && r.Reason.ToLower().Contains(s)));
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
        #endregion

        #region Khởi tạo Yêu cầu (RMA Initiation)
        public async Task<int> CreateAsync(CustomerReturnCreateDto dto, int? currentUserId = null)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Phiếu trả hàng phải có ít nhất 1 dòng chi tiết.");

            // 1. Kiểm tra đơn hàng gốc
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                throw new ArgumentException($"Đơn hàng với ID {dto.OrderId} không tồn tại.");

            // 2. Kiểm tra khách hàng
            var customerId = dto.CustomerId > 0 ? dto.CustomerId : order.CustomerId;
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists)
                throw new ArgumentException($"Khách hàng với ID {customerId} không tồn tại.");

            // 3. Kiểm tra kho tiếp nhận
            var warehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : (order.WarehouseId ?? 1);
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == warehouseId);
            if (!warehouseExists)
            {
                var firstWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive && !w.IsDeleted);
                warehouseId = firstWh?.Id ?? 1;
            }

            // 4. Kiểm tra User tiếp nhận an toàn
            int safeUserId = currentUserId ?? dto.ReceivedById ?? 1;
            var userExists = await _context.IAUsers.AnyAsync(u => u.Id == safeUserId);
            if (!userExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                safeUserId = firstUser?.Id ?? 1;
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = new CustomerReturn
                {
                    ReturnCode = $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    OrderId = dto.OrderId,
                    CustomerId = customerId,
                    WarehouseId = warehouseId,
                    ReceivedById = safeUserId,
                    ReturnDate = dto.ReturnDate ?? DateTime.UtcNow,
                    Status = CustomerReturnStatus.Pending,
                    Reason = dto.Reason?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
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
            });
        }
        #endregion

        #region Kiểm định & Hạch toán (QC & Fulfillment)
        public async Task<bool> InspectAndCompleteAsync(int id, int receivedById, CustomerReturnInspectionDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = await _context.CustomerReturns
                    .Include(r => r.Details)
                    .Include(r => r.Order).ThenInclude(o => o!.Details)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (ret == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");
                if (ret.Status != CustomerReturnStatus.Pending && ret.Status != CustomerReturnStatus.Inspecting)
                    throw new InvalidOperationException("Phiếu trả hàng phải ở trạng thái Pending hoặc Inspecting mới có thể nghiệm thu.");

                int safeUserId = receivedById;
                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == safeUserId);
                if (!userExists)
                {
                    var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                    safeUserId = firstUser?.Id ?? 1;
                }

                decimal totalRefund = 0;

                // 1. Phân loại kiểm định từng dòng: Hàng tốt -> QuantityAvailable, Hàng hỏng -> QuantityDamaged
                foreach (var detail in ret.Details)
                {
                    var itemInspection = dto.Items.FirstOrDefault(i => i.DetailId == detail.Id);
                    if (itemInspection != null)
                    {
                        detail.AcceptedQuantity = itemInspection.AcceptedQuantity;
                        detail.DamagedQuantity = itemInspection.DamagedQuantity;
                        detail.RejectReason = itemInspection.RejectReason?.Trim();
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
                                QuantityQC = 0,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.WarehouseInventories.Add(inv);
                        }
                        else
                        {
                            // Tăng tồn kho thực tế theo kết quả nghiệm thu QC
                            inv.QuantityAvailable += detail.AcceptedQuantity;
                            inv.QuantityDamaged += detail.DamagedQuantity;

                            // Giải phóng giữ chỗ nếu đơn hàng chưa từng thực hiện xuất kho (tránh bị kẹt giữ chỗ ảo)
                            if (ret.Order != null && (ret.Order.Status == OrderStatus.Confirmed || ret.Order.Status == OrderStatus.Processing))
                            {
                                var totalItemReclaimed = detail.AcceptedQuantity + detail.DamagedQuantity;
                                if (inv.QuantityReserved > 0 && totalItemReclaimed > 0)
                                {
                                    var releaseReserve = Math.Min(inv.QuantityReserved, totalItemReclaimed);
                                    inv.QuantityReserved -= releaseReserve;
                                }
                            }

                            inv.UpdatedAt = DateTime.UtcNow;
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
                                CreatedById = safeUserId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                // 2. Cập nhật trạng thái hoàn tiền trên Đơn hàng gốc
                if (ret.Order != null)
                {
                    ret.Order.PaymentStatus = PaymentStatus.Refunded;
                    ret.Order.UpdatedAt = DateTime.UtcNow;
                }

                ret.Status = CustomerReturnStatus.Completed;
                ret.ReceivedById = safeUserId;
                ret.RefundAmount = totalRefund;
                ret.InspectionNotes = dto.InspectionNotes?.Trim();
                ret.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> RejectReturnAsync(int id, string reason)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = await _context.CustomerReturns.FindAsync(id);
                if (ret == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");
                if (ret.Status == CustomerReturnStatus.Completed)
                    throw new InvalidOperationException("Không thể từ chối Phiếu trả hàng đã hoàn tất.");

                ret.Status = CustomerReturnStatus.Rejected;
                ret.InspectionNotes = $"Từ chối nhận hàng: {reason?.Trim()}";
                ret.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion

        #region Quản trị Hệ thống (Admin Options)
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var ret = await _context.CustomerReturns.FindAsync(id);
                if (ret == null) throw new KeyNotFoundException("Không tìm thấy Phiếu trả hàng.");

                if (ret.Status == CustomerReturnStatus.Completed)
                    throw new InvalidOperationException("Không thể xóa Phiếu trả hàng đã hoàn tất.");

                ret.IsDeleted = true;
                ret.DeletedAt = DateTime.UtcNow;
                ret.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }
        #endregion
    }
}
