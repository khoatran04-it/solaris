import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { 
    LayoutDashboard, Home, Package, Users, Settings, 
    Sun, ChevronLeft, ChevronRight, ChevronDown, LucideIcon,
    Apple, Ruler, ShieldCheck, LogOut, ShoppingCart, ClipboardCheck,
    ShoppingBag, ArrowLeftRight, ClipboardList, Scale
} from 'lucide-react';

// 🔥 Import store và constants quyền
import { useAuthStore } from '../../stores/useAuthStore';
import { PERMISSIONS } from '../../constants/permissions';

// 1. CẤU HÌNH MENU (Thêm trường permission)
interface MenuItem {
    id: string;
    label: string;
    icon: LucideIcon;
    path?: string;
    basePath?: string;
    permission?: string; // Quyền của Menu cha
    children?: { label: string; path: string; permission?: string }[]; // Quyền của Menu con
}

const MENU_CONFIG: MenuItem[] = [
    { 
        id: 'dashboard', 
        label: 'Dashboard', 
        icon: LayoutDashboard, 
        path: '/' 
        // Không truyền permission -> Ai đăng nhập cũng thấy
    },
    { 
        id: 'supplier', 
        label: 'Nhà cung cấp', 
        icon: Home, 
        basePath: '/supplier',
        permission: PERMISSIONS.SUPPLIER.VIEW,
        children: [
            { label: 'Phân loại NCC', path: '/supplier-types', permission: PERMISSIONS.SUPPLIER.CONFIG },
            { label: 'Danh sách NCC', path: '/suppliers', permission: PERMISSIONS.SUPPLIER.VIEW },
            { label: 'Bảng giá & SP NCC', path: '/supplier-products', permission: PERMISSIONS.SUPPLIER.VIEW },
        ]
    },
    { 
        id: 'customer', 
        label: 'Khách hàng', 
        icon: Users, 
        basePath: '/customer',
        permission: PERMISSIONS.CUSTOMER.VIEW,
        children: [
            { label: 'Phân loại KH', path: '/customer-types', permission: PERMISSIONS.CUSTOMER.CONFIG },
            { label: 'Phân nhóm KH', path: '/customer-groups', permission: PERMISSIONS.CUSTOMER.CONFIG },
            { label: 'Phân bậc KH', path: '/customer-tiers', permission: PERMISSIONS.CUSTOMER.CONFIG },
            { label: 'Danh sách KH', path: '/customers', permission: PERMISSIONS.CUSTOMER.VIEW },
        ]
    },
    {
        id: 'product',
        label: 'Sản phẩm',
        icon: Apple,
        basePath: '/products',
        permission: PERMISSIONS.PRODUCT.VIEW,
        children: [
            { label: 'Danh sách SP', path: '/products', permission: PERMISSIONS.PRODUCT.VIEW },
            { label: 'Biến thể SP', path: '/product-variants', permission: PERMISSIONS.PRODUCT.VIEW },
            { label: 'Nhóm danh mục', path: '/product-category-groups', permission: PERMISSIONS.PRODUCT.CATEGORY_MANAGE },
            { label: 'Danh mục SP', path: '/product-categories', permission: PERMISSIONS.PRODUCT.CATEGORY_MANAGE },
            { label: 'Từ điển thuộc tính', path: '/attributes', permission: PERMISSIONS.ATTRIBUTE.MANAGE },
            { label: 'Thuộc tính loại SP', path: '/category-attributes', permission: PERMISSIONS.ATTRIBUTE.MANAGE },
            { label: 'Danh sách khuyến mãi', path: '/promotions', permission: PERMISSIONS.PROMOTION.VIEW },
        ]
    },
    {
        id: 'uom',
        label: 'Đơn vị tính',
        icon: Ruler,
        basePath: '/uom',
        permission: PERMISSIONS.UOM.VIEW,
        children: [
            { label: 'Danh mục ĐVT', path: '/uom-categories', permission: PERMISSIONS.UOM.VIEW },
            { label: 'Đơn vị tính', path: '/uoms', permission: PERMISSIONS.UOM.VIEW },
            { label: 'Danh sách quy đổi', path: '/uom-conversions', permission: PERMISSIONS.UOM.VIEW },
        ]
    },
    { 
        id: 'warehouse', 
        label: 'Kho Vật Tư', 
        icon: Package, 
        basePath: '/warehouse',
        permission: PERMISSIONS.WAREHOUSE.VIEW,
        children: [
            { label: 'Danh sách kho', path: '/warehouses', permission: PERMISSIONS.WAREHOUSE.VIEW },
            { label: 'Tồn kho', path: '/inventories', permission: PERMISSIONS.INVENTORY.VIEW },
        ]
    },
    {
        id: 'procurement',
        label: 'Mua hàng & Nhập kho',
        icon: ShoppingCart,
        basePath: '/purchase',
        permission: PERMISSIONS.INVENTORY.VIEW,
        children: [
            { label: 'Đơn mua hàng', path: '/purchase-orders', permission: PERMISSIONS.INVENTORY.VIEW },
            { label: 'Phiếu nhập kho', path: '/inventory-receipts', permission: PERMISSIONS.INVENTORY.VIEW },
        ]
    },
    {
        id: 'sales',
        label: 'Bán hàng & Xuất kho',
        icon: ShoppingBag,
        basePath: '/orders',
        permission: PERMISSIONS.INVENTORY.VIEW,
        children: [
            { label: 'Đơn bán hàng', path: '/orders', permission: PERMISSIONS.INVENTORY.VIEW },
            { label: 'Phiếu xuất kho', path: '/inventory-issues', permission: PERMISSIONS.INVENTORY.VIEW },
        ]
    },
    {
        id: 'logistics',
        label: 'Điều phối & Trả hàng',
        icon: ArrowLeftRight,
        basePath: '/logistics',
        permission: PERMISSIONS.INVENTORY.VIEW,
        children: [
            { label: 'Chuyển kho nội bộ', path: '/inventory-transfers', permission: PERMISSIONS.INVENTORY.VIEW },
            { label: 'Khách hàng trả hàng', path: '/customer-returns', permission: PERMISSIONS.INVENTORY.VIEW },
        ]
    },
    {
        id: 'audit',
        label: 'Kiểm kê & Sổ cái',
        icon: ClipboardList,
        basePath: '/audit',
        permission: PERMISSIONS.INVENTORY.VIEW,
        children: [
            { label: 'Kiểm kê kho', path: '/inventory-audits', permission: PERMISSIONS.INVENTORY.VIEW },
            { label: 'Điều chỉnh tồn kho', path: '/inventory-adjustments', permission: PERMISSIONS.INVENTORY.VIEW },
            { label: 'Sổ cái & Chốt ca', path: '/inventory-reconciliation', permission: PERMISSIONS.INVENTORY.VIEW },
        ]
    },

    
    
    // 🔥 Nhóm quản trị hệ thống (Roles & Users)
    {
        id: 'system',
        label: 'Hệ thống',
        icon: ShieldCheck,
        basePath: '/system',
        permission: PERMISSIONS.SYSTEM.USER_VIEW, // Tạm lấy quyền xem user làm gốc
        children: [
            { label: 'Quản lý Vai trò', path: '/roles', permission: PERMISSIONS.SYSTEM.ROLE_VIEW },
            { label: 'Quản lý Nhân sự', path: '/users', permission: PERMISSIONS.SYSTEM.USER_VIEW },
        ]
    }
];

export const Sidebar: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    
    // 🔥 Lấy thông tin quyền VÀ HÀM LOGOUT từ Zustand
    const { userInfo, logout } = useAuthStore();
    const userPermissions = userInfo?.permissions || [];

    const [isCollapsed, setIsCollapsed] = useState(false);
    const [openMenus, setOpenMenus] = useState<string[]>([]);

    // 🔥 HÀM XỬ LÝ ĐĂNG XUẤT
    const handleLogout = () => {
        logout(); // Xóa Token và State trong Zustand/LocalStorage
        navigate('/login'); // Đá văng về trang Đăng nhập
    };

    // 🔥 HÀM KIỂM TRA QUYỀN
    const hasPermission = (requiredPermission?: string) => {
        if (!requiredPermission) return true; // Nếu không yêu cầu quyền -> Ai cũng xem được
        return userPermissions.includes(requiredPermission);
    };

    // 🔥 LỌC MENU DỰA TRÊN QUYỀN
    const filteredMenu = MENU_CONFIG.map(menu => {
        if (!hasPermission(menu.permission)) return null;

        let allowedChildren = undefined;
        if (menu.children) {
            allowedChildren = menu.children.filter(child => hasPermission(child.permission));
            if (allowedChildren.length === 0 && !menu.path) return null;
        }

        return { ...menu, children: allowedChildren };
    }).filter(Boolean) as MenuItem[]; 

    useEffect(() => {
        const activeParent = filteredMenu.find(m => 
            m.children?.some(c => location.pathname.includes(c.path))
        );
        if (activeParent && !openMenus.includes(activeParent.id)) {
            setOpenMenus(prev => [...prev, activeParent.id]);
        }
    }, [location.pathname]);

    const toggleSubmenu = (id: string) => {
        if (isCollapsed) setIsCollapsed(false);
        setOpenMenus(prev => prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]);
    };

    return (
        <aside className={`relative bg-white border-r border-slate-100 flex flex-col z-50 transition-all duration-500 ease-in-out ${isCollapsed ? 'w-19.5' : 'w-64'}`}>
            
            {/* --- LOGO AREA --- */}
            <div className={`h-16 flex items-center bg-amber-400 shrink-0 shadow-[0_4px_20px_-4px_rgba(251,191,36,0.4)] transition-all duration-500 ${isCollapsed ? 'justify-center' : 'px-6'}`}>
                <div className="flex items-center justify-center">
                    <div className="bg-slate-900 p-2 rounded-xl shrink-0 shadow-lg flex items-center justify-center transition-transform duration-500 hover:rotate-12">
                        <Sun size={20} className="text-amber-400 fill-amber-400" />
                    </div>
                    {!isCollapsed && (
                        <span className="font-black text-xl tracking-[0.2em] text-slate-900 italic ml-3 animate-in fade-in slide-in-from-left-4 duration-700 whitespace-nowrap">
                            SOLARIS
                        </span>
                    )}
                </div>
            </div>

            {/* --- TOGGLE BUTTON --- */}
            <button 
                onClick={() => setIsCollapsed(!isCollapsed)}
                className="absolute -right-3.5 top-16 -translate-y-1/2 w-7 h-7 bg-white border border-slate-200 rounded-full flex items-center justify-center cursor-pointer z-60 hover:border-amber-400 hover:text-amber-600 transition-all duration-300 shadow-[0_2px_8px_rgba(0,0,0,0.08)] group"
            >
                {isCollapsed ? <ChevronRight size={16} className="group-hover:translate-x-0.5 transition-transform" /> : <ChevronLeft size={16} className="group-hover:-translate-x-0.5 transition-transform" />}
            </button>

            {/* --- NAV AREA --- */}
            <nav className="flex-1 py-8 px-3.5 flex flex-col gap-2 overflow-y-auto overflow-x-hidden custom-scrollbar">
                
                {filteredMenu.map((item) => {
                    const Icon = item.icon;
                    const isOpen = openMenus.includes(item.id);
                    const isActive = item.path 
                        ? location.pathname === item.path 
                        : location.pathname.startsWith(item.basePath || '##');

                    return (
                        <div key={item.id} className="flex flex-col">
                            {/* Menu Button */}
                            <button 
                                onClick={() => item.children ? toggleSubmenu(item.id) : navigate(item.path!)}
                                className={`w-full flex items-center rounded-2xl transition-all duration-300 group p-3.5 relative
                                ${isCollapsed ? 'justify-center' : 'justify-between'} 
                                ${isActive ? 'bg-amber-50 text-amber-700 shadow-[inset_0_0_0_1px_rgba(251,191,36,0.2)]' : 'text-slate-500 hover:bg-slate-50 hover:text-slate-900'}`}
                            >
                                <div className="flex items-center justify-center">
                                    <div className={`shrink-0 transition-all duration-300 ${isActive ? 'text-amber-600 scale-110' : 'group-hover:text-amber-500 group-hover:scale-110'}`}>
                                        <Icon size={22} strokeWidth={isActive ? 2.5 : 2} />
                                    </div>
                                    {!isCollapsed && (
                                        <span className="text-[13px] font-bold tracking-wide uppercase ml-3 whitespace-nowrap animate-in fade-in duration-500">
                                            {item.label}
                                        </span>
                                    )}
                                </div>
                                
                                {item.children && !isCollapsed && (
                                    <ChevronDown size={16} className={`transition-transform duration-500 ${isOpen ? 'rotate-180' : 'text-slate-300'}`} />
                                )}

                                {/* Indicator Line */}
                                {isActive && !isCollapsed && (
                                    <div className="absolute left-0 top-3 bottom-3 w-1.5 bg-amber-500 rounded-r-full shadow-[2px_0_8px_rgba(245,158,11,0.4)]" />
                                )}
                            </button>

                            {/* Submenu Area */}
                            {item.children && !isCollapsed && (
                                <div className={`grid transition-all duration-500 ease-in-out ${isOpen ? 'grid-rows-[1fr] opacity-100 mt-1' : 'grid-rows-[0fr] opacity-0'}`}>
                                    <div className="overflow-hidden">
                                        {item.children.map((child) => {
                                            const isSubActive = location.pathname.includes(child.path);
                                            return (
                                                <button 
                                                    key={child.path}
                                                    onClick={() => navigate(child.path)}
                                                    className={`w-full flex items-center py-2.5 pl-12 pr-4 rounded-xl text-[12px] font-bold transition-all duration-300 group
                                                    ${isSubActive ? 'text-amber-600 bg-amber-50/40' : 'text-slate-400 hover:text-slate-900 hover:bg-slate-50 hover:pl-13'}`}
                                                >
                                                    <div className={`mr-3 w-1.5 h-1.5 rounded-full border-2 transition-all duration-500 
                                                        ${isSubActive ? 'bg-amber-500 border-amber-200 scale-125 shadow-[0_0_8px_rgba(245,158,11,0.5)]' : 'bg-transparent border-slate-300 group-hover:border-amber-400'}`} 
                                                    />
                                                    <span className="uppercase tracking-widest whitespace-nowrap">{child.label}</span>
                                                </button>
                                            );
                                        })}
                                    </div>
                                </div>
                            )}
                        </div>
                    );
                })}
            </nav>

            {/* --- FOOTER (Cài đặt & Đăng xuất) --- */}
            <div className="p-4 border-t border-slate-50 flex flex-col gap-1">
                {/* Nút Cài đặt */}
                <button 
                    onClick={() => {}}
                    className={`w-full flex items-center rounded-2xl transition-all duration-300 group p-3.5
                    ${isCollapsed ? 'justify-center' : 'justify-start'} text-slate-500 hover:bg-slate-50 hover:text-slate-900`}
                >
                    <div className="shrink-0 group-hover:rotate-45 transition-transform duration-500">
                        <Settings size={22} />
                    </div>
                    {!isCollapsed && (
                        <span className="text-[13px] font-bold tracking-wide uppercase ml-3 whitespace-nowrap">Cài đặt hệ thống</span>
                    )}
                </button>

                {/* 🔥 Nút Đăng xuất MỚI */}
                <button 
                    onClick={handleLogout}
                    className={`w-full flex items-center rounded-2xl transition-all duration-300 group p-3.5
                    ${isCollapsed ? 'justify-center' : 'justify-start'} text-red-500 hover:bg-red-50 hover:text-red-600`}
                >
                    <div className="shrink-0 transition-transform duration-500 group-hover:scale-110">
                        <LogOut size={22} />
                    </div>
                    {!isCollapsed && (
                        <span className="text-[13px] font-bold tracking-wide uppercase ml-3 whitespace-nowrap">Đăng xuất</span>
                    )}
                </button>
            </div>
        </aside>
    );
};