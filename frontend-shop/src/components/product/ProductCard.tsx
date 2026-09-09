import React from "react";
import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { ShopProductCard } from "@/types/product";
import { formatVND } from "@/lib/utils";

interface ProductCardProps {
  product: ShopProductCard;
}

export default function ProductCard({ product }: ProductCardProps) {
  const productUrl = `/san-pham/${product.slug}`;
  const isMultiVariant = (product.variantCount ?? 1) > 1;
  const isPriceRange =
    product.minPrice > 0 && product.maxPrice > product.minPrice;

  return (
    <div className="group relative bg-white rounded-2xl border border-slate-200/80 shadow-2xs hover:shadow-lg hover:border-emerald-400 transition-all duration-300 flex flex-col overflow-hidden">
      {/* 1. Badges Trên Ảnh */}
      <div className="absolute top-2 left-2 z-10 flex flex-wrap items-center gap-1">
        {product.hasPromotion && product.discountPercent > 0 && (
          <span className="bg-rose-600 text-white text-[9px] font-black px-1.5 py-0.5 rounded shadow-2xs">
            -{product.discountPercent}%
          </span>
        )}
        {isMultiVariant && (
          <span className="bg-slate-900/75 backdrop-blur-xs text-white text-[9px] font-bold px-1.5 py-0.5 rounded shadow-2xs">
            {product.variantCount} quy cách
          </span>
        )}
      </div>

      {!product.isInStock && (
        <div className="absolute top-2 right-2 z-10 bg-slate-900/80 backdrop-blur-xs text-white text-[9px] font-bold px-2 py-0.5 rounded shadow-2xs">
          Tạm hết
        </div>
      )}

      {/* 2. Ảnh Sản Phẩm (Tỷ lệ 4:3 gọn gàng để show được nhiều SP) */}
      <Link
        href={productUrl}
        className="block relative aspect-[4/3] bg-slate-50 overflow-hidden border-b border-slate-100 group-hover:bg-emerald-50/10 transition-colors"
      >
        {product.imagePath ? (
          <img
            src={product.imagePath}
            alt={product.name}
            className="w-full h-full object-cover group-hover:scale-106 transition-transform duration-300 ease-out"
            loading="lazy"
          />
        ) : (
          <div className="w-full h-full flex flex-col items-center justify-center text-slate-300 group-hover:text-emerald-500 transition-colors">
            <svg
              viewBox="0 0 24 24"
              className="w-10 h-10 fill-none stroke-current stroke-1.5"
            >
              <path d="M12 2a9 9 0 0 1 9 9c0 4.97-4.03 9-9 9a9 9 0 0 1-9-9c0-4.97 4.03-9 9-9z" />
              <path d="M12 7v5l3 3" />
              <path d="M9 12a3 3 0 1 0 6 0 3 3 0 0 0-6 0z" />
            </svg>
            <span className="text-[9px] font-bold uppercase tracking-wider mt-1 text-slate-400">
              Solaris
            </span>
          </div>
        )}
      </Link>

      {/* 3. Nội Dung Thẻ (Cân bằng, tinh gọn) */}
      <div className="p-2.5 sm:p-3 flex-1 flex flex-col justify-between space-y-2">
        <div className="space-y-1">
          {/* Xuất xứ / Tag nhỏ */}
          {product.origin && (
            <div className="flex items-center gap-1 text-[9px] text-slate-500">
              <span className="inline-block w-1.5 h-1.5 rounded-full bg-emerald-500" />
              <span className="truncate max-w-[120px] font-medium">
                {product.origin}
              </span>
            </div>
          )}

          {/* Tên Sản Phẩm Cha */}
          <h3 className="font-bold text-xs sm:text-[13px] text-slate-800 group-hover:text-emerald-700 transition-colors line-clamp-2 min-h-[34px] leading-snug">
            <Link href={productUrl}>{product.name}</Link>
          </h3>
        </div>

        {/* 4. Giá Bán & Nút Xem Chi Tiết */}
        <div className="pt-2 border-t border-slate-100 flex items-end justify-between gap-1.5">
          <div className="min-w-0 flex-1">
            <div className="flex items-baseline gap-0.5">
              {isPriceRange && (
                <span className="text-[10px] font-bold text-slate-400 mr-0.5">
                  Từ
                </span>
              )}
              <span className="font-black text-emerald-700 text-sm sm:text-base leading-none truncate">
                {formatVND(product.discountedPrice || product.minPrice)}
              </span>
            </div>

            <div className="flex items-center gap-1 mt-0.5">
              <span className="text-[10px] font-medium text-slate-400">
                /{product.baseUoMName || "Kg"}
              </span>
              {product.hasPromotion &&
                product.discountedPrice < product.originalPrice && (
                  <span className="text-[10px] text-slate-400 line-through">
                    {formatVND(product.originalPrice)}
                  </span>
                )}
            </div>
          </div>

          {/* Nút Xem Chi Tiết (Gọn gàng) */}
          <Link
            href={productUrl}
            title={product.isInStock ? "Xem chi tiết quy cách" : "Tạm hết hàng"}
            className={`w-7 h-7 sm:w-8 sm:h-8 rounded-xl transition-all flex items-center justify-center shrink-0 ${
              !product.isInStock
                ? "bg-slate-100 text-slate-400 pointer-events-none"
                : "bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white group-hover:bg-emerald-600 group-hover:text-white shadow-2xs active:scale-95"
            }`}
          >
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>
    </div>
  );
}
