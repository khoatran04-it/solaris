using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý hồ sơ Khách hàng và các liên kết nghiệp vụ (Nhóm, Hạng, Loại, Địa chỉ).
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        public async Task<IEnumerable<CustomerReadDto>> GetAllListAsync()
        {
            var customers = await _context.Customers
                .Include(c => c.CustomerType)
                .Include(c => c.CustomerTier)
                .Include(c => c.GroupLinks)
                    .ThenInclude(gl => gl.CustomerGroup)
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
                var lowerSearch = search.Trim().ToLower();
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
                query = query.Where(x => x.UpdatedAt.HasValue && x.UpdatedAt.Value >= startDate && x.UpdatedAt.Value < endDate);
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
                .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                .Include(c => c.GroupLinks)
                    .ThenInclude(gl => gl.CustomerGroup)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            var dto = _mapper.Map<CustomerReadDto>(customer);

            // 1. Tính tổng tiền đã chi tiêu (các đơn hàng hoàn tất hoặc đã thanh toán)
            var totalSpent = await _context.Orders
                .Where(o => o.CustomerId == id && o.Status == OrderStatus.Completed && !o.IsDeleted)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var totalOrders = await _context.Orders
                .Where(o => o.CustomerId == id && !o.IsDeleted)
                .CountAsync();

            dto.TotalSpent = totalSpent;
            dto.TotalOrders = totalOrders;
            dto.DiscountPercent = customer.CustomerTier?.DiscountPercent ?? 0m;

            // 2. Tính toán tiến độ nâng hạng thành viên (Tier Progress)
            var tiers = await _context.CustomerTiers
                .Where(t => t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.MinSpending)
                .ToListAsync();

            var currentMin = customer.CustomerTier?.MinSpending ?? 0m;
            var nextTier = tiers.FirstOrDefault(t => t.MinSpending > totalSpent);

            if (nextTier != null)
            {
                dto.NextTierName = nextTier.Name;
                dto.NextTierMinSpending = nextTier.MinSpending;
                dto.AmountToNextTier = Math.Max(0, nextTier.MinSpending - totalSpent);

                var span = nextTier.MinSpending - currentMin;
                dto.TierProgressPercent = span > 0
                    ? Math.Min(100m, Math.Max(0m, Math.Round((totalSpent - currentMin) / span * 100m, 1)))
                    : 100m;
            }
            else
            {
                // Đã đạt hạng cao nhất
                dto.NextTierName = null;
                dto.NextTierMinSpending = null;
                dto.AmountToNextTier = 0;
                dto.TierProgressPercent = 100m;
            }

            return dto;
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        public async Task<int> CreateAsync(CustomerCreateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trimmedCode = dto.Code.Trim();
                var trimmedPhone = dto.PhoneNumber.Trim();

                if (await _context.Customers.AnyAsync(c => c.Code.ToLower() == trimmedCode.ToLower()))
                    throw new InvalidOperationException($"Mã khách hàng '{trimmedCode}' đã tồn tại.");

                if (await _context.Customers.AnyAsync(c => c.PhoneNumber == trimmedPhone))
                    throw new InvalidOperationException($"Số điện thoại '{trimmedPhone}' đã được đăng ký cho khách hàng khác.");

                var newCustomer = _mapper.Map<Customer>(dto);
                newCustomer.Code = trimmedCode.ToUpper();
                newCustomer.Name = dto.Name.Trim();
                newCustomer.PhoneNumber = trimmedPhone;
                newCustomer.Email = dto.Email?.Trim();
                newCustomer.TaxCode = dto.TaxCode?.Trim();
                newCustomer.AvatarPath = dto.AvatarPath?.Trim();
                newCustomer.Birthday = dto.Birthday;
                newCustomer.Gender = dto.Gender;
                newCustomer.Note = dto.Note?.Trim();
                newCustomer.IsActive = dto.IsActive;
                newCustomer.CustomerTypeId = dto.CustomerTypeId;
                newCustomer.CustomerTierId = dto.CustomerTierId;
                newCustomer.CreatedAt = DateTime.UtcNow;
                newCustomer.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    newCustomer.Username = trimmedPhone;
                    newCustomer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
                }

                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();

                // Gán Nhóm khách hàng
                if (dto.GroupIds != null && dto.GroupIds.Any())
                {
                    var groupLinks = dto.GroupIds.Distinct().Select(groupId => new CustomerGroupLink
                    {
                        CustomerId = newCustomer.Id,
                        CustomerGroupId = groupId,
                        AssignedAt = DateTime.UtcNow
                    });

                    await _context.CustomerGroupLinks.AddRangeAsync(groupLinks);
                }

                // Gán Địa chỉ ban đầu (nếu có gửi kèm)
                if (dto.Addresses != null && dto.Addresses.Any())
                {
                    var isFirst = true;
                    foreach (var addrDto in dto.Addresses)
                    {
                        if (!string.IsNullOrWhiteSpace(addrDto.StreetAddress))
                        {
                            var addrEntity = _mapper.Map<CustomerAddress>(addrDto);
                            addrEntity.CustomerId = newCustomer.Id;
                            addrEntity.ReceiverName = addrDto.ReceiverName.Trim();
                            addrEntity.Phone = addrDto.Phone.Trim();
                            addrEntity.Province = addrDto.Province.Trim();
                            addrEntity.District = addrDto.District.Trim();
                            addrEntity.Ward = addrDto.Ward.Trim();
                            addrEntity.StreetAddress = addrDto.StreetAddress.Trim();
                            addrEntity.Latitude = addrDto.Latitude;
                            addrEntity.Longitude = addrDto.Longitude;
                            addrEntity.IsDefault = isFirst || addrDto.IsDefault;
                            addrEntity.CreatedAt = DateTime.UtcNow;
                            addrEntity.UpdatedAt = DateTime.UtcNow;
                            _context.CustomerAddresses.Add(addrEntity);
                            isFirst = false;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return newCustomer.Id;
            });
        }

        public async Task<bool> UpdateAsync(int id, CustomerUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var customer = await _context.Customers
                    .Include(c => c.GroupLinks)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

                var trimmedCode = dto.Code.Trim();
                var trimmedPhone = dto.PhoneNumber.Trim();

                if (await _context.Customers.AnyAsync(c => c.Id != id && c.Code.ToLower() == trimmedCode.ToLower()))
                    throw new InvalidOperationException($"Cập nhật thất bại: Mã khách hàng '{trimmedCode}' đã tồn tại.");

                if (await _context.Customers.AnyAsync(c => c.Id != id && c.PhoneNumber == trimmedPhone))
                    throw new InvalidOperationException($"Cập nhật thất bại: Số điện thoại '{trimmedPhone}' đã được đăng ký bởi khách hàng khác.");

                _mapper.Map(dto, customer);
                customer.Code = trimmedCode.ToUpper();
                customer.Name = dto.Name.Trim();
                customer.PhoneNumber = trimmedPhone;
                customer.Email = dto.Email?.Trim();
                customer.TaxCode = dto.TaxCode?.Trim();
                customer.AvatarPath = dto.AvatarPath?.Trim();
                customer.Birthday = dto.Birthday;
                customer.Gender = dto.Gender;
                customer.Note = dto.Note?.Trim();
                customer.IsActive = dto.IsActive;
                customer.CustomerTypeId = dto.CustomerTypeId;
                customer.CustomerTierId = dto.CustomerTierId;
                customer.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    customer.Username = trimmedPhone;
                    customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim());
                }

                // Quản lý liên kết Nhóm khách hàng (Xử lý an toàn với Soft Delete và Composite Key)
                var allExistingLinks = await _context.CustomerGroupLinks
                    .IgnoreQueryFilters()
                    .Where(gl => gl.CustomerId == customer.Id)
                    .ToListAsync();

                var newGroupIds = (dto.GroupIds ?? new List<int>()).Distinct().ToList();

                // 1. Soft delete những nhóm cũ không còn trong danh sách mới
                foreach (var link in allExistingLinks.Where(l => !l.IsDeleted && !newGroupIds.Contains(l.CustomerGroupId)))
                {
                    _context.CustomerGroupLinks.Remove(link);
                }

                // 2. Kích hoạt lại (nếu đã soft delete) hoặc thêm mới liên kết
                foreach (var groupId in newGroupIds)
                {
                    var existing = allExistingLinks.FirstOrDefault(l => l.CustomerGroupId == groupId);
                    if (existing != null)
                    {
                        if (existing.IsDeleted)
                        {
                            existing.IsDeleted = false;
                            existing.DeletedAt = null;
                            existing.AssignedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        var newLink = new CustomerGroupLink
                        {
                            CustomerId = customer.Id,
                            CustomerGroupId = groupId,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.CustomerGroupLinks.Add(newLink);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

                // Kiểm tra ràng buộc dữ liệu toàn vẹn
                bool hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id && !o.IsDeleted);
                if (hasOrders)
                    throw new InvalidOperationException("Không thể xóa khách hàng này vì đã có lịch sử Đơn Hàng trong hệ thống.");

                bool hasReturns = await _context.CustomerReturns.AnyAsync(cr => cr.CustomerId == id && !cr.IsDeleted);
                if (hasReturns)
                    throw new InvalidOperationException("Không thể xóa khách hàng này vì đã có lịch sử Phiếu Trả Hàng trong hệ thống.");

                _context.Customers.Remove(customer); // Soft Delete
                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null) throw new KeyNotFoundException("Không tìm thấy khách hàng.");

                customer.IsActive = !customer.IsActive;
                customer.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}