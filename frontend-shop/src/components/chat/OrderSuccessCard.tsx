import React from "react";
import Link from "next/link";
import { CheckCircle2, ExternalLink, Package } from "lucide-react";
import { formatVND } from "@/lib/utils";

interface OrderSuccessCardProps {
  payload: {
    orderId?: number;
    orderCode: string;
    totalAmount: number;
    paymentMethodName?: string;
    paymentUrl?: string;
  };
}

export default function OrderSuccessCard({ payload }: OrderSuccessCardProps) {
  return (
    <div className="bg-emerald-50 border border-emerald-200 rounded-2xl p-4 space-y-3 text-xs">
      <div className="flex items-center gap-2 text-emerald-800 font-bold">
        <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />
        <span>Đặt hàng thành công qua AI Chatbot!</span>
      </div>

      <div className="p-3 bg-white rounded-xl border border-emerald-100 space-y-1.5">
        <div className="flex justify-between">
          <span className="text-slate-500">Mã đơn hàng:</span>
          <span className="font-mono font-bold text-slate-900">
            {payload.orderCode}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-slate-500">Tổng tiền:</span>
          <span className="font-bold text-emerald-700">
            {formatVND(payload.totalAmount)}
          </span>
        </div>
        {payload.paymentMethodName && (
          <div className="flex justify-between">
            <span className="text-slate-500">Hình thức:</span>
            <span className="font-medium text-slate-700">
              {payload.paymentMethodName}
            </span>
          </div>
        )}
      </div>

      <div className="flex items-center gap-2 pt-1">
        {payload.paymentUrl ? (
          <a
            href={payload.paymentUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="flex-1 py-2 bg-blue-600 hover:bg-blue-500 text-white rounded-xl font-bold text-center flex items-center justify-center gap-1 shadow-sm"
          >
            <span>Cổng VNPay</span>
            <ExternalLink className="w-3.5 h-3.5" />
          </a>
        ) : null}

        <Link
          href={`/tai-khoan/don-hang/${payload.orderCode}`}
          className="flex-1 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl font-bold text-center flex items-center justify-center gap-1 shadow-sm"
        >
          <Package className="w-3.5 h-3.5" />
          <span>Xem đơn hàng</span>
        </Link>
      </div>
    </div>
  );
}
