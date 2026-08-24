// Danh sách Mã quyền khớp 100% với Database Backend
export const PERMISSIONS = {
  // 1. Hệ thống
  SYSTEM: {
    ROLE_VIEW: 'ROLE_VIEW',
    ROLE_MANAGE: 'ROLE_MANAGE',
    USER_VIEW: 'USER_VIEW',
    USER_MANAGE: 'USER_MANAGE',
  },

  // 2. Nhà cung cấp (Bao gồm Supplier, SupplierType)
  SUPPLIER: {
    VIEW: 'SUPPLIER_VIEW',
    MANAGE: 'SUPPLIER_MANAGE',
    CONFIG: 'SUPPLIER_CONFIG', // Dành cho các trang Type
  },

  // 3. Khách hàng (Bao gồm Customer, Type, Tier, Group)
  CUSTOMER: {
    VIEW: 'CUSTOMER_VIEW',
    MANAGE: 'CUSTOMER_MANAGE',
    CONFIG: 'CUSTOMER_CONFIG', // Dành cho Type, Tier, Group
  },

  // 4. Sản phẩm (Bao gồm Product, Variant, Category, CategoryGroup)
  PRODUCT: {
    VIEW: 'PRODUCT_VIEW',
    MANAGE: 'PRODUCT_MANAGE',
    CATEGORY_MANAGE: 'CATEGORY_MANAGE',
  },

  // 5. Thuộc tính (AttributeDefinition, CategoryAttribute)
  ATTRIBUTE: {
    MANAGE: 'ATTRIBUTE_MANAGE', // Thường thuộc tính ít người xem đơn thuần, gộp luôn vào Manage
  },

  // 6. Đơn vị tính (UoM, UoMCategory, UoMConversion)
  UOM: {
    VIEW: 'UOM_VIEW',
    MANAGE: 'UOM_MANAGE',
  },

  // 7. Khuyến mãi (PromotionCampaign)
  PROMOTION: {
    VIEW: 'PROMOTION_VIEW',
    MANAGE: 'PROMOTION_MANAGE',
  },

  // 8. Kho (Warehouse)
  WAREHOUSE: {
    VIEW: 'WAREHOUSE_VIEW',
    MANAGE: 'WAREHOUSE_MANAGE',
  },

  //9. Kho Tổng (Inventory)
  INVENTORY: {
    VIEW: 'INVENTORY_VIEW',
    MANAGE: 'INVENTORY_MANAGE',
  },
};
