"use client";

import React, { useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  ShoppingBag,
  Trash2,
  ArrowRight,
  ShieldCheck,
  Truck,
  RotateCcw,
  Sparkles,
  ThermometerSnowflake,
} from "lucide-react";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";
import { formatVND } from "@/lib/utils";
import EmptyState from "@/components/common/EmptyState";

export default function GioHangPage() {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const {
    cart,
    guestItems,
    totalCount,
    isLoading,
    fetchCart,
    updateQuantity,
    removeItem,
    clearCart,
  } = useCartStore();

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  const handleProceedCheckout = () => {
    if (!isAuthenticated) {
      router.push("/dang-nhap?redirect=/thanh-toan");
    } else {
      router.push("/thanh-toan");
    }
  };

  const hasItems =
    (cart && cart.items && cart.items.length > 0) ||
    (guestItems && guestItems.length > 0);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between pb-5 border-b border-slate-200">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 flex items-center gap-2.5">
            <ShoppingBag className="w-7 h-7 text-emerald-600" />
            <span>Giỏ Hàng Của Bạn</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Bạn đang có{" "}
            <strong className="text-emerald-700">{totalCount} món hàng</strong>{" "}
            trong giỏ
          </p>
        </div>

        {hasItems && (
          <button
            onClick={() => {
              if (
                confirm("Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?")
              ) {
                clearCart();
              }
            }}
            className="text-xs font-bold text-rose-600 hover:text-rose-700 hover:bg-rose-50 px-3 py-1.5 rounded-xl transition-all flex items-center gap-1.5"
          >
            <Trash2 className="w-3.5 h-3.5" />
            <span>Xóa tất cả</span>
          </button>
        )}
      </div>

      {/* Cold Chain Banner */}
      {(cart?.hasColdChain || cart?.items?.some((i) => i.requiresColdChain)) && (
        <div className="p-4 rounded-2xl bg-cyan-50/90 border border-cyan-200 flex items-center gap-3 text-cyan-950 text-xs font-medium shadow-2xs">
          <ThermometerSnowflake className="w-5 h-5 text-cyan-600 shrink-0" />
          <span>
            <strong>Chuỗi cung ứng lạnh Solaris Cold-Chain:</strong> Giỏ hàng của bạn có thực phẩm tươi sống (thịt, cá, hải sản, rau củ). Đơn hàng sẽ được vận chuyển hỏa tốc bằng xe máy thùng lạnh chuyên dụng trong bán kính tối đa 15km để giữ trọn độ tươi ngon.
          </span>
        </div>
      )}

      {isLoading && (!cart || !cart.items || cart.items.length === 0) ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 lg:gap-10 items-start animate-pulse">
          <div className="lg:col-span-2 bg-white rounded-3xl border border-slate-200/80 p-6 space-y-6">
            <div className="h-20 bg-slate-100 rounded-2xl"></div>
            <div className="h-20 bg-slate-100 rounded-2xl"></div>
          </div>
          <div className="bg-white rounded-3xl border border-slate-200/80 p-6 space-y-4">
            <div className="h-6 w-1/2 bg-slate-100 rounded-xl"></div>
            <div className="h-24 bg-slate-100 rounded-2xl"></div>
            <div className="h-12 bg-slate-100 rounded-2xl"></div>
          </div>
        </div>
      ) : hasItems && cart && cart.items.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 lg:gap-10 items-start">
          {/* Left: Items List */}
          <div className="lg:col-span-2 bg-white rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] border border-slate-200/80 p-5 sm:p-7 space-y-5">
            <div className="divide-y divide-slate-100">
              {cart.items.map((item) => (
                <div
                  key={item.id}
                  className="py-5 first:pt-0 last:pb-0 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 group"
                >
                  <div className="flex items-center gap-4 flex-1">
                    <div className="w-18 h-18 rounded-2xl bg-slate-50 border border-slate-100 overflow-hidden shrink-0 flex items-center justify-center">
                      {item.imagePath ? (
                        <img
                          src={item.imagePath}
                          alt={item.variantName}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <div className="text-slate-300">
                          <ShoppingBag className="w-7 h-7" />
                        </div>
                      )}
                    </div>

                    <div className="space-y-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        <h3 className="font-bold text-sm text-slate-900 group-hover:text-emerald-700 transition-colors line-clamp-1">
                          {item.variantName}
                        </h3>
                        {item.requiresColdChain && (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-cyan-100 text-cyan-800 border border-cyan-200">
                            <ThermometerSnowflake className="w-2.5 h-2.5 text-cyan-600" />
                            Bảo quản lạnh 0-4°C
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-slate-500">
                        ĐVT:{" "}
                        <strong className="text-slate-700">
                          {item.uoMName}
                        </strong>
                        {item.origin && ` • Vùng trồng: ${item.origin}`}
                      </p>
                      <div className="flex items-baseline gap-2 pt-0.5">
                        <span className="font-black text-sm text-emerald-700">
                          {formatVND(item.unitPrice)}
                        </span>
                        {item.discountAmount > 0 && (
                          <span className="text-xs text-slate-400 line-through">
                            {formatVND(item.originalPrice)}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Controls & Total */}
                  <div className="flex items-center justify-between sm:justify-end gap-6 w-full sm:w-auto pt-3 sm:pt-0 border-t sm:border-t-0 border-slate-100">
                    {/* Quantity Counter */}
                    <div className="flex items-center h-10 rounded-xl border border-slate-200 bg-slate-50 overflow-hidden shadow-2xs">
                      <button
                        type="button"
                        onClick={() =>
                          updateQuantity(
                            item.id,
                            Math.max(1, item.quantity - 1),
                          )
                        }
                        className="px-3 h-full text-slate-600 hover:bg-white font-black text-xs transition-colors cursor-pointer"
                      >
                        -
                      </button>
                      <span className="px-3.5 h-full flex items-center justify-center text-xs font-black text-slate-900 min-w-[34px] text-center">
                        {item.quantity}
                      </span>
                      <button
                        type="button"
                        onClick={() =>
                          updateQuantity(item.id, item.quantity + 1)
                        }
                        className="px-3 h-full text-slate-600 hover:bg-white font-black text-xs transition-colors cursor-pointer"
                      >
                        +
                      </button>
                    </div>

                    {/* Line Total */}
                    <div className="text-right min-w-[100px]">
                      <span className="font-black text-sm sm:text-base text-slate-900">
                        {formatVND(item.totalPrice)}
                      </span>
                    </div>

                    {/* Remove Item */}
                    <button
                      type="button"
                      onClick={() => removeItem(item.id)}
                      className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors cursor-pointer"
                      title="Xóa khỏi giỏ hàng"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Right: Order Summary */}
          <div className="space-y-6">
            <div className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] p-6 sm:p-7 space-y-5">
              <h2 className="font-extrabold text-base text-slate-900 pb-3.5 border-b border-slate-100">
                Tóm Tắt Đơn Hàng
              </h2>

              <div className="space-y-3 text-xs">
                <div className="flex justify-between text-slate-600">
                  <span>Tạm tính tiền hàng:</span>
                  <span className="font-bold text-slate-900">
                    {formatVND(cart.subTotal)}
                  </span>
                </div>

                {cart.totalDiscount > 0 && (
                  <div className="flex justify-between text-rose-600 font-semibold">
                    <span className="flex items-center gap-1">
                      <Sparkles className="w-3.5 h-3.5" />
                      Giảm giá khuyến mãi:
                    </span>
                    <span className="font-bold">
                      -{formatVND(cart.totalDiscount)}
                    </span>
                  </div>
                )}

                <div className="flex justify-between text-slate-600">
                  <span>Phí giao hàng:</span>
                  <span className="font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md">
                    Tính tại bước thanh toán
                  </span>
                </div>

                <div className="pt-4 border-t border-slate-100 flex justify-between items-baseline">
                  <span className="font-extrabold text-slate-900 text-sm">
                    Tổng thanh toán:
                  </span>
                  <span className="font-black text-2xl text-emerald-800">
                    {formatVND(cart.estimatedTotal)}
                  </span>
                </div>
              </div>

              <button
                onClick={handleProceedCheckout}
                className="w-full h-12 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl font-extrabold text-xs transition-all shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 active:scale-98 cursor-pointer"
              >
                <span>Tiến Hành Đặt Hàng</span>
                <ArrowRight className="w-4 h-4" />
              </button>
            </div>

            {/* Badges */}
            <div className="p-5 bg-emerald-50/60 rounded-3xl border border-emerald-200/60 text-xs text-emerald-900 space-y-2.5">
              <div className="flex items-center gap-2.5 font-bold">
                <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Bảo toàn độ tươi ngon theo chuẩn FEFO</span>
              </div>
              <div className="flex items-center gap-2.5 font-bold">
                <Truck className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Giao hàng lạnh bảo quản 2H nội thành</span>
              </div>
              <div className="flex items-center gap-2.5 font-bold">
                <RotateCcw className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Đổi trả miễn phí trong 24h nếu lỗi dập nát</span>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <EmptyState
          icon={<ShoppingBag className="w-8 h-8" />}
          title="Giỏ hàng của bạn đang trống"
          description="Hãy dạo quanh nông trại Solaris để chọn những món rau củ quả tươi sạch cho gia đình nhé!"
          actionText="Khám phá sản phẩm ngay"
          actionHref="/san-pham"
          className="max-w-lg mx-auto"
        />
      )}
    </div>
  );
}
