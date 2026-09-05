import React from 'react';

/**
 * ============================================================================
 * SOLARIS ADMIN DESIGN SYSTEM - BADGE & STATUS SYSTEM
 * Tập trung toàn bộ quy chuẩn:
 * - Font chữ: Inter / System Font, cỡ chữ text-xs, font-bold
 * - Kích thước: padding px-2.5 py-1, bo góc rounded-full (Pill shape)
 * - Viền: border mỏng 1px tinh tế
 * - Chấm trạng thái (Status Dot): w-1.5 h-1.5 rounded-full
 * - Hệ 6 màu ngữ nghĩa: Emerald, Amber, Rose, Slate, Blue, Indigo
 * ============================================================================
 */

export type BadgeVariant = 'emerald' | 'amber' | 'rose' | 'slate' | 'blue' | 'indigo';

export interface BadgeStyleConfig {
  wrapper: string;
  dot: string;
}

export const BADGE_TYPOGRAPHY = {
  fontFamily: 'Inter, system-ui, -apple-system, sans-serif',
  fontSize: '0.75rem', // 12px (text-xs)
  fontWeight: '700', // font-bold
  lineHeight: '1rem',
} as const;

export const BADGE_COLORS = {
  emerald: {
    bg: '#ecfdf5',
    text: '#047857',
    border: '#a7f3d0',
    dot: '#10b981',
  },
  amber: {
    bg: '#fffbeb',
    text: '#b45309',
    border: '#fde68a',
    dot: '#f59e0b',
  },
  rose: {
    bg: '#fff1f2',
    text: '#be123c',
    border: '#fecdd3',
    dot: '#f43f5e',
  },
  slate: {
    bg: '#f8fafc',
    text: '#475569',
    border: '#e2e8f0',
    dot: '#94a3b8',
  },
  blue: {
    bg: '#eff6ff',
    text: '#1d4ed8',
    border: '#bfdbfe',
    dot: '#3b82f6',
  },
  indigo: {
    bg: '#eef2ff',
    text: '#4338ca',
    border: '#c7d2fe',
    dot: '#6366f1',
  },
} as const;

export const BADGE_BASE_CLASS =
  'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold border transition-all select-none';

export const BADGE_DOT_BASE_CLASS = 'w-1.5 h-1.5 rounded-full shrink-0';

export const BADGE_VARIANTS: Record<BadgeVariant, BadgeStyleConfig> = {
  // Hoạt động / Đang bán / Đã duyệt / Hoàn tất / Đã thanh toán
  emerald: {
    wrapper: 'bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100/70',
    dot: 'bg-emerald-500',
  },
  // Chờ xử lý / Đang giao / Cảnh báo / Chờ duyệt
  amber: {
    wrapper: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-100/70',
    dot: 'bg-amber-500',
  },
  // Tạm khóa / Ngừng bán / Đã hủy / Từ chối / Lỗi
  rose: {
    wrapper: 'bg-rose-50 text-rose-700 border-rose-200 hover:bg-rose-100/70',
    dot: 'bg-rose-500',
  },
  // Lưu nháp / Chưa thanh toán / Mặc định
  slate: {
    wrapper: 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100/70',
    dot: 'bg-slate-400',
  },
  // Đã xác nhận / Đang kiểm đếm / Thông tin
  blue: {
    wrapper: 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100/70',
    dot: 'bg-blue-500',
  },
  // Đang đóng gói / Đang xử lý
  indigo: {
    wrapper: 'bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100/70',
    dot: 'bg-indigo-500',
  },
};

export interface StatusBadgeProps {
  label: React.ReactNode;
  variant?: BadgeVariant;
  showDot?: boolean;
  className?: string;
  onClick?: (e: React.MouseEvent) => void;
  title?: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  label,
  variant = 'emerald',
  showDot = true,
  className = '',
  onClick,
  title,
}) => {
  const config = BADGE_VARIANTS[variant] || BADGE_VARIANTS.slate;
  const isClickable = Boolean(onClick);

  if (isClickable) {
    return (
      <button
        type="button"
        onClick={onClick}
        title={title}
        className={`
          ${BADGE_BASE_CLASS}
          ${config.wrapper}
          cursor-pointer hover:shadow-xs active:scale-95
          ${className}
        `}
      >
        {showDot && <span className={`${BADGE_DOT_BASE_CLASS} ${config.dot}`} />}
        {label}
      </button>
    );
  }

  return (
    <span
      title={title}
      className={`
        ${BADGE_BASE_CLASS}
        ${config.wrapper}
        cursor-default
        ${className}
      `}
    >
      {showDot && <span className={`${BADGE_DOT_BASE_CLASS} ${config.dot}`} />}
      {label}
    </span>
  );
};

/**
 * Helper sinh nhanh props cho trạng thái Boolean (Active / Inactive)
 */
export const getActiveBadgeProps = (
  isActive: boolean,
  activeLabel = 'Hoạt động',
  inactiveLabel = 'Tạm khóa'
) => ({
  variant: (isActive ? 'emerald' : 'rose') as BadgeVariant,
  label: isActive ? activeLabel : inactiveLabel,
  title: 'Nhấn để đổi trạng thái',
});
