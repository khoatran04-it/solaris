import React from 'react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from 'recharts';

// ============================================================
// DASHBOARD CHARTS — Recharts + Solaris Design System
// Giao diện phong cách doanh nghiệp tối giản, chuyên nghiệp.
// ============================================================

export const fmtVnd = (v: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(v);

export const fmtNum = (v: number) => new Intl.NumberFormat('vi-VN').format(v);

export const CHART_PALETTE = [
  '#f59e0b', // amber-500
  '#3b82f6', // blue-500
  '#10b981', // emerald-500
  '#8b5cf6', // purple-500
  '#f43f5e', // rose-500
  '#64748b', // slate-500
  '#06b6d4', // cyan-500
  '#ec4899', // pink-500
];

// --- Custom Recharts Tooltip ---
interface CustomTooltipProps {
  active?: boolean;
  payload?: any[];
  label?: string;
  valueFormatter?: (v: number) => string;
}

export const ModernTooltip: React.FC<CustomTooltipProps> = ({
  active,
  payload,
  label,
  valueFormatter = fmtNum,
}) => {
  if (!active || !payload || !payload.length) return null;
  const item = payload[0];
  return (
    <div className="bg-slate-900 text-white text-xs rounded-xl px-3.5 py-2.5 shadow-xl border border-slate-800">
      <div className="text-slate-400 font-medium text-[11px] mb-0.5">{label || item.name}</div>
      <div className="text-sm font-bold text-white">{valueFormatter(item.value)}</div>
    </div>
  );
};

// --- KPI Card ---
interface KpiMetricCardProps {
  title: string;
  value: string;
  subtitle?: string;
  badge?: string;
  badgePositive?: boolean;
  icon?: React.ReactNode;
}

export const KpiMetricCard: React.FC<KpiMetricCardProps> = ({
  title,
  value,
  subtitle,
  badge,
  badgePositive,
  icon,
}) => (
  <div className="bg-white rounded-2xl p-5 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] border border-slate-200/80 flex flex-col justify-between">
    <div className="flex items-center justify-between mb-3">
      <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">{title}</span>
      {icon && (
        <div className="w-8 h-8 rounded-lg bg-slate-50 text-slate-600 border border-slate-100 flex items-center justify-center shrink-0">
          {icon}
        </div>
      )}
    </div>
    <div className="text-2xl font-black text-slate-800 tracking-tight mb-2">{value}</div>
    <div className="flex items-center gap-2 flex-wrap text-xs">
      {subtitle && <span className="text-slate-500">{subtitle}</span>}
      {badge && (
        <span
          className={`text-[11px] font-bold px-2 py-0.5 rounded-full ${
            badgePositive
              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200/60'
              : 'bg-rose-50 text-rose-700 border border-rose-200/60'
          }`}
        >
          {badge}
        </span>
      )}
    </div>
  </div>
);

// --- Card Container ---
export const DashCard: React.FC<{
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
  action?: React.ReactNode;
}> = ({ title, subtitle, children, className = '', action }) => (
  <div
    className={`bg-white rounded-2xl shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] border border-slate-200/80 p-6 ${className}`}
  >
    <div className="flex items-start justify-between gap-4 mb-5">
      <div>
        <h3 className="text-sm font-bold text-slate-800 tracking-wide">{title}</h3>
        {subtitle && <p className="text-xs text-slate-500 mt-0.5">{subtitle}</p>}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
    {children}
  </div>
);

// --- 1. AreaLineChart (Recharts AreaChart) ---
interface AreaLineChartProps {
  data: { label: string; value: number }[];
  color?: string;
  height?: number;
  formatTooltip?: (v: number) => string;
}

export const AreaLineChart: React.FC<AreaLineChartProps> = ({
  data,
  color = '#f59e0b',
  height = 200,
  formatTooltip = fmtVnd,
}) => {
  if (!data.length) {
    return (
      <div className="flex items-center justify-center text-slate-400 text-xs py-12">
        Chưa có dữ liệu thống kê
      </div>
    );
  }

  const gradId = `areaGrad-${color.replace('#', '')}`;

  return (
    <div style={{ width: '100%', height }}>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor={color} stopOpacity={0.25} />
              <stop offset="95%" stopColor={color} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
          <XAxis
            dataKey="label"
            stroke="#94a3b8"
            fontSize={11}
            tickLine={false}
            axisLine={{ stroke: '#f1f5f9' }}
          />
          <YAxis
            stroke="#94a3b8"
            fontSize={11}
            tickLine={false}
            axisLine={false}
            tickFormatter={(v) =>
              v >= 1000000
                ? `${(v / 1000000).toFixed(1)}M`
                : v >= 1000
                  ? `${(v / 1000).toFixed(0)}k`
                  : `${v}`
            }
          />
          <Tooltip content={<ModernTooltip valueFormatter={formatTooltip} />} />
          <Area
            type="monotone"
            dataKey="value"
            stroke={color}
            strokeWidth={2.5}
            fillOpacity={1}
            fill={`url(#${gradId})`}
            activeDot={{ r: 5, stroke: color, strokeWidth: 2, fill: '#ffffff' }}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
};

// --- 2. DonutChart (Recharts PieChart) ---
interface DonutChartProps {
  data: { label: string; value: number; percent: number }[];
  size?: number;
  formatTooltip?: (v: number) => string;
}

export const DonutChart: React.FC<DonutChartProps> = ({
  data,
  size = 180,
  formatTooltip = fmtVnd,
}) => {
  if (!data.length) {
    return (
      <div className="flex items-center justify-center text-slate-400 text-xs py-10">
        Chưa có dữ liệu
      </div>
    );
  }

  return (
    <div className="flex flex-col sm:flex-row items-center justify-between gap-6">
      <div style={{ width: size, height: size }} className="shrink-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="label"
              cx="50%"
              cy="50%"
              innerRadius="58%"
              outerRadius="82%"
              paddingAngle={2}
            >
              {data.map((_, i) => (
                <Cell
                  key={i}
                  fill={CHART_PALETTE[i % CHART_PALETTE.length]}
                  stroke="#ffffff"
                  strokeWidth={2}
                />
              ))}
            </Pie>
            <Tooltip content={<ModernTooltip valueFormatter={formatTooltip} />} />
          </PieChart>
        </ResponsiveContainer>
      </div>

      <div className="flex-1 w-full flex flex-col gap-2 min-w-0">
        {data.map((item, i) => (
          <div key={i} className="flex items-center justify-between gap-3 text-xs">
            <div className="flex items-center gap-2 min-w-0">
              <span
                className="w-2.5 h-2.5 rounded-full shrink-0"
                style={{ background: CHART_PALETTE[i % CHART_PALETTE.length] }}
              />
              <span className="text-slate-700 font-medium truncate">{item.label}</span>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <span className="font-semibold text-slate-800">{formatTooltip(item.value)}</span>
              <span className="text-slate-500 font-bold min-w-[38px] text-right">
                {item.percent.toFixed(1)}%
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

// --- 3. RankedBarChart (Modern Ranked Progress Bars) ---
interface RankedBarChartProps {
  data: { label: string; value: number; percent?: number }[];
  color?: string;
  formatValue?: (v: number) => string;
  maxItems?: number;
  height?: number;
}

export const RankedBarChart: React.FC<RankedBarChartProps> = ({
  data,
  color = '#3b82f6',
  formatValue = fmtNum,
  maxItems = 8,
}) => {
  const items = data.slice(0, maxItems);
  if (!items.length) {
    return (
      <div className="flex items-center justify-center text-slate-400 text-xs py-10">
        Chưa có dữ liệu
      </div>
    );
  }

  const maxVal = Math.max(...items.map((i) => i.value), 1);

  return (
    <div className="flex flex-col gap-3 py-1">
      {items.map((item, idx) => {
        const pct = item.percent ?? +((item.value / maxVal) * 100).toFixed(1);
        return (
          <div key={idx} className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between gap-2 text-xs">
              <div className="flex items-center gap-2 min-w-0">
                <span className="w-4 text-slate-400 font-mono text-[11px] font-bold text-right shrink-0">
                  {idx + 1}
                </span>
                <span className="font-semibold text-slate-800 truncate">{item.label}</span>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <span className="font-bold text-slate-900">{formatValue(item.value)}</span>
                {item.percent !== undefined && (
                  <span className="text-slate-400 text-[11px] font-medium min-w-[32px] text-right">
                    {item.percent}%
                  </span>
                )}
              </div>
            </div>
            <div className="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
              <div
                className="h-full rounded-full transition-all duration-500"
                style={{
                  width: `${Math.min(100, pct)}%`,
                  backgroundColor: color,
                }}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
};

// --- 4. CapacityGauge (Recharts Semi-circle Pie) ---
interface CapacityGaugeProps {
  label: string;
  value: number;
  max: number;
  percent: number;
  unit?: string;
  status?: 'Safe' | 'Warning' | 'Critical';
  warningThreshold?: number;
}

export const CapacityGauge: React.FC<CapacityGaugeProps> = ({
  label,
  value,
  max,
  percent,
  unit = 'm³',
  status = 'Safe',
}) => {
  const clamped = Math.min(100, Math.max(0, percent));
  // Nếu có hàng tồn nhưng tỷ lệ quá nhỏ, hiển thị 1 vệt cung màu tối thiểu 1.5% để người dùng nhìn thấy trực quan
  const visualArcPercent = value > 0 ? Math.max(clamped, 1.5) : clamped;
  const remaining = 100 - visualArcPercent;
  const color = status === 'Critical' ? '#f43f5e' : status === 'Warning' ? '#f59e0b' : '#10b981';

  const gaugeData = [
    { name: 'Đã dùng', value: visualArcPercent },
    { name: 'Còn trống', value: remaining },
  ];

  // Hiển thị số phần trăm: nếu có hàng tồn nhưng < 0.1% -> '< 0.1%', < 10% -> 1 chữ số thập phân, còn lại làm tròn
  const percentText =
    value > 0 && clamped < 0.1
      ? '< 0.1%'
      : clamped > 0 && clamped < 10
        ? `${clamped.toFixed(1)}%`
        : `${clamped.toFixed(0)}%`;

  return (
    <div className="flex flex-col items-center">
      <div style={{ width: 120, height: 75 }} className="relative flex items-center justify-center">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={gaugeData}
              startAngle={180}
              endAngle={0}
              cx="50%"
              cy="80%"
              innerRadius={36}
              outerRadius={50}
              paddingAngle={0}
              dataKey="value"
            >
              <Cell fill={color} stroke="none" />
              <Cell fill="#e2e8f0" stroke="none" />
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <div className="absolute top-7 flex flex-col items-center pointer-events-none">
          <span className="text-sm font-black tracking-tight" style={{ color }}>
            {percentText}
          </span>
          <span className="text-[10px] text-slate-500 font-semibold uppercase">{status}</span>
        </div>
      </div>
      <div className="text-center mt-1">
        <div className="text-xs font-bold text-slate-700 truncate max-w-[130px]">{label}</div>
        <div className="text-[11px] text-slate-500 font-medium">
          {fmtNum(value)} / {fmtNum(max)} {unit}
        </div>
      </div>
    </div>
  );
};
