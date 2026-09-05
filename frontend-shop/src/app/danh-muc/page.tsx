import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import { Layers, ArrowRight, Sparkles, CheckCircle2 } from 'lucide-react';
import EmptyState from '@/components/common/EmptyState';
import shopProductApi from '@/api/shopProductApi';
import { ShopCategoryTree } from '@/types/product';

export const metadata: Metadata = {
    title: 'Danh Mục Nông Sản Sạch',
    description: 'Khám phá hệ thống danh mục nông sản sạch, trái cây tươi ngon, rau củ quả hữu cơ đạt chuẩn VietGAP, GlobalGAP tại Solaris Farm.',
};

export const dynamic = 'force-dynamic';

export default async function DanhMucOverviewPage() {
    let categories: ShopCategoryTree[] = [];
    try {
        categories = await shopProductApi.getCategories();
    } catch {
        categories = [];
    }

    const totalProducts = categories.reduce(
        (sum, g) => sum + g.categories.reduce((cSum, c) => cSum + c.productCount, 0),
        0
    );

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-10">
            {/* 1. Header Banner */}
            <div className="bg-gradient-to-br from-emerald-800 via-emerald-700 to-teal-800 rounded-3xl p-6 sm:p-10 text-white shadow-xl relative overflow-hidden">
                <div className="absolute right-0 top-0 w-80 h-80 bg-white/5 rounded-full blur-3xl pointer-events-none" />
                <div className="relative z-10 max-w-2xl space-y-3">
                    <div className="inline-flex items-center gap-2 px-3 py-1 bg-white/15 rounded-full text-xs font-bold text-emerald-100 backdrop-blur-xs">
                        <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                        <span>HỆ THỐNG DANH MỤC NÔNG SẢN SOLARIS</span>
                    </div>

                    <h1 className="text-2xl sm:text-3xl lg:text-4xl font-black tracking-tight leading-tight">
                        Nông Sản Sạch Chuẩn Quốc Tế Cho Gia Đình Việt
                    </h1>

                    <p className="text-xs sm:text-sm text-emerald-100 leading-relaxed font-normal">
                        Khám phá hơn {totalProducts} mặt hàng nông sản tươi ngon, truy xuất nguồn gốc minh bạch từ các nông trại công nghệ cao Đà Lạt và các hợp tác xã nông nghiệp sạch.
                    </p>

                    <div className="flex items-center gap-4 pt-1 text-xs font-semibold text-emerald-200">
                        <span className="flex items-center gap-1.5"><CheckCircle2 className="w-4 h-4 text-amber-300" /> 100% kiểm định QC</span>
                        <span className="flex items-center gap-1.5"><CheckCircle2 className="w-4 h-4 text-amber-300" /> Giao lạnh 2H</span>
                    </div>
                </div>
            </div>

            {/* 2. Grid Các Nhóm Danh Mục */}
            <div className="space-y-8">
                <div className="flex items-center justify-between pb-3 border-b border-slate-200">
                    <div>
                        <h2 className="text-xl font-extrabold text-slate-900 tracking-tight flex items-center gap-2">
                            <Layers className="w-5 h-5 text-emerald-600" />
                            Phân Loại Ngành Hàng
                        </h2>
                        <p className="text-xs text-slate-500 mt-0.5">
                            Lựa chọn nhóm sản phẩm phù hợp với nhu cầu của bạn
                        </p>
                    </div>

                    <Link
                        href="/san-pham"
                        className="text-xs font-bold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
                    >
                        <span>Xem tất cả sản phẩm</span>
                        <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                    </Link>
                </div>

                {categories.length > 0 ? (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 sm:gap-8">
                        {categories.map((group) => {
                            const groupProductCount = group.categories.reduce((s, c) => s + c.productCount, 0);

                            return (
                                <div
                                    key={group.groupId}
                                    className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.04)] hover:shadow-xl hover:border-emerald-300 transition-all duration-300 p-6 sm:p-7 flex flex-col justify-between space-y-6 group"
                                >
                                    <div className="space-y-4">
                                        {/* Card Header */}
                                        <div className="flex items-center justify-between">
                                            <div className="w-12 h-12 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center font-bold text-lg shadow-2xs group-hover:bg-emerald-600 group-hover:text-white transition-colors">
                                                <Layers className="w-6 h-6" />
                                            </div>
                                            <span className="text-[11px] font-bold px-2.5 py-1 bg-slate-100 text-slate-600 rounded-full">
                                                {groupProductCount} sản phẩm
                                            </span>
                                        </div>

                                        <div>
                                            <h3 className="text-lg font-black text-slate-900 group-hover:text-emerald-700 transition-colors">
                                                <Link href={`/danh-muc/${group.groupSlug}`}>
                                                    {group.groupName}
                                                </Link>
                                            </h3>
                                            <p className="text-xs text-slate-500 mt-1">
                                                Bao gồm {group.categories.length} phân loại sản phẩm chuyên biệt
                                            </p>
                                        </div>

                                        {/* Sub-categories List */}
                                        {group.categories.length > 0 && (
                                            <div className="space-y-1.5 pt-2 border-t border-slate-100">
                                                {group.categories.map((cat) => (
                                                    <Link
                                                        key={cat.categoryId}
                                                        href={`/danh-muc/${cat.categorySlug}`}
                                                        className="flex items-center justify-between py-1.5 px-2.5 rounded-xl text-xs text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-semibold transition-colors"
                                                    >
                                                        <span>{cat.categoryName}</span>
                                                        <span className="text-[10px] text-slate-400 font-medium">{cat.productCount} SP</span>
                                                    </Link>
                                                ))}
                                            </div>
                                        )}
                                    </div>

                                    {/* Link to Group */}
                                    <Link
                                        href={`/danh-muc/${group.groupSlug}`}
                                        className="w-full py-3 bg-slate-50 group-hover:bg-emerald-600 text-slate-700 group-hover:text-white text-xs font-bold rounded-2xl transition-all flex items-center justify-center gap-1.5 shadow-2xs"
                                    >
                                        <span>Khám phá {group.groupName}</span>
                                        <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                                    </Link>
                                </div>
                            );
                        })}
                    </div>
                ) : (
                    <EmptyState
                        icon={<Layers className="w-7 h-7" />}
                        title="Đang cập nhật danh mục ngành hàng"
                        description="Hệ thống danh mục nông sản đang được đồng bộ và cập nhật thêm các vùng trồng mới."
                        actionText="Xem tất cả sản phẩm"
                        actionHref="/san-pham"
                    />
                )}
            </div>
        </div>
    );
}
