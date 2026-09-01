import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import ProductCard from '@/components/product/ProductCard';
import ProductFilter from '@/components/product/ProductFilter';
import shopProductApi from '@/api/shopProductApi';
import { PagedResult } from '@/types/common';
import { ShopProductCard, ShopCategoryTree, ShopProductFilterParams } from '@/types/product';

interface CategoryPageProps {
    params: Promise<{ slug: string }>;
    searchParams: Promise<{
        page?: string;
        origin?: string;
        cert?: string;
        sort?: string;
    }>;
}

export async function generateMetadata({ params }: CategoryPageProps): Promise<Metadata> {
    const { slug } = await params;
    try {
        const categories = await shopProductApi.getCategories();
        const matchedGroup = categories.find((g) => g.groupSlug === slug);
        if (matchedGroup) {
            return {
                title: `${matchedGroup.groupName} - Nông Sản Sạch`,
                description: `Mua sắm các sản phẩm ${matchedGroup.groupName} chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
            };
        }
        for (const g of categories) {
            const matchedCat = g.categories.find((c) => c.categorySlug === slug);
            if (matchedCat) {
                return {
                    title: `${matchedCat.categoryName} - Nông Sản Sạch`,
                    description: `Mua sắm ${matchedCat.categoryName} tươi ngon chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
                };
            }
        }
    } catch {}
    return { title: 'Danh Mục Nông Sản' };
}

export default async function CategoryPage({ params, searchParams }: CategoryPageProps) {
    const { slug } = await params;
    const sParams = await searchParams;
    const page = parseInt(sParams.page || '1', 10);
    const origin = sParams.origin || '';
    const cert = sParams.cert || '';
    const sort = sParams.sort || 'newest';

    let categories: ShopCategoryTree[] = [];
    let origins: string[] = [];
    let certifications: string[] = [];

    try {
        const [catRes, origRes, certRes] = await Promise.all([
            shopProductApi.getCategories().catch(() => []),
            shopProductApi.getOrigins().catch(() => []),
            shopProductApi.getCertifications().catch(() => []),
        ]);
        categories = catRes;
        origins = origRes;
        certifications = certRes;
    } catch {}

    // Find if slug is groupSlug or categorySlug
    let title = 'Danh Mục Nông Sản';
    let groupSlug: string | undefined = undefined;
    let categorySlug: string | undefined = undefined;

    const matchedGroup = categories.find((g) => g.groupSlug === slug);
    if (matchedGroup) {
        title = matchedGroup.groupName;
        groupSlug = slug;
    } else {
        for (const g of categories) {
            const matchedCat = g.categories.find((c) => c.categorySlug === slug);
            if (matchedCat) {
                title = matchedCat.categoryName;
                categorySlug = slug;
                break;
            }
        }
    }

    const filterParams: ShopProductFilterParams = {
        pageIndex: page,
        pageSize: 12,
        categoryGroupSlug: groupSlug,
        categorySlug: categorySlug,
        origin: origin || undefined,
        certification: cert || undefined,
        sortBy: sort,
    };

    let productsResult: PagedResult<ShopProductCard> = {
        items: [],
        totalRecords: 0,
        totalPages: 0,
        currentPage: 1,
        pageSize: 12,
    };

    try {
        productsResult = await shopProductApi.getAll(filterParams);
    } catch {}

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
            {/* Breadcrumb & Header */}
            <div className="space-y-2">
                <div className="flex items-center gap-2 text-xs text-slate-500">
                    <Link href="/" className="hover:text-emerald-600">Trang chủ</Link>
                    <span>/</span>
                    <Link href="/san-pham" className="hover:text-emerald-600">Sản phẩm</Link>
                    <span>/</span>
                    <span className="font-semibold text-slate-800">{title}</span>
                </div>

                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                            {title}
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
                        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-4 shadow-sm">
                            <div className="text-6xl">🌾</div>
                            <h3 className="text-lg font-bold text-slate-800">Chưa có sản phẩm trong danh mục này</h3>
                            <p className="text-xs text-slate-500 max-w-md mx-auto">
                                Hãy thử khám phá các danh mục khác hoặc quay lại xem toàn bộ sản phẩm.
                            </p>
                            <Link
                                href="/san-pham"
                                className="inline-block px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition-colors shadow-md"
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
