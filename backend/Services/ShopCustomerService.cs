using backend.Data;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ShopCustomerService : IShopCustomerService
    {
        private readonly SolarisDbContext _context;

        public ShopCustomerService(SolarisDbContext context)
        {
            _context = context;
        }

        public async Task<ShopCustomerProfileDto> GetProfileAsync(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerTier)
                .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            return new ShopCustomerProfileDto
            {
                Id = customer.Id,
                Code = customer.Code,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                Birthday = customer.Birthday,
                Gender = customer.Gender,
                AvatarPath = customer.AvatarPath,
                CustomerTierName = customer.CustomerTier?.Name ?? "Thành Viên",
                DiscountPercent = customer.CustomerTier?.DiscountPercent ?? 0,
                Addresses = customer.Addresses.Select(a => new ShopAddressDto
                {
                    Id = a.Id,
                    ReceiverName = a.ReceiverName,
                    Phone = a.Phone,
                    Province = a.Province,
                    District = a.District,
                    Ward = a.Ward,
                    StreetAddress = a.StreetAddress,
                    IsDefault = a.IsDefault,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude
                }).ToList()
            };
        }

        public async Task<ShopCustomerProfileDto> UpdateProfileAsync(int customerId, ShopCustomerProfileUpdateDto request)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerTier)
                .Include(c => c.Addresses.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            customer.Name = request.Name.Trim();
            customer.PhoneNumber = request.PhoneNumber.Trim();
            customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            customer.Birthday = request.Birthday;
            customer.Gender = request.Gender;
            if (!string.IsNullOrEmpty(request.AvatarPath))
                customer.AvatarPath = request.AvatarPath;

            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(customerId);
        }

        public async Task<List<ShopAddressDto>> GetAddressesAsync(int customerId)
        {
            var addresses = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId && !a.IsDeleted)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new ShopAddressDto
                {
                    Id = a.Id,
                    ReceiverName = a.ReceiverName,
                    Phone = a.Phone,
                    Province = a.Province,
                    District = a.District,
                    Ward = a.Ward,
                    StreetAddress = a.StreetAddress,
                    IsDefault = a.IsDefault,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude
                })
                .ToListAsync();

            return addresses;
        }

        public async Task<ShopAddressDto> AddAddressAsync(int customerId, ShopAddressCreateDto request)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);
            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy khách hàng.");

            var existingCount = await _context.CustomerAddresses.CountAsync(a => a.CustomerId == customerId && !a.IsDeleted);

            // Nếu là địa chỉ đầu tiên hoặc request yêu cầu default -> set default
            bool makeDefault = request.IsDefault || existingCount == 0;

            if (makeDefault)
            {
                var currentDefaults = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault && !a.IsDeleted)
                    .ToListAsync();
                foreach (var cd in currentDefaults)
                    cd.IsDefault = false;
            }

            var address = new CustomerAddress
            {
                CustomerId = customerId,
                ReceiverName = request.ReceiverName.Trim(),
                Phone = request.Phone.Trim(),
                Province = request.Province.Trim(),
                District = request.District.Trim(),
                Ward = request.Ward.Trim(),
                StreetAddress = request.StreetAddress.Trim(),
                IsDefault = makeDefault,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();

            return new ShopAddressDto
            {
                Id = address.Id,
                ReceiverName = address.ReceiverName,
                Phone = address.Phone,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                StreetAddress = address.StreetAddress,
                IsDefault = address.IsDefault,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };
        }

        public async Task<ShopAddressDto> UpdateAddressAsync(int customerId, int addressId, ShopAddressUpdateDto request)
        {
            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId && !a.IsDeleted);

            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            if (request.IsDefault && !address.IsDefault)
            {
                var currentDefaults = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault && !a.IsDeleted)
                    .ToListAsync();
                foreach (var cd in currentDefaults)
                    cd.IsDefault = false;
            }

            address.ReceiverName = request.ReceiverName.Trim();
            address.Phone = request.Phone.Trim();
            address.Province = request.Province.Trim();
            address.District = request.District.Trim();
            address.Ward = request.Ward.Trim();
            address.StreetAddress = request.StreetAddress.Trim();
            address.IsDefault = request.IsDefault;
            address.Latitude = request.Latitude;
            address.Longitude = request.Longitude;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ShopAddressDto
            {
                Id = address.Id,
                ReceiverName = address.ReceiverName,
                Phone = address.Phone,
                Province = address.Province,
                District = address.District,
                Ward = address.Ward,
                StreetAddress = address.StreetAddress,
                IsDefault = address.IsDefault,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };
        }

        public async Task<bool> DeleteAddressAsync(int customerId, int addressId)
        {
            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId && !a.IsDeleted);

            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            address.IsDeleted = true;
            address.DeletedAt = DateTime.UtcNow;

            // Nếu xóa địa chỉ mặc định, tự động chuyển 1 địa chỉ còn lại thành mặc định
            if (address.IsDefault)
            {
                var nextDefault = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.Id != addressId && !a.IsDeleted)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextDefault != null)
                    nextDefault.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetDefaultAddressAsync(int customerId, int addressId)
        {
            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId && !a.IsDeleted);

            if (address == null)
                throw new KeyNotFoundException("Không tìm thấy địa chỉ.");

            var currentDefaults = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsDefault && !a.IsDeleted)
                .ToListAsync();

            foreach (var cd in currentDefaults)
                cd.IsDefault = false;

            address.IsDefault = true;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
