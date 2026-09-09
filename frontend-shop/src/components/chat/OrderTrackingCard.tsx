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
  Phone,
  RotateCcw,
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
      tracking.status === 6 ||
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
    if (tracking.status === 7 || name.includes("Hủy")) {
      return (
        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-100 text-rose-800 border border-rose-200">
          <AlertCircle className="w-3 h-3 text-rose-600" />
          {name}
        </span>
      );
    }
    if (tracking.status === 5 || name.includes("giao")) {
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

      {/* Cancellation / Rejection Banner from Warehouse */}
      {(tracking.status === 7 || tracking.cancellationReason) && (
        <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-900 text-[11px] space-y-1">
          <div className="flex items-center gap-1.5 font-bold text-rose-700">
            <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
            <span>Kho từ chối / Hủy đơn hàng</span>
          </div>
          {tracking.cancellationReason ? (
            <p className="text-[10.5px] text-rose-800 leading-relaxed font-medium pl-5">
              Lý do hủy:{" "}
              <strong className="text-rose-950">
                {tracking.cancellationReason}
              </strong>
            </p>
          ) : (
            <p className="text-[10px] text-rose-700 pl-5">
              Đơn hàng đã được hủy trên hệ thống.
            </p>
          )}
        </div>
      )}

      {/* TMS Cold-Chain Delivery Trip Details */}
      {tracking.deliveryTripCode && (
        <div className="p-3 rounded-xl bg-sky-50/80 border border-sky-200 text-sky-950 text-[11px] space-y-2">
          <div className="flex items-center justify-between font-bold">
            <span className="flex items-center gap-1.5 text-sky-900">
              <Truck className="w-4 h-4 text-sky-600" />
              Chuyến xe TMS:{" "}
              <span className="font-mono text-sky-800">
                {tracking.deliveryTripCode}
              </span>
            </span>
            {tracking.isColdChainVehicle && (
              <span className="px-1.5 py-0.5 rounded bg-sky-200 text-sky-800 text-[9px] font-bold">
                Thùng lạnh 2-8°C
              </span>
            )}
          </div>

          <div className="grid grid-cols-2 gap-2 text-[10.5px] text-sky-900 pt-1.5 border-t border-sky-200/60">
            {tracking.licensePlate && (
              <div>
                Biển số xe:{" "}
                <strong className="text-sky-950 font-mono">
                  {tracking.licensePlate}
                </strong>
              </div>
            )}
            {tracking.driverName && (
              <div>
                Tài xế:{" "}
                <strong className="text-sky-950">{tracking.driverName}</strong>
              </div>
            )}
          </div>

          {tracking.driverPhone && (
            <div className="pt-1 flex items-center justify-between text-[10.5px] border-t border-sky-200/40">
              <span className="text-sky-700">Liên hệ tài xế giao:</span>
              <a
                href={`tel:${tracking.driverPhone}`}
                className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-lg bg-sky-600 hover:bg-sky-700 text-white font-bold transition-all shadow-xs"
              >
                <Phone className="w-3 h-3" />
                <span>{tracking.driverPhone} (Gọi)</span>
              </a>
            </div>
          )}
        </div>
      )}

      {/* Return / Refund Info (nếu có RMA) */}
      {tracking.returnCode && (
        <div className="p-2.5 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-[11px] flex justify-between items-center">
          <div className="flex items-center gap-1.5">
            <RotateCcw className="w-3.5 h-3.5 text-amber-600 shrink-0" />
            <span>
              Mã yêu cầu đổi trả:{" "}
              <strong className="font-mono">{tracking.returnCode}</strong>
            </span>
          </div>
          {tracking.refundAmount !== undefined && tracking.refundAmount > 0 && (
            <span className="font-bold text-amber-700">
              Hoàn: {formatVND(tracking.refundAmount)}
            </span>
          )}
        </div>
      )}

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
            {tracking.shippingProvider ||
              "Solaris Cold-Chain Express (TMS 2°C - 8°C)"}
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
