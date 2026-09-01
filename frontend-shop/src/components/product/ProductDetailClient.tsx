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

    const baseStock = selectedVariant ? selectedVariant.quantityAvailable : 0;
    const conversionFactor = selectedPrice?.conversionFactor || 1;
    const stockInSelectedUoM =
        conversionFactor > 0
            ? conversionFactor === 1
                ? baseStock
                : Math.floor(baseStock / conversionFactor)
            : 0;

    const hasStockForSelectedUoM = stockInSelectedUoM > 0;
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

                    {selectedPrice?.discountPercent && selectedPrice.discountPercent > 0 ? (
                        <span className="px-2 py-0.5 bg-rose-500 text-white text-[11px] font-black rounded-full shadow-xs">
                            -{selectedPrice.discountPercent}%
                        </span>
                    ) : null}
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
                                        ? 'bg-white text-emerald-600 shadow-sm border-slate-200 font-bold'
                                        : 'bg-slate-50 text-slate-500 hover:text-slate-700 border-transparent'
                                }`}
                            >
                                <p className="truncate">{v.name}</p>
                                <span className="text-[10px] text-slate-400 font-normal">
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
                    <div className="flex items-center justify-between">
                        <label className="text-xs font-bold text-slate-800 block">
                            Đơn vị tính bán lẻ / sỉ:
                        </label>
                        {selectedPrice?.conversionText && (
                            <span className="text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200">
                                💡 {selectedPrice.conversionText}
                            </span>
                        )}
                    </div>
                    <div className="flex flex-wrap gap-2">
                        {selectedVariant.prices.map((pr) => (
                            <button
                                key={pr.priceId}
                                onClick={() => {
                                    setSelectedPrice(pr);
                                    setQuantity(1);
                                }}
                                className={`px-4 py-2 rounded-xl border text-xs transition-all cursor-pointer ${
                                    selectedPrice?.priceId === pr.priceId
                                        ? 'bg-white text-emerald-600 shadow-sm border-slate-200 font-bold ring-2 ring-emerald-500/20'
                                        : 'bg-slate-50 text-slate-500 hover:text-slate-700 border-transparent font-semibold'
                                }`}
                            >
                                {pr.uoMName} ({formatVND(pr.discountedPrice)})
                            </button>
                        ))}
                    </div>
                    {selectedPrice?.conversionText && (
                        <p className="text-[11px] text-slate-500 font-medium">
                            Quy đổi: <strong>{selectedPrice.conversionText}</strong>
                        </p>
                    )}
                </div>
            )}

            {/* Tình trạng tồn kho khả dụng */}
            {selectedVariant && (
                <div>
                    {hasStockForSelectedUoM ? (
                        stockInSelectedUoM <= 10 ? (
                            <div className="flex items-center gap-2 text-xs font-bold text-amber-700 bg-amber-50 px-3.5 py-2 rounded-xl border border-amber-200 animate-in fade-in">
                                <span className="w-2 h-2 rounded-full bg-amber-500 animate-pulse"></span>
                                <span>
                                    Chỉ còn <strong>{stockInSelectedUoM} {selectedPrice?.uoMName}</strong>{' '}
                                    {conversionFactor > 1 && `(tương đương ${baseStock} ${product.baseUoMName})`}{' '}
                                    khả dụng trong kho - Nhanh tay kẻo hết!
                                </span>
                            </div>
                        ) : (
                            <div className="flex items-center gap-2 text-xs font-bold text-emerald-700 bg-emerald-50/80 px-3.5 py-2 rounded-xl border border-emerald-200 animate-in fade-in">
                                <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
                                <span>
                                    Tồn kho khả dụng: <strong>{stockInSelectedUoM} {selectedPrice?.uoMName}</strong>{' '}
                                    {conversionFactor > 1 && `(tương đương ${baseStock} ${product.baseUoMName})`}{' '}
                                    sẵn sàng giao nhanh
                                </span>
                            </div>
                        )
                    ) : (
                        baseStock > 0 ? (
                            <div className="flex items-center gap-2 text-xs font-bold text-amber-800 bg-amber-50 px-3.5 py-2 rounded-xl border border-amber-200 animate-in fade-in">
                                <span className="w-2 h-2 rounded-full bg-amber-500"></span>
                                <span>
                                    Kho còn {baseStock} {product.baseUoMName}, không đủ đóng 1 {selectedPrice?.uoMName} (quy cách: {selectedPrice?.conversionText || `1 ${selectedPrice?.uoMName} = ${conversionFactor} ${product.baseUoMName}`}). Vui lòng chọn mua theo {product.baseUoMName}!
                                </span>
                            </div>
                        ) : (
                            <div className="flex items-center gap-2 text-xs font-bold text-rose-700 bg-rose-50 px-3.5 py-2 rounded-xl border border-rose-200 animate-in fade-in">
                                <span className="w-2 h-2 rounded-full bg-rose-500"></span>
                                <span>Mặt hàng hiện đang tạm hết tồn kho khả dụng tại các kho gần bạn</span>
                            </div>
                        )
                    )}
                </div>
            )}

            {/* Số lượng & Nút Thêm vào giỏ */}
            <div className="space-y-4 pt-1">
                <div className="flex items-center gap-4">
                    {/* Counter */}
                    <div className="flex items-center rounded-xl border border-slate-200 bg-white overflow-hidden shadow-xs">
                        <button
                            type="button"
                            onClick={() => setQuantity(Math.max(1, quantity - 1))}
                            disabled={!hasStockForSelectedUoM || quantity <= 1}
                            className="px-3.5 py-2.5 text-slate-600 hover:bg-slate-100 font-bold text-sm transition-colors disabled:opacity-30 disabled:hover:bg-transparent cursor-pointer"
                        >
                            -
                        </button>
                        <span className="px-4 py-2.5 text-xs font-bold text-slate-800 min-w-[40px] text-center">
                            {quantity}
                        </span>
                        <button
                            type="button"
                            onClick={() =>
                                setQuantity((prev) =>
                                    Math.min(stockInSelectedUoM || 1, prev + 1)
                                )
                            }
                            disabled={!hasStockForSelectedUoM || quantity >= (stockInSelectedUoM || 1)}
                            className="px-3.5 py-2.5 text-slate-600 hover:bg-slate-100 font-bold text-sm transition-colors disabled:opacity-30 disabled:hover:bg-transparent cursor-pointer"
                        >
                            +
                        </button>
                    </div>

                    {/* Add to Cart CTA */}
                    <button
                        onClick={handleAddToCart}
                        disabled={!hasStockForSelectedUoM || isAdding}
                        className={`flex-1 w-full h-[46px] flex items-center justify-center gap-2 rounded-xl text-sm font-bold transition-all shadow-md cursor-pointer ${
                            !hasStockForSelectedUoM
                                ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                                : addedSuccess
                                ? 'bg-emerald-700 text-white shadow-emerald-700/30'
                                : 'bg-emerald-600 hover:bg-emerald-700 text-white'
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
                                <span>{hasStockForSelectedUoM ? 'Thêm Vào Giỏ Hàng' : 'Tạm Hết Hàng'}</span>
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
