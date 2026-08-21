'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Package, Calendar, ArrowRight, Eye, User, MapPin, RotateCcw, Clock } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { apiClient, formatVND, formatDate } from '@/lib/api';
import { PagedResult, ShopOrder } from '@/types/shop';

export default function DonHangListPage() {
    const router = useRouter();
    const { isAuthenticated, initAuth } = useAuthStore();
    const [ordersResult, setOrdersResult] = useState<PagedResult<ShopOrder>>({
        items: [],
        totalRecords: 0,
        totalPages: 0,
        currentPage: 1,
        pageSize: 10
    });
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    useEffect(() => {
        if (!isAuthenticated) {
            router.push('/dang-nhap?redirect=/tai-khoan/don-hang');
            return;
        }

        apiClient.get<PagedResult<ShopOrder>>('/orders?pageIndex=1&pageSize=10')
            .then((res) => {
                setOrdersResult(res);
                setIsLoading(false);
            })
            .catch(() => {
                setIsLoading(false);
            });
    }, [isAuthenticated, router]);

    const getStatusBadge = (statusName: string) => {
        switch (statusName) {
            case 'Đã xác nhận':
            case 'Đang chuẩn bị hàng':
                return 'bg-blue-50 text-blue-700 border-blue-200';
            case 'Đang giao hàng':
                return 'bg-amber-50 text-amber-700 border-amber-200';
            case 'Giao thành công':
                return 'bg-emerald-50 text-emerald-700 border-emerald-200';
            case 'Đã hủy':
                return 'bg-rose-50 text-rose-700 border-rose-200';
            default:
                return 'bg-slate-50 text-slate-700 border-slate-200';
        }
    };

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Lịch Sử Đơn Hàng
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Theo dõi tiến độ xử lý và hành trình giao hàng nông sản
                </p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                
                {/* Navigation Sidebar */}
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
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-emerald-50 text-emerald-800 font-bold text-xs"
                    >
                        <Package className="w-4 h-4 text-emerald-600" />
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
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                    >
                        <RotateCcw className="w-4 h-4 text-slate-400" />
                        <span>Đổi trả hàng (RMA)</span>
                    </Link>
                </div>

                {/* Orders List */}
                <div className="lg:col-span-3 space-y-4">
                    {ordersResult.items.length > 0 ? (
                        ordersResult.items.map((order) => (
                            <div
                                key={order.id}
                                className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs hover:border-emerald-200 transition-all space-y-4"
                            >
                                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-100">
                                    <div>
                                        <p className="font-mono font-bold text-sm text-slate-900">
                                            {order.orderCode}
                                        </p>
                                        <span className="text-[11px] text-slate-400 flex items-center gap-1 mt-0.5">
                                            <Calendar className="w-3 h-3" />
                                            {formatDate(order.orderDate)}
                                        </span>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        <span className={`px-3 py-1 text-xs font-bold rounded-full border ${getStatusBadge(order.statusName)}`}>
                                            {order.statusName}
                                        </span>
                                        <span className="px-3 py-1 bg-slate-100 text-slate-700 text-xs font-semibold rounded-full">
                                            {order.paymentStatusName}
                                        </span>
                                    </div>
                                </div>

                                {/* Items Snapshot */}
                                <div className="space-y-2">
                                    {order.items.map((item) => (
                                        <div key={item.detailId} className="flex justify-between items-center text-xs">
                                            <span className="text-slate-700 font-medium">
                                                {item.quantity} x {item.variantName} ({item.uoMName})
                                            </span>
                                            <span className="font-bold text-slate-900">
                                                {formatVND(item.totalPrice)}
                                            </span>
                                        </div>
                                    ))}
                                </div>

                                {/* Total & CTA */}
                                <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
                                    <div>
                                        <span className="text-xs text-slate-500">Tổng thanh toán: </span>
                                        <span className="font-black text-emerald-700 text-base">
                                            {formatVND(order.totalAmount)}
                                        </span>
                                    </div>

                                    <Link
                                        href={`/tai-khoan/don-hang/${order.orderCode}`}
                                        className="inline-flex items-center gap-1.5 px-4 py-2 bg-slate-100 hover:bg-emerald-50 text-slate-800 hover:text-emerald-700 font-bold text-xs rounded-xl transition-colors"
                                    >
                                        <Eye className="w-3.5 h-3.5" />
                                        <span>Xem chi tiết</span>
                                    </Link>
                                </div>
                            </div>
                        ))
                    ) : (
                        <div className="bg-white rounded-3xl border border-slate-200 p-16 text-center space-y-4">
                            <div className="w-16 h-16 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center text-3xl mx-auto">
                                📦
                            </div>
                            <h3 className="text-base font-bold text-slate-800">Bạn chưa có đơn hàng nào</h3>
                            <p className="text-xs text-slate-500">Hãy trải nghiệm đặt mua nông sản sạch tươi ngon hôm nay!</p>
                            <Link
                                href="/san-pham"
                                className="inline-block px-5 py-2.5 bg-emerald-600 text-white text-xs font-bold rounded-full"
                            >
                                Mua sắm ngay
                            </Link>
                        </div>
                    )}
                </div>

            </div>

        </div>
    );
}
