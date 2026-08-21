'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { 
    MapPin, 
    CreditCard, 
    Truck, 
    ShieldCheck, 
    CheckCircle2, 
    ShoppingBag, 
    ArrowRight,
    AlertCircle
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import shopOrderApi from '@/api/shopOrderApi';
import shopCustomerApi from '@/api/shopCustomerApi';
import { formatVND } from '@/lib/utils';
import { ShopAddress } from '@/types/customer';
import { ShopOrder, ShopCheckoutPayload } from '@/types/order';

export default function ThanhToanPage() {
    const router = useRouter();
    const { user, isAuthenticated, initAuth } = useAuthStore();
    const { cart, fetchCart, clearCart } = useCartStore();

    const [addresses, setAddresses] = useState<ShopAddress[]>([]);
    const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);

    // New address form
    const [useNewAddress, setUseNewAddress] = useState(false);
    const [receiverName, setReceiverName] = useState('');
    const [receiverPhone, setReceiverPhone] = useState('');
    const [province, setProvince] = useState('TP. Hồ Chí Minh');
    const [district, setDistrict] = useState('');
    const [ward, setWard] = useState('');
    const [streetAddress, setStreetAddress] = useState('');
    const [note, setNote] = useState('');

    const [paymentMethod, setPaymentMethod] = useState<number>(0); // 0 = COD, 1 = BankTransfer
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [errorMessage, setErrorMessage] = useState('');

    useEffect(() => {
        initAuth();
        fetchCart();
    }, [initAuth, fetchCart]);

    useEffect(() => {
        if (isAuthenticated) {
            shopCustomerApi.getAddresses()
                .then((addrs: ShopAddress[]) => {
                    setAddresses(addrs);
                    const defaultAddr = addrs.find((a: ShopAddress) => a.isDefault) || addrs[0];
                    if (defaultAddr) {
                        setSelectedAddressId(defaultAddr.id);
                    } else {
                        setUseNewAddress(true);
                    }
                })
                .catch(() => {
                    setUseNewAddress(true);
                });
        }
    }, [isAuthenticated]);

    const handleCheckout = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrorMessage('');

        if (!cart || cart.items.length === 0) {
            setErrorMessage('Giỏ hàng của bạn đang trống.');
            return;
        }

        if (!useNewAddress && !selectedAddressId) {
            setErrorMessage('Vui lòng chọn hoặc thêm địa chỉ nhận hàng.');
            return;
        }

        if (useNewAddress) {
            if (!receiverName.trim() || !receiverPhone.trim() || !streetAddress.trim()) {
                setErrorMessage('Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ nhận hàng.');
                return;
            }
        }

        setIsSubmitting(true);

        try {
            const payload: ShopCheckoutPayload = {
                customerAddressId: useNewAddress ? undefined : (selectedAddressId ?? undefined),
                receiverName: useNewAddress ? receiverName.trim() : undefined,
                receiverPhone: useNewAddress ? receiverPhone.trim() : undefined,
                province: useNewAddress ? province : undefined,
                district: useNewAddress ? district : undefined,
                ward: useNewAddress ? ward : undefined,
                streetAddress: useNewAddress ? streetAddress.trim() : undefined,
                latitude: 10.7769,
                longitude: 106.7009,
                paymentMethod: paymentMethod,
                note: note.trim()
            };

            const order: ShopOrder = await shopOrderApi.checkout(payload);
            
            // Xóa giỏ hàng trên client
            await clearCart();

            // Redirect tới chi tiết đơn hàng
            router.push(`/tai-khoan/don-hang/${order.orderCode}`);
        } catch (error: any) {
            const msg = error?.message || error?.Message || error?.details || error?.Details || error?.title || (typeof error === 'string' ? error : 'Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.');
            setErrorMessage(msg);
        } finally {
            setIsSubmitting(false);
        }
    };

    if (!cart || cart.items.length === 0) {
        return (
            <div className="max-w-7xl mx-auto px-4 py-16 text-center space-y-4">
                <p className="text-sm text-slate-500">Giỏ hàng đang trống.</p>
                <Link href="/san-pham" className="inline-block px-5 py-2.5 bg-emerald-600 text-white text-xs font-bold rounded-full">
                    Tiếp tục mua hàng
                </Link>
            </div>
        );
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Header */}
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Xác Nhận & Thanh Toán
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Vui lòng kiểm tra kỹ địa chỉ nhận hàng và phương thức thanh toán
                </p>
            </div>

            {errorMessage && (
                <div className="p-4 bg-rose-50 border border-rose-200 rounded-2xl flex items-center gap-3 text-rose-700 text-xs font-medium">
                    <AlertCircle className="w-5 h-5 shrink-0" />
                    <span>{errorMessage}</span>
                </div>
            )}

            <form onSubmit={handleCheckout} className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
                
                {/* Left (2 Columns): Delivery Info & Payment */}
                <div className="lg:col-span-2 space-y-6">
                    
                    {/* 1. Sổ Địa Chỉ Giao Hàng */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs space-y-5">
                        <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                            <h2 className="text-sm font-bold text-slate-900 flex items-center gap-2">
                                <MapPin className="w-4 h-4 text-emerald-600" />
                                1. Địa Chỉ Nhận Hàng
                            </h2>

                            {addresses.length > 0 && (
                                <button
                                    type="button"
                                    onClick={() => setUseNewAddress(!useNewAddress)}
                                    className="text-xs font-bold text-emerald-700 hover:text-emerald-800"
                                >
                                    {useNewAddress ? 'Chọn địa chỉ có sẵn' : '+ Thêm địa chỉ mới'}
                                </button>
                            )}
                        </div>

                        {/* List Saved Addresses */}
                        {!useNewAddress && addresses.length > 0 ? (
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                                {addresses.map((addr) => (
                                    <div
                                        key={addr.id}
                                        onClick={() => setSelectedAddressId(addr.id)}
                                        className={`p-4 rounded-2xl border cursor-pointer transition-all ${
                                            selectedAddressId === addr.id
                                                ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                                : 'border-slate-200 hover:border-slate-300'
                                        }`}
                                    >
                                        <div className="flex items-start justify-between">
                                            <div className="space-y-1">
                                                <p className="font-bold text-xs text-slate-900 flex items-center gap-1.5">
                                                    {addr.receiverName} • {addr.phone}
                                                    {addr.isDefault && (
                                                        <span className="px-1.5 py-0.2 bg-emerald-100 text-emerald-800 text-[9px] font-bold rounded">
                                                            Mặc định
                                                        </span>
                                                    )}
                                                </p>
                                                <p className="text-xs text-slate-600 leading-relaxed">
                                                    {addr.streetAddress}, {addr.ward}, {addr.district}, {addr.province}
                                                </p>
                                            </div>
                                            {selectedAddressId === addr.id && (
                                                <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0" />
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            /* New Address Form */
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Họ tên người nhận *</label>
                                    <input
                                        type="text"
                                        placeholder="Nguyễn Văn A"
                                        value={receiverName}
                                        onChange={(e) => setReceiverName(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                        required={useNewAddress}
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Số điện thoại nhận hàng *</label>
                                    <input
                                        type="tel"
                                        placeholder="0912345678"
                                        value={receiverPhone}
                                        onChange={(e) => setReceiverPhone(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                        required={useNewAddress}
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Tỉnh / Thành phố *</label>
                                    <input
                                        type="text"
                                        value={province}
                                        onChange={(e) => setProvince(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required={useNewAddress}
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Quận / Huyện *</label>
                                    <input
                                        type="text"
                                        placeholder="Quận 1 / TP. Thủ Đức"
                                        value={district}
                                        onChange={(e) => setDistrict(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required={useNewAddress}
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Phường / Xã</label>
                                    <input
                                        type="text"
                                        placeholder="Phường Bến Nghé"
                                        value={ward}
                                        onChange={(e) => setWard(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                    />
                                </div>

                                <div className="space-y-1 sm:col-span-2">
                                    <label className="text-xs font-semibold text-slate-700">Địa chỉ cụ thể (Số nhà, tên đường) *</label>
                                    <input
                                        type="text"
                                        placeholder="123 Đường Lê Lợi"
                                        value={streetAddress}
                                        onChange={(e) => setStreetAddress(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required={useNewAddress}
                                    />
                                </div>
                            </div>
                        )}

                        <div className="space-y-1 pt-2">
                            <label className="text-xs font-semibold text-slate-700">Ghi chú đơn hàng (Tùy chọn)</label>
                            <input
                                type="text"
                                placeholder="Ví dụ: Giao vào giờ hành chính, gọi trước khi đến..."
                                value={note}
                                onChange={(e) => setNote(e.target.value)}
                                className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                            />
                        </div>
                    </div>

                    {/* 2. Phương Thức Thanh Toán */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs space-y-4">
                        <h2 className="text-sm font-bold text-slate-900 flex items-center gap-2 pb-3 border-b border-slate-100">
                            <CreditCard className="w-4 h-4 text-emerald-600" />
                            2. Phương Thức Thanh Toán
                        </h2>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                            <div
                                onClick={() => setPaymentMethod(0)}
                                className={`p-4 rounded-2xl border cursor-pointer transition-all flex items-center justify-between ${
                                    paymentMethod === 0
                                        ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                        : 'border-slate-200 hover:border-slate-300'
                                }`}
                            >
                                <div className="flex items-center gap-3">
                                    <div className="w-9 h-9 rounded-xl bg-emerald-100 text-emerald-800 flex items-center justify-center font-bold text-sm">
                                        💵
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-xs text-slate-900">Thanh toán khi nhận (COD)</h4>
                                        <p className="text-[10px] text-slate-500">Kiểm hàng trước khi thanh toán</p>
                                    </div>
                                </div>
                                {paymentMethod === 0 && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
                            </div>

                            <div
                                onClick={() => setPaymentMethod(1)}
                                className={`p-4 rounded-2xl border cursor-pointer transition-all flex items-center justify-between ${
                                    paymentMethod === 1
                                        ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                        : 'border-slate-200 hover:border-slate-300'
                                }`}
                            >
                                <div className="flex items-center gap-3">
                                    <div className="w-9 h-9 rounded-xl bg-blue-100 text-blue-800 flex items-center justify-center font-bold text-sm">
                                        🏦
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-xs text-slate-900">Chuyển khoản VietQR</h4>
                                        <p className="text-[10px] text-slate-500">Quét mã QR qua Mobile Banking</p>
                                    </div>
                                </div>
                                {paymentMethod === 1 && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
                            </div>
                        </div>

                        <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-[11px] text-amber-800">
                            💡 Cổng thanh toán trực tuyến tự động <strong>VNPay Gateway</strong> sẽ được tích hợp hoàn thiện trong <strong>Phase 2</strong>.
                        </div>
                    </div>

                </div>

                {/* Right: Cart Summary & Submit CTA */}
                <div className="space-y-6">
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-4">
                        <h3 className="font-bold text-sm text-slate-900 pb-3 border-b border-slate-100">
                            Đơn Hàng ({cart.items.length} món)
                        </h3>

                        {/* List Items mini */}
                        <div className="space-y-3 max-h-60 overflow-y-auto pr-1">
                            {cart.items.map((item) => (
                                <div key={item.id} className="flex items-center justify-between text-xs gap-3">
                                    <div className="flex-1 truncate">
                                        <p className="font-bold text-slate-800 truncate">{item.variantName}</p>
                                        <span className="text-[10px] text-slate-500">
                                            {item.quantity} x {formatVND(item.unitPrice)}
                                        </span>
                                    </div>
                                    <span className="font-bold text-slate-900 shrink-0">
                                        {formatVND(item.totalPrice)}
                                    </span>
                                </div>
                            ))}
                        </div>

                        <div className="pt-3 border-t border-slate-100 space-y-2 text-xs">
                            <div className="flex justify-between text-slate-600">
                                <span>Tạm tính:</span>
                                <span className="font-semibold text-slate-900">{formatVND(cart.subTotal)}</span>
                            </div>

                            {cart.totalDiscount > 0 && (
                                <div className="flex justify-between text-rose-600">
                                    <span>Khuyến mãi & Hội viên:</span>
                                    <span className="font-bold">-{formatVND(cart.totalDiscount)}</span>
                                </div>
                            )}

                            <div className="flex justify-between text-slate-600">
                                <span>Phí giao hàng:</span>
                                <span className="font-semibold text-emerald-600">Miễn phí</span>
                            </div>

                            <div className="pt-3 border-t border-slate-100 flex justify-between items-baseline">
                                <span className="font-bold text-slate-800 text-sm">Tổng thanh toán:</span>
                                <span className="font-black text-xl text-emerald-700">{formatVND(cart.estimatedTotal)}</span>
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="w-full py-4 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-2xl text-xs font-bold transition-all shadow-lg shadow-emerald-600/30 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50"
                        >
                            <span>{isSubmitting ? 'Đang xử lý...' : 'Xác Nhận & Đặt Hàng'}</span>
                            <ArrowRight className="w-4 h-4" />
                        </button>
                    </div>

                    <div className="p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100 text-xs text-emerald-800 space-y-1">
                        <p className="font-bold">🛡️ Cam kết giữ chỗ tồn kho tức thì</p>
                        <p className="text-[11px] text-slate-600">
                            Ngay sau khi đặt hàng, hệ thống tự động khóa giữ chỗ lô hàng còn hạn sử dụng mới nhất cho bạn.
                        </p>
                    </div>
                </div>

            </form>

        </div>
    );
}

