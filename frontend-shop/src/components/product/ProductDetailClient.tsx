"use client";

import React, { useState, useEffect, useMemo } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  ShoppingBag,
  Check,
  ShieldCheck,
  Truck,
  RotateCcw,
  Sparkles,
  Zap,
  MapPin,
  Award,
  CheckCircle2,
  FileText,
  Info,
  Layers,
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
  const searchParams = useSearchParams();
  const variantParam = searchParams.get("variant");
  const { addItem } = useCartStore();

  // Xác định biến thể ban đầu dựa trên param ?variant= hoặc lấy biến thể đầu tiên
  const initialVariant = useMemo(() => {
    if (variantParam && product.variants?.length) {
      const match = product.variants.find(
        (v) =>
          v.id.toString() === variantParam ||
          v.code.toLowerCase() === variantParam.toLowerCase(),
      );
      if (match) return match;
    }
    return product.variants?.[0] || null;
  }, [variantParam, product.variants]);

  // Biến thể đang chọn
  const [selectedVariant, setSelectedVariant] =
    useState<ShopProductVariant | null>(initialVariant);

  // Đơn vị tính đang chọn
  const [selectedPrice, setSelectedPrice] = useState<ShopVariantPrice | null>(
    selectedVariant?.prices.find((p) => p.isDefault) ||
      selectedVariant?.prices[0] ||
      null,
  );

  // Ảnh hiển thị chính (ưu tiên ảnh biến thể, fallback ảnh sản phẩm cha)
  const [activeImage, setActiveImage] = useState<string | null>(
    selectedVariant?.imagePath || product.imagePath || null,
  );

  // Đồng bộ khi URL param variant thay đổi
  useEffect(() => {
    if (variantParam && product.variants?.length) {
      const match = product.variants.find(
        (v) =>
          v.id.toString() === variantParam ||
          v.code.toLowerCase() === variantParam.toLowerCase(),
      );
      if (match && match.id !== selectedVariant?.id) {
        setSelectedVariant(match);
        const defaultPr =
          match.prices.find((p) => p.isDefault) || match.prices[0] || null;
        setSelectedPrice(defaultPr);
        setActiveImage(match.imagePath || product.imagePath || null);
        setQuantity(1);
      }
    }
  }, [variantParam, product.variants]);

  const [quantity, setQuantity] = useState(1);
  const [isAdding, setIsAdding] = useState(false);
  const [addedSuccess, setAddedSuccess] = useState(false);

  const handleVariantChange = (variant: ShopProductVariant) => {
    setSelectedVariant(variant);
    const defaultPr =
      variant.prices.find((p) => p.isDefault) || variant.prices[0] || null;
    setSelectedPrice(defaultPr);
    setActiveImage(variant.imagePath || product.imagePath || null);
    setQuantity(1);
    if (typeof window !== "undefined") {
      window.history.replaceState(
        null,
        "",
        `/san-pham/${product.slug}?variant=${variant.id}`,
      );
    }
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

  // Lấy danh sách ảnh từ sản phẩm cha và các biến thể
  const galleryImages = useMemo(() => {
    const list: { url: string; label: string; variantId?: number }[] = [];
    if (product.imagePath) {
      list.push({ url: product.imagePath, label: product.name });
    }
    product.variants?.forEach((v) => {
      if (v.imagePath && !list.some((item) => item.url === v.imagePath)) {
        list.push({ url: v.imagePath, label: v.name, variantId: v.id });
      }
    });
    return list;
  }, [product]);

  // Thuộc tính kết hợp giữa sản phẩm cha và biến thể đang chọn
  const mergedAttributes = useMemo(() => {
    return {
      ...(product.attributes || {}),
      ...(selectedVariant?.attributes || {}),
    };
  }, [product.attributes, selectedVariant?.attributes]);

  const origin =
    mergedAttributes["Xuất xứ / Vùng trồng"] ||
    mergedAttributes["Xuất xứ"] ||
    null;
  const cert =
    mergedAttributes["Chứng nhận chất lượng"] ||
    mergedAttributes["Chứng nhận"] ||
    null;

  return (
    <div className="space-y-12">
      {/* 1. KHU VỰC TRÌNH DIỄN & ĐẶT MUA SẢN PHẨM */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
        {/* Cột trái: Ảnh sản phẩm lớn & Bộ sưu tập biến thể */}
        <div className="lg:col-span-6 space-y-4">
          <div className="relative aspect-square bg-white rounded-3xl border border-slate-200/80 shadow-[0_4px_25px_-5px_rgba(0,0,0,0.06)] overflow-hidden flex items-center justify-center group">
            {activeImage ? (
              <img
                src={activeImage}
                alt={selectedVariant?.name || product.name}
                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500 ease-out"
              />
            ) : (
              <div className="w-full h-full flex flex-col items-center justify-center text-slate-300">
                <svg
                  viewBox="0 0 24 24"
                  className="w-24 h-24 fill-none stroke-current stroke-1.5"
                >
                  <path d="M12 2a9 9 0 0 1 9 9c0 4.97-4.03 9-9 9a9 9 0 0 1-9-9c0-4.97 4.03-9 9-9z" />
                  <path d="M12 7v5l3 3" />
                  <path d="M9 12a3 3 0 1 0 6 0 3 3 0 0 0-6 0z" />
                </svg>
                <span className="text-xs font-bold uppercase tracking-wider mt-2 text-slate-400">
                  Solaris Farm
                </span>
              </div>
            )}

            {/* Badges tiêu chuẩn trên ảnh */}
            <div className="absolute top-4 left-4 flex flex-col gap-1.5 items-start">
              <span className="bg-emerald-600 text-white text-[11px] font-extrabold px-3 py-1 rounded-full shadow-md flex items-center gap-1">
                <CheckCircle2 className="w-3.5 h-3.5" />
                100% Nông Sản Sạch
              </span>
              {cert && (
                <span className="bg-white/95 backdrop-blur-xs text-emerald-800 text-[10px] font-bold px-2.5 py-0.5 rounded-full shadow-xs border border-emerald-200">
                  {cert}
                </span>
              )}
            </div>
          </div>

          {/* Thumbnails từ các biến thể */}
          {galleryImages.length > 1 && (
            <div className="flex gap-2.5 overflow-x-auto pb-2">
              {galleryImages.map((img, idx) => {
                const isActive = activeImage === img.url;
                return (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => {
                      setActiveImage(img.url);
                      if (img.variantId) {
                        const targetVar = product.variants.find(
                          (v) => v.id === img.variantId,
                        );
                        if (targetVar) handleVariantChange(targetVar);
                      }
                    }}
                    className={`w-16 h-16 rounded-2xl border overflow-hidden shrink-0 transition-all cursor-pointer ${
                      isActive
                        ? "border-emerald-600 ring-2 ring-emerald-500/30 scale-95"
                        : "border-slate-200 hover:border-emerald-300 opacity-70 hover:opacity-100"
                    }`}
                    title={img.label}
                  >
                    <img
                      src={img.url}
                      alt={img.label}
                      className="w-full h-full object-cover"
                    />
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Cột phải: Thông tin định danh & Bộ điều khiển mua hàng */}
        <div className="lg:col-span-6 bg-white rounded-3xl shadow-[0_4px_25px_-5px_rgba(0,0,0,0.05)] border border-slate-200/80 p-6 sm:p-8 space-y-6">
          <div className="space-y-2.5 pb-4 border-b border-slate-100">
            {/* Dynamic SKU & Harvest Badge */}
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-mono font-bold tracking-wider text-slate-400 uppercase">
                SKU: {selectedVariant?.code || product.code}
              </span>
              <span className="text-xs font-semibold text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200/60 flex items-center gap-1">
                <Sparkles className="w-3 h-3 text-emerald-600" />
                Thu hoạch trong ngày
              </span>
            </div>

            {/* Tên dòng sản phẩm */}
            <h1 className="text-2xl sm:text-3xl font-black text-slate-900 leading-snug">
              {product.name}
            </h1>

            {/* Subtitle biến thể đang chọn */}
            {selectedVariant &&
              selectedVariant.name.trim().toLowerCase() !==
                product.name.trim().toLowerCase() && (
                <div className="flex items-center gap-2">
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 bg-emerald-50 text-emerald-800 text-xs font-bold rounded-xl border border-emerald-200/60">
                    <Layers className="w-3.5 h-3.5 text-emerald-600" />
                    Biến thể: {selectedVariant.name}
                  </span>
                </div>
              )}

            {/* Category & Attributes Preview */}
            <div className="flex flex-wrap items-center gap-2 pt-1 text-xs">
              <span className="px-3 py-1 bg-slate-100 text-slate-700 font-bold rounded-xl">
                {product.categoryName || "Nông sản tươi"}
              </span>
              {origin && (
                <span className="px-3 py-1 bg-slate-100 text-slate-700 font-bold rounded-xl flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5 text-emerald-600" />
                  {origin}
                </span>
              )}
              {cert && (
                <span className="px-3 py-1 bg-emerald-50 text-emerald-800 font-bold rounded-xl border border-emerald-200/60 flex items-center gap-1">
                  <Award className="w-3.5 h-3.5 text-emerald-600" />
                  {cert}
                </span>
              )}
            </div>
          </div>

          {/* Bảng giá */}
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

          {/* Chọn biến thể (Quy cách đóng gói) */}
          {product.variants.length > 1 ? (
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
          ) : product.variants.length === 1 ? (
            <div className="text-xs text-slate-600 bg-slate-50 border border-slate-200/70 p-3 rounded-2xl flex items-center justify-between">
              <span className="font-semibold text-slate-700">
                Quy cách:{" "}
                <strong className="text-emerald-800">
                  {product.variants[0].name}
                </strong>
              </span>
              <span className="font-mono text-[11px] text-slate-400 font-medium">
                Mã SKU: {product.variants[0].code}
              </span>
            </div>
          ) : null}

          {/* Đơn vị tính bán hàng & Tỷ lệ quy đổi */}
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

          {/* Tình trạng tồn kho khả dụng */}
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
                    Mặt hàng hiện đang tạm hết tồn kho khả dụng tại các kho gần
                    bạn
                  </span>
                </div>
              )}
            </div>
          )}

          {/* Bộ đếm số lượng & Các nút CTA */}
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
                    !hasStockForSelectedUoM ||
                    quantity >= (stockInSelectedUoM || 1)
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

            {/* Cam kết giao hàng & chất lượng */}
            <div className="grid grid-cols-3 gap-3 pt-4 border-t border-slate-100 text-center">
              <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
                <Truck className="w-4 h-4 mx-auto text-emerald-600" />
                <p className="text-[11px] font-bold text-slate-800">
                  Giao Lạnh 2H
                </p>
                <p className="text-[10px] text-slate-400">Nội thành TP.HCM</p>
              </div>
              <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
                <RotateCcw className="w-4 h-4 mx-auto text-emerald-600" />
                <p className="text-[11px] font-bold text-slate-800">
                  Đổi Trả 24H
                </p>
                <p className="text-[10px] text-slate-400">Hoàn tiền 100%</p>
              </div>
              <div className="p-3 bg-slate-50/80 rounded-2xl border border-slate-100 space-y-1">
                <ShieldCheck className="w-4 h-4 mx-auto text-emerald-600" />
                <p className="text-[11px] font-bold text-slate-800">
                  Quản Lý FEFO
                </p>
                <p className="text-[10px] text-slate-400">Xuất hạn trước</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. KHU VỰC MÔ TẢ & THÔNG SỐ NÔNG SẢN MINH BẠCH (Nghị định 15/2018/NĐ-CP) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 pt-6 border-t border-slate-200">
        {/* Cột trái: Mô tả chi tiết biến thể & dòng sản phẩm */}
        <div className="lg:col-span-2 space-y-6 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-2xs">
          <div className="flex items-center gap-2.5 pb-4 border-b border-slate-100">
            <div className="p-2 rounded-xl bg-emerald-50 text-emerald-700">
              <FileText className="w-5 h-5" />
            </div>
            <h2 className="text-lg font-black text-slate-900">
              Mô Tả & Thông Tin Chi Tiết
            </h2>
          </div>

          {/* Mô tả riêng của Biến thể (SKU) - Được đồng bộ trực tiếp từ ProductVariant.Description */}
          {selectedVariant?.description ? (
            <div className="bg-emerald-50/70 border border-emerald-200/80 p-5 sm:p-6 rounded-2xl space-y-2 shadow-2xs">
              <div className="flex items-center gap-2 text-xs font-black text-emerald-800 uppercase tracking-wide">
                <Sparkles className="w-4 h-4 text-emerald-600" />
                <span>Đặc Điểm & Quy Cách Biến Thể: {selectedVariant.name}</span>
              </div>
              <p className="text-sm text-slate-700 leading-relaxed whitespace-pre-line">
                {selectedVariant.description}
              </p>
            </div>
          ) : null}

          {/* Mô tả chung dòng sản phẩm (Từ Product.Description) */}
          <div className="text-sm text-slate-600 leading-relaxed space-y-3">
            {selectedVariant?.description && product.description && (
              <h4 className="text-xs font-extrabold text-slate-500 uppercase tracking-wider">
                Giới thiệu dòng sản phẩm {product.name}
              </h4>
            )}
            <p className="whitespace-pre-line">
              {product.description ||
                (!selectedVariant?.description &&
                  `${product.name} được nuôi trồng và thu hoạch theo quy chuẩn nông nghiệp sạch, bảo đảm độ tươi giòn tự nhiên và giữ trọn hàm lượng vitamin khoáng chất thiết yếu.`)}
            </p>
          </div>

          {/* Quality Badges */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-4 border-t border-slate-100">
            <div className="p-4 rounded-2xl bg-slate-50/80 border border-slate-100 flex items-start gap-3">
              <ShieldCheck className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
              <div>
                <h4 className="text-xs font-bold text-slate-800">
                  Kiểm Soát FEFO Nghiêm Ngặt
                </h4>
                <p className="text-[11px] text-slate-500 mt-0.5">
                  Xuất kho ưu tiên theo hạn sử dụng, đảm bảo độ tươi mới tối đa.
                </p>
              </div>
            </div>

            <div className="p-4 rounded-2xl bg-slate-50/80 border border-slate-100 flex items-start gap-3">
              <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
              <div>
                <h4 className="text-xs font-bold text-slate-800">
                  Nghị Định 15/2018/NĐ-CP
                </h4>
                <p className="text-[11px] text-slate-500 mt-0.5">
                  Đầy đủ hồ sơ tự công bố chất lượng và kiểm nghiệm an toàn thực
                  phẩm.
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Cột phải: Thông số nông sản theo EAV */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-2xs space-y-5">
          <div className="flex items-center gap-2.5 pb-4 border-b border-slate-100">
            <div className="p-2 rounded-xl bg-emerald-50 text-emerald-700">
              <Info className="w-5 h-5" />
            </div>
            <h2 className="text-base font-black text-slate-900">
              Thông Số Nông Sản
            </h2>
          </div>

          <dl className="space-y-3.5 text-xs">
            <div className="flex justify-between pb-2.5 border-b border-slate-100">
              <dt className="text-slate-400 font-semibold">Đơn vị cơ sở:</dt>
              <dd className="font-bold text-slate-800">
                {product.baseUoMName || "Kg"}
              </dd>
            </div>

            <div className="flex justify-between pb-2.5 border-b border-slate-100">
              <dt className="text-slate-400 font-semibold">Danh mục:</dt>
              <dd className="font-bold text-slate-800">
                {product.categoryName || "Nông sản"}
              </dd>
            </div>

            {selectedVariant && (
              <div className="flex justify-between pb-2.5 border-b border-slate-100">
                <dt className="text-slate-400 font-semibold">
                  Biến thể đang chọn:
                </dt>
                <dd className="font-bold text-emerald-800 text-right max-w-[160px] truncate">
                  {selectedVariant.name}
                </dd>
              </div>
            )}

            {Object.entries(mergedAttributes).map(([key, value]) => (
              <div
                key={key}
                className="flex justify-between pb-2.5 border-b border-slate-100"
              >
                <dt className="text-slate-400 font-semibold">{key}:</dt>
                <dd className="font-bold text-emerald-800 text-right max-w-[160px] truncate">
                  {value}
                </dd>
              </div>
            ))}

            <div className="flex justify-between pt-1">
              <dt className="text-slate-400 font-semibold">
                Bảo quản đề xuất:
              </dt>
              <dd className="font-bold text-slate-800">Nhiệt độ 2°C - 8°C</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
}
