using AutoMapper;
using backend.Data;
using backend.DTOs.SupplierAddressDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý địa chỉ kho / giao nhận của nhà cung cấp.
    /// Đảm bảo luôn có ít nhất một địa chỉ mặc định cho mỗi nhà cung cấp.
    /// </summary>
    public class SupplierAddressService : ISupplierAddressService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierAddressService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Read Operations

        /// <summary>
        /// Lấy danh sách địa chỉ của một nhà cung cấp cụ thể.
        /// </summary>
        /// <remarks>Địa chỉ mặc định luôn được ưu tiên lên đầu danh sách.</remarks>
        public async Task<IEnumerable<SupplierAddressReadDto>> GetBySupplierIdAsync(int supplierId)
        {
            var addresses = await _context.SupplierAddresses
                .Where(a => a.SupplierId == supplierId)
                .AsNoTracking()
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierAddressReadDto>>(addresses);
        }

        /// <summary>
        /// Lấy chi tiết một địa chỉ theo ID.
        /// </summary>
        public async Task<SupplierAddressReadDto?> GetByIdAsync(int id)
        {
            var address = await _context.SupplierAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (address == null) return null;

            return _mapper.Map<SupplierAddressReadDto>(address);
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Thêm địa chỉ mới. Tự động xử lý trạng thái 'Mặc định' (IsDefault).
        /// </summary>
        public async Task<int> CreateAsync(int supplierId, SupplierAddressCreateDto dto)
        {
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == supplierId);
            if (!supplierExists) 
                throw new KeyNotFoundException("Không tìm thấy thông tin nhà cung cấp.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<SupplierAddress>(dto);
                entity.SupplierId = supplierId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // BUSINESS RULE: Xử lý Logic IsDefault
                var existingAddresses = await _context.SupplierAddresses
                    .Where(a => a.SupplierId == supplierId)
                    .ToListAsync();

                if (!existingAddresses.Any())
                {
                    // Nếu là địa chỉ đầu tiên, bắt buộc phải là mặc định
                    entity.IsDefault = true;
                }
                else if (dto.IsDefault)
                {
                    // Nếu địa chỉ mới được set làm mặc định, hủy trạng thái mặc định của các địa chỉ cũ
                    var currentDefault = existingAddresses.FirstOrDefault(a => a.IsDefault);
                    if (currentDefault != null) currentDefault.IsDefault = false;
                }

                _context.SupplierAddresses.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật địa chỉ. Đảm bảo tính nhất quán của trạng thái mặc định.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, SupplierAddressUpdateDto dto)
        {
            var entity = await _context.SupplierAddresses.FindAsync(id);
            if (entity == null) 
                throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // BUSINESS RULE: Luân chuyển quyền mặc định
                if (dto.IsDefault && !entity.IsDefault)
                {
                    // Chuyển địa chỉ này thành mặc định -> Tìm và tắt Default cũ
                    var currentDefault = await _context.SupplierAddresses
                        .Where(a => a.SupplierId == entity.SupplierId && a.IsDefault && a.Id != id)
                        .FirstOrDefaultAsync();

                    if (currentDefault != null) currentDefault.IsDefault = false;
                }
                else if (!dto.IsDefault && entity.IsDefault)
                {
                    // Cố tình tắt Default của địa chỉ đang là mặc định
                    var otherAddresses = await _context.SupplierAddresses
                       .Where(a => a.SupplierId == entity.SupplierId && a.Id != id)
                       .OrderBy(a => a.CreatedAt)
                       .ToListAsync();

                    if (!otherAddresses.Any())
                    {
                        // Nếu không còn địa chỉ nào khác, không cho phép tắt mặc định
                        dto.IsDefault = true;
                    }
                    else
                    {
                        // Nếu có địa chỉ khác, chọn địa chỉ cũ nhất làm mặc định thay thế
                        otherAddresses.First().IsDefault = true;
                    }
                }

                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <summary>
        /// Xóa địa chỉ. Nếu xóa địa chỉ mặc định, quyền này sẽ được chuyển giao cho địa chỉ khác.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.SupplierAddresses.FindAsync(id);
            if (entity == null) 
                throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // SAFETY LOGIC: Tránh trường hợp nhà cung cấp không có địa chỉ mặc định sau khi xóa
                if (entity.IsDefault)
                {
                    var nextAddress = await _context.SupplierAddresses
                        .Where(a => a.SupplierId == entity.SupplierId && a.Id != id)
                        .OrderBy(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (nextAddress != null)
                    {
                        nextAddress.IsDefault = true;
                    }

                    entity.IsDefault = false;
                }

                _context.SupplierAddresses.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <summary>
        /// Thủ công chỉ định một địa chỉ làm mặc định.
        /// </summary>
        public async Task<bool> SetDefaultAsync(int id, int supplierId)
        {
            var entity = await _context.SupplierAddresses.FindAsync(id);
            if (entity == null || entity.SupplierId != supplierId)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ hoặc địa chỉ không thuộc về nhà cung cấp này.");

            if (entity.IsDefault) return true;

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // Tắt Default hiện tại của nhà cung cấp này
                var currentDefault = await _context.SupplierAddresses
                    .Where(a => a.SupplierId == supplierId && a.IsDefault)
                    .FirstOrDefaultAsync();

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                }

                entity.IsDefault = true;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}