using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace backend.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CustomerReadDto>> GetAllListAsync()
        {
            var customers = await _context.Customers
                .Include(c => c.GroupLinks)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerReadDto>>(customers);
        }

        public async Task<PagedResult<CustomerReadDto>> GetPagedAsync(
            string? search,
            string? customerTypeId,
            string? customerTierId,
            string? customerGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Customers
                .Include(c => c.CustomerType)
                .Include(c => c.CustomerTier)
                .Include(c => c.GroupLinks)
                .ThenInclude(gl => gl.CustomerGroup)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch) ||
                    x.PhoneNumber.Contains(lowerSearch)
                );
            }

            // 1. Lọc Đa luồng: Loại Khách Hàng (Type)
            if (!string.IsNullOrWhiteSpace(customerTypeId))
            {
                var typeIdList = customerTypeId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (typeIdList.Any())
                {
                    query = query.Where(x => x.CustomerTypeId.HasValue && typeIdList.Contains(x.CustomerTypeId.Value));
                }
            }

            // 2. Lọc Đa luồng: Bậc Khách Hàng (Tier)
            if (!string.IsNullOrWhiteSpace(customerTierId))
            {
                var tierIdList = customerTierId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (tierIdList.Any())
                {
                    query = query.Where(x => x.CustomerTierId.HasValue && tierIdList.Contains(x.CustomerTierId.Value));
                }
            }

            // 3. Lọc Đa luồng: Nhóm Khách Hàng (Nhiều-Nhiều)
            if (!string.IsNullOrWhiteSpace(customerGroupId))
            {
                var groupIdList = customerGroupId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (groupIdList.Any())
                {
                    // Lấy khách hàng có ít nhất 1 Group nằm trong danh sách đang được tick lọc
                    query = query.Where(x => x.GroupLinks.Any(gl => groupIdList.Contains(gl.CustomerGroupId)));
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < endDate);
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<CustomerReadDto>>(items);

            return new PagedResult<CustomerReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<CustomerReadDto?> GetByIdAsync(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerType)
                .Include(c => c.CustomerTier)
                .Include(c => c.GroupLinks)
                .ThenInclude(gl => gl.CustomerGroup)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            return _mapper.Map<CustomerReadDto>(customer);
        }

        public async Task<int> CreateAsync(CustomerCreateDto dto)
        {
            if (await _context.Customers.AnyAsync(c => c.Code == dto.Code || c.PhoneNumber == dto.PhoneNumber))
                throw new Exception("Mã khách hàng hoặc Số điện thoại đã tồn tại.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newCustomer = _mapper.Map<Customer>(dto);

                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();

                if (dto.GroupIds != null && dto.GroupIds.Any())
                {
                    var groupLinks = dto.GroupIds.Select(groupId => new CustomerGroupLink
                    {
                        CustomerId = newCustomer.Id,
                        CustomerGroupId = groupId,
                        AssignedAt = DateTime.UtcNow
                    });

                    await _context.CustomerGroupLinks.AddRangeAsync(groupLinks);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return newCustomer.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("Có lỗi xảy ra khi lưu dữ liệu. Đã hoàn tác an toàn.");
            }
        }

        public async Task<bool> UpdateAsync(int id, CustomerUpdateDto dto)
        {
            var customer = await _context.Customers
                .Include(c => c.GroupLinks)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            bool isDuplicate = await _context.Customers.AnyAsync(c =>
                c.Id != id &&
                (c.Code == dto.Code || c.PhoneNumber == dto.PhoneNumber));

            if (isDuplicate)
                throw new Exception("Cập nhật thất bại: Mã khách hàng hoặc Số điện thoại đã bị trùng lặp.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _mapper.Map(dto, customer);
                _context.CustomerGroupLinks.RemoveRange(customer.GroupLinks);

                if (dto.GroupIds != null && dto.GroupIds.Any())
                {
                    var newLinks = dto.GroupIds.Select(groupId => new CustomerGroupLink
                    {
                        CustomerId = customer.Id,
                        CustomerGroupId = groupId,
                        AssignedAt = DateTime.UtcNow
                    });
                    await _context.CustomerGroupLinks.AddRangeAsync(newLinks);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("Cập nhật thất bại. Đã hoàn tác an toàn.");
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            _context.Customers.Remove(customer); // Interceptor sẽ hack thành Soft Delete
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            customer.IsActive = !customer.IsActive;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}