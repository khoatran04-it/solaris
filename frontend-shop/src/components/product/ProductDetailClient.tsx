'use client';

import React, { useState } from 'react';
import { ShoppingBag, Check, ShieldCheck, Truck, RotateCcw } from 'lucide-react';
import { ShopProductDetail, ShopProductVariant, ShopVariantPrice } from '@/types/product';
import { formatVND } from '@/lib/utils';
import { useCartStore } from '@/stores/cartStore';

interface ProductDetailClientProps {
    product: ShopProductDetail;
}

export default function ProductDetailClient({ product }: ProductDetailClientProps) {
    const { addItem } = useCartStore();

    // Biến thể đang chọn
    const [selectedVariant, setSelectedVariant] = useState<ShopProductVariant>(
        product.variants[0] || null
    );

    // Đơn vị tính đang chọn
    const [selectedPrice, setSelectedPrice] = useState<ShopVariantPrice>(
        selectedVariant?.prices.find(p => p.isDefault) || selectedVariant?.prices[0] || null
    );

    const [quantity, setQuantity] = useState(1);
    const [isAdding, setIsAdding] = useState(false);
    const [addedSuccess, setAddedSuccess] = useState(false);

    const handleVariantChange = (variant: ShopProductVariant) => {
        setSelectedVariant(variant);
        const defaultPr = variant.prices.find(p => p.isDefault) || variant.prices[0];
        setSelectedPrice(defaultPr);
    };

    const handleAddToCart = async () => {
        if (!selectedVariant || !selectedPrice || isAdding) return;
        setIsAdding(true);

        try {
            await addItem(selectedVariant.id, selectedPrice.uoMId, quantity);
            setAddedSuccess(true);
            setTimeout(() => setAddedSuccess(false), 2000);
        } catch (error: any) {
            alert(error?.message || 'Không thể thêm sản phẩm vào giỏ hàng.');
        } finally {
            setIsAdding(false);
        }
    };

    const hasStock = selectedVariant ? selectedVariant.quantityAvailable > 0 : false;
    const currentPrice = selectedPrice?.price || 0;
    const discountedPrice = selectedPrice?.discountedPrice || currentPrice;
    const hasDiscount = discountedPrice < currentPrice;

    return (
        <div className="space-y-6">
            
            {/* Price Box */}
            <div className="bg-emerald-50/60 border border-emerald-100 p-5 rounded-2xl space-y-1">
                <div className="flex items-baseline gap-3">
                    <span className="text-3xl font-black text-emerald-800">
                        {formatVND(discountedPrice)}
                    </span>
                    <span className="text-xs font-semibold text-slate-500">
                        / {selectedPrice?.uoMName || product.baseUoMName || 'Kg'}
                    </span>

                    {hasDiscount && (
                        <span className="text-sm font-semibold text-slate-400 line-through">
                            {formatVND(currentPrice)}
                        </span>
                    )}

                    {selectedPrice?.discountPercent > 0 && (
                        <span className="px-2 py-0.5 bg-rose-500 text-white text-[11px] font-black rounded-full shadow-xs">
                            -{selectedPrice.discountPercent}%
                        </span>
                    )}
                </div>

                <p className="text-[11px] text-emerald-700 font-medium flex items-center gap-1 pt-1">
                    <ShieldCheck className="w-3.5 h-3.5" />
                    Giá đã bao gồm VAT và cam kết đúng chất lượng niêm yết
                </p>
            </div>

            {/* Quy cách đóng gói (Biến thể SKUs) */}
            {product.variants.length > 1 && (
                <div className="space-y-2">
                    <label className="text-xs font-bold text-slate-800 block">
                        Chọn quy cách đóng gói:
                    </label>
                    <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                        {product.variants.map((v) => (
                            <button
                                key={v.id}
                                onClick={() => handleVariantChange(v)}
                                className={`p-3 rounded-xl border text-left text-xs transition-all ${
                                    selectedVariant?.id === v.id
                                        ? 'border-emerald-600 bg-emerald-50/50 text-emerald-900 font-bold shadow-xs'
                                        : 'border-slate-200 bg-white text-slate-700 hover:border-slate-300'
                                }`}
                            >
                                <p className="truncate">{v.name}</p>
                                <span className="text-[10px] text-slate-500 font-normal">
                                    {v.quantityAvailable > 0 ? `Còn ${v.quantityAvailable}` : 'Tạm hết'}
                                </span>
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Đơn vị tính bán hàng (UoM) */}
            {selectedVariant && selectedVariant.prices.length > 1 && (
                <div className="space-y-2">
                    <label className="text-xs font-bold text-slate-800 block">
                        Đơn vị tính bán lẻ / sỉ:
                    </label>
                    <div className="flex flex-wrap gap-2">
                        {selectedVariant.prices.map((pr) => (
                            <button
                                key={pr.priceId}
                                onClick={() => setSelectedPrice(pr)}
                                className={`px-4 py-2 rounded-xl border text-xs font-semibold transition-all ${
                                    selectedPrice?.priceId === pr.priceId
                                        ? 'border-emerald-600 bg-emerald-600 text-white shadow-xs'
                                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                                }`}
                            >
                                {pr.uoMName} ({formatVND(pr.discountedPrice)})
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Số lượng & Nút Thêm vào giỏ */}
            <div className="space-y-4 pt-2">
                <div className="flex items-center gap-4">
                    {/* Counter */}
                    <div className="flex items-center border border-slate-200 rounded-xl bg-white overflow-hidden shadow-xs">
                        <button
                            onClick={() => setQuantity(Math.max(1, quantity - 1))}
                            className="px-3.5 py-2.5 text-slate-600 hover:bg-slate-100 font-bold text-sm transition-colors"
                        >
                            -
                        </button>
                        <span className="px-4 py-2.5 text-xs font-bold text-slate-800 min-w-[40px] text-center">
                            {quantity}
                        </span>
                        <button
                            onClick={() => setQuantity(quantity + 1)}
                            className="px-3.5 py-2.5 text-slate-600 hover:bg-slate-100 font-bold text-sm transition-colors"
                        >
                            +
                        </button>
                    </div>

                    {/* Add to Cart CTA */}
                    <button
                        onClick={handleAddToCart}
                        disabled={!hasStock || isAdding}
                        className={`flex-1 flex items-center justify-center gap-2 py-3.5 px-6 rounded-2xl text-xs font-bold transition-all shadow-lg ${
                            !hasStock
                                ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                                : addedSuccess
                                ? 'bg-emerald-700 text-white shadow-emerald-700/30'
                                : 'bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white shadow-emerald-600/30 active:scale-98'
                        }`}
                    >
                        {addedSuccess ? (
                            <>
                                <Check className="w-4 h-4" />
                                <span>Đã thêm vào giỏ hàng!</span>
                            </>
                        ) : (
                            <>
                                <ShoppingBag className="w-4 h-4" />
                                <span>{hasStock ? 'Thêm Vào Giỏ Hàng' : 'Tạm Hết Hàng'}</span>
                            </>
                        )}
                    </button>
                </div>

                {/* Cam kết giao hàng */}
                <div className="grid grid-cols-3 gap-2 pt-4 border-t border-slate-100 text-center">
                    <div className="p-2.5 bg-slate-50 rounded-xl space-y-1">
                        <Truck className="w-4 h-4 mx-auto text-emerald-600" />
                        <p className="text-[10px] font-bold text-slate-700">Giao 2H</p>
                    </div>
                    <div className="p-2.5 bg-slate-50 rounded-xl space-y-1">
                        <RotateCcw className="w-4 h-4 mx-auto text-emerald-600" />
                        <p className="text-[10px] font-bold text-slate-700">Đổi trả 24H</p>
                    </div>
                    <div className="p-2.5 bg-slate-50 rounded-xl space-y-1">
                        <ShieldCheck className="w-4 h-4 mx-auto text-emerald-600" />
                        <p className="text-[10px] font-bold text-slate-700">FEFO Chuẩn</p>
                    </div>
                </div>
            </div>

        </div>
    );
}
