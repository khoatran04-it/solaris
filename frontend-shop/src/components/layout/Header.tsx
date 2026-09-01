'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { 
    ShoppingBag, 
    User, 
    Search, 
    Menu, 
    X, 
    Sparkles, 
    ChevronDown, 
    LogOut, 
    Package, 
    MapPin, 
    RotateCcw
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { ShopCategoryTree } from '@/types/product';
import shopProductApi from '@/api/shopProductApi';

export default function Header() {
    const router = useRouter();
    const { user, isAuthenticated, initAuth, logout } = useAuthStore();
    const { totalCount, fetchCart } = useCartStore();

    const [searchQuery, setSearchQuery] = useState('');
    const [categories, setCategories] = useState<ShopCategoryTree[]>([]);
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);

    useEffect(() => {
        initAuth();
        fetchCart();

        // Load categories for navigation
        shopProductApi.getCategories()
            .then(res => setCategories(res))
            .catch(() => {});
    }, [initAuth, fetchCart]);

    const handleSearch = (e: React.FormEvent) => {
        e.preventDefault();
        if (searchQuery.trim()) {
            router.push(`/san-pham?search=${encodeURIComponent(searchQuery.trim())}`);
        }
    };

    return (
        <header className="sticky top-0 z-40 bg-white border-b border-slate-100 shadow-[0_1px_2px_rgba(0,0,0,0.03)]">
            {/* Top Notification Bar */}
            <div className="bg-emerald-600 text-white text-xs py-1.5 px-4 text-center font-medium flex items-center justify-center gap-2">
                <Sparkles className="w-3.5 h-3.5 animate-pulse" />
                <span>Nông sản tươi chuẩn VietGAP / GlobalGAP — Cam kết đổi trả trong 24h</span>
                <span className="hidden md:inline-block">| Hotline: 1900 8888</span>
            </div>

            {/* Main Navigation */}
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex items-center justify-between h-18 gap-4">
                    
                    {/* Logo & Brand */}
                    <div className="flex items-center gap-3">
                        <Link href="/" className="flex items-center gap-2.5 group">
                            <div className="w-10 h-10 rounded-xl bg-emerald-600 flex items-center justify-center text-white font-bold text-xl shadow-md shadow-emerald-500/20 group-hover:scale-105 transition-transform">
                                ☀️
                            </div>
                            <div>
                                <span className="text-2xl font-black tracking-tight text-slate-900 group-hover:text-emerald-600 transition-colors">
                                    SOLARIS
                                </span>
                                <span className="block text-[10px] uppercase font-bold tracking-widest text-emerald-600 -mt-1">
                                    Nông Sản Sạch
                                </span>
                            </div>
                        </Link>
                    </div>

                    {/* Search Bar (Desktop) */}
                    <div className="hidden md:flex flex-1 max-w-lg mx-6">
                        <form onSubmit={handleSearch} className="relative w-full">
                            <input
                                type="text"
                                placeholder="Tìm trái cây, rau củ, xuất xứ Đà Lạt..."
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                className="w-full pl-10 pr-20 py-2.5 bg-slate-50/50 border border-slate-200 rounded-xl text-sm focus:outline-none focus:ring-4 focus:ring-emerald-500/20 focus:border-emerald-500 focus:bg-white transition-all text-slate-800 placeholder-slate-400 font-medium"
                            />
                            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                            <button
                                type="submit"
                                className="absolute right-1.5 top-1.5 px-4 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold transition-colors"
                            >
                                Tìm
                            </button>
                        </form>
                    </div>

                    {/* Action Buttons: Cart & User */}
                    <div className="flex items-center gap-3">
                        
                        {/* Cart Button */}
                        <Link
                            href="/gio-hang"
                            className="relative p-2.5 text-slate-700 hover:text-emerald-600 hover:bg-emerald-50 rounded-full transition-all flex items-center gap-1.5"
                        >
                            <ShoppingBag className="w-6 h-6" />
                            {totalCount > 0 && (
                                <span className="absolute -top-1 -right-1 bg-rose-500 text-white text-[11px] font-bold rounded-full h-5 min-w-[20px] px-1 flex items-center justify-center border-2 border-white shadow-sm">
                                    {totalCount}
                                </span>
                            )}
                            <span className="hidden lg:inline text-xs font-semibold text-slate-600">Giỏ hàng</span>
                        </Link>

                        {/* User Account / Login */}
                        {isAuthenticated && user ? (
                            <div className="relative">
                                <button
                                    onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                                    className="flex items-center gap-2 p-1.5 pl-3 bg-slate-50 border border-slate-200 rounded-full hover:bg-slate-100 transition-all text-left"
                                >
                                    <div className="w-7 h-7 rounded-full bg-emerald-600 text-white flex items-center justify-center font-bold text-xs">
                                        {user.name.charAt(0).toUpperCase()}
                                    </div>
                                    <div className="hidden lg:block pr-1">
                                        <p className="text-xs font-bold text-slate-800 leading-none truncate max-w-[100px]">
                                            {user.name}
                                        </p>
                                        <span className="text-[10px] text-emerald-600 font-semibold">
                                            {user.customerTierName || 'Thành Viên'}
                                        </span>
                                    </div>
                                    <ChevronDown className="w-3.5 h-3.5 text-slate-400" />
                                </button>

                                {/* Dropdown Menu */}
                                {isUserMenuOpen && (
                                    <div 
                                        className="absolute right-0 mt-2 w-56 bg-white rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] border border-slate-100 py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-150"
                                        onMouseLeave={() => setIsUserMenuOpen(false)}
                                    >
                                        <div className="px-4 py-2 border-b border-slate-100">
                                            <p className="text-xs text-slate-500">Xin chào,</p>
                                            <p className="text-sm font-bold text-slate-900 truncate">{user.name}</p>
                                            {user.discountPercent > 0 && (
                                                <span className="inline-block mt-1 px-2 py-0.5 bg-emerald-50 text-emerald-700 text-[10px] font-bold rounded-full">
                                                    Chiết khấu hạng: {user.discountPercent}%
                                                </span>
                                            )}
                                        </div>

                                        <Link 
                                            href="/tai-khoan" 
                                            onClick={() => setIsUserMenuOpen(false)}
                                            className="flex items-center gap-2.5 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors font-medium"
                                        >
                                            <User className="w-4 h-4 text-slate-400" />
                                            Hồ sơ cá nhân
                                        </Link>

                                        <Link 
                                            href="/tai-khoan/don-hang" 
                                            onClick={() => setIsUserMenuOpen(false)}
                                            className="flex items-center gap-2.5 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors font-medium"
                                        >
                                            <Package className="w-4 h-4 text-slate-400" />
                                            Lịch sử đơn hàng
                                        </Link>

                                        <Link 
                                            href="/tai-khoan/dia-chi" 
                                            onClick={() => setIsUserMenuOpen(false)}
                                            className="flex items-center gap-2.5 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors font-medium"
                                        >
                                            <MapPin className="w-4 h-4 text-slate-400" />
                                            Sổ địa chỉ
                                        </Link>

                                        <Link 
                                            href="/tai-khoan/tra-hang" 
                                            onClick={() => setIsUserMenuOpen(false)}
                                            className="flex items-center gap-2.5 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors font-medium"
                                        >
                                            <RotateCcw className="w-4 h-4 text-slate-400" />
                                            Đổi trả hàng (RMA)
                                        </Link>

                                        <div className="border-t border-slate-100 mt-1 pt-1">
                                            <button
                                                onClick={() => {
                                                    logout();
                                                    setIsUserMenuOpen(false);
                                                    router.push('/');
                                                }}
                                                className="w-full flex items-center gap-2.5 px-4 py-2 text-xs text-rose-600 hover:bg-rose-50 transition-colors font-semibold text-left"
                                            >
                                                <LogOut className="w-4 h-4" />
                                                Đăng xuất
                                            </button>
                                        </div>
                                    </div>
                                )}
                            </div>
                        ) : (
                            <Link
                                href="/dang-nhap"
                                className="flex items-center gap-1.5 px-4 py-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-full text-xs font-bold transition-colors"
                            >
                                <User className="w-4 h-4" />
                                <span>Đăng nhập</span>
                            </Link>
                        )}

                        {/* Mobile Menu Toggle */}
                        <button
                            onClick={() => setIsMenuOpen(!isMenuOpen)}
                            className="p-2 text-slate-700 md:hidden hover:bg-slate-100 rounded-lg"
                        >
                            {isMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
                        </button>
                    </div>
                </div>

                {/* Sub Header / Category Navbar (Desktop) */}
                <nav className="hidden md:flex items-center gap-6 py-2.5 text-xs font-semibold text-slate-700 border-t border-slate-100">
                    <Link href="/san-pham" className="hover:text-emerald-600 transition-colors flex items-center gap-1">
                        <span>Tất cả sản phẩm</span>
                    </Link>

                    {categories.map((group) => (
                        <div key={group.groupId} className="relative group">
                            <Link 
                                href={`/danh-muc/${group.groupSlug}`} 
                                className="hover:text-emerald-600 transition-colors py-1 flex items-center gap-1"
                            >
                                <span>{group.groupName}</span>
                                {group.categories.length > 0 && <ChevronDown className="w-3 h-3 text-slate-400 group-hover:rotate-180 transition-transform" />}
                            </Link>

                            {group.categories.length > 0 && (
                                <div className="absolute left-0 top-full hidden group-hover:block w-48 bg-white shadow-xl rounded-xl border border-slate-100 py-2 z-50 animate-in fade-in">
                                    {group.categories.map((cat) => (
                                        <Link
                                            key={cat.categoryId}
                                            href={`/danh-muc/${cat.categorySlug}`}
                                            className="block px-4 py-2 text-xs text-slate-600 hover:bg-emerald-50 hover:text-emerald-700 font-medium"
                                        >
                                            {cat.categoryName}
                                        </Link>
                                    ))}
                                </div>
                            )}
                        </div>
                    ))}

                    <Link href="/khuyen-mai" className="text-rose-600 hover:text-rose-700 font-bold ml-auto flex items-center gap-1">
                        <Sparkles className="w-3.5 h-3.5" />
                        <span>Khuyến mãi sốc</span>
                    </Link>
                </nav>
            </div>

            {/* Mobile Dropdown Search & Menu */}
            {isMenuOpen && (
                <div className="md:hidden border-t border-slate-100 bg-white p-4 space-y-4">
                    <form onSubmit={handleSearch} className="relative w-full">
                        <input
                            type="text"
                            placeholder="Tìm kiếm nông sản..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-lg text-sm"
                        />
                        <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
                    </form>

                    <div className="space-y-2">
                        <Link 
                            href="/san-pham" 
                            onClick={() => setIsMenuOpen(false)}
                            className="block py-2 text-sm font-semibold text-slate-800"
                        >
                            🌾 Tất cả sản phẩm
                        </Link>
                        {categories.map((group) => (
                            <Link
                                key={group.groupId}
                                href={`/danh-muc/${group.groupSlug}`}
                                onClick={() => setIsMenuOpen(false)}
                                className="block py-2 text-sm font-medium text-slate-600 pl-2 border-l-2 border-slate-200 hover:border-emerald-500"
                            >
                                {group.groupName}
                            </Link>
                        ))}
                        <Link 
                            href="/khuyen-mai" 
                            onClick={() => setIsMenuOpen(false)}
                            className="block py-2 text-sm font-bold text-rose-600"
                        >
                            🔥 Khuyến mãi sốc
                        </Link>
                    </div>
                </div>
            )}
        </header>
    );
}
