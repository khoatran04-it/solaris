import React, { useState, useEffect, useCallback } from 'react';
import {
  Scale,
  FileSpreadsheet,
  History,
  ArrowDownRight,
  ArrowUpRight,
  RefreshCw,
  Box,
} from 'lucide-react';

import {
  ListPageContainer,
  ListHeader,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateTimeCell,
} from '../../components/commons/ListUI';
import { TabGroup, TabButton } from '../../components/commons/TabUI';
import { FormSelect } from '../../components/commons/FormUI';
import CustomDatePicker from '../../components/commons/CustomDatePicker';
import { Toast } from '../../components/commons/Toast';

import { inventoryReconciliationApi } from '../../api/inventoryReconciliationApi';
import { warehouseApi } from '../../api/warehouseApi';
import { ShiftClosingReport, StockLedgerEntry } from '../../types/inventoryReconciliation';

const InventoryReconciliation: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'shift' | 'ledger'>('shift');

  // --- FILTER STATES ---
  const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number>(0);
  const [fromDate, setFromDate] = useState<Date>(
    new Date(new Date().setDate(new Date().getDate() - 7))
  );
  const [toDate, setToDate] = useState<Date>(new Date());

  // --- DATA STATES: SHIFT REPORT ---
  const [shiftData, setShiftData] = useState<ShiftClosingReport | null>(null);
  const [shiftLoading, setShiftLoading] = useState(false);

  // --- DATA STATES: LEDGER ---
  const [ledgerEntries, setLedgerEntries] = useState<StockLedgerEntry[]>([]);
  const [ledgerLoading, setLedgerLoading] = useState(false);
  const [ledgerPage, setLedgerPage] = useState(1);
  const [ledgerTotalPages, setLedgerTotalPages] = useState(0);
  const [ledgerTotalItems, setLedgerTotalItems] = useState(0);
  const ledgerPageSize = 15;

  // --- TOAST ---
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // Helper format ngày YYYY-MM-DD theo giờ địa phương Việt Nam (tránh lệch timezone)
  const formatDateParam = (d: Date) => {
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  // --- EFFECT: LOAD WAREHOUSES ---
  useEffect(() => {
    const loadWarehouses = async () => {
      try {
        const whList = await warehouseApi.getAllList().catch(() => []);
        const opts = whList.map((w: any) => ({ label: `${w.name} (${w.code})`, value: w.id }));
        setWarehouseOptions(opts);
        if (opts.length > 0) {
          setSelectedWarehouseId(opts[0].value);
        }
      } catch (err) {
        console.error('Error loading warehouses:', err);
      }
    };
    loadWarehouses();
  }, []);

  // --- FETCH SHIFT REPORT ---
  const fetchShiftReport = useCallback(async () => {
    if (!selectedWarehouseId) return;
    try {
      setShiftLoading(true);
      const data = await inventoryReconciliationApi.getShiftClosing(
        selectedWarehouseId,
        formatDateParam(fromDate),
        formatDateParam(toDate)
      );
      setShiftData(data);
    } catch (err) {
      console.error('Error fetching shift report:', err);
      showToast('error', 'Không thể tải báo cáo chốt ca kho!');
    } finally {
      setShiftLoading(false);
    }
  }, [selectedWarehouseId, fromDate, toDate]);

  // --- FETCH LEDGER ---
  const fetchLedger = useCallback(async () => {
    if (!selectedWarehouseId) return;
    try {
      setLedgerLoading(true);
      const data = await inventoryReconciliationApi.getStockLedger(
        selectedWarehouseId,
        undefined, // Tạm thời không filter theo ProductVariant ở màn hình tổng này
        formatDateParam(fromDate),
        formatDateParam(toDate),
        ledgerPage,
        ledgerPageSize
      );
      setLedgerEntries(data.items || []);
      setLedgerTotalPages(data.totalPages || 0);
      setLedgerTotalItems(data.totalRecords || 0);
    } catch (err) {
      console.error('Error fetching stock ledger:', err);
      showToast('error', 'Không thể tải sổ cái giao dịch!');
    } finally {
      setLedgerLoading(false);
    }
  }, [selectedWarehouseId, fromDate, toDate, ledgerPage]);

  // --- EFFECT: TRIGGER FETCH WHEN TAB OR FILTERS CHANGE ---
  useEffect(() => {
    if (activeTab === 'shift') {
      fetchShiftReport();
    } else {
      fetchLedger();
    }
  }, [activeTab, fetchShiftReport, fetchLedger]);

  // Tự động về trang 1 của Ledger nếu đổi điều kiện lọc
  useEffect(() => {
    setLedgerPage(1);
  }, [selectedWarehouseId, fromDate, toDate]);

  return (
    <ListPageContainer>
      <Toast {...toast} />

      {/* ListHeader */}
      <div className="[&>div>div:last-child]:hidden">
        <ListHeader
          title="Cân Đối Phát Sinh & Sổ Cái (Reconciliation)"
          subtitle="Theo dõi biến động Nhập - Xuất - Tồn và lưu vết mọi giao dịch kho"
          icon={Scale}
          searchTerm=""
          onSearchChange={() => {}}
          onAdd={() => {}}
        />
      </div>

      {/* ================= GLOBAL REPORT PARAMETERS BAR ================= */}
      <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4 mb-6 p-5 bg-white border border-slate-100 rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)]">
        <div className="flex flex-wrap items-center gap-3.5 w-full lg:w-auto">
          <span className="text-xs font-extrabold text-slate-600 uppercase tracking-wider shrink-0">
            Tham số Báo cáo:
          </span>
          <div className="w-px h-6 bg-slate-200 hidden md:block"></div>

          {/* Chọn Kho */}
          <div className="w-64 min-w-[200px]">
            <FormSelect
              label=""
              options={warehouseOptions}
              value={selectedWarehouseId}
              onSelect={(val) => setSelectedWarehouseId(Number(val))}
              placeholder="-- Chọn Kho hàng --"
              showSearch
              searchPlaceholder="Tìm kiếm kho..."
            />
          </div>

          {/* Từ ngày */}
          <div className="w-44">
            <CustomDatePicker
              label=""
              value={fromDate}
              onChange={(d) => d && setFromDate(d)}
              placeholder="Từ ngày"
            />
          </div>

          {/* Đến ngày */}
          <div className="w-44">
            <CustomDatePicker
              label=""
              value={toDate}
              onChange={(d) => d && setToDate(d)}
              placeholder="Đến ngày"
            />
          </div>
        </div>

        <div className="flex items-center gap-2 w-full lg:w-auto justify-end">
          <button
            onClick={() => (activeTab === 'shift' ? fetchShiftReport() : fetchLedger())}
            className="flex items-center gap-2 px-5 py-2.5 bg-amber-50 hover:bg-amber-100 text-amber-900 border border-amber-200 font-bold text-sm rounded-xl transition-all shadow-2xs cursor-pointer"
          >
            <RefreshCw size={16} className={shiftLoading || ledgerLoading ? 'animate-spin' : ''} />
            Tải Lại Số Liệu
          </button>
        </div>
      </div>

      {/* ================= TABS NAVIGATION ================= */}
      <div className="mb-4">
        <TabGroup>
          <TabButton
            active={activeTab === 'shift'}
            onClick={() => setActiveTab('shift')}
            icon={FileSpreadsheet}
            label="1. BẢNG CÂN ĐỐI PHÁT SINH (CHỐT CA)"
          />
          <TabButton
            active={activeTab === 'ledger'}
            onClick={() => setActiveTab('ledger')}
            icon={History}
            label="2. SỔ CÁI GIAO DỊCH (AUDIT TRAIL)"
          />
        </TabGroup>
      </div>

      {/* ================= TAB 1: BẢNG CÂN ĐỐI PHÁT SINH ================= */}
      {activeTab === 'shift' && (
        <div className="animate-in fade-in duration-300">
          {/* METRIC OVERVIEW CARDS */}
          {shiftData && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <div className="p-5 bg-white border border-slate-200/80 rounded-2xl shadow-2xs relative overflow-hidden group">
                <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:opacity-20 transition-opacity">
                  <Box size={48} />
                </div>
                <div className="text-xs font-bold text-slate-500 uppercase tracking-wider relative z-10">
                  Tồn Đầu Kỳ
                </div>
                <div className="text-3xl font-black text-slate-800 mt-2 relative z-10">
                  {shiftData.totalOpeningItems.toLocaleString('vi-VN')}
                </div>
              </div>
              <div className="p-5 bg-emerald-500 border border-emerald-600 rounded-2xl shadow-sm shadow-emerald-200/50 relative overflow-hidden group">
                <div className="absolute top-0 right-0 p-4 opacity-20 group-hover:scale-110 transition-transform">
                  <ArrowDownRight size={48} className="text-emerald-900" />
                </div>
                <div className="text-xs font-bold text-emerald-100 uppercase tracking-wider relative z-10">
                  Tổng Phát Sinh Nhập (+)
                </div>
                <div className="text-3xl font-black text-white mt-2 relative z-10">
                  +{shiftData.totalInflowItems.toLocaleString('vi-VN')}
                </div>
              </div>
              <div className="p-5 bg-rose-500 border border-rose-600 rounded-2xl shadow-sm shadow-rose-200/50 relative overflow-hidden group">
                <div className="absolute top-0 right-0 p-4 opacity-20 group-hover:scale-110 transition-transform">
                  <ArrowUpRight size={48} className="text-rose-900" />
                </div>
                <div className="text-xs font-bold text-rose-100 uppercase tracking-wider relative z-10">
                  Tổng Phát Sinh Xuất (-)
                </div>
                <div className="text-3xl font-black text-white mt-2 relative z-10">
                  -{shiftData.totalOutflowItems.toLocaleString('vi-VN')}
                </div>
              </div>
              <div className="p-5 bg-indigo-600 border border-indigo-700 rounded-2xl shadow-sm shadow-indigo-200/50 relative overflow-hidden group">
                <div className="absolute top-0 right-0 p-4 opacity-20 group-hover:scale-110 transition-transform">
                  <Scale size={48} className="text-indigo-900" />
                </div>
                <div className="text-xs font-bold text-indigo-200 uppercase tracking-wider relative z-10">
                  Tồn Cuối Kỳ Lý Thuyết
                </div>
                <div className="text-3xl font-black text-white mt-2 relative z-10">
                  {shiftData.totalClosingItems.toLocaleString('vi-VN')}
                </div>
              </div>
            </div>
          )}

          <ListCard>
            <div className="overflow-x-auto min-h-[400px] pb-24">
              <table className="w-full text-left whitespace-nowrap text-sm min-w-[1200px]">
                <thead className="bg-slate-50 text-slate-600 font-bold uppercase tracking-wider border-b border-slate-200 text-xs">
                  <tr>
                    <th className="px-4 py-4 text-center w-12">#</th>
                    <th className="px-4 py-4 min-w-30">Mã SKU</th>
                    <th className="px-4 py-4 min-w-50">Tên Sản Phẩm</th>
                    <th className="px-4 py-4 text-center min-w-20">ĐVT</th>

                    <th className="px-4 py-4 text-center bg-slate-100/80 min-w-25 border-l border-slate-200">
                      Tồn Đầu
                    </th>

                    <th className="px-4 py-4 text-center text-emerald-800 bg-emerald-50/60 min-w-25 border-l border-emerald-100">
                      Nhập Mua
                    </th>
                    <th className="px-4 py-4 text-center text-emerald-800 bg-emerald-50/60 min-w-25">
                      Chuyển Đến
                    </th>
                    <th className="px-4 py-4 text-center text-emerald-800 bg-emerald-50/60 min-w-25">
                      Khách Trả
                    </th>

                    <th className="px-4 py-4 text-center text-rose-800 bg-rose-50/60 min-w-25 border-l border-rose-100">
                      Xuất Bán
                    </th>
                    <th className="px-4 py-4 text-center text-rose-800 bg-rose-50/60 min-w-25">
                      Chuyển Đi
                    </th>

                    <th className="px-4 py-4 text-center text-amber-800 bg-amber-50/60 min-w-28 border-l border-amber-100">
                      Điều Chỉnh (±)
                    </th>

                    <th className="px-4 py-4 text-center font-black text-indigo-900 bg-indigo-100/60 min-w-30 border-l border-indigo-200">
                      Tồn Cuối Kỳ
                    </th>

                    <th className="px-4 py-4 text-center text-emerald-700 min-w-25 border-l border-slate-200">
                      Khả Dụng
                    </th>
                    <th className="px-4 py-4 text-center text-indigo-700 min-w-25">Giữ Chỗ</th>
                    <th className="px-4 py-4 text-center text-rose-700 min-w-25">Hàng Hỏng</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {shiftLoading ? (
                    <TableLoading colSpan={15} />
                  ) : !shiftData || shiftData.items.length === 0 ? (
                    <TableEmpty
                      colSpan={15}
                      message="Không có phát sinh giao dịch nào trong khoảng thời gian này."
                    />
                  ) : (
                    shiftData.items.map((row, idx) => (
                      <tr key={row.variantId} className="hover:bg-slate-50/70 transition-colors">
                        <td className="px-4 py-3 text-center text-slate-400">{idx + 1}</td>
                        <td className="px-4 py-3 font-mono font-bold text-indigo-700">
                          {row.variantCode}
                        </td>
                        <td className="px-4 py-3 font-bold text-slate-800">{row.variantName}</td>
                        <td className="px-4 py-3 text-center text-slate-600 font-medium">
                          {row.uoMName || row.uomName || '-'}
                        </td>

                        <td className="px-4 py-3 text-center font-bold text-[13px] text-slate-700 bg-slate-50/40 border-l border-slate-100">
                          {row.openingStock}
                        </td>

                        <td className="px-4 py-3 text-center font-bold text-[13px] text-emerald-600 bg-emerald-50/20 border-l border-emerald-50">
                          {row.totalReceipt > 0 ? `+${row.totalReceipt}` : '-'}
                        </td>
                        <td className="px-4 py-3 text-center font-bold text-[13px] text-emerald-600 bg-emerald-50/20">
                          {row.totalTransferIn > 0 ? `+${row.totalTransferIn}` : '-'}
                        </td>
                        <td className="px-4 py-3 text-center font-bold text-[13px] text-emerald-600 bg-emerald-50/20">
                          {row.totalReturn > 0 ? `+${row.totalReturn}` : '-'}
                        </td>

                        <td className="px-4 py-3 text-center font-bold text-[13px] text-rose-600 bg-rose-50/20 border-l border-rose-50">
                          {row.totalIssue > 0 ? `-${row.totalIssue}` : '-'}
                        </td>
                        <td className="px-4 py-3 text-center font-bold text-[13px] text-rose-600 bg-rose-50/20">
                          {row.totalTransferOut > 0 ? `-${row.totalTransferOut}` : '-'}
                        </td>

                        <td className="px-4 py-3 text-center font-black text-[13px] text-amber-600 bg-amber-50/20 border-l border-amber-50">
                          {row.totalAdjustment !== 0
                            ? row.totalAdjustment > 0
                              ? `+${row.totalAdjustment}`
                              : row.totalAdjustment
                            : '-'}
                        </td>

                        <td className="px-4 py-3 text-center font-black text-[15px] text-indigo-900 bg-indigo-50/50 border-l border-indigo-100">
                          {row.closingStock}
                        </td>

                        <td className="px-4 py-3 text-center font-bold text-[13px] text-emerald-700 border-l border-slate-100">
                          {row.currentAvailable}
                        </td>
                        <td className="px-4 py-3 text-center font-bold text-[13px] text-indigo-700">
                          {row.currentReserved}
                        </td>
                        <td className="px-4 py-3 text-center font-bold text-[13px] text-rose-700">
                          {row.currentDamaged}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </ListCard>
        </div>
      )}

      {/* ================= TAB 2: SỔ CÁI GIAO DỊCH ================= */}
      {activeTab === 'ledger' && (
        <div className="animate-in fade-in duration-300">
          <ListCard>
            <div className="overflow-x-auto min-h-[400px] pb-24">
              <table className="w-full text-left whitespace-nowrap text-sm min-w-[1100px]">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-4 text-center w-12">#</th>
                    <th className="px-4 py-4 min-w-[130px]">Thời Gian</th>
                    <th className="px-4 py-4 min-w-[170px]">Mã Giao Dịch</th>
                    <th className="px-4 py-4 min-w-[120px]">Loại Nghiệp Vụ</th>
                    <th className="px-4 py-4 min-w-[130px]">Chứng Từ Gốc</th>
                    <th className="px-4 py-4 min-w-[180px]">Sản Phẩm</th>
                    <th className="px-4 py-4 min-w-[150px]">Lô Hàng</th>
                    <th className="px-4 py-4 text-center min-w-[100px] bg-indigo-50/50 text-indigo-900">
                      Biến Động
                    </th>
                    <th className="px-4 py-4 min-w-[140px]">Người Thực Hiện</th>
                    <th className="px-4 py-4 min-w-[180px]">Ghi Chú</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {ledgerLoading ? (
                    <TableLoading colSpan={10} />
                  ) : ledgerEntries.length === 0 ? (
                    <TableEmpty
                      colSpan={10}
                      message="Chưa có giao dịch sổ cái nào trong khoảng thời gian này."
                    />
                  ) : (
                    ledgerEntries.map((entry, idx) => {
                      const isPositive = entry.quantity > 0;
                      return (
                        <tr
                          key={entry.transactionCode || idx}
                          className="hover:bg-slate-50/80 transition-colors"
                        >
                          <td className="px-4 py-3 text-center text-slate-400 text-xs">
                            {(ledgerPage - 1) * ledgerPageSize + idx + 1}
                          </td>
                          <td className="px-4 py-3 text-slate-600">
                            <DateTimeCell isoString={entry.transactionDate} />
                          </td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center px-2 py-1 rounded-md bg-slate-100 text-slate-700 font-mono text-xs font-bold border border-slate-200/80">
                              {entry.transactionCode}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className="text-[11px] font-bold uppercase tracking-wider text-slate-700 bg-slate-100 px-2 py-0.5 rounded">
                              {entry.transactionType}
                            </span>
                          </td>
                          <td className="px-4 py-3 font-mono font-bold text-indigo-600 text-xs">
                            {entry.referenceCode || '---'}
                          </td>
                          <td className="px-4 py-3 font-bold text-slate-800">
                            {entry.variantName}
                          </td>
                          <td className="px-4 py-3 font-mono font-bold text-indigo-700 text-xs">
                            {entry.batchCode || '---'}
                          </td>

                          {/* Cột Biến Động */}
                          <td className="px-4 py-3 text-center bg-indigo-50/20 border-l border-indigo-50">
                            <span
                              className={`text-[15px] font-black ${isPositive ? 'text-emerald-600' : 'text-rose-600'}`}
                            >
                              {entry.quantity > 0 ? `+${entry.quantity}` : entry.quantity}
                            </span>
                          </td>

                          <td className="px-4 py-3 text-slate-600 text-[13px] font-medium">
                            {entry.performedBy}
                          </td>
                          <td
                            className="px-4 py-3 text-[13px] italic text-slate-500 max-w-xs truncate"
                            title={entry.note}
                          >
                            {entry.note || '---'}
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            <ListPagination
              currentPage={ledgerPage}
              totalPages={ledgerTotalPages}
              totalItems={ledgerTotalItems}
              onPageChange={setLedgerPage}
              isLoading={ledgerLoading}
            />
          </ListCard>
        </div>
      )}
    </ListPageContainer>
  );
};

export default InventoryReconciliation;
