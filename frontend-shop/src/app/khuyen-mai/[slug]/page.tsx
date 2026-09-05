import React from 'react';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { Sparkles, Clock, Tag } from 'lucide-react';
import ProductCard from '@/components/product/ProductCard';
import EmptyState from '@/components/common/EmptyState';
import shopProductApi from '@/api/shopProductApi';
import { formatDate } from '@/lib/utils';
import { ShopProductCard } from '@/types/product';
import { ShopPromotionDetail } from '@/types/promotion';

interface PromoDetailPageProps {
    params: Promise<{
        slug: string;
    }>;
}

export async function generateMetadata({ params }: PromoDetailPageProps): Promise<Metadata> {
    const { slug } = await params;
    try {
        const promo = await shopProductApi.getPromotionBySlug(slug);
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
        promo = await shopProductApi.getPromotionBySlug(slug);
    } catch {
        notFound();
    }

    if (!promo) {
        notFound();
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-10">
            
            {/* Breadcrumb */}
            <div className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                <Link href="/" className="hover:text-emerald-700 transition-colors">Trang chủ</Link>
                <span>/</span>
                <Link href="/khuyen-mai" className="hover:text-emerald-700 transition-colors">Khuyến mãi</Link>
                <span>/</span>
                <span className="font-bold text-slate-900 truncate max-w-xs">{promo.name}</span>
            </div>

            {/* Campaign Hero Banner */}
            <div className="bg-gradient-to-r from-rose-700 via-pink-700 to-amber-700 text-white rounded-3xl p-8 sm:p-12 shadow-xl space-y-4 relative overflow-hidden">
                <div className="absolute right-0 top-0 w-96 h-96 bg-white/10 rounded-full blur-3xl pointer-events-none" />
                
                <div className="relative z-10 space-y-3">
                    <div className="inline-flex items-center gap-1.5 px-3.5 py-1 bg-white/20 backdrop-blur-xs rounded-full text-xs font-extrabold tracking-wide">
                        <Sparkles className="w-3.5 h-3.5 text-amber-300 animate-pulse" />
                        <span>ƯU ĐÃI ĐẶC BIỆT: {promo.isPercentage ? `GIẢM ${promo.discountValue}%` : `GIẢM ${promo.discountValue}Đ`}</span>
                    </div>

                    <h1 className="text-2xl sm:text-4xl font-black tracking-tight leading-tight">
                        {promo.name}
                    </h1>

                    {promo.description && (
                        <p className="text-xs sm:text-sm text-pink-100/90 max-w-2xl leading-relaxed font-medium">
                            {promo.description}
                        </p>
                    )}

                    <div className="flex items-center gap-2 text-xs text-amber-200 font-bold pt-2">
                        <Clock className="w-4 h-4" />
                        <span>Thời gian áp dụng: {formatDate(promo.startDate)} đến {formatDate(promo.endDate)}</span>
                    </div>
                </div>
            </div>

            {/* Products Grid */}
            <div className="space-y-6">
                <div>
                    <h2 className="text-xl font-black text-slate-900 tracking-tight">
                        Sản Phẩm Áp Dụng Ưu Đãi ({promo.products.length})
                    </h2>
                    <p className="text-xs text-slate-500 mt-0.5">
                        Giá hiển thị đã được tự động áp dụng mức chiết khấu của chương trình
                    </p>
                </div>

                {promo.products.length > 0 ? (
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
                        {promo.products.map((product: ShopProductCard) => (
                            <ProductCard key={product.id} product={product} />
                        ))}
                    </div>
                ) : (
                    <EmptyState
                        icon={<Tag className="w-7 h-7" />}
                        title="Các mặt hàng đang được chuẩn bị lên kệ"
                        description="Các sản phẩm áp dụng mức chiết khấu này sẽ sớm xuất hiện tại cửa hàng."
                        actionText="Xem tất cả sản phẩm khác"
                        actionHref="/san-pham"
                    />
                )}
            </div>

        </div>
    );
}
