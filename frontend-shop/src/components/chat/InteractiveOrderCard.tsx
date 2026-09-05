'use client';

import React, { useState } from 'react';
import { 
    ShoppingBag, 
    Plus, 
    Minus, 
    Trash2, 
    ArrowRight, 
    Gift,
    MapPin,
    Phone,
    User,
    CreditCard,
    Banknote,
    CheckCircle2,
    Truck
} from 'lucide-react';
import { InteractiveOrderPayload, InteractiveOrderItem } from '@/types/chat';
import { formatVND } from '@/lib/utils';
import shopAiApi from '@/api/shopAiApi';

interface InteractiveOrderCardProps {
    sessionId: number;
    payload: InteractiveOrderPayload;
    onOrderSuccess?: (result: any) => void;
}

export default function InteractiveOrderCard({ sessionId, payload, onOrderSuccess }: InteractiveOrderCardProps) {
    const [items, setItems] = useState<InteractiveOrderItem[]>(payload.items || []);
    const [receiverName, setReceiverName] = useState(payload.suggestedReceiverName || '');
    const [receiverPhone, setReceiverPhone] = useState(payload.suggestedReceiverPhone || '');
    const [deliveryAddress, setDeliveryAddress] = useState(payload.suggestedDeliveryAddress || '');
    const [paymentMethod, setPaymentMethod] = useState<number>(3); // 3 = VNPay, 1 = COD
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isSuccess, setIsSuccess] = useState(false);
    const [orderCode, setOrderCode] = useState<string>('');

    // Handle Quantity Change
    const handleUpdateQuantity = (index: number, delta: number) => {
        const updated = [...items];
        const newQty = Math.max(1, updated[index].quantity + delta);
        updated[index].quantity = newQty;
        updated[index].totalPrice = Math.max(0, (newQty * updated[index].unitPrice) - updated[index].discountAmount);
        setItems(updated);
    };

    // Handle Remove Item
    const handleRemoveItem = (index: number) => {
        const updated = items.filter((_, i) => i !== index);
        setItems(updated);
    };

    // Calculations
    const subTotal = items.reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);
    const totalDiscount = items.reduce((sum, item) => sum + item.discountAmount, 0);
    const netSubTotal = Math.max(0, subTotal - totalDiscount);
    const isFreeShipping = netSubTotal >= 300000;
    const shippingFee = items.length === 0 ? 0 : (isFreeShipping ? 0 : 25000);
    const totalAmount = Math.max(0, netSubTotal + shippingFee);
    const missingForFreeship = Math.max(0, 300000 - netSubTotal);

    // Confirm & Place Order
    const handleConfirmOrder = async () => {
        if (items.length === 0) {
            alert('Đơn hàng không có sản phẩm nào.');
            return;
        }

        setIsSubmitting(true);
        try {
            const res = await shopAiApi.confirmOrder({
                sessionId,
                items,
                receiverName: receiverName.trim() || 'Khách hàng',
                receiverPhone: receiverPhone.trim() || '0900000000',
                deliveryAddress: deliveryAddress.trim() || 'Địa chỉ nhận hàng',
                shippingFee,
                paymentMethod
            });

            setIsSuccess(true);
            setOrderCode(res.orderCode);
            if (onOrderSuccess) onOrderSuccess(res);

            // Nếu thanh toán VNPay và có link chuyển hướng
            if (paymentMethod === 3 && res.paymentUrl) {
                setTimeout(() => {
                    window.location.href = res.paymentUrl!;
                }, 1200);
            }
        } catch (error: any) {
            alert(error?.message || 'Có lỗi xảy ra khi tạo đơn hàng.');
        } finally {
            setIsSubmitting(false);
        }
    };

    if (isSuccess) {
        return (
            <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-4 text-center space-y-2">
                <div className="w-10 h-10 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center mx-auto text-lg font-bold">
                    <CheckCircle2 className="w-6 h-6" />
                </div>
                <h4 className="font-bold text-xs text-emerald-900">ĐÃ TẠO ĐƠN HÀNG THÀNH CÔNG!</h4>
                <p className="text-[11px] text-emerald-700">Mã đơn: <span className="font-mono font-bold">{orderCode}</span></p>
                <p className="text-[10px] text-slate-500">Đơn hàng đang được kho Solaris đóng gói để giao nhanh qua GHN Express.</p>
            </div>
        );
    }

    if (items.length === 0) {
        return (
            <div className="bg-slate-50 border border-slate-200 rounded-2xl p-4 text-center text-xs text-slate-500">
                Đơn hàng gợi ý đã trống. Bạn hãy nhắn tin để em tìm thêm nông sản khác nhé!
            </div>
        );
    }

    return (
        <div className="bg-white rounded-2xl border border-emerald-200 shadow-sm p-4 space-y-3.5 text-xs">
            {/* Header */}
            <div className="flex items-center justify-between pb-2.5 border-b border-slate-100">
                <div className="flex items-center gap-1.5 font-bold text-slate-900 text-xs">
                    <ShoppingBag className="w-4 h-4 text-emerald-600" />
                    <span>{payload.title || 'Đơn Hàng Đặt Lại'}</span>
                </div>
                <span className="px-2 py-0.5 bg-emerald-100 text-emerald-800 text-[10px] font-bold rounded-full">
                    Báo giá động
                </span>
            </div>

            {/* Freeship Indicator */}
            <div className={`p-2.5 rounded-xl border flex items-center gap-2 text-[11px] font-medium ${
                isFreeShipping
                    ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                    : 'bg-amber-50 border-amber-200 text-amber-900'
            }`}>
                {isFreeShipping ? (
                    <>
                        <Truck className="w-4 h-4 text-emerald-600 shrink-0" />
                        <span>Đơn hàng đã đạt <strong>MIỄN PHÍ GIAO HÀNG</strong> qua GHN!</span>
                    </>
                ) : (
                    <>
                        <Gift className="w-4 h-4 text-amber-600 shrink-0" />
                        <span>Thêm <strong>{formatVND(missingForFreeship)}</strong> để được <strong>FREESHIP</strong>.</span>
                    </>
                )}
            </div>

            {/* Item List with Quantity Controls */}
            <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                {items.map((item, idx) => (
                    <div key={item.variantId || idx} className="p-2.5 bg-slate-50 rounded-xl flex items-center justify-between gap-2">
                        <div className="flex-1 min-w-0">
                            <p className="font-bold text-slate-800 truncate text-[11px]">{item.variantName}</p>
                            <p className="text-[10px] text-slate-500">
                                {formatVND(item.unitPrice)} / {item.uoMName || 'Kg'}
                            </p>
                        </div>

                        {/* Interactive +/- Buttons */}
                        <div className="flex items-center gap-1.5 shrink-0">
                            <button
                                type="button"
                                onClick={() => handleUpdateQuantity(idx, -1)}
                                className="w-6 h-6 rounded-lg bg-white border border-slate-200 text-slate-700 hover:bg-slate-100 flex items-center justify-center font-bold"
                                title="Giảm số lượng"
                            >
                                <Minus className="w-3 h-3" />
                            </button>
                            <span className="w-5 text-center font-bold text-slate-900 text-xs">
                                {item.quantity}
                            </span>
                            <button
                                type="button"
                                onClick={() => handleUpdateQuantity(idx, 1)}
                                className="w-6 h-6 rounded-lg bg-white border border-slate-200 text-slate-700 hover:bg-slate-100 flex items-center justify-center font-bold"
                                title="Tăng số lượng"
                            >
                                <Plus className="w-3 h-3" />
                            </button>
                            <button
                                type="button"
                                onClick={() => handleRemoveItem(idx)}
                                className="w-6 h-6 rounded-lg text-rose-500 hover:bg-rose-50 flex items-center justify-center ml-1"
                                title="Xóa món"
                            >
                                <Trash2 className="w-3 h-3" />
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            {/* Delivery Info Inputs Mini */}
            <div className="p-2.5 bg-slate-50 rounded-xl space-y-1.5 text-[11px]">
                <div className="flex items-center gap-1 text-slate-600 font-semibold">
                    <MapPin className="w-3.5 h-3.5 text-emerald-600 shrink-0" />
                    <input
                        type="text"
                        placeholder="Địa chỉ giao hàng..."
                        value={deliveryAddress}
                        onChange={(e) => setDeliveryAddress(e.target.value)}
                        className="w-full bg-transparent border-b border-slate-200 pb-0.5 focus:outline-hidden focus:border-emerald-500 text-slate-800"
                    />
                </div>
                <div className="grid grid-cols-2 gap-2 pt-1">
                    <div className="flex items-center gap-1 text-slate-600">
                        <User className="w-3 h-3 text-slate-400 shrink-0" />
                        <input
                            type="text"
                            placeholder="Tên người nhận"
                            value={receiverName}
                            onChange={(e) => setReceiverName(e.target.value)}
                            className="w-full bg-transparent border-b border-slate-200 pb-0.5 focus:outline-hidden focus:border-emerald-500 text-slate-800 text-[10px]"
                        />
                    </div>
                    <div className="flex items-center gap-1 text-slate-600">
                        <Phone className="w-3 h-3 text-slate-400 shrink-0" />
                        <input
                            type="tel"
                            placeholder="Số điện thoại"
                            value={receiverPhone}
                            onChange={(e) => setReceiverPhone(e.target.value)}
                            className="w-full bg-transparent border-b border-slate-200 pb-0.5 focus:outline-hidden focus:border-emerald-500 text-slate-800 text-[10px]"
                        />
                    </div>
                </div>
            </div>

            {/* Payment Method Choice */}
            <div className="flex items-center gap-2 pt-1">
                <label className={`flex-1 p-2 rounded-xl border cursor-pointer transition-all flex items-center gap-1.5 text-[11px] ${
                    paymentMethod === 3 ? 'border-emerald-600 bg-emerald-50/50 text-emerald-900 font-bold' : 'border-slate-200 text-slate-600'
                }`}>
                    <input
                        type="radio"
                        name="payMethod"
                        checked={paymentMethod === 3}
                        onChange={() => setPaymentMethod(3)}
                        className="hidden"
                    />
                    <CreditCard className="w-3.5 h-3.5 text-blue-600" />
                    <span>VNPay Sandbox</span>
                </label>

                <label className={`flex-1 p-2 rounded-xl border cursor-pointer transition-all flex items-center gap-1.5 text-[11px] ${
                    paymentMethod === 1 ? 'border-emerald-600 bg-emerald-50/50 text-emerald-900 font-bold' : 'border-slate-200 text-slate-600'
                }`}>
                    <input
                        type="radio"
                        name="payMethod"
                        checked={paymentMethod === 1}
                        onChange={() => setPaymentMethod(1)}
                        className="hidden"
                    />
                    <Banknote className="w-3.5 h-3.5 text-emerald-600" />
                    <span>Khi nhận (COD)</span>
                </label>
            </div>

            {/* Price Summary & Submit Button */}
            <div className="pt-2 border-t border-slate-100 space-y-2">
                <div className="flex justify-between items-center text-xs">
                    <span className="text-slate-500">Phí ship GHN:</span>
                    <span className="font-semibold text-slate-800">
                        {isFreeShipping ? <span className="text-emerald-600 font-bold">Miễn phí</span> : formatVND(shippingFee)}
                    </span>
                </div>
                <div className="flex justify-between items-baseline text-xs">
                    <span className="font-bold text-slate-800">Tổng thanh toán:</span>
                    <span className="font-black text-sm text-emerald-700">{formatVND(totalAmount)}</span>
                </div>

                <button
                    type="button"
                    onClick={handleConfirmOrder}
                    disabled={isSubmitting || items.length === 0}
                    className="w-full py-2.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-xl font-bold text-xs transition-all shadow-md shadow-emerald-600/20 flex items-center justify-center gap-1.5 active:scale-98 disabled:opacity-50 cursor-pointer"
                >
                    <span>{isSubmitting ? 'Đang tạo đơn...' : paymentMethod === 3 ? 'Xác Nhận & Thanh Toán VNPay' : 'Xác Nhận Đặt Đơn Này'}</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                </button>
            </div>
        </div>
    );
}
