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
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.OrderCode.ToLower().Contains(lowerSearch));
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
            var entity = _mapper.Map<PurchaseOrder>(dto);

            // Generate OrderCode: PO-YYYYMMDD-HHMMSS
            entity.OrderCode = $"PO-{DateTime.UtcNow:yyyyMMdd}-{DateTime.UtcNow:HHmmss}";
            entity.Status = PurchaseOrderStatus.Draft;

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
                throw new Exception("Chỉ được phép chỉnh sửa đơn hàng đang ở trạng thái Nháp.");

            entity.SupplierId = dto.SupplierId;
            entity.OrderDate = dto.OrderDate;
            entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            entity.Note = dto.Note;

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

            // Chỉ cho phép update status, note, dates
            entity.Status = dto.Status;
            entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            entity.Note = dto.Note;
            
            if (dto.Status == PurchaseOrderStatus.Cancelled)
            {
                entity.CancellationReason = dto.CancellationReason;
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
                throw new Exception("Chỉ được phép xóa đơn hàng đang ở trạng thái Nháp.");

            _context.PurchaseOrders.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
