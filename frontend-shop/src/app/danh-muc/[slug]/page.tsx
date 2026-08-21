import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import ProductCard from '@/components/product/ProductCard';
import ProductFilter from '@/components/product/ProductFilter';
import { apiClient } from '@/lib/api';
import { PagedResult, ShopProductCard, ShopCategoryTree } from '@/types/shop';

interface CategoryPageProps {
    params: Promise<{
        slug: string;
    }>;
    searchParams: Promise<{
        page?: string;
        origin?: string;
        cert?: string;
        sort?: string;
    }>;
}

export async function generateMetadata({ params }: CategoryPageProps): Promise<Metadata> {
    const { slug } = await params;
    return {
        title: `Danh Mục: ${slug.replace(/-/g, ' ').toUpperCase()}`,
        description: `Danh sách các sản phẩm nông sản tươi sạch thuộc danh mục ${slug} tại Solaris Farm.`,
    };
}

export default async function CategoryPage({ params, searchParams }: CategoryPageProps) {
    const { slug } = await params;
    const sParams = await searchParams;
    const page = parseInt(sParams.page || '1', 10);
    const origin = sParams.origin || '';
    const cert = sParams.cert || '';
    const sort = sParams.sort || 'newest';

    const queryStr = new URLSearchParams({
        PageIndex: page.toString(),
        PageSize: '12',
        CategorySlug: slug,
        Origin: origin,
        Certification: cert,
        SortBy: sort
    }).toString();

    let productsResult: PagedResult<ShopProductCard> = { items: [], totalRecords: 0, totalPages: 0, currentPage: 1, pageSize: 12 };
    let categories: ShopCategoryTree[] = [];
    let origins: string[] = [];
    let certifications: string[] = [];

    try {
        const [prodRes, catRes, origRes, certRes] = await Promise.all([
            apiClient.get<PagedResult<ShopProductCard>>(`/products?${queryStr}`),
            apiClient.get<ShopCategoryTree[]>('/products/categories').catch(() => []),
            apiClient.get<string[]>('/products/origins').catch(() => []),
            apiClient.get<string[]>('/products/certifications').catch(() => []),
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
            
            {/* Breadcrumb */}
            <div className="flex items-center gap-2 text-xs text-slate-500">
                <Link href="/" className="hover:text-emerald-600">Trang chủ</Link>
                <span>/</span>
                <Link href="/san-pham" className="hover:text-emerald-600">Sản phẩm</Link>
                <span>/</span>
                <span className="font-semibold text-slate-800 uppercase">{slug.replace(/-/g, ' ')}</span>
            </div>

            {/* Header */}
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900 capitalize">
                    {slug.replace(/-/g, ' ')}
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Tìm thấy {productsResult.totalRecords} sản phẩm đạt chuẩn chất lượng
                </p>
            </div>

            {/* Layout */}
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                <div className="lg:col-span-1 sticky top-24">
                    <ProductFilter
                        categories={categories}
                        origins={origins}
                        certifications={certifications}
                    />
                </div>

                <div className="lg:col-span-3 space-y-8">
                    {productsResult.items.length > 0 ? (
                        <>
                            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
                                {productsResult.items.map((product) => (
                                    <ProductCard key={product.id} product={product} />
                                ))}
                            </div>

                            {/* Pagination */}
                            {productsResult.totalPages > 1 && (
                                <div className="flex items-center justify-center gap-2 pt-8 border-t border-slate-200">
                                    {Array.from({ length: productsResult.totalPages }, (_, i) => i + 1).map((p) => {
                                        const pUrl = new URLSearchParams(sParams as any);
                                        pUrl.set('page', p.toString());
                                        const isCurrent = p === productsResult.currentPage;

                                        return (
                                            <Link
                                                key={p}
                                                href={`/danh-muc/${slug}?${pUrl.toString()}`}
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
                            <div className="text-6xl">🥦</div>
                            <h3 className="text-lg font-bold text-slate-800">Chưa có sản phẩm trong danh mục này</h3>
                            <Link
                                href="/san-pham"
                                className="inline-block px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-full transition-colors"
                            >
                                Xem tất cả sản phẩm
                            </Link>
                        </div>
                    )}
                </div>
            </div>

        </div>
    );
}
