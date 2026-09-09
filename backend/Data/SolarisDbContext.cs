using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class SolarisDbContext : DbContext
    {
        public SolarisDbContext(DbContextOptions<SolarisDbContext> options) : base(options)
        {
        }

        // --- Danh mục Supplier ---
        public DbSet<SupplierType> SupplierTypes { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierAddress> SupplierAddresses { get; set; }
        public DbSet<SupplierProduct> SupplierProducts { get; set; }

        // --- Danh mục Customer ---
        public DbSet<CustomerType> CustomerTypes { get; set; }
        public DbSet<CustomerTier> CustomerTiers { get; set; }
        public DbSet<CustomerGroup> CustomerGroups { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<CustomerGroupLink> CustomerGroupLinks { get; set; }

        // --- Danh mục UoM ---
        public DbSet<UoMCategory> UoMCategories { get; set; }
        public DbSet<UoM> UoMs { get; set; }
        public DbSet<UoMConversion> UoMConversions { get; set; }

        // --- Danh mục Product ---
        public DbSet<ProductCategoryGroup> ProductCategoryGroups { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantPrice> ProductVariantPrices { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }


        public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
        public DbSet<CategoryAttribute> CategoryAttributes { get; set; }

        // --- Danh mục Promotion ---
        public DbSet<PromotionCampaign> PromotionCampaigns { get; set; }
        public DbSet<PromotionVariant> PromotionVariants { get; set; }

        // --- Danh mục RBAC ---
        public DbSet<IARole> IARoles { get; set; }
        public DbSet<IAUser> IAUsers { get; set; }
        public DbSet<IAUserRole> IAUserRoles { get; set; }
        public DbSet<IAPermission> IAPermissions { get; set; }
        public DbSet<IARolePermission> IARolePermissions { get; set; }
        public DbSet<IAUserPermission> IAUserPermissions { get; set; }
        public DbSet<IAUserWarehouse> IAUserWarehouses { get; set; }

        // --- Danh mục Inventory ---
        public DbSet<WarehouseAddress> WarehouseAddresses { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }

        public DbSet<ProductBatch> ProductBatches { get; set; }
        public DbSet<WarehouseInventory> WarehouseInventories { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        // --- Danh mục Procurement (Phase 3) ---
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<InventoryReceipt> InventoryReceipts { get; set; }
        public DbSet<InventoryReceiptDetail> InventoryReceiptDetails { get; set; }

        // --- Danh mục Sales & Outbound (Phase 4) ---
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<InventoryIssue> InventoryIssues { get; set; }
        public DbSet<InventoryIssueDetail> InventoryIssueDetails { get; set; }
        public DbSet<CustomerReturn> CustomerReturns { get; set; }
        public DbSet<CustomerReturnDetail> CustomerReturnDetails { get; set; }

        // --- Danh mục E-Commerce Shop & Giỏ hàng ---
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

        // --- Danh mục Logistics & Transfer (Phase 5) ---
        public DbSet<InventoryTransfer> InventoryTransfers { get; set; }
        public DbSet<InventoryTransferDetail> InventoryTransferDetails { get; set; }

        // --- Danh mục Quản lý Vận tải & Đội xe nội bộ (Cold-Chain Fleet) ---
        public DbSet<DeliveryVehicle> DeliveryVehicles { get; set; }
        public DbSet<DeliveryTrip> DeliveryTrips { get; set; }
        public DbSet<DeliveryTripOrder> DeliveryTripOrders { get; set; }

        // --- Danh mục Audit, Adjustment & Reconciliation (Phase 6) ---
        public DbSet<InventoryAudit> InventoryAudits { get; set; }
        public DbSet<InventoryAuditDetail> InventoryAuditDetails { get; set; }
        public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }
        public DbSet<InventoryAdjustmentDetail> InventoryAdjustmentDetails { get; set; }

        // --- Danh mục AI Chatbot & Hội thoại (Phase 3) ---
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Chỉ giữ lại Index Unique cho các bảng chưa tách file Configuration
            modelBuilder.Entity<Product>().HasIndex(s => s.Code).IsUnique();


            // 2. Set mặc định CẤM XÓA DÂY CHUYỀN (Restrict) cho toàn bộ các bảng
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 3. Quét các file Configuration trong Assembly ở bước cuối cùng
            // Cấu hình đặc thù trong Configuration sẽ ghi đè thiết lập Restrict mặc định
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SolarisDbContext).Assembly);
        }

        // Tự động xử lý Soft Delete cho các Entity kế thừa ISoftDelete
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<ISoftDelete>();

            foreach (var entry in entries)
            {
                // Kiểm tra các thực thể đang ở trạng thái Deleted
                if (entry.State == EntityState.Deleted)
                {
                    // Chuyển trạng thái sang Modified và kích hoạt cờ IsDeleted
                    entry.State = EntityState.Modified;

                    // Đánh dấu cờ đã xóa mềm và lưu lại thời gian xóa
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}