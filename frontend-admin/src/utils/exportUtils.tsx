import React, { useState } from 'react';

export interface ExportColumn<T> {
  header: string;
  key?: keyof T;
  accessor?: (item: T) => string | number | null | undefined | boolean;
}

export interface ExportCsvOptions<T> {
  filename: string;
  columns: ExportColumn<T>[];
  data: T[];
}

export interface FilterSnapshotItem {
  label: string;
  value: string;
}

/**
 * Xuất mảng dữ liệu ra file CSV với UTF-8 BOM (\uFEFF)
 * Bảo đảm Microsoft Excel hiển thị tiếng Việt có dấu chuẩn xác 100%.
 */
export function exportToCsv<T>({ filename, columns, data }: ExportCsvOptions<T>): void {
  if (!data || data.length === 0) {
    alert('Không có dữ liệu để xuất file.');
    return;
  }

  // 1. Tiêu đề cột
  const headers = columns.map((col) => escapeCsvCell(col.header)).join(',');

  // 2. Từng dòng dữ liệu
  const rows = data.map((item) => {
    return columns
      .map((col) => {
        let value: any = '';
        if (col.accessor) {
          value = col.accessor(item);
        } else if (col.key) {
          value = item[col.key];
        }

        if (value === null || value === undefined) {
          return '""';
        }
        return escapeCsvCell(String(value));
      })
      .join(',');
  });

  // 3. Ghép chuỗi với tiền tố BOM \uFEFF
  const csvContent = '\uFEFF' + [headers, ...rows].join('\r\n');

  // 4. Tạo Blob và tải file
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  // Chuẩn hóa tên file: thêm đuôi .csv nếu chưa có
  const cleanFilename = filename.endsWith('.csv') ? filename : `${filename}.csv`;
  link.setAttribute('href', url);
  link.setAttribute('download', cleanFilename);
  link.style.visibility = 'hidden';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Xử lý escape dấu ngoặc kép, phẩy, xuống dòng theo chuẩn RFC 4180
 */
function escapeCsvCell(cell: string): string {
  const needsQuotes =
    cell.includes(',') || cell.includes('"') || cell.includes('\n') || cell.includes('\r');
  const escaped = cell.replace(/"/g, '""');
  return needsQuotes ? `"${escaped}"` : `"${escaped}"`;
}

export interface ExportCsvButtonProps<T> {
  filename: string;
  columns: ExportColumn<T>[];
  data?: T[];
  currentPageData?: T[];
  totalItems?: number;
  filterSnapshots?: FilterSnapshotItem[];
  onFetchAll?: () => Promise<T[]>;
  label?: string;
  className?: string;
  disabled?: boolean;
}

/**
 * Nút Xuất CSV thông minh:
 * - Nếu có `filterSnapshots` hoặc `onFetchAll`: Mở Modal bước đệm "chụp" bộ lọc và chọn phạm vi xuất (Tất cả kết quả lọc vs Chỉ trang hiện tại).
 * - Nếu không có: Xuất trực tiếp dữ liệu `data` được truyền vào (dùng cho bảng thống kê dashboard).
 */
export function ExportCsvButton<T>({
  filename,
  columns,
  data,
  currentPageData,
  totalItems,
  filterSnapshots,
  onFetchAll,
  label = 'Xuất CSV',
  className = '',
  disabled = false,
}: ExportCsvButtonProps<T>) {
  const [modalOpen, setModalOpen] = useState(false);
  const [exportScope, setExportScope] = useState<'all' | 'current'>('all');
  const [isFetchingAll, setIsFetchingAll] = useState(false);

  // Dữ liệu trang hiện tại an toàn
  const safeCurrentPage = currentPageData || data || [];
  const effectiveTotal = totalItems ?? (data ? data.length : 0);

  // Bộ lọc hợp lệ (bỏ qua giá trị rỗng)
  const activeFilters = (filterSnapshots || []).filter(
    (f) => f.value && f.value.trim() !== '' && f.value !== 'Tất cả'
  );

  const handleClick = () => {
    // Nếu có modal bước đệm (khi có bộ lọc hoặc cần fetch toàn bộ)
    if (onFetchAll || (filterSnapshots && filterSnapshots.length > 0)) {
      setModalOpen(true);
      setExportScope('all');
    } else {
      // Xuất trực tiếp
      exportToCsv({ filename, columns, data: data || safeCurrentPage });
    }
  };

  const handleConfirmExport = async () => {
    if (exportScope === 'current') {
      exportToCsv({ filename, columns, data: safeCurrentPage });
      setModalOpen(false);
      return;
    }

    // Xuất toàn bộ kết quả lọc
    if (onFetchAll) {
      try {
        setIsFetchingAll(true);
        const allData = await onFetchAll();
        exportToCsv({ filename, columns, data: allData });
        setModalOpen(false);
      } catch (err) {
        console.error('Lỗi khi tải toàn bộ dữ liệu xuất CSV:', err);
        alert('Không thể tải dữ liệu để xuất file. Vui lòng thử lại.');
      } finally {
        setIsFetchingAll(false);
      }
    } else {
      exportToCsv({ filename, columns, data: data || safeCurrentPage });
      setModalOpen(false);
    }
  };

  const defaultStyles =
    'px-3.5 py-2 text-xs font-bold rounded-xl border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 hover:text-slate-900 transition-all shadow-xs cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed';

  return (
    <>
      <button
        type="button"
        onClick={handleClick}
        disabled={disabled || (effectiveTotal === 0 && safeCurrentPage.length === 0)}
        className={className || defaultStyles}
        title="Tải bảng dữ liệu về máy định dạng CSV (mở được bằng Excel)"
      >
        {label}
      </button>

      {/* MODAL BƯỚC ĐỆM CHỤP BỘ LỌC VÀ CHỌN PHẠM VI XUẤT */}
      {modalOpen && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 overflow-y-auto">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden border border-slate-200">
            {/* Header Modal */}
            <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <div>
                <h3 className="text-sm font-black text-slate-900 uppercase tracking-wider">
                  Xác Nhận Xuất Dữ Liệu CSV
                </h3>
                <p className="text-xs text-slate-500">
                  Kiểm tra các tiêu chí lọc và lựa chọn phạm vi dữ liệu trước khi tải
                </p>
              </div>
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                disabled={isFetchingAll}
                className="w-8 h-8 rounded-xl hover:bg-slate-200 text-slate-500 flex items-center justify-center font-bold transition-colors disabled:opacity-50"
              >
                ✕
              </button>
            </div>

            {/* Nội dung Modal */}
            <div className="p-5 space-y-4 text-xs text-slate-700 font-sans">
              {/* 1. KHỐI TÓM TẮT BỘ LỌC ĐANG ÁP DỤNG */}
              <div>
                <span className="font-bold text-slate-500 uppercase tracking-wider block text-[11px] mb-2">
                  1. Bộ lọc đang áp dụng (Filter Snapshot):
                </span>
                {activeFilters.length === 0 ? (
                  <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 text-slate-500 italic">
                    Không áp dụng bộ lọc (Xuất toàn bộ hệ thống)
                  </div>
                ) : (
                  <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 flex flex-wrap gap-2">
                    {activeFilters.map((f, idx) => (
                      <span
                        key={idx}
                        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white border border-slate-200 text-slate-800 font-medium shadow-2xs"
                      >
                        <strong className="text-slate-500">{f.label}:</strong> {f.value}
                      </span>
                    ))}
                  </div>
                )}
              </div>

              {/* 2. KHỐI LỰA CHỌN PHẠM VI XUẤT */}
              <div>
                <span className="font-bold text-slate-500 uppercase tracking-wider block text-[11px] mb-2">
                  2. Chọn phạm vi dữ liệu xuất:
                </span>
                <div className="space-y-2">
                  <label
                    className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-all ${
                      exportScope === 'all'
                        ? 'bg-amber-50/60 border-amber-400 text-slate-900 shadow-2xs'
                        : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="exportScope"
                      checked={exportScope === 'all'}
                      onChange={() => setExportScope('all')}
                      className="mt-0.5 text-amber-500 focus:ring-amber-400"
                    />
                    <div>
                      <div className="font-bold text-slate-900">
                        Xuất toàn bộ kết quả lọc ({effectiveTotal.toLocaleString('vi-VN')} bản ghi)
                      </div>
                      <div className="text-[11px] text-slate-500">
                        Tải đầy đủ tất cả các trang thỏa mãn các tiêu chí lọc ở trên.
                      </div>
                    </div>
                  </label>

                  <label
                    className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-all ${
                      exportScope === 'current'
                        ? 'bg-amber-50/60 border-amber-400 text-slate-900 shadow-2xs'
                        : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="exportScope"
                      checked={exportScope === 'current'}
                      onChange={() => setExportScope('current')}
                      className="mt-0.5 text-amber-500 focus:ring-amber-400"
                    />
                    <div>
                      <div className="font-bold text-slate-900">
                        Chỉ xuất trang hiện tại ({safeCurrentPage.length} bản ghi)
                      </div>
                      <div className="text-[11px] text-slate-500">
                        Chỉ xuất các dòng dữ liệu đang hiển thị trên bảng.
                      </div>
                    </div>
                  </label>
                </div>
              </div>
            </div>

            {/* Footer Modal Actions */}
            <div className="p-4 bg-slate-50 border-t border-slate-200 flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                disabled={isFetchingAll}
                className="px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-200 rounded-xl transition-all disabled:opacity-50"
              >
                Hủy Bỏ
              </button>
              <button
                type="button"
                onClick={handleConfirmExport}
                disabled={isFetchingAll}
                className="px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-bold transition-all shadow-xs flex items-center gap-1.5 disabled:opacity-50"
              >
                {isFetchingAll ? 'Đang Tải Dữ Liệu...' : 'Tải File CSV Ngay'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
