"use client";

import React from "react";
import Link from "next/link";
import {
  Package,
  Truck,
  MapPin,
  Calendar,
  CreditCard,
  ChevronRight,
  CheckCircle2,
  Clock,
  AlertCircle,
} from "lucide-react";
import { AiOrderTracking } from "@/types/chat";
import { formatVND, formatDate } from "@/lib/utils";

interface OrderTrackingCardProps {
  tracking: AiOrderTracking;
}

export default function OrderTrackingCard({
  tracking,
}: OrderTrackingCardProps) {
  // Determine status badge color
  const getStatusBadge = () => {
    const name = tracking.statusName || "Đang xử lý";
    if (
      tracking.status === 4 ||
      name.includes("thành công") ||
      name.includes("Đã giao")
    ) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-200">
          <CheckCircle2 className="w-3 h-3 text-emerald-600" />
          {name}
        </span>
      );
    }
    if (tracking.status === 5 || name.includes("Hủy")) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-100 text-rose-800 border border-rose-200">
          <AlertCircle className="w-3 h-3 text-rose-600" />
          {name}
        </span>
      );
    }
    if (tracking.status === 3 || name.includes("giao")) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-sky-100 text-sky-800 border border-sky-200">
          <Truck className="w-3 h-3 text-sky-600" />
          {name}
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-800 border border-amber-200">
        <Clock className="w-3 h-3 text-amber-600" />
        {name}
      </span>
    );
  };

  return (
    <div className="bg-white rounded-2xl border border-emerald-200/90 shadow-sm p-4 space-y-3.5 text-xs">
      {/* Header */}
      <div className="flex items-center justify-between pb-2.5 border-b border-slate-100">
        <div className="flex items-center gap-1.5 font-bold text-slate-900 text-xs">
          <Package className="w-4 h-4 text-emerald-600" />
          <span className="font-mono">{tracking.orderCode}</span>
        </div>
        {getStatusBadge()}
      </div>

      {/* Date & Payment & Delivery Overview */}
      <div className="bg-slate-50 rounded-xl p-3 space-y-2 text-[11px]">
        <div className="flex justify-between items-center text-slate-600">
          <span className="flex items-center gap-1">
            <Calendar className="w-3.5 h-3.5 text-slate-400" />
            Ngày đặt:
          </span>
          <span className="font-semibold text-slate-800">
            {formatDate(tracking.orderDate)}
          </span>
        </div>

        <div className="flex justify-between items-center text-slate-600">
          <span className="flex items-center gap-1">
            <CreditCard className="w-3.5 h-3.5 text-slate-400" />
            Thanh toán:
          </span>
          <span className="font-semibold text-slate-800">
            {tracking.paymentMethodName || "VNPay"} (
            {tracking.paymentStatusName || "Đã thanh toán"})
          </span>
        </div>

        <div className="flex justify-between items-center text-slate-600">
          <span className="flex items-center gap-1">
            <Truck className="w-3.5 h-3.5 text-slate-400" />
            Vận chuyển:
          </span>
          <span className="font-semibold text-emerald-700">
            {tracking.shippingProvider || "GHN Express (2H Nông Sản)"}
          </span>
        </div>

        {tracking.deliveryAddress && (
          <div className="pt-1.5 border-t border-slate-200/60 flex items-start gap-1 text-slate-600">
            <MapPin className="w-3.5 h-3.5 text-emerald-600 shrink-0 mt-0.5" />
            <span className="line-clamp-2 text-[10px] text-slate-700 font-medium leading-relaxed">
              {tracking.receiverName
                ? `${tracking.receiverName} (${tracking.receiverPhone || ""}) - `
                : ""}
              {tracking.deliveryAddress}
            </span>
          </div>
        )}
      </div>

      {/* Product Items Mini List */}
      {tracking.items && tracking.items.length > 0 && (
        <div className="space-y-1.5 max-h-36 overflow-y-auto pr-1">
          <p className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">
            Danh sách sản phẩm
          </p>
          {tracking.items.map((item, idx) => (
            <div
              key={idx}
              className="flex justify-between items-center py-1 border-b border-slate-50 last:border-0 text-[11px]"
            >
              <div className="flex-1 min-w-0 pr-2">
                <p className="font-semibold text-slate-800 truncate">
                  {item.variantName}
                </p>
                <p className="text-[10px] text-slate-400">
                  SL: {item.quantity} {item.uoMName || "Kg"}
                </p>
              </div>
              <span className="font-bold text-slate-900 shrink-0">
                {formatVND(item.totalPrice)}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Pricing Breakdown */}
      <div className="pt-2 border-t border-slate-100 space-y-1 text-xs">
        <div className="flex justify-between text-slate-500 text-[11px]">
          <span>Tạm tính tiền hàng:</span>
          <span>{formatVND(tracking.subTotal)}</span>
        </div>
        <div className="flex justify-between text-slate-500 text-[11px]">
          <span>Phí vận chuyển:</span>
          <span>
            {tracking.shippingFee === 0 ? (
              <strong className="text-emerald-700">Miễn phí</strong>
            ) : (
              formatVND(tracking.shippingFee)
            )}
          </span>
        </div>
        <div className="flex justify-between items-baseline pt-1 border-t border-slate-100 font-bold">
          <span className="text-slate-800">Tổng thanh toán:</span>
          <span className="text-emerald-700 font-black text-sm">
            {formatVND(tracking.totalAmount)}
          </span>
        </div>
      </div>

      {/* Action Link to Order Detail Page */}
      <Link
        href={`/tai-khoan/don-hang/${tracking.orderCode}`}
        className="w-full py-2 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-xl font-bold text-center flex items-center justify-center gap-1.5 shadow-sm transition-all"
      >
        <span>Xem Chi Tiết Đơn Hàng</span>
        <ChevronRight className="w-3.5 h-3.5" />
      </Link>
    </div>
  );
}
