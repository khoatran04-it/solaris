using backend.Data;
using backend.DTOs.ShopDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Services
{
    public class ShopAuthService : IShopAuthService
    {
        private readonly SolarisDbContext _context;
        private readonly IConfiguration _config;

        public ShopAuthService(SolarisDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<ShopAuthResponseDto> RegisterAsync(ShopRegisterRequestDto request)
        {
            // 1. Kiểm tra tài khoản đã tồn tại hay chưa
            string normalizedUsername = request.PhoneNumber.Trim();

            var existingPhone = await _context.Customers
                .Include(c => c.CustomerTier)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.PhoneNumber == normalizedUsername || c.Username == normalizedUsername);

            if (existingPhone != null)
            {
                // Trường hợp 1: Tài khoản đã có mật khẩu online
                if (!string.IsNullOrEmpty(existingPhone.PasswordHash))
                {
                    throw new InvalidOperationException("Số điện thoại này đã được đăng ký tài khoản trực tuyến. Vui lòng đăng nhập.");
                }

                // Trường hợp 2: Khách hàng đã có hồ sơ (từ Admin/Cửa hàng) nhưng chưa có tài khoản trực tuyến -> Kích hoạt tài khoản online
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var existingEmail = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Id != existingPhone.Id && (c.Email == request.Email.Trim() || c.Username == request.Email.Trim()));
                    if (existingEmail != null)
                        throw new InvalidOperationException("Email này đã được sử dụng bởi một tài khoản khác.");

                    existingPhone.Email = request.Email.Trim();
                }

                existingPhone.Name = request.FullName.Trim();
                existingPhone.Username = normalizedUsername;
                existingPhone.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingPhone.IsActive = true;
                existingPhone.UpdatedAt = DateTime.UtcNow;

                // Nếu có địa chỉ ban đầu thì lưu
                if (!string.IsNullOrWhiteSpace(request.Province) && !string.IsNullOrWhiteSpace(request.StreetAddress))
                {
                    existingPhone.Addresses.Add(new CustomerAddress
                    {
                        ReceiverName = request.FullName.Trim(),
                        Phone = normalizedUsername,
                        Province = request.Province.Trim(),
                        District = request.District?.Trim() ?? string.Empty,
                        Ward = request.Ward?.Trim() ?? string.Empty,
                        StreetAddress = request.StreetAddress.Trim(),
                        IsDefault = true,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                string activeToken = GenerateCustomerToken(existingPhone, existingPhone.CustomerTier?.Name ?? "Thành Viên", existingPhone.CustomerTier?.DiscountPercent ?? 0);

                return new ShopAuthResponseDto
                {
                    Token = activeToken,
                    CustomerInfo = new ShopCustomerInfoDto
                    {
                        Id = existingPhone.Id,
                        Code = existingPhone.Code,
                        Name = existingPhone.Name,
                        PhoneNumber = existingPhone.PhoneNumber,
                        Email = existingPhone.Email,
                        AvatarPath = existingPhone.AvatarPath,
                        CustomerTierId = existingPhone.CustomerTierId,
                        CustomerTierName = existingPhone.CustomerTier?.Name ?? "Thành Viên",
                        DiscountPercent = existingPhone.CustomerTier?.DiscountPercent ?? 0
                    }
                };
            }

            // Trường hợp 3: Khách hàng mới hoàn toàn
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingEmail = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == request.Email.Trim() || c.Username == request.Email.Trim());
                if (existingEmail != null)
                    throw new InvalidOperationException("Email này đã được sử dụng bởi một tài khoản khác.");
            }

            // 2. Tìm CustomerType mặc định (ví dụ: Khách lẻ)
            var defaultType = await _context.CustomerTypes
                .FirstOrDefaultAsync(t => t.IsActive && (t.Code == "LE" || t.Code == "RETAIL"))
                ?? await _context.CustomerTypes.FirstOrDefaultAsync(t => t.IsActive);

            // 3. Tìm CustomerTier khởi đầu (Đồng / Khách mới)
            var defaultTier = await _context.CustomerTiers
                .OrderBy(t => t.MinSpending)
                .FirstOrDefaultAsync(t => t.IsActive);

            // 4. Sinh mã khách hàng CUST-YYYYMMDD-XXXX
            string todayStr = DateTimeHelper.VietnamDateString;
            string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
            string customerCode = $"CUST-{todayStr}-{randomSuffix}";

            var customer = new Customer
            {
                Code = customerCode,
                Name = request.FullName.Trim(),
                PhoneNumber = normalizedUsername,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CustomerTypeId = defaultType?.Id,
                CustomerTierId = defaultTier?.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Nếu có địa chỉ ban đầu thì lưu luôn
            if (!string.IsNullOrWhiteSpace(request.Province) && !string.IsNullOrWhiteSpace(request.StreetAddress))
            {
                customer.Addresses.Add(new CustomerAddress
                {
                    ReceiverName = request.FullName.Trim(),
                    Phone = normalizedUsername,
                    Province = request.Province.Trim(),
                    District = request.District?.Trim() ?? string.Empty,
                    Ward = request.Ward?.Trim() ?? string.Empty,
                    StreetAddress = request.StreetAddress.Trim(),
                    IsDefault = true,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // 5. Tạo JWT Token & trả về
            string token = GenerateCustomerToken(customer, defaultTier?.Name ?? "Thành Viên", defaultTier?.DiscountPercent ?? 0);

            return new ShopAuthResponseDto
            {
                Token = token,
                CustomerInfo = new ShopCustomerInfoDto
                {
                    Id = customer.Id,
                    Code = customer.Code,
                    Name = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    Email = customer.Email,
                    AvatarPath = customer.AvatarPath,
                    CustomerTierId = customer.CustomerTierId,
                    CustomerTierName = defaultTier?.Name ?? "Thành Viên Mới",
                    DiscountPercent = defaultTier?.DiscountPercent ?? 0
                }
            };
        }

        public async Task<ShopAuthResponseDto> LoginAsync(ShopLoginRequestDto request)
        {
            string loginIdentifier = request.Username.Trim();

            // Tìm theo Username, Email hoặc PhoneNumber
            var customer = await _context.Customers
                .Include(c => c.CustomerTier)
                .FirstOrDefaultAsync(c => c.Username == loginIdentifier || c.Email == loginIdentifier || c.PhoneNumber == loginIdentifier);

            if (customer == null || !customer.IsActive)
                throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");

            if (string.IsNullOrEmpty(customer.PasswordHash))
                throw new UnauthorizedAccessException("Tài khoản này chưa thiết lập mật khẩu đăng nhập trực tuyến. Vui lòng liên hệ hỗ trợ hoặc dùng chức năng Quên mật khẩu.");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Mật khẩu không chính xác.");

            string tierName = customer.CustomerTier?.Name ?? "Thành Viên";
            decimal discountPercent = customer.CustomerTier?.DiscountPercent ?? 0;

            string token = GenerateCustomerToken(customer, tierName, discountPercent);

            return new ShopAuthResponseDto
            {
                Token = token,
                CustomerInfo = new ShopCustomerInfoDto
                {
                    Id = customer.Id,
                    Code = customer.Code,
                    Name = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    Email = customer.Email,
                    AvatarPath = customer.AvatarPath,
                    CustomerTierId = customer.CustomerTierId,
                    CustomerTierName = tierName,
                    DiscountPercent = discountPercent
                }
            };
        }

        public async Task<ShopCustomerInfoDto> GetCurrentCustomerAsync(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerTier)
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin khách hàng.");

            return new ShopCustomerInfoDto
            {
                Id = customer.Id,
                Code = customer.Code,
                Name = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                AvatarPath = customer.AvatarPath,
                CustomerTierId = customer.CustomerTierId,
                CustomerTierName = customer.CustomerTier?.Name ?? "Thành Viên",
                DiscountPercent = customer.CustomerTier?.DiscountPercent ?? 0
            };
        }

        private string GenerateCustomerToken(Customer customer, string tierName, decimal discountPercent)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new Claim(ClaimTypes.Name, customer.Name),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("CustomerId", customer.Id.ToString()),
                new Claim("CustomerCode", customer.Code),
                new Claim("CustomerTierName", tierName),
                new Claim("DiscountPercent", discountPercent.ToString())
            };

            if (!string.IsNullOrEmpty(customer.PhoneNumber))
                claims.Add(new Claim(ClaimTypes.MobilePhone, customer.PhoneNumber));

            if (!string.IsNullOrEmpty(customer.Email))
                claims.Add(new Claim(ClaimTypes.Email, customer.Email));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"] ?? "1440"));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
