import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import { Layers, Sparkles } from 'lucide-react';
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
                title: `${matchedGroup.groupName} - Nông Sản Sạch Chuẩn VietGAP`,
                description: `Mua sắm các sản phẩm ${matchedGroup.groupName} tươi sạch đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
            };
        }
        for (const g of categories) {
            const matchedCat = g.categories.find((c) => c.categorySlug === slug);
            if (matchedCat) {
                return {
                    title: `${matchedCat.categoryName} - Nông Sản Sạch Chuẩn VietGAP`,
                    description: `Mua sắm ${matchedCat.categoryName} tươi ngon đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.`,
                };
            }
        }
    } catch {}
    return { title: 'Danh Mục Nông Sản Sạch' };
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

    // Phân giải slug: GroupSlug hoặc CategorySlug hoặc ID
    let title = 'Danh Mục Nông Sản';
    let groupSlug: string | undefined = undefined;
    let categorySlug: string | undefined = undefined;
    let matchedGroupName = '';

    const matchedGroup = categories.find((g) => g.groupSlug === slug || g.groupId?.toString() === slug);
    if (matchedGroup) {
        title = matchedGroup.groupName;
        groupSlug = matchedGroup.groupSlug || slug;
    } else {
        for (const g of categories) {
            const matchedCat = g.categories.find((c) => c.categorySlug === slug || c.categoryId?.toString() === slug);
            if (matchedCat) {
                title = matchedCat.categoryName;
                matchedGroupName = g.groupName;
                categorySlug = matchedCat.categorySlug || slug;
                break;
            }
        }
        // Robust Fallback: Nếu không match trong tree, gửi slug trực tiếp làm categorySlug
        if (!categorySlug && !groupSlug) {
            categorySlug = slug;
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
            {/* 1. Header Banner & Breadcrumb */}
            <div className="bg-gradient-to-r from-emerald-800 via-emerald-700 to-teal-800 rounded-3xl p-6 sm:p-10 text-white shadow-lg relative overflow-hidden">
                <div className="relative z-10 space-y-3 max-w-3xl">
                    <nav className="flex items-center gap-2 text-xs text-emerald-200 font-medium">
                        <Link href="/" className="hover:text-white transition-colors">Trang chủ</Link>
                        <span>/</span>
                        <Link href="/danh-muc" className="hover:text-white transition-colors">Danh mục</Link>
                        {matchedGroupName && (
                            <>
                                <span>/</span>
                                <span className="text-emerald-100">{matchedGroupName}</span>
                            </>
                        )}
                        <span>/</span>
                        <span className="font-bold text-white">{title}</span>
                    </nav>

                    <div className="space-y-1">
                        <h1 className="text-2xl sm:text-3xl font-black tracking-tight">
                            {title}
                        </h1>
                        <p className="text-xs text-emerald-100/90 leading-relaxed">
                            Tuyển chọn các mặt hàng {title.toLowerCase()} tươi ngon mỗi ngày, đạt tiêu chuẩn kiểm soát chất lượng FEFO và nguồn gốc vùng trồng minh bạch.
                        </p>
                    </div>

                    <div className="flex items-center gap-3 pt-1">
                        <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-white/15 rounded-full text-[11px] font-bold text-white backdrop-blur-xs">
                            <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                            {productsResult.totalRecords} sản phẩm sẵn có
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
                                        const pUrl = new URLSearchParams(sParams as any);
                                        pUrl.set('page', p.toString());
                                        const isCurrent = p === productsResult.currentPage;

                                        return (
                                            <Link
                                                key={p}
                                                href={`/danh-muc/${slug}?${pUrl.toString()}`}
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
                                <Layers className="w-8 h-8" />
                            </div>
                            <h3 className="text-lg font-bold text-slate-900">Chưa có sản phẩm trong danh mục này</h3>
                            <p className="text-xs text-slate-500 max-w-md mx-auto leading-relaxed">
                                Vui lòng quay lại sau hoặc tham khảo các danh mục nông sản tươi sạch khác của chúng tôi.
                            </p>
                            <Link
                                href="/san-pham"
                                className="inline-block px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-2xl transition-all shadow-md"
                            >
                                Khám phá toàn bộ sản phẩm
                            </Link>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
