using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.PurchaseOrderDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public PurchaseOrderService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<PurchaseOrderReadDto>> GetPagedAsync(
            string? search, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize)
        {
            var query = _context.PurchaseOrders
                .Include(x => x.Supplier)
                .Include(x => x.CreatedBy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower().Trim();
                query = query.Where(x => x.OrderCode.ToLower().Contains(lowerSearch) ||
                                         (x.Supplier != null && x.Supplier.Name.ToLower().Contains(lowerSearch)) ||
                                         (x.Note != null && x.Note.ToLower().Contains(lowerSearch)));
            }

            if (supplierId.HasValue)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            if (status.HasValue && Enum.IsDefined(typeof(PurchaseOrderStatus), status.Value))
                query = query.Where(x => x.Status == (PurchaseOrderStatus)status.Value);

            if (startDate.HasValue)
                query = query.Where(x => x.OrderDate >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.OrderDate < end);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<PurchaseOrderReadDto>>(items);

            return new PagedResult<PurchaseOrderReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<PurchaseOrderReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.PurchaseOrders
                .Include(x => x.Supplier)
                .Include(x => x.CreatedBy)
                .Include(x => x.Details).ThenInclude(d => d.Variant)
                .Include(x => x.Details).ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Đơn mua hàng.");

            return _mapper.Map<PurchaseOrderReadDto>(entity);
        }

        public async Task<int> CreateAsync(PurchaseOrderCreateDto dto)
        {
            if (dto.Details == null || !dto.Details.Any())
                throw new ArgumentException("Đơn mua hàng phải có ít nhất 1 mặt hàng.");

            // Kiểm tra NCC tồn tại
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId);
            if (!supplierExists)
                throw new ArgumentException($"Nhà cung cấp với ID {dto.SupplierId} không tồn tại.");

            // Kiểm tra Creator an toàn
            int creatorId = dto.CreatedById;
            var userExists = await _context.IAUsers.AnyAsync(u => u.Id == creatorId);
            if (!userExists)
            {
                var firstUser = await _context.IAUsers.FirstOrDefaultAsync();
                if (firstUser != null) creatorId = firstUser.Id;
            }

            var entity = _mapper.Map<PurchaseOrder>(dto);

            // Sinh mã duy nhất chống xung đột
            entity.OrderCode = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
            entity.Status = PurchaseOrderStatus.Draft;
            entity.CreatedById = creatorId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;

            decimal totalAmount = 0;
            foreach (var detail in entity.Details)
            {
                detail.TotalPrice = detail.OrderQuantity * detail.UnitPrice;
                detail.ReceivedQuantity = 0;
                totalAmount += detail.TotalPrice;
            }

            entity.TotalAmount = totalAmount;

            _context.PurchaseOrders.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PurchaseOrderCreateDto dto)
        {
            var entity = await _context.PurchaseOrders
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Đơn mua hàng.");

            if (entity.Status != PurchaseOrderStatus.Draft)
                throw new InvalidOperationException("Chỉ được phép chỉnh sửa đơn hàng đang ở trạng thái Nháp.");

            entity.SupplierId = dto.SupplierId;
            entity.OrderDate = dto.OrderDate;
            entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            entity.Note = dto.Note?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            // Xóa chi tiết cũ và gán chi tiết mới
            _context.PurchaseOrderDetails.RemoveRange(entity.Details);
            entity.Details.Clear();

            decimal totalAmount = 0;
            foreach (var d in dto.Details)
            {
                var detail = new PurchaseOrderDetail
                {
                    PurchaseOrderId = entity.Id,
                    VariantId = d.VariantId,
                    UoMId = d.UoMId,
                    OrderQuantity = d.OrderQuantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.OrderQuantity * d.UnitPrice,
                    ReceivedQuantity = 0
                };
                totalAmount += detail.TotalPrice;
                entity.Details.Add(detail);
            }

            entity.TotalAmount = totalAmount;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, PurchaseOrderUpdateDto dto)
        {
            var entity = await _context.PurchaseOrders.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Đơn mua hàng.");

            entity.Status = dto.Status;
            entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate ?? entity.ExpectedDeliveryDate;
            entity.Note = dto.Note ?? entity.Note;
            entity.UpdatedAt = DateTime.UtcNow;
            
            if (dto.Status == PurchaseOrderStatus.Cancelled)
            {
                entity.CancellationReason = dto.CancellationReason?.Trim();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.PurchaseOrders.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy Đơn mua hàng.");

            if (entity.Status != PurchaseOrderStatus.Draft)
                throw new InvalidOperationException("Chỉ được phép xóa đơn mua hàng đang ở trạng thái Nháp.");

            // Soft delete chuẩn mực
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
