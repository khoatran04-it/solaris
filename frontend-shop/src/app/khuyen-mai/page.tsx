import React from 'react';
import { Metadata } from 'next';
import Link from 'next/link';
import { Sparkles, Clock, ArrowRight, Tag } from 'lucide-react';
import { apiClient, formatDate } from '@/lib/api';
import { ShopPromotionBadge } from '@/types/shop';

export const metadata: Metadata = {
    title: 'Khuyến Mãi Nông Sản Sạch',
    description: 'Tổng hợp các chương trình ưu đãi, flash sale nông sản sạch tươi ngon tại Solaris Farm.',
};

export const revalidate = 60;

export default async function KhuyenMaiPage() {
    let promotions: ShopPromotionBadge[] = [];
    try {
        promotions = await apiClient.get<ShopPromotionBadge[]>('/products/promotions');
    } catch {
        promotions = [];
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
            
            {/* Header Banner */}
            <div className="bg-gradient-to-r from-rose-600 via-pink-600 to-amber-600 text-white rounded-3xl p-8 sm:p-12 shadow-xl relative overflow-hidden">
                <div className="relative z-10 space-y-3 max-w-xl">
                    <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-white/20 backdrop-blur-xs rounded-full text-xs font-bold tracking-wide">
                        <Sparkles className="w-3.5 h-3.5 text-amber-200" />
                        ĐẠI TIỆC NÔNG SẢN TƯƠI SẠCH
                    </span>
                    <h1 className="text-3xl sm:text-4xl font-black tracking-tight">
                        Chương Trình Khuyến Mãi
                    </h1>
                    <p className="text-xs sm:text-sm text-pink-100 leading-relaxed">
                        Săn nông sản sạch đạt chuẩn VietGAP với mức giá siêu ưu đãi từ vườn.
                    </p>
                </div>
            </div>

            {/* Promotions List */}
            {promotions.length > 0 ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {promotions.map((promo) => (
                        <div
                            key={promo.id}
                            className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs hover:shadow-xl hover:border-pink-200 transition-all flex flex-col justify-between space-y-4 group"
                        >
                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <span className="px-3 py-1 bg-rose-50 text-rose-700 font-extrabold text-xs rounded-full border border-rose-200">
                                        Giảm {promo.isPercentage ? `${promo.discountValue}%` : `${promo.discountValue}đ`}
                                    </span>
                                    <Tag className="w-4 h-4 text-slate-400 group-hover:text-rose-600 transition-colors" />
                                </div>

                                <h3 className="font-black text-base text-slate-900 group-hover:text-rose-600 transition-colors line-clamp-2">
                                    {promo.name}
                                </h3>

                                <div className="flex items-center gap-1.5 text-xs text-slate-500">
                                    <Clock className="w-3.5 h-3.5 text-amber-500" />
                                    <span>Hết hạn: {formatDate(promo.endDate)}</span>
                                </div>
                            </div>

                            <Link
                                href={`/khuyen-mai/${promo.slug}`}
                                className="inline-flex items-center justify-center gap-2 w-full py-2.5 bg-slate-900 group-hover:bg-rose-600 text-white rounded-xl text-xs font-bold transition-colors"
                            >
                                <span>Xem sản phẩm ưu đãi</span>
                                <ArrowRight className="w-3.5 h-3.5" />
                            </Link>
                        </div>
                    ))}
                </div>
            ) : (
                <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
                    <div className="text-6xl">🎁</div>
                    <h3 className="text-lg font-bold text-slate-800">Chưa có chương trình mới hôm nay</h3>
                    <p className="text-xs text-slate-500">Vui lòng quay lại sau để đón nhận các đợt ưu đãi vụ mùa mới!</p>
                </div>
            )}

        </div>
    );
}
