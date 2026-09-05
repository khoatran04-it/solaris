import React, { useState, useEffect } from 'react';
import { RefreshCw } from 'lucide-react';
import { dashboardApi } from '../../api/dashboardApi';
import type { DashboardSalesGeographyDto, DashboardPeriod } from '../../types/dashboard';
import {
  RankedBarChart,
  DonutChart,
  DashCard,
  fmtVnd,
  fmtNum,
} from '../../components/dashboard/DashboardCharts';

const PERIODS: { label: string; value: DashboardPeriod }[] = [
  { label: '30 ngày', value: '30days' },
  { label: 'Năm nay', value: 'year' },
];

export default function SalesGeographyDashboard() {
  const [period, setPeriod] = useState<DashboardPeriod>('30days');
  const [data, setData] = useState<DashboardSalesGeographyDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setData(await dashboardApi.getSalesGeography(period));
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [period]);

  const topProductsData =
    data?.topProducts.map((p) => ({
      label: p.name,
      value: p.totalRevenue,
      percent: p.revenuePercent,
    })) ?? [];

  const categoryData =
    data?.categoryBreakdown.map((c) => ({
      label: c.categoryGroupName,
      value: c.totalRevenue,
      percent: c.percent,
    })) ?? [];

  const geoData =
    data?.geographyBreakdown.map((g) => ({
      label: g.provinceName,
      value: g.orderCount,
      percent: g.percent,
    })) ?? [];

  const tierData =
    data?.customerTierBreakdown.map((t) => ({
      label: t.tierName,
      value: t.totalRevenue,
      percent: t.percent,
    })) ?? [];

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Doanh số & Phân bổ Địa lý
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Phân tích sản phẩm bán chạy, cơ cấu ngành hàng và khu vực giao dịch trọng điểm
            </p>
          </div>
          <div className="flex items-center gap-2">
            <div className="flex bg-white border border-slate-200 rounded-xl p-1 shadow-xs">
              {PERIODS.map((p) => (
                <button
                  key={p.value}
                  onClick={() => setPeriod(p.value)}
                  className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition-all ${
                    period === p.value
                      ? 'bg-slate-900 text-white shadow-xs'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                  }`}
                >
                  {p.label}
                </button>
              ))}
            </div>
            <button
              onClick={load}
              disabled={loading}
              title="Làm mới dữ liệu"
              className="p-2 bg-white border border-slate-200 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-all shadow-xs disabled:opacity-50"
            >
              <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            </button>
          </div>
        </div>

        {loading ? (
          <div className="flex items-center justify-center h-72">
            <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
              {/* Top Products */}
              <DashCard
                title={`Top ${data?.topProducts.length ?? 0} Sản phẩm Bán chạy`}
                subtitle="Xếp hạng các SKU đóng góp doanh thu cao nhất"
              >
                <RankedBarChart
                  data={topProductsData}
                  color="#f59e0b"
                  formatValue={fmtVnd}
                  height={240}
                />
              </DashCard>

              {/* Category Breakdown */}
              <DashCard
                title="Cơ cấu Ngành hàng"
                subtitle="Tỷ trọng doanh thu theo nhóm danh mục sản phẩm"
              >
                <DonutChart data={categoryData} size={170} />
              </DashCard>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
              {/* Geography */}
              <DashCard
                title="Phân bổ Doanh số theo Tỉnh / Thành phố"
                subtitle="Số lượng đơn hàng giao đến từng địa phương"
              >
                <RankedBarChart
                  data={geoData}
                  color="#3b82f6"
                  formatValue={(v) => `${fmtNum(v)} đơn`}
                  maxItems={8}
                  height={240}
                />
              </DashCard>

              {/* Customer Tier */}
              <DashCard
                title="Phân khúc Khách hàng"
                subtitle="Đóng góp doanh thu và cơ cấu theo phân hạng thành viên"
              >
                <DonutChart data={tierData} size={170} />

                {/* Detail Table */}
                <div className="mt-5 pt-4 border-t border-slate-100 overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead>
                      <tr className="text-slate-400 font-semibold uppercase tracking-wider text-[10px] border-b border-slate-100">
                        <th className="text-left pb-2 font-bold">Hạng</th>
                        <th className="text-right pb-2 font-bold">Số khách</th>
                        <th className="text-right pb-2 font-bold">Số đơn</th>
                        <th className="text-right pb-2 font-bold">Doanh số</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {(data?.customerTierBreakdown ?? []).map((t, i) => (
                        <tr key={i} className="hover:bg-slate-50/50">
                          <td className="py-2 font-semibold text-slate-800">{t.tierName}</td>
                          <td className="py-2 text-right text-slate-600 font-medium">
                            {fmtNum(t.customerCount)}
                          </td>
                          <td className="py-2 text-right text-slate-600 font-medium">
                            {fmtNum(t.orderCount)}
                          </td>
                          <td className="py-2 text-right font-bold text-slate-900">
                            {fmtVnd(t.totalRevenue)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </DashCard>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
