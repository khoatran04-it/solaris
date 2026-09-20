using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Kho Hàng (Warehouse) và Hồ sơ Địa chỉ vật lý (WarehouseAddress).
    /// Đảm bảo tính toàn vẹn dữ liệu tồn kho, bọc toàn bộ mutations trong Database Transaction,
    /// chuẩn hóa tự động mã kho, kiểm soát quan hệ quản lý kho và thực thi 8 tầng khiên bảo vệ an toàn (Safety Shields).
    /// </summary>
    public class WarehouseService : IWarehouseService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        /// <summary>
        /// Khởi tạo WarehouseService với DbContext và AutoMapper.
        /// </summary>
        /// <param name="context">Database context của hệ thống Solaris.</param>
        /// <param name="mapper">Bộ ánh xạ AutoMapper.</param>
        public WarehouseService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách Kho hàng (không phân trang).
        /// Thường dùng cho các tính năng Lookup (Dropdown/Combobox) khi tạo Phiếu nhập/xuất kho, 
        /// điều chuyển hàng hóa, hoặc gán quyền truy cập kho cho nhân viên.
        /// </summary>
        /// <param name="isActiveOnly">Nếu true, chỉ lấy các kho đang hoạt động. Nếu false, lấy tất cả.</param>
        /// <param name="warehouseType">Tùy chọn lọc theo loại kho cụ thể.</param>
        /// <returns>Danh sách kho hàng kèm thông tin địa chỉ và trưởng kho.</returns>
        public async Task<IEnumerable<WarehouseReadDto>> GetAllListAsync(bool isActiveOnly = false, string? warehouseType = null, List<int>? allowedWarehouseIds = null)
        {
            var query = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsNoTracking()
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.Id));
            }

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(warehouseType))
            {
                query = query.Where(x => x.WarehouseType == warehouseType.Trim());
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<WarehouseReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách Kho hàng có hỗ trợ phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Tên hoặc Mã kho.</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm đóng).</param>
        /// <param name="province">Lọc kho hàng theo Khu vực / Tỉnh thành.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        /// <param name="allowedWarehouseIds">Danh sách các kho được phép truy cập theo quyền người dùng.</param>
        /// <returns>Kết quả phân trang chứa danh sách kho hàng và metadata.</returns>
        public async Task<PagedResult<WarehouseReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            string? province,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null)
        {
            var query = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsQueryable();

            // 0. Phân quyền kho được phép truy cập
            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                query = query.Where(x => allowedWarehouseIds.Contains(x.Id));
            }

            // 1. Lọc theo Search (Mã kho, Tên kho)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(searchLower) ||
                                         x.Name.ToLower().Contains(searchLower));
            }

            // 2. Lọc theo Trạng thái
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. Lọc theo Tỉnh/Thành phố
            if (!string.IsNullOrWhiteSpace(province))
            {
                var provinceTrimmed = province.Trim().ToLower();
                query = query.Where(x => x.Address != null && x.Address.Province.ToLower() == provinceTrimmed);
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<WarehouseReadDto>
            {
                Items = _mapper.Map<IEnumerable<WarehouseReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một Kho hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        /// <returns>Thông tin DTO của kho hoặc null nếu không tồn tại.</returns>
        public async Task<WarehouseReadDto?> GetByIdAsync(int id)
        {
            var entity = await _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.Manager)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return null;

            return _mapper.Map<WarehouseReadDto>(entity);
        }

        /// <summary>
        /// Lấy thông tin trạng thái sức chứa tức thời (CBM, Tải trọng kg, % lấp đầy) của kho hàng.
        /// </summary>
        public async Task<WarehouseCapacityStatusDto> GetCapacityStatusAsync(int id)
        {
            var warehouse = await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (warehouse == null)
                throw new KeyNotFoundException($"Không tìm thấy kho hàng với ID {id}.");

            decimal totalCapacityCbm = warehouse.TotalCapacityCbm ?? 500m; // Mặc định 500 CBM nếu chưa cấu hình
            decimal maxWeightCapacityKg = warehouse.MaxWeightCapacityKg ?? 100000m; // Mặc định 100 Tấn nếu chưa cấu hình

            var inventories = await _context.WarehouseInventories
                .Include(w => w.Variant)
                .Where(w => w.WarehouseId == id && (w.QuantityAvailable > 0 || w.QuantityReserved > 0 || w.QuantityQC > 0 || w.QuantityDamaged > 0))
                .AsNoTracking()
                .ToListAsync();

            decimal occupiedCbm = 0;
            decimal occupiedWeightKg = 0;

            foreach (var inv in inventories)
            {
                var totalQty = inv.QuantityAvailable + inv.QuantityReserved + inv.QuantityQC + inv.QuantityDamaged;
                var unitCbm = inv.Variant?.UnitCbm ?? (
                    (inv.Variant?.LengthCm > 0 && inv.Variant?.WidthCm > 0 && inv.Variant?.HeightCm > 0)
                        ? (inv.Variant.LengthCm.Value * inv.Variant.WidthCm.Value * inv.Variant.HeightCm.Value) / 1000000m
                        : 0.02m
                );
                var unitWeight = inv.Variant?.GrossWeightKg ?? 1m;

                occupiedCbm += totalQty * unitCbm;
                occupiedWeightKg += totalQty * unitWeight;
            }

            decimal availableCbm = Math.Max(0, totalCapacityCbm - occupiedCbm);
            decimal occupancyRateCbm = totalCapacityCbm > 0 ? Math.Round((occupiedCbm / totalCapacityCbm) * 100m, 2) : 0;

            decimal availableWeightKg = Math.Max(0, maxWeightCapacityKg - occupiedWeightKg);
            decimal occupancyRateWeight = maxWeightCapacityKg > 0 ? Math.Round((occupiedWeightKg / maxWeightCapacityKg) * 100m, 2) : 0;

            string status = "Safe";
            if (occupancyRateCbm >= 100 || occupancyRateWeight >= 100)
            {
                status = "Critical";
            }
            else if (occupancyRateCbm >= warehouse.WarningThresholdPercent || occupancyRateWeight >= warehouse.WarningThresholdPercent)
            {
                status = "Warning";
            }

            return new WarehouseCapacityStatusDto
            {
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                TotalCapacityCbm = totalCapacityCbm,
                OccupiedCbm = Math.Round(occupiedCbm, 2),
                AvailableCbm = Math.Round(availableCbm, 2),
                OccupancyRateCbm = occupancyRateCbm,
                MaxWeightCapacityKg = maxWeightCapacityKg,
                OccupiedWeightKg = Math.Round(occupiedWeightKg, 2),
                AvailableWeightKg = Math.Round(availableWeightKg, 2),
                OccupancyRateWeight = occupancyRateWeight,
                WarningThresholdPercent = warehouse.WarningThresholdPercent,
                Status = status
            };
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một Kho hàng và hồ sơ Địa chỉ vật lý trong một Database Transaction duy nhất.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới Kho hàng và Địa chỉ.</param>
        /// <returns>ID của kho hàng vừa được tạo thành công.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi mã/tên kho đã tồn tại, dữ liệu địa chỉ trống hoặc trưởng kho không tồn tại.</exception>
        public async Task<int> CreateAsync(WarehouseCreateDto dto)
        {
            var trimmedCode = dto.Code?.Trim() ?? string.Empty;
            var trimmedName = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedCode))
                throw new InvalidOperationException("Mã kho không được để trống.");

            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new InvalidOperationException("Tên kho không được để trống.");

            if (dto.Address == null ||
                string.IsNullOrWhiteSpace(dto.Address.Province) ||
                string.IsNullOrWhiteSpace(dto.Address.District) ||
                string.IsNullOrWhiteSpace(dto.Address.Ward) ||
                string.IsNullOrWhiteSpace(dto.Address.StreetAddress))
            {
                throw new InvalidOperationException("Thông tin địa chỉ kho không hợp lệ hoặc bị thiếu các trường bắt buộc.");
            }

            // Kiểm tra trùng Mã Kho (case-insensitive)
            if (await _context.Warehouses.AnyAsync(x => x.Code.ToLower() == trimmedCode.ToLower()))
                throw new InvalidOperationException($"Mã kho '{trimmedCode}' đã tồn tại trong hệ thống.");

            // Kiểm tra trùng Tên Kho (case-insensitive)
            if (await _context.Warehouses.AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower()))
                throw new InvalidOperationException($"Tên kho '{trimmedName}' đã tồn tại trong hệ thống.");

            // Kiểm tra Trưởng kho nếu có chỉ định
            if (dto.ManagerId.HasValue && dto.ManagerId.Value > 0)
            {
                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == dto.ManagerId.Value && !u.IsDeleted);
                if (!userExists)
                    throw new InvalidOperationException("Trưởng kho được chọn không tồn tại trong hệ thống.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<Warehouse>(dto);
                entity.Code = trimmedCode.ToUpper();
                entity.Name = trimmedName;
                entity.WarehouseType = dto.WarehouseType?.Trim();
                entity.ManagerId = (dto.ManagerId.HasValue && dto.ManagerId.Value > 0) ? dto.ManagerId : null;
                entity.IsActive = dto.IsActive;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entity.Address != null)
                {
                    entity.Address.Province = dto.Address.Province.Trim();
                    entity.Address.District = dto.Address.District.Trim();
                    entity.Address.Ward = dto.Address.Ward.Trim();
                    entity.Address.StreetAddress = dto.Address.StreetAddress.Trim();
                    entity.Address.Latitude = dto.Address.Latitude;
                    entity.Address.Longitude = dto.Address.Longitude;

                    // Tự động gán tọa độ mặc định nếu người dùng để trống hoặc nhập 0
                    if (entity.Address.Latitude == 0 && entity.Address.Longitude == 0)
                    {
                        var (defLat, defLng) = GetDefaultCoordinatesForDistrict(entity.Address.Province, entity.Address.District);
                        entity.Address.Latitude = defLat;
                        entity.Address.Longitude = defLng;
                    }

                    entity.Address.CreatedAt = DateTime.UtcNow;
                    entity.Address.UpdatedAt = DateTime.UtcNow;
                }

                _context.Warehouses.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin Kho hàng và đồng bộ thông tin Địa chỉ vật lý.
        /// </summary>
        /// <param name="id">Mã định danh của kho cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy dữ liệu kho hàng.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi tên kho bị trùng hoặc thông tin không hợp lệ.</exception>
        public async Task<bool> UpdateAsync(int id, WarehouseUpdateDto dto)
        {
            var trimmedName = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new InvalidOperationException("Tên kho không được để trống.");

            if (dto.Address == null ||
                string.IsNullOrWhiteSpace(dto.Address.Province) ||
                string.IsNullOrWhiteSpace(dto.Address.District) ||
                string.IsNullOrWhiteSpace(dto.Address.Ward) ||
                string.IsNullOrWhiteSpace(dto.Address.StreetAddress))
            {
                throw new InvalidOperationException("Thông tin địa chỉ kho không hợp lệ hoặc bị thiếu các trường bắt buộc.");
            }

            // Kiểm tra Trưởng kho nếu có chỉ định
            if (dto.ManagerId.HasValue && dto.ManagerId.Value > 0)
            {
                var userExists = await _context.IAUsers.AnyAsync(u => u.Id == dto.ManagerId.Value && !u.IsDeleted);
                if (!userExists)
                    throw new InvalidOperationException("Trưởng kho được chọn không tồn tại trong hệ thống.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.Warehouses
                    .Include(x => x.Address)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

                // Kiểm tra trùng Tên Kho (trừ chính nó)
                var isDuplicateName = await _context.Warehouses
                    .AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower());
                if (isDuplicateName)
                    throw new InvalidOperationException($"Cập nhật thất bại: Tên kho '{trimmedName}' đã bị trùng.");

                // 1. Map đè dữ liệu cơ bản của Kho
                _mapper.Map(dto, entity);
                entity.Name = trimmedName;
                entity.WarehouseType = dto.WarehouseType?.Trim();
                entity.ManagerId = (dto.ManagerId.HasValue && dto.ManagerId.Value > 0) ? dto.ManagerId : null;
                entity.IsActive = dto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                // 2. Cập nhật dữ liệu Address
                if (entity.Address != null)
                {
                    _mapper.Map(dto.Address, entity.Address);
                    entity.Address.Province = dto.Address.Province.Trim();
                    entity.Address.District = dto.Address.District.Trim();
                    entity.Address.Ward = dto.Address.Ward.Trim();
                    entity.Address.StreetAddress = dto.Address.StreetAddress.Trim();
                    entity.Address.Latitude = dto.Address.Latitude;
                    entity.Address.Longitude = dto.Address.Longitude;

                    // Tự động gán tọa độ mặc định nếu người dùng để trống hoặc nhập 0
                    if (entity.Address.Latitude == 0 && entity.Address.Longitude == 0)
                    {
                        var (defLat, defLng) = GetDefaultCoordinatesForDistrict(entity.Address.Province, entity.Address.District);
                        entity.Address.Latitude = defLat;
                        entity.Address.Longitude = defLng;
                    }

                    entity.Address.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa (mềm) một Kho hàng và hồ sơ Địa chỉ tương ứng sau khi kiểm tra nghiêm ngặt 8 tầng khiên bảo vệ (Safety Shields).
        /// </summary>
        /// <param name="id">Mã định danh của kho cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy kho hàng theo ID.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi kho còn tồn kho hoặc đã phát sinh chứng từ kế toán/logistic.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.Warehouses
                    .Include(x => x.Address)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

                // 1. SAFETY SHIELD: Chặn xóa nếu còn tồn kho
                var hasStock = await _context.WarehouseInventories
                    .AnyAsync(wi => wi.WarehouseId == id && (wi.QuantityAvailable + wi.QuantityReserved + wi.QuantityQC + wi.QuantityDamaged) > 0);
                if (hasStock)
                    throw new InvalidOperationException("Không thể xóa kho hàng này vì vẫn còn tồn kho sản phẩm.");

                // 2. SAFETY SHIELD: Chặn xóa nếu có Phiếu nhập kho
                var hasReceipts = await _context.InventoryReceipts.AnyAsync(ir => !ir.IsDeleted && ir.WarehouseId == id);
                if (hasReceipts)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu nhập kho.");

                // 3. SAFETY SHIELD: Chặn xóa nếu có Phiếu xuất kho
                var hasIssues = await _context.InventoryIssues.AnyAsync(ii => !ii.IsDeleted && ii.WarehouseId == id);
                if (hasIssues)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu xuất kho.");

                // 4. SAFETY SHIELD: Chặn xóa nếu có Phiếu chuyển kho
                var hasTransfers = await _context.InventoryTransfers
                    .AnyAsync(it => !it.IsDeleted && (it.FromWarehouseId == id || it.ToWarehouseId == id));
                if (hasTransfers)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu chuyển kho.");

                // 5. SAFETY SHIELD: Chặn xóa nếu có Phiếu kiểm kê
                var hasAudits = await _context.InventoryAudits.AnyAsync(ia => !ia.IsDeleted && ia.WarehouseId == id);
                if (hasAudits)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu kiểm kê.");

                // 6. SAFETY SHIELD: Chặn xóa nếu có Phiếu điều chỉnh
                var hasAdjustments = await _context.InventoryAdjustments.AnyAsync(ia => !ia.IsDeleted && ia.WarehouseId == id);
                if (hasAdjustments)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh dữ liệu phiếu điều chỉnh tồn kho.");

                // 7. SAFETY SHIELD: Chặn xóa nếu có Đơn hàng
                var hasOrders = await _context.Orders.AnyAsync(o => !o.IsDeleted && o.WarehouseId == id);
                if (hasOrders)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh đơn hàng bán xuất từ kho này.");

                // 8. SAFETY SHIELD: Chặn xóa nếu có Đơn trả hàng
                var hasReturns = await _context.CustomerReturns.AnyAsync(cr => !cr.IsDeleted && cr.WarehouseId == id);
                if (hasReturns)
                    throw new InvalidOperationException("Không thể xóa kho hàng vì đã phát sinh đơn trả hàng nhập vào kho này.");

                // Thực hiện Soft Delete cả Kho và Địa chỉ
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                if (entity.Address != null)
                {
                    entity.Address.IsDeleted = true;
                    entity.Address.DeletedAt = DateTime.UtcNow;
                    entity.Address.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm đóng của Kho hàng.
        /// </summary>
        /// <param name="id">Mã định danh của kho cần thao tác.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy kho hàng theo ID.</exception>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.Warehouses.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy dữ liệu kho hàng.");

                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// Ước tính tọa độ trung tâm cho Quận/Huyện phổ biến để tránh lỗi khoảng cách vô cực khi người dùng không nhập GPS.
        /// </summary>
        private static (double Lat, double Lng) GetDefaultCoordinatesForDistrict(string province, string district)
        {
            var p = province.ToLowerInvariant();
            var d = district.ToLowerInvariant();

            if (p.Contains("hồ chí minh") || p.Contains("hcm"))
            {
                if (d.Contains("quận 1") || d.Contains("quan 1")) return (10.7769, 106.7009);
                if (d.Contains("quận 3") || d.Contains("quan 3")) return (10.7828, 106.6853);
                if (d.Contains("quận 4") || d.Contains("quan 4")) return (10.7584, 106.7118);
                if (d.Contains("quận 5") || d.Contains("quan 5")) return (10.7540, 106.6667);
                if (d.Contains("quận 7") || d.Contains("quan 7")) return (10.7420, 106.6975);
                if (d.Contains("quận 10") || d.Contains("quan 10")) return (10.7716, 106.6669);
                if (d.Contains("bình thạnh") || d.Contains("binh thanh")) return (10.8030, 106.7100);
                if (d.Contains("tân bình") || d.Contains("tan binh")) return (10.8015, 106.6548);
                if (d.Contains("thủ đức") || d.Contains("thu duc")) return (10.8494, 106.7537);
                return (10.7769, 106.7009); // Mặc định trung tâm TP.HCM
            }

            if (p.Contains("hà nội") || p.Contains("ha noi"))
            {
                return (21.0285, 105.8542); // Trung tâm Hà Nội
            }

            if (p.Contains("đà nẵng") || p.Contains("da nang"))
            {
                return (16.0544, 108.2022); // Trung tâm Đà Nẵng
            }

            return (0, 0);
        }
        #endregion
    }
}