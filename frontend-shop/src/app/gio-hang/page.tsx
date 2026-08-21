'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { ShoppingBag, Trash2, ArrowRight, ShieldCheck, Truck, RotateCcw, AlertTriangle } from 'lucide-react';
import { useCartStore } from '@/stores/cartStore';
import { useAuthStore } from '@/stores/authStore';
import { formatVND } from '@/lib/utils';

export default function GioHangPage() {
    const router = useRouter();
    const { isAuthenticated } = useAuthStore();
    const { cart, guestItems, totalCount, fetchCart, updateQuantity, removeItem, clearCart, isLoading } = useCartStore();

    useEffect(() => {
        fetchCart();
    }, [fetchCart]);

    const handleProceedCheckout = () => {
        if (!isAuthenticated) {
            // Yêu cầu đăng nhập trước khi checkout
            router.push('/dang-nhap?redirect=/thanh-toan');
        } else {
            router.push('/thanh-toan');
        }
    };

    const hasItems = (cart && cart.items.length > 0) || guestItems.length > 0;

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Header */}
            <div className="flex items-center justify-between pb-4 border-b border-slate-200">
                <div>
                    <h1 className="text-2xl sm:text-3xl font-black text-slate-900 flex items-center gap-2">
                        <ShoppingBag className="w-7 h-7 text-emerald-600" />
                        Giỏ Hàng Của Bạn
                    </h1>
                    <p className="text-xs text-slate-500 mt-1">
                        {totalCount} mặt hàng trong giỏ
                    </p>
                </div>

                {hasItems && (
                    <button
                        onClick={() => {
                            if (confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ?')) {
                                clearCart();
                            }
                        }}
                        className="text-xs font-semibold text-rose-600 hover:text-rose-700 flex items-center gap-1"
                    >
                        <Trash2 className="w-3.5 h-3.5" />
                        Xóa tất cả
                    </button>
                )}
            </div>

            {hasItems && cart ? (
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
                    
                    {/* Left: Items List */}
                    <div className="lg:col-span-2 space-y-4">
                        {cart.items.map((item) => (
                            <div
                                key={item.id}
                                className="bg-white rounded-2xl border border-slate-200 p-4 sm:p-5 shadow-xs flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 group"
                            >
                                <div className="flex items-center gap-4 flex-1">
                                    <div className="w-16 h-16 rounded-xl bg-slate-50 border border-slate-100 overflow-hidden shrink-0 flex items-center justify-center">
                                        {item.imagePath ? (
                                            <img src={item.imagePath} alt={item.variantName} className="w-full h-full object-cover" />
                                        ) : (
                                            <span className="text-2xl">🥑</span>
                                        )}
                                    </div>

                                    <div className="space-y-1">
                                        <h3 className="font-bold text-sm text-slate-900 group-hover:text-emerald-600 transition-colors">
                                            {item.variantName}
                                        </h3>
                                        <p className="text-[11px] text-slate-500">
                                            ĐVT: <strong className="text-slate-700">{item.uoMName}</strong>
                                            {item.origin && ` • Xuất xứ: ${item.origin}`}
                                        </p>
                                        <div className="flex items-baseline gap-2">
                                            <span className="font-bold text-xs text-emerald-700">
                                                {formatVND(item.unitPrice)}
                                            </span>
                                            {item.discountAmount > 0 && (
                                                <span className="text-[10px] text-slate-400 line-through">
                                                    {formatVND(item.originalPrice)}
                                                </span>
                                            )}
                                        </div>
                                    </div>
                                </div>

                                {/* Controls & Total */}
                                <div className="flex items-center justify-between sm:justify-end gap-6 w-full sm:w-auto pt-3 sm:pt-0 border-t sm:border-t-0 border-slate-100">
                                    {/* Quantity Buttons */}
                                    <div className="flex items-center border border-slate-200 rounded-lg bg-white overflow-hidden">
                                        <button
                                            onClick={() => updateQuantity(item.id, Math.max(1, item.quantity - 1))}
                                            className="px-2.5 py-1 text-slate-600 hover:bg-slate-100 font-bold text-xs"
                                        >
                                            -
                                        </button>
                                        <span className="px-3 py-1 text-xs font-bold text-slate-800 min-w-[32px] text-center">
                                            {item.quantity}
                                        </span>
                                        <button
                                            onClick={() => updateQuantity(item.id, item.quantity + 1)}
                                            className="px-2.5 py-1 text-slate-600 hover:bg-slate-100 font-bold text-xs"
                                        >
                                            +
                                        </button>
                                    </div>

                                    {/* Line Total */}
                                    <div className="text-right min-w-[90px]">
                                        <span className="font-black text-sm text-slate-900">
                                            {formatVND(item.totalPrice)}
                                        </span>
                                    </div>

                                    {/* Remove Item */}
                                    <button
                                        onClick={() => removeItem(item.id)}
                                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                                        title="Xóa khỏi giỏ"
                                    >
                                        <Trash2 className="w-4 h-4" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Right: Order Summary */}
                    <div className="space-y-6">
                        <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-4">
                            <h2 className="font-bold text-sm text-slate-900 pb-3 border-b border-slate-100">
                                Tóm Tắt Đơn Hàng
                            </h2>

                            <div className="space-y-2 text-xs">
                                <div className="flex justify-between text-slate-600">
                                    <span>Tạm tính:</span>
                                    <span className="font-semibold text-slate-900">{formatVND(cart.subTotal)}</span>
                                </div>

                                {cart.totalDiscount > 0 && (
                                    <div className="flex justify-between text-rose-600">
                                        <span>Giảm giá khuyến mãi:</span>
                                        <span className="font-bold">-{formatVND(cart.totalDiscount)}</span>
                                    </div>
                                )}

                                <div className="flex justify-between text-slate-600">
                                    <span>Phí giao hàng:</span>
                                    <span className="font-semibold text-emerald-600">Tính khi thanh toán</span>
                                </div>

                                <div className="pt-3 border-t border-slate-100 flex justify-between items-baseline">
                                    <span className="font-bold text-slate-800 text-sm">Tổng cộng:</span>
                                    <span className="font-black text-xl text-emerald-700">{formatVND(cart.estimatedTotal)}</span>
                                </div>
                            </div>

                            <button
                                onClick={handleProceedCheckout}
                                className="w-full py-3.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-2xl text-xs font-bold transition-all shadow-lg shadow-emerald-600/30 flex items-center justify-center gap-2 active:scale-98"
                            >
                                <span>Tiến Hành Đặt Hàng</span>
                                <ArrowRight className="w-4 h-4" />
                            </button>
                        </div>

                        {/* Badges */}
                        <div className="p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100 text-xs text-emerald-800 space-y-2">
                            <div className="flex items-center gap-2 font-semibold">
                                <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                                <span>Bảo toàn độ tươi ngon theo chuẩn FEFO</span>
                            </div>
                            <div className="flex items-center gap-2 font-semibold">
                                <Truck className="w-4 h-4 text-emerald-600 shrink-0" />
                                <span>Giao hàng lạnh 2H nội thành</span>
                            </div>
                        </div>
                    </div>

                </div>
            ) : (
                <div className="bg-white rounded-3xl border border-slate-200 p-16 text-center space-y-4 max-w-lg mx-auto">
                    <div className="w-20 h-20 rounded-full bg-emerald-50 flex items-center justify-center text-4xl mx-auto">
                        🛒
                    </div>
                    <h3 className="text-lg font-bold text-slate-800">Giỏ hàng của bạn đang trống</h3>
                    <p className="text-xs text-slate-500 leading-relaxed">
                        Hãy dạo một vòng quanh nông trại Solaris để chọn những món nông sản tươi ngon nhất cho gia đình nhé!
                    </p>
                    <Link
                        href="/san-pham"
                        className="inline-flex items-center gap-2 px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-full transition-colors shadow-md"
                    >
                        <span>Khám phá sản phẩm ngay</span>
                        <ArrowRight className="w-3.5 h-3.5" />
                    </Link>
                </div>
            )}

        </div>
    );
}

