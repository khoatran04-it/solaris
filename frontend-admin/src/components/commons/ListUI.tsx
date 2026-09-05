import React from 'react';
import { Search, Plus, Loader2, Inbox, ChevronLeft, ChevronRight, LucideIcon } from 'lucide-react';

// Re-export Badge System
export * from './Badge';

// 0. Container riêng cho trang Danh sách
export const ListPageContainer: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="h-full flex flex-col p-6 bg-slate-50/30 overflow-y-auto font-sans">
    <div className="max-w-[1600px] w-full mx-auto flex flex-col gap-8 pb-12">{children}</div>
  </div>
);

// 1. Container bọc bảng (Card)
export const ListCard: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="flex-1 bg-white rounded-2xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)] border border-slate-100 flex flex-col overflow-hidden">
    {children}
  </div>
);

// 2. Header trang danh sách
interface ListHeaderProps {
  title: string;
  subtitle: string;
  searchTerm?: string;
  onSearchChange: (val: string) => void;
  onAdd: () => void;
  searchPlaceholder?: string;
  icon: LucideIcon;
}
export const ListHeader: React.FC<ListHeaderProps> = ({
  title,
  subtitle,
  searchTerm,
  onSearchChange,
  onAdd,
  searchPlaceholder,
  icon: Icon,
}) => (
  <div className="flex justify-between items-end mb-2">
    <div>
      <div className="flex items-center gap-2 mb-1.5">
        <Icon className="w-6 h-6 text-yellow-500 fill-yellow-100" />
        <h2 className="m-0 font-bold text-3xl text-slate-800 tracking-tight">{title}</h2>
      </div>
      <p className="text-slate-500 text-sm font-medium ml-8 truncate max-w-2xl">{subtitle}</p>
    </div>
    <div className="flex gap-4">
      <div className="flex items-center bg-white/80 backdrop-blur-md border border-slate-200/80 rounded-xl px-4 py-2.5 w-80 focus-within:border-yellow-400 focus-within:ring-4 focus-within:ring-yellow-400/20 transition-all duration-300 shadow-sm">
        <Search size={18} className="text-slate-400 mr-2.5" />
        <input
          type="text"
          placeholder={searchPlaceholder || 'Tìm kiếm...'}
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
          className="border-none outline-none w-full text-sm bg-transparent placeholder-slate-400 text-slate-700 font-medium"
        />
      </div>
      <button
        onClick={onAdd}
        className="group bg-yellow-400 hover:bg-yellow-500 text-slate-900 border-none rounded-xl px-6 py-2.5 text-[15px] flex items-center gap-2 cursor-pointer font-bold transition-all duration-300 shadow-[0_4px_14px_0_rgba(250,204,21,0.39)] hover:shadow-[0_6px_20px_rgba(250,204,21,0.23)] hover:-translate-y-0.5"
      >
        <Plus size={18} strokeWidth={3} className="transition-transform group-hover:rotate-90" />{' '}
        THÊM
      </button>
    </div>
  </div>
);

// 3. Trạng thái Loading cho Table
export const TableLoading: React.FC<{ colSpan: number }> = ({ colSpan }) => (
  <tr>
    <td colSpan={colSpan} className="py-24 text-center bg-slate-50/30">
      <div className="flex flex-col items-center justify-center text-slate-400">
        <div className="relative mb-4">
          <div className="absolute inset-0 border-4 border-yellow-200 rounded-full animate-ping opacity-20"></div>
          <Loader2 className="w-10 h-10 animate-spin text-yellow-500 relative z-10" />
        </div>
        <span className="text-sm font-semibold tracking-wide uppercase">Đang tải dữ liệu...</span>
      </div>
    </td>
  </tr>
);

// 4. Trạng thái Trống (Empty)
export const TableEmpty: React.FC<{ colSpan: number; message?: string }> = ({
  colSpan,
  message,
}) => (
  <tr>
    <td colSpan={colSpan} className="py-24 text-center bg-slate-50/30">
      <div className="flex flex-col items-center justify-center text-slate-400">
        <div className="bg-white p-5 rounded-2xl shadow-sm border border-slate-100 mb-4 text-slate-300">
          <Inbox className="w-12 h-12" strokeWidth={1.5} />
        </div>
        <span className="text-base font-bold text-slate-600 mb-1">Không tìm thấy dữ liệu</span>
        <span className="text-sm font-medium text-slate-400">
          {message || 'Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc.'}
        </span>
      </div>
    </td>
  </tr>
);

// 5. Phân trang (Pagination)
interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  totalItems: number;
  isLoading?: boolean;
}
export const ListPagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  totalItems,
  isLoading,
}) => (
  <div className="flex justify-between items-center px-6 py-4 border-t border-slate-100 bg-white">
    <span className="text-sm font-medium text-slate-500">
      Hiển thị <strong className="text-slate-800 font-bold">{totalItems}</strong> kết quả
    </span>
    <div className="flex items-center gap-2">
      <button
        onClick={() => onPageChange(Math.max(currentPage - 1, 1))}
        disabled={currentPage <= 1 || isLoading}
        className="p-2 rounded-lg border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 disabled:opacity-40 transition-all shadow-sm"
      >
        <ChevronLeft size={18} />
      </button>
      <div className="flex items-center px-3 py-1.5 rounded-lg bg-slate-50 border border-slate-200 text-sm font-semibold text-slate-600">
        <span className="text-slate-900 mx-1">{totalPages === 0 ? 0 : currentPage}</span>
        <span className="mx-1 text-slate-400">/</span>
        <span className="mx-1">{totalPages}</span>
      </div>
      <button
        onClick={() => onPageChange(Math.min(currentPage + 1, totalPages))}
        disabled={currentPage >= totalPages || totalPages === 0 || isLoading}
        className="p-2 rounded-lg border border-slate-200 text-slate-600 bg-white hover:bg-slate-50 disabled:opacity-40 transition-all shadow-sm"
      >
        <ChevronRight size={18} />
      </button>
    </div>
  </div>
);

// 6. Cell Ngày thuần túy (DD/MM/YYYY chuẩn múi giờ Việt Nam)
export const DateCell: React.FC<{ isoString?: string | null }> = ({ isoString }) => {
  if (!isoString) return <span className="text-slate-400 font-medium">-</span>;

  let safeIsoString = isoString;
  if (!isoString.endsWith('Z') && !isoString.includes('+')) {
    safeIsoString = `${isoString}Z`;
  }

  const d = new Date(safeIsoString);
  if (isNaN(d.getTime())) {
    return <span className="text-sm font-semibold text-slate-700">{isoString}</span>;
  }

  const dateStr = d.toLocaleDateString('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  return <span className="text-sm font-semibold text-slate-700 tracking-tight">{dateStr}</span>;
};

// 7. Cell Ngày giờ (Date + Time chuẩn múi giờ Việt Nam)
export const DateTimeCell: React.FC<{ isoString?: string | null }> = ({ isoString }) => {
  if (!isoString) return <span className="text-slate-400 font-medium">-</span>;

  let safeIsoString = isoString;
  if (!isoString.endsWith('Z') && !isoString.includes('+')) {
    safeIsoString = `${isoString}Z`;
  }

  const d = new Date(safeIsoString);
  if (isNaN(d.getTime())) {
    return <span className="text-sm font-semibold text-slate-700">{isoString}</span>;
  }

  const dateStr = d.toLocaleDateString('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
  const timeStr = d.toLocaleTimeString('vi-VN', {
    timeZone: 'Asia/Ho_Chi_Minh',
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div className="flex flex-col items-start">
      <span className="text-sm font-semibold text-slate-700 tracking-tight">{dateStr}</span>
      <span className="text-[11px] text-slate-400 font-bold uppercase tracking-tighter">
        {timeStr}
      </span>
    </div>
  );
};
