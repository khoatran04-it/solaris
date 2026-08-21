import React from 'react';
import { Metadata } from 'next';
import ProductCard from '@/components/product/ProductCard';
import ProductFilter from '@/components/product/ProductFilter';
import shopProductApi from '@/api/shopProductApi';
import { PagedResult } from '@/types/common';
import { ShopProductCard, ShopCategoryTree, ShopProductFilterParams } from '@/types/product';
import Link from 'next/link';

export const metadata: Metadata = {
    title: 'Danh Sách Nông Sản Sạch',
    description: 'Khám phá tất cả các loại rau củ quả, trái cây tươi sạch đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.',
};

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

    // Fetch products, categories, origins and certs in parallel
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
            
            {/* Breadcrumb & Header */}
            <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs text-slate-500">
                    <Link href="/" className="hover:text-emerald-600">Trang chủ</Link>
                    <span>/</span>
                    <span className="font-semibold text-slate-800">Sản phẩm</span>
                    {search && (
                        <>
                            <span>/</span>
                            <span className="text-emerald-600 font-bold">Tìm: &quot;{search}&quot;</span>
                        </>
                    )}
                </div>

                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                            Nông Sản Tươi Ngon
                        </h1>
                        <p className="text-xs text-slate-500 mt-1">
                            Hiển thị {productsResult.totalRecords} sản phẩm đạt chuẩn chất lượng
                        </p>
                    </div>
                </div>
            </div>

            {/* Layout: Sidebar Filter + Product Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                
                {/* Left Sidebar Filter */}
                <div className="lg:col-span-1 sticky top-24">
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
                                                className={`w-9 h-9 rounded-xl flex items-center justify-center text-xs font-bold transition-all ${
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
                        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-4">
                            <div className="text-6xl">🌾</div>
                            <h3 className="text-lg font-bold text-slate-800">Không tìm thấy sản phẩm phù hợp</h3>
                            <p className="text-xs text-slate-500 max-w-md mx-auto">
                                Hãy thử thay đổi từ khóa tìm kiếm hoặc chọn lại các tiêu chí lọc vùng trồng, chứng nhận.
                            </p>
                            <Link
                                href="/san-pham"
                                className="inline-block px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-full transition-colors"
                            >
                                Xóa tất cả bộ lọc
                            </Link>
                        </div>
                    )}
                </div>

            </div>

        </div>
    );
}



