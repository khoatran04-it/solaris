"use client";

import React, { useState } from "react";
import Link from "next/link";
import {
  MapPin,
  Award,
  ArrowRight,
  Sprout,
  ShoppingBag,
  Check,
} from "lucide-react";
import { AiProductCard } from "@/types/chat";
import { formatVND } from "@/lib/utils";
import { useCartStore } from "@/stores/cartStore";

interface ProductCardMiniProps {
  product: AiProductCard;
}

export default function ProductCardMini({ product }: ProductCardMiniProps) {
  const { addItem } = useCartStore();
  const [added, setAdded] = useState(false);

  const handleAddToCart = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    try {
      await addItem(product.variantId, product.uoMId || 1, 1);
      setAdded(true);
      setTimeout(() => setAdded(false), 1800);
    } catch (err) {
      console.error("Failed to add item to cart", err);
    }
  };

  const productUrl = `/san-pham/${product.slug}${product.variantId ? `?variant=${product.variantId}` : ""}`;

  return (
    <div className="bg-white rounded-2xl border border-slate-200/80 p-3 shadow-2xs hover:shadow-md hover:border-emerald-300 transition-all flex gap-3 items-center">
      {/* Image */}
      <Link
        href={productUrl}
        className="w-16 h-16 rounded-xl bg-slate-50 overflow-hidden shrink-0 flex items-center justify-center border border-slate-100"
      >
        {product.imagePath ? (
          <img
            src={product.imagePath}
            alt={product.name}
            className="w-full h-full object-cover"
          />
        ) : (
          <Sprout className="w-6 h-6 text-emerald-500" />
        )}
      </Link>

      {/* Info */}
      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-center gap-1.5 text-[9px]">
          {product.categoryName && (
            <span className="inline-flex items-center px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded font-medium truncate max-w-[100px]">
              {product.categoryName}
            </span>
          )}
          {product.origin && (
            <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-slate-100 text-slate-700 rounded font-medium">
              <MapPin className="w-2.5 h-2.5 text-emerald-600" />
              {product.origin}
            </span>
          )}
          {product.certification && (
            <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 bg-emerald-50 text-emerald-700 rounded font-bold">
              <Award className="w-2.5 h-2.5 text-emerald-600" />
              {product.certification}
            </span>
          )}
        </div>

        <h4 className="font-bold text-xs text-slate-900 truncate">
          <Link
            href={productUrl}
            className="hover:text-emerald-700"
          >
            {product.name}
          </Link>
        </h4>

        <div className="flex items-baseline gap-1">
          <span className="font-black text-emerald-700 text-xs">
            {formatVND(product.discountedPrice || product.price)}
          </span>
          <span className="text-[10px] text-slate-400 font-medium">
            /{product.uoMName || "Kg"}
          </span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-1.5 shrink-0">
        <button
          type="button"
          onClick={handleAddToCart}
          disabled={!product.isInStock}
          className={`w-7 h-7 rounded-full flex items-center justify-center transition-all shadow-2xs cursor-pointer ${
            !product.isInStock
              ? "bg-slate-100 text-slate-400 cursor-not-allowed"
              : added
                ? "bg-emerald-600 text-white"
                : "bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white"
          }`}
          title={added ? "Đã thêm vào giỏ" : "Thêm vào giỏ hàng"}
        >
          {added ? (
            <Check className="w-3.5 h-3.5" />
          ) : (
            <ShoppingBag className="w-3.5 h-3.5" />
          )}
        </button>

        <Link
          href={productUrl}
          className="w-7 h-7 rounded-full bg-slate-50 text-slate-600 hover:bg-emerald-600 hover:text-white transition-all flex items-center justify-center shadow-2xs"
          title="Xem chi tiết"
        >
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>
    </div>
  );
}
