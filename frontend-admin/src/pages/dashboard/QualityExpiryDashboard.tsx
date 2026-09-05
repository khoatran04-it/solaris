import React, { useState, useEffect } from 'react';
import { RefreshCw, AlertTriangle, CheckCircle2 } from 'lucide-react';
import { dashboardApi } from '../../api/dashboardApi';
import type { DashboardQualityExpiryDto } from '../../types/dashboard';
import {
  DonutChart,
  RankedBarChart,
  DashCard,
  fmtVnd,
  fmtNum,
} from '../../components/dashboard/DashboardCharts';

const EXPIRY_CARD_STYLES = {
  expired: 'bg-rose-50/80 border-rose-200/80 text-rose-800',
  critical: 'bg-amber-50/80 border-amber-200/80 text-amber-800',
  warning: 'bg-yellow-50/80 border-yellow-200/80 text-yellow-800',
  safe: 'bg-emerald-50/80 border-emerald-200/80 text-emerald-800',
};

function getDaysLabel(days: number) {
  if (days < 0) return `Quá hạn ${Math.abs(days)} ngày`;
  if (days === 0) return 'Hết hạn hôm nay';
  return `Còn ${days} ngày`;
}

function getDaysBadge(days: number) {
  if (days < 0) return 'bg-rose-100 text-rose-700 font-bold border border-rose-200/80';
  if (days < 3) return 'bg-amber-100 text-amber-700 font-bold border border-amber-200/80';
  if (days < 7) return 'bg-yellow-100 text-yellow-700 font-semibold border border-yellow-200/80';
  return 'bg-emerald-100 text-emerald-700 font-medium border border-emerald-200/80';
}

export default function QualityExpiryDashboard() {
  const [data, setData] = useState<DashboardQualityExpiryDto | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setData(await dashboardApi.getQualityExpiry());
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const expiryDonutData = data
    ? [
        { label: 'Đã quá hạn', value: data.expiryOverview.expiredCount, percent: 0 },
        { label: 'Khẩn cấp (< 3 ngày)', value: data.expiryOverview.criticalCount, percent: 0 },
        { label: 'Cảnh báo (3-7 ngày)', value: data.expiryOverview.warningCount, percent: 0 },
        { label: 'An toàn (> 7 ngày)', value: data.expiryOverview.safeCount, percent: 0 },
      ].map((d) => {
        const total =
          (data?.expiryOverview.expiredCount ?? 0) +
          (data?.expiryOverview.criticalCount ?? 0) +
          (data?.expiryOverview.warningCount ?? 0) +
          (data?.expiryOverview.safeCount ?? 0);
        return { ...d, percent: total > 0 ? +((d.value / total) * 100).toFixed(1) : 0 };
      })
    : [];

  const rejectReasonsData =
    data?.qcRejectReasons.map((r) => ({
      label: r.reason,
      value: r.count,
      percent: r.percent,
    })) ?? [];

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Chất lượng & Hạn dùng (FEFO)
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Quản trị vòng đời nông sản, tỷ lệ từ chối tại cửa kho và nguy cơ quá hạn
            </p>
          </div>
          <button
            onClick={load}
            disabled={loading}
            className="flex items-center gap-2 px-3.5 py-2 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-700 hover:text-slate-900 hover:bg-slate-50 transition-all shadow-xs disabled:opacity-50"
          >
            <RefreshCw size={13} className={loading ? 'animate-spin' : ''} />
            Làm mới
          </button>
        </div>

        {loading ? (
          <div className="flex items-center justify-center h-72">
            <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            {/* FEFO Status Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                {
                  label: 'Đã Quá Hạn',
                  count: data?.expiryOverview.expiredCount ?? 0,
                  style: EXPIRY_CARD_STYLES.expired,
                },
                {
                  label: 'Khẩn Cấp (< 3 ngày)',
                  count: data?.expiryOverview.criticalCount ?? 0,
                  style: EXPIRY_CARD_STYLES.critical,
                },
                {
                  label: 'Cảnh Báo (3-7 ngày)',
                  count: data?.expiryOverview.warningCount ?? 0,
                  style: EXPIRY_CARD_STYLES.warning,
                },
                {
                  label: 'An Toàn (> 7 ngày)',
                  count: data?.expiryOverview.safeCount ?? 0,
                  style: EXPIRY_CARD_STYLES.safe,
                },
              ].map((card, i) => (
                <div
                  key={i}
                  className={`rounded-2xl p-5 border shadow-xs flex flex-col justify-between ${card.style}`}
                >
                  <span className="text-xs font-bold uppercase tracking-wider block mb-2 opacity-85">
                    {card.label}
                  </span>
                  <div className="text-2xl font-black">{fmtNum(card.count)}</div>
                  <span className="text-[11px] opacity-75 mt-1 block">lô hàng trong kho</span>
                </div>
              ))}
            </div>

            {/* Inbound QC & Customer Return Rate */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
              <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)]">
                <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-2">
                  Tỷ lệ Từ chối Nhập kho (Inbound QC)
                </span>
                <div className="flex items-baseline gap-2 mb-2">
                  <span className="text-3xl font-black text-slate-900">
                    {data?.inboundQcRejectRatePercent ?? 0}%
                  </span>
                  <span className="text-xs text-slate-500 font-medium">
                    ({fmtNum(data?.totalRejectedItems ?? 0)} /{' '}
                    {fmtNum(data?.totalInboundItems ?? 0)} đơn vị lỗi)
                  </span>
                </div>
                <div className="w-full bg-slate-100 h-2 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-rose-500 rounded-full transition-all duration-500"
                    style={{
                      width: `${Math.min(100, data?.inboundQcRejectRatePercent ?? 0)}%`,
                    }}
                  />
                </div>
              </div>

              <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)]">
                <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block mb-2">
                  Tỷ lệ Hoàn trả từ Khách hàng
                </span>
                <div className="flex items-baseline gap-2 mb-2">
                  <span className="text-3xl font-black text-slate-900">
                    {data?.customerReturnRatePercent ?? 0}%
                  </span>
                  <span className="text-xs text-slate-500 font-medium">
                    ({fmtNum(data?.totalReturnOrders ?? 0)} / {fmtNum(data?.totalOrders ?? 0)} đơn
                    hoàn)
                  </span>
                </div>
                <div className="w-full bg-slate-100 h-2 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-amber-500 rounded-full transition-all duration-500"
                    style={{
                      width: `${Math.min(100, data?.customerReturnRatePercent ?? 0)}%`,
                    }}
                  />
                </div>
              </div>
            </div>

            {/* Charts row */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
              <DashCard
                title="Phân nhóm Hạn sử dụng (FEFO)"
                subtitle="Cơ cấu số lượng lô hàng theo các mốc rủi ro cận date"
              >
                <DonutChart data={expiryDonutData} size={160} formatTooltip={fmtNum} />
              </DashCard>

              <DashCard
                title="Nguyên nhân Từ chối Nhập kho"
                subtitle="Top lý do hàng không đạt chuẩn QC tại cửa kho"
              >
                <RankedBarChart
                  data={rejectReasonsData}
                  color="#f43f5e"
                  formatValue={(v) => `${v} lần`}
                  height={220}
                />
              </DashCard>
            </div>

            {/* Expiring Batches Table */}
            <DashCard
              title="Danh sách Lô hàng Cần Xử lý Gấp (< 7 ngày)"
              subtitle="Các lô hàng cận date hoặc đã quá hạn cần kích hoạt xuất kho sớm hoặc thanh lý"
            >
              {data?.expiringBatches.length === 0 ? (
                <div className="flex items-center justify-center gap-2 text-emerald-700 bg-emerald-50/60 border border-emerald-200/60 rounded-xl py-6 text-xs font-semibold">
                  <CheckCircle2 size={16} />
                  Tồn kho an toàn: Không có lô hàng nào sắp hết hạn trong 7 ngày tới.
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead>
                      <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase tracking-wider text-[10px]">
                        <th className="text-left pb-3 pr-4 font-bold">Mã lô</th>
                        <th className="text-left pb-3 pr-4 font-bold">Sản phẩm</th>
                        <th className="text-left pb-3 pr-4 font-bold">Kho lưu trữ</th>
                        <th className="text-left pb-3 pr-4 font-bold">Hạn dùng</th>
                        <th className="text-left pb-3 pr-4 font-bold">Tình trạng</th>
                        <th className="text-right pb-3 pr-4 font-bold">SL tồn</th>
                        <th className="text-right pb-3 font-bold">Giá trị rủi ro</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {data?.expiringBatches.map((b, i) => (
                        <tr
                          key={i}
                          className={`hover:bg-slate-50/60 transition-colors ${
                            b.daysRemaining < 0 ? 'bg-rose-50/30' : ''
                          }`}
                        >
                          <td className="py-3 pr-4 font-mono font-bold text-slate-800">
                            {b.batchCode}
                          </td>
                          <td className="py-3 pr-4">
                            <div className="font-semibold text-slate-800 truncate max-w-[160px]">
                              {b.productName}
                            </div>
                            <div className="text-slate-400 text-[10px] truncate">
                              {b.variantName}
                            </div>
                          </td>
                          <td className="py-3 pr-4 text-slate-600 font-medium">
                            {b.warehouseName}
                          </td>
                          <td className="py-3 pr-4 text-slate-600 font-mono text-[11px]">
                            {new Date(b.expiryDate).toLocaleDateString('vi-VN', {
                              day: '2-digit',
                              month: '2-digit',
                              year: 'numeric',
                            })}
                          </td>
                          <td className="py-3 pr-4">
                            <span
                              className={`px-2 py-0.5 rounded-full text-[10px] ${getDaysBadge(
                                b.daysRemaining
                              )}`}
                            >
                              {getDaysLabel(b.daysRemaining)}
                            </span>
                          </td>
                          <td className="py-3 pr-4 text-right font-semibold text-slate-800">
                            {fmtNum(b.quantityAvailable)}
                          </td>
                          <td className="py-3 text-right font-bold text-rose-600">
                            {b.estimatedLossValue > 0 ? fmtVnd(b.estimatedLossValue) : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </DashCard>

            {/* Warning banner */}
            {(data?.expiryOverview.expiredCount ?? 0) + (data?.expiryOverview.criticalCount ?? 0) >
              0 && (
              <div className="flex items-start gap-3 bg-amber-50/80 border border-amber-200/80 rounded-2xl p-4">
                <AlertTriangle size={17} className="text-amber-600 shrink-0 mt-0.5" />
                <div>
                  <div className="text-xs font-bold text-amber-900">
                    Cần hành động: Phát hiện{' '}
                    {(data?.expiryOverview.expiredCount ?? 0) +
                      (data?.expiryOverview.criticalCount ?? 0)}{' '}
                    lô hàng đang ở mức khẩn cấp hoặc quá hạn
                  </div>
                  <p className="text-[11px] text-amber-700 mt-0.5 leading-relaxed">
                    Khuyến nghị bộ phận bán hàng xem xét điều chỉnh giá khuyến mãi (Flash Sale) hoặc
                    chuyển sang kế hoạch xả hàng thanh lý để thu hồi vốn.
                  </p>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
