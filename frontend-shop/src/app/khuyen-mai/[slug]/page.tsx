import React from 'react';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { Sparkles, Clock, Tag } from 'lucide-react';
import ProductCard from '@/components/product/ProductCard';
import { apiClient, formatDate } from '@/lib/api';
import { ShopPromotionDetail } from '@/types/shop';

interface PromoDetailPageProps {
    params: Promise<{
        slug: string;
    }>;
}

export async function generateMetadata({ params }: PromoDetailPageProps): Promise<Metadata> {
    const { slug } = await params;
    try {
        const promo = await apiClient.get<ShopPromotionDetail>(`/products/promotions/${slug}`);
        if (!promo) return { title: 'Chương trình khuyến mãi' };

        return {
            title: `${promo.name} - Giảm Giá Nông Sản Sạch`,
            description: promo.description || `Khám phá các sản phẩm giảm giá cực sốc trong chương trình ${promo.name} tại Solaris Farm.`,
        };
    } catch {
        return { title: 'Khuyến Mãi' };
    }
}

export default async function PromoDetailPage({ params }: PromoDetailPageProps) {
    const { slug } = await params;

    let promo: ShopPromotionDetail | null = null;
    try {
        promo = await apiClient.get<ShopPromotionDetail>(`/products/promotions/${slug}`);
    } catch {
        notFound();
    }

    if (!promo) {
        notFound();
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-10">
            
            {/* Breadcrumb */}
            <div className="flex items-center gap-2 text-xs text-slate-500">
                <Link href="/" className="hover:text-emerald-600">Trang chủ</Link>
                <span>/</span>
                <Link href="/khuyen-mai" className="hover:text-emerald-600">Khuyến mãi</Link>
                <span>/</span>
                <span className="font-semibold text-slate-800 truncate max-w-xs">{promo.name}</span>
            </div>

            {/* Campaign Hero Banner */}
            <div className="bg-gradient-to-r from-rose-700 via-pink-700 to-amber-600 text-white rounded-3xl p-8 sm:p-12 shadow-xl space-y-4">
                <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-white/20 backdrop-blur-xs rounded-full text-xs font-bold tracking-wide">
                    <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                    <span>ƯU ĐÃI: {promo.isPercentage ? `GIẢM ${promo.discountValue}%` : `GIẢM ${promo.discountValue}Đ`}</span>
                </div>

                <h1 className="text-2xl sm:text-4xl font-black tracking-tight leading-tight">
                    {promo.name}
                </h1>

                {promo.description && (
                    <p className="text-xs sm:text-sm text-pink-100/90 max-w-2xl leading-relaxed">
                        {promo.description}
                    </p>
                )}

                <div className="flex items-center gap-2 text-xs text-amber-200 font-semibold pt-2">
                    <Clock className="w-4 h-4" />
                    <span>Thời gian áp dụng: {formatDate(promo.startDate)} đến {formatDate(promo.endDate)}</span>
                </div>
            </div>

            {/* Products Grid */}
            <div className="space-y-6">
                <div>
                    <h2 className="text-xl font-black text-slate-900">
                        Sản Phẩm Trong Chương Trình ({promo.products.length})
                    </h2>
                    <p className="text-xs text-slate-500 mt-0.5">
                        Giá hiển thị đã được tự động áp dụng mức chiết khấu của chương trình
                    </p>
                </div>

                {promo.products.length > 0 ? (
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
                        {promo.products.map((product) => (
                            <ProductCard key={product.id} product={product} />
                        ))}
                    </div>
                ) : (
                    <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-3">
                        <div className="text-6xl">🏷️</div>
                        <h3 className="text-lg font-bold text-slate-800">Các mặt hàng đang được chuẩn bị lên kệ</h3>
                        <Link
                            href="/san-pham"
                            className="inline-block px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-full transition-colors"
                        >
                            Xem tất cả sản phẩm khác
                        </Link>
                    </div>
                )}
            </div>

        </div>
    );
}
