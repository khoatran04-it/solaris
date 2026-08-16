using AutoMapper;
using backend.Data;
using backend.DTOs.CustomerAddressDTOs;
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
        /// <remarks>Địa chỉ mặc định luôn được ưu tiên xếp lên đầu danh sách để thuận tiện cho UI/UX.</remarks>
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
            var address = await _context.CustomerAddresses.FindAsync(id);
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
        /// <remarks>Tự động thiết lập địa chỉ đầu tiên làm mặc định nếu khách hàng chưa có địa chỉ nào.</remarks>
        public async Task<int> CreateAsync(int customerId, CustomerAddressCreateDto dto)
        {
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            var entity = _mapper.Map<CustomerAddress>(dto);
            entity.CustomerId = customerId;

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
                var currentDefault = existingAddresses.FirstOrDefault(a => a.IsDefault);
                if (currentDefault != null) currentDefault.IsDefault = false;
            }

            _context.CustomerAddresses.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin địa chỉ và duy trì tính duy nhất của địa chỉ mặc định.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, CustomerAddressUpdateDto dto)
        {
            var entity = await _context.CustomerAddresses.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            // Xử lý Business Case: Thay đổi trạng thái mặc định
            if (dto.IsDefault && !entity.IsDefault)
            {
                // Case: Địa chỉ này muốn lên làm Default -> Tìm và hạ bệ Default cũ
                var currentDefault = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == entity.CustomerId && a.IsDefault && a.Id != id)
                    .FirstOrDefaultAsync();

                if (currentDefault != null) currentDefault.IsDefault = false;
            }
            else if (!dto.IsDefault && entity.IsDefault)
            {
                // Case: Cố tình tắt Default của địa chỉ đang là mặc định (Fix lỗi bốc hơi Default)
                var otherAddresses = await _context.CustomerAddresses
                   .Where(a => a.CustomerId == entity.CustomerId && a.Id != id)
                   .OrderBy(a => a.CreatedAt)
                   .ToListAsync();

                if (!otherAddresses.Any())
                {
                    // Nếu chỉ còn duy nhất 1 địa chỉ, không được phép tắt IsDefault
                    dto.IsDefault = true;
                }
                else
                {
                    // Chuyển quyền mặc định cho địa chỉ được tạo sớm nhất còn lại
                    otherAddresses.First().IsDefault = true;
                }
            }

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa địa chỉ. Nếu địa chỉ bị xóa đang là mặc định, quyền này sẽ được chuyển giao.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
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
                }

                // Note: Tước quyền IsDefault trước khi xóa để Interceptor Soft Delete lưu trạng thái sạch
                entity.IsDefault = false;
            }

            _context.CustomerAddresses.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Thủ công chỉ định một địa chỉ cụ thể làm mặc định cho khách hàng.
        /// </summary>
        public async Task<bool> SetDefaultAsync(int id, int customerId)
        {
            var entity = await _context.CustomerAddresses.FindAsync(id);
            if (entity == null || entity.CustomerId != customerId)
                throw new KeyNotFoundException("Địa chỉ không tồn tại hoặc không thuộc quyền sở hữu của khách hàng.");

            if (entity.IsDefault) return true; // Đã là mặc định, không cần xử lý thêm

            // Tìm và vô hiệu hóa Default hiện hành
            var currentDefault = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefault)
                .FirstOrDefaultAsync();

            if (currentDefault != null) currentDefault.IsDefault = false;

            entity.IsDefault = true;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}