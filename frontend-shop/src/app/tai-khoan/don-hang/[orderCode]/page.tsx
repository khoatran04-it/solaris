'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { 
    Package, 
    Calendar, 
    MapPin, 
    CreditCard, 
    ShieldCheck, 
    RotateCcw, 
    XCircle, 
    ArrowLeft,
    CheckCircle2
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import shopOrderApi from '@/api/shopOrderApi';
import { formatVND, formatDateTime } from '@/lib/utils';
import { ShopOrder } from '@/types/order';

export default function OrderDetailPage() {
    const params = useParams();
    const router = useRouter();
    const orderCode = params.orderCode as string;

    const { isAuthenticated, initAuth } = useAuthStore();
    const [order, setOrder] = useState<ShopOrder | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isCancelling, setIsCancelling] = useState(false);
    const [cancelReason, setCancelReason] = useState('');
    const [showCancelModal, setShowCancelModal] = useState(false);

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    useEffect(() => {
        if (!isAuthenticated) {
            router.push(`/dang-nhap?redirect=/tai-khoan/don-hang/${orderCode}`);
            return;
        }

        shopOrderApi.getByCode(orderCode)
            .then((res) => {
                setOrder(res);
                setIsLoading(false);
            })
            .catch(() => {
                setIsLoading(false);
            });
    }, [isAuthenticated, orderCode, router]);

    const handleCancelOrder = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!cancelReason.trim()) return;

        setIsCancelling(true);
        try {
            await shopOrderApi.cancel(orderCode, {
                reason: cancelReason.trim()
            });

            // Reload order
            const updated = await shopOrderApi.getByCode(orderCode);
            setOrder(updated);
            setShowCancelModal(false);
        } catch (error: any) {
            alert(error?.message || 'Không thể hủy đơn hàng.');
        } finally {
            setIsCancelling(false);
        }
    };

    if (isLoading) {
        return (
            <div className="max-w-7xl mx-auto px-4 py-16 text-center text-xs text-slate-500">
                Đang tải thông tin đơn hàng...
            </div>
        );
    }

    if (!order) {
        return (
            <div className="max-w-7xl mx-auto px-4 py-16 text-center space-y-4">
                <h3 className="text-base font-bold text-slate-800">Không tìm thấy đơn hàng</h3>
                <Link href="/tai-khoan/don-hang" className="inline-block px-5 py-2.5 bg-emerald-600 text-white text-xs font-bold rounded-full">
                    Quay lại danh sách đơn hàng
                </Link>
            </div>
        );
    }

    const canCancel = order.statusName === 'Chờ xác nhận' || order.statusName === 'Đã xác nhận';
    const canReturn = order.statusName === 'Giao thành công' || order.statusName === 'Đang giao hàng';

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Top Bar */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
                <div className="flex items-center gap-3">
                    <Link
                        href="/tai-khoan/don-hang"
                        className="p-2 bg-white border border-slate-200 hover:bg-slate-50 rounded-xl text-slate-700 transition-colors"
                    >
                        <ArrowLeft className="w-4 h-4" />
                    </Link>
                    <div>
                        <h1 className="text-xl sm:text-2xl font-black text-slate-900">
                            Đơn Hàng: {order.orderCode}
                        </h1>
                        <p className="text-xs text-slate-500 mt-0.5">
                            Ngày đặt: {formatDateTime(order.orderDate)}
                        </p>
                    </div>
                </div>

                <div className="flex items-center gap-3">
                    <span className="px-3.5 py-1.5 bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs font-bold rounded-full">
                        {order.statusName}
                    </span>

                    {canCancel && (
                        <button
                            onClick={() => setShowCancelModal(true)}
                            className="px-4 py-2 bg-rose-50 hover:bg-rose-100 text-rose-700 text-xs font-bold rounded-xl border border-rose-200 transition-colors"
                        >
                            Hủy đơn hàng
                        </button>
                    )}

                    {canReturn && (
                        <Link
                            href={`/tai-khoan/tra-hang?orderCode=${order.orderCode}`}
                            className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition-colors flex items-center gap-1.5"
                        >
                            <RotateCcw className="w-3.5 h-3.5" />
                            <span>Yêu cầu đổi/trả</span>
                        </Link>
                    )}
                </div>
            </div>

            {order.cancellationReason && (
                <div className="p-4 bg-rose-50 border border-rose-200 rounded-2xl text-xs text-rose-800 space-y-1">
                    <p className="font-bold">Lý do hủy đơn:</p>
                    <p>{order.cancellationReason}</p>
                </div>
            )}

            {/* Content Columns */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
                
                {/* Items List */}
                <div className="lg:col-span-2 space-y-6">
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-4">
                        <h2 className="text-sm font-bold text-slate-900 pb-3 border-b border-slate-100 flex items-center gap-2">
                            <Package className="w-4 h-4 text-emerald-600" />
                            Chi Tiết Mặt Hàng ({order.items.length})
                        </h2>

                        <div className="divide-y divide-slate-100">
                            {order.items.map((item) => (
                                <div key={item.detailId} className="py-4 flex items-center justify-between gap-4">
                                    <div className="space-y-1 flex-1">
                                        <p className="font-bold text-xs text-slate-900">{item.variantName}</p>
                                        <p className="text-[11px] text-slate-500">
                                            Đơn giá: {formatVND(item.unitPrice)} / {item.uoMName}
                                        </p>
                                    </div>

                                    <div className="text-right">
                                        <p className="text-xs text-slate-600 font-semibold">x {item.quantity}</p>
                                        <p className="font-bold text-xs text-slate-900 mt-0.5">{formatVND(item.totalPrice)}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Receiver Info & Summary */}
                <div className="space-y-6">
                    {/* Receiver */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-3 text-xs">
                        <h3 className="font-bold text-slate-900 pb-2 border-b border-slate-100 flex items-center gap-2">
                            <MapPin className="w-4 h-4 text-emerald-600" />
                            Thông Tin Người Nhận
                        </h3>
                        <p className="font-bold text-slate-800">{order.receiverName} • {order.receiverPhone}</p>
                        <p className="text-slate-600 leading-relaxed">{order.deliveryAddress}</p>
                        {order.note && (
                            <p className="text-[11px] text-slate-500 italic pt-1">Ghi chú: {order.note}</p>
                        )}
                    </div>

                    {/* Financial Summary */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-3 text-xs">
                        <h3 className="font-bold text-slate-900 pb-2 border-b border-slate-100">
                            Chi Tiết Thanh Toán
                        </h3>
                        <div className="flex justify-between text-slate-600">
                            <span>Tạm tính:</span>
                            <span className="font-semibold text-slate-900">{formatVND(order.subTotal)}</span>
                        </div>
                        {order.discountAmount > 0 && (
                            <div className="flex justify-between text-rose-600">
                                <span>Giảm giá:</span>
                                <span className="font-bold">-{formatVND(order.discountAmount)}</span>
                            </div>
                        )}
                        <div className="flex justify-between text-slate-600">
                            <span>Phí giao hàng:</span>
                            <span className="font-semibold text-slate-900">{formatVND(order.shippingFee)}</span>
                        </div>
                        <div className="pt-2 border-t border-slate-100 flex justify-between items-baseline">
                            <span className="font-bold text-slate-800 text-sm">Tổng cộng:</span>
                            <span className="font-black text-lg text-emerald-700">{formatVND(order.totalAmount)}</span>
                        </div>
                        <div className="pt-2 border-t border-slate-100 text-[11px] text-slate-500">
                            Hình thức: <strong>{order.paymentMethodName}</strong> ({order.paymentStatusName})
                        </div>
                    </div>
                </div>

            </div>

            {/* Cancel Modal */}
            {showCancelModal && (
                <div className="fixed inset-0 z-50 bg-slate-950/50 backdrop-blur-xs flex items-center justify-center p-4">
                    <div className="bg-white rounded-3xl max-w-md w-full p-6 space-y-4 shadow-2xl border border-slate-100 animate-in fade-in zoom-in-95 duration-150">
                        <h3 className="font-bold text-sm text-slate-900">Xác Nhận Hủy Đơn Hàng</h3>
                        <p className="text-xs text-slate-600">
                            Lô hàng đã được giữ chỗ sẽ được hoàn trả lại tồn kho khả dụng cho khách hàng khác.
                        </p>
                        <form onSubmit={handleCancelOrder} className="space-y-4">
                            <textarea
                                placeholder="Vui lòng nhập lý do hủy đơn..."
                                value={cancelReason}
                                onChange={(e) => setCancelReason(e.target.value)}
                                className="w-full text-xs p-3 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-rose-500 h-24"
                                required
                            />
                            <div className="flex items-center justify-end gap-2">
                                <button
                                    type="button"
                                    onClick={() => setShowCancelModal(false)}
                                    className="px-4 py-2 bg-slate-100 text-slate-700 text-xs font-semibold rounded-xl"
                                >
                                    Đóng
                                </button>
                                <button
                                    type="submit"
                                    disabled={isCancelling}
                                    className="px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold rounded-xl transition-colors"
                                >
                                    {isCancelling ? 'Đang hủy...' : 'Đồng Ý Hủy'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

        </div>
    );
}


