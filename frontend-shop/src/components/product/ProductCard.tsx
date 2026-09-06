import React from "react";
import Link from "next/link";
import { MapPin, ArrowRight, Sparkles, CheckCircle2 } from "lucide-react";
import { ShopProductCard } from "@/types/product";
import { formatVND } from "@/lib/utils";

interface ProductCardProps {
  product: ShopProductCard;
}

export default function ProductCard({ product }: ProductCardProps) {
  const productUrl = `/san-pham/${product.slug}${product.variantId ? `?variant=${product.variantId}` : ""}`;

  return (
    <div className="group relative bg-white rounded-3xl border border-slate-200/60 shadow-[0_2px_12px_-3px_rgba(0,0,0,0.03)] hover:shadow-[0_10px_25px_-5px_rgba(0,0,0,0.06)] hover:border-emerald-300 transition-all duration-300 flex flex-col overflow-hidden">
      {/* 1. Badges: Khuyến mãi & Tạm hết hàng */}
      <div className="absolute top-3 left-3 z-10 flex items-center gap-1.5">
        {product.hasPromotion && product.discountPercent > 0 && (
          <span className="bg-rose-600 text-white text-[10px] font-black px-2 py-0.5 rounded-full shadow-2xs">
            -{product.discountPercent}%
          </span>
        )}
        {product.certification && (
          <span className="bg-white/90 backdrop-blur-xs text-emerald-800 border border-emerald-200/60 text-[10px] font-bold px-2 py-0.5 rounded-full shadow-2xs flex items-center gap-0.5">
            <CheckCircle2 className="w-2.5 h-2.5 text-emerald-600" />
            {product.certification}
          </span>
        )}
      </div>

      {!product.isInStock && (
        <div className="absolute top-3 right-3 z-10 bg-slate-900/80 backdrop-blur-xs text-white text-[10px] font-bold px-2.5 py-0.5 rounded-full shadow-2xs">
          Tạm hết hàng
        </div>
      )}

      {/* 2. Product Image Area */}
      <Link
        href={productUrl}
        className="block relative aspect-square bg-slate-50 overflow-hidden border-b border-slate-100/80 group-hover:bg-emerald-50/20 transition-colors"
      >
        {product.imagePath ? (
          <img
            src={product.imagePath}
            alt={product.name}
            className="w-full h-full object-cover group-hover:scale-108 transition-transform duration-500 ease-out"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex flex-col items-center justify-center text-slate-300 group-hover:text-emerald-500 transition-colors">
            <svg
              viewBox="0 0 24 24"
              className="w-16 h-16 fill-none stroke-current stroke-1.5"
            >
              <path d="M12 2a9 9 0 0 1 9 9c0 4.97-4.03 9-9 9a9 9 0 0 1-9-9c0-4.97 4.03-9 9-9z" />
              <path d="M12 7v5l3 3" />
              <path d="M9 12a3 3 0 1 0 6 0 3 3 0 0 0-6 0z" />
            </svg>
            <span className="text-[10px] font-bold uppercase tracking-wider mt-1 text-slate-400">
              Solaris Farm
            </span>
          </div>
        )}
      </Link>

      {/* 3. Content Details */}
      <div className="p-4 sm:p-5 flex-1 flex flex-col justify-between space-y-3">
        <div className="space-y-2">
          {/* Tags: Xuất xứ & Brix */}
          <div className="flex flex-wrap items-center gap-1.5 text-[11px]">
            {product.origin && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 text-slate-700 font-semibold rounded-lg">
                <MapPin className="w-2.5 h-2.5 text-emerald-600" />
                <span>{product.origin}</span>
              </span>
            )}
            {product.brixLevel && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-amber-50 text-amber-800 font-bold rounded-lg border border-amber-200/50">
                <Sparkles className="w-2.5 h-2.5 text-amber-500" />
                <span>{product.brixLevel}°Bx</span>
              </span>
            )}
          </div>

          {/* Product Name */}
          <h3 className="font-bold text-sm text-slate-900 group-hover:text-emerald-700 transition-colors line-clamp-2 leading-snug">
            <Link href={productUrl}>{product.name}</Link>
          </h3>
        </div>

        {/* 4. Pricing & Action CTA */}
        <div className="pt-3 border-t border-slate-100 flex items-end justify-between gap-2">
          <div>
            <div className="flex items-baseline gap-1">
              <span className="font-black text-emerald-700 text-lg sm:text-xl">
                {formatVND(product.discountedPrice)}
              </span>
              <span className="text-[11px] font-semibold text-slate-500">
                /{product.baseUoMName || "Kg"}
              </span>
            </div>
            {product.hasPromotion &&
              product.discountedPrice < product.originalPrice && (
                <span className="text-xs text-slate-400 line-through font-medium block">
                  {formatVND(product.originalPrice)}
                </span>
              )}
          </div>

          {/* View Details Button */}
          <Link
            href={productUrl}
            title={
              product.isInStock ? "Xem chi tiết & Mua ngay" : "Tạm hết hàng"
            }
            className={`p-2.5 rounded-2xl transition-all flex items-center justify-center ${
              !product.isInStock
                ? "bg-slate-100 text-slate-400 pointer-events-none"
                : "bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white shadow-2xs group-hover:bg-emerald-600 group-hover:text-white active:scale-95"
            }`}
          >
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      </div>
    </div>
  );
}
