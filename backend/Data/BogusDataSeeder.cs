using Bogus;
using backend.Data;
using backend.Models;
using backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Data
{
    public class SeedingSummaryDto
    {
        public int TotalWarehouses { get; set; }
        public int TotalCategories { get; set; }
        public int TotalProducts { get; set; }
        public int TotalVariants { get; set; }
        public int TotalBatches { get; set; }
        public int TotalInventories { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalAddresses { get; set; }
        public string StressTestVariantCode { get; set; } = string.Empty;
        public decimal StressTestAvailableStock { get; set; }
    }

    /// <summary>
    /// Bộ Sinh Dữ Liệu Mầm Thực Nghiệm Bằng Thư Viện Bogus (Local Data Generator).
    /// Đảm bảo dữ liệu sinh ra hợp lệ 100% (Foreign Keys, Constraints, Cold-chain, FEFO Lots).
    /// </summary>
    public static class BogusDataSeeder
    {
        public static async Task<SeedingSummaryDto> SeedEvaluationDataAsync(SolarisDbContext context)
        {
            var now = DateTime.UtcNow;
            var fakerVi = new Faker("vi");

            // 1. Kiểm tra hoặc Tạo Nhà Cung Cấp (Suppliers)
            var supplier = await context.Suppliers.FirstOrDefaultAsync(s => !s.IsDeleted);
            if (supplier == null)
            {
                supplier = new Supplier
                {
                    Code = "SUP-DALAT-GAP",
                    Name = "Hợp Tác Xã Nông Sản Sạch Đà Lạt",
                    Phone = "0912345678",
                    Email = "dalatgap@solaris.vn",
                    IsActive = true
                };
                context.Suppliers.Add(supplier);
                await context.SaveChangesAsync();
            }

            // 2. Thiết lập 5 Kho Bán Lẻ Nội Đô TP.HCM với Tọa độ GPS Chuẩn
            var existingWarehouses = await context.Warehouses.Include(w => w.Address).Where(w => !w.IsDeleted).ToListAsync();
            var warehouseConfigs = new List<(string Code, string Name, string District, string Ward, string Street, double Lat, double Lng)>
            {
                ("WH-BT-01", "Kho Bán Lẻ Bình Thạnh", "Quận Bình Thạnh", "Phường 25", "123 Điện Biên Phủ", 10.8035, 106.7152),
                ("WH-Q1-01", "Kho Bán Lẻ Quận 1", "Quận 1", "Phường Bến Nghé", "456 Lê Lợi", 10.7769, 106.7009),
                ("WH-Q7-01", "Kho Bán Lẻ Quận 7", "Quận 7", "Phường Tân Phong", "789 Nguyễn Văn Linh", 10.7325, 106.7150),
                ("WH-Q4-01", "Kho Bán Lẻ Quận 4", "Quận 4", "Phường 12", "321 Hoàng Diệu", 10.7580, 106.7020),
                ("WH-GV-01", "Kho Bán Lẻ Gò Vấp", "Quận Gò Vấp", "Phường 10", "654 Quang Trung", 10.8350, 106.6680)
            };

            foreach (var cfg in warehouseConfigs)
            {
                var existingWh = existingWarehouses.FirstOrDefault(w => w.Code == cfg.Code);
                if (existingWh == null)
                {
                    var whAddr = new WarehouseAddress
                    {
                        Province = "Thành phố Hồ Chí Minh",
                        District = cfg.District,
                        Ward = cfg.Ward,
                        StreetAddress = cfg.Street,
                        Latitude = cfg.Lat,
                        Longitude = cfg.Lng
                    };
                    context.WarehouseAddresses.Add(whAddr);
                    await context.SaveChangesAsync();

                    var newWh = new Warehouse
                    {
                        Code = cfg.Code,
                        Name = cfg.Name,
                        WarehouseType = "Retail",
                        MaxColdChainRadiusKm = 15.0,
                        AddressId = whAddr.Id,
                        IsActive = true
                    };
                    context.Warehouses.Add(newWh);
                    await context.SaveChangesAsync();
                }
            }

            var allRetailWarehouses = await context.Warehouses.Where(w => w.IsActive && !w.IsDeleted).ToListAsync();
            var primaryWh = allRetailWarehouses.First();

            // 3. Kiểm tra Từ điển Thuộc tính EAV (Attribute Definitions)
            var attrDefs = await context.AttributeDefinitions.Where(a => !a.IsDeleted).ToListAsync();
            var requiredAttrs = new[] { "Xuất xứ", "Tiêu chuẩn chứng nhận", "Độ ngọt Brix", "Phương pháp bảo quản" };
            foreach (var attrName in requiredAttrs)
            {
                if (!attrDefs.Any(a => a.Name.Equals(attrName, StringComparison.OrdinalIgnoreCase)))
                {
                    var newAttr = new AttributeDefinition
                    {
                        Name = attrName,
                        DataType = attrName.Contains("Brix") ? "Number" : "String",
                        IsActive = true
                    };
                    context.AttributeDefinitions.Add(newAttr);
                    await context.SaveChangesAsync();
                    attrDefs.Add(newAttr);
                }
            }

            // 4. Kiểm tra Nhóm Ngành Hàng & Loại Sản Phẩm
            var freshGroup = await context.ProductCategoryGroups.FirstOrDefaultAsync(g => g.Code == "FRESH_PRODUCE");
            if (freshGroup == null)
            {
                freshGroup = new ProductCategoryGroup { Code = "FRESH_PRODUCE", Name = "Nông Sản Tươi Sống", Slug = "nong-san-tuoi-song", IsActive = true };
                context.ProductCategoryGroups.Add(freshGroup);
                await context.SaveChangesAsync();
            }

            var dryGroup = await context.ProductCategoryGroups.FirstOrDefaultAsync(g => g.Code == "DRY_PRODUCE");
            if (dryGroup == null)
            {
                dryGroup = new ProductCategoryGroup { Code = "DRY_PRODUCE", Name = "Thực Phẩm Chế Biến & Đồ Khô", Slug = "thuc-pham-do-kho", IsActive = true };
                context.ProductCategoryGroups.Add(dryGroup);
                await context.SaveChangesAsync();
            }

            var categoryConfigs = new List<(string Code, string Name, string Slug, bool RequiresColdChain, int GroupId)>
            {
                ("CAT-RAU-CU", "Rau Củ Quả Hữu Cơ", "rau-cu-qua-huu-co", true, freshGroup.Id),
                ("CAT-TRAI-CAY", "Trái Cây Tươi Nhiệt Đới", "trai-cay-tuoi-nhiet-doi", true, freshGroup.Id),
                ("CAT-THIT-TUOI", "Thịt Tươi VietGAP", "thit-tuoi-vietgap", true, freshGroup.Id),
                ("CAT-THUY-SAN", "Thủy Hải Sản Tươi Sống", "thuy-hai-san-tuoi-song", true, freshGroup.Id),
                ("CAT-GAO-HAT", "Gạo & Ngũ Cốc Đặc Sản", "gao-ngu-coc-dac-san", false, dryGroup.Id),
                ("CAT-MI-GOI", "Mì & Thực Phẩm Đóng Gói", "mi-thuc-pham-dong-goi", false, dryGroup.Id)
            };

            var categories = new List<ProductCategory>();
            foreach (var cc in categoryConfigs)
            {
                var cat = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == cc.Code);
                if (cat == null)
                {
                    cat = new ProductCategory
                    {
                        Code = cc.Code,
                        Name = cc.Name,
                        Slug = cc.Slug,
                        RequiresColdChain = cc.RequiresColdChain,
                        CategoryGroupId = cc.GroupId,
                        IsActive = true
                    };
                    context.ProductCategories.Add(cat);
                    await context.SaveChangesAsync();
                }
                categories.Add(cat);
            }

            // Lấy UoMs chuẩn
            var uomKg = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "KG") ?? (await context.UoMs.FirstAsync());
            var uomHop = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "HOP") ?? uomKg;
            var uomThung = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "THUNG") ?? uomKg;
            var uomGoi = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "GOI") ?? uomKg;

            // 5. Danh Mục Sản Phẩm Mẫu Nông Nghiệp Chuẩn Hóa
            var productCatalog = new List<(string Code, string Name, string Slug, int CatIdx, decimal BasePrice, bool IsMultiUoM)>
            {
                ("PRD-CAI-BO-XOI", "Cải Bó Xôi Hữu Cơ", "cai-bo-xoi-huu-co", 0, 35000, true),
                ("PRD-CA-CHUA-BEEF", "Cà Chua Beef Đà Lạt", "ca-chua-beef-da-lat", 0, 45000, true),
                ("PRD-BO-SAP-034", "Bơ Sáp 034 Đắk Lắk", "bo-sap-034-dak-lak", 1, 50000, true),
                ("PRD-XOAI-CAT-CHU", "Xoài Cát Chu Cao Lãnh", "xoai-cat-chu-cao-lanh", 1, 65000, true),
                ("PRD-SAU-RIENG-RI6", "Sầu Riêng Ri6 Chín Cây", "sau-rieng-ri6-chin-cay", 1, 145000, false),
                ("PRD-THIT-BA-CHI", "Thịt Ba Chỉ Heo VietGAP", "thit-ba-chi-heo-vietgap", 2, 135000, false),
                ("PRD-THIT-DUI-HEO", "Thịt Đùi Heo Thảo Mộc", "thit-dui-heo-thao-moc", 2, 120000, false),
                ("PRD-CA-HOI-NAUY", "Cá Hồi Na Uy Tươi Sống", "ca-hoi-nauy-tuoi-song", 3, 380000, false),
                ("PRD-GAO-ST25", "Gạo ST25 Ông Cua Thượng Hạng", "gao-st25-ong-cua", 4, 38000, true),
                ("PRD-MI-HAO-HAO", "Mì Hảo Hảo Tôm Chua Cay", "mi-hao-hao-tom-chua-cay", 5, 4500, false)
            };

            var originAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Xuất xứ"));
            var certAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Tiêu chuẩn"));
            var brixAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Brix"));

            foreach (var item in productCatalog)
            {
                var prod = await context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Code == item.Code);
                if (prod == null)
                {
                    prod = new Product
                    {
                        Code = item.Code,
                        Name = item.Name,
                        Slug = item.Slug,
                        CategoryId = categories[item.CatIdx].Id,
                        BaseUoMId = item.CatIdx == 5 ? uomGoi.Id : uomKg.Id,
                        IsActive = true
                    };
                    context.Products.Add(prod);
                    await context.SaveChangesAsync();

                    // Tạo biến thể
                    var variant = new ProductVariant
                    {
                        Code = $"{item.Code}-SKU1",
                        Name = $"{item.Name} - Loại 1",
                        ProductId = prod.Id,
                        IsActive = true
                    };
                    context.ProductVariants.Add(variant);
                    await context.SaveChangesAsync();

                    // Bảng giá
                    var basePrice = new ProductVariantPrice
                    {
                        VariantId = variant.Id,
                        UoMId = prod.BaseUoMId,
                        Price = item.BasePrice,
                        IsDefault = true,
                        IsActive = true
                    };
                    context.ProductVariantPrices.Add(basePrice);

                    // Đa quy cách bán (Cách 2)
                    if (item.IsMultiUoM)
                    {
                        context.ProductVariantPrices.Add(new ProductVariantPrice
                        {
                            VariantId = variant.Id,
                            UoMId = uomHop.Id,
                            Price = Math.Round(item.BasePrice * 3 * 0.95m), // Hộp 3kg giảm 5%
                            IsDefault = false,
                            IsActive = true
                        });
                        context.ProductVariantPrices.Add(new ProductVariantPrice
                        {
                            VariantId = variant.Id,
                            UoMId = uomThung.Id,
                            Price = Math.Round(item.BasePrice * 10 * 0.90m), // Thùng 10kg giảm 10%
                            IsDefault = false,
                            IsActive = true
                        });
                    }
                    await context.SaveChangesAsync();

                    // Thuộc tính EAV
                    if (originAttrDef != null)
                    {
                        context.ProductAttributes.Add(new ProductAttribute
                        {
                            VariantId = variant.Id,
                            AttributeDefinitionId = originAttrDef.Id,
                            AttributeValue = item.CatIdx <= 1 ? "Lâm Đồng, Đà Lạt" : (item.CatIdx == 2 ? "Đồng Nai" : "Sóc Trăng")
                        });
                    }
                    if (certAttrDef != null)
                    {
                        context.ProductAttributes.Add(new ProductAttribute
                        {
                            VariantId = variant.Id,
                            AttributeDefinitionId = certAttrDef.Id,
                            AttributeValue = item.CatIdx <= 3 ? "VietGAP / GlobalGAP" : "HACCP"
                        });
                    }
                    if (brixAttrDef != null && item.CatIdx == 1) // Trái cây có độ ngọt
                    {
                        context.ProductAttributes.Add(new ProductAttribute
                        {
                            VariantId = variant.Id,
                            AttributeDefinitionId = brixAttrDef.Id,
                            AttributeValue = "14.5"
                        });
                    }
                    await context.SaveChangesAsync();

                    // 6. Lô Hàng FEFO (3 Lô: Cận hạn 3 ngày, Vừa 15 ngày, Xa 60 ngày)
                    var batchNear = new ProductBatch
                    {
                        BatchCode = $"LOT-{variant.Id}-03D",
                        VariantId = variant.Id,
                        SupplierId = supplier.Id,
                        ManufactureDate = now.AddDays(-2),
                        ExpiryDate = now.AddDays(3),
                        IsActive = true
                    };
                    var batchFar = new ProductBatch
                    {
                        BatchCode = $"LOT-{variant.Id}-15D",
                        VariantId = variant.Id,
                        SupplierId = supplier.Id,
                        ManufactureDate = now.AddDays(-1),
                        ExpiryDate = now.AddDays(15),
                        IsActive = true
                    };
                    context.ProductBatches.AddRange(batchNear, batchFar);
                    await context.SaveChangesAsync();

                    // Tồn kho phân bổ tại các kho
                    foreach (var wh in allRetailWarehouses)
                    {
                        context.WarehouseInventories.Add(new WarehouseInventory
                        {
                            WarehouseId = wh.Id,
                            VariantId = variant.Id,
                            BatchId = batchNear.Id,
                            QuantityAvailable = 30, // 30kg cận hạn
                            QuantityReserved = 0
                        });
                        context.WarehouseInventories.Add(new WarehouseInventory
                        {
                            WarehouseId = wh.Id,
                            VariantId = variant.Id,
                            BatchId = batchFar.Id,
                            QuantityAvailable = 70, // 70kg hạn xa
                            QuantityReserved = 0
                        });
                    }
                    await context.SaveChangesAsync();
                }
            }

            // 7. TẠO BIẾN THỂ CHUYÊN DỤNG CHO BÀI TEST RQ2: STRESS-TEST 500 CCU -> 50 SẢN PHẨM
            string stressCode = "SKU-STRESS-50";
            var stressVariant = await context.ProductVariants.FirstOrDefaultAsync(v => v.Code == stressCode);
            if (stressVariant == null)
            {
                var stressProd = await context.Products.FirstOrDefaultAsync(p => p.Code == "PRD-THIT-BA-CHI") ?? await context.Products.FirstAsync();
                stressVariant = new ProductVariant
                {
                    Code = stressCode,
                    Name = "Thịt Ba Chỉ Heo Test Chống Bán Âm (Tồn = 50)",
                    ProductId = stressProd.Id,
                    IsActive = true
                };
                context.ProductVariants.Add(stressVariant);
                await context.SaveChangesAsync();

                context.ProductVariantPrices.Add(new ProductVariantPrice
                {
                    VariantId = stressVariant.Id,
                    UoMId = stressProd.BaseUoMId,
                    Price = 135000,
                    IsDefault = true,
                    IsActive = true
                });

                var stressBatch = new ProductBatch
                {
                    BatchCode = "LOT-STRESS-50-FEFO",
                    VariantId = stressVariant.Id,
                    SupplierId = supplier.Id,
                    ManufactureDate = now.AddDays(-1),
                    ExpiryDate = now.AddDays(10),
                    IsActive = true
                };
                context.ProductBatches.Add(stressBatch);
                await context.SaveChangesAsync();

                // Đặt chính xác tồn kho khả dụng = 50 tại Kho 1 (Bình Thạnh)
                context.WarehouseInventories.Add(new WarehouseInventory
                {
                    WarehouseId = primaryWh.Id,
                    VariantId = stressVariant.Id,
                    BatchId = stressBatch.Id,
                    QuantityAvailable = 50,
                    QuantityReserved = 0
                });
                await context.SaveChangesAsync();
            }

            // 8. TẠO 20 KHÁCH HÀNG & SỔ ĐỊA CHỈ PHỤC VỤ TEST RQ3 (NỘI THÀNH < 15KM & NGOẠI THÀNH > 15KM)
            var custCount = await context.Customers.CountAsync();
            if (custCount < 20)
            {
                for (int i = 1; i <= 20; i++)
                {
                    string phone = $"090{fakerVi.Random.Number(1000000, 9999999)}";
                    var customer = new Customer
                    {
                        Code = $"CUST-{i:D3}",
                        Name = fakerVi.Name.FullName(),
                        PhoneNumber = phone,
                        Email = $"customer{i}@gmail.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Solaris@123"),
                        IsActive = true
                    };
                    context.Customers.Add(customer);
                    await context.SaveChangesAsync();

                    // Địa chỉ 1: Nội thành (< 15km)
                    context.CustomerAddresses.Add(new CustomerAddress
                    {
                        CustomerId = customer.Id,
                        ReceiverName = customer.Name,
                        Phone = customer.PhoneNumber,
                        Province = "Thành phố Hồ Chí Minh",
                        District = "Quận Bình Thạnh",
                        Ward = "Phường 25",
                        StreetAddress = $"{i * 10} Điện Biên Phủ",
                        Latitude = 10.8030 + (i * 0.001),
                        Longitude = 106.7150 + (i * 0.001),
                        IsDefault = true
                    });

                    // Địa chỉ 2: Ngoại thành Biên Hòa (> 15km, cách kho 22 - 28km)
                    context.CustomerAddresses.Add(new CustomerAddress
                    {
                        CustomerId = customer.Id,
                        ReceiverName = customer.Name,
                        Phone = customer.PhoneNumber,
                        Province = "Đồng Nai",
                        District = "Thành phố Biên Hòa",
                        Ward = "Phường Tân Phong",
                        StreetAddress = $"{i * 15} Nguyễn Ái Quốc",
                        Latitude = 10.9650 + (i * 0.002),
                        Longitude = 106.8200 + (i * 0.002),
                        IsDefault = false
                    });
                    await context.SaveChangesAsync();
                }
            }

            return new SeedingSummaryDto
            {
                TotalWarehouses = await context.Warehouses.CountAsync(w => !w.IsDeleted),
                TotalCategories = await context.ProductCategories.CountAsync(c => !c.IsDeleted),
                TotalProducts = await context.Products.CountAsync(p => !p.IsDeleted),
                TotalVariants = await context.ProductVariants.CountAsync(v => !v.IsDeleted),
                TotalBatches = await context.ProductBatches.CountAsync(b => !b.IsDeleted),
                TotalInventories = await context.WarehouseInventories.CountAsync(),
                TotalCustomers = await context.Customers.CountAsync(c => !c.IsDeleted),
                TotalAddresses = await context.CustomerAddresses.CountAsync(a => !a.IsDeleted),
                StressTestVariantCode = stressCode,
                StressTestAvailableStock = 50
            };
        }
    }
}
