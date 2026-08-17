import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';

// 1. Import Layout
import Layout from './components/layout/Layout'; 

import LoginPage from './pages/login/LoginPage';
import ProtectedRoute from './components/auth/ProtectedRoute';

// Import các trang quản trị
import RoleList from './pages/role/RoleList';
import RoleForm from './pages/role/RoleForm';

import UserList from './pages/user/UserList';
import UserForm from './pages/user/UserForm';

import SupplierTypeList from './pages/supplierType/SupplierTypeList';
import SupplierTypeForm from './pages/supplierType/SupplierTypeForm';
import SupplierList from './pages/supplier/SupplierList';
import SupplierForm from './pages/supplier/SupplierForm';
import SupplierDetail from './pages/supplier/SupplierDetail';
import CustomerTypeList from './pages/customerType/CustomerTypeList';
import CustomerTypeForm from './pages/customerType/CustomerTypeForm';
import CustomerTierList from './pages/customerTier/CustomerTierList';
import CustomerTierForm from './pages/customerTier/CustomerTierForm';
import CustomerGroupList from './pages/customerGroup/CustomerGroupList';
import CustomerGroupForm from './pages/customerGroup/CustomerGroupForm';
import CustomerList from './pages/customer/CustomerList';
import CustomerForm from './pages/customer/CustomerForm';
import CustomerDetail from './pages/customer/CustomerDetail';
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
import CategoryAttributeForm from './pages/categoryAttribute/CategoryAttributeForm';
import CategoryAttributeList from './pages/categoryAttribute/CategoryAttributeList';
import PromotionCampaignList from './pages/promotionCampaign/PromotionCampaignList';
import PromotionCampaignForm from './pages/promotionCampaign/PromotionCampaignForm';
import UoMCategoryList from './pages/uomCategory/UoMCategoryList';
import UoMCategoryForm from './pages/uomCategory/UoMCategoryForm';
import UoMList from './pages/uom/UoMList';
import UoMForm from './pages/uom/UoMForm';
import UoMConversionList from './pages/uomConversion/UoMConversionList';
import UoMConversionForm from './pages/uomConversion/UoMConversionForm';
import WarehouseList from './pages/warehouse/WarehouseList';
import WarehouseForm from './pages/warehouse/WarehouseForm';

import InventoryDashboard from './pages/inventory/InventoryDashboard';

import PurchaseOrderList from './pages/purchaseOrder/PurchaseOrderList';
import PurchaseOrderForm from './pages/purchaseOrder/PurchaseOrderForm';
import PurchaseOrderDetail from './pages/purchaseOrder/PurchaseOrderDetail';

import InventoryReceiptList from './pages/inventoryReceipt/InventoryReceiptList';
import InventoryReceiptForm from './pages/inventoryReceipt/InventoryReceiptForm';
import InventoryReceiptDetail from './pages/inventoryReceipt/InventoryReceiptDetail';

import OrderList from './pages/order/OrderList';
import OrderForm from './pages/order/OrderForm';
import OrderDetail from './pages/order/OrderDetail';

import InventoryIssueList from './pages/inventoryIssue/InventoryIssueList';
import InventoryIssueForm from './pages/inventoryIssue/InventoryIssueForm';
import InventoryIssueDetail from './pages/inventoryIssue/InventoryIssueDetail';

import InventoryTransferList from './pages/inventoryTransfer/InventoryTransferList';
import InventoryTransferForm from './pages/inventoryTransfer/InventoryTransferForm';
import InventoryTransferDetail from './pages/inventoryTransfer/InventoryTransferDetail';

import CustomerReturnList from './pages/customerReturn/CustomerReturnList';
import CustomerReturnForm from './pages/customerReturn/CustomerReturnForm';
import CustomerReturnDetail from './pages/customerReturn/CustomerReturnDetail';

import InventoryAuditList from './pages/inventoryAudit/InventoryAuditList';
import InventoryAuditForm from './pages/inventoryAudit/InventoryAuditForm';
import InventoryAuditDetail from './pages/inventoryAudit/InventoryAuditDetail';

import InventoryAdjustmentList from './pages/inventoryAdjustment/InventoryAdjustmentList';
import InventoryAdjustmentForm from './pages/inventoryAdjustment/InventoryAdjustmentForm';
import InventoryAdjustmentDetail from './pages/inventoryAdjustment/InventoryAdjustmentDetail';

import InventoryReconciliation from './pages/inventory/InventoryReconciliation';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        
        {/* 🔥 BỔ SUNG: Route Công Khai (Ai cũng vào được) */}
        <Route path="/login" element={<LoginPage />} />

        {/* 🔥 BỔ SUNG: Bọc toàn bộ các trang quản trị bằng ProtectedRoute */}
        <Route element={<ProtectedRoute />}>
          
          {/* Layout chính của hệ thống */}
          <Route path="/" element={<Layout />}>

            {/* Dashboard mặc định */}
            <Route index element={<h2>Chào mừng đến với hệ thống quản trị Solaris!</h2>} />

            {/* Phân Loại NCC */}
            <Route path="supplier-types" element={<SupplierTypeList />} />
            <Route path="supplier-types/create" element={<SupplierTypeForm />} />
            <Route path="supplier-types/edit/:id" element={<SupplierTypeForm />} />
            
            {/* Nhà Cung Cấp */}
            <Route path="suppliers" element={<SupplierList />} />
            <Route path="suppliers/create" element={<SupplierForm />} />
            <Route path="suppliers/edit/:id" element={<SupplierForm />} />
            <Route path="suppliers/:id" element={<SupplierDetail />} />

            {/* Phân Loại Khách Hàng */}
            <Route path="customer-types" element={<CustomerTypeList />} />
            <Route path="customer-types/create" element={<CustomerTypeForm />} />
            <Route path="customer-types/edit/:id" element={<CustomerTypeForm />} />
            
            {/* Cấp Bậc Khách Hàng */}
            <Route path="customer-tiers" element={<CustomerTierList />} />
            <Route path="customer-tiers/create" element={<CustomerTierForm />} />
            <Route path="customer-tiers/edit/:id" element={<CustomerTierForm />} />

            {/* Nhóm Khách Hàng */}
            <Route path="customer-groups" element={<CustomerGroupList />} />
            <Route path="customer-groups/create" element={<CustomerGroupForm />} />
            <Route path="customer-groups/edit/:id" element={<CustomerGroupForm />} />

            {/* Khách Hàng */}
            <Route path="customers" element={<CustomerList />} />
            <Route path="customers/create" element={<CustomerForm />} />
            <Route path="customers/edit/:id" element={<CustomerForm />} />
            <Route path="customers/:id" element={<CustomerDetail />} />

            {/* Nhóm Danh Mục Sản Phẩm */}
            <Route path="product-category-groups" element={<ProductCategoryGroupList />} />
            <Route path="product-category-groups/create" element={<ProductCategoryGroupForm />} />
            <Route path="product-category-groups/edit/:id" element={<ProductCategoryGroupForm />} />

            {/* Sản phẩm */}
            <Route path="products" element={<ProductList />} />
            <Route path="products/create" element={<ProductForm />} />
            <Route path="products/edit/:id" element={<ProductForm />} />

            {/* Danh Mục Sản Phẩm */}
            <Route path="product-categories" element={<ProductCategoryList />} />
            <Route path="product-categories/create" element={<ProductCategoryForm />} />
            <Route path="product-categories/edit/:id" element={<ProductCategoryForm />} />

            {/* Biến thể sản phẩm */}
            <Route path="product-variants" element={<ProductVariantList />} />
            <Route path="product-variants/create" element={<ProductVariantForm />} />
            <Route path="product-variants/edit/:id" element={<ProductVariantForm />} />

            {/* Từ điển thuộc tính */}
            <Route path="attributes" element={<AttributeDefinitionList />} />
            <Route path="attributes/create" element={<AttributeDefinitionForm />} />
            <Route path="attributes/edit/:id" element={<AttributeDefinitionForm />} />

            {/* Liên kết thuộc tính với danh mục */}
            <Route path="category-attributes" element={<CategoryAttributeList />} />
            <Route path="category-attributes/create" element={<CategoryAttributeForm />} />
            <Route path="category-attributes/edit/:id" element={<CategoryAttributeForm />} />

            {/* Danh sách khuyến mãi */}
            <Route path="promotions" element={<PromotionCampaignList />} />
            <Route path="promotions/create" element={<PromotionCampaignForm />} />
            <Route path="promotions/edit/:id" element={<PromotionCampaignForm />} />

            {/* Loại Đơn Vị Tính */}
            <Route path="uom-categories" element={<UoMCategoryList />} />
            <Route path="uom-categories/create" element={<UoMCategoryForm />} />
            <Route path="uom-categories/edit/:id" element={<UoMCategoryForm />} />

            {/* Đơn Vị Tính */}
            <Route path="uoms" element={<UoMList />} />
            <Route path="uoms/create" element={<UoMForm />} />
            <Route path="uoms/edit/:id" element={<UoMForm />} />

            <Route path="uom-conversions" element={<UoMConversionList />} />
            <Route path="uom-conversions/create" element={<UoMConversionForm />} />
            <Route path="uom-conversions/edit/:id" element={<UoMConversionForm />} />

            {/* Kho Vật Tư */}
            <Route path="warehouses" element={<WarehouseList />} />
            <Route path="warehouses/create" element={<WarehouseForm />} />
            <Route path="warehouses/edit/:id" element={<WarehouseForm />} />

            {/* Tồn Kho */}
            <Route path="inventories" element={<InventoryDashboard />} />

            {/* Đơn Mua Hàng (Phase 3) */}
            <Route path="purchase-orders" element={<PurchaseOrderList />} />
            <Route path="purchase-orders/create" element={<PurchaseOrderForm />} />
            <Route path="purchase-orders/edit/:id" element={<PurchaseOrderForm />} />
            <Route path="purchase-orders/:id" element={<PurchaseOrderDetail />} />

            {/* Phiếu Nhập Kho (Phase 3) */}
            <Route path="inventory-receipts" element={<InventoryReceiptList />} />
            <Route path="inventory-receipts/create" element={<InventoryReceiptForm />} />
            <Route path="inventory-receipts/:id" element={<InventoryReceiptDetail />} />

            {/* Đơn Hàng Bán & Định Tuyến Kho (Phase 4) */}
            <Route path="orders" element={<OrderList />} />
            <Route path="orders/create" element={<OrderForm />} />
            <Route path="orders/:id" element={<OrderDetail />} />

            {/* Phiếu Xuất Kho & Phân Lô FEFO (Phase 4) */}
            <Route path="inventory-issues" element={<InventoryIssueList />} />
            <Route path="inventory-issues/create" element={<InventoryIssueForm />} />
            <Route path="inventory-issues/:id" element={<InventoryIssueDetail />} />

            {/* Chuyển Kho Liên Chi Nhánh (Phase 5) */}
            <Route path="inventory-transfers" element={<InventoryTransferList />} />
            <Route path="inventory-transfers/create" element={<InventoryTransferForm />} />
            <Route path="inventory-transfers/:id" element={<InventoryTransferDetail />} />

            {/* Khách Trả Hàng & Nghiệm Thu QC (Phase 5) */}
            <Route path="customer-returns" element={<CustomerReturnList />} />
            <Route path="customer-returns/create" element={<CustomerReturnForm />} />
            <Route path="customer-returns/:id" element={<CustomerReturnDetail />} />

            {/* Kiểm Kê Kho (Phase 6) */}
            <Route path="inventory-audits" element={<InventoryAuditList />} />
            <Route path="inventory-audits/create" element={<InventoryAuditForm />} />
            <Route path="inventory-audits/:id" element={<InventoryAuditDetail />} />

            {/* Điều Chỉnh & Xuất Hủy Tồn Kho (Phase 6) */}
            <Route path="inventory-adjustments" element={<InventoryAdjustmentList />} />
            <Route path="inventory-adjustments/create" element={<InventoryAdjustmentForm />} />
            <Route path="inventory-adjustments/:id" element={<InventoryAdjustmentDetail />} />

            {/* Sổ Cái & Chốt Ca Tồn Kho (Phase 6) */}
            <Route path="inventory-reconciliation" element={<InventoryReconciliation />} />

            {/* Vai Trò */}
            <Route path="roles" element={<RoleList />} />
            <Route path="roles/create" element={<RoleForm />} />
            <Route path="roles/edit/:id" element={<RoleForm />} />

            {/* Người Dùng */}
            <Route path="users" element={<UserList />} />
            <Route path="users/create" element={<UserForm />} />
            <Route path="users/edit/:id" element={<UserForm />} />

            {/* 404 */}
            <Route path="*" element={<h2>404 - Không tìm thấy trang</h2>} />
          </Route>
          
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;