using System;
using System.Linq;
using System.Threading.Tasks;
using backend.Enums;
using backend.Helpers;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    /// <summary>
    /// Bộ khởi tạo dữ liệu mầm (Database Initializer) cho hệ sinh thái Solaris ERP.
    /// Tự động nạp danh mục 56 Quyền hạn, Vai trò Quản trị viên tối cao (ADMIN) và Tài khoản Super Admin khi Database rỗng.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(SolarisDbContext context)
        {
            // 1. Tự động đồng bộ & làm sạch toàn bộ danh mục Quyền hạn theo SystemPermission Enum
            var enumDefinitions = SystemPermissionExtensions.GetAllDefinitions();
            var validEnumCodes = enumDefinitions.Select(d => d.Code).ToHashSet();

            // Xóa các quyền cũ lỗi thời không còn nằm trong Enum SystemPermission
            var obsoletePermissions = await context.IAPermissions
                .Where(p => !validEnumCodes.Contains(p.Code))
                .ToListAsync();

            if (obsoletePermissions.Any())
            {
                var obsoleteIds = obsoletePermissions.Select(p => p.Id).ToList();

                // Xóa liên kết trong IARolePermissions và IAUserPermissions trước
                var rolePermsToDelete = await context.IARolePermissions
                    .Where(rp => obsoleteIds.Contains(rp.PermissionId))
                    .ToListAsync();
                if (rolePermsToDelete.Any())
                {
                    context.IARolePermissions.RemoveRange(rolePermsToDelete);
                }

                var userPermsToDelete = await context.IAUserPermissions
                    .Where(up => obsoleteIds.Contains(up.PermissionId))
                    .ToListAsync();
                if (userPermsToDelete.Any())
                {
                    context.IAUserPermissions.RemoveRange(userPermsToDelete);
                }

                context.IAPermissions.RemoveRange(obsoletePermissions);
                await context.SaveChangesAsync();
            }

            // Đồng bộ / Cập nhật chuẩn hóa Module và Name (Unicode Tiếng Việt chuẩn) cho tất cả 56 quyền
            var currentDbPerms = await context.IAPermissions.ToListAsync();
            foreach (var def in enumDefinitions)
            {
                var existing = currentDbPerms.FirstOrDefault(p => p.Code == def.Code);
                if (existing != null)
                {
                    existing.Module = def.Module;
                    existing.Name = def.Name;
                }
                else
                {
                    context.IAPermissions.Add(new IAPermission
                    {
                        Code = def.Code,
                        Name = def.Name,
                        Module = def.Module
                    });
                }
            }
            await context.SaveChangesAsync();

            // 2. Khởi tạo Vai trò ADMIN (Super Admin Role)
            var adminRole = await context.IARoles.FirstOrDefaultAsync(r => r.Code == "ADMIN");
            if (adminRole == null)
            {
                adminRole = new IARole
                {
                    Code = "ADMIN",
                    Name = "Quản trị viên tối cao",
                    Description = "Toàn quyền cấu hình, vận hành và quản trị hệ thống Solaris ERP",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.IARoles.Add(adminRole);
                await context.SaveChangesAsync();
            }

            // 3. Gán toàn bộ 56 quyền hạn vào Vai trò ADMIN
            var allPermissions = await context.IAPermissions.ToListAsync();
            var currentAssignedPermIds = await context.IARolePermissions
                .Where(rp => rp.RoleId == adminRole.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingRolePerms = allPermissions
                .Where(p => !currentAssignedPermIds.Contains(p.Id))
                .Select(p => new IARolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = p.Id
                })
                .ToList();

            if (missingRolePerms.Any())
            {
                context.IARolePermissions.AddRange(missingRolePerms);
                await context.SaveChangesAsync();
            }

            // 4. Khởi tạo Tài khoản Super Admin
            var adminUser = await context.IAUsers
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Username == "admin");

            if (adminUser == null)
            {
                adminUser = new IAUser
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123456"),
                    FullName = "Quản trị viên Hệ thống",
                    Email = "admin@solaris.vn",
                    PhoneNumber = "0901234567",
                    CitizenId = "000000000001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.IAUsers.Add(adminUser);
                await context.SaveChangesAsync();

                // Gán vai trò ADMIN cho tài khoản admin
                context.IAUserRoles.Add(new IAUserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });
                await context.SaveChangesAsync();
            }

            // 5. Khởi tạo Từ điển thuộc tính EAV Nông sản chuẩn
            var defaultAttrs = new (string Name, string DataType)[]
            {
                ("Xuất xứ / Vùng trồng", "string"),
                ("Chứng nhận chất lượng", "string"),
                ("Độ ngọt (Brix)", "number"),
                ("Hạn sử dụng", "string"),
                ("Hướng dẫn bảo quản", "string"),
                ("Hướng dẫn sử dụng", "string"),
                ("Khối lượng tịnh", "string")
            };

            foreach (var attr in defaultAttrs)
            {
                if (!await context.AttributeDefinitions.AnyAsync(a => a.Name.ToLower() == attr.Name.ToLower() && !a.IsDeleted))
                {
                    context.AttributeDefinitions.Add(new AttributeDefinition
                    {
                        Name = attr.Name,
                        DataType = attr.DataType,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();

            // 6. Backfill Slugs cho Nhóm ngành hàng, Danh mục, Sản phẩm, Khuyến mãi
            var allGroups = await context.ProductCategoryGroups.ToListAsync();
            foreach (var g in allGroups.Where(g => string.IsNullOrEmpty(g.Slug) || g.Slug.All(char.IsDigit)))
            {
                g.Slug = SlugHelper.GenerateSlug(g.Name);
            }

            var allCats = await context.ProductCategories.ToListAsync();
            foreach (var c in allCats.Where(c => string.IsNullOrEmpty(c.Slug) || c.Slug.All(char.IsDigit)))
            {
                c.Slug = SlugHelper.GenerateSlug(c.Name);
            }

            var allProds = await context.Products.ToListAsync();
            foreach (var p in allProds.Where(p => string.IsNullOrEmpty(p.Slug) || p.Slug.All(char.IsDigit)))
            {
                p.Slug = SlugHelper.GenerateSlug(p.Name);
            }

            var allPromos = await context.PromotionCampaigns.ToListAsync();
            foreach (var pc in allPromos.Where(pc => string.IsNullOrEmpty(pc.Slug) || pc.Slug.All(char.IsDigit)))
            {
                pc.Slug = SlugHelper.GenerateSlug(pc.Name);
            }

            await context.SaveChangesAsync();
        }
    }
}
