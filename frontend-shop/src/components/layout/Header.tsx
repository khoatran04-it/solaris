"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
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
  RotateCcw,
  PhoneCall,
  Clock,
  ArrowRight,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import { useLocationStore } from "@/stores/locationStore";
import { ShopCategoryTree } from "@/types/product";
import shopProductApi from "@/api/shopProductApi";

export default function Header() {
  const router = useRouter();
  const { user, isAuthenticated, initAuth, logout } = useAuthStore();
  const { totalCount, fetchCart } = useCartStore();
  const { deliveryAddress, deliveryDistrict, openModal, initLocation } =
    useLocationStore();

  const [searchQuery, setSearchQuery] = useState("");
  const [categories, setCategories] = useState<ShopCategoryTree[]>([]);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);

  useEffect(() => {
    initAuth();
    fetchCart();
    initLocation();

    // Load categories for navigation
    shopProductApi
      .getCategories()
      .then((res) => setCategories(res))
      .catch(() => {});
  }, [initAuth, fetchCart, initLocation]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/san-pham?search=${encodeURIComponent(searchQuery.trim())}`);
      setIsMenuOpen(false);
    }
  };

  return (
    <header className="sticky top-0 z-40 bg-white/95 backdrop-blur-md border-b border-slate-200/80 shadow-[0_4px_20px_-4px_rgba(0,0,0,0.03)] transition-all">
      {/* Main Navigation Bar */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-20 gap-4 lg:gap-8">
          {/* Brand Logo */}
          <div className="flex items-center">
            <Link
              href="/"
              className="flex items-center gap-3 group select-none"
            >
              <div className="w-11 h-11 rounded-2xl bg-gradient-to-br from-emerald-600 to-teal-700 flex items-center justify-center text-white shadow-md shadow-emerald-600/20 group-hover:scale-105 group-hover:shadow-emerald-600/30 transition-all">
                <svg
                  viewBox="0 0 24 24"
                  className="w-6 h-6 fill-none stroke-current stroke-2"
                >
                  <circle
                    cx="12"
                    cy="12"
                    r="5"
                    className="fill-amber-400 stroke-amber-400"
                  />
                  <path d="M12 2v2" strokeLinecap="round" />
                  <path d="M12 20v2" strokeLinecap="round" />
                  <path d="m4.93 4.93 1.41 1.41" strokeLinecap="round" />
                  <path d="m17.66 17.66 1.41 1.41" strokeLinecap="round" />
                  <path d="M2 12h2" strokeLinecap="round" />
                  <path d="M20 12h2" strokeLinecap="round" />
                  <path d="m6.34 17.66-1.41 1.41" strokeLinecap="round" />
                  <path d="m19.07 4.93-1.41 1.41" strokeLinecap="round" />
                </svg>
              </div>
              <div className="flex flex-col">
                <span className="text-2xl font-extrabold tracking-tight text-slate-900 group-hover:text-emerald-700 transition-colors leading-none font-sans">
                  SOLARIS
                </span>
                <span className="text-[10px] font-extrabold tracking-widest text-emerald-600 uppercase mt-1">
                  Nông Sản Sạch Cao Cấp
                </span>
              </div>
            </Link>
          </div>

          {/* Location Selector Pill (NO ICON - Pure Text & Badges) */}
          <div
            onClick={openModal}
            className="hidden xl:flex items-center gap-2 px-3.5 py-2 bg-slate-50 hover:bg-emerald-50/80 border border-slate-200/90 hover:border-emerald-300 rounded-2xl cursor-pointer transition-all select-none group"
            title="Bấm để đổi địa chỉ nhận hàng"
          >
            <div className="flex flex-col text-left">
              <div className="flex items-center gap-1.5">
                <span className="text-[9px] font-black uppercase tracking-wider text-emerald-800 bg-emerald-100/90 px-1.5 py-0.2 rounded font-mono">
                  GIAO ĐẾN
                </span>
                <span className="text-xs font-bold text-slate-800 group-hover:text-emerald-800 transition-colors truncate max-w-[150px]">
                  {deliveryDistrict
                    ? `${deliveryDistrict}, TP.HCM`
                    : "TP. Hồ Chí Minh"}
                </span>
              </div>
              <span className="text-[10px] font-medium text-slate-400 mt-0.5 truncate max-w-[180px]">
                {deliveryAddress || "Giao tươi 2 giờ"} •{" "}
                <span className="text-emerald-700 font-bold group-hover:underline">
                  [Đổi]
                </span>
              </span>
            </div>
          </div>

          {/* Search Bar (Desktop) */}
          <div className="hidden md:flex flex-1 max-w-xl">
            <form onSubmit={handleSearch} className="relative w-full">
              <input
                type="text"
                placeholder="Tìm kiếm trái cây, rau củ sạch, đặc sản Đà Lạt..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-11 pr-24 py-3 bg-slate-50/80 border border-slate-200/90 rounded-2xl text-xs font-medium text-slate-800 placeholder-slate-400 focus:outline-none focus:ring-4 focus:ring-emerald-500/15 focus:border-emerald-600 focus:bg-white transition-all shadow-2xs"
              />
              <Search className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
              <button
                type="submit"
                className="absolute right-1.5 top-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-xl text-xs font-bold transition-all shadow-xs"
              >
                Tìm kiếm
              </button>
            </form>
          </div>

          {/* Actions: Hotline / Cart / Account */}
          <div className="flex items-center gap-2 sm:gap-3">
            {/* Cart Button */}
            <Link
              href="/gio-hang"
              className="relative p-2.5 sm:px-4 sm:py-2.5 text-slate-700 hover:text-emerald-700 hover:bg-emerald-50/80 rounded-2xl border border-transparent hover:border-emerald-200/60 transition-all flex items-center gap-2 group"
            >
              <div className="relative">
                <ShoppingBag className="w-5 h-5 text-slate-700 group-hover:text-emerald-700 transition-colors" />
                {totalCount > 0 && (
                  <span className="absolute -top-2 -right-2.5 bg-rose-500 text-white text-[10px] font-black rounded-full h-4.5 min-w-[18px] px-1 flex items-center justify-center border-2 border-white shadow-xs animate-in zoom-in-75">
                    {totalCount}
                  </span>
                )}
              </div>
              <div className="hidden lg:flex flex-col text-left leading-none">
                <span className="text-[10px] text-slate-400 font-bold uppercase">
                  Giỏ hàng
                </span>
                <span className="text-xs font-black text-slate-800 group-hover:text-emerald-700 transition-colors mt-0.5">
                  {totalCount} sản phẩm
                </span>
              </div>
            </Link>

            {/* User Account Menu */}
            {isAuthenticated && user ? (
              <div className="relative">
                <button
                  onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                  className="flex items-center gap-2.5 p-1.5 sm:pr-3 bg-slate-50 hover:bg-slate-100/80 border border-slate-200 rounded-2xl transition-all text-left group"
                >
                  <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-emerald-600 to-teal-700 text-white flex items-center justify-center font-black text-xs shadow-xs">
                    {user.name.charAt(0).toUpperCase()}
                  </div>
                  <div className="hidden lg:block">
                    <p className="text-xs font-bold text-slate-900 leading-none truncate max-w-[110px]">
                      {user.name}
                    </p>
                    <span className="text-[10px] text-emerald-700 font-bold bg-emerald-50 px-1.5 py-0.2 rounded-md inline-block mt-0.5">
                      {user.customerTierName || "Thành Viên"}
                    </span>
                  </div>
                  <ChevronDown
                    className={`w-3.5 h-3.5 text-slate-400 transition-transform duration-200 ${isUserMenuOpen ? "rotate-180" : ""}`}
                  />
                </button>

                {/* User Dropdown */}
                {isUserMenuOpen && (
                  <div
                    className="absolute right-0 mt-2 w-64 bg-white rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] border border-slate-100 py-2 z-50 animate-in fade-in slide-in-from-top-2 duration-150"
                    onMouseLeave={() => setIsUserMenuOpen(false)}
                  >
                    <div className="px-4 py-3 border-b border-slate-100 bg-slate-50/50">
                      <p className="text-[11px] text-slate-400 font-medium">
                        Tài khoản thành viên
                      </p>
                      <p className="text-sm font-bold text-slate-900 truncate mt-0.5">
                        {user.name}
                      </p>
                      {user.discountPercent > 0 && (
                        <div className="mt-2 flex items-center gap-1.5 px-2 py-1 bg-emerald-50 text-emerald-800 text-[11px] font-bold rounded-lg border border-emerald-200/50">
                          <Sparkles className="w-3 h-3 text-amber-500" />
                          <span>Chiết khấu hạng: -{user.discountPercent}%</span>
                        </div>
                      )}
                    </div>

                    <div className="py-1">
                      <Link
                        href="/tai-khoan"
                        onClick={() => setIsUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-semibold"
                      >
                        <User className="w-4 h-4 text-slate-400" />
                        <span>Hồ sơ cá nhân & Thẻ VIP</span>
                      </Link>

                      <Link
                        href="/tai-khoan/don-hang"
                        onClick={() => setIsUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-semibold"
                      >
                        <Package className="w-4 h-4 text-slate-400" />
                        <span>Lịch sử đơn hàng</span>
                      </Link>

                      <Link
                        href="/tai-khoan/dia-chi"
                        onClick={() => setIsUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-semibold"
                      >
                        <MapPin className="w-4 h-4 text-slate-400" />
                        <span>Sổ địa chỉ nhận hàng</span>
                      </Link>

                      <Link
                        href="/tai-khoan/tra-hang"
                        onClick={() => setIsUserMenuOpen(false)}
                        className="flex items-center gap-3 px-4 py-2.5 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 transition-colors font-semibold"
                      >
                        <RotateCcw className="w-4 h-4 text-slate-400" />
                        <span>Yêu cầu đổi trả nông sản (RMA)</span>
                      </Link>
                    </div>

                    <div className="border-t border-slate-100 pt-1 mt-1">
                      <button
                        onClick={() => {
                          logout();
                          setIsUserMenuOpen(false);
                          router.push("/");
                        }}
                        className="w-full flex items-center gap-3 px-4 py-2.5 text-xs text-rose-600 hover:bg-rose-50 transition-colors font-bold text-left"
                      >
                        <LogOut className="w-4 h-4" />
                        <span>Đăng xuất</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <Link
                href="/dang-nhap"
                className="flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-2xl text-xs font-bold transition-all shadow-sm shadow-emerald-600/20 active:scale-95"
              >
                <User className="w-4 h-4" />
                <span className="hidden sm:inline">Đăng nhập</span>
              </Link>
            )}

            {/* Mobile Menu Button */}
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="p-2.5 text-slate-700 md:hidden hover:bg-slate-100 rounded-xl"
              aria-label="Toggle navigation menu"
            >
              {isMenuOpen ? (
                <X className="w-6 h-6" />
              ) : (
                <Menu className="w-6 h-6" />
              )}
            </button>
          </div>
        </div>

        {/* 3. Sub-navigation: Mega Category Bar (Desktop) */}
        <nav className="hidden md:flex items-center gap-1 lg:gap-2 py-2 text-xs font-bold text-slate-700 border-t border-slate-100">
          <Link
            href="/san-pham"
            className="px-3.5 py-2 rounded-xl text-slate-800 hover:text-emerald-700 hover:bg-emerald-50/70 transition-all flex items-center gap-1.5"
          >
            <span>Tất Cả Sản Phẩm</span>
          </Link>

          {categories.map((group) => (
            <div key={group.groupId} className="relative group">
              <Link
                href={`/danh-muc/${group.groupSlug}`}
                className="px-3.5 py-2 rounded-xl text-slate-700 hover:text-emerald-700 hover:bg-emerald-50/70 transition-all flex items-center gap-1"
              >
                <span>{group.groupName}</span>
                {group.categories.length > 0 && (
                  <ChevronDown className="w-3 h-3 text-slate-400 group-hover:rotate-180 group-hover:text-emerald-600 transition-transform" />
                )}
              </Link>

              {group.categories.length > 0 && (
                <div className="absolute left-0 top-full hidden group-hover:block w-56 bg-white shadow-[0_10px_30px_-5px_rgba(0,0,0,0.12)] rounded-2xl border border-slate-100 py-2.5 z-50 animate-in fade-in duration-150">
                  <div className="px-4 py-1.5 text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                    {group.groupName}
                  </div>
                  {group.categories.map((cat) => (
                    <Link
                      key={cat.categoryId}
                      href={`/danh-muc/${cat.categorySlug}`}
                      className="flex items-center justify-between px-4 py-2 text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-semibold transition-colors"
                    >
                      <span>{cat.categoryName}</span>
                      <span className="text-[10px] text-slate-400 font-normal">
                        {cat.productCount}
                      </span>
                    </Link>
                  ))}
                </div>
              )}
            </div>
          ))}

          <Link
            href="/khuyen-mai"
            className="ml-auto px-4 py-2 rounded-xl bg-gradient-to-r from-rose-50 to-orange-50 hover:from-rose-100 hover:to-orange-100 text-rose-700 font-bold border border-rose-200/60 transition-all flex items-center gap-1.5 shadow-2xs"
          >
            <Sparkles className="w-3.5 h-3.5 text-rose-600 animate-bounce" />
            <span>Ưu Đãi Hôm Nay</span>
          </Link>
        </nav>
      </div>

      {/* 4. Mobile Menu Drawer */}
      {isMenuOpen && (
        <div className="md:hidden border-t border-slate-200 bg-white p-5 space-y-5 animate-in slide-in-from-top-2 duration-200">
          <form onSubmit={handleSearch} className="relative w-full">
            <input
              type="text"
              placeholder="Tìm trái cây, rau củ sạch..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
          </form>

          <div className="space-y-1">
            <div className="text-[10px] font-bold text-slate-400 uppercase px-2 mb-1">
              Danh Mục Nông Sản
            </div>
            <Link
              href="/san-pham"
              onClick={() => setIsMenuOpen(false)}
              className="flex items-center justify-between py-2.5 px-3 rounded-xl text-xs font-bold text-slate-800 hover:bg-slate-50"
            >
              <span>Tất cả sản phẩm</span>
              <ArrowRight className="w-3.5 h-3.5 text-slate-400" />
            </Link>
            {categories.map((group) => (
              <Link
                key={group.groupId}
                href={`/danh-muc/${group.groupSlug}`}
                onClick={() => setIsMenuOpen(false)}
                className="flex items-center justify-between py-2.5 px-3 rounded-xl text-xs font-semibold text-slate-700 hover:bg-emerald-50 hover:text-emerald-800"
              >
                <span>{group.groupName}</span>
                <span className="text-[10px] text-slate-400 font-medium">
                  {group.categories.length} loại
                </span>
              </Link>
            ))}
            <Link
              href="/khuyen-mai"
              onClick={() => setIsMenuOpen(false)}
              className="flex items-center justify-between py-2.5 px-3 rounded-xl text-xs font-bold text-rose-600 hover:bg-rose-50"
            >
              <span>Ưu đãi khuyến mãi</span>
              <Sparkles className="w-3.5 h-3.5 text-rose-500" />
            </Link>
          </div>

          {isAuthenticated && user && (
            <div className="border-t border-slate-100 pt-3 space-y-1">
              <div className="text-[10px] font-bold text-slate-400 uppercase px-2 mb-1">
                Tài Khoản Của Tôi
              </div>
              <Link
                href="/tai-khoan"
                onClick={() => setIsMenuOpen(false)}
                className="flex items-center gap-2.5 py-2 px-3 rounded-xl text-xs font-semibold text-slate-700"
              >
                <User className="w-4 h-4 text-emerald-600" />
                <span>Hồ sơ & Thẻ thành viên</span>
              </Link>
              <Link
                href="/tai-khoan/don-hang"
                onClick={() => setIsMenuOpen(false)}
                className="flex items-center gap-2.5 py-2 px-3 rounded-xl text-xs font-semibold text-slate-700"
              >
                <Package className="w-4 h-4 text-emerald-600" />
                <span>Lịch sử đơn hàng</span>
              </Link>
              <Link
                href="/tai-khoan/dia-chi"
                onClick={() => setIsMenuOpen(false)}
                className="flex items-center gap-2.5 py-2 px-3 rounded-xl text-xs font-semibold text-slate-700"
              >
                <MapPin className="w-4 h-4 text-emerald-600" />
                <span>Sổ địa chỉ</span>
              </Link>
              <button
                onClick={() => {
                  logout();
                  setIsMenuOpen(false);
                  router.push("/");
                }}
                className="w-full flex items-center gap-2.5 py-2 px-3 rounded-xl text-xs font-bold text-rose-600"
              >
                <LogOut className="w-4 h-4" />
                <span>Đăng xuất</span>
              </button>
            </div>
          )}
        </div>
      )}
    </header>
  );
}
