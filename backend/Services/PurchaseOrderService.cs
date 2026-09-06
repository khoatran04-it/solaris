using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.PurchaseOrderDTOs;
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
    /// Service xử lý nghiệp vụ Quản lý Đơn Đặt Mua Hàng từ Nhà cung cấp (Purchase Orders - PO).
    /// Đảm bảo tính toàn vẹn giao dịch (Database Strategy), kiểm soát vòng đời trạng thái và làm căn cứ nhập kho.
    /// </summary>
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public PurchaseOrderService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Read Operations

        /// <inheritdoc />
        public async Task<IEnumerable<PurchaseOrderReadDto>> GetAllListAsync()
        {
            var items = await _context.PurchaseOrders
                .Include(x => x.Supplier)
                .Include(x => x.Warehouse)
                .Include(x => x.CreatedBy)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Variant)
                .Include(x => x.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PurchaseOrderReadDto>>(items);
        }

        /// <inheritdoc />
        public async Task<PagedResult<PurchaseOrderReadDto>> GetPagedAsync(
            string? search,
            int? supplierId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize)
        {
            var query = _context.PurchaseOrders
                .Include(x => x.Supplier)
                .Include(x => x.Warehouse)
                .Include(x => x.CreatedBy)
                .Include(x => x.Details)
                .AsQueryable();

            // 1. Tìm kiếm theo Mã đơn, Tên nhà cung cấp hoặc Ghi chú
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.OrderCode.ToLower().Contains(lowerSearch) ||
                                         (x.Supplier != null && x.Supplier.Name.ToLower().Contains(lowerSearch)) ||
                                         (x.Note != null && x.Note.ToLower().Contains(lowerSearch)));
            }

            // 2. Lọc theo Nhà cung cấp
            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            // 3. Lọc theo Trạng thái đơn hàng
            if (status.HasValue && Enum.IsDefined(typeof(PurchaseOrderStatus), status.Value))
                query = query.Where(x => x.Status == (PurchaseOrderStatus)status.Value);

            // 4. Lọc theo Khoảng thời gian lập đơn
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

        /// <inheritdoc />
        public async Task<PurchaseOrderReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.PurchaseOrders
                .Include(x => x.Supplier)
                .Include(x => x.Warehouse)
                .Include(x => x.CreatedBy)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Variant)
                .Include(x => x.Details)
                    .ThenInclude(d => d.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException($"Không tìm thấy Đơn đặt mua hàng với ID {id}.");

            return _mapper.Map<PurchaseOrderReadDto>(entity);
        }

        #endregion

        #region Write & Workflow Operations

        /// <inheritdoc />
        public async Task<int> CreateAsync(PurchaseOrderCreateDto dto, int currentUserId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // 1. Kiểm tra danh sách mặt hàng
                if (dto.Details == null || !dto.Details.Any())
                    throw new InvalidOperationException("Đơn đặt mua hàng phải có ít nhất 1 mặt hàng.");

                foreach (var detail in dto.Details)
                {
                    if (detail.OrderQuantity <= 0)
                        throw new InvalidOperationException("Số lượng đặt mua của từng mặt hàng phải lớn hơn 0.");

                    if (detail.UnitPrice < 0)
                        throw new InvalidOperationException("Đơn giá đặt mua không được là số âm.");

                    if (!await _context.ProductVariants.AnyAsync(v => v.Id == detail.VariantId && !v.IsDeleted))
                        throw new InvalidOperationException($"Biến thể sản phẩm (ID: {detail.VariantId}) không tồn tại hoặc đã bị xóa.");

                    if (!await _context.UoMs.AnyAsync(u => u.Id == detail.UoMId && !u.IsDeleted))
                        throw new InvalidOperationException($"Đơn vị tính (ID: {detail.UoMId}) không tồn tại hoặc đã bị xóa.");
                }

                // 2. Kiểm tra Nhà cung cấp hợp lệ
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);
                if (!supplierExists)
                    throw new InvalidOperationException($"Nhà cung cấp với ID {dto.SupplierId} không tồn tại hoặc đã bị xóa.");

                // 3. Xác định Nhân viên lập đơn hợp lệ
                int validCreatorId = currentUserId;
                if (validCreatorId <= 0 || !await _context.IAUsers.AnyAsync(u => u.Id == validCreatorId && !u.IsDeleted))
                {
                    var fallbackUser = await _context.IAUsers.FirstOrDefaultAsync(u => !u.IsDeleted);
                    validCreatorId = fallbackUser?.Id ?? 1;
                }

                // 3.1. Kiểm tra và gán Kho nhận hàng (Chỉ Kho Tổng mới được phép tiếp nhận đơn đặt mua từ NCC)
                int? targetWarehouseId = dto.WarehouseId;
                if (targetWarehouseId.HasValue && targetWarehouseId.Value > 0)
                {
                    var wh = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == targetWarehouseId.Value && !w.IsDeleted);
                    if (wh == null)
                        throw new InvalidOperationException($"Kho nhận hàng với ID {targetWarehouseId.Value} không tồn tại hoặc đã bị xóa.");

                    if (!string.IsNullOrWhiteSpace(wh.WarehouseType) && wh.WarehouseType != WarehouseTypeConstants.MasterHub)
                        throw new InvalidOperationException("Chỉ Kho Tổng (Master Hub) mới được phép tiếp nhận đơn đặt mua hàng từ Nhà cung cấp. Kho được chọn không phải là Kho Tổng.");
                }
                else
                {
                    // Tự động gán Kho Tổng đang hoạt động nếu có
                    var defaultMasterHub = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive && !w.IsDeleted && w.WarehouseType == WarehouseTypeConstants.MasterHub);
                    if (defaultMasterHub != null)
                    {
                        targetWarehouseId = defaultMasterHub.Id;
                    }
                }

                // 4. Ánh xạ sang Entity
                var entity = _mapper.Map<PurchaseOrder>(dto);

                // Sinh mã chứng từ nếu client không truyền
                if (string.IsNullOrWhiteSpace(dto.OrderCode))
                {
                    entity.OrderCode = $"PO-{DateTimeHelper.VietnamDateString}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                }
                else
                {
                    var trimmedCode = dto.OrderCode.Trim().ToUpper();
                    if (await _context.PurchaseOrders.AnyAsync(po => po.OrderCode.ToUpper() == trimmedCode))
                        throw new InvalidOperationException($"Mã đơn mua hàng '{trimmedCode}' đã tồn tại trong hệ thống.");
                    entity.OrderCode = trimmedCode;
                }

                entity.WarehouseId = targetWarehouseId;
                entity.Status = PurchaseOrderStatus.Draft;
                entity.CreatedById = validCreatorId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                // 5. Tính toán chi phí từng dòng và tổng đơn hàng
                decimal totalAmount = 0;
                entity.Details.Clear();

                foreach (var itemDto in dto.Details)
                {
                    var detailEntity = _mapper.Map<PurchaseOrderDetail>(itemDto);
                    detailEntity.TotalPrice = itemDto.OrderQuantity * itemDto.UnitPrice;
                    detailEntity.ReceivedQuantity = 0;

                    entity.Details.Add(detailEntity);
                    totalAmount += detailEntity.TotalPrice;
                }

                entity.TotalAmount = totalAmount;

                _context.PurchaseOrders.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(int id, PurchaseOrderCreateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PurchaseOrders
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException($"Không tìm thấy Đơn đặt mua hàng với ID {id}.");

                // SAFETY SHIELD: Chỉ cho phép chỉnh sửa nội dung khi đơn ở trạng thái Nháp
                if (entity.Status != PurchaseOrderStatus.Draft)
                    throw new InvalidOperationException("Chỉ được phép chỉnh sửa đơn đặt mua hàng khi đang ở trạng thái Nháp (Draft).");

                // 1. Kiểm tra danh sách mặt hàng
                if (dto.Details == null || !dto.Details.Any())
                    throw new InvalidOperationException("Đơn đặt mua hàng phải có ít nhất 1 mặt hàng.");

                foreach (var detail in dto.Details)
                {
                    if (detail.OrderQuantity <= 0)
                        throw new InvalidOperationException("Số lượng đặt mua của từng mặt hàng phải lớn hơn 0.");

                    if (detail.UnitPrice < 0)
                        throw new InvalidOperationException("Đơn giá đặt mua không được là số âm.");

                    if (!await _context.ProductVariants.AnyAsync(v => v.Id == detail.VariantId && !v.IsDeleted))
                        throw new InvalidOperationException($"Biến thể sản phẩm (ID: {detail.VariantId}) không tồn tại hoặc đã bị xóa.");

                    if (!await _context.UoMs.AnyAsync(u => u.Id == detail.UoMId && !u.IsDeleted))
                        throw new InvalidOperationException($"Đơn vị tính (ID: {detail.UoMId}) không tồn tại hoặc đã bị xóa.");
                }

                // 2. Kiểm tra Nhà cung cấp
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted);
                if (!supplierExists)
                    throw new InvalidOperationException($"Nhà cung cấp với ID {dto.SupplierId} không tồn tại hoặc đã bị xóa.");

                // 3. Cập nhật thông tin Header
                entity.SupplierId = dto.SupplierId;
                entity.OrderDate = dto.OrderDate;
                entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
                entity.Note = dto.Note?.Trim();

                if (dto.WarehouseId.HasValue && dto.WarehouseId.Value > 0)
                {
                    var wh = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId.Value && !w.IsDeleted);
                    if (wh == null)
                        throw new InvalidOperationException($"Kho nhận hàng với ID {dto.WarehouseId.Value} không tồn tại hoặc đã bị xóa.");

                    if (!string.IsNullOrWhiteSpace(wh.WarehouseType) && wh.WarehouseType != WarehouseTypeConstants.MasterHub)
                        throw new InvalidOperationException("Chỉ Kho Tổng (Master Hub) mới được phép tiếp nhận đơn đặt mua hàng từ Nhà cung cấp. Kho được chọn không phải là Kho Tổng.");

                    entity.WarehouseId = dto.WarehouseId.Value;
                }

                entity.UpdatedAt = DateTime.UtcNow;

                // 4. Đồng bộ lại danh sách chi tiết (Details)
                _context.PurchaseOrderDetails.RemoveRange(entity.Details);
                entity.Details.Clear();

                decimal totalAmount = 0;
                foreach (var itemDto in dto.Details)
                {
                    var detailEntity = _mapper.Map<PurchaseOrderDetail>(itemDto);
                    detailEntity.PurchaseOrderId = entity.Id;
                    detailEntity.TotalPrice = itemDto.OrderQuantity * itemDto.UnitPrice;
                    detailEntity.ReceivedQuantity = 0;

                    entity.Details.Add(detailEntity);
                    totalAmount += detailEntity.TotalPrice;
                }

                entity.TotalAmount = totalAmount;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatusUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PurchaseOrders
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException($"Không tìm thấy Đơn đặt mua hàng với ID {id}.");

                // SAFETY SHIELD: Không cho phép đổi trạng thái nếu đơn đã hoàn tất hoặc đã bị hủy
                if (entity.Status == PurchaseOrderStatus.Completed)
                    throw new InvalidOperationException("Không thể thay đổi trạng thái của đơn mua hàng đã Hoàn tất.");

                if (entity.Status == PurchaseOrderStatus.Cancelled)
                    throw new InvalidOperationException("Không thể thay đổi trạng thái của đơn mua hàng đã bị Hủy.");

                // SAFETY SHIELD: Nếu chuyển sang trạng thái Cancelled (Hủy đơn)
                if (dto.Status == PurchaseOrderStatus.Cancelled)
                {
                    if (string.IsNullOrWhiteSpace(dto.CancellationReason))
                        throw new InvalidOperationException("Vui lòng cung cấp lý do hủy đơn đặt mua hàng.");

                    // Chặn hủy nếu đã phát sinh số lượng nhận thực tế
                    var hasReceived = entity.Details.Any(d => d.ReceivedQuantity > 0) ||
                                      await _context.InventoryReceiptDetails.AnyAsync(ird => ird.PurchaseOrderDetail != null && ird.PurchaseOrderDetail.PurchaseOrderId == id && !ird.InventoryReceipt!.IsDeleted);
                    if (hasReceived)
                        throw new InvalidOperationException("Không thể hủy đơn mua hàng này vì đã phát sinh số lượng nhập kho thực tế.");

                    entity.CancellationReason = dto.CancellationReason.Trim();
                }

                entity.Status = dto.Status;
                if (dto.ExpectedDeliveryDate.HasValue)
                    entity.ExpectedDeliveryDate = dto.ExpectedDeliveryDate.Value;
                if (dto.Note != null)
                    entity.Note = dto.Note.Trim();

                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PurchaseOrders
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException($"Không tìm thấy Đơn đặt mua hàng với ID {id}.");

                // SAFETY SHIELD: Chỉ cho phép xóa đơn ở trạng thái Nháp hoặc Đã hủy
                if (entity.Status != PurchaseOrderStatus.Draft && entity.Status != PurchaseOrderStatus.Cancelled)
                    throw new InvalidOperationException("Chỉ được phép xóa đơn đặt mua hàng khi đang ở trạng thái Nháp (Draft) hoặc Đã hủy (Cancelled).");

                // SAFETY SHIELD: Chặn xóa nếu đã phát sinh nhập kho
                var hasReceived = entity.Details.Any(d => d.ReceivedQuantity > 0) ||
                                  await _context.InventoryReceiptDetails.AnyAsync(ird => ird.PurchaseOrderDetail != null && ird.PurchaseOrderDetail.PurchaseOrderId == id && !ird.InventoryReceipt!.IsDeleted);
                if (hasReceived)
                    throw new InvalidOperationException("Không thể xóa đơn mua hàng này vì đã có hàng hóa nhập kho liên kết.");

                // Xóa mềm (Soft Delete)
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}
