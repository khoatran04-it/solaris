'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ShoppingBag, Sparkles, MapPin, Award, Check } from 'lucide-react';
import { ShopProductCard } from '@/types/shop';
import { formatVND } from '@/lib/api';
import { useCartStore } from '@/stores/cartStore';

interface ProductCardProps {
    product: ShopProductCard;
}

export default function ProductCard({ product }: ProductCardProps) {
    const { addItem } = useCartStore();
    const [isAdding, setIsAdding] = useState(false);
    const [addedSuccess, setAddedSuccess] = useState(false);

    const handleQuickAdd = async (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();

        if (isAdding) return;
        setIsAdding(true);

        try {
            // Thêm biến thể mặc định của sản phẩm vào giỏ
            // Default uoMId = 1 or based on default price
            await addItem(product.id, 1, 1);
            setAddedSuccess(true);
            setTimeout(() => setAddedSuccess(false), 1500);
        } catch {
            // If failed, redirect to detail page for variant selection
            window.location.href = `/san-pham/${product.slug}`;
        } finally {
            setIsAdding(false);
        }
    };

    return (
        <div className="group relative bg-white rounded-2xl border border-slate-100 shadow-xs hover:shadow-xl hover:border-emerald-200 transition-all duration-300 flex flex-col overflow-hidden">
            
            {/* Discount Badge */}
            {product.hasPromotion && product.discountPercent > 0 && (
                <div className="absolute top-3 left-3 z-10 bg-gradient-to-r from-rose-500 to-pink-600 text-white text-[11px] font-black px-2 py-0.5 rounded-full shadow-md">
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

                {/* Pricing & Add Button */}
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

                    {/* Add to Cart Button */}
                    <button
                        onClick={handleQuickAdd}
                        disabled={!product.isInStock || isAdding}
                        title={product.isInStock ? 'Thêm vào giỏ hàng' : 'Hết hàng'}
                        className={`p-2.5 rounded-full transition-all duration-200 shadow-xs flex items-center justify-center ${
                            !product.isInStock 
                                ? 'bg-slate-100 text-slate-400 cursor-not-allowed'
                                : addedSuccess
                                ? 'bg-emerald-600 text-white'
                                : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white active:scale-95'
                        }`}
                    >
                        {addedSuccess ? <Check className="w-4 h-4" /> : <ShoppingBag className="w-4 h-4" />}
                    </button>
                </div>

            </div>
        </div>
    );
}
