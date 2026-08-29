using AutoMapper;
using backend.Data;
using backend.DTOs.CustomerAddressDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý hệ thống địa chỉ của khách hàng.
    /// Chịu trách nhiệm xử lý logic luân chuyển trạng thái 'Mặc định' (IsDefault).
    /// </summary>
    public class CustomerAddressService : ICustomerAddressService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerAddressService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách địa chỉ của một khách hàng.
        /// </summary>
        public async Task<IEnumerable<CustomerAddressReadDto>> GetByCustomerIdAsync(int customerId)
        {
            var addresses = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId)
                .AsNoTracking()
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerAddressReadDto>>(addresses);
        }

        /// <summary>
        /// Truy vấn chi tiết một địa chỉ cụ thể qua ID.
        /// </summary>
        public async Task<CustomerAddressReadDto?> GetByIdAsync(int id)
        {
            var address = await _context.CustomerAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (address == null) return null;

            return _mapper.Map<CustomerAddressReadDto>(address);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Thêm địa chỉ mới cho khách hàng.
        /// </summary>
        public async Task<int> CreateAsync(int customerId, CustomerAddressCreateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
                if (!customerExists) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

                var entity = _mapper.Map<CustomerAddress>(dto);
                entity.CustomerId = customerId;
                entity.ReceiverName = dto.ReceiverName.Trim();
                entity.Phone = dto.Phone.Trim();
                entity.Province = dto.Province.Trim();
                entity.District = dto.District.Trim();
                entity.Ward = dto.Ward.Trim();
                entity.StreetAddress = dto.StreetAddress.Trim();
                entity.Latitude = dto.Latitude;
                entity.Longitude = dto.Longitude;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // Logic: Kiểm tra danh sách hiện tại để xử lý cờ IsDefault
                var existingAddresses = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId)
                    .ToListAsync();

                if (!existingAddresses.Any())
                {
                    // Quy tắc 1: Địa chỉ đầu tiên khởi tạo BẮT BUỘC là mặc định
                    entity.IsDefault = true;
                }
                else if (dto.IsDefault)
                {
                    // Quy tắc 2: Nếu đặt địa chỉ mới làm mặc định, hủy trạng thái của địa chỉ cũ
                    foreach (var addr in existingAddresses.Where(a => a.IsDefault))
                    {
                        addr.IsDefault = false;
                        addr.UpdatedAt = DateTime.UtcNow;
                    }
                }

                _context.CustomerAddresses.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin địa chỉ và duy trì tính duy nhất của địa chỉ mặc định.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, CustomerAddressUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.CustomerAddresses.FindAsync(id);
                if (entity == null) throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

                // Xử lý Business Case: Thay đổi trạng thái mặc định
                if (dto.IsDefault && !entity.IsDefault)
                {
                    var currentDefaults = await _context.CustomerAddresses
                        .Where(a => a.CustomerId == entity.CustomerId && a.IsDefault && a.Id != id)
                        .ToListAsync();

                    foreach (var addr in currentDefaults)
                    {
                        addr.IsDefault = false;
                        addr.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (!dto.IsDefault && entity.IsDefault)
                {
                    var otherAddresses = await _context.CustomerAddresses
                       .Where(a => a.CustomerId == entity.CustomerId && a.Id != id)
                       .OrderBy(a => a.CreatedAt)
                       .ToListAsync();

                    if (!otherAddresses.Any())
                    {
                        dto.IsDefault = true;
                    }
                    else
                    {
                        var newDefault = otherAddresses.First();
                        newDefault.IsDefault = true;
                        newDefault.UpdatedAt = DateTime.UtcNow;
                    }
                }

                _mapper.Map(dto, entity);
                entity.ReceiverName = dto.ReceiverName.Trim();
                entity.Phone = dto.Phone.Trim();
                entity.Province = dto.Province.Trim();
                entity.District = dto.District.Trim();
                entity.Ward = dto.Ward.Trim();
                entity.StreetAddress = dto.StreetAddress.Trim();
                entity.Latitude = dto.Latitude;
                entity.Longitude = dto.Longitude;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <summary>
        /// Xóa địa chỉ. Nếu địa chỉ bị xóa đang là mặc định, quyền này sẽ được chuyển giao.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.CustomerAddresses.FindAsync(id);
                if (entity == null) throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

                // Bảo vệ luồng dữ liệu: Luân chuyển Default trước khi xóa thực tế
                if (entity.IsDefault)
                {
                    var nextAddress = await _context.CustomerAddresses
                        .Where(a => a.CustomerId == entity.CustomerId && a.Id != id)
                        .OrderBy(a => a.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (nextAddress != null)
                    {
                        nextAddress.IsDefault = true;
                        nextAddress.UpdatedAt = DateTime.UtcNow;
                    }

                    entity.IsDefault = false;
                }

                _context.CustomerAddresses.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <summary>
        /// Thủ công chỉ định một địa chỉ cụ thể làm mặc định cho khách hàng.
        /// </summary>
        public async Task<bool> SetDefaultAsync(int id, int customerId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.CustomerAddresses.FindAsync(id);
                if (entity == null || entity.CustomerId != customerId)
                    throw new KeyNotFoundException("Địa chỉ không tồn tại hoặc không thuộc quyền sở hữu của khách hàng.");

                if (entity.IsDefault) return true;

                var currentDefaults = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .ToListAsync();

                foreach (var addr in currentDefaults)
                {
                    addr.IsDefault = false;
                    addr.UpdatedAt = DateTime.UtcNow;
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