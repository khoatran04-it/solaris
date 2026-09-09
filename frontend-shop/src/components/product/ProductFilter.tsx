"use client";

import React, { Suspense, useState, useRef, useEffect } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import {
  Filter,
  RotateCcw,
  Check,
  MapPin,
  Award,
  ChevronDown,
  ChevronUp,
  Layers,
  Sparkles,
  Flame,
  TrendingUp,
  TrendingDown,
  ArrowDownAZ,
} from "lucide-react";
import { ShopCategoryTree } from "@/types/product";

interface ProductFilterProps {
  categories: ShopCategoryTree[];
  origins: string[];
  certifications: string[];
}

const SORT_OPTIONS = [
  { value: "newest", label: "Mới nhất hôm nay", icon: Sparkles },
  { value: "discount", label: "Khuyến mãi nhiều nhất", icon: Flame },
  { value: "price-asc", label: "Giá: Thấp đến Cao", icon: TrendingUp },
  { value: "price-desc", label: "Giá: Cao đến Thấp", icon: TrendingDown },
  { value: "name-asc", label: "Tên: A — Z", icon: ArrowDownAZ },
];

function ProductFilterContent({
  categories,
  origins,
  certifications,
}: ProductFilterProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const sortDropdownRef = useRef<HTMLDivElement>(null);

  // Mở rộng tất cả các nhóm danh mục theo mặc định
  const [expandedGroups, setExpandedGroups] = useState<Record<number, boolean>>(
    () => {
      const initial: Record<number, boolean> = {};
      categories.forEach((g) => {
        initial[g.groupId] = true;
      });
      return initial;
    },
  );

  const [isSortOpen, setIsSortOpen] = useState(false);

  // Close sort dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        sortDropdownRef.current &&
        !sortDropdownRef.current.contains(event.target as Node)
      ) {
        setIsSortOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Trạng thái mở rộng cho từng danh mục con (Cấp 2 chứa Cấp 3)
  const [expandedCategories, setExpandedCategories] = useState<
    Record<number, boolean>
  >({});

  const toggleGroup = (groupId: number) => {
    setExpandedGroups((prev) => ({
      ...prev,
      [groupId]: !prev[groupId],
    }));
  };

  const toggleCategory = (catId: number, e: React.MouseEvent) => {
    e.stopPropagation();
    setExpandedCategories((prev) => ({
      ...prev,
      [catId]: !prev[catId],
    }));
  };

  const currentCatSlug = searchParams.get("category") || "";
  const currentProductSlug = searchParams.get("product") || "";
  const currentOrigin = searchParams.get("origin") || "";
  const currentCert = searchParams.get("cert") || "";
  const currentSort = searchParams.get("sort") || "newest";

  // Xác định xem route hiện tại có đang nằm trong /danh-muc/[slug] không
  const isCategoryPage = pathname.startsWith("/danh-muc/");
  const currentRouteSlug = isCategoryPage
    ? pathname.replace("/danh-muc/", "")
    : "";

  // Tự động mở rộng nhóm và danh mục nếu có category hoặc product đang chọn
  useEffect(() => {
    if (currentCatSlug || currentProductSlug || currentRouteSlug) {
      categories.forEach((g) => {
        g.categories.forEach((c) => {
          const isCatMatched =
            c.categorySlug === currentCatSlug ||
            c.categorySlug === currentRouteSlug;
          const isProdMatched = c.products?.some(
            (p) =>
              p.productSlug === currentProductSlug ||
              p.productSlug === currentRouteSlug,
          );
          if (isCatMatched || isProdMatched) {
            setExpandedCategories((prev) => ({
              ...prev,
              [c.categoryId]: true,
            }));
            setExpandedGroups((prev) => ({ ...prev, [g.groupId]: true }));
          }
        });
      });
    }
  }, [currentCatSlug, currentProductSlug, currentRouteSlug, categories]);

  const isCategoryActive = (categorySlug: string) => {
    return (
      (currentCatSlug === categorySlug || currentRouteSlug === categorySlug) &&
      !currentProductSlug
    );
  };

  const isProductActive = (productSlug: string) => {
    return currentProductSlug === productSlug;
  };

  const isGroupActive = (groupSlug: string) => {
    return (
      currentRouteSlug === groupSlug && !currentCatSlug && !currentProductSlug
    );
  };

  const isAllActive =
    !currentCatSlug && !currentProductSlug && !currentRouteSlug;

  const handleSelectCategory = (catSlug?: string) => {
    if (!catSlug) {
      router.push("/san-pham");
      return;
    }

    if (isCategoryPage) {
      router.push(`/danh-muc/${catSlug}`);
    } else {
      const params = new URLSearchParams(searchParams.toString());
      params.delete("product"); // Xóa lọc cấp 3 khi chọn cấp 2
      params.set("category", catSlug);
      params.set("page", "1");
      router.push(`/san-pham?${params.toString()}`);
    }
  };

  const handleSelectProduct = (productSlug: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("product", productSlug);
    params.set("page", "1");
    if (isCategoryPage) {
      router.push(`/san-pham?${params.toString()}`);
    } else {
      router.push(`${pathname}?${params.toString()}`);
    }
  };

  const handleSelectGroup = (groupSlug: string) => {
    router.push(`/danh-muc/${groupSlug}`);
  };

  const updateFilter = (key: string, value: string) => {
    const params = new URLSearchParams(searchParams.toString());
    if (value) {
      params.set(key, value);
    } else {
      params.delete(key);
    }
    params.set("page", "1");
    router.push(`${pathname}?${params.toString()}`);
  };

  const handleSelectSort = (sortValue: string) => {
    updateFilter("sort", sortValue);
    setIsSortOpen(false);
  };

  const resetFilters = () => {
    router.push("/san-pham");
  };

  const hasActiveFilters = Boolean(
    currentCatSlug ||
    currentProductSlug ||
    currentOrigin ||
    currentCert ||
    searchParams.get("search") ||
    (isCategoryPage && currentRouteSlug),
  );

  const activeSortOption =
    SORT_OPTIONS.find((s) => s.value === currentSort) || SORT_OPTIONS[0];
  const ActiveSortIcon = activeSortOption.icon;

  return (
    <aside className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] p-6 space-y-7">
      {/* Header Bộ Lọc */}
      <div className="flex items-center justify-between pb-4 border-b border-slate-100">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-2xl bg-emerald-50 text-emerald-700 shadow-2xs">
            <Filter className="w-4 h-4" />
          </div>
          <div>
            <h3 className="font-extrabold text-sm text-slate-900 tracking-tight">
              Bộ Lọc Nông Sản
            </h3>
            <p className="text-[11px] text-slate-400 font-medium">
              Tùy chọn theo tiêu chuẩn & xuất xứ
            </p>
          </div>
        </div>

        {hasActiveFilters && (
          <button
            onClick={resetFilters}
            className="text-slate-500 hover:text-rose-600 transition-colors flex items-center gap-1 text-xs font-bold px-2.5 py-1 rounded-xl hover:bg-rose-50 cursor-pointer"
            title="Xóa tất cả bộ lọc"
          >
            <RotateCcw className="w-3.5 h-3.5" />
            <span>Xóa lọc</span>
          </button>
        )}
      </div>

      {/* 1. Custom Sắp xếp thứ tự Dropdown (Đồng bộ Theme) */}
      <div className="space-y-2 relative" ref={sortDropdownRef}>
        <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider block">
          Sắp xếp sản phẩm
        </label>

        {/* Dropdown Button */}
        <button
          type="button"
          onClick={() => setIsSortOpen(!isSortOpen)}
          className="w-full h-11 px-3.5 bg-slate-50 hover:bg-emerald-50/40 border border-slate-200/80 hover:border-emerald-300 rounded-2xl flex items-center justify-between text-xs font-bold text-slate-800 transition-all cursor-pointer shadow-2xs focus:outline-hidden"
        >
          <div className="flex items-center gap-2 truncate">
            <ActiveSortIcon className="w-4 h-4 text-emerald-600 shrink-0" />
            <span className="truncate">{activeSortOption.label}</span>
          </div>
          <ChevronDown
            className={`w-4 h-4 text-slate-400 shrink-0 transition-transform duration-200 ${isSortOpen ? "rotate-180 text-emerald-600" : ""}`}
          />
        </button>

        {/* Dropdown Menu Popup */}
        {isSortOpen && (
          <div className="absolute top-full left-0 right-0 mt-1.5 z-40 bg-white border border-slate-200/90 rounded-2xl shadow-xl p-1.5 space-y-1 animate-in fade-in zoom-in-95">
            {SORT_OPTIONS.map((opt) => {
              const Icon = opt.icon;
              const isSelected = opt.value === currentSort;

              return (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => handleSelectSort(opt.value)}
                  className={`w-full px-3 py-2.5 rounded-xl text-xs flex items-center justify-between transition-all cursor-pointer ${
                    isSelected
                      ? "bg-emerald-50 text-emerald-800 font-extrabold shadow-2xs"
                      : "text-slate-700 hover:bg-slate-50 hover:text-emerald-700 font-semibold"
                  }`}
                >
                  <div className="flex items-center gap-2.5">
                    <Icon
                      className={`w-4 h-4 ${isSelected ? "text-emerald-600" : "text-slate-400"}`}
                    />
                    <span>{opt.label}</span>
                  </div>
                  {isSelected && <Check className="w-4 h-4 text-emerald-600" />}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* 2. Danh mục sản phẩm (Phân cấp theo Nhóm) */}
      <div className="space-y-3 pt-4 border-t border-slate-100">
        <div className="flex items-center justify-between">
          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
            <Layers className="w-3.5 h-3.5 text-emerald-600" />
            Danh mục sản phẩm
          </label>
        </div>

        <div className="space-y-2">
          {/* Nút Xem tất cả */}
          <button
            onClick={() => handleSelectCategory()}
            className={`w-full text-left text-xs px-3.5 py-2.5 rounded-2xl border flex items-center justify-between transition-all cursor-pointer ${
              isAllActive
                ? "bg-emerald-600 border-emerald-600 text-white font-extrabold shadow-xs"
                : "border-slate-200/80 hover:border-emerald-300 text-slate-700 hover:bg-emerald-50/50 font-bold"
            }`}
          >
            <span>Tất cả sản phẩm</span>
            {isAllActive && <Check className="w-3.5 h-3.5 text-white" />}
          </button>

          {/* Danh sách từng Group */}
          {categories.map((group) => {
            const isGrpActive = isGroupActive(group.groupSlug);
            const isExpanded = expandedGroups[group.groupId];

            return (
              <div
                key={group.groupId}
                className="rounded-2xl border border-slate-200/70 bg-slate-50/40 overflow-hidden shadow-2xs"
              >
                {/* Group Header */}
                <div className="flex items-center justify-between p-2.5 hover:bg-slate-100/60 transition-colors">
                  <button
                    onClick={() => handleSelectGroup(group.groupSlug)}
                    className={`text-xs font-bold text-left flex-1 hover:text-emerald-700 transition-colors cursor-pointer ${
                      isGrpActive
                        ? "text-emerald-700 font-extrabold"
                        : "text-slate-800"
                    }`}
                  >
                    <span>{group.groupName}</span>
                  </button>

                  <button
                    onClick={() => toggleGroup(group.groupId)}
                    className="p-1 text-slate-400 hover:text-slate-600 rounded-lg cursor-pointer"
                    title="Mở rộng / Thu gọn"
                  >
                    {isExpanded ? (
                      <ChevronUp className="w-3.5 h-3.5" />
                    ) : (
                      <ChevronDown className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>

                {/* Sub-categories (Cấp 2) & Dòng Sản Phẩm (Cấp 3) */}
                {isExpanded && group.categories.length > 0 && (
                  <div className="px-2 pb-2 space-y-1.5">
                    {group.categories.map((cat) => {
                      const active = isCategoryActive(cat.categorySlug);
                      const isCatExpanded = expandedCategories[cat.categoryId];
                      const hasProducts =
                        cat.products && cat.products.length > 0;

                      return (
                        <div key={cat.categoryId} className="space-y-1">
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() =>
                                handleSelectCategory(cat.categorySlug)
                              }
                              className={`flex-1 text-left text-xs px-3 py-2 rounded-xl flex items-center justify-between transition-all cursor-pointer ${
                                active
                                  ? "bg-emerald-600 text-white font-extrabold shadow-2xs"
                                  : "text-slate-700 hover:bg-white hover:text-emerald-700 font-medium"
                              }`}
                            >
                              <span className="truncate pr-2">
                                {cat.categoryName}
                              </span>
                              <span
                                className={`text-[10px] font-bold px-1.5 py-0.5 rounded-md ${active ? "bg-emerald-700/50 text-white" : "bg-slate-200/60 text-slate-500"}`}
                              >
                                {cat.productCount}
                              </span>
                            </button>

                            {hasProducts && (
                              <button
                                onClick={(e) =>
                                  toggleCategory(cat.categoryId, e)
                                }
                                className="p-1.5 text-slate-400 hover:text-emerald-700 hover:bg-white rounded-lg cursor-pointer transition-colors"
                                title={
                                  isCatExpanded
                                    ? "Thu gọn dòng sản phẩm"
                                    : "Mở rộng dòng sản phẩm"
                                }
                              >
                                {isCatExpanded ? (
                                  <ChevronUp className="w-3.5 h-3.5" />
                                ) : (
                                  <ChevronDown className="w-3.5 h-3.5" />
                                )}
                              </button>
                            )}
                          </div>

                          {/* Cấp 3: Dòng Sản Phẩm (Product Master) */}
                          {isCatExpanded && hasProducts && (
                            <div className="pl-3.5 pr-1 py-1 space-y-1 border-l-2 border-emerald-200/60 ml-3">
                              {cat.products!.map((prod) => {
                                const prodActive = isProductActive(
                                  prod.productSlug,
                                );

                                return (
                                  <button
                                    key={prod.productId}
                                    onClick={() =>
                                      handleSelectProduct(prod.productSlug)
                                    }
                                    className={`w-full text-left text-[11px] px-2.5 py-1.5 rounded-lg flex items-center justify-between transition-all cursor-pointer ${
                                      prodActive
                                        ? "bg-emerald-100 text-emerald-900 font-bold border border-emerald-300 shadow-2xs"
                                        : "text-slate-600 hover:text-emerald-700 hover:bg-white font-medium"
                                    }`}
                                  >
                                    <span className="truncate pr-1">
                                      • {prod.productName}
                                    </span>
                                    {prod.variantCount > 0 && (
                                      <span className="text-[9px] text-slate-400 shrink-0 font-normal">
                                        {prod.variantCount} loại
                                      </span>
                                    )}
                                  </button>
                                );
                              })}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* 3. Xuất xứ / Vùng trồng */}
      {origins.length > 0 && (
        <div className="space-y-3 pt-4 border-t border-slate-100">
          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
            <MapPin className="w-3.5 h-3.5 text-emerald-600" />
            Xuất xứ / Vùng trồng
          </label>
          <div className="flex flex-wrap gap-1.5">
            {origins.map((origin) => {
              const isSelected = currentOrigin === origin;
              return (
                <button
                  key={origin}
                  onClick={() =>
                    updateFilter("origin", isSelected ? "" : origin)
                  }
                  className={`text-xs px-3 py-1.5 rounded-xl border transition-all flex items-center gap-1.5 cursor-pointer ${
                    isSelected
                      ? "bg-emerald-50 border-emerald-600 text-emerald-800 font-bold shadow-2xs"
                      : "border-slate-200/80 hover:border-emerald-300 text-slate-600 hover:bg-slate-50 font-medium"
                  }`}
                >
                  <span>{origin}</span>
                  {isSelected && <Check className="w-3 h-3 text-emerald-600" />}
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* 4. Tiêu chuẩn an toàn & Chứng nhận */}
      {certifications.length > 0 && (
        <div className="space-y-3 pt-4 border-t border-slate-100">
          <label className="text-[11px] font-extrabold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
            <Award className="w-3.5 h-3.5 text-emerald-600" />
            Tiêu chuẩn an toàn
          </label>
          <div className="flex flex-wrap gap-1.5">
            {certifications.map((cert) => {
              const isSelected = currentCert === cert;
              return (
                <button
                  key={cert}
                  onClick={() => updateFilter("cert", isSelected ? "" : cert)}
                  className={`text-xs px-3 py-1.5 rounded-xl border transition-all flex items-center gap-1.5 cursor-pointer ${
                    isSelected
                      ? "bg-emerald-50 border-emerald-600 text-emerald-800 font-bold shadow-2xs"
                      : "border-slate-200/80 hover:border-emerald-300 text-slate-600 hover:bg-slate-50 font-medium"
                  }`}
                >
                  <span>{cert}</span>
                  {isSelected && <Check className="w-3 h-3 text-emerald-600" />}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </aside>
  );
}

export default function ProductFilter(props: ProductFilterProps) {
  return (
    <Suspense
      fallback={
        <div className="bg-white rounded-3xl p-6 text-xs text-slate-400 border border-slate-100">
          Đang tải bộ lọc...
        </div>
      }
    >
      <ProductFilterContent {...props} />
    </Suspense>
  );
}
