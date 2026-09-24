import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { TrendingUp, DollarSign, CreditCard, RefreshCw, Scale } from 'lucide-react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';
import { dashboardApi } from '../../api/dashboardApi';
import type { DashboardFinancialPerformanceDto, DashboardPeriod } from '../../types/dashboard';
import {
  KpiMetricCard,
  DashCard,
  fmtVnd,
  fmtNum,
} from '../../components/dashboard/DashboardCharts';
import { ExportCsvButton } from '../../utils/exportUtils';

const PERIODS: { label: string; value: DashboardPeriod }[] = [
  { label: 'Hôm nay', value: 'today' },
  { label: '7 ngày', value: '7days' },
  { label: '30 ngày', value: '30days' },
  { label: 'Năm nay', value: 'year' },
];

export default function FinancialPerformanceDashboard() {
  const navigate = useNavigate();
  const [period, setPeriod] = useState<DashboardPeriod>('30days');
  const [data, setData] = useState<DashboardFinancialPerformanceDto | null>(null);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await dashboardApi.getFinancialPerformance(period);
      setData(res);
    } catch (err) {
      console.error('Lỗi khi tải dữ liệu báo cáo tài chính:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [period]);

  const timelineData =
    data?.timeline?.map((p) => ({
      label: p.label,
      revenue: p.revenue,
      cogs: p.cogs,
      grossProfit: p.grossProfit,
      cashInflow: p.cashInflow,
      cashOutflow: p.cashOutflow,
    })) ?? [];

  const bridge = data?.cashFlowBridge;
  const shrinkage = data?.shrinkageLoss;

  const actualCashCollected =
    (bridge?.onlinePaymentInflow ?? 0) +
    (bridge?.codCollectedInflow ?? 0) -
    (bridge?.customerRefundOutflow ?? 0);

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header & Filter Bar */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Dòng tiền & Đối soát Công nợ
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Cân đối dòng tiền hoạt động giữa Bán hàng và Nhập hàng, đối soát công nợ nhà cung cấp
              và biên lợi nhuận
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
              onClick={loadData}
              disabled={loading}
              title="Làm mới dữ liệu"
              className="p-2 bg-white border border-slate-200 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-all shadow-xs disabled:opacity-50"
            >
              <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            </button>
          </div>
        </div>

        {/* 4 KPI Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiMetricCard
            title="Doanh thu thuần"
            value={fmtVnd(data?.netRevenue ?? 0)}
            subtitle={`Doanh số: ${fmtVnd(data?.grossRevenue ?? 0)} | Hoàn: -${fmtVnd(data?.customerRefunds ?? 0)}`}
            badge={
              data?.netRevenueGrowthPercent !== undefined
                ? `Tăng trưởng ${data.netRevenueGrowthPercent >= 0 ? '+' : ''}${data.netRevenueGrowthPercent}%`
                : undefined
            }
            badgePositive={(data?.netRevenueGrowthPercent ?? 0) >= 0}
            icon={<DollarSign size={18} />}
          />

          <KpiMetricCard
            title="Giá vốn hàng bán (COGS)"
            value={fmtVnd(data?.totalCogs ?? 0)}
            subtitle={`Tỷ lệ giá vốn: ${
              data && data.netRevenue > 0
                ? ((data.totalCogs / data.netRevenue) * 100).toFixed(1)
                : '0'
            }% doanh thu`}
            badge="Giá vốn thực xuất"
            badgePositive={false}
            icon={<Scale size={18} />}
          />

          <KpiMetricCard
            title="Lợi nhuận gộp"
            value={fmtVnd(data?.grossProfit ?? 0)}
            subtitle="Doanh thu thuần trừ Giá vốn"
            badge={`Biên lãi: ${data?.grossMarginPercent ?? 0}%`}
            badgePositive={(data?.grossMarginPercent ?? 0) >= 0}
            icon={<TrendingUp size={18} />}
          />

          <KpiMetricCard
            title="Dòng tiền HĐKD ròng"
            value={fmtVnd(data?.estimatedNetCashFlow ?? 0)}
            subtitle="Thực thu bán hàng trừ Tiền nhập kho"
            badge={(data?.estimatedNetCashFlow ?? 0) >= 0 ? 'Thặng dư tiền' : 'Thâm hụt tiền'}
            badgePositive={(data?.estimatedNetCashFlow ?? 0) >= 0}
            icon={<CreditCard size={18} />}
          />
        </div>

        {/* Cầu nối Dòng tiền (Cash Flow Bridge: Nhập hàng vs Bán hàng) */}
        <DashCard
          title="Cầu nối Dòng tiền Hoạt động (Bán hàng & Nhập hàng)"
          subtitle="Đối soát đối ứng giữa dòng tiền thực thu từ khách hàng và nghĩa vụ thanh toán cho nhà cung cấp"
        >
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Cột 1: Dòng tiền vào (Bán hàng) */}
            <div className="rounded-xl border border-slate-200/90 bg-white p-5 flex flex-col justify-between shadow-xs">
              <div>
                <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                    <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wide">
                      Dòng tiền vào (Bán hàng)
                    </h3>
                  </div>
                  <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200/60">
                    Thực thu & Chờ nộp
                  </span>
                </div>

                <div className="mt-4 space-y-3">
                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <span className="text-slate-600">
                      Thanh toán điện tử thực nhận (VNPay / Thẻ / Chuyển khoản)
                    </span>
                    <span className="font-bold text-slate-900">
                      {fmtVnd(bridge?.onlinePaymentInflow ?? 0)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <span className="text-slate-600">
                      Tiền mặt COD đã giao thành công & nộp quỹ
                    </span>
                    <span className="font-bold text-slate-900">
                      {fmtVnd(bridge?.codCollectedInflow ?? 0)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <span className="text-slate-600">
                      Tiền COD đang vận chuyển (Shipper đang giữ)
                    </span>
                    <span className="font-semibold text-amber-700">
                      {fmtVnd(bridge?.codInTransitAmount ?? 0)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <span className="text-slate-600">
                      Tiền hoàn trả khách hàng (Đơn RMA chấp thuận)
                    </span>
                    <span className="font-bold text-rose-600">
                      -{fmtVnd(bridge?.customerRefundOutflow ?? 0)}
                    </span>
                  </div>
                </div>
              </div>

              <div className="mt-6 pt-3 border-t border-slate-200 flex items-center justify-between bg-slate-50/80 -mx-5 -mb-5 p-4 rounded-b-xl">
                <div>
                  <div className="text-xs font-semibold text-slate-500">
                    Tổng tiền thực thu về quỹ
                  </div>
                  <div className="text-[11px] text-slate-400">Đã trừ các khoản hoàn trả</div>
                </div>
                <div className="text-base font-black text-emerald-700">
                  {fmtVnd(actualCashCollected)}
                </div>
              </div>
            </div>

            {/* Cột 2: Dòng tiền ra & Công nợ (Nhập hàng) */}
            <div className="rounded-xl border border-slate-200/90 bg-white p-5 flex flex-col justify-between shadow-xs">
              <div>
                <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                  <div className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full bg-blue-500"></span>
                    <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wide">
                      Nghĩa vụ chi trả (Nhập hàng)
                    </h3>
                  </div>
                  <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 border border-blue-200/60">
                    Kho & Nhà cung cấp
                  </span>
                </div>

                <div className="mt-4 space-y-3">
                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <div>
                      <span className="text-slate-600 block">
                        Tiền hàng đã thực nhập kho (GRN hoàn tất)
                      </span>
                      <span className="text-[11px] text-slate-400">
                        Nghĩa vụ nợ phát sinh trả NCC
                      </span>
                    </div>
                    <span className="font-bold text-slate-900">
                      {fmtVnd(bridge?.inboundGoodsReceiptOutflow ?? 0)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <div>
                      <span className="text-slate-600 block">
                        Cam kết đặt mua chưa nhập kho (PO đã duyệt)
                      </span>
                      <span className="text-[11px] text-slate-400">
                        Ngân sách dự kiến chuẩn bị thanh toán
                      </span>
                    </div>
                    <span className="font-semibold text-slate-700">
                      {fmtVnd(bridge?.pendingPoCommitment ?? 0)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs py-1.5 border-b border-slate-50">
                    <span className="text-slate-600">
                      Tổng giá trị đơn đặt hàng phát sinh trong kỳ
                    </span>
                    <span className="font-bold text-slate-900">
                      {fmtVnd(
                        (bridge?.inboundGoodsReceiptOutflow ?? 0) +
                          (bridge?.pendingPoCommitment ?? 0)
                      )}
                    </span>
                  </div>
                </div>
              </div>

              <div className="mt-6 pt-3 border-t border-slate-200 flex items-center justify-between bg-slate-50/80 -mx-5 -mb-5 p-4 rounded-b-xl">
                <div>
                  <div className="text-xs font-semibold text-slate-500">
                    Dòng tiền HĐKD ròng (Thực thu - Thực nhập)
                  </div>
                  <div className="text-[11px] text-slate-400">Chỉ số cân đối vốn lưu động</div>
                </div>
                <div
                  className={`text-base font-black ${
                    (bridge?.netOperatingCashFlow ?? 0) >= 0 ? 'text-emerald-700' : 'text-rose-600'
                  }`}
                >
                  {fmtVnd(bridge?.netOperatingCashFlow ?? 0)}
                </div>
              </div>
            </div>
          </div>
        </DashCard>

        {/* Biểu đồ xu hướng: Doanh thu thuần vs Giá vốn vs Lợi nhuận gộp */}
        <DashCard
          title="Xu hướng Tài chính & Lợi nhuận gộp"
          subtitle="So sánh tương quan giữa Doanh thu thuần, Giá vốn hàng bán và Lợi nhuận gộp theo mốc thời gian"
        >
          <div className="h-80 w-full pt-2">
            {timelineData.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-slate-400">
                Chưa có dữ liệu phát sinh trong kỳ
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={timelineData} margin={{ top: 10, right: 10, left: 10, bottom: 0 }}>
                  <defs>
                    <linearGradient id="colorRev" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#10b981" stopOpacity={0.2} />
                      <stop offset="95%" stopColor="#10b981" stopOpacity={0.0} />
                    </linearGradient>
                    <linearGradient id="colorCogs" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.2} />
                      <stop offset="95%" stopColor="#3b82f6" stopOpacity={0.0} />
                    </linearGradient>
                    <linearGradient id="colorProfit" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#6366f1" stopOpacity={0.2} />
                      <stop offset="95%" stopColor="#6366f1" stopOpacity={0.0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
                  <XAxis
                    dataKey="label"
                    stroke="#94a3b8"
                    fontSize={11}
                    tickLine={false}
                    axisLine={{ stroke: '#e2e8f0' }}
                  />
                  <YAxis
                    stroke="#94a3b8"
                    fontSize={11}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v) =>
                      v >= 1_000_000_000
                        ? `${(v / 1_000_000_000).toFixed(1)}B`
                        : v >= 1_000_000
                          ? `${(v / 1_000_000).toFixed(0)}M`
                          : `${v}`
                    }
                  />
                  <Tooltip
                    formatter={(value: any, name: any) => {
                      const n = Number(value) || 0;
                      const labelMap: Record<string, string> = {
                        revenue: 'Doanh thu thuần',
                        cogs: 'Giá vốn (COGS)',
                        grossProfit: 'Lợi nhuận gộp',
                      };
                      return [fmtVnd(n), labelMap[name] || name];
                    }}
                    contentStyle={{
                      backgroundColor: '#0f172a',
                      borderRadius: '0.75rem',
                      border: '1px solid #1e293b',
                      fontSize: '11px',
                      color: '#fff',
                    }}
                  />
                  <Legend
                    verticalAlign="top"
                    align="right"
                    wrapperStyle={{ fontSize: '11px', paddingBottom: '12px' }}
                    formatter={(val) => {
                      if (val === 'revenue') return 'Doanh thu thuần';
                      if (val === 'cogs') return 'Giá vốn hàng bán';
                      if (val === 'grossProfit') return 'Lợi nhuận gộp';
                      return val;
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="revenue"
                    stroke="#10b981"
                    strokeWidth={2}
                    fillOpacity={1}
                    fill="url(#colorRev)"
                  />
                  <Area
                    type="monotone"
                    dataKey="cogs"
                    stroke="#3b82f6"
                    strokeWidth={2}
                    fillOpacity={1}
                    fill="url(#colorCogs)"
                  />
                  <Area
                    type="monotone"
                    dataKey="grossProfit"
                    stroke="#6366f1"
                    strokeWidth={2}
                    fillOpacity={1}
                    fill="url(#colorProfit)"
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </div>
        </DashCard>

        {/* 2 Bảng: Đối soát Nhà Cung Cấp & Hiệu suất Ngành hàng */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Bảng 1: Đối soát Công nợ & Mua hàng Theo Nhà Cung Cấp */}
          <DashCard
            title="Đối soát Mua hàng & Công nợ Theo Nhà Cung Cấp"
            subtitle="Theo dõi giá trị đặt hàng, hàng đã nhập kho và các khoản giảm trừ tại cổng kiểm định QC"
            action={
              <ExportCsvButton
                filename={`Doi-soat-cong-no-nha-cung-cap_${period}`}
                label="Xuất CSV"
                data={data?.supplierPayables || []}
                columns={[
                  { header: 'Mã NCC', accessor: (s) => s.supplierCode },
                  { header: 'Tên Nhà Cung Cấp', accessor: (s) => s.supplierName },
                  { header: 'Số Đơn PO', accessor: (s) => s.totalPoCount },
                  { header: 'Tổng Giá Trị Đặt (VNĐ)', accessor: (s) => s.totalPoValue },
                  { header: 'Đã Nhập Kho (VNĐ)', accessor: (s) => s.receivedValue },
                  { header: 'Từ Chối QC (VNĐ)', accessor: (s) => s.qcRejectedValue },
                  { header: 'Đã Thanh Toán (VNĐ)', accessor: (s) => s.totalPaidAmount ?? 0 },
                  { header: 'Còn Nợ Thực Tế (VNĐ)', accessor: (s) => s.remainingDebt ?? (s.receivedValue - (s.totalPaidAmount ?? 0)) },
                  { header: 'Trạng Thái', accessor: (s) => s.paymentStatus ?? 'Chưa thanh toán' },
                  { header: 'Cam Kết Chờ Nhập (VNĐ)', accessor: (s) => s.pendingCommitment },
                ]}
              />
            }
          >
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-slate-200 text-slate-500">
                    <th className="pb-2.5 font-bold">Nhà cung cấp</th>
                    <th className="pb-2.5 font-bold text-center">Số PO</th>
                    <th className="pb-2.5 font-bold text-right">Tổng đặt</th>
                    <th className="pb-2.5 font-bold text-right">Đã nhập kho</th>
                    <th className="pb-2.5 font-bold text-right">Từ chối QC</th>
                    <th className="pb-2.5 font-bold text-right">Đã trả</th>
                    <th className="pb-2.5 font-bold text-right">Nợ còn lại</th>
                    <th className="pb-2.5 font-bold text-center">Trạng thái</th>
                    <th className="pb-2.5 font-bold text-right">Chờ nhập</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {!data?.supplierPayables || data.supplierPayables.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="py-6 text-center text-slate-400">
                        Chưa phát sinh dữ liệu đơn mua hàng trong kỳ
                      </td>
                    </tr>
                  ) : (
                    data.supplierPayables.map((sup) => (
                      <tr key={sup.supplierId} className="hover:bg-slate-50/80 transition-colors">
                        <td className="py-2.5">
                          <div className="font-bold text-slate-900">{sup.supplierName}</div>
                          <div className="text-[11px] text-slate-400 font-mono">
                            {sup.supplierCode}
                          </div>
                        </td>
                        <td className="py-2.5 text-center font-medium text-slate-700">
                          {sup.totalPoCount}
                        </td>
                        <td className="py-2.5 text-right font-medium text-slate-800">
                          {fmtVnd(sup.totalPoValue)}
                        </td>
                        <td className="py-2.5 text-right font-bold text-emerald-700">
                          {fmtVnd(sup.receivedValue)}
                        </td>
                        <td className="py-2.5 text-right font-medium text-rose-600">
                          {sup.qcRejectedValue > 0 ? `-${fmtVnd(sup.qcRejectedValue)}` : '0 ₫'}
                        </td>
                        <td className="py-2.5 text-right font-medium text-blue-600">
                          {fmtVnd(sup.totalPaidAmount ?? 0)}
                        </td>
                        <td className="py-2.5 text-right font-bold text-amber-700">
                          {fmtVnd(sup.remainingDebt ?? Math.max(0, sup.receivedValue - (sup.totalPaidAmount ?? 0)))}
                        </td>
                        <td className="py-2.5 text-center">
                          <span
                            className={`inline-block px-2 py-0.5 text-[10px] font-bold rounded-full border ${
                              sup.paymentStatus === 'Đã tất toán'
                                ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                : sup.paymentStatus === 'Đã trả một phần'
                                  ? 'bg-amber-50 text-amber-700 border-amber-200'
                                  : sup.paymentStatus === 'Không phát sinh nợ'
                                    ? 'bg-slate-50 text-slate-600 border-slate-200'
                                    : 'bg-rose-50 text-rose-700 border-rose-200'
                            }`}
                          >
                            {sup.paymentStatus || 'Chưa thanh toán'}
                          </span>
                        </td>
                        <td className="py-2.5 text-right font-medium text-slate-500">
                          {fmtVnd(sup.pendingCommitment)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </DashCard>

          {/* Bảng 2: Hiệu suất Sinh lời Theo Ngành hàng */}
          <DashCard
            title="Hiệu suất Sinh lời Theo Ngành hàng"
            subtitle="Phân tích doanh thu, giá vốn và biên lợi nhuận gộp theo từng phân loại sản phẩm"
            action={
              <ExportCsvButton
                filename={`Hieu-suat-nganh-hang_${period}`}
                label="Xuất CSV"
                data={data?.categoryProfitability || []}
                columns={[
                  { header: 'Ngành Hàng', accessor: (c) => c.categoryGroupName },
                  { header: 'Sản Lượng Bán', accessor: (c) => c.quantitySold },
                  { header: 'Doanh Thu (VNĐ)', accessor: (c) => c.revenue },
                  { header: 'Giá Vốn COGS (VNĐ)', accessor: (c) => c.cogs },
                  { header: 'Lợi Nhuận Gộp (VNĐ)', accessor: (c) => c.grossProfit },
                  { header: 'Biên Lợi Nhuận (%)', accessor: (c) => `${c.grossMarginPercent}%` },
                ]}
              />
            }
          >
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-slate-200 text-slate-500">
                    <th className="pb-2.5 font-bold">Ngành hàng</th>
                    <th className="pb-2.5 font-bold text-center">SL bán</th>
                    <th className="pb-2.5 font-bold text-right">Doanh thu</th>
                    <th className="pb-2.5 font-bold text-right">Giá vốn</th>
                    <th className="pb-2.5 font-bold text-right">Lợi nhuận</th>
                    <th className="pb-2.5 font-bold text-right">Biên lãi</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {!data?.categoryProfitability || data.categoryProfitability.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-6 text-center text-slate-400">
                        Chưa có dữ liệu bán hàng trong kỳ
                      </td>
                    </tr>
                  ) : (
                    data.categoryProfitability.map((cat, idx) => (
                      <tr key={idx} className="hover:bg-slate-50/80 transition-colors">
                        <td className="py-2.5 font-bold text-slate-900">{cat.categoryGroupName}</td>
                        <td className="py-2.5 text-center font-medium text-slate-700">
                          {fmtNum(cat.quantitySold)}
                        </td>
                        <td className="py-2.5 text-right font-medium text-slate-800">
                          {fmtVnd(cat.revenue)}
                        </td>
                        <td className="py-2.5 text-right font-medium text-slate-600">
                          {fmtVnd(cat.cogs)}
                        </td>
                        <td className="py-2.5 text-right font-bold text-emerald-700">
                          {fmtVnd(cat.grossProfit)}
                        </td>
                        <td className="py-2.5 text-right">
                          <span
                            className={`inline-block px-2 py-0.5 rounded-full font-bold text-[11px] ${
                              cat.grossMarginPercent >= 20
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200/60'
                                : cat.grossMarginPercent >= 10
                                  ? 'bg-blue-50 text-blue-700 border border-blue-200/60'
                                  : 'bg-amber-50 text-amber-700 border border-amber-200/60'
                            }`}
                          >
                            {cat.grossMarginPercent}%
                          </span>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </DashCard>
        </div>

        {/* Khối Cảnh báo Hao hụt & Rủi ro Hạn dùng (Shrinkage Loss & Expiry Risk) */}
        <DashCard
          title="Tổn thất & Rủi ro Tồn kho (Shrinkage Loss)"
          subtitle="Đo lường thiệt hại phát sinh từ hàng hỏng kho, hàng cận hạn sử dụng và chi phí hoàn hàng lỗi"
        >
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="bg-rose-50/40 rounded-xl p-4 border border-rose-100 flex flex-col justify-between">
              <div>
                <div className="text-xs font-semibold text-rose-700 uppercase tracking-wider">
                  Hàng hỏng lưu kho
                </div>
                <div className="text-lg font-black text-rose-900 mt-1">
                  {fmtVnd(shrinkage?.damagedStockValue ?? 0)}
                </div>
              </div>
              <div className="text-[11px] text-rose-600/80 mt-2">
                Hao hụt vật lý được ghi nhận tại các ngăn kho
              </div>
            </div>

            <div className="bg-amber-50/40 rounded-xl p-4 border border-amber-100 flex flex-col justify-between">
              <div>
                <div className="text-xs font-semibold text-amber-700 uppercase tracking-wider">
                  Rủi ro cận hạn (dưới 7 ngày)
                </div>
                <div className="text-lg font-black text-amber-900 mt-1">
                  {fmtVnd(shrinkage?.expiringStockRiskValue ?? 0)}
                </div>
              </div>
              <div className="text-[11px] text-amber-600/80 mt-2">
                Giá trị lô hàng cần xả hàng hoặc khuyến mãi gấp
              </div>
            </div>

            <div className="bg-indigo-50/40 rounded-xl p-4 border border-indigo-100 flex flex-col justify-between">
              <div>
                <div className="text-xs font-semibold text-indigo-700 uppercase tracking-wider">
                  Tổn thất hoàn trả (RMA)
                </div>
                <div className="text-lg font-black text-indigo-900 mt-1">
                  {fmtVnd(shrinkage?.returnRefundLoss ?? 0)}
                </div>
              </div>
              <div className="text-[11px] text-indigo-600/80 mt-2">
                Chi phí hoàn tiền đơn trả hàng đã hoàn tất
              </div>
            </div>

            <div className="bg-slate-900 text-white rounded-xl p-4 border border-slate-800 flex flex-col justify-between">
              <div>
                <div className="text-xs font-semibold text-slate-300 uppercase tracking-wider">
                  Tổng hao hụt & rủi ro
                </div>
                <div className="text-lg font-black text-rose-400 mt-1">
                  {fmtVnd(shrinkage?.totalShrinkageLoss ?? 0)}
                </div>
              </div>
              <div className="text-[11px] text-slate-400 mt-2">
                Tác động trực tiếp làm suy giảm lợi nhuận ròng
              </div>
            </div>
          </div>
        </DashCard>
      </div>
    </div>
  );
}
