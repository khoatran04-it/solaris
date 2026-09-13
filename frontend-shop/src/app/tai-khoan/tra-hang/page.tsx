"use client";

import React, { useState, useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { RotateCcw, Plus } from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import shopOrderApi from "@/api/shopOrderApi";
import shopReturnApi from "@/api/shopReturnApi";
import { formatVND, formatDateTime } from "@/lib/utils";
import { PagedResult } from "@/types/common";
import { ShopReturn } from "@/types/return";
import { ShopOrder } from "@/types/order";
import AccountSidebar from "@/components/account/AccountSidebar";
import EmptyState from "@/components/common/EmptyState";

function TraHangContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const defaultOrderCode = searchParams.get("orderCode") || "";

  const { isAuthenticated, initAuth } = useAuthStore();
  const [returnsResult, setReturnsResult] = useState<PagedResult<ShopReturn>>({
    items: [],
    totalRecords: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 10,
  });

  const [showCreateForm, setShowCreateForm] = useState(
    Boolean(defaultOrderCode),
  );
  const [orderCode, setOrderCode] = useState(defaultOrderCode);
  const [reason, setReason] = useState("");
  const [targetOrder, setTargetOrder] = useState<ShopOrder | null>(null);
  const [selectedItems, setSelectedItems] = useState<{
    [variantId: number]: { qty: number; reason: string; uoMId: number };
  }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    initAuth();
  }, [initAuth]);

  const loadReturns = () => {
    shopReturnApi
      .getAll(1, 10)
      .then(setReturnsResult)
      .catch(() => {});
  };

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/dang-nhap?redirect=/tai-khoan/tra-hang");
      return;
    }
    loadReturns();
  }, [isAuthenticated, router]);

  const handleSearchOrder = async () => {
    if (!orderCode.trim()) return;
    try {
      const ord: ShopOrder = await shopOrderApi.getByCode(orderCode.trim());

      // Kiểm tra thời hạn 12 giờ kể từ khi nhận hàng
      if (ord.deliveredAt) {
        const deliveryTime = new Date(ord.deliveredAt).getTime();
        const diffHours = (Date.now() - deliveryTime) / (1000 * 60 * 60);
        if (diffHours > 12) {
          alert(
            `Đơn hàng ${ord.orderCode} đã nhận cách đây hơn 12 giờ. Chính sách nông sản tươi Solaris chỉ hỗ trợ đổi/trả trong vòng 12 giờ kể từ khi nhận hàng.`,
          );
          setTargetOrder(null);
          return;
        }
      }

      // Kiểm tra xem đơn hàng đã có yêu cầu đổi trả chưa
      if (ord.hasReturnRequest) {
        alert(
          `Đơn hàng ${ord.orderCode} đã gửi yêu cầu đổi trả (Mã: ${ord.returnCode || "RET"}, Trạng thái: ${ord.returnStatusName || "Chờ tiếp nhận"}). Vui lòng không gửi lặp lại.`,
        );
        setTargetOrder(null);
        return;
      }

      setTargetOrder(ord);
      const init: any = {};
      ord.items.forEach((i) => {
        init[i.variantId] = {
          qty: i.quantity,
          reason: "Dập nát hoặc không đạt độ tươi",
          uoMId: i.uoMId,
        };
      });
      setSelectedItems(init);
    } catch {
      alert("Không tìm thấy mã đơn hàng hợp lệ.");
    }
  };

  const handleCreateReturn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetOrder) return;

    setIsSubmitting(true);
    try {
      const items = Object.entries(selectedItems)
        .map(([variantId, data]) => ({
          variantId: parseInt(variantId, 10),
          uoMId: data.uoMId,
          batchId: 0,
          returnedQuantity: data.qty,
          reason: data.reason,
        }))
        .filter((i) => i.returnedQuantity > 0);

      await shopReturnApi.create({
        orderCode: targetOrder.orderCode,
        reason: reason.trim(),
        items,
      });

      setShowCreateForm(false);
      setTargetOrder(null);
      setReason("");
      loadReturns();
    } catch (error: any) {
      alert(error?.message || "Không thể gửi yêu cầu đổi/trả.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
      {/* Sidebar */}
      <AccountSidebar activeTab="returns" className="lg:col-span-4" />

      {/* Content */}
      <div className="lg:col-span-8 space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider">
            Danh Sách Phiếu Đổi Trả ({returnsResult.totalRecords})
          </h2>

          {!showCreateForm && (
            <button
              onClick={() => setShowCreateForm(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl text-xs font-extrabold transition-all shadow-md shadow-emerald-600/20 active:scale-95 cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Tạo yêu cầu mới</span>
            </button>
          )}
        </div>

        {showCreateForm && (
          <div className="bg-white rounded-3xl border border-emerald-300 p-6 sm:p-8 shadow-md space-y-5 animate-in fade-in">
            <h3 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider pb-3 border-b border-slate-100 flex items-center gap-2">
              <span className="w-1.5 h-4 bg-emerald-600 rounded-full"></span>
              Tạo Phiếu Đổi / Trả Hàng Nông Sản
            </h3>

            <div className="flex gap-2">
              <input
                type="text"
                placeholder="Nhập mã đơn hàng (VD: ORD-20260821-XXXX)..."
                value={orderCode}
                onChange={(e) => setOrderCode(e.target.value)}
                className="flex-1 text-xs px-3.5 h-11 bg-slate-50 border border-slate-200 rounded-xl font-medium focus:outline-none focus:border-emerald-600 focus:bg-white"
              />
              <button
                type="button"
                onClick={handleSearchOrder}
                className="px-5 h-11 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-bold transition-colors cursor-pointer"
              >
                Kiểm tra đơn
              </button>
            </div>

            {targetOrder && (
              <form onSubmit={handleCreateReturn} className="space-y-4 pt-2">
                <div className="space-y-2">
                  <label className="text-xs font-extrabold text-slate-800 uppercase tracking-wide block">
                    Chọn mặt hàng và số lượng cần trả:
                  </label>
                  <div className="space-y-2">
                    {targetOrder.items.map((item) => (
                      <div
                        key={item.detailId}
                        className="p-3.5 bg-slate-50/80 rounded-2xl flex items-center justify-between text-xs gap-4 border border-slate-100"
                      >
                        <div className="flex-1 truncate">
                          <p className="font-bold text-slate-900 truncate">
                            {item.variantName}
                          </p>
                          <p className="text-[11px] text-slate-500 font-medium">
                            Đã nhận: {item.quantity} {item.uoMName}
                          </p>
                        </div>

                        <div className="flex items-center gap-2">
                          <span className="text-[11px] text-slate-600 font-semibold">
                            SL trả:
                          </span>
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
                                  reason:
                                    selectedItems[item.variantId]?.reason ||
                                    "Không đạt chất lượng",
                                },
                              });
                            }}
                            className="w-16 h-9 bg-white border border-slate-200 rounded-xl text-center font-bold"
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="space-y-1">
                  <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
                    Lý do đổi trả chi tiết *
                  </label>
                  <textarea
                    placeholder="Mô tả cụ thể tình trạng hàng hóa (dập nát, héo, sai quy cách...)"
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    className="w-full text-xs p-3.5 bg-slate-50 border border-slate-200 rounded-2xl h-24 font-medium focus:outline-none focus:border-emerald-600 focus:bg-white"
                    required
                  />
                </div>

                <div className="flex items-center gap-3 pt-2">
                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="px-6 h-11 bg-emerald-600 hover:bg-emerald-700 text-white rounded-2xl text-xs font-extrabold transition-all shadow-md shadow-emerald-600/20 active:scale-98 cursor-pointer disabled:opacity-50"
                  >
                    {isSubmitting ? "Đang gửi..." : "Gửi yêu cầu trả hàng"}
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowCreateForm(false)}
                    className="px-5 h-11 bg-slate-100 text-slate-700 rounded-2xl text-xs font-bold hover:bg-slate-200 transition-colors cursor-pointer"
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
                className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] space-y-3"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-slate-100">
                  <div>
                    <p className="font-mono font-bold text-xs text-slate-900">
                      {ret.returnCode}
                    </p>
                    <p className="text-[11px] text-slate-500 font-medium">
                      Đơn hàng: <strong>{ret.orderCode}</strong> •{" "}
                      {formatDateTime(ret.returnDate)}
                    </p>
                  </div>

                  {ret.status === 1 && (
                    <span className="px-3 py-1 bg-amber-50 text-amber-800 border border-amber-200 text-xs font-bold rounded-full">
                      Chờ tiếp nhận
                    </span>
                  )}
                  {ret.status === 2 && (
                    <span className="px-3 py-1 bg-blue-50 text-blue-800 border border-blue-200 text-xs font-bold rounded-full">
                      Đã duyệt — Chờ nhận hàng tại kho
                    </span>
                  )}
                  {ret.status === 6 && (
                    <span className="px-3 py-1 bg-indigo-50 text-indigo-800 border border-indigo-200 text-xs font-bold rounded-full">
                      Đang thu hồi hàng
                    </span>
                  )}
                  {ret.status === 3 && (
                    <span className="px-3 py-1 bg-purple-50 text-purple-800 border border-purple-200 text-xs font-bold rounded-full">
                      Đang kiểm định & Xử lý
                    </span>
                  )}
                  {ret.status === 4 && (
                    <span className="px-3 py-1 bg-emerald-50 text-emerald-800 border border-emerald-200 text-xs font-bold rounded-full">
                      Hoàn tất & Đã hoàn tiền
                    </span>
                  )}
                  {ret.status === 5 && (
                    <span className="px-3 py-1 bg-rose-50 text-rose-800 border border-rose-200 text-xs font-bold rounded-full">
                      Từ chối trả hàng
                    </span>
                  )}
                  {ret.statusName && ![1, 2, 3, 4, 5, 6].includes(ret.status) && (
                    <span className="px-3 py-1 bg-slate-100 text-slate-800 border border-slate-200 text-xs font-bold rounded-full">
                      {ret.statusName}
                    </span>
                  )}
                </div>

                <div className="space-y-1.5 text-xs text-slate-600">
                  <p>
                    Lý do yêu cầu:{" "}
                    <strong className="text-slate-800">
                      {ret.reason || "Đổi trả bảo hành"}
                    </strong>
                  </p>
                  <p>
                    Số tiền hoàn dự kiến:{" "}
                    <strong className="text-emerald-700 font-black">
                      {formatVND(ret.refundAmount)}
                    </strong>
                  </p>
                  {ret.inspectionNotes && (
                    <p className="text-slate-500 italic bg-slate-50 p-2.5 rounded-xl border border-slate-100">
                      Ghi chú QC: {ret.inspectionNotes}
                    </p>
                  )}
                </div>
              </div>
            ))
          ) : (
            <EmptyState
              icon={<RotateCcw className="w-7 h-7" />}
              title="Bạn chưa có yêu cầu đổi trả nào"
              description="Solaris cam kết bảo hành 1 đổi 1 hoặc hoàn tiền trong 12H nếu sản phẩm dập nát, héo úa hoặc không đạt chuẩn an toàn."
            />
          )}
        </div>
      </div>
    </div>
  );
}

export default function TraHangPage() {
  return (
    <div className="min-h-[85vh] bg-slate-50/40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
            Đổi Trả Hàng & Hoàn Tiền (RMA)
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Chính sách cam kết đổi trả trong 12H nếu hàng hóa không đạt chuẩn
            chất lượng tươi ngon
          </p>
        </div>

        <Suspense
          fallback={
            <div className="text-xs text-slate-500">
              Đang tải danh sách đổi trả...
            </div>
          }
        >
          <TraHangContent />
        </Suspense>
      </div>
    </div>
  );
}
