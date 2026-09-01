import React from 'react';
import Link from 'next/link';
import { MapPin, Award, ArrowRight } from 'lucide-react';
import { ShopProductCard } from '@/types/product';
import { formatVND } from '@/lib/utils';

interface ProductCardProps {
    product: ShopProductCard;
}

export default function ProductCard({ product }: ProductCardProps) {
    return (
        <div className="group relative bg-white rounded-2xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)] border border-slate-100 hover:shadow-md hover:-translate-y-0.5 transition-all duration-300 flex flex-col overflow-hidden">
            
            {/* Discount Badge */}
            {product.hasPromotion && product.discountPercent > 0 && (
                <div className="absolute top-3 left-3 z-10 bg-rose-500 text-white text-[11px] font-black px-2 py-0.5 rounded-lg shadow-sm">
                    -{product.discountPercent}%
                </div>
            )}

            {/* Out of Stock Badge */}
            {!product.isInStock && (
                <div className="absolute top-3 right-3 z-10 bg-slate-900/80 backdrop-blur-xs text-white text-[10px] font-bold px-2 py-0.5 rounded-md">
                    Tạm hết hàng
                </div>
            )}

            {/* Product Image Area */}
            <Link href={`/san-pham/${product.slug}`} className="block relative aspect-square bg-slate-50 overflow-hidden">
                {product.imagePath ? (
                    <img
                        src={product.imagePath}
                        alt={product.name}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                        loading="lazy"
                    />
                ) : (
                    <div className="w-full h-full flex items-center justify-center text-4xl bg-emerald-50/50 group-hover:scale-105 transition-transform duration-500">
                        🥑
                    </div>
                )}
            </Link>

            {/* Content Area */}
            <div className="p-4 flex-1 flex flex-col justify-between space-y-3">
                <div className="space-y-1.5">
                    {/* Category & Origin Tags */}
                    <div className="flex flex-wrap items-center gap-1.5 text-[10px]">
                        {product.origin && (
                            <span className="inline-flex items-center gap-0.5 px-2 py-0.5 bg-slate-100 text-slate-700 font-semibold rounded-md">
                                <MapPin className="w-2.5 h-2.5 text-emerald-600" />
                                {product.origin}
                            </span>
                        )}
                        {product.certification && (
                            <span className="inline-flex items-center gap-0.5 px-2 py-0.5 bg-emerald-50 text-emerald-700 font-bold rounded-md border border-emerald-200">
                                <Award className="w-2.5 h-2.5 text-emerald-600" />
                                {product.certification}
                            </span>
                        )}
                        {product.brixLevel && (
                            <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-amber-50 text-amber-700 font-bold rounded-md">
                                🍬 {product.brixLevel}°Bx
                            </span>
                        )}
                    </div>

                    {/* Product Title */}
                    <h3 className="font-bold text-sm text-slate-900 group-hover:text-emerald-700 line-clamp-2 transition-colors">
                        <Link href={`/san-pham/${product.slug}`}>
                            {product.name}
                        </Link>
                    </h3>
                </div>

                {/* Pricing & View Detail Button */}
                <div className="pt-2 border-t border-slate-50 flex items-center justify-between">
                    <div>
                        <div className="flex items-baseline gap-1.5">
                            <span className="font-black text-emerald-700 text-base">
                                {formatVND(product.discountedPrice)}
                            </span>
                            <span className="text-[11px] font-medium text-slate-500">
                                /{product.baseUoMName || 'Kg'}
                            </span>
                        </div>
                        {product.hasPromotion && product.discountedPrice < product.originalPrice && (
                            <span className="text-[11px] text-slate-400 line-through">
                                {formatVND(product.originalPrice)}
                            </span>
                        )}
                    </div>

                    {/* View Detail Button (replaces broken Quick Add) */}
                    <Link
                        href={`/san-pham/${product.slug}`}
                        title={product.isInStock ? 'Xem chi tiết & Chọn mua' : 'Hết hàng'}
                        className={`p-2.5 shadow-md flex items-center justify-center transition-all duration-200 ${
                            !product.isInStock
                                ? 'bg-slate-100 text-slate-400 pointer-events-none rounded-xl'
                                : 'bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl active:scale-95'
                        }`}
                    >
                        <ArrowRight className="w-4 h-4" />
                    </Link>
                </div>
            </div>
        </div>
    );
}
