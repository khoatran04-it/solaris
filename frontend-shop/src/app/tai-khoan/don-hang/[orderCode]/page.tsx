'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { 
    Package, 
    MapPin, 
    RotateCcw, 
    ArrowLeft,
    CheckCircle2,
    CreditCard,
    X
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import shopOrderApi from '@/api/shopOrderApi';
import { formatVND, formatDateTime } from '@/lib/utils';
import { ShopOrder } from '@/types/order';
import EmptyState from '@/components/common/EmptyState';

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
    const [showConfirmDeliveryModal, setShowConfirmDeliveryModal] = useState(false);
    const [isConfirmingDelivery, setIsConfirmingDelivery] = useState(false);

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

    const handleConfirmDelivery = async () => {
        setIsConfirmingDelivery(true);
        try {
            const updated = await shopOrderApi.confirmDelivery(orderCode);
            setOrder(updated);
            setShowConfirmDeliveryModal(false);
        } catch (error: any) {
            alert(error?.message || 'Không thể xác nhận nhận hàng.');
        } finally {
            setIsConfirmingDelivery(false);
        }
    };

    if (isLoading) {
        return (
            <div className="max-w-7xl mx-auto px-4 py-16 text-center text-xs font-semibold text-slate-500">
                Đang tải thông tin đơn hàng...
            </div>
        );
    }

    if (!order) {
        return (
            <div className="max-w-7xl mx-auto px-4 py-16">
                <EmptyState
                    icon={<Package className="w-8 h-8" />}
                    title="Không tìm thấy đơn hàng"
                    description="Đơn hàng không tồn tại hoặc bạn không có quyền truy cập."
                    actionText="Quay lại danh sách đơn hàng"
                    actionHref="/tai-khoan/don-hang"
                />
            </div>
        );
    }

    const canCancel = order.statusName === 'Chờ xác nhận' || order.statusName === 'Đã xác nhận';
    const canConfirmDelivery = order.statusName === 'Đang giao hàng' || order.statusName === 'Đang chuẩn bị hàng';
    const canReturn = order.statusName === 'Giao thành công';

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Top Bar */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-5 border-b border-slate-200">
                <div className="flex items-center gap-3">
                    <Link
                        href="/tai-khoan/don-hang"
                        className="p-2.5 bg-white border border-slate-200 hover:bg-slate-50 rounded-2xl text-slate-700 transition-colors shadow-2xs"
                    >
                        <ArrowLeft className="w-4 h-4" />
                    </Link>
                    <div>
                        <h1 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                            Đơn Hàng #{order.orderCode}
                        </h1>
                        <p className="text-xs text-slate-500 mt-0.5 font-medium">
                            Ngày đặt: {formatDateTime(order.orderDate)}
                        </p>
                    </div>
                </div>

                <div className="flex items-center gap-2.5 flex-wrap">
                    <span className="px-3.5 py-1.5 bg-emerald-50 text-emerald-800 border border-emerald-200/80 text-xs font-bold rounded-full">
                        {order.statusName}
                    </span>

                    {canCancel && (
                        <button
                            onClick={() => setShowCancelModal(true)}
                            className="px-4 py-2 bg-rose-50 hover:bg-rose-100 text-rose-700 text-xs font-bold rounded-xl border border-rose-200 transition-colors cursor-pointer"
                        >
                            Hủy đơn hàng
                        </button>
                    )}

                    {canConfirmDelivery && (
                        <button
                            onClick={() => setShowConfirmDeliveryModal(true)}
                            className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition-all shadow-md shadow-emerald-600/20 flex items-center gap-1.5 active:scale-95 cursor-pointer"
                        >
                            <CheckCircle2 className="w-3.5 h-3.5" />
                            <span>Đã nhận được hàng</span>
                        </button>
                    )}

                    {canReturn && (
                        <Link
                            href={`/tai-khoan/tra-hang?orderCode=${order.orderCode}`}
                            className="inline-flex items-center gap-1.5 px-4 py-2 bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 text-xs font-bold rounded-xl transition-colors"
                        >
                            <RotateCcw className="w-3.5 h-3.5" />
                            <span>Yêu cầu đổi trả (RMA)</span>
                        </Link>
                    )}
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
                
                {/* Left (7 Cols): Items list */}
                <div className="lg:col-span-7 space-y-6">
                    <div className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] p-6 sm:p-7 space-y-5">
                        <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider pb-3.5 border-b border-slate-100 flex items-center gap-2">
                            <span className="w-1.5 h-4 bg-emerald-600 rounded-full"></span>
                            Danh Sách Nông Sản Trong Đơn
                        </h2>

                        <div className="divide-y divide-slate-100">
                            {order.items.map((item) => (
                                <div key={item.detailId} className="py-4 first:pt-0 last:pb-0 flex items-center justify-between gap-4">
                                    <div className="flex items-center gap-3.5 flex-1 truncate">
                                        <div className="w-14 h-14 rounded-2xl bg-slate-50 border border-slate-100 flex items-center justify-center shrink-0">
                                            {item.imagePath ? (
                                                <img src={item.imagePath} alt={item.variantName} className="w-full h-full object-cover rounded-2xl" />
                                            ) : (
                                                <Package className="w-6 h-6 text-slate-300" />
                                            )}
                                        </div>
                                        <div className="truncate">
                                            <p className="font-bold text-xs text-slate-900 truncate">{item.variantName}</p>
                                            <p className="text-[11px] text-slate-400 mt-0.5 font-medium">
                                                ĐVT: <strong>{item.uoMName}</strong> • {item.quantity} x {formatVND(item.unitPrice)}
                                            </p>
                                        </div>
                                    </div>

                                    <div className="text-right shrink-0">
                                        <span className="font-black text-sm text-slate-900">{formatVND(item.totalPrice)}</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Right (5 Cols): Address & Summary */}
                <div className="lg:col-span-5 space-y-6">
                    {/* Delivery Details */}
                    <div className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] p-6 sm:p-7 space-y-4">
                        <h3 className="font-extrabold text-xs text-slate-900 uppercase tracking-wider pb-3.5 border-b border-slate-100 flex items-center gap-2">
                            <MapPin className="w-4 h-4 text-emerald-600" />
                            Thông Tin Giao Hàng
                        </h3>

                        <div className="space-y-2 text-xs text-slate-600">
                            <p className="font-bold text-slate-900">
                                {order.receiverName} • {order.receiverPhone}
                            </p>
                            <p className="leading-relaxed text-slate-500">
                                {order.deliveryAddress}
                            </p>
                            {order.note && (
                                <p className="pt-2 text-slate-500 italic">
                                    Ghi chú: &quot;{order.note}&quot;
                                </p>
                            )}
                        </div>
                    </div>

                    {/* Order Financial Summary */}
                    <div className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] p-6 sm:p-7 space-y-4">
                        <h3 className="font-extrabold text-xs text-slate-900 uppercase tracking-wider pb-3.5 border-b border-slate-100 flex items-center gap-2">
                            <CreditCard className="w-4 h-4 text-emerald-600" />
                            Thanh Toán & Cước Vận Chuyển
                        </h3>

                        <div className="space-y-2.5 text-xs">
                            <div className="flex justify-between text-slate-600">
                                <span>Tiền hàng tạm tính:</span>
                                <span className="font-bold text-slate-900">{formatVND(order.subTotal)}</span>
                            </div>

                            {order.discountAmount > 0 && (
                                <div className="flex justify-between text-rose-600 font-semibold">
                                    <span>Chiết khấu khuyến mãi:</span>
                                    <span className="font-bold">-{formatVND(order.discountAmount)}</span>
                                </div>
                            )}

                            <div className="flex justify-between text-slate-600">
                                <span>Phí giao hàng:</span>
                                <span className="font-bold text-slate-900">{formatVND(order.shippingFee)}</span>
                            </div>

                            <div className="flex justify-between text-slate-600">
                                <span>Phương thức thanh toán:</span>
                                <span className="font-bold text-slate-900">{order.paymentMethodName}</span>
                            </div>

                            <div className="flex justify-between text-slate-600">
                                <span>Trạng thái thanh toán:</span>
                                <span className="font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md">
                                    {order.paymentStatusName}
                                </span>
                            </div>

                            <div className="pt-3.5 border-t border-slate-100 flex justify-between items-baseline">
                                <span className="font-extrabold text-slate-900 text-sm">Tổng thanh toán:</span>
                                <span className="font-black text-xl text-emerald-800">{formatVND(order.totalAmount)}</span>
                            </div>
                        </div>
                    </div>
                </div>

            </div>

            {/* Cancel Modal */}
            {showCancelModal && (
                <div className="fixed inset-0 bg-slate-950/40 backdrop-blur-xs z-50 flex items-center justify-center p-4">
                    <div className="bg-white rounded-3xl p-6 sm:p-7 max-w-md w-full shadow-2xl space-y-4 animate-in fade-in zoom-in-95">
                        <div className="flex items-center justify-between">
                            <h3 className="text-base font-black text-slate-900">Xác Nhận Hủy Đơn Hàng</h3>
                            <button onClick={() => setShowCancelModal(false)} className="p-1 text-slate-400 hover:text-slate-600 rounded-lg">
                                <X className="w-5 h-5" />
                            </button>
                        </div>
                        <p className="text-xs text-slate-500 leading-relaxed">
                            Vui lòng cho Solaris biết lý do bạn muốn hủy đơn hàng này để chúng tôi cải thiện chất lượng phục vụ:
                        </p>
                        <form onSubmit={handleCancelOrder} className="space-y-4">
                            <textarea
                                value={cancelReason}
                                onChange={(e) => setCancelReason(e.target.value)}
                                placeholder="Nhập lý do hủy đơn (ví dụ: Thay đổi địa chỉ, Đặt nhầm sản phẩm...)"
                                className="w-full p-3 bg-slate-50 border border-slate-200 rounded-2xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                                rows={3}
                                required
                            />
                            <div className="flex justify-end gap-2.5">
                                <button
                                    type="button"
                                    onClick={() => setShowCancelModal(false)}
                                    className="px-4 py-2.5 bg-slate-100 text-slate-700 font-bold text-xs rounded-xl"
                                >
                                    Đóng
                                </button>
                                <button
                                    type="submit"
                                    disabled={isCancelling}
                                    className="px-5 py-2.5 bg-rose-600 hover:bg-rose-700 text-white font-bold text-xs rounded-xl transition-all shadow-md shadow-rose-600/20 disabled:opacity-50"
                                >
                                    {isCancelling ? 'Đang hủy...' : 'Xác Nhận Hủy Đơn'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {/* Confirm Delivery Modal */}
            {showConfirmDeliveryModal && (
                <div className="fixed inset-0 bg-slate-950/40 backdrop-blur-xs z-50 flex items-center justify-center p-4">
                    <div className="bg-white rounded-3xl p-6 sm:p-7 max-w-md w-full shadow-2xl space-y-4 animate-in fade-in zoom-in-95 text-center">
                        <div className="w-14 h-14 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto">
                            <CheckCircle2 className="w-8 h-8" />
                        </div>
                        <div className="space-y-1">
                            <h3 className="text-lg font-black text-slate-900">Xác Nhận Đã Nhận Hàng?</h3>
                            <p className="text-xs text-slate-500 leading-relaxed">
                                Bạn xác nhận đã nhận đầy đủ nông sản tươi sạch từ đơn hàng #{order.orderCode} và hài lòng với chất lượng?
                            </p>
                        </div>
                        <div className="flex justify-center gap-3 pt-2">
                            <button
                                type="button"
                                onClick={() => setShowConfirmDeliveryModal(false)}
                                className="px-5 py-2.5 bg-slate-100 text-slate-700 font-bold text-xs rounded-xl"
                            >
                                Chưa nhận được
                            </button>
                            <button
                                type="button"
                                onClick={handleConfirmDelivery}
                                disabled={isConfirmingDelivery}
                                className="px-6 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs rounded-xl transition-all shadow-md shadow-emerald-600/20 disabled:opacity-50"
                            >
                                {isConfirmingDelivery ? 'Đang cập nhật...' : 'Đã Nhận Hàng Thành Công'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

        </div>
    );
}
