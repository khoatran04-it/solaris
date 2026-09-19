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
    public class ExtendedSeedingSummaryDto
    {
        public int TotalWarehouses { get; set; }
        public int TotalCategories { get; set; }
        public int TotalProducts { get; set; }
        public int TotalVariants { get; set; }
        public int TotalBatches { get; set; }
        public int TotalInventories { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalAddresses { get; set; }
        public int TotalOrders { get; set; }
        public int TotalOrderDetails { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// Bộ sinh dữ liệu mở rộng cho Nền tảng Nông sản Solaris (Option A: Catalog & Historical Orders).
    /// Bổ sung ~50 sản phẩm đặc sản Việt Nam, 100+ biến thể, hàng trăm tồn kho và 150+ đơn hàng
    /// để phục vụ hiển thị sinh động trên Web Storefront và Dashboard Quản trị.
    /// BẢO TỒN NGUYÊN VẸN các mầm kiểm thử gốc (SKU-STRESS-50, LOT-STRESS-50-FEFO, WH-BT-01, CUST-001 - CUST-020).
    /// </summary>
    public static class ExtendedCatalogSeeder
    {
        public static async Task<ExtendedSeedingSummaryDto> SeedAsync(SolarisDbContext context)
        {
            var now = DateTime.UtcNow;
            var fakerVi = new Faker("vi");

            // 1. Kiểm tra nhà cung cấp
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

            // 2. Lấy các kho bán lẻ thực nghiệm
            var retailWarehouses = await context.Warehouses
                .Where(w => w.IsActive && !w.IsDeleted && w.Code.StartsWith("WH-"))
                .ToListAsync();

            if (retailWarehouses.Count == 0)
            {
                retailWarehouses = await context.Warehouses.Where(w => w.IsActive && !w.IsDeleted).ToListAsync();
            }

            // 3. Lấy UoMs chuẩn
            var uomKg = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "KG") ?? await context.UoMs.FirstAsync();
            var uomHop = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "HOP") ?? uomKg;
            var uomThung = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "THUNG") ?? uomKg;
            var uomGoi = await context.UoMs.FirstOrDefaultAsync(u => u.Code == "GOI") ?? uomKg;

            // 4. Lấy danh mục
            var catFreshFruit = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-TRAI-CAY" || c.Code == "CAT-FRESH-FRUITS") ?? await context.ProductCategories.FirstAsync();
            var catVeg = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-RAU-CU") ?? catFreshFruit;
            var catMeat = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-THIT-TUOI") ?? catFreshFruit;
            var catSeafood = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-THUY-SAN") ?? catFreshFruit;
            var catRice = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-GAO-HAT" || c.Code == "CAT-GRAIN-RICE") ?? catFreshFruit;
            var catDryFood = await context.ProductCategories.FirstOrDefaultAsync(c => c.Code == "CAT-MI-GOI") ?? catFreshFruit;

            // 5. Lấy từ điển thuộc tính EAV
            var attrDefs = await context.AttributeDefinitions.Where(a => !a.IsDeleted).ToListAsync();
            var originAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Xuất xứ"));
            var certAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Tiêu chuẩn"));
            var brixAttrDef = attrDefs.FirstOrDefault(a => a.Name.Contains("Brix"));

            // 6. Danh sách 50 Sản Phẩm Nông Sản Mở Rộng
            var extendedProducts = new List<(string Code, string Name, string Slug, ProductCategory Cat, decimal Price, string Origin, string Cert, string? Brix, bool MultiUoM)>
            {
                // Trái Cây Đặc Sản
                ("PRD-EXT-BUOI-DA-XANH", "Bưởi Da Xanh Bến Tre Tuyển Chọn", "buoi-da-xanh-ben-tre-tuyen-chon", catFreshFruit, 65000, "Bến Tre", "GlobalGAP", "12.5", true),
                ("PRD-EXT-CAM-SANH", "Cam Sành Hàm Yên Mọng Nước", "cam-sanh-ham-yen-mong-nuoc", catFreshFruit, 38000, "Tuyên Quang", "VietGAP", "11.0", true),
                ("PRD-EXT-MANG-CUT", "Măng Cụt Lái Thiêu Loại 1", "mang-cut-lai-thieu-loai-1", catFreshFruit, 85000, "Bình Dương", "VietGAP", "14.0", true),
                ("PRD-EXT-VAI-THIEU", "Vải Thiều Lục Ngạn Chín Cây", "vai-thieu-luc-ngan-chin-cay", catFreshFruit, 70000, "Bắc Giang", "GlobalGAP", "16.0", true),
                ("PRD-EXT-NHAN-XUONG", "Nhãn Xuồng Cơm Vàng Vũng Tàu", "nhan-xuong-com-vang-vung-tau", catFreshFruit, 75000, "Bà Rịa - Vũng Tàu", "VietGAP", "15.5", true),
                ("PRD-EXT-DUA-LUOI", "Dưa Lưới Ruột Cam Ichiba", "dua-luoi-ruot-cam-ichiba", catFreshFruit, 55000, "Bình Phước", "VietGAP", "14.5", true),
                ("PRD-EXT-MAN-HAU", "Mận Hậu Mộc Châu Giòn Ngọt", "man-hau-moc-chau-gion-ngot", catFreshFruit, 60000, "Sơn La", "VietGAP", "13.0", false),
                ("PRD-EXT-THANH-LONG", "Thanh Long Ruột Đỏ Chợ Gạo", "thanh-long-ruot-do-cho-gao", catFreshFruit, 35000, "Tiền Giang", "GlobalGAP", "13.5", true),
                ("PRD-EXT-CHUOI-GIA", "Chuối Già Nam Mỹ Xuất Khẩu", "chuoi-gia-nam-my-xuat-khau", catFreshFruit, 28000, "Đồng Nai", "GlobalGAP", "15.0", false),
                ("PRD-EXT-VU-SUA", "Vú Sữa Lò Rèn Vĩnh Kim", "vu-sua-lo-ren-vinh-kim", catFreshFruit, 90000, "Tiền Giang", "OCOP 4 sao", "14.0", true),
                ("PRD-EXT-OI-NU-HOANG", "Ổi Nữ Hoàng Giòn Ngọt Hữu Cơ", "oi-nu-hoang-gion-ngot-huu-co", catFreshFruit, 32000, "Tiền Giang", "Hữu cơ PGS", "11.5", false),
                ("PRD-EXT-QUYT-DUONG", "Quýt Đường Trà Vinh Trái Mọng", "quyt-duong-tra-vinh-trai-mong", catFreshFruit, 42000, "Trà Vinh", "VietGAP", "13.8", true),

                // Rau Củ Quả Hữu Cơ
                ("PRD-EXT-CA-ROT", "Cà Rốt Hữu Cơ Đà Lạt", "ca-rot-huu-co-da-lat", catVeg, 38000, "Lâm Đồng, Đà Lạt", "Hữu cơ PGS", null, true),
                ("PRD-EXT-KHOAI-TAY", "Khoai Tây Hồng Đà Lạt Bùi Thơm", "khoai-tay-hong-da-lat-bui-thom", catVeg, 42000, "Lâm Đồng, Đà Lạt", "VietGAP", null, true),
                ("PRD-EXT-BI-DO", "Bí Đỏ Hạt Đậu Hữu Cơ", "bi-do-hat-dau-huu-co", catVeg, 30000, "Đắk Lắk", "VietGAP", null, false),
                ("PRD-EXT-BONG-CAI", "Bông Cải Xanh Baby Hữu Cơ", "bong-cai-xanh-baby-huu-co", catVeg, 62000, "Lâm Đồng, Đà Lạt", "Hữu cơ PGS", null, false),
                ("PRD-EXT-BAP-CAI", "Bắp Cải Trái Tim Đà Lạt", "bap-cai-trai-tim-da-lat", catVeg, 26000, "Lâm Đồng, Đà Lạt", "VietGAP", null, true),
                ("PRD-EXT-NAM-DUI-GA", "Nấm Đùi Gà Hữu Cơ Sinh Học", "nam-dui-ga-huu-co-sinh-hoc", catVeg, 58000, "Đồng Nai", "HACCP", null, false),
                ("PRD-EXT-NAM-DONG-CO", "Nấm Đông Cô Tươi Đà Lạt", "nam-dong-co-tuoi-da-lat", catVeg, 85000, "Lâm Đồng, Đà Lạt", "VietGAP", null, false),
                ("PRD-EXT-XA-LACH-LOLO", "Xà Lách Lô Lô Xanh Thủy Canh", "xa-lach-lo-lo-xanh-thuy-canh", catVeg, 48000, "Lâm Đồng, Đà Lạt", "GlobalGAP", null, false),
                ("PRD-EXT-DAU-CO-VE", "Đậu Cô Ve Nhật Bản Giòn Ngọt", "dau-co-ve-nhat-ban-gion-ngot", catVeg, 36000, "Lâm Đồng, Đà Lạt", "VietGAP", null, false),
                ("PRD-EXT-OT-CHUONG", "Ớt Chuông Đà Lạt Đủ Màu", "ot-chuong-da-lat-du-mau", catVeg, 52000, "Lâm Đồng, Đà Lạt", "GlobalGAP", null, false),
                ("PRD-EXT-CA-TIM", "Cà Tím Hữu Cơ Đồng Nai", "ca-tim-huu-co-dong-nai", catVeg, 24000, "Đồng Nai", "VietGAP", null, false),
                ("PRD-EXT-MUOP-HUONG", "Mướp Hương Đồng Tháp Nấu Canh", "muop-huong-dong-thap-nau-canh", catVeg, 28000, "Đồng Tháp", "VietGAP", null, false),

                // Thịt Sạch VietGAP
                ("PRD-EXT-SUON-NON", "Sườn Non Heo VietGAP Mềm Ngon", "suon-non-heo-vietgap-mem-ngon", catMeat, 185000, "Đồng Nai", "VietGAP", null, false),
                ("PRD-EXT-NAC-DAM", "Thịt Nạc Dăm Heo VietGAP Tươi", "thit-nac-dam-heo-vietgap-tuoi", catMeat, 140000, "Đồng Nai", "VietGAP", null, false),
                ("PRD-EXT-BAP-BO", "Bắp Bò Tươi Tây Ninh Thơm Ngon", "bap-bo-tuoi-tay-ninh-thom-ngon", catMeat, 260000, "Tây Ninh", "HACCP", null, false),
                ("PRD-EXT-UC-GA", "File Ức Gà Thảo Mộc Ít Béo", "file-uc-ga-thao-moc-it-beo", catMeat, 95000, "Bình Phước", "VietGAP", null, false),
                ("PRD-EXT-GA-TA", "Gà Ta Thả Vườn Yên Thế Làm Sạch", "ga-ta-tha-vuon-yen-the-lam-sach", catMeat, 145000, "Bắc Giang", "OCOP 4 sao", null, false),
                ("PRD-EXT-GIO-SONG", "Giò Sống Heo Sạch Không Hàn The", "gio-song-heo-sach-khong-han-the", catMeat, 150000, "TP. Hồ Chí Minh", "HACCP", null, false),
                ("PRD-EXT-CO-HEO", "Thịt Cổ Heo Nướng Thảo Mộc", "thit-co-heo-nuong-thao-moc", catMeat, 165000, "Đồng Nai", "VietGAP", null, false),
                ("PRD-EXT-BO-TO", "Bò Tơ Củ Chi Cắt Lát Mềm Ngọt", "bo-to-cu-chi-cat-lat-mem-ngot", catMeat, 275000, "TP. Hồ Chí Minh", "HACCP", null, false),

                // Thủy Hải Sản Tươi Sống
                ("PRD-EXT-TOM-CANG", "Tôm Càng Xanh Bến Tre Sống", "tom-cang-xanh-ben-tre-song", catSeafood, 320000, "Bến Tre", "VietGAP", null, false),
                ("PRD-EXT-CUA-CA-MAU", "Cua Biển Năm Căn Cà Mau Gạch Đầy", "cua-bien-nam-can-ca-mau-gach-day", catSeafood, 450000, "Cà Mau", "OCOP 4 sao", null, false),
                ("PRD-EXT-MUC-ONG", "Mực Ống Tươi Côn Đảo Thân Dày", "muc-ong-tuoi-con-dao-than-day", catSeafood, 290000, "Bà Rịa - Vũng Tàu", "HACCP", null, false),
                ("PRD-EXT-CA-CHEM", "Cá Chẽm Phi Lê Cấp Đông Tươi", "ca-chem-phi-le-cap-dong-tuoi", catSeafood, 195000, "Bạc Liêu", "VietGAP", null, false),
                ("PRD-EXT-NGHEU-GO-CONG", "Nghêu Sạch Gò Công Con To", "ngheu-sach-go-cong-con-to", catSeafood, 65000, "Tiền Giang", "VietGAP", null, false),
                ("PRD-EXT-BACH-TUOC", "Bạch Tuộc Baby Phú Quốc Tươi", "bach-tuoc-baby-phu-quoc-tuoi", catSeafood, 220000, "Kiên Giang", "HACCP", null, false),
                ("PRD-EXT-CA-BASA", "Cá Basa Phi Lê Xuất Khẩu An Giang", "ca-basa-phi-le-xuat-khau-an-giang", catSeafood, 88000, "An Giang", "GlobalGAP", null, false),
                ("PRD-EXT-CHA-CA-THAC-LAC", "Chả Cá Thác Lác Hậu Giang Quết Tay", "cha-ca-thac-lac-hau-giang-quet-tay", catSeafood, 240000, "Hậu Giang", "OCOP 4 sao", null, false),

                // Gạo & Ngũ Cốc Đặc Sản
                ("PRD-EXT-NEP-CAI-HOA-VANG", "Gạo Nếp Cái Hoa Vàng Thơm Lừng", "gao-nep-cai-hoa-vang-thom-lung", catRice, 45000, "Hải Dương", "OCOP 4 sao", null, true),
                ("PRD-EXT-HAT-DIEU", "Hạt Điều Rang Muối Vỏ Lụa A++", "hat-dieu-rang-muoi-vo-lua-a", catRice, 260000, "Bình Phước", "OCOP 4 sao", null, false),
                ("PRD-EXT-DAU-DEN", "Đậu Đen Xanh Lòng Hạt Nhỏ Hữu Cơ", "dau-den-xanh-long-hat-nho-huu-co", catRice, 55000, "Đắk Lắk", "VietGAP", null, false),
                ("PRD-EXT-GAO-LUT", "Gạo Lứt Huyết Rồng Dinh Dưỡng", "gao-lut-huyet-rong-dinh-duong", catRice, 42000, "Sóc Trăng", "HACCP", null, true),
                ("PRD-EXT-MAC-CA", "Hạt Mắc Ca Tây Nguyên Nứt Vỏ", "hat-mac-ca-tay-nguyen-nut-vo", catRice, 280000, "Lâm Đồng", "OCOP 4 sao", null, false),
                ("PRD-EXT-DAU-XANH", "Đậu Xanh Tằm Quê Đắk Lắk Ruột Vàng", "dau-xanh-tam-que-dak-lak-ruot-vang", catRice, 52000, "Đắk Lắk", "VietGAP", null, false),

                // Mì & Thực Phẩm Chế Biến Khô
                ("PRD-EXT-HU-TIEU-SA-DEC", "Hủ Tiếu Sa Đéc Sợi Dai Truyền Thống", "hu-tieu-sa-dec-soi-dai-truyen-thong", catDryFood, 35000, "Đồng Tháp", "OCOP 4 sao", null, false),
                ("PRD-EXT-BUN-KHO", "Bún Tươi Sấy Khô Ba Khánh", "bun-tuoi-say-kho-ba-khanh", catDryFood, 28000, "Bến Tre", "HACCP", null, false),
                ("PRD-EXT-NUOC-MAM", "Nước Mắm Nhĩ Phú Quốc 40 Độ Đạm", "nuoc-mam-nhi-phu-quoc-40-do-dam", catDryFood, 160000, "Kiên Giang", "Chỉ dẫn địa lý", null, false),
                ("PRD-EXT-MAT-ONG", "Mật Ong Hoa Cà Phê Tây Nguyên Tự Nhiên", "mat-ong-hoa-ca-phe-tay-nguyen-tu-nhien", catDryFood, 195000, "Gia Lai", "VietGAP", null, false),
                ("PRD-EXT-TIEU-DEN", "Tiêu Đen Phú Quốc Hạt Chín Cay Nồng", "tieu-den-phu-quoc-hat-chin-cay-nong", catDryFood, 85000, "Kiên Giang", "OCOP 4 sao", null, false)
            };

            var newlyAddedVariants = new List<ProductVariant>();

            foreach (var item in extendedProducts)
            {
                var existingProd = await context.Products.FirstOrDefaultAsync(p => p.Code == item.Code);
                if (existingProd != null) continue;

                var prod = new Product
                {
                    Code = item.Code,
                    Name = item.Name,
                    Slug = item.Slug,
                    CategoryId = item.Cat.Id,
                    BaseUoMId = uomKg.Id,
                    IsActive = true
                };
                context.Products.Add(prod);
                await context.SaveChangesAsync();

                // Tạo biến thể Loại 1
                var variant = new ProductVariant
                {
                    Code = $"{item.Code}-SKU1",
                    Name = $"{item.Name} - Chuẩn Loại 1",
                    ProductId = prod.Id,
                    IsActive = true
                };
                context.ProductVariants.Add(variant);
                await context.SaveChangesAsync();
                newlyAddedVariants.Add(variant);

                // Bảng giá Đơn vị cơ sở
                context.ProductVariantPrices.Add(new ProductVariantPrice
                {
                    VariantId = variant.Id,
                    UoMId = prod.BaseUoMId,
                    Price = item.Price,
                    IsDefault = true,
                    IsActive = true
                });

                // Đa quy cách bán (Hộp 3kg giảm 5%, Thùng 10kg giảm 10%)
                if (item.MultiUoM)
                {
                    context.ProductVariantPrices.Add(new ProductVariantPrice
                    {
                        VariantId = variant.Id,
                        UoMId = uomHop.Id,
                        Price = Math.Round(item.Price * 3 * 0.95m),
                        IsDefault = false,
                        IsActive = true
                    });
                    context.ProductVariantPrices.Add(new ProductVariantPrice
                    {
                        VariantId = variant.Id,
                        UoMId = uomThung.Id,
                        Price = Math.Round(item.Price * 10 * 0.90m),
                        IsDefault = false,
                        IsActive = true
                    });
                }
                await context.SaveChangesAsync();

                // Thuộc tính EAV động
                if (originAttrDef != null)
                {
                    context.ProductAttributes.Add(new ProductAttribute
                    {
                        VariantId = variant.Id,
                        AttributeDefinitionId = originAttrDef.Id,
                        AttributeValue = item.Origin
                    });
                }
                if (certAttrDef != null)
                {
                    context.ProductAttributes.Add(new ProductAttribute
                    {
                        VariantId = variant.Id,
                        AttributeDefinitionId = certAttrDef.Id,
                        AttributeValue = item.Cert
                    });
                }
                if (brixAttrDef != null && !string.IsNullOrEmpty(item.Brix))
                {
                    context.ProductAttributes.Add(new ProductAttribute
                    {
                        VariantId = variant.Id,
                        AttributeDefinitionId = brixAttrDef.Id,
                        AttributeValue = item.Brix
                    });
                }
                await context.SaveChangesAsync();

                // Lô hàng FEFO (1 lô cận hạn 5 ngày, 1 lô hạn xa 30 ngày)
                var batchNear = new ProductBatch
                {
                    BatchCode = $"LOT-EXT-{variant.Id}-05D",
                    VariantId = variant.Id,
                    SupplierId = supplier.Id,
                    ManufactureDate = now.AddDays(-2),
                    ExpiryDate = now.AddDays(5),
                    IsActive = true
                };
                var batchFar = new ProductBatch
                {
                    BatchCode = $"LOT-EXT-{variant.Id}-30D",
                    VariantId = variant.Id,
                    SupplierId = supplier.Id,
                    ManufactureDate = now.AddDays(-1),
                    ExpiryDate = now.AddDays(30),
                    IsActive = true
                };
                context.ProductBatches.AddRange(batchNear, batchFar);
                await context.SaveChangesAsync();

                // Phân bổ Tồn kho tại các kho bán lẻ
                foreach (var wh in retailWarehouses)
                {
                    context.WarehouseInventories.Add(new WarehouseInventory
                    {
                        WarehouseId = wh.Id,
                        VariantId = variant.Id,
                        BatchId = batchNear.Id,
                        QuantityAvailable = fakerVi.Random.Number(20, 60),
                        QuantityReserved = 0
                    });
                    context.WarehouseInventories.Add(new WarehouseInventory
                    {
                        WarehouseId = wh.Id,
                        VariantId = variant.Id,
                        BatchId = batchFar.Id,
                        QuantityAvailable = fakerVi.Random.Number(50, 150),
                        QuantityReserved = 0
                    });
                }
                await context.SaveChangesAsync();
            }

            // 7. Bổ sung Khách Hàng & Sổ Địa Chỉ Mở Rộng (Tới 100 khách)
            var currentCustomerCount = await context.Customers.CountAsync(c => !c.IsDeleted);
            var targetCustomerCount = 100;
            var customerAddList = new List<Customer>();

            var hcmcDistricts = new List<(string District, string Ward, string Street, double Lat, double Lng)>
            {
                ("Quận 1", "Phường Bến Nghé", "Lê Duẩn", 10.7810, 106.7000),
                ("Quận 3", "Phường Võ Thị Sáu", "Nam Kỳ Khởi Nghĩa", 10.7880, 106.6890),
                ("Quận 5", "Phường 5", "Trần Hưng Đạo", 10.7540, 106.6660),
                ("Quận 7", "Phường Tân Phong", "Nguyễn Thị Thập", 10.7380, 106.7110),
                ("Quận 10", "Phường 12", "Sư Vạn Hạnh", 10.7720, 106.6670),
                ("Quận Bình Thạnh", "Phường 25", "Điện Biên Phủ", 10.8030, 106.7150),
                ("Quận Phú Nhuận", "Phường 2", "Phan Xích Long", 10.7960, 106.6900),
                ("Quận Tân Bình", "Phường 4", "Trường Sơn", 10.8080, 106.6630),
                ("Quận Gò Vấp", "Phường 10", "Quang Trung", 10.8350, 106.6680),
                ("Thành phố Thủ Đức", "Phường Thảo Điền", "Xuân Thủy", 10.8050, 106.7320)
            };

            for (int i = currentCustomerCount + 1; i <= targetCustomerCount; i++)
            {
                var custName = fakerVi.Name.FullName();
                var phone = $"09{fakerVi.Random.Number(10000000, 99999999)}";
                var customer = new Customer
                {
                    Code = $"CUST-EXT-{i:D3}",
                    Name = custName,
                    PhoneNumber = phone,
                    Email = $"khachhang{i}@solaris.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Solaris@123"),
                    IsActive = true
                };
                context.Customers.Add(customer);
                await context.SaveChangesAsync();
                customerAddList.Add(customer);

                var d = hcmcDistricts[i % hcmcDistricts.Count];
                context.CustomerAddresses.Add(new CustomerAddress
                {
                    CustomerId = customer.Id,
                    ReceiverName = customer.Name,
                    Phone = customer.PhoneNumber,
                    Province = "Thành phố Hồ Chí Minh",
                    District = d.District,
                    Ward = d.Ward,
                    StreetAddress = $"{fakerVi.Random.Number(10, 450)} {d.Street}",
                    Latitude = d.Lat + (fakerVi.Random.Double(-0.005, 0.005)),
                    Longitude = d.Lng + (fakerVi.Random.Double(-0.005, 0.005)),
                    IsDefault = true
                });
                await context.SaveChangesAsync();
            }

            // 8. Bổ sung 150 Đơn Hàng Lịch Sử Phục Vụ Dashboard Analytics
            var existingOrdersCount = await context.Orders.CountAsync(o => !o.IsDeleted);
            if (existingOrdersCount < 100)
            {
                var allCustomers = await context.Customers.Include(c => c.Addresses).Where(c => !c.IsDeleted).ToListAsync();
                var allVariants = await context.ProductVariants.Include(v => v.Prices).Where(v => v.IsActive && !v.IsDeleted).ToListAsync();
                var primaryWarehouse = retailWarehouses.First();

                var orderStatuses = new[]
                {
                    OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Completed,
                    OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Shipping,
                    OrderStatus.Processing, OrderStatus.Cancelled
                };

                for (int ordIdx = 1; ordIdx <= 150; ordIdx++)
                {
                    var cust = allCustomers[fakerVi.Random.Number(0, allCustomers.Count - 1)];
                    var addr = cust.Addresses.FirstOrDefault() ?? new CustomerAddress
                    {
                        ReceiverName = cust.Name,
                        Phone = cust.PhoneNumber,
                        Province = "Thành phố Hồ Chí Minh",
                        District = "Quận Bình Thạnh",
                        Ward = "Phường 25",
                        StreetAddress = "123 Điện Biên Phủ"
                    };

                    var assignedWh = retailWarehouses[fakerVi.Random.Number(0, retailWarehouses.Count - 1)];
                    var status = orderStatuses[fakerVi.Random.Number(0, orderStatuses.Length - 1)];
                    var daysAgo = fakerVi.Random.Number(1, 40);
                    var orderDate = now.AddDays(-daysAgo).AddHours(fakerVi.Random.Number(7, 21));

                    var order = new Order
                    {
                        OrderCode = $"ORD-EXT-{orderDate:yyyyMMdd}-{ordIdx:D3}",
                        CustomerId = cust.Id,
                        CustomerAddressId = addr.Id > 0 ? addr.Id : null,
                        ReceiverName = addr.ReceiverName,
                        ReceiverPhone = addr.Phone,
                        DeliveryAddress = $"{addr.StreetAddress}, {addr.Ward}, {addr.District}, {addr.Province}",
                        WarehouseId = assignedWh.Id,
                        ShippingProvider = "Solaris Cold-Express",
                        TrackingCode = $"SLR-EXP-{fakerVi.Random.AlphaNumeric(8).ToUpper()}",
                        Status = status,
                        PaymentMethod = (PaymentMethod)fakerVi.Random.Number(1, 3),
                        PaymentStatus = status == OrderStatus.Completed ? PaymentStatus.Paid : (status == OrderStatus.Cancelled ? PaymentStatus.Refunded : PaymentStatus.Paid),
                        OrderDate = orderDate,
                        CreatedAt = orderDate,
                        ExpectedDeliveryDate = orderDate.AddHours(4),
                        DeliveredAt = status == OrderStatus.Completed ? orderDate.AddHours(3) : null
                    };

                    context.Orders.Add(order);
                    await context.SaveChangesAsync();

                    // Tạo 2 - 4 chi tiết đơn hàng
                    int numItems = fakerVi.Random.Number(2, 4);
                    decimal subTotal = 0;

                    for (int j = 0; j < numItems; j++)
                    {
                        var pickedVariant = allVariants[fakerVi.Random.Number(0, allVariants.Count - 1)];
                        var priceObj = pickedVariant.Prices.FirstOrDefault(p => p.IsDefault) ?? pickedVariant.Prices.First();
                        var qty = fakerVi.Random.Number(1, 5);
                        var lineAmount = priceObj.Price * qty;
                        subTotal += lineAmount;

                        context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            VariantId = pickedVariant.Id,
                            UoMId = priceObj.UoMId,
                            Quantity = qty,
                            BaseQuantity = qty,
                            UnitPrice = priceObj.Price,
                            DiscountAmount = 0,
                            TotalPrice = lineAmount
                        });
                    }

                    order.SubTotal = subTotal;
                    order.ShippingFee = 25000;
                    order.DiscountAmount = subTotal > 500000 ? 50000 : 0;
                    order.TotalAmount = order.SubTotal - order.DiscountAmount + order.ShippingFee;
                    await context.SaveChangesAsync();
                }
            }

            var totalRev = await context.Orders
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);

            return new ExtendedSeedingSummaryDto
            {
                TotalWarehouses = await context.Warehouses.CountAsync(w => !w.IsDeleted),
                TotalCategories = await context.ProductCategories.CountAsync(c => !c.IsDeleted),
                TotalProducts = await context.Products.CountAsync(p => !p.IsDeleted),
                TotalVariants = await context.ProductVariants.CountAsync(v => !v.IsDeleted),
                TotalBatches = await context.ProductBatches.CountAsync(b => !b.IsDeleted),
                TotalInventories = await context.WarehouseInventories.CountAsync(),
                TotalCustomers = await context.Customers.CountAsync(c => !c.IsDeleted),
                TotalAddresses = await context.CustomerAddresses.CountAsync(a => !a.IsDeleted),
                TotalOrders = await context.Orders.CountAsync(o => !o.IsDeleted),
                TotalOrderDetails = await context.OrderDetails.CountAsync(),
                TotalRevenue = totalRev
            };
        }
    }
}
