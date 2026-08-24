import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Package,
  ShieldCheck,
  Settings,
  Sun,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  LucideIcon,
  Apple,
  LogOut,
  ShoppingCart,
  ShoppingBag,
  Search,
} from 'lucide-react';

// 🔥 Import store và constants quyền
import { useAuthStore } from '../../stores/useAuthStore';
import { PERMISSIONS } from '../../constants/permissions';

// 1. CẤU HÌNH MENU (Đã bỏ basePath, gom nhóm chuẩn ERP)
interface MenuItem {
  id: string;
  label: string;
  icon: LucideIcon;
  path?: string;
  permission?: string;
  children?: { label: string; path: string; permission?: string }[];
}

const MENU_CONFIG: MenuItem[] = [
  {
    id: 'dashboard',
    label: 'Tổng quan',
    icon: LayoutDashboard,
    path: '/',
  },
  {
    id: 'sales',
    label: 'Bán hàng & Khách hàng',
    icon: ShoppingBag,
    children: [
      { label: 'Đơn hàng bán', path: '/orders', permission: PERMISSIONS.INVENTORY.VIEW },
      { label: 'Đơn hàng trả', path: '/customer-returns', permission: PERMISSIONS.INVENTORY.VIEW },
      { label: 'Danh sách Khách hàng', path: '/customers', permission: PERMISSIONS.CUSTOMER.VIEW },
      {
        label: 'Phân loại Khách hàng',
        path: '/customer-types',
        permission: PERMISSIONS.CUSTOMER.CONFIG,
      },
      {
        label: 'Phân nhóm Khách hàng',
        path: '/customer-groups',
        permission: PERMISSIONS.CUSTOMER.CONFIG,
      },
      {
        label: 'Phân bậc Khách hàng',
        path: '/customer-tiers',
        permission: PERMISSIONS.CUSTOMER.CONFIG,
      },
      {
        label: 'Danh sách khuyến mãi',
        path: '/promotions',
        permission: PERMISSIONS.PROMOTION.VIEW,
      },
    ],
  },
  {
    id: 'procurement',
    label: 'Mua hàng & Nhà cung cấp',
    icon: ShoppingCart,
    children: [
      { label: 'Đơn mua hàng', path: '/purchase-orders', permission: PERMISSIONS.INVENTORY.VIEW },
      {
        label: 'Danh sách Nhà cung cấp',
        path: '/suppliers',
        permission: PERMISSIONS.SUPPLIER.VIEW,
      },
      {
        label: 'Phân loại Nhà cung cấp',
        path: '/supplier-types',
        permission: PERMISSIONS.SUPPLIER.CONFIG,
      },
      {
        label: 'Sản phẩm Nhà cung cấp',
        path: '/supplier-products',
        permission: PERMISSIONS.SUPPLIER.VIEW,
      },
    ],
  },
  {
    id: 'inventory',
    label: 'Quản lý Kho bãi',
    icon: Package,
    children: [
      { label: 'Tổng quan Tồn kho', path: '/inventories', permission: PERMISSIONS.INVENTORY.VIEW },
      {
        label: 'Phiếu nhập kho',
        path: '/inventory-receipts',
        permission: PERMISSIONS.INVENTORY.VIEW,
      },
      {
        label: 'Phiếu xuất kho',
        path: '/inventory-issues',
        permission: PERMISSIONS.INVENTORY.VIEW,
      },
      {
        label: 'Chuyển kho nội bộ',
        path: '/inventory-transfers',
        permission: PERMISSIONS.INVENTORY.VIEW,
      },
      { label: 'Kiểm kê kho', path: '/inventory-audits', permission: PERMISSIONS.INVENTORY.VIEW },
      {
        label: 'Điều chỉnh tồn kho',
        path: '/inventory-adjustments',
        permission: PERMISSIONS.INVENTORY.VIEW,
      },
      {
        label: 'Sổ cái & Chốt ca',
        path: '/inventory-reconciliation',
        permission: PERMISSIONS.INVENTORY.VIEW,
      },
      { label: 'Danh sách Kho bãi', path: '/warehouses', permission: PERMISSIONS.WAREHOUSE.VIEW },
    ],
  },
  {
    id: 'catalog',
    label: 'Cấu hình Sản phẩm',
    icon: Apple,
    children: [
      { label: 'Danh sách Sản phẩm', path: '/products', permission: PERMISSIONS.PRODUCT.VIEW },
      {
        label: 'Biến thể Sản phẩm',
        path: '/product-variants',
        permission: PERMISSIONS.PRODUCT.VIEW,
      },
      {
        label: 'Danh mục Sản phẩm',
        path: '/product-categories',
        permission: PERMISSIONS.PRODUCT.CATEGORY_MANAGE,
      },
      {
        label: 'Nhóm danh mục',
        path: '/product-category-groups',
        permission: PERMISSIONS.PRODUCT.CATEGORY_MANAGE,
      },
      {
        label: 'Từ điển thuộc tính',
        path: '/attributes',
        permission: PERMISSIONS.ATTRIBUTE.MANAGE,
      },
      {
        label: 'Thuộc tính loại Sản phẩm',
        path: '/category-attributes',
        permission: PERMISSIONS.ATTRIBUTE.MANAGE,
      },
      { label: 'Đơn vị tính', path: '/uoms', permission: PERMISSIONS.UOM.VIEW },
      { label: 'Danh mục Đơn vị tính', path: '/uom-categories', permission: PERMISSIONS.UOM.VIEW },
      { label: 'Quy đổi Đơn vị tính', path: '/uom-conversions', permission: PERMISSIONS.UOM.VIEW },
    ],
  },
  {
    id: 'system',
    label: 'Hệ thống',
    icon: ShieldCheck,
    children: [
      { label: 'Quản lý Vai trò', path: '/roles', permission: PERMISSIONS.SYSTEM.ROLE_VIEW },
      { label: 'Quản lý Nhân sự', path: '/users', permission: PERMISSIONS.SYSTEM.USER_VIEW },
    ],
  },
];

export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const { userInfo, logout } = useAuthStore();
  const userPermissions = userInfo?.permissions || [];

  const [isCollapsed, setIsCollapsed] = useState(false);
  const [openMenus, setOpenMenus] = useState<string[]>([]);

  // 🔥 State cho thanh tìm kiếm
  const [searchTerm, setSearchTerm] = useState('');

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const hasPermission = (requiredPermission?: string) => {
    if (!requiredPermission) return true;
    return userPermissions.includes(requiredPermission);
  };

  // Lọc menu theo quyền (Dùng useMemo để tối ưu)
  const permittedMenu = useMemo(() => {
    return MENU_CONFIG.map((menu) => {
      if (menu.permission && !hasPermission(menu.permission)) return null;

      let allowedChildren = undefined;
      if (menu.children) {
        allowedChildren = menu.children.filter((child) => hasPermission(child.permission));
        if (allowedChildren.length === 0) return null;
      }

      return { ...menu, children: allowedChildren };
    }).filter(Boolean) as MenuItem[];
  }, [userPermissions]);

  // Lọc tiếp menu dựa trên từ khóa tìm kiếm
  const displayMenu = useMemo(() => {
    if (!searchTerm.trim()) return permittedMenu;

    const lowerTerm = searchTerm.toLowerCase();
    return permittedMenu
      .map((menu) => {
        const matchParent = menu.label.toLowerCase().includes(lowerTerm);
        const matchedChildren = menu.children?.filter((c) =>
          c.label.toLowerCase().includes(lowerTerm)
        );

        if (matchParent || (matchedChildren && matchedChildren.length > 0)) {
          return {
            ...menu,
            children: matchParent ? menu.children : matchedChildren,
          };
        }
        return null;
      })
      .filter(Boolean) as MenuItem[];
  }, [permittedMenu, searchTerm]);

  // Tự động mở menu cha nếu đang ở menu con HOẶC khi đang search
  useEffect(() => {
    if (searchTerm) {
      // Khi search, tự động mở bung tất cả các menu có kết quả
      const allParentIds = displayMenu.map((m) => m.id);
      setOpenMenus(allParentIds);
    } else {
      // Khi không search, mở menu chứa url hiện tại
      const activeParent = displayMenu.find((m) =>
        m.children?.some((c) => location.pathname.startsWith(c.path))
      );
      if (activeParent && !openMenus.includes(activeParent.id)) {
        setOpenMenus((prev) => [...prev, activeParent.id]);
      }
    }
  }, [location.pathname, searchTerm, displayMenu]);

  const toggleSubmenu = (id: string) => {
    if (isCollapsed) setIsCollapsed(false);
    setOpenMenus((prev) => (prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id]));
  };

  return (
    <aside
      className={`relative flex flex-col h-screen bg-white border-r border-slate-200 z-50 transition-all duration-300 ease-in-out ${isCollapsed ? 'w-20' : 'w-72'}`}
    >
      {/* --- LOGO AREA --- */}
      <div
        className={`h-16 flex items-center shrink-0 border-b border-slate-100 transition-all duration-300 ${isCollapsed ? 'justify-center' : 'px-6'}`}
      >
        <div className="flex items-center gap-3">
          <div className="bg-amber-400 p-2 rounded-lg flex items-center justify-center shrink-0 shadow-sm transition-transform duration-300 hover:rotate-12">
            <Sun size={24} className="text-white fill-white" />
          </div>
          {!isCollapsed && (
            <span className="font-black text-xl tracking-wider text-slate-800 whitespace-nowrap overflow-hidden transition-opacity duration-300">
              SOLARIS
            </span>
          )}
        </div>
      </div>

      {/* --- TOGGLE BUTTON --- */}
      <button
        onClick={() => setIsCollapsed(!isCollapsed)}
        className="absolute -right-3 top-20 bg-white border border-slate-200 rounded-full p-1 text-slate-400 hover:text-amber-600 hover:border-amber-400 hover:bg-amber-50 shadow-sm cursor-pointer z-50 transition-colors"
      >
        {isCollapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
      </button>

      {/* 🔥 THANH TÌM KIẾM --- */}
      {!isCollapsed && (
        <div className="px-4 py-3 shrink-0">
          <div className="relative flex items-center">
            <Search size={16} className="absolute left-3 text-slate-400" />
            <input
              type="text"
              placeholder="Tìm tính năng..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full bg-slate-50 border border-slate-200 rounded-lg pl-9 pr-3 py-2 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500 transition-all placeholder:text-slate-400"
            />
          </div>
        </div>
      )}

      {/* --- NAV AREA --- */}
      <nav
        className={`flex-1 overflow-y-auto overflow-x-hidden custom-scrollbar flex flex-col gap-1.5 ${isCollapsed ? 'py-4 px-3' : 'px-3 pb-4'}`}
      >
        {displayMenu.map((item) => {
          const Icon = item.icon;
          const isOpen = openMenus.includes(item.id);

          const isChildActive = item.children?.some((c) => location.pathname.startsWith(c.path));
          const isActive = (item.path && location.pathname === item.path) || isChildActive;

          return (
            <div key={item.id} className="flex flex-col">
              {/* Menu Button */}
              <button
                onClick={() => (item.children ? toggleSubmenu(item.id) : navigate(item.path!))}
                className={`w-full flex items-center justify-between p-3 rounded-xl transition-colors group
                                ${isActive ? 'bg-amber-50 text-amber-700' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'}
                                ${isCollapsed ? 'justify-center' : ''}`}
              >
                <div className="flex items-center gap-3 overflow-hidden">
                  <Icon
                    size={20}
                    className={`shrink-0 transition-colors ${isActive ? 'text-amber-600' : 'text-slate-400 group-hover:text-slate-600'}`}
                  />

                  {!isCollapsed && (
                    <span
                      className={`text-sm whitespace-nowrap transition-all duration-300 ${isActive ? 'font-semibold' : 'font-medium'}`}
                    >
                      {item.label}
                    </span>
                  )}
                </div>

                {item.children && !isCollapsed && (
                  <ChevronDown
                    size={16}
                    className={`shrink-0 transition-transform duration-300 ${isOpen ? 'rotate-180 text-amber-600' : 'text-slate-400'}`}
                  />
                )}
              </button>

              {/* Submenu Area */}
              {item.children && !isCollapsed && (
                /* 🔥 FIXED BUG: Đã xóa "|| isActive" khỏi class điều kiện, menu giờ chỉ mở nếu isOpen = true */
                <div
                  className={`grid transition-all duration-300 ease-in-out ${isOpen ? 'grid-rows-[1fr] opacity-100 mt-1' : 'grid-rows-[0fr] opacity-0'}`}
                >
                  <div className="overflow-hidden flex flex-col gap-1 relative">
                    <div className="absolute left-5 top-0 bottom-2 w-px bg-slate-200" />

                    {item.children.map((child) => {
                      const isSubActive = location.pathname.startsWith(child.path);
                      return (
                        <button
                          key={child.path}
                          onClick={() => navigate(child.path)}
                          className={`relative w-full flex items-center py-2.5 pl-11 pr-3 rounded-lg text-sm transition-all
                                                    ${isSubActive ? 'text-amber-700 font-medium bg-amber-50/50' : 'text-slate-500 font-normal hover:text-slate-900 hover:bg-slate-50'}`}
                        >
                          {isSubActive && (
                            <div className="absolute left-4.5 w-1.5 h-1.5 rounded-full bg-amber-500 ring-4 ring-white" />
                          )}
                          <span className="whitespace-nowrap text-left truncate flex-1">
                            {child.label}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          );
        })}

        {/* Text báo không tìm thấy */}
        {displayMenu.length === 0 && searchTerm && (
          <div className="text-center py-8 px-4 text-slate-500 text-sm">
            Không tìm thấy tính năng "{searchTerm}"
          </div>
        )}
      </nav>

      {/* --- FOOTER --- */}
      <div className="p-3 border-t border-slate-200 flex flex-col gap-1 shrink-0">
        <button
          className={`w-full flex items-center gap-3 p-3 rounded-xl transition-colors group text-slate-600 hover:bg-slate-50 hover:text-slate-900 ${isCollapsed ? 'justify-center' : ''}`}
        >
          <Settings
            size={20}
            className="shrink-0 text-slate-400 group-hover:rotate-90 group-hover:text-slate-600 transition-all duration-300"
          />
          {!isCollapsed && <span className="text-sm font-medium whitespace-nowrap">Cài đặt</span>}
        </button>

        <button
          onClick={handleLogout}
          className={`w-full flex items-center gap-3 p-3 rounded-xl transition-colors group text-slate-600 hover:bg-red-50 hover:text-red-600 ${isCollapsed ? 'justify-center' : ''}`}
        >
          <LogOut
            size={20}
            className="shrink-0 text-slate-400 group-hover:text-red-600 transition-colors"
          />
          {!isCollapsed && <span className="text-sm font-medium whitespace-nowrap">Đăng xuất</span>}
        </button>
      </div>
    </aside>
  );
};
