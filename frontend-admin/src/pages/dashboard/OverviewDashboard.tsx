import React, { useState, useEffect } from 'react';
import { TrendingUp, ShoppingBag, CheckCircle2, DollarSign, RefreshCw } from 'lucide-react';
import { dashboardApi } from '../../api/dashboardApi';
import type { DashboardOverviewDto, DashboardPeriod } from '../../types/dashboard';
import {
  KpiMetricCard,
  AreaLineChart,
  DonutChart,
  RankedBarChart,
  DashCard,
  fmtVnd,
  fmtNum,
} from '../../components/dashboard/DashboardCharts';

const PERIODS: { label: string; value: DashboardPeriod }[] = [
  { label: 'Hôm nay', value: 'today' },
  { label: '7 ngày', value: '7days' },
  { label: '30 ngày', value: '30days' },
  { label: 'Năm nay', value: 'year' },
];

const STATUS_LABEL: Record<string, string> = {
  Draft: 'Nháp',
  Pending: 'Chờ duyệt',
  Confirmed: 'Đã xác nhận',
  Processing: 'Đang xử lý',
  Shipping: 'Đang giao',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
};

const STATUS_COLOR: Record<string, string> = {
  Completed: 'bg-emerald-50 text-emerald-700 border border-emerald-200/60',
  Shipping: 'bg-blue-50 text-blue-700 border border-blue-200/60',
  Pending: 'bg-amber-50 text-amber-700 border border-amber-200/60',
  Cancelled: 'bg-rose-50 text-rose-700 border border-rose-200/60',
  Processing: 'bg-indigo-50 text-indigo-700 border border-indigo-200/60',
  default: 'bg-slate-100 text-slate-700 border border-slate-200/60',
};

export default function OverviewDashboard() {
  const [period, setPeriod] = useState<DashboardPeriod>('30days');
  const [data, setData] = useState<DashboardOverviewDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      const res = await dashboardApi.getOverview(period);
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [period]);

  const timelineData =
    data?.revenueTimeline.map((p) => ({ label: p.label, value: p.revenue })) ?? [];
  const paymentData =
    data?.paymentMethodBreakdown.map((p) => ({
      label:
        p.method === 'COD'
          ? 'COD (Thu hộ)'
          : p.method === 'EWallet' || p.method === 'VNPay'
            ? 'Ví điện tử / VNPay'
            : p.method === 'BankTransfer'
              ? 'Chuyển khoản'
              : p.method,
      value: p.amount,
      percent: p.percent,
    })) ?? [];
  const statusData =
    data?.orderStatusPipeline.map((s) => ({
      label: STATUS_LABEL[s.status] ?? s.status,
      value: s.count,
    })) ?? [];

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Bàn làm việc Điều hành
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Tổng hợp nhịp đập kinh doanh, tiến độ xử lý đơn hàng và các chỉ số sức khỏe trọng yếu
            </p>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
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
            {/* KPI Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <KpiMetricCard
                title="Tổng Doanh Thu"
                value={fmtVnd(data?.totalRevenue ?? 0)}
                subtitle="Đơn hàng hoàn tất"
                badge={
                  data && data.revenueGrowthPercent !== 0
                    ? `${data.revenueGrowthPercent > 0 ? '+' : ''}${data.revenueGrowthPercent}%`
                    : undefined
                }
                badgePositive={(data?.revenueGrowthPercent ?? 0) >= 0}
                icon={<DollarSign size={16} />}
              />
              <KpiMetricCard
                title="Tổng Đơn Hàng"
                value={fmtNum(data?.totalOrders ?? 0)}
                subtitle={`${fmtNum(data?.completedOrders ?? 0)} đơn hoàn tất`}
                icon={<ShoppingBag size={16} />}
              />
              <KpiMetricCard
                title="Giá Trị TB Đơn"
                value={fmtVnd(data?.averageOrderValue ?? 0)}
                subtitle="Giá trị trung bình mỗi đơn"
                icon={<TrendingUp size={16} />}
              />
              <KpiMetricCard
                title="Tỉ Lệ Hoàn Tất"
                value={`${data?.fulfillmentRatePercent ?? 0}%`}
                subtitle="Tỷ lệ hoàn thành đơn bán"
                icon={<CheckCircle2 size={16} />}
              />
            </div>

            {/* Revenue Trend AreaChart */}
            <DashCard
              title="Xu hướng Doanh thu"
              subtitle="Thống kê giá trị đơn hàng hoàn tất theo mốc thời gian"
            >
              <AreaLineChart data={timelineData} color="#f59e0b" height={220} />
            </DashCard>

            {/* Payment & Order Pipeline */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
              <DashCard
                title="Cơ cấu Thanh toán"
                subtitle="Tỷ trọng doanh thu theo hình thức thanh toán"
              >
                <DonutChart data={paymentData} size={160} />
              </DashCard>
              <DashCard
                title="Phễu Trạng thái Đơn hàng"
                subtitle="Số lượng đơn theo các giai đoạn xử lý"
                className="lg:col-span-2"
              >
                <RankedBarChart
                  data={statusData}
                  color="#3b82f6"
                  formatValue={fmtNum}
                  height={200}
                />
              </DashCard>
            </div>

            {/* Recent Orders Table */}
            <DashCard
              title="Đơn hàng Mới nhất"
              subtitle="Danh sách các giao dịch phát sinh gần đây"
            >
              <div className="overflow-x-auto">
                <table className="w-full text-xs">
                  <thead>
                    <tr className="border-b border-slate-100 text-slate-500 font-semibold uppercase tracking-wider text-[11px]">
                      <th className="text-left pb-3 pr-4 font-bold">Mã đơn</th>
                      <th className="text-left pb-3 pr-4 font-bold">Khách hàng</th>
                      <th className="text-left pb-3 pr-4 font-bold">Tổng tiền</th>
                      <th className="text-left pb-3 pr-4 font-bold">Trạng thái</th>
                      <th className="text-right pb-3 font-bold">Thời gian đặt</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {(data?.recentOrders ?? []).map((o) => (
                      <tr key={o.id} className="hover:bg-slate-50/80 transition-colors">
                        <td className="py-3 pr-4 font-mono font-bold text-slate-800">
                          {o.orderCode}
                        </td>
                        <td className="py-3 pr-4 text-slate-700 font-medium">{o.customerName}</td>
                        <td className="py-3 pr-4 font-bold text-slate-900">
                          {fmtVnd(o.totalAmount)}
                        </td>
                        <td className="py-3 pr-4">
                          <span
                            className={`px-2 py-0.5 rounded-full text-[11px] font-semibold ${
                              STATUS_COLOR[o.status] ?? STATUS_COLOR.default
                            }`}
                          >
                            {STATUS_LABEL[o.status] ?? o.status}
                          </span>
                        </td>
                        <td className="py-3 text-right text-slate-500 text-[11px]">
                          {new Date(o.orderDate).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                          })}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {!data?.recentOrders?.length && (
                  <p className="text-center text-slate-400 text-xs py-8">
                    Không có đơn hàng nào trong khoảng thời gian này
                  </p>
                )}
              </div>
            </DashCard>
          </>
        )}
      </div>
    </div>
  );
}
