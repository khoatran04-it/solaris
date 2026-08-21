'use client';

import React, { Suspense } from 'react';
import { useRouter, usePathname, useSearchParams } from 'next/navigation';
import { Filter, RotateCcw, Check, Sparkles, MapPin, Award } from 'lucide-react';
import { ShopCategoryTree } from '@/types/shop';

interface ProductFilterProps {
    categories: ShopCategoryTree[];
    origins: string[];
    certifications: string[];
}

function ProductFilterContent({ categories, origins, certifications }: ProductFilterProps) {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();

    const currentCatSlug = searchParams.get('category') || '';
    const currentOrigin = searchParams.get('origin') || '';
    const currentCert = searchParams.get('cert') || '';
    const currentSort = searchParams.get('sort') || 'newest';

    const updateFilter = (key: string, value: string) => {
        const params = new URLSearchParams(searchParams.toString());
        if (value) {
            params.set(key, value);
        } else {
            params.delete(key);
        }
        params.set('page', '1');
        router.push(`${pathname}?${params.toString()}`);
    };

    const resetFilters = () => {
        router.push(pathname);
    };

    const hasActiveFilters = Boolean(currentCatSlug || currentOrigin || currentCert || searchParams.get('search'));

    return (
        <aside className="bg-white rounded-2xl border border-slate-200/80 p-5 space-y-6 shadow-xs">
            {/* Header */}
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                <div className="flex items-center gap-2">
                    <Filter className="w-4 h-4 text-emerald-600" />
                    <h3 className="font-bold text-sm text-slate-900">Bộ Lọc Tìm Kiếm</h3>
                </div>

                {hasActiveFilters && (
                    <button
                        onClick={resetFilters}
                        className="text-[11px] font-semibold text-rose-600 hover:text-rose-700 flex items-center gap-1"
                    >
                        <RotateCcw className="w-3 h-3" />
                        Xóa lọc
                    </button>
                )}
            </div>

            {/* Sắp xếp nhanh */}
            <div className="space-y-2">
                <label className="text-xs font-bold text-slate-700 block">Sắp xếp theo</label>
                <select
                    value={currentSort}
                    onChange={(e) => updateFilter('sort', e.target.value)}
                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-emerald-500 font-medium"
                >
                    <option value="newest">Mới nhất hôm nay</option>
                    <option value="discount">Khuyến mãi nhiều nhất</option>
                    <option value="price-asc">Giá: Thấp đến Cao</option>
                    <option value="price-desc">Giá: Cao đến Thấp</option>
                    <option value="name-asc">Tên: A - Z</option>
                </select>
            </div>

            {/* Danh mục ngành hàng */}
            <div className="space-y-3">
                <label className="text-xs font-bold text-slate-700 block">Danh mục sản phẩm</label>
                <div className="space-y-1.5 max-h-56 overflow-y-auto pr-1">
                    <button
                        onClick={() => updateFilter('category', '')}
                        className={`w-full text-left text-xs px-3 py-2 rounded-lg font-medium flex items-center justify-between transition-colors ${
                            !currentCatSlug ? 'bg-emerald-50 text-emerald-800 font-bold' : 'text-slate-600 hover:bg-slate-50'
                        }`}
                    >
                        <span>Tất cả sản phẩm</span>
                        {!currentCatSlug && <Check className="w-3.5 h-3.5 text-emerald-600" />}
                    </button>

                    {categories.flatMap(g => g.categories).map((cat) => (
                        <button
                            key={cat.categoryId}
                            onClick={() => updateFilter('category', cat.categorySlug)}
                            className={`w-full text-left text-xs px-3 py-2 rounded-lg font-medium flex items-center justify-between transition-colors ${
                                currentCatSlug === cat.categorySlug ? 'bg-emerald-50 text-emerald-800 font-bold' : 'text-slate-600 hover:bg-slate-50'
                            }`}
                        >
                            <span>{cat.categoryName}</span>
                            {currentCatSlug === cat.categorySlug && <Check className="w-3.5 h-3.5 text-emerald-600" />}
                        </button>
                    ))}
                </div>
            </div>

            {/* Xuất xứ / Vùng trồng */}
            {origins.length > 0 && (
                <div className="space-y-3 pt-3 border-t border-slate-100">
                    <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                        <MapPin className="w-3.5 h-3.5 text-emerald-600" />
                        Xuất xứ / Vùng trồng
                    </label>
                    <div className="space-y-1.5 max-h-40 overflow-y-auto pr-1">
                        {origins.map((origin) => (
                            <button
                                key={origin}
                                onClick={() => updateFilter('origin', currentOrigin === origin ? '' : origin)}
                                className={`w-full text-left text-xs px-3 py-1.5 rounded-lg font-medium flex items-center justify-between transition-colors ${
                                    currentOrigin === origin ? 'bg-emerald-50 text-emerald-800 font-bold' : 'text-slate-600 hover:bg-slate-50'
                                }`}
                            >
                                <span>{origin}</span>
                                {currentOrigin === origin && <Check className="w-3.5 h-3.5 text-emerald-600" />}
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Chứng nhận chất lượng */}
            {certifications.length > 0 && (
                <div className="space-y-3 pt-3 border-t border-slate-100">
                    <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                        <Award className="w-3.5 h-3.5 text-emerald-600" />
                        Tiêu chuẩn an toàn
                    </label>
                    <div className="space-y-1.5">
                        {certifications.map((cert) => (
                            <button
                                key={cert}
                                onClick={() => updateFilter('cert', currentCert === cert ? '' : cert)}
                                className={`w-full text-left text-xs px-3 py-1.5 rounded-lg font-medium flex items-center justify-between transition-colors ${
                                    currentCert === cert ? 'bg-emerald-50 text-emerald-800 font-bold' : 'text-slate-600 hover:bg-slate-50'
                                }`}
                            >
                                <span>{cert}</span>
                                {currentCert === cert && <Check className="w-3.5 h-3.5 text-emerald-600" />}
                            </button>
                        ))}
                    </div>
                </div>
            )}
        </aside>
    );
}

export default function ProductFilter(props: ProductFilterProps) {
    return (
        <Suspense fallback={<div className="bg-white rounded-2xl p-5 text-xs text-slate-400">Đang tải bộ lọc...</div>}>
            <ProductFilterContent {...props} />
        </Suspense>
    );
}
