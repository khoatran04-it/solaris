import React, { useState, useEffect } from "react";
import { RefreshCw, AlertCircle, Warehouse, Layers, Coins } from "lucide-react";
import { dashboardApi } from "../../api/dashboardApi";
import type { DashboardInventoryCapacityDto } from "../../types/dashboard";
import {
  CapacityGauge,
  DonutChart,
  RankedBarChart,
  DashCard,
  fmtVnd,
  fmtNum,
} from "../../components/dashboard/DashboardCharts";

export default function InventoryCapacityDashboard() {
  const [data, setData] = useState<DashboardInventoryCapacityDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setData(await dashboardApi.getInventoryCapacity());
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const comp = data?.inventoryCompartments;
  const totalQty =
    (comp?.availableQty ?? 0) +
    (comp?.reservedQty ?? 0) +
    (comp?.inQcQty ?? 0) +
    (comp?.damagedQty ?? 0);

  const compartmentData = comp
    ? [
        {
          label: "Khả dụng",
          value: comp.availableQty,
          percent: totalQty > 0 ? +(comp.availableQty / totalQty * 100).toFixed(1) : 0,
        },
        {
          label: "Giữ chỗ (Đơn hàng)",
          value: comp.reservedQty,
          percent: totalQty > 0 ? +(comp.reservedQty / totalQty * 100).toFixed(1) : 0,
        },
        {
          label: "Đang kiểm định (QC)",
          value: comp.inQcQty,
          percent: totalQty > 0 ? +(comp.inQcQty / totalQty * 100).toFixed(1) : 0,
        },
        {
          label: "Hàng hỏng / Hủy",
          value: comp.damagedQty,
          percent: totalQty > 0 ? +(comp.damagedQty / totalQty * 100).toFixed(1) : 0,
        },
      ]
    : [];

  const spaceData =
    data?.topSpaceConsumingProducts.map((p) => ({
      label: p.variantName,
      value: p.totalCbm,
    })) ?? [];

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Tồn kho & Sức chứa Kho hàng
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Giám sát tải trọng vật lý, dung tích lưu trữ CBM và trạng thái số dư 4 ngăn
            </p>
          </div>
          <button
            onClick={load}
            disabled={loading}
            className="flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-700 hover:text-slate-900 hover:bg-slate-50 transition-all shadow-xs disabled:opacity-50"
          >
            <RefreshCw size={13} className={loading ? "animate-spin" : ""} />
            Làm mới
          </button>
        </div>

        {loading ? (
          <div className="flex items-center justify-center h-72">
            <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            {/* Top Metrics Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1">
                    Kho đang hoạt động
                  </span>
                  <div className="text-2xl font-black text-slate-800">
                    {data?.totalActiveWarehouses ?? 0}
                  </div>
                  <span className="text-xs text-slate-400 mt-0.5 block">kho vật lý</span>
                </div>
                <div className="w-10 h-10 rounded-xl bg-slate-50 border border-slate-100 flex items-center justify-center text-slate-600">
                  <Warehouse size={18} />
                </div>
              </div>

              <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1">
                    Tổng Lượng Tồn Kho
                  </span>
                  <div className="text-2xl font-black text-slate-800">
                    {fmtNum(totalQty)}
                  </div>
                  <span className="text-xs text-slate-400 mt-0.5 block">đơn vị (cả 4 ngăn)</span>
                </div>
                <div className="w-10 h-10 rounded-xl bg-slate-50 border border-slate-100 flex items-center justify-center text-slate-600">
                  <Layers size={18} />
                </div>
              </div>

              <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-1">
                    Tổng Giá Trị Tồn Kho
                  </span>
                  <div className="text-2xl font-black text-slate-800">
                    {fmtVnd(data?.totalStockValue ?? 0)}
                  </div>
                  <span className="text-xs text-slate-400 mt-0.5 block">ước tính giá niêm yết</span>
                </div>
                <div className="w-10 h-10 rounded-xl bg-slate-50 border border-slate-100 flex items-center justify-center text-slate-600">
                  <Coins size={18} />
                </div>
              </div>
            </div>

            {/* Warehouse Capacity Gauges */}
            <DashCard
              title="Sức chứa Lưu trữ Từng kho"
              subtitle="Tỷ lệ lấp đầy Thể tích (m³) và Tải trọng sàn (kg) theo thời gian thực"
            >
              {data?.warehouseCapacities.length === 0 ? (
                <p className="text-center text-slate-400 text-xs py-8">
                  Chưa có kho nào được cấu hình sức chứa
                </p>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
                  {data?.warehouseCapacities.map((w) => (
                    <div
                      key={w.warehouseId}
                      className="flex flex-col items-center bg-slate-50/70 rounded-2xl p-4 border border-slate-200/70"
                    >
                      <div className="text-center mb-3">
                        <div className="text-xs font-bold text-slate-800">
                          {w.warehouseName}
                        </div>
                        <div className="text-[11px] font-mono text-slate-400">
                          {w.warehouseCode}
                        </div>
                      </div>

                      <div className="flex items-center justify-center gap-6 w-full">
                        <CapacityGauge
                          label="Thể tích CBM"
                          value={w.occupiedCbm}
                          max={w.totalCbm}
                          percent={w.occupancyCbmPercent}
                          unit="m³"
                          status={w.status}
                          warningThreshold={w.warningThresholdPercent}
                        />
                        <CapacityGauge
                          label="Tải trọng"
                          value={w.occupiedWeightKg}
                          max={w.maxWeightKg}
                          percent={w.occupancyWeightPercent}
                          unit="kg"
                          status={w.status}
                          warningThreshold={w.warningThresholdPercent}
                        />
                      </div>

                      {w.status !== "Safe" && (
                        <div
                          className={`mt-3 w-full text-center text-[11px] font-semibold py-1 px-2.5 rounded-lg flex items-center justify-center gap-1.5 ${
                            w.status === "Critical"
                              ? "bg-rose-100/80 text-rose-700"
                              : "bg-amber-100/80 text-amber-700"
                          }`}
                        >
                          <AlertCircle size={12} />
                          {w.status === "Critical"
                            ? "Nguy hiểm: Đã vượt 95% sức chứa"
                            : "Cảnh báo: Đang tiệm cận ngưỡng tối đa"}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </DashCard>

            {/* Compartments & Top Space */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
              <DashCard
                title="Cơ cấu Phân bổ Tồn kho"
                subtitle="Số lượng hàng hóa theo 4 trạng thái lưu kho"
              >
                <DonutChart data={compartmentData} size={160} formatTooltip={fmtNum} />
              </DashCard>

              <DashCard
                title="Top Sản phẩm Chiếm Thể tích Kho"
                subtitle="Các SKU chiếm nhiều mét khối lưu trữ nhất"
              >
                <RankedBarChart
                  data={spaceData}
                  color="#8b5cf6"
                  formatValue={(v) => `${v.toFixed(2)} m³`}
                  maxItems={5}
                  height={220}
                />
              </DashCard>
            </div>

            {/* Low Stock Alerts */}
            {(data?.lowStockAlerts?.length ?? 0) > 0 && (
              <DashCard
                title="Cảnh báo Tồn kho Thấp"
                subtitle="Các sản phẩm có số dư khả dụng thấp hơn định mức an toàn (Safety Stock)"
              >
                <div className="overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead>
                      <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase tracking-wider text-[10px]">
                        <th className="text-left pb-2.5 pr-4 font-bold">Sản phẩm</th>
                        <th className="text-left pb-2.5 pr-4 font-bold">Mã SKU</th>
                        <th className="text-left pb-2.5 pr-4 font-bold">Kho lưu trữ</th>
                        <th className="text-right pb-2.5 font-bold">Tồn khả dụng</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {data?.lowStockAlerts.map((a, i) => (
                        <tr key={i} className="hover:bg-rose-50/20">
                          <td className="py-2.5 pr-4 font-semibold text-slate-800">
                            {a.variantName}
                          </td>
                          <td className="py-2.5 pr-4 text-slate-500 font-mono text-[11px]">
                            {a.variantCode}
                          </td>
                          <td className="py-2.5 pr-4 text-slate-600">{a.warehouseName}</td>
                          <td className="py-2.5 text-right font-bold text-rose-600">
                            {fmtNum(a.availableQty)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </DashCard>
            )}
          </>
        )}
      </div>
    </div>
  );
}
