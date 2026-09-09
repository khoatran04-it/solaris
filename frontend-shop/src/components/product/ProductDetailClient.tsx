"use client";

import React, { useState, useEffect, useMemo } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  ShopProductDetail,
  ShopProductVariant,
  ShopVariantPrice,
} from "@/types/product";
import { formatVND } from "@/lib/utils";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { useLocationStore } from "@/stores/locationStore";
import shopProductApi from "@/api/shopProductApi";

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
  const { isAuthenticated } = useAuthStore();
  const { selectedWarehouse, deliveryAddress, deliveryDistrict, openModal } =
    useLocationStore();

  const [currentProduct, setCurrentProduct] =
    useState<ShopProductDetail>(product);

  // Xác định biến thể ban đầu dựa trên param ?variant= hoặc lấy biến thể đầu tiên
  const initialVariant = useMemo(() => {
    if (variantParam && currentProduct.variants?.length) {
      const match = currentProduct.variants.find(
        (v) =>
          v.id.toString() === variantParam ||
          v.code.toLowerCase() === variantParam.toLowerCase(),
      );
      if (match) return match;
    }
    return currentProduct.variants?.[0] || null;
  }, [variantParam, currentProduct.variants]);

  // Biến thể đang chọn
  const [selectedVariant, setSelectedVariant] =
    useState<ShopProductVariant | null>(initialVariant);

  // Đơn vị tính đang chọn
  const [selectedPrice, setSelectedPrice] = useState<ShopVariantPrice | null>(
    initialVariant?.prices.find((p) => p.isDefault) ||
      initialVariant?.prices[0] ||
      null,
  );

  // Ảnh hiển thị chính (ưu tiên ảnh biến thể, fallback ảnh sản phẩm cha)
  const [activeImage, setActiveImage] = useState<string | null>(
    initialVariant?.imagePath || currentProduct.imagePath || null,
  );

  // Cập nhật dữ liệu tồn kho khi kho phục vụ (Store-Locking) thay đổi
  useEffect(() => {
    if (selectedWarehouse?.id) {
      shopProductApi
        .getBySlug(product.slug, selectedWarehouse.id)
        .then((updated) => {
          if (updated) {
            setCurrentProduct(updated);
            if (selectedVariant) {
              const matched = updated.variants.find(
                (v) => v.id === selectedVariant.id,
              );
              if (matched) {
                setSelectedVariant(matched);
                if (selectedPrice) {
                  const matchedPrice =
                    matched.prices.find(
                      (p) => p.priceId === selectedPrice.priceId,
                    ) || matched.prices[0];
                  setSelectedPrice(matchedPrice);
                }
              }
            }
          }
        })
        .catch(() => {});
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedWarehouse?.id, product.slug]);

  // Đồng bộ khi URL param variant thay đổi
  useEffect(() => {
    if (variantParam && currentProduct.variants?.length) {
      const match = currentProduct.variants.find(
        (v) =>
          v.id.toString() === variantParam ||
          v.code.toLowerCase() === variantParam.toLowerCase(),
      );
      if (match && match.id !== selectedVariant?.id) {
        setSelectedVariant(match);
        const defaultPr =
          match.prices.find((p) => p.isDefault) || match.prices[0] || null;
        setSelectedPrice(defaultPr);
        setActiveImage(match.imagePath || currentProduct.imagePath || null);
        setQuantity(1);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [variantParam, currentProduct.variants]);

  const [quantity, setQuantity] = useState(1);
  const [isAdding, setIsAdding] = useState(false);
  const [addedSuccess, setAddedSuccess] = useState(false);

  const handleVariantChange = (variant: ShopProductVariant) => {
    setSelectedVariant(variant);
    const defaultPr =
      variant.prices.find((p) => p.isDefault) || variant.prices[0] || null;
    setSelectedPrice(defaultPr);
    setActiveImage(variant.imagePath || currentProduct.imagePath || null);
    setQuantity(1);
    if (typeof window !== "undefined") {
      window.history.replaceState(
        null,
        "",
        `/san-pham/${currentProduct.slug}?variant=${variant.id}`,
      );
    }
  };

  const handleAddToCart = async (redirectCheckout = false) => {
    if (!isAuthenticated) {
      router.push(`/dang-nhap?redirect=/san-pham/${currentProduct.slug}`);
      return;
    }
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

  // Danh sách ảnh từ sản phẩm cha và các biến thể
  const galleryImages = useMemo(() => {
    const list: { url: string; label: string; variantId?: number }[] = [];
    if (currentProduct.imagePath) {
      list.push({ url: currentProduct.imagePath, label: currentProduct.name });
    }
    currentProduct.variants?.forEach((v) => {
      if (v.imagePath && !list.some((item) => item.url === v.imagePath)) {
        list.push({ url: v.imagePath, label: v.name, variantId: v.id });
      }
    });
    return list;
  }, [currentProduct]);

  // Thuộc tính kết hợp giữa sản phẩm cha và biến thể đang chọn
  const mergedAttributes = useMemo(() => {
    return {
      ...(currentProduct.attributes || {}),
      ...(selectedVariant?.attributes || {}),
    };
  }, [currentProduct.attributes, selectedVariant?.attributes]);

  const origin =
    mergedAttributes["Xuất xứ / Vùng trồng"] ||
    mergedAttributes["Xuất xứ"] ||
    null;
  const cert =
    mergedAttributes["Chứng nhận chất lượng"] ||
    mergedAttributes["Chứng nhận"] ||
    null;

  return (
    <div className="space-y-10">
      {/* 1. KHU VỰC TRÌNH DIỄN & ĐẶT MUA SẢN PHẨM */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 lg:gap-10 items-start">
        {/* Cột trái: Ảnh sản phẩm lớn & Bộ sưu tập biến thể */}
        <div className="lg:col-span-6 space-y-3.5">
          <div className="relative aspect-square sm:aspect-[4/3] lg:aspect-square bg-white rounded-2xl border border-slate-200/80 shadow-xs overflow-hidden flex items-center justify-center group">
            {activeImage ? (
              <img
                src={activeImage}
                alt={selectedVariant?.name || currentProduct.name}
                className="w-full h-full object-cover group-hover:scale-104 transition-transform duration-500 ease-out"
              />
            ) : (
              <div className="w-full h-full flex flex-col items-center justify-center text-slate-300">
                <span className="text-4xl font-black text-slate-200">
                  SOLARIS
                </span>
                <span className="text-[10px] font-bold uppercase tracking-wider mt-1 text-slate-400">
                  Nông sản sạch
                </span>
              </div>
            )}

            {/* Badges thông tin trên ảnh (Thuần Typography, KHÔNG ICON) */}
            <div className="absolute top-3 left-3 flex flex-col gap-1.5 items-start">
              <span className="bg-emerald-700 text-white text-[10px] font-black tracking-wider uppercase px-2.5 py-1 rounded-md shadow-xs">
                NÔNG SẢN VIETGAP
              </span>
              {cert && (
                <span className="bg-white/95 backdrop-blur-xs text-emerald-800 text-[10px] font-bold px-2 py-0.5 rounded-md shadow-xs border border-emerald-200">
                  {cert}
                </span>
              )}
            </div>
          </div>

          {/* Thumbnails từ các biến thể */}
          {galleryImages.length > 1 && (
            <div className="flex gap-2 overflow-x-auto pb-1.5">
              {galleryImages.map((img, idx) => {
                const isActive = activeImage === img.url;
                return (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => {
                      setActiveImage(img.url);
                      if (img.variantId) {
                        const targetVar = currentProduct.variants.find(
                          (v) => v.id === img.variantId,
                        );
                        if (targetVar) handleVariantChange(targetVar);
                      }
                    }}
                    className={`w-14 h-14 rounded-xl border overflow-hidden shrink-0 transition-all cursor-pointer ${
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

        {/* Cột phải: Thông tin định danh & Ma trận mua hàng Shopee Style */}
        <div className="lg:col-span-6 bg-white rounded-2xl shadow-xs border border-slate-200/80 p-5 sm:p-7 space-y-5">
          {/* Header định danh & Kho hàng phục vụ */}
          <div className="space-y-2 pb-3.5 border-b border-slate-100">
            {/* Thanh thông tin địa chỉ giao hàng */}
            <div className="flex flex-wrap items-center justify-between gap-2 bg-slate-50 border border-slate-200/70 px-3 py-1.5 rounded-xl text-xs">
              <div className="flex items-center gap-2 text-slate-700">
                <span className="inline-block w-2 h-2 rounded-full bg-emerald-500" />
                <span className="text-[11px] font-semibold">
                  Giao đến:{" "}
                  <strong className="text-slate-900">
                    {deliveryAddress ||
                      (deliveryDistrict
                        ? `${deliveryDistrict}, TP.HCM`
                        : "TP. Hồ Chí Minh")}
                  </strong>
                </span>
              </div>
              <button
                type="button"
                onClick={openModal}
                className="text-[11px] font-black text-emerald-700 hover:text-emerald-900 hover:underline cursor-pointer"
              >
                [Đổi địa chỉ]
              </button>
            </div>

            {/* SKU & Category Tags */}
            <div className="flex flex-wrap items-center gap-1.5 pt-1 text-[11px]">
              <span className="font-mono font-bold text-slate-400 uppercase">
                SKU: {selectedVariant?.code || currentProduct.code}
              </span>
              <span className="text-slate-300">•</span>
              <span className="font-semibold text-slate-600">
                {currentProduct.categoryName || "Nông sản tươi"}
              </span>
              {origin && (
                <>
                  <span className="text-slate-300">•</span>
                  <span className="text-emerald-700 font-bold bg-emerald-50 px-2 py-0.5 rounded border border-emerald-100">
                    {origin}
                  </span>
                </>
              )}
            </div>

            {/* Tên dòng sản phẩm cha */}
            <h1 className="text-xl sm:text-2xl font-black text-slate-900 leading-snug">
              {currentProduct.name}
            </h1>
          </div>

          {/* Bảng giá hiện thời */}
          <div className="bg-slate-50/80 border border-slate-200/80 p-4 sm:p-5 rounded-2xl space-y-1.5">
            <div className="flex flex-wrap items-baseline gap-2.5">
              <span className="text-2xl sm:text-3xl font-black text-emerald-800 tracking-tight">
                {formatVND(discountedPrice)}
              </span>
              <span className="text-xs font-bold text-slate-500 uppercase">
                / {selectedPrice?.uoMName || currentProduct.baseUoMName || "Kg"}
              </span>

              {hasDiscount && (
                <span className="text-xs font-semibold text-slate-400 line-through">
                  {formatVND(currentPrice)}
                </span>
              )}

              {selectedPrice?.discountPercent &&
              selectedPrice.discountPercent > 0 ? (
                <span className="px-2 py-0.5 bg-rose-600 text-white text-[10px] font-black rounded shadow-2xs">
                  -{selectedPrice.discountPercent}%
                </span>
              ) : null}
            </div>

            {hasDiscount && savedAmount > 0 && (
              <p className="text-[11px] font-bold text-emerald-700">
                Tiết kiệm {formatVND(savedAmount)} so với giá niêm yết
              </p>
            )}

            <p className="text-[10px] text-slate-400 font-medium pt-1 border-t border-slate-200/60">
              Giá đã bao gồm VAT và cam kết truy xuất nguồn gốc chuẩn VietGAP
            </p>
          </div>

          {/* MA TRẬN BIẾN THỂ SHOPEE STYLE */}
          <div className="space-y-4 pt-1">
            {/* TẦNG 1: Quy cách đóng gói (SKU / Variant) */}
            {currentProduct.variants.length > 1 ? (
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-[11px] font-black text-slate-700 uppercase tracking-wider">
                    Quy cách đóng gói:
                  </label>
                  <span className="text-[10px] text-slate-400 font-medium">
                    {currentProduct.variants.length} quy cách có sẵn
                  </span>
                </div>

                <div className="flex flex-wrap gap-2">
                  {currentProduct.variants.map((v) => {
                    const isSelected = selectedVariant?.id === v.id;
                    const isOutOfStock = v.quantityAvailable <= 0;

                    return (
                      <button
                        key={v.id}
                        type="button"
                        onClick={() => handleVariantChange(v)}
                        className={`px-3.5 py-2 rounded-xl text-xs text-left transition-all cursor-pointer flex items-center gap-2 border ${
                          isSelected
                            ? "bg-emerald-50/80 border-emerald-600 text-emerald-950 font-bold ring-2 ring-emerald-500/20 shadow-2xs"
                            : isOutOfStock
                              ? "bg-slate-50 border-slate-200/80 text-slate-400 opacity-60 line-through hover:opacity-100 hover:border-slate-300"
                              : "bg-white border-slate-200 text-slate-700 hover:border-emerald-300 hover:bg-slate-50 font-medium"
                        }`}
                      >
                        <span>{v.name}</span>
                        <span
                          className={`text-[9px] font-bold px-1.5 py-0.5 rounded ${
                            isSelected
                              ? "bg-emerald-600 text-white"
                              : isOutOfStock
                                ? "bg-slate-200 text-slate-600"
                                : "bg-slate-100 text-slate-600"
                          }`}
                        >
                          {v.quantityAvailable > 0
                            ? `Còn ${v.quantityAvailable}`
                            : "Hết"}
                        </span>
                      </button>
                    );
                  })}
                </div>
              </div>
            ) : currentProduct.variants.length === 1 ? (
              <div className="text-xs text-slate-700 bg-slate-50 border border-slate-200/70 px-3.5 py-2.5 rounded-xl flex items-center justify-between">
                <span>
                  Quy cách:{" "}
                  <strong className="text-emerald-800">
                    {currentProduct.variants[0].name}
                  </strong>
                </span>
                <span className="font-mono text-[10px] text-slate-400">
                  Mã: {currentProduct.variants[0].code}
                </span>
              </div>
            ) : null}

            {/* TẦNG 2: Đơn vị tính bán hàng & Tỷ lệ quy đổi */}
            {selectedVariant && selectedVariant.prices.length > 1 && (
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-[11px] font-black text-slate-700 uppercase tracking-wider">
                    Đơn vị tính bán lẻ / sỉ:
                  </label>
                  {selectedPrice?.conversionText && (
                    <span className="text-[10px] font-bold text-emerald-800 bg-emerald-50 px-2 py-0.5 rounded border border-emerald-200">
                      Quy đổi: {selectedPrice.conversionText}
                    </span>
                  )}
                </div>

                <div className="flex flex-wrap gap-2">
                  {selectedVariant.prices.map((pr) => {
                    const isSelected = selectedPrice?.priceId === pr.priceId;
                    const factor = pr.conversionFactor || 1;
                    const uomStock =
                      factor > 0
                        ? Math.floor(selectedVariant.quantityAvailable / factor)
                        : 0;
                    const isUomOutOfStock = uomStock <= 0;

                    return (
                      <button
                        key={pr.priceId}
                        type="button"
                        onClick={() => {
                          setSelectedPrice(pr);
                          setQuantity(1);
                        }}
                        className={`px-3.5 py-2 rounded-xl border text-xs transition-all cursor-pointer flex items-center gap-1.5 ${
                          isSelected
                            ? "bg-emerald-700 border-emerald-700 text-white font-bold shadow-xs"
                            : isUomOutOfStock
                              ? "bg-slate-50 border-slate-200 text-slate-400 opacity-60 line-through hover:opacity-100 hover:border-slate-300"
                              : "bg-white border-slate-200 text-slate-700 hover:border-emerald-300 font-semibold"
                        }`}
                      >
                        <span>{pr.uoMName}</span>
                        <span
                          className={
                            isSelected ? "text-emerald-100" : "text-slate-400"
                          }
                        >
                          ({formatVND(pr.discountedPrice)})
                        </span>
                        {isUomOutOfStock && (
                          <span className="text-[9px] bg-slate-200 text-slate-600 px-1 py-0.2 rounded font-bold">
                            Hết
                          </span>
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* THANH TÓM TẮT LỰA CHỌN (Shopee Selection Bar) */}
            <div className="bg-slate-50 border border-slate-200/80 rounded-xl p-3 space-y-1.5 text-xs">
              <div className="flex items-center justify-between">
                <span className="text-slate-500 font-medium">
                  Đang chọn:{" "}
                  <strong className="text-slate-900">
                    {selectedVariant?.name}
                  </strong>{" "}
                  •{" "}
                  <strong className="text-emerald-800">
                    {selectedPrice?.uoMName}
                  </strong>
                </span>
                <span className="font-black text-emerald-800 text-sm">
                  {formatVND(discountedPrice)}
                </span>
              </div>

              {/* Tình trạng tồn kho khả dụng tại khu vực nhận hàng */}
              <div>
                {hasStockForSelectedUoM ? (
                  stockInSelectedUoM <= 10 ? (
                    <div className="flex items-center gap-1.5 text-[11px] font-bold text-amber-800">
                      <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse shrink-0" />
                      <span>
                        Khu vực của bạn: Chỉ còn{" "}
                        <strong>
                          {stockInSelectedUoM} {selectedPrice?.uoMName}
                        </strong>
                        {conversionFactor > 1 &&
                          ` (~${baseStock} ${currentProduct.baseUoMName})`}
                      </span>
                    </div>
                  ) : (
                    <div className="flex items-center gap-1.5 text-[11px] font-bold text-emerald-800">
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 shrink-0" />
                      <span>
                        Khu vực của bạn: Còn{" "}
                        <strong>
                          {stockInSelectedUoM} {selectedPrice?.uoMName}
                        </strong>{" "}
                        (Sẵn sàng giao nhanh 2h)
                      </span>
                    </div>
                  )
                ) : (
                  <div className="flex items-center justify-between text-[11px] font-bold text-rose-700 bg-rose-50 px-2.5 py-1.5 rounded-lg border border-rose-100">
                    <div className="flex items-center gap-1.5">
                      <span className="w-1.5 h-1.5 rounded-full bg-rose-500 shrink-0" />
                      <span>Tạm hết hàng tại khu vực giao này</span>
                    </div>
                    <button
                      type="button"
                      onClick={openModal}
                      className="text-rose-800 underline hover:text-rose-950 ml-2"
                    >
                      [Đổi địa chỉ nhận]
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* BỘ ĐIỀU KHIỂN SỐ LƯỢNG & CÁC NÚT MUA HÀNG */}
          <div className="space-y-3 pt-2">
            <div className="flex flex-col sm:flex-row items-stretch gap-2.5">
              {/* Quantity Counter */}
              <div className="flex items-center justify-between sm:justify-center rounded-xl border border-slate-200 bg-slate-50 overflow-hidden shadow-2xs h-11 px-1">
                <button
                  type="button"
                  onClick={() => setQuantity(Math.max(1, quantity - 1))}
                  disabled={!hasStockForSelectedUoM || quantity <= 1}
                  className="w-9 h-9 flex items-center justify-center text-slate-600 hover:bg-white rounded-lg font-black text-sm transition-all disabled:opacity-20 cursor-pointer"
                >
                  -
                </button>
                <span className="px-3 text-xs font-black text-slate-900 min-w-[36px] text-center">
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
                  className="w-9 h-9 flex items-center justify-center text-slate-600 hover:bg-white rounded-lg font-black text-sm transition-all disabled:opacity-20 cursor-pointer"
                >
                  +
                </button>
              </div>

              {/* Add To Cart Button */}
              <button
                type="button"
                onClick={() => handleAddToCart(false)}
                disabled={!hasStockForSelectedUoM || isAdding}
                className={`flex-1 h-11 flex items-center justify-center gap-2 rounded-xl text-xs font-extrabold uppercase tracking-wide transition-all shadow-xs cursor-pointer ${
                  !hasStockForSelectedUoM
                    ? "bg-slate-200 text-slate-400 cursor-not-allowed"
                    : addedSuccess
                      ? "bg-emerald-800 text-white"
                      : "bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white shadow-emerald-600/10 active:scale-98"
                }`}
              >
                {addedSuccess
                  ? "Đã Thêm Vào Giỏ Hàng!"
                  : hasStockForSelectedUoM
                    ? "Thêm Vào Giỏ Hàng"
                    : "Tạm Hết Hàng Tại Khu Vực Này"}
              </button>

              {/* Buy Now CTA */}
              {hasStockForSelectedUoM && (
                <button
                  type="button"
                  onClick={() => handleAddToCart(true)}
                  disabled={isAdding}
                  className="h-11 px-5 flex items-center justify-center rounded-xl bg-amber-500 hover:bg-amber-600 active:bg-amber-700 text-white text-xs font-extrabold uppercase tracking-wide transition-all shadow-xs active:scale-98 cursor-pointer"
                >
                  Mua Ngay
                </button>
              )}
            </div>

            {/* Cam kết giao hàng & chất lượng (Thuần Text Cards, KHÔNG ICON) */}
            <div className="grid grid-cols-3 gap-2 pt-3 border-t border-slate-100 text-center">
              <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100 space-y-0.5">
                <p className="text-[11px] font-black text-slate-800 uppercase tracking-tight">
                  Giao Lạnh 2H
                </p>
                <p className="text-[10px] text-slate-400 font-medium">
                  Nội thành TP.HCM
                </p>
              </div>
              <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100 space-y-0.5">
                <p className="text-[11px] font-black text-slate-800 uppercase tracking-tight">
                  Đổi Trả 24H
                </p>
                <p className="text-[10px] text-slate-400 font-medium">
                  Hoàn tiền 100%
                </p>
              </div>
              <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-100 space-y-0.5">
                <p className="text-[11px] font-black text-slate-800 uppercase tracking-tight">
                  Quản Lý FEFO
                </p>
                <p className="text-[10px] text-slate-400 font-medium">
                  Date tươi mới
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. KHU VỰC MÔ TẢ & THÔNG SỐ NÔNG SẢN (Thuần Typography, KHÔNG ICON) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 pt-4 border-t border-slate-200">
        {/* Cột trái: Mô tả chi tiết biến thể & dòng sản phẩm */}
        <div className="lg:col-span-2 space-y-5 bg-white p-5 sm:p-7 rounded-2xl border border-slate-200/80 shadow-2xs">
          <div className="pb-3 border-b border-slate-100">
            <span className="text-[10px] font-black text-emerald-800 uppercase tracking-widest bg-emerald-50 px-2.5 py-1 rounded border border-emerald-100">
              THÔNG TIN SẢN PHẨM
            </span>
            <h2 className="text-base sm:text-lg font-black text-slate-900 mt-2">
              Mô Tả & Thông Tin Chi Tiết
            </h2>
          </div>

          {/* Mô tả riêng của Biến thể (SKU) */}
          {selectedVariant?.description ? (
            <div className="bg-emerald-50/70 border border-emerald-200/80 p-4 sm:p-5 rounded-xl space-y-1.5">
              <span className="text-[11px] font-black text-emerald-800 uppercase tracking-wide block">
                Đặc Điểm & Quy Cách: {selectedVariant.name}
              </span>
              <p className="text-xs sm:text-sm text-slate-700 leading-relaxed whitespace-pre-line">
                {selectedVariant.description}
              </p>
            </div>
          ) : null}

          {/* Mô tả chung dòng sản phẩm */}
          <div className="text-xs sm:text-sm text-slate-600 leading-relaxed space-y-2.5">
            {selectedVariant?.description && currentProduct.description && (
              <h4 className="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">
                Giới thiệu dòng sản phẩm {currentProduct.name}
              </h4>
            )}
            <p className="whitespace-pre-line">
              {currentProduct.description ||
                (!selectedVariant?.description &&
                  `${currentProduct.name} được nuôi trồng và thu hoạch theo quy chuẩn nông nghiệp sạch, bảo đảm độ tươi giòn tự nhiên và giữ trọn hàm lượng vitamin khoáng chất thiết yếu.`)}
            </p>
          </div>

          {/* Quality Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-3 border-t border-slate-100">
            <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-100">
              <h4 className="text-xs font-black text-slate-800 uppercase tracking-tight">
                Kiểm Soát FEFO Nghiêm Ngặt
              </h4>
              <p className="text-[11px] text-slate-500 mt-0.5 leading-relaxed">
                Xuất kho ưu tiên theo hạn sử dụng, đảm bảo độ tươi mới tối đa.
              </p>
            </div>

            <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-100">
              <h4 className="text-xs font-black text-slate-800 uppercase tracking-tight">
                Nghị Định 15/2018/NĐ-CP
              </h4>
              <p className="text-[11px] text-slate-500 mt-0.5 leading-relaxed">
                Đầy đủ hồ sơ tự công bố chất lượng và kiểm nghiệm an toàn thực
                phẩm.
              </p>
            </div>
          </div>
        </div>

        {/* Cột phải: Thông số nông sản theo EAV */}
        <div className="bg-white p-5 sm:p-7 rounded-2xl border border-slate-200/80 shadow-2xs space-y-4">
          <div className="pb-3 border-b border-slate-100">
            <span className="text-[10px] font-black text-emerald-800 uppercase tracking-widest bg-emerald-50 px-2.5 py-1 rounded border border-emerald-100">
              TIÊU CHUẨN
            </span>
            <h2 className="text-base font-black text-slate-900 mt-2">
              Thông Số Nông Sản
            </h2>
          </div>

          <dl className="space-y-3 text-xs">
            <div className="flex justify-between pb-2 border-b border-slate-100">
              <dt className="text-slate-400 font-medium">Đơn vị cơ sở:</dt>
              <dd className="font-bold text-slate-800">
                {currentProduct.baseUoMName || "Kg"}
              </dd>
            </div>

            <div className="flex justify-between pb-2 border-b border-slate-100">
              <dt className="text-slate-400 font-medium">Danh mục:</dt>
              <dd className="font-bold text-slate-800">
                {currentProduct.categoryName || "Nông sản"}
              </dd>
            </div>

            {selectedVariant && (
              <div className="flex justify-between pb-2 border-b border-slate-100">
                <dt className="text-slate-400 font-medium">
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
                className="flex justify-between pb-2 border-b border-slate-100"
              >
                <dt className="text-slate-400 font-medium">{key}:</dt>
                <dd className="font-bold text-emerald-800 text-right max-w-[160px] truncate">
                  {value}
                </dd>
              </div>
            ))}

            <div className="flex justify-between pt-0.5">
              <dt className="text-slate-400 font-medium">Bảo quản đề xuất:</dt>
              <dd className="font-bold text-slate-800">Nhiệt độ 2°C - 8°C</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  );
}
