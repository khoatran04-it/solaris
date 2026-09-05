"use client";

import React, { useState } from "react";
import { useRouter } from "next/navigation";
import {
  ShoppingBag,
  Check,
  ShieldCheck,
  Truck,
  RotateCcw,
  Sparkles,
  Zap,
} from "lucide-react";
import {
  ShopProductDetail,
  ShopProductVariant,
  ShopVariantPrice,
} from "@/types/product";
import { formatVND } from "@/lib/utils";
import { useCartStore } from "@/stores/cartStore";

interface ProductDetailClientProps {
  product: ShopProductDetail;
}

export default function ProductDetailClient({
  product,
}: ProductDetailClientProps) {
  const router = useRouter();
  const { addItem } = useCartStore();

  // Biến thể đang chọn
  const [selectedVariant, setSelectedVariant] = useState<ShopProductVariant>(
    product.variants[0] || null,
  );

  // Đơn vị tính đang chọn
  const [selectedPrice, setSelectedPrice] = useState<ShopVariantPrice>(
    selectedVariant?.prices.find((p) => p.isDefault) ||
      selectedVariant?.prices[0] ||
      null,
  );

  const [quantity, setQuantity] = useState(1);
  const [isAdding, setIsAdding] = useState(false);
  const [addedSuccess, setAddedSuccess] = useState(false);

  const handleVariantChange = (variant: ShopProductVariant) => {
    setSelectedVariant(variant);
    const defaultPr =
      variant.prices.find((p) => p.isDefault) || variant.prices[0];
    setSelectedPrice(defaultPr);
    setQuantity(1);
  };

  const handleAddToCart = async (redirectCheckout = false) => {
    if (!selectedVariant || !selectedPrice || isAdding) return;
    setIsAdding(true);

    try {
      await addItem(selectedVariant.id, selectedPrice.uoMId, quantity);
      setAddedSuccess(true);
      setTimeout(() => setAddedSuccess(false), 2000);
      if (redirectCheckout) {
        router.push("/gio-hang");
      }
    } catch (error: any) {
      alert(error?.message || "Không thể thêm sản phẩm vào giỏ hàng.");
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
  const savedAmount = hasDiscount ? currentPrice - discountedPrice : 0;

  return (
    <div className="space-y-6">
      {/* 1. Price Box */}
      <div className="bg-gradient-to-br from-emerald-50/70 via-teal-50/40 to-slate-50 border border-emerald-200/70 p-6 rounded-3xl space-y-2 shadow-2xs">
        <div className="flex flex-wrap items-baseline gap-3">
          <span className="text-3xl sm:text-4xl font-black text-emerald-800 tracking-tight">
            {formatVND(discountedPrice)}
          </span>
          <span className="text-xs font-bold text-slate-500 uppercase">
            / {selectedPrice?.uoMName || product.baseUoMName || "Kg"}
          </span>

          {hasDiscount && (
            <span className="text-sm font-semibold text-slate-400 line-through">
              {formatVND(currentPrice)}
            </span>
          )}

          {selectedPrice?.discountPercent &&
          selectedPrice.discountPercent > 0 ? (
            <span className="px-2.5 py-0.5 bg-gradient-to-r from-rose-500 to-red-600 text-white text-[11px] font-black rounded-full shadow-xs">
              -{selectedPrice.discountPercent}%
            </span>
          ) : null}
        </div>

        {hasDiscount && savedAmount > 0 && (
          <p className="text-xs font-bold text-emerald-700 flex items-center gap-1">
            <Sparkles className="w-3.5 h-3.5 text-amber-500" />
            <span>
              Tiết kiệm <strong>{formatVND(savedAmount)}</strong> so với giá
              niêm yết
            </span>
          </p>
        )}

        <div className="pt-2 flex items-center gap-2 text-[11px] text-slate-500 font-medium border-t border-emerald-100/60">
          <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
          <span>
            Giá đã bao gồm VAT và cam kết truy xuất nguồn gốc chuẩn VietGAP
          </span>
        </div>
      </div>

      {/* 2. Quy cách đóng gói (Biến thể SKUs) */}
      {product.variants.length > 1 && (
        <div className="space-y-2.5">
          <label className="text-xs font-extrabold text-slate-800 uppercase tracking-wider block">
            Quy cách đóng gói:
          </label>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
            {product.variants.map((v) => {
              const isSelected = selectedVariant?.id === v.id;
              return (
                <button
                  key={v.id}
                  type="button"
                  onClick={() => handleVariantChange(v)}
                  className={`p-3.5 rounded-2xl border text-left transition-all cursor-pointer ${
                    isSelected
                      ? "bg-emerald-50/60 border-emerald-600 text-emerald-900 font-bold ring-2 ring-emerald-500/20 shadow-2xs"
                      : "bg-white border-slate-200/80 text-slate-700 hover:border-emerald-300 hover:bg-slate-50"
                  }`}
                >
                  <p className="text-xs truncate font-bold">{v.name}</p>
                  <span className="text-[10px] text-slate-400 font-medium block mt-0.5">
                    {v.quantityAvailable > 0
                      ? `Còn ${v.quantityAvailable}`
                      : "Tạm hết"}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* 3. Đơn vị tính bán hàng (UoM) & Tỷ lệ quy đổi */}
      {selectedVariant && selectedVariant.prices.length > 1 && (
        <div className="space-y-2.5">
          <div className="flex items-center justify-between">
            <label className="text-xs font-extrabold text-slate-800 uppercase tracking-wider block">
              Đơn vị tính bán lẻ / sỉ:
            </label>
            {selectedPrice?.conversionText && (
              <span className="text-[11px] font-bold text-emerald-800 bg-emerald-100/70 px-2.5 py-0.5 rounded-lg border border-emerald-200">
                💡 {selectedPrice.conversionText}
              </span>
            )}
          </div>

          <div className="flex flex-wrap gap-2">
            {selectedVariant.prices.map((pr) => {
              const isSelected = selectedPrice?.priceId === pr.priceId;
              return (
                <button
                  key={pr.priceId}
                  type="button"
                  onClick={() => {
                    setSelectedPrice(pr);
                    setQuantity(1);
                  }}
                  className={`px-4 py-2.5 rounded-2xl border text-xs transition-all cursor-pointer ${
                    isSelected
                      ? "bg-emerald-600 border-emerald-600 text-white font-bold shadow-xs"
                      : "bg-white border-slate-200/80 text-slate-700 hover:border-emerald-300 font-semibold"
                  }`}
                >
                  <span>{pr.uoMName}</span>
                  <span className="opacity-80 ml-1">
                    ({formatVND(pr.discountedPrice)})
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* 4. Tình trạng tồn kho khả dụng */}
      {selectedVariant && (
        <div>
          {hasStockForSelectedUoM ? (
            stockInSelectedUoM <= 10 ? (
              <div className="flex items-center gap-2 text-xs font-bold text-amber-800 bg-amber-50 px-4 py-2.5 rounded-2xl border border-amber-200 shadow-2xs">
                <span className="w-2.5 h-2.5 rounded-full bg-amber-500 animate-pulse"></span>
                <span>
                  Chỉ còn{" "}
                  <strong>
                    {stockInSelectedUoM} {selectedPrice?.uoMName}
                  </strong>{" "}
                  {conversionFactor > 1 &&
                    `(tương đương ${baseStock} ${product.baseUoMName})`}{" "}
                  khả dụng trong kho — Đặt sớm để giữ hàng tươi!
                </span>
              </div>
            ) : (
              <div className="flex items-center gap-2 text-xs font-bold text-emerald-800 bg-emerald-50 px-4 py-2.5 rounded-2xl border border-emerald-200 shadow-2xs">
                <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                <span>
                  Tồn kho khả dụng:{" "}
                  <strong>
                    {stockInSelectedUoM} {selectedPrice?.uoMName}
                  </strong>{" "}
                  {conversionFactor > 1 &&
                    `(tương đương ${baseStock} ${product.baseUoMName})`}{" "}
                  sẵn sàng đóng gói và giao nhanh 2h
                </span>
              </div>
            )
          ) : (
            <div className="flex items-center gap-2 text-xs font-bold text-rose-800 bg-rose-50 px-4 py-2.5 rounded-2xl border border-rose-200">
              <span className="w-2.5 h-2.5 rounded-full bg-rose-500"></span>
              <span>
                Mặt hàng hiện đang tạm hết tồn kho khả dụng tại các kho gần bạn
              </span>
            </div>
          )}
        </div>
      )}

      {/* 5. Bộ đếm số lượng & Các nút CTA */}
      <div className="space-y-3 pt-2">
        <div className="flex flex-col sm:flex-row items-stretch gap-3">
          {/* Quantity Counter */}
          <div className="flex items-center justify-between sm:justify-center rounded-2xl border border-slate-200 bg-slate-50/50 overflow-hidden shadow-2xs h-12 px-1">
            <button
              type="button"
              onClick={() => setQuantity(Math.max(1, quantity - 1))}
              disabled={!hasStockForSelectedUoM || quantity <= 1}
              className="w-10 h-10 flex items-center justify-center text-slate-600 hover:bg-white rounded-xl font-black text-sm transition-all disabled:opacity-20 cursor-pointer"
            >
              -
            </button>
            <span className="px-4 text-sm font-black text-slate-900 min-w-[40px] text-center">
              {quantity}
            </span>
            <button
              type="button"
              onClick={() =>
                setQuantity((prev) =>
                  Math.min(stockInSelectedUoM || 1, prev + 1),
                )
              }
              disabled={
                !hasStockForSelectedUoM || quantity >= (stockInSelectedUoM || 1)
              }
              className="w-10 h-10 flex items-center justify-center text-slate-600 hover:bg-white rounded-xl font-black text-sm transition-all disabled:opacity-20 cursor-pointer"
            >
              +
            </button>
          </div>

          {/* Add To Cart */}
          <button
            type="button"
            onClick={() => handleAddToCart(false)}
            disabled={!hasStockForSelectedUoM || isAdding}
            className={`flex-1 h-12 flex items-center justify-center gap-2 rounded-2xl text-xs font-bold transition-all shadow-md cursor-pointer ${
              !hasStockForSelectedUoM
                ? "bg-slate-200 text-slate-400 cursor-not-allowed"
                : addedSuccess
                  ? "bg-emerald-800 text-white shadow-emerald-800/30"
                  : "bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white shadow-emerald-600/20 active:scale-98"
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
                <span>
                  {hasStockForSelectedUoM
                    ? "Thêm Vào Giỏ Hàng"
                    : "Tạm Hết Hàng"}
                </span>
              </>
            )}
          </button>

          {/* Buy Now CTA */}
          {hasStockForSelectedUoM && (
            <button
              type="button"
              onClick={() => handleAddToCart(true)}
              disabled={isAdding}
              className="h-12 px-6 flex items-center justify-center gap-1.5 rounded-2xl bg-amber-500 hover:bg-amber-600 active:bg-amber-700 text-white text-xs font-extrabold transition-all shadow-md shadow-amber-500/20 active:scale-98 cursor-pointer"
            >
              <Zap className="w-4 h-4" />
              <span>Mua Ngay</span>
            </button>
          )}
        </div>

        {/* 6. Cam kết giao hàng & chất lượng */}
        <div className="grid grid-cols-3 gap-3 pt-4 border-t border-slate-100 text-center">
          <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
            <Truck className="w-4 h-4 mx-auto text-emerald-600" />
            <p className="text-[11px] font-bold text-slate-800">Giao Lạnh 2H</p>
            <p className="text-[10px] text-slate-400">Nội thành TP.HCM</p>
          </div>
          <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
            <RotateCcw className="w-4 h-4 mx-auto text-emerald-600" />
            <p className="text-[11px] font-bold text-slate-800">Đổi Trả 24H</p>
            <p className="text-[10px] text-slate-400">Hoàn tiền 100%</p>
          </div>
          <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
            <ShieldCheck className="w-4 h-4 mx-auto text-emerald-600" />
            <p className="text-[11px] font-bold text-slate-800">Quản Lý FEFO</p>
            <p className="text-[10px] text-slate-400">Xuất hạn trước</p>
          </div>
        </div>
      </div>
    </div>
  );
}
