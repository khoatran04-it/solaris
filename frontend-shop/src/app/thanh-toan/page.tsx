'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { 
    MapPin, 
    CreditCard, 
    Truck, 
    CheckCircle2, 
    ShoppingBag, 
    ArrowRight,
    AlertCircle,
    Sparkles,
    Gift
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import shopOrderApi from '@/api/shopOrderApi';
import shopCustomerApi from '@/api/shopCustomerApi';
import shopShippingApi from '@/api/shopShippingApi';
import shopPaymentApi from '@/api/shopPaymentApi';
import { formatVND } from '@/lib/utils';
import { ShopAddress } from '@/types/customer';
import { ShopOrder, ShopCheckoutPayload } from '@/types/order';
import { GhnProvince, GhnDistrict, GhnWard } from '@/types/shipping';

export default function ThanhToanPage() {
    const router = useRouter();
    const { isAuthenticated, initAuth } = useAuthStore();
    const { cart, fetchCart, clearCart } = useCartStore();

    const [addresses, setAddresses] = useState<ShopAddress[]>([]);
    const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);

    // GHN Location Data
    const [provinces, setProvinces] = useState<GhnProvince[]>([]);
    const [districts, setDistricts] = useState<GhnDistrict[]>([]);
    const [wards, setWards] = useState<GhnWard[]>([]);

    // Address selection state
    const [useNewAddress, setUseNewAddress] = useState(false);
    const [receiverName, setReceiverName] = useState('');
    const [receiverPhone, setReceiverPhone] = useState('');
    const [selectedProvinceId, setSelectedProvinceId] = useState<number>(201); // Mặc định TP.HCM
    const [selectedDistrictId, setSelectedDistrictId] = useState<number | null>(null);
    const [selectedWardCode, setSelectedWardCode] = useState<string>('');
    const [streetAddress, setStreetAddress] = useState('');
    const [note, setNote] = useState('');

    // Shipping Fee & Freeship State
    const [shippingFee, setShippingFee] = useState<number>(25000);
    const [isFreeShipping, setIsFreeShipping] = useState<boolean>(false);
    const [isCalculatingFee, setIsCalculatingFee] = useState<boolean>(false);
    const freeShippingThreshold = 300000;

    // Payment method: 1 = COD, 2 = BankTransfer, 3 = VNPay
    const [paymentMethod, setPaymentMethod] = useState<number>(3); // Mặc định VNPay Sandbox
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [errorMessage, setErrorMessage] = useState('');

    useEffect(() => {
        initAuth();
        fetchCart();
    }, [initAuth, fetchCart]);

    // Load Provinces
    useEffect(() => {
        shopShippingApi.getProvinces()
            .then(data => {
                setProvinces(data);
                if (data.length > 0) {
                    const hcm = data.find(p => p.provinceName.includes('Hồ Chí Minh')) || data[0];
                    setSelectedProvinceId(hcm.provinceID);
                }
            })
            .catch(() => {});
    }, []);

    // Load Districts when Province changes
    useEffect(() => {
        if (selectedProvinceId) {
            shopShippingApi.getDistricts(selectedProvinceId)
                .then(data => {
                    setDistricts(data);
                    if (data.length > 0) {
                        setSelectedDistrictId(data[0].districtID);
                    } else {
                        setSelectedDistrictId(null);
                    }
                })
                .catch(() => {});
        }
    }, [selectedProvinceId]);

    // Load Wards when District changes
    useEffect(() => {
        if (selectedDistrictId) {
            shopShippingApi.getWards(selectedDistrictId)
                .then(data => {
                    setWards(data);
                    if (data.length > 0) {
                        setSelectedWardCode(data[0].wardCode);
                    } else {
                        setSelectedWardCode('');
                    }
                })
                .catch(() => {});
        }
    }, [selectedDistrictId]);

    // Calculate Realtime Shipping Fee via GHN
    const calculateFee = useCallback(async () => {
        if (!selectedDistrictId || !selectedWardCode || !cart) return;

        setIsCalculatingFee(true);
        try {
            const res = await shopShippingApi.calculateFee({
                toDistrictId: selectedDistrictId,
                toWardCode: selectedWardCode,
                subTotal: cart.subTotal - cart.totalDiscount,
                weightGram: cart.items.length * 500
            });

            setShippingFee(res.totalFee);
            setIsFreeShipping(res.isFreeShipping);
        } catch {
            // Giữ giá trị dự phòng nếu có lỗi mạng
            const isEligibleFree = (cart.subTotal - cart.totalDiscount) >= freeShippingThreshold;
            setShippingFee(isEligibleFree ? 0 : 25000);
            setIsFreeShipping(isEligibleFree);
        } finally {
            setIsCalculatingFee(false);
        }
    }, [selectedDistrictId, selectedWardCode, cart]);

    useEffect(() => {
        calculateFee();
    }, [calculateFee]);

    // Load saved customer addresses if authenticated
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

    // Khi đổi địa chỉ đã lưu -> Đồng bộ sang GHN District & Ward để tính phí ship chuẩn xác
    useEffect(() => {
        if (!useNewAddress && selectedAddressId && addresses.length > 0 && provinces.length > 0) {
            const addr = addresses.find(a => a.id === selectedAddressId);
            if (addr) {
                const prov = provinces.find(p => 
                    p.provinceName.toLowerCase().includes(addr.province.toLowerCase()) || 
                    addr.province.toLowerCase().includes(p.provinceName.toLowerCase())
                );
                if (prov) {
                    setSelectedProvinceId(prov.provinceID);
                    shopShippingApi.getDistricts(prov.provinceID).then(distList => {
                        setDistricts(distList);
                        const dist = distList.find(d => 
                            d.districtName.toLowerCase().includes(addr.district.toLowerCase()) || 
                            addr.district.toLowerCase().includes(d.districtName.toLowerCase())
                        ) || distList[0];
                        if (dist) {
                            setSelectedDistrictId(dist.districtID);
                            shopShippingApi.getWards(dist.districtID).then(wardList => {
                                setWards(wardList);
                                const ward = wardList.find(w => 
                                    w.wardName.toLowerCase().includes(addr.ward.toLowerCase()) || 
                                    addr.ward.toLowerCase().includes(w.wardName.toLowerCase())
                                ) || wardList[0];
                                if (ward) {
                                    setSelectedWardCode(ward.wardCode);
                                }
                            });
                        }
                    });
                }
            }
        }
    }, [selectedAddressId, useNewAddress, addresses, provinces]);

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
            const currentProvince = provinces.find(p => p.provinceID === selectedProvinceId)?.provinceName || 'TP. Hồ Chí Minh';
            const currentDistrict = districts.find(d => d.districtID === selectedDistrictId)?.districtName || '';
            const currentWard = wards.find(w => w.wardCode === selectedWardCode)?.wardName || '';

            const payload: ShopCheckoutPayload = {
                customerAddressId: useNewAddress ? undefined : (selectedAddressId ?? undefined),
                receiverName: useNewAddress ? receiverName.trim() : undefined,
                receiverPhone: useNewAddress ? receiverPhone.trim() : undefined,
                province: useNewAddress ? currentProvince : undefined,
                district: useNewAddress ? currentDistrict : undefined,
                ward: useNewAddress ? currentWard : undefined,
                streetAddress: useNewAddress ? streetAddress.trim() : undefined,
                ghnDistrictId: selectedDistrictId ?? undefined,
                ghnWardCode: selectedWardCode || undefined,
                shippingFee: shippingFee,
                latitude: 10.7769,
                longitude: 106.7009,
                paymentMethod: paymentMethod,
                note: note.trim()
            };

            // 1. Tạo đơn hàng trên backend
            const order: ShopOrder = await shopOrderApi.checkout(payload);
            
            // 2. Xóa giỏ hàng trên client
            await clearCart();

            // 3. Nếu chọn VNPay -> Tạo URL thanh toán và chuyển hướng
            if (paymentMethod === 3) {
                const vnPayRes = await shopPaymentApi.createVnPayUrl({
                    orderCode: order.orderCode,
                    orderDescription: `Thanh toán đơn hàng Solaris ${order.orderCode}`
                });
                
                if (vnPayRes?.paymentUrl) {
                    window.location.href = vnPayRes.paymentUrl;
                    return;
                }
            }

            // Redirect tới chi tiết đơn hàng (COD / BankTransfer)
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
                <ShoppingBag className="w-12 h-12 text-slate-300 mx-auto" />
                <p className="text-sm font-semibold text-slate-600">Giỏ hàng của bạn đang trống.</p>
                <Link href="/san-pham" className="inline-block px-6 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-full transition-colors shadow-md">
                    Tiếp tục mua hàng
                </Link>
            </div>
        );
    }

    const netSubTotal = cart.subTotal - cart.totalDiscount;
    const finalTotal = Math.max(0, netSubTotal + shippingFee);
    const amountMissingForFreeShip = Math.max(0, freeShippingThreshold - netSubTotal);

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Header */}
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Xác Nhận & Thanh Toán
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Giao hàng nhanh toàn quốc qua GHN • Thanh toán bảo mật VNPay Sandbox
                </p>
            </div>

            {/* Freeship Alert Banner */}
            <div className={`p-4 rounded-2xl border transition-all flex items-center justify-between gap-3 text-xs ${
                isFreeShipping || netSubTotal >= freeShippingThreshold
                    ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                    : 'bg-amber-50 border-amber-200 text-amber-900'
            }`}>
                <div className="flex items-center gap-2.5 font-medium">
                    {isFreeShipping || netSubTotal >= freeShippingThreshold ? (
                        <>
                            <Sparkles className="w-5 h-5 text-emerald-600 shrink-0" />
                            <span>🎉 Chúc mừng! Đơn hàng của bạn đạt điều kiện <strong>MIỄN PHÍ VẬN CHUYỂN</strong> toàn quốc.</span>
                        </>
                    ) : (
                        <>
                            <Gift className="w-5 h-5 text-amber-600 shrink-0" />
                            <span>Mua thêm <strong>{formatVND(amountMissingForFreeShip)}</strong> để được <strong>FREESHIP 100%</strong> (Đơn từ 300k).</span>
                        </>
                    )}
                </div>
                {amountMissingForFreeShip > 0 && (
                    <Link href="/san-pham" className="text-[11px] font-bold text-amber-800 underline shrink-0 hover:text-amber-900">
                        Mua thêm
                    </Link>
                )}
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
                    
                    {/* 1. Sổ Địa Chỉ Giao Hàng & GHN Selector */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs space-y-5">
                        <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                            <h2 className="text-sm font-bold text-slate-900 flex items-center gap-2">
                                <MapPin className="w-4 h-4 text-emerald-600" />
                                1. Địa Chỉ Nhận Hàng (GHN Logistics)
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
                            /* New Address Form with GHN 3-level selector */
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
                                    <label className="text-xs font-semibold text-slate-700">Tỉnh / Thành phố (GHN) *</label>
                                    <select
                                        value={selectedProvinceId}
                                        onChange={(e) => setSelectedProvinceId(Number(e.target.value))}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                    >
                                        {provinces.map(p => (
                                            <option key={p.provinceID} value={p.provinceID}>
                                                {p.provinceName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Quận / Huyện (GHN) *</label>
                                    <select
                                        value={selectedDistrictId || ''}
                                        onChange={(e) => setSelectedDistrictId(Number(e.target.value))}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                    >
                                        {districts.map(d => (
                                            <option key={d.districtID} value={d.districtID}>
                                                {d.districtName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Phường / Xã (GHN) *</label>
                                    <select
                                        value={selectedWardCode}
                                        onChange={(e) => setSelectedWardCode(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                    >
                                        {wards.map(w => (
                                            <option key={w.wardCode} value={w.wardCode}>
                                                {w.wardName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Địa chỉ cụ thể (Số nhà, tên đường) *</label>
                                    <input
                                        type="text"
                                        placeholder="123 Đường Lê Lợi"
                                        value={streetAddress}
                                        onChange={(e) => setStreetAddress(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
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
                                className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                            />
                        </div>
                    </div>

                    {/* 2. Phương Thức Thanh Toán */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs space-y-4">
                        <h2 className="text-sm font-bold text-slate-900 flex items-center gap-2 pb-3 border-b border-slate-100">
                            <CreditCard className="w-4 h-4 text-emerald-600" />
                            2. Phương Thức Thanh Toán
                        </h2>

                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                            {/* VNPay */}
                            <div
                                onClick={() => setPaymentMethod(3)}
                                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                                    paymentMethod === 3
                                        ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                        : 'border-slate-200 hover:border-slate-300'
                                }`}
                            >
                                <div className="space-y-2">
                                    <div className="flex items-center justify-between">
                                        <div className="w-8 h-8 rounded-lg bg-blue-600 text-white flex items-center justify-center font-black text-xs">
                                            VNP
                                        </div>
                                        {paymentMethod === 3 && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-xs text-slate-900">Cổng VNPay Sandbox</h4>
                                        <p className="text-[10px] text-slate-500">Quét VNPAY-QR, Thẻ ATM/Visa</p>
                                    </div>
                                </div>
                            </div>

                            {/* COD */}
                            <div
                                onClick={() => setPaymentMethod(1)}
                                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                                    paymentMethod === 1
                                        ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                        : 'border-slate-200 hover:border-slate-300'
                                }`}
                            >
                                <div className="space-y-2">
                                    <div className="flex items-center justify-between">
                                        <div className="w-8 h-8 rounded-lg bg-emerald-100 text-emerald-800 flex items-center justify-center font-bold text-sm">
                                            💵
                                        </div>
                                        {paymentMethod === 1 && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-xs text-slate-900">Khi nhận hàng (COD)</h4>
                                        <p className="text-[10px] text-slate-500">Kiểm hàng trước khi trả tiền</p>
                                    </div>
                                </div>
                            </div>

                            {/* BankTransfer */}
                            <div
                                onClick={() => setPaymentMethod(2)}
                                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                                    paymentMethod === 2
                                        ? 'border-emerald-600 bg-emerald-50/50 shadow-xs'
                                        : 'border-slate-200 hover:border-slate-300'
                                }`}
                            >
                                <div className="space-y-2">
                                    <div className="flex items-center justify-between">
                                        <div className="w-8 h-8 rounded-lg bg-purple-100 text-purple-800 flex items-center justify-center font-bold text-sm">
                                            🏦
                                        </div>
                                        {paymentMethod === 2 && <CheckCircle2 className="w-4 h-4 text-emerald-600" />}
                                    </div>
                                    <div>
                                        <h4 className="font-bold text-xs text-slate-900">Chuyển khoản VietQR</h4>
                                        <p className="text-[10px] text-slate-500">Quét mã QR Mobile Banking</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {paymentMethod === 3 && (
                            <div className="p-3 bg-blue-50/70 rounded-xl border border-blue-100 text-[11px] text-blue-900 flex items-center gap-2">
                                <span>🔒</span>
                                <span>Sau khi bấm đặt hàng, bạn sẽ được chuyển hướng an toàn sang cổng <strong>VNPay Sandbox</strong> để hoàn tất thanh toán.</span>
                            </div>
                        )}
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

                            <div className="flex justify-between items-center text-slate-600">
                                <span className="flex items-center gap-1">
                                    <Truck className="w-3.5 h-3.5 text-slate-400" />
                                    Phí giao hàng (GHN):
                                </span>
                                {isCalculatingFee ? (
                                    <span className="text-[11px] text-slate-400 italic">Đang tính...</span>
                                ) : isFreeShipping || shippingFee === 0 ? (
                                    <span className="font-bold text-emerald-600">Miễn phí</span>
                                ) : (
                                    <span className="font-semibold text-slate-900">{formatVND(shippingFee)}</span>
                                )}
                            </div>

                            <div className="pt-3 border-t border-slate-100 flex justify-between items-baseline">
                                <span className="font-bold text-slate-800 text-sm">Tổng thanh toán:</span>
                                <span className="font-black text-xl text-emerald-700">{formatVND(finalTotal)}</span>
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="w-full py-4 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-2xl text-xs font-bold transition-all shadow-lg shadow-emerald-600/30 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50"
                        >
                            <span>{isSubmitting ? 'Đang xử lý...' : paymentMethod === 3 ? 'Thanh Toán Qua VNPay' : 'Xác Nhận & Đặt Hàng'}</span>
                            <ArrowRight className="w-4 h-4" />
                        </button>
                    </div>

                    <div className="p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100 text-xs text-emerald-800 space-y-1">
                        <p className="font-bold">🛡️ Cam kết giao nông sản tươi mới</p>
                        <p className="text-[11px] text-slate-600">
                            Đơn hàng được bàn giao ngay cho GHN Express với bao bì đóng gói bảo quản nông sản chuyên dụng.
                        </p>
                    </div>
                </div>

            </form>

        </div>
    );
}
