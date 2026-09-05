"use client";

import React, { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Package, Calendar, Eye } from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import shopOrderApi from "@/api/shopOrderApi";
import { formatVND, formatDateTime } from "@/lib/utils";
import { PagedResult } from "@/types/common";
import { ShopOrder } from "@/types/order";
import AccountSidebar from "@/components/account/AccountSidebar";
import EmptyState from "@/components/common/EmptyState";

export default function DonHangListPage() {
  const router = useRouter();
  const { isAuthenticated, initAuth } = useAuthStore();
  const [ordersResult, setOrdersResult] = useState<PagedResult<ShopOrder>>({
    items: [],
    totalRecords: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 10,
  });
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    initAuth();
  }, [initAuth]);

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/dang-nhap?redirect=/tai-khoan/don-hang");
      return;
    }

    shopOrderApi
      .getAll(1, 10)
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
      case "Đã xác nhận":
      case "Đang chuẩn bị hàng":
        return "bg-amber-50 text-amber-800 border-amber-200";
      case "Đang giao hàng":
        return "bg-blue-50 text-blue-800 border-blue-200";
      case "Giao thành công":
        return "bg-emerald-50 text-emerald-800 border-emerald-200";
      case "Đã hủy":
        return "bg-rose-50 text-rose-700 border-rose-200";
      default:
        return "bg-slate-50 text-slate-700 border-slate-200";
    }
  };

  return (
    <div className="min-h-[85vh] bg-slate-50/40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
            Lịch Sử Đơn Hàng
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Theo dõi tiến độ xử lý và hành trình giao hàng nông sản tươi sạch
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Navigation Sidebar */}
          <AccountSidebar activeTab="orders" className="lg:col-span-4" />

          {/* Orders List */}
          <div className="lg:col-span-8 space-y-4">
            {isLoading ? (
              <div className="bg-white rounded-3xl border border-slate-200/80 p-16 text-center text-xs font-semibold text-slate-400">
                Đang tải lịch sử đơn hàng...
              </div>
            ) : ordersResult.items.length > 0 ? (
              ordersResult.items.map((order) => (
                <div
                  key={order.id}
                  className="bg-white rounded-3xl border border-slate-200/80 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] hover:shadow-md hover:border-emerald-300 transition-all space-y-4 p-6 sm:p-7"
                >
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3.5 border-b border-slate-100">
                    <div>
                      <p className="font-mono font-bold text-sm text-slate-900 tracking-wider">
                        {order.orderCode}
                      </p>
                      <span className="text-[11px] text-slate-400 flex items-center gap-1.5 mt-0.5 font-medium">
                        <Calendar className="w-3.5 h-3.5" />
                        {formatDateTime(order.orderDate)}
                      </span>
                    </div>

                    <div className="flex items-center gap-2">
                      <span
                        className={`px-3 py-1 text-xs font-bold rounded-full border ${getStatusBadge(order.statusName)}`}
                      >
                        {order.statusName}
                      </span>
                      <span className="px-3 py-1 bg-slate-100 text-slate-700 text-xs font-semibold rounded-full">
                        {order.paymentStatusName}
                      </span>
                    </div>
                  </div>

                  {/* Items Snapshot */}
                  <div className="space-y-2 py-1">
                    {order.items.map((item) => (
                      <div
                        key={item.detailId}
                        className="flex justify-between items-center text-xs"
                      >
                        <span className="text-slate-700 font-semibold truncate pr-2">
                          {item.quantity} x {item.variantName} ({item.uoMName})
                        </span>
                        <span className="font-bold text-slate-900 shrink-0">
                          {formatVND(item.totalPrice)}
                        </span>
                      </div>
                    ))}
                  </div>

                  {/* Total & CTA */}
                  <div className="flex items-center justify-between pt-3 border-t border-slate-100">
                    <div>
                      <span className="text-xs text-slate-500 font-medium">
                        Tổng thanh toán:{" "}
                      </span>
                      <span className="font-black text-base text-emerald-800 ml-1">
                        {formatVND(order.totalAmount)}
                      </span>
                    </div>

                    <Link
                      href={`/tai-khoan/don-hang/${order.orderCode}`}
                      className="inline-flex items-center gap-1.5 px-4 py-2 bg-emerald-50 hover:bg-emerald-600 text-emerald-700 hover:text-white font-bold text-xs rounded-xl transition-all shadow-2xs"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>Xem chi tiết</span>
                    </Link>
                  </div>
                </div>
              ))
            ) : (
              <EmptyState
                icon={<Package className="w-8 h-8" />}
                title="Bạn chưa có đơn hàng nào"
                description="Hãy trải nghiệm đặt mua những mặt hàng nông sản tươi ngon cho gia đình hôm nay!"
                actionText="Mua sắm ngay"
                actionHref="/san-pham"
              />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
