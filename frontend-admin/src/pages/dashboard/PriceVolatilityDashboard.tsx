import React, { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { RefreshCw, Search, Scale, DollarSign, TrendingUp, Percent, HelpCircle } from 'lucide-react';
import {
  ResponsiveContainer,
  ComposedChart,
  Line,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ReferenceLine,
  Cell,
} from 'recharts';
import { dashboardApi } from '../../api/dashboardApi';
import type {
  DashboardPriceVolatilityDto,
  SkuSelectItem,
  PriceVolatilityTimeframe,
} from '../../types/dashboard';
import {
  KpiMetricCard,
  DashCard,
  fmtVnd,
  fmtNum,
} from '../../components/dashboard/DashboardCharts';
import { ExportCsvButton } from '../../utils/exportUtils';

const TIMEFRAMES: { label: string; value: PriceVolatilityTimeframe }[] = [
  { label: 'Tuần', value: 'week' },
  { label: 'Tháng', value: 'month' },
  { label: 'Năm', value: 'year' },
];

export default function PriceVolatilityDashboard() {
  const navigate = useNavigate();

  const [timeframe, setTimeframe] = useState<PriceVolatilityTimeframe>('month');
  const [skuList, setSkuList] = useState<SkuSelectItem[]>([]);
  const [selectedVariantId, setSelectedVariantId] = useState<number | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showSkuDropdown, setShowSkuDropdown] = useState(false);

  const [data, setData] = useState<DashboardPriceVolatilityDto | null>(null);
  const [loading, setLoading] = useState(true);

  // Tải danh sách SKU để phục vụ bộ lọc tìm kiếm
  useEffect(() => {
    const fetchSkus = async () => {
      try {
        const skus = await dashboardApi.getPriceVolatilitySkus();
        setSkuList(skus);
        if (skus.length > 0 && !selectedVariantId) {
          setSelectedVariantId(skus[0].variantId);
        }
      } catch (err) {
        console.error('Lỗi khi tải danh sách SKU:', err);
      }
    };
    fetchSkus();
  }, []);

  // Tải dữ liệu biến động giá của SKU đang chọn
  const loadData = async (vId?: number | null) => {
    const targetId = vId ?? selectedVariantId;
    setLoading(true);
    try {
      const res = await dashboardApi.getPriceVolatility(targetId || undefined, timeframe);
      setData(res);
      if (res.variantId && res.variantId !== selectedVariantId) {
        setSelectedVariantId(res.variantId);
      }
    } catch (err) {
      console.error('Lỗi khi tải dữ liệu biến động giá:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (selectedVariantId !== null) {
      loadData(selectedVariantId);
    }
  }, [selectedVariantId, timeframe]);

  // Lọc danh sách SKU theo từ khóa tìm kiếm
  const filteredSkus = useMemo(() => {
    if (!searchTerm.trim()) return skuList.slice(0, 20);
    const term = searchTerm.toLowerCase();
    return skuList
      .filter(
        (s) =>
          s.variantName.toLowerCase().includes(term) ||
          s.variantCode.toLowerCase().includes(term) ||
          s.productName.toLowerCase().includes(term)
      )
      .slice(0, 20);
  }, [skuList, searchTerm]);

  const currentSku = useMemo(() => {
    return skuList.find((s) => s.variantId === selectedVariantId);
  }, [skuList, selectedVariantId]);

  const timelineData = data?.timeline ?? [];

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        {/* Header & Bộ lọc */}
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4 border-b border-slate-200/80 pb-5">
          <div>
            <h1 className="text-xl font-bold text-slate-900 tracking-tight">
              Biến động Giá Nhập & Giá Bán Theo Mặt Hàng
            </h1>
            <p className="text-xs text-slate-500 mt-1">
              Phân tích tương quan giữa giá mua vào từ Nhà cung cấp và giá bán ra cho Khách hàng quy
              đổi về Đơn vị cơ sở
            </p>
          </div>

          <div className="flex items-center gap-3 flex-wrap">
            {/* Bộ lọc sản phẩm (Tìm kiếm SKU) */}
            <div className="relative">
              <div className="flex items-center gap-2 px-3 py-1.5 bg-white border border-slate-200 rounded-xl shadow-xs w-72">
                <Search size={14} className="text-slate-400 shrink-0" />
                <input
                  type="text"
                  placeholder="Tìm kiếm sản phẩm / SKU..."
                  value={searchTerm}
                  onFocus={() => setShowSkuDropdown(true)}
                  onChange={(e) => {
                    setSearchTerm(e.target.value);
                    setShowSkuDropdown(true);
                  }}
                  className="w-full text-xs text-slate-800 placeholder-slate-400 bg-transparent outline-none font-medium"
                />
              </div>

              {/* Dropdown danh sách kết quả */}
              {showSkuDropdown && (
                <div
                  className="absolute left-0 top-full mt-1.5 w-80 bg-white border border-slate-200 rounded-xl shadow-xl z-50 max-h-64 overflow-y-auto divide-y divide-slate-100"
                  onMouseLeave={() => setShowSkuDropdown(false)}
                >
                  {filteredSkus.length === 0 ? (
                    <div className="p-3 text-xs text-slate-400 text-center">
                      Không tìm thấy mặt hàng phù hợp
                    </div>
                  ) : (
                    filteredSkus.map((sku) => (
                      <button
                        key={sku.variantId}
                        type="button"
                        onClick={() => {
                          setSelectedVariantId(sku.variantId);
                          setSearchTerm('');
                          setShowSkuDropdown(false);
                        }}
                        className={`w-full text-left p-2.5 text-xs hover:bg-slate-50 transition-colors flex flex-col gap-0.5 ${
                          selectedVariantId === sku.variantId ? 'bg-slate-50 font-bold' : ''
                        }`}
                      >
                        <div className="text-slate-900 font-bold line-clamp-1">
                          {sku.variantName}
                        </div>
                        <div className="flex items-center gap-2 text-[11px] text-slate-400 font-mono">
                          <span>{sku.variantCode}</span>
                          <span>•</span>
                          <span>ĐVT: {sku.baseUoMName}</span>
                        </div>
                      </button>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* Bộ lọc thời gian: Tuần / Tháng / Năm */}
            <div className="flex bg-white border border-slate-200 rounded-xl p-1 shadow-xs">
              {TIMEFRAMES.map((t) => (
                <button
                  key={t.value}
                  onClick={() => setTimeframe(t.value)}
                  className={`px-3 py-1.5 text-xs font-semibold rounded-lg transition-all ${
                    timeframe === t.value
                      ? 'bg-slate-900 text-white shadow-xs'
                      : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                  }`}
                >
                  {t.label}
                </button>
              ))}
            </div>

            {/* Nút Làm mới */}
            <button
              onClick={() => loadData()}
              disabled={loading}
              title="Làm mới dữ liệu"
              className="p-2 bg-white border border-slate-200 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-all shadow-xs disabled:opacity-50"
            >
              <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            </button>
          </div>
        </div>

        {/* Thông tin mặt hàng đang khảo sát */}
        <div className="bg-white border border-slate-200/80 rounded-2xl p-4 shadow-xs flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div>
            <div className="text-xs font-bold text-slate-400 uppercase tracking-wider">
              Mặt hàng đang phân tích
            </div>
            <div className="text-base font-bold text-slate-900 mt-0.5">
              {data?.variantName || currentSku?.variantName || 'Đang tải...'}
            </div>
            <div className="text-xs text-slate-500 mt-0.5">
              Mã SKU: <span className="font-mono text-slate-700">{data?.variantCode}</span> • Đơn vị
              tính cơ sở: <span className="font-bold text-slate-800">{data?.baseUoMName}</span>
            </div>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            {data?.volatilityLevel && (
              <div className="relative group cursor-help inline-flex items-center">
                <span
                  className={`px-3 py-1 text-xs font-bold rounded-full border inline-flex items-center gap-1.5 ${
                    data.volatilityLevel === 'Ổn định'
                      ? 'bg-blue-50 text-blue-700 border-blue-200'
                      : data.volatilityLevel === 'Vừa phải'
                        ? 'bg-amber-50 text-amber-700 border-amber-200'
                        : 'bg-rose-50 text-rose-700 border-rose-200'
                  }`}
                >
                  <span>Biến động: {data.volatilityLevel} (CV: {data.coefficientOfVariation ?? 0}%)</span>
                  <HelpCircle size={13} className="text-slate-400 group-hover:text-slate-600 transition-colors" />
                </span>
                <div className="absolute right-0 top-full mt-2 hidden group-hover:block z-50 w-72 p-3 bg-slate-900 text-white text-[11px] font-normal leading-relaxed rounded-xl shadow-xl pointer-events-none">
                  <div className="font-bold text-amber-400 mb-1">Hệ số biến thiên CV% (Kinh tế lượng):</div>
                  <div className="text-slate-300 mb-1">CV% = (Độ lệch chuẩn σ / Giá TB μ) × 100%</div>
                  <div className="space-y-0.5 text-slate-400 border-t border-slate-800 pt-1 mt-1">
                    <div>• &lt; 5%: Giá nhập rất ổn định</div>
                    <div>• 5% - 15%: Biến động vừa phải</div>
                    <div>• &gt; 15%: Biến động mạnh (Rủi ro biên lãi)</div>
                  </div>
                  <div className="absolute bottom-full right-6 -mb-1 border-4 border-transparent border-b-slate-900" />
                </div>
              </div>
            )}
            <span
              className={`px-3 py-1 text-xs font-bold rounded-full border ${
                (data?.priceSpread ?? 0) >= 0
                  ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                  : 'bg-rose-50 text-rose-700 border-rose-200'
              }`}
            >
              {(data?.priceSpread ?? 0) >= 0 ? 'Kinh doanh có lãi' : 'Cảnh báo bán lỗ'}
            </span>
          </div>
        </div>

        {/* 4 KPI Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiMetricCard
            title={`Giá nhập gần nhất (${data?.baseUoMName || 'ĐVT'})`}
            value={fmtVnd(data?.latestImportPrice ?? 0)}
            subtitle={
              data?.minImportPrice && data?.maxImportPrice && data.minImportPrice !== data.maxImportPrice
                ? `Biên độ: ${fmtVnd(data.minImportPrice)} - ${fmtVnd(data.maxImportPrice)}`
                : 'Đơn giá nhập từ đơn mua PO'
            }
            tooltip="Giá mua vào gần nhất từ đơn đặt hàng PO hoặc phiếu nhập kho, quy chuẩn về đơn vị tính cơ sở."
            badge={
              data?.importPriceChangePercent !== undefined
                ? `Biến động ${data.importPriceChangePercent >= 0 ? '+' : ''}${data.importPriceChangePercent}%`
                : undefined
            }
            badgePositive={(data?.importPriceChangePercent ?? 0) <= 0}
            icon={<Scale size={18} />}
          />

          <KpiMetricCard
            title={`Giá bán hiện tại (${data?.baseUoMName || 'ĐVT'})`}
            value={fmtVnd(data?.currentSellingPrice ?? 0)}
            subtitle="Đơn giá xuất bán thực tế"
            tooltip="Đơn giá bán niêm yết hiện tại quy đổi về cùng đơn vị tính cơ sở."
            badge="Giá bán chuẩn hóa"
            badgePositive={true}
            icon={<DollarSign size={18} />}
          />

          <KpiMetricCard
            title={`Chênh lệch giá (Spread)`}
            value={fmtVnd(data?.priceSpread ?? 0)}
            subtitle="Giá bán trừ Giá nhập cơ sở"
            tooltip="Spread = Đơn giá bán - Đơn giá nhập cơ sở. Thể hiện biên thặng dư danh nghĩa trên mỗi đơn vị sản phẩm bán ra."
            badge={(data?.priceSpread ?? 0) >= 0 ? 'Thặng dư giá' : 'Bán dưới giá vốn'}
            badgePositive={(data?.priceSpread ?? 0) >= 0}
            icon={<TrendingUp size={18} />}
          />

          <KpiMetricCard
            title="Biên lợi nhuận gộp"
            value={`${data?.marginPercent ?? 0}%`}
            subtitle="Tỷ lệ lãi trên doanh thu"
            tooltip="Biên lợi nhuận gộp = (Chênh lệch giá Spread / Giá bán) × 100%."
            badge={
              (data?.marginPercent ?? 0) >= 20
                ? 'Biên an toàn'
                : (data?.marginPercent ?? 0) >= 0
                  ? 'Biên hẹp'
                  : 'Thâm hụt'
            }
            badgePositive={(data?.marginPercent ?? 0) >= 0}
            icon={<Percent size={18} />}
          />
        </div>

        {/* Biểu đồ đường kép kết hợp cột chênh lệch (Composed Dual-Line Chart) */}
        <DashCard
          title="Tương Quan Giá Nhập vs Giá Bán Theo Thời Gian"
          subtitle={`So sánh đường Giá bán (Xanh lá) và Giá nhập (Xanh dương) kèm Cột chênh lệch lãi/lỗ (Đơn vị tính: VNĐ / ${data?.baseUoMName || 'ĐVT'})`}
        >
          <div className="h-88 w-full pt-2">
            {timelineData.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-slate-400">
                Chưa có dữ liệu giao dịch nhập/bán trong khoảng thời gian này
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <ComposedChart
                  data={timelineData}
                  margin={{ top: 10, right: 20, left: 10, bottom: 0 }}
                >
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
                  <XAxis
                    dataKey="label"
                    stroke="#94a3b8"
                    fontSize={11}
                    tickLine={false}
                    axisLine={{ stroke: '#e2e8f0' }}
                  />
                  <YAxis
                    yAxisId="price"
                    stroke="#94a3b8"
                    fontSize={11}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v) =>
                      v >= 1_000_000
                        ? `${(v / 1_000_000).toFixed(1)}M`
                        : v >= 1_000
                          ? `${(v / 1_000).toFixed(0)}k`
                          : `${v}`
                    }
                  />
                  <YAxis
                    yAxisId="spread"
                    orientation="right"
                    stroke="#94a3b8"
                    fontSize={11}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v) =>
                      v >= 1_000_000
                        ? `${(v / 1_000_000).toFixed(1)}M`
                        : v >= 1_000
                          ? `${(v / 1_000).toFixed(0)}k`
                          : `${v}`
                    }
                  />
                  <ReferenceLine yAxisId="spread" y={0} stroke="#cbd5e1" strokeDasharray="2 2" />
                  <Tooltip
                    formatter={(value: any, name: any) => {
                      const n = Number(value) || 0;
                      if (name === 'avgSellingPrice') return [fmtVnd(n), 'Giá bán bình quân'];
                      if (name === 'avgImportPrice') return [fmtVnd(n), 'Giá nhập bình quân'];
                      if (name === 'spread') {
                        const status = n >= 0 ? 'Lãi' : 'Lỗ';
                        return [`${fmtVnd(n)} (${status})`, 'Chênh lệch Spread'];
                      }
                      return [n, name];
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
                      if (val === 'avgSellingPrice') return 'Giá bán bình quân';
                      if (val === 'avgImportPrice') return 'Giá nhập bình quân';
                      if (val === 'spread') return 'Chênh lệch lãi/lỗ (Spread)';
                      return val;
                    }}
                  />
                  <Bar
                    yAxisId="spread"
                    dataKey="spread"
                    name="spread"
                    barSize={16}
                    radius={[4, 4, 0, 0]}
                  >
                    {timelineData.map((entry, index) => (
                      <Cell
                        key={`cell-${index}`}
                        fill={entry.spread >= 0 ? '#10b981' : '#f43f5e'}
                      />
                    ))}
                  </Bar>
                  <Line
                    yAxisId="price"
                    type="monotone"
                    dataKey="avgSellingPrice"
                    name="avgSellingPrice"
                    stroke="#10b981"
                    strokeWidth={2.5}
                    dot={{ r: 3, fill: '#10b981' }}
                  />
                  <Line
                    yAxisId="price"
                    type="monotone"
                    dataKey="avgImportPrice"
                    name="avgImportPrice"
                    stroke="#3b82f6"
                    strokeWidth={2.5}
                    dot={{ r: 3, fill: '#3b82f6' }}
                  />
                </ComposedChart>
              </ResponsiveContainer>
            )}
          </div>
        </DashCard>

        {/* Bảng Lịch sử giao dịch chi tiết */}
        <DashCard
          title="Lịch Sử Giao Dịch Nhập Hàng & Xuất Bán"
          subtitle={`Chi tiết từng lần mua từ Nhà cung cấp và bán cho Khách hàng đã được quy đổi về ${data?.baseUoMName || 'ĐVT cơ sở'}`}
          action={
            <ExportCsvButton
              filename={`Lich-su-bien-dong-gia_${data?.skuCode || 'SKU'}_${timeframe}`}
              label="Xuất CSV"
              data={data?.transactions || []}
              columns={[
                { header: 'Ngày', accessor: (tx) => new Date(tx.date).toLocaleDateString('vi-VN') },
                { header: 'Loại Giao Dịch', accessor: (tx) => tx.type },
                { header: 'Mã Chứng Từ', accessor: (tx) => tx.documentCode },
                { header: 'Đối Tác', accessor: (tx) => tx.partnerName },
                { header: 'Đơn Giá Gốc', accessor: (tx) => tx.originalPrice },
                { header: 'Đơn Vị Gốc', accessor: (tx) => tx.originalUoM },
                {
                  header: `Đơn Giá Quy Đổi (VNĐ/${data?.baseUoMName || 'ĐVT'})`,
                  accessor: (tx) => tx.normalizedPrice,
                },
                {
                  header: `Số Lượng Quy Đổi (${data?.baseUoMName || 'ĐVT'})`,
                  accessor: (tx) => tx.normalizedQuantity,
                },
              ]}
            />
          }
        >
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-slate-200 text-slate-500">
                  <th className="pb-2.5 font-bold">Ngày</th>
                  <th className="pb-2.5 font-bold">Loại giao dịch</th>
                  <th className="pb-2.5 font-bold">Mã chứng từ</th>
                  <th className="pb-2.5 font-bold">Đối tác</th>
                  <th className="pb-2.5 font-bold text-right">Đơn giá gốc</th>
                  <th className="pb-2.5 font-bold text-right">Đơn giá quy đổi</th>
                  <th className="pb-2.5 font-bold text-right">SL quy đổi</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {!data?.transactions || data.transactions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-6 text-center text-slate-400">
                      Chưa có dữ liệu giao dịch phát sinh
                    </td>
                  </tr>
                ) : (
                  data.transactions.map((tx, idx) => (
                    <tr key={idx} className="hover:bg-slate-50/80 transition-colors">
                      <td className="py-2.5 font-medium text-slate-600">
                        {new Date(tx.date).toLocaleDateString('vi-VN')}
                      </td>
                      <td className="py-2.5">
                        <span
                          className={`inline-block px-2 py-0.5 rounded-full font-bold text-[11px] border ${
                            tx.type === 'Nhập hàng'
                              ? 'bg-blue-50 text-blue-700 border-blue-200/60'
                              : tx.type === 'Điều chỉnh giá NCC'
                              ? 'bg-amber-50 text-amber-700 border-amber-200/60'
                              : 'bg-emerald-50 text-emerald-700 border-emerald-200/60'
                          }`}
                        >
                          {tx.type}
                        </span>
                      </td>
                      <td className="py-2.5 font-mono font-bold text-slate-800">
                        {tx.documentCode}
                      </td>
                      <td className="py-2.5 font-medium text-slate-700">{tx.partnerName}</td>
                      <td className="py-2.5 text-right text-slate-500 font-medium">
                        {fmtVnd(tx.originalUnitPrice)} / {tx.originalUoMName}
                      </td>
                      <td className="py-2.5 text-right font-bold text-slate-900">
                        {fmtVnd(tx.normalizedUnitPrice)} / {tx.baseUoMName}
                      </td>
                      <td className="py-2.5 text-right font-medium text-slate-700">
                        {fmtNum(tx.quantityInBaseUoM)} {tx.baseUoMName}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </DashCard>
      </div>
    </div>
  );
}
