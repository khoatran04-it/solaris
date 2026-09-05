import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import { Sparkles, Search, Layers, RotateCcw } from 'lucide-react';
import ProductCard from '@/components/product/ProductCard';
import ProductFilter from '@/components/product/ProductFilter';
import shopProductApi from '@/api/shopProductApi';
import { PagedResult } from '@/types/common';
import { ShopProductCard, ShopCategoryTree, ShopProductFilterParams } from '@/types/product';

export const metadata: Metadata = {
    title: 'Tất Cả Nông Sản Sạch',
    description: 'Khám phá tất cả các loại rau củ quả, trái cây tươi sạch đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.',
};

export const dynamic = 'force-dynamic';

interface SanPhamPageProps {
    searchParams: Promise<{
        search?: string;
        category?: string;
        group?: string;
        origin?: string;
        cert?: string;
        sort?: string;
        page?: string;
    }>;
}

export default async function SanPhamPage({ searchParams }: SanPhamPageProps) {
    const params = await searchParams;
    const page = parseInt(params.page || '1', 10);
    const search = params.search || '';
    const categorySlug = params.category || '';
    const groupSlug = params.group || '';
    const origin = params.origin || '';
    const cert = params.cert || '';
    const sort = params.sort || 'newest';

    const filterParams: ShopProductFilterParams = {
        pageIndex: page,
        pageSize: 12,
        search: search || undefined,
        categorySlug: categorySlug || undefined,
        categoryGroupSlug: groupSlug || undefined,
        origin: origin || undefined,
        certification: cert || undefined,
        sortBy: sort,
    };

    let productsResult: PagedResult<ShopProductCard> = { items: [], totalRecords: 0, totalPages: 0, currentPage: 1, pageSize: 12 };
    let categories: ShopCategoryTree[] = [];
    let origins: string[] = [];
    let certifications: string[] = [];

    try {
        const [prodRes, catRes, origRes, certRes] = await Promise.all([
            shopProductApi.getAll(filterParams),
            shopProductApi.getCategories().catch(() => []),
            shopProductApi.getOrigins().catch(() => []),
            shopProductApi.getCertifications().catch(() => []),
        ]);

        productsResult = prodRes;
        categories = catRes;
        origins = origRes;
        certifications = certRes;
    } catch {
        // Fallback
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
            {/* 1. Header Banner & Breadcrumbs */}
            <div className="bg-gradient-to-r from-emerald-800 via-emerald-700 to-teal-800 rounded-3xl p-6 sm:p-10 text-white shadow-lg relative overflow-hidden">
                <div className="relative z-10 space-y-3 max-w-3xl">
                    <nav className="flex items-center gap-2 text-xs text-emerald-200 font-medium">
                        <Link href="/" className="hover:text-white transition-colors">Trang chủ</Link>
                        <span>/</span>
                        <span className="text-white font-bold">Tất cả sản phẩm</span>
                        {search && (
                            <>
                                <span>/</span>
                                <span className="text-amber-300 font-bold">Từ khóa: &quot;{search}&quot;</span>
                            </>
                        )}
                    </nav>

                    <div className="space-y-1">
                        <h1 className="text-2xl sm:text-3xl font-black tracking-tight">
                            {search ? `Kết quả tìm kiếm cho "${search}"` : 'Tất Cả Nông Sản Sạch'}
                        </h1>
                        <p className="text-xs text-emerald-100/90 leading-relaxed">
                            Tuyển chọn nông sản hữu cơ, trái cây nhập khẩu và rau củ Đà Lạt thu hoạch mỗi sớm mai với cam kết an toàn thực phẩm.
                        </p>
                    </div>

                    <div className="flex items-center gap-3 pt-1">
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-white/15 rounded-full text-[11px] font-bold text-white backdrop-blur-xs">
                            <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                            Hiển thị {productsResult.totalRecords} sản phẩm
                        </span>
                    </div>
                </div>
            </div>

            {/* 2. Layout: Sidebar Filter + Product Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                
                {/* Left Sidebar Filter */}
                <div className="lg:col-span-1 sticky top-28">
                    <ProductFilter
                        categories={categories}
                        origins={origins}
                        certifications={certifications}
                    />
                </div>

                {/* Right Product Grid */}
                <div className="lg:col-span-3 space-y-8">
                    {productsResult.items.length > 0 ? (
                        <>
                            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
                                {productsResult.items.map((product) => (
                                    <ProductCard key={product.id} product={product} />
                                ))}
                            </div>

                            {/* Pagination Controls */}
                            {productsResult.totalPages > 1 && (
                                <div className="flex items-center justify-center gap-2 pt-8 border-t border-slate-200">
                                    {Array.from({ length: productsResult.totalPages }, (_, i) => i + 1).map((p) => {
                                        const pUrl = new URLSearchParams(params as any);
                                        pUrl.set('page', p.toString());
                                        const isCurrent = p === productsResult.currentPage;

                                        return (
                                            <Link
                                                key={p}
                                                href={`/san-pham?${pUrl.toString()}`}
                                                className={`w-10 h-10 rounded-2xl flex items-center justify-center text-xs font-bold transition-all ${
                                                    isCurrent
                                                        ? 'bg-emerald-600 text-white shadow-md shadow-emerald-600/30'
                                                        : 'bg-white text-slate-700 hover:bg-slate-100 border border-slate-200'
                                                }`}
                                            >
                                                {p}
                                            </Link>
                                        );
                                    })}
                                </div>
                            )}
                        </>
                    ) : (
                        <div className="bg-white rounded-3xl border border-slate-200 p-16 text-center space-y-4 shadow-sm">
                            <div className="w-16 h-16 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto">
                                <Search className="w-8 h-8" />
                            </div>
                            <h3 className="text-lg font-bold text-slate-900">Không tìm thấy sản phẩm phù hợp</h3>
                            <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
                                Vui lòng thử tìm kiếm với từ khóa khác hoặc xóa bớt các điều kiện lọc đang áp dụng.
                            </p>
                            <Link
                                href="/san-pham"
                                className="inline-flex items-center gap-1.5 px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-2xl transition-all shadow-md"
                            >
                                <RotateCcw className="w-4 h-4" />
                                <span>Xóa tất cả bộ lọc</span>
                            </Link>
                        </div>
                    )}
                </div>

            </div>

        </div>
    );
}
