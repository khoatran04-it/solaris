import { BrowserRouter, Routes, Route } from 'react-router-dom';

// --- LAYOUT & AUTH ---
import Layout from './components/layout/Layout'; 
import LoginPage from './pages/login/LoginPage';
import ProtectedRoute from './components/auth/ProtectedRoute';
import { PERMISSIONS } from './constants/permissions';

// =============================================================================
// 📊 DASHBOARDS & EXECUTIVE REPORTING
// =============================================================================
import OverviewDashboard from './pages/dashboard/OverviewDashboard';
import SalesGeographyDashboard from './pages/dashboard/SalesGeographyDashboard';
import InventoryCapacityDashboard from './pages/dashboard/InventoryCapacityDashboard';
import QualityExpiryDashboard from './pages/dashboard/QualityExpiryDashboard';

// =============================================================================
// 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
// =============================================================================
import RoleList from './pages/role/RoleList';
import RoleForm from './pages/role/RoleForm';
import UserList from './pages/user/UserList';
import UserForm from './pages/user/UserForm';

// =============================================================================
// 📦 MODULE 2: PARTNER MANAGEMENT (SUPPLIERS & CUSTOMERS)
// =============================================================================
// Suppliers
import SupplierTypeList from './pages/supplierType/SupplierTypeList';
import SupplierTypeForm from './pages/supplierType/SupplierTypeForm';
import SupplierList from './pages/supplier/SupplierList';
import SupplierForm from './pages/supplier/SupplierForm';
import SupplierDetail from './pages/supplier/SupplierDetail';
import SupplierProductList from './pages/supplierProduct/SupplierProductList';
// Customers
import CustomerTypeList from './pages/customerType/CustomerTypeList';
import CustomerTypeForm from './pages/customerType/CustomerTypeForm';
import CustomerTierList from './pages/customerTier/CustomerTierList';
import CustomerTierForm from './pages/customerTier/CustomerTierForm';
import CustomerGroupList from './pages/customerGroup/CustomerGroupList';
import CustomerGroupForm from './pages/customerGroup/CustomerGroupForm';
import CustomerList from './pages/customer/CustomerList';
import CustomerForm from './pages/customer/CustomerForm';
import CustomerDetail from './pages/customer/CustomerDetail';

// =============================================================================
// 📦 MODULE 3: UNIT OF MEASURE & CONVERSIONS (UOM)
// =============================================================================
import UoMCategoryList from './pages/uomCategory/UoMCategoryList';
import UoMCategoryForm from './pages/uomCategory/UoMCategoryForm';
import UoMList from './pages/uom/UoMList';
import UoMForm from './pages/uom/UoMForm';
import UoMConversionList from './pages/uomConversion/UoMConversionList';
import UoMConversionForm from './pages/uomConversion/UoMConversionForm';

// =============================================================================
// 📦 MODULE 4: PRODUCT CATALOG & PRICING
// =============================================================================
import ProductCategoryGroupList from './pages/productCategoryGroup/ProductCategoryGroupList';
import ProductCategoryGroupForm from './pages/productCategoryGroup/ProductCategoryGroupForm';
import ProductCategoryList from './pages/productCategory/ProductCategoryList';
import ProductCategoryForm from './pages/productCategory/ProductCategoryForm';
import ProductList from './pages/product/ProductList';
import ProductForm from './pages/product/ProductForm';
import ProductVariantList from './pages/productVariant/ProductVariantList';
import ProductVariantForm from './pages/productVariant/ProductVariantForm';
import AttributeDefinitionList from './pages/attributeDefinition/AttributeDefinitionList';
import AttributeDefinitionForm from './pages/attributeDefinition/AttributeDefinitionForm';
import CategoryAttributeList from './pages/categoryAttribute/CategoryAttributeList';
import CategoryAttributeForm from './pages/categoryAttribute/CategoryAttributeForm';
import PromotionCampaignList from './pages/promotionCampaign/PromotionCampaignList';
import PromotionCampaignForm from './pages/promotionCampaign/PromotionCampaignForm';

// =============================================================================
// 📦 MODULE 5: WAREHOUSE CORE & INVENTORY LEDGER
// =============================================================================
import WarehouseList from './pages/warehouse/WarehouseList';
import WarehouseForm from './pages/warehouse/WarehouseForm';
import InventoryDashboard from './pages/inventory/InventoryDashboard';

// =============================================================================
// 📦 MODULE 6: SUPPLY CHAIN OPERATIONS (SCM)
// =============================================================================
// Purchasing (Mua hàng)
import PurchaseOrderList from './pages/purchaseOrder/PurchaseOrderList';
import PurchaseOrderForm from './pages/purchaseOrder/PurchaseOrderForm';
import PurchaseOrderDetail from './pages/purchaseOrder/PurchaseOrderDetail';
import InventoryReceiptList from './pages/inventoryReceipt/InventoryReceiptList';
import InventoryReceiptForm from './pages/inventoryReceipt/InventoryReceiptForm';
import InventoryReceiptDetail from './pages/inventoryReceipt/InventoryReceiptDetail';
// Sales (Bán hàng)
import OrderList from './pages/order/OrderList';
import OrderForm from './pages/order/OrderForm';
import OrderDetail from './pages/order/OrderDetail';
import InventoryIssueList from './pages/inventoryIssue/InventoryIssueList';
import InventoryIssueForm from './pages/inventoryIssue/InventoryIssueForm';
import InventoryIssueDetail from './pages/inventoryIssue/InventoryIssueDetail';
// Transfer & Returns (Chuyển kho & Trả hàng)
import InventoryTransferList from './pages/inventoryTransfer/InventoryTransferList';
import InventoryTransferForm from './pages/inventoryTransfer/InventoryTransferForm';
import InventoryTransferDetail from './pages/inventoryTransfer/InventoryTransferDetail';
import CustomerReturnList from './pages/customerReturn/CustomerReturnList';
import CustomerReturnForm from './pages/customerReturn/CustomerReturnForm';
import CustomerReturnDetail from './pages/customerReturn/CustomerReturnDetail';

// =============================================================================
// 📦 MODULE 7: INVENTORY AUDIT, ADJUSTMENT & RECONCILIATION
// =============================================================================
import InventoryAuditList from './pages/inventoryAudit/InventoryAuditList';
import InventoryAuditForm from './pages/inventoryAudit/InventoryAuditForm';
import InventoryAuditDetail from './pages/inventoryAudit/InventoryAuditDetail';
import InventoryAdjustmentList from './pages/inventoryAdjustment/InventoryAdjustmentList';
import InventoryAdjustmentForm from './pages/inventoryAdjustment/InventoryAdjustmentForm';
import InventoryAdjustmentDetail from './pages/inventoryAdjustment/InventoryAdjustmentDetail';
import InventoryReconciliation from './pages/inventory/InventoryReconciliation';

// =============================================================================
// 📦 MODULE 8: TRANSPORTATION & DISPATCH (COLD-CHAIN FLEET)
// =============================================================================
import TransportationDashboardPage from './pages/transportation/TransportationDashboardPage';
import VehicleManagementPage from './pages/transportation/VehicleManagementPage';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        
        {/* Public Routes */}
        <Route path="/login" element={<LoginPage />} />

        {/* Private Routes (Bọc trong ProtectedRoute) */}
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<Layout />}>
            {/* 📊 DASHBOARDS & EXECUTIVE REPORTING */}
            <Route index element={<OverviewDashboard />} />
            <Route path="dashboards/sales" element={<SalesGeographyDashboard />} />
            <Route path="dashboards/inventory" element={<InventoryCapacityDashboard />} />
            <Route path="dashboards/quality" element={<QualityExpiryDashboard />} />

            {/* 📦 MODULE 1: IAM & PHÂN QUYỀN (RÀNG BUỘC RBAC) */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.SYSTEM.ROLE_VIEW} />}>
              <Route path="roles" element={<RoleList />} />
              <Route path="roles/create" element={<RoleForm />} />
              <Route path="roles/edit/:id" element={<RoleForm />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.SYSTEM.USER_VIEW} />}>
              <Route path="users" element={<UserList />} />
              <Route path="users/create" element={<UserForm />} />
              <Route path="users/edit/:id" element={<UserForm />} />
            </Route>

            {/* 📦 MODULE 2: PARTNER MANAGEMENT */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.SUPPLIER.VIEW} />}>
              <Route path="supplier-types" element={<SupplierTypeList />} />
              <Route path="supplier-types/create" element={<SupplierTypeForm />} />
              <Route path="supplier-types/edit/:id" element={<SupplierTypeForm />} />
              <Route path="suppliers" element={<SupplierList />} />
              <Route path="suppliers/create" element={<SupplierForm />} />
              <Route path="suppliers/edit/:id" element={<SupplierForm />} />
              <Route path="suppliers/:id" element={<SupplierDetail />} />
              <Route path="supplier-products" element={<SupplierProductList />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.CUSTOMER.VIEW} />}>
              <Route path="customer-types" element={<CustomerTypeList />} />
              <Route path="customer-types/create" element={<CustomerTypeForm />} />
              <Route path="customer-types/edit/:id" element={<CustomerTypeForm />} />
              <Route path="customer-tiers" element={<CustomerTierList />} />
              <Route path="customer-tiers/create" element={<CustomerTierForm />} />
              <Route path="customer-tiers/edit/:id" element={<CustomerTierForm />} />
              <Route path="customer-groups" element={<CustomerGroupList />} />
              <Route path="customer-groups/create" element={<CustomerGroupForm />} />
              <Route path="customer-groups/edit/:id" element={<CustomerGroupForm />} />
              <Route path="customers" element={<CustomerList />} />
              <Route path="customers/create" element={<CustomerForm />} />
              <Route path="customers/edit/:id" element={<CustomerForm />} />
              <Route path="customers/:id" element={<CustomerDetail />} />
            </Route>

            {/* 📦 MODULE 3: UNIT OF MEASURE (UOM) */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.UOM.VIEW} />}>
              <Route path="uom-categories" element={<UoMCategoryList />} />
              <Route path="uom-categories/create" element={<UoMCategoryForm />} />
              <Route path="uom-categories/edit/:id" element={<UoMCategoryForm />} />
              <Route path="uoms" element={<UoMList />} />
              <Route path="uoms/create" element={<UoMForm />} />
              <Route path="uoms/edit/:id" element={<UoMForm />} />
              <Route path="uom-conversions" element={<UoMConversionList />} />
              <Route path="uom-conversions/create" element={<UoMConversionForm />} />
              <Route path="uom-conversions/edit/:id" element={<UoMConversionForm />} />
            </Route>

            {/* 📦 MODULE 4: PRODUCT CATALOG & PRICING */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.PRODUCT.VIEW} />}>
              <Route path="product-category-groups" element={<ProductCategoryGroupList />} />
              <Route path="product-category-groups/create" element={<ProductCategoryGroupForm />} />
              <Route path="product-category-groups/edit/:id" element={<ProductCategoryGroupForm />} />
              <Route path="product-categories" element={<ProductCategoryList />} />
              <Route path="product-categories/create" element={<ProductCategoryForm />} />
              <Route path="product-categories/edit/:id" element={<ProductCategoryForm />} />
              <Route path="products" element={<ProductList />} />
              <Route path="products/create" element={<ProductForm />} />
              <Route path="products/edit/:id" element={<ProductForm />} />
              <Route path="product-variants" element={<ProductVariantList />} />
              <Route path="product-variants/create" element={<ProductVariantForm />} />
              <Route path="product-variants/edit/:id" element={<ProductVariantForm />} />
              <Route path="attributes" element={<AttributeDefinitionList />} />
              <Route path="attributes/create" element={<AttributeDefinitionForm />} />
              <Route path="attributes/edit/:id" element={<AttributeDefinitionForm />} />
              <Route path="category-attributes" element={<CategoryAttributeList />} />
              <Route path="category-attributes/create" element={<CategoryAttributeForm />} />
              <Route path="category-attributes/edit/:id" element={<CategoryAttributeForm />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.PROMOTION.VIEW} />}>
              <Route path="promotions" element={<PromotionCampaignList />} />
              <Route path="promotions/create" element={<PromotionCampaignForm />} />
              <Route path="promotions/edit/:id" element={<PromotionCampaignForm />} />
            </Route>

            {/* 📦 MODULE 5: WAREHOUSE CORE & LEDGER */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.WAREHOUSE.VIEW} />}>
              <Route path="warehouses" element={<WarehouseList />} />
              <Route path="warehouses/create" element={<WarehouseForm />} />
              <Route path="warehouses/edit/:id" element={<WarehouseForm />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.VIEW} />}>
              <Route path="inventories" element={<InventoryDashboard />} />
              <Route path="inventory-reconciliation" element={<InventoryReconciliation />} />
            </Route>

            {/* 📦 MODULE 6: SUPPLY CHAIN OPERATIONS (SCM) */}
            {/* Purchase Order Process */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.PURCHASE.VIEW} />}>
              <Route path="purchase-orders" element={<PurchaseOrderList />} />
              <Route path="purchase-orders/create" element={<PurchaseOrderForm />} />
              <Route path="purchase-orders/edit/:id" element={<PurchaseOrderForm />} />
              <Route path="purchase-orders/:id" element={<PurchaseOrderDetail />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.RECEIPT_VIEW} />}>
              <Route path="inventory-receipts" element={<InventoryReceiptList />} />
              <Route path="inventory-receipts/create" element={<InventoryReceiptForm />} />
              <Route path="inventory-receipts/:id" element={<InventoryReceiptDetail />} />
            </Route>

            {/* Sales & Issue Process */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.ORDER.VIEW} />}>
              <Route path="orders" element={<OrderList />} />
              <Route path="orders/create" element={<OrderForm />} />
              <Route path="orders/:id" element={<OrderDetail />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.ISSUE_VIEW} />}>
              <Route path="inventory-issues" element={<InventoryIssueList />} />
              <Route path="inventory-issues/create" element={<InventoryIssueForm />} />
              <Route path="inventory-issues/:id" element={<InventoryIssueDetail />} />
            </Route>

            {/* Transfer & Return */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.TRANSFER_VIEW} />}>
              <Route path="inventory-transfers" element={<InventoryTransferList />} />
              <Route path="inventory-transfers/create" element={<InventoryTransferForm />} />
              <Route path="inventory-transfers/:id" element={<InventoryTransferDetail />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.RETURN.VIEW} />}>
              <Route path="customer-returns" element={<CustomerReturnList />} />
              <Route path="customer-returns/create" element={<CustomerReturnForm />} />
              <Route path="customer-returns/:id" element={<CustomerReturnDetail />} />
            </Route>

            {/* 📦 MODULE 7: AUDIT, ADJUSTMENT & RECONCILIATION */}
            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.AUDIT_VIEW} />}>
              <Route path="inventory-audits" element={<InventoryAuditList />} />
              <Route path="inventory-audits/create" element={<InventoryAuditForm />} />
              <Route path="inventory-audits/:id" element={<InventoryAuditDetail />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermission={PERMISSIONS.INVENTORY.ADJUSTMENT_VIEW} />}>
              <Route path="inventory-adjustments" element={<InventoryAdjustmentList />} />
              <Route path="inventory-adjustments/create" element={<InventoryAdjustmentForm />} />
              <Route path="inventory-adjustments/:id" element={<InventoryAdjustmentDetail />} />
            </Route>

            {/* 📦 MODULE 8: TRANSPORTATION & DISPATCH */}
            <Route path="transportation/dashboard" element={<TransportationDashboardPage />} />
            <Route path="transportation/vehicles" element={<VehicleManagementPage />} />

            {/* 404 Not Found */}
            <Route path="*" element={<h2>404 - Không tìm thấy trang</h2>} />
          </Route>
        </Route>

      </Routes>
    </BrowserRouter>
  );
}

export default App;