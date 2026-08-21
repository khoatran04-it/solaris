'use client';

import React, { useState, useEffect, Suspense } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { RotateCcw, User, Package, MapPin, Plus, CheckCircle2, AlertCircle, Calendar } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import shopOrderApi from '@/api/shopOrderApi';
import shopReturnApi from '@/api/shopReturnApi';
import { formatVND, formatDateTime } from '@/lib/utils';
import { PagedResult } from '@/types/common';
import { ShopReturn } from '@/types/return';
import { ShopOrder } from '@/types/order';

function TraHangContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const defaultOrderCode = searchParams.get('orderCode') || '';

    const { isAuthenticated, initAuth } = useAuthStore();
    const [returnsResult, setReturnsResult] = useState<PagedResult<ShopReturn>>({
        items: [],
        totalRecords: 0,
        totalPages: 0,
        currentPage: 1,
        pageSize: 10
    });

    const [showCreateForm, setShowCreateForm] = useState(Boolean(defaultOrderCode));
    const [orderCode, setOrderCode] = useState(defaultOrderCode);
    const [reason, setReason] = useState('');
    const [targetOrder, setTargetOrder] = useState<ShopOrder | null>(null);
    const [selectedItems, setSelectedItems] = useState<{ [variantId: number]: { qty: number; reason: string; uoMId: number } }>({});
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    const loadReturns = () => {
        shopReturnApi.getAll(1, 10)
            .then(setReturnsResult)
            .catch(() => {});
    };

    useEffect(() => {
        if (!isAuthenticated) {
            router.push('/dang-nhap?redirect=/tai-khoan/tra-hang');
            return;
        }
        loadReturns();
    }, [isAuthenticated, router]);

    const handleSearchOrder = async () => {
        if (!orderCode.trim()) return;
        try {
            const ord: ShopOrder = await shopOrderApi.getByCode(orderCode.trim());
            setTargetOrder(ord);
            const init: any = {};
            ord.items.forEach(i => {
                init[i.variantId] = { qty: i.quantity, reason: 'Dập nát hoặc không đạt độ tươi', uoMId: i.uoMId };
            });
            setSelectedItems(init);
        } catch {
            alert('Không tìm thấy mã đơn hàng hợp lệ.');
        }
    };

    const handleCreateReturn = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!targetOrder) return;

        setIsSubmitting(true);
        try {
            const items = Object.entries(selectedItems).map(([variantId, data]) => ({
                variantId: parseInt(variantId, 10),
                uoMId: data.uoMId,
                batchId: 0,
                returnedQuantity: data.qty,
                reason: data.reason
            })).filter(i => i.returnedQuantity > 0);

            await shopReturnApi.create({
                orderCode: targetOrder.orderCode,
                reason: reason.trim(),
                items
            });

            setShowCreateForm(false);
            setTargetOrder(null);
            setReason('');
            loadReturns();
        } catch (error: any) {
            alert(error?.message || 'Không thể gửi yêu cầu đổi/trả.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
            {/* Sidebar */}
            <div className="lg:col-span-1 bg-white rounded-3xl border border-slate-200 p-5 space-y-2 shadow-xs">
                <Link
                    href="/tai-khoan"
                    className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                >
                    <User className="w-4 h-4 text-slate-400" />
                    <span>Hồ sơ cá nhân</span>
                </Link>

                <Link
                    href="/tai-khoan/don-hang"
                    className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                >
                    <Package className="w-4 h-4 text-slate-400" />
                    <span>Lịch sử đơn hàng</span>
                </Link>

                <Link
                    href="/tai-khoan/dia-chi"
                    className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                >
                    <MapPin className="w-4 h-4 text-slate-400" />
                    <span>Sổ địa chỉ</span>
                </Link>

                <Link
                    href="/tai-khoan/tra-hang"
                    className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-emerald-50 text-emerald-800 font-bold text-xs"
                >
                    <RotateCcw className="w-4 h-4 text-emerald-600" />
                    <span>Đổi trả hàng (RMA)</span>
                </Link>
            </div>

            {/* Content */}
            <div className="lg:col-span-3 space-y-6">
                <div className="flex items-center justify-between">
                    <h2 className="text-sm font-bold text-slate-900">
                        Danh Sách Yêu Cầu Đổi Trả ({returnsResult.totalRecords})
                    </h2>

                    {!showCreateForm && (
                        <button
                            onClick={() => setShowCreateForm(true)}
                            className="inline-flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition-colors shadow-xs"
                        >
                            <Plus className="w-4 h-4" />
                            <span>Tạo yêu cầu mới</span>
                        </button>
                    )}
                </div>

                {showCreateForm && (
                    <div className="bg-white rounded-3xl border border-emerald-200 p-6 sm:p-8 shadow-md space-y-5">
                        <h3 className="text-sm font-bold text-slate-900 pb-2 border-b border-slate-100">
                            Tạo Phiếu Đổi/Trả Hàng Nông Sản
                        </h3>

                        <div className="flex gap-2">
                            <input
                                type="text"
                                placeholder="Nhập mã đơn hàng (VD: ORD-20260821-XXXX)..."
                                value={orderCode}
                                onChange={(e) => setOrderCode(e.target.value)}
                                className="flex-1 text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                            />
                            <button
                                type="button"
                                onClick={handleSearchOrder}
                                className="px-4 py-2.5 bg-slate-900 text-white rounded-xl text-xs font-bold"
                            >
                                Kiểm tra đơn
                            </button>
                        </div>

                        {targetOrder && (
                            <form onSubmit={handleCreateReturn} className="space-y-4 pt-2">
                                <div className="space-y-2">
                                    <label className="text-xs font-bold text-slate-800 block">
                                        Chọn mặt hàng và số lượng cần trả:
                                    </label>
                                    <div className="space-y-2">
                                        {targetOrder.items.map((item) => (
                                            <div key={item.detailId} className="p-3 bg-slate-50 rounded-xl flex items-center justify-between text-xs gap-4">
                                                <div className="flex-1 truncate">
                                                    <p className="font-bold text-slate-900 truncate">{item.variantName}</p>
                                                    <p className="text-[11px] text-slate-500">Đã nhận: {item.quantity} {item.uoMName}</p>
                                                </div>

                                                <div className="flex items-center gap-2">
                                                    <span className="text-[11px] text-slate-600">SL trả:</span>
                                                    <input
                                                        type="number"
                                                        min={1}
                                                        max={item.quantity}
                                                        value={selectedItems[item.variantId]?.qty || 1}
                                                        onChange={(e) => {
                                                            const val = parseInt(e.target.value, 10) || 1;
                                                            setSelectedItems({
                                                                ...selectedItems,
                                                                [item.variantId]: {
                                                                    ...selectedItems[item.variantId],
                                                                    qty: val,
                                                                    uoMId: item.uoMId,
                                                                    reason: selectedItems[item.variantId]?.reason || 'Không đạt chất lượng'
                                                                }
                                                            });
                                                        }}
                                                        className="w-16 p-1.5 bg-white border border-slate-200 rounded-lg text-center font-bold"
                                                    />
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Lý do chung *</label>
                                    <textarea
                                        placeholder="Mô tả cụ thể tình trạng hàng hóa..."
                                        value={reason}
                                        onChange={(e) => setReason(e.target.value)}
                                        className="w-full text-xs p-3 bg-slate-50 border border-slate-200 rounded-xl h-20"
                                        required
                                    />
                                </div>

                                <div className="flex items-center gap-3 pt-2">
                                    <button
                                        type="submit"
                                        disabled={isSubmitting}
                                        className="px-6 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold"
                                    >
                                        {isSubmitting ? 'Đang gửi...' : 'Gửi yêu cầu trả hàng'}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setShowCreateForm(false)}
                                        className="px-4 py-2.5 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold"
                                    >
                                        Hủy
                                    </button>
                                </div>
                            </form>
                        )}
                    </div>
                )}

                {/* Returns List */}
                <div className="space-y-4">
                    {returnsResult.items.length > 0 ? (
                        returnsResult.items.map((ret) => (
                            <div
                                key={ret.id}
                                className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs space-y-3"
                            >
                                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                                    <div>
                                        <p className="font-mono font-bold text-xs text-slate-900">{ret.returnCode}</p>
                                        <p className="text-[11px] text-slate-500">Đơn hàng: <strong>{ret.orderCode}</strong> • {formatDateTime(ret.returnDate)}</p>
                                    </div>

                                    <span className="px-3 py-1 bg-amber-50 text-amber-800 border border-amber-200 text-xs font-bold rounded-full">
                                        {ret.statusName}
                                    </span>
                                </div>

                                <div className="space-y-1.5 text-xs text-slate-600">
                                    <p>Lý do: {ret.reason || 'Đổi trả bảo hành'}</p>
                                    <p>Tiền hoàn dự kiến: <strong className="text-emerald-700 font-bold">{formatVND(ret.refundAmount)}</strong></p>
                                    {ret.inspectionNotes && (
                                        <p className="text-slate-500 italic">Ghi chú QC: {ret.inspectionNotes}</p>
                                    )}
                                </div>
                            </div>
                        ))
                    ) : (
                        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center space-y-2">
                            <p className="text-xs text-slate-500">Bạn chưa có yêu cầu đổi trả hàng nào.</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default function TraHangPage() {
    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Đổi Trả Hàng & Hoàn Tiền (RMA)
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Chính sách cam kết đổi trả trong 24H nếu hàng dập nát hoặc không đạt chuẩn chất lượng
                </p>
            </div>

            <Suspense fallback={<div className="text-xs text-slate-500">Đang tải...</div>}>
                <TraHangContent />
            </Suspense>
        </div>
    );
}

