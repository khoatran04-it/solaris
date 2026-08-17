import React, { useState, useEffect, useCallback } from 'react';
import {
    Scale,
    FileSpreadsheet,
    History,
    Calendar,
    Warehouse as WarehouseIcon,
    ArrowDownRight,
    ArrowUpRight,
    RefreshCw,
    Download
} from 'lucide-react';

import {
    ListPageContainer,
    ListHeader,
    ListCard,
    TableLoading,
    TableEmpty,
    ListPagination,
    DateTimeCell
} from '../../components/commons/ListUI';
import { TabGroup, TabButton } from '../../components/commons/TabUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';

import { inventoryReconciliationApi } from '../../api/inventoryReconciliationApi';
import { warehouseApi } from '../../api/warehouseApi';
import { ShiftClosingReport, StockLedgerEntry } from '../../types/inventoryReconciliation';

const InventoryReconciliation: React.FC = () => {
    const [activeTab, setActiveTab] = useState<'shift' | 'ledger'>('shift');

    // Filter states
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);
    const [selectedWarehouseId, setSelectedWarehouseId] = useState<number>(0);
    const [fromDate, setFromDate] = useState<Date>(new Date(new Date().setDate(new Date().getDate() - 7)));
    const [toDate, setToDate] = useState<Date>(new Date());

    // Shift report data
    const [shiftData, setShiftData] = useState<ShiftClosingReport | null>(null);
    const [shiftLoading, setShiftLoading] = useState(false);

    // Ledger data
    const [ledgerEntries, setLedgerEntries] = useState<StockLedgerEntry[]>([]);
    const [ledgerLoading, setLedgerLoading] = useState(false);
    const [ledgerPage, setLedgerPage] = useState(1);
    const [ledgerTotalPages, setLedgerTotalPages] = useState(0);
    const [ledgerTotalItems, setLedgerTotalItems] = useState(0);
    const ledgerPageSize = 15;

    // Toast
    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>({
        show: false,
        type: 'success',
        message: '',
    });

    const showToast = (type: 'success' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // Load warehouse list
    useEffect(() => {
        const loadWarehouses = async () => {
            try {
                const whList = await warehouseApi.getAllList().catch(() => []);
                const opts = whList.map((w: any) => ({ label: w.name, value: w.id }));
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

    // Fetch Shift Closing Report
    const fetchShiftReport = useCallback(async () => {
        if (!selectedWarehouseId) return;
        try {
            setShiftLoading(true);
            const data = await inventoryReconciliationApi.getShiftClosing(
                selectedWarehouseId,
                fromDate.toLocaleDateString('en-CA'),
                toDate.toLocaleDateString('en-CA')
            );
            setShiftData(data);
        } catch (err) {
            console.error('Error fetching shift report:', err);
            showToast('error', 'Không thể tải báo cáo chốt ca kho!');
        } finally {
            setShiftLoading(false);
        }
    }, [selectedWarehouseId, fromDate, toDate]);

    // Fetch Stock Ledger Audit Trail
    const fetchLedger = useCallback(async () => {
        if (!selectedWarehouseId) return;
        try {
            setLedgerLoading(true);
            const data = await inventoryReconciliationApi.getStockLedger(
                selectedWarehouseId,
                undefined,
                fromDate.toLocaleDateString('en-CA'),
                toDate.toLocaleDateString('en-CA'),
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

    useEffect(() => {
        if (activeTab === 'shift') {
            fetchShiftReport();
        } else {
            fetchLedger();
        }
    }, [activeTab, fetchShiftReport, fetchLedger]);

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                title="Sổ Cái & Chốt Ca Tồn Kho (Reconciliation)"
                subtitle="Bảng cân đối phát sinh Nhập - Xuất - Tồn - Hao hụt & Lịch sử biến động sổ cái"
                icon={Scale}
            />

            {/* TOP FILTER BAR */}
            <div className="flex flex-wrap items-center justify-between gap-4 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
                <div className="flex flex-wrap items-center gap-3">
                    <CustomFilter
                        label="Chọn kho"
                        options={warehouseOptions}
                        selectedValues={selectedWarehouseId ? [selectedWarehouseId] : []}
                        onChange={(val) => {
                            if (val.length > 0) setSelectedWarehouseId(Number(val[0]));
                        }}
                        isMulti={false}
                    />

                    <CustomDateFilter
                        label="Từ ngày"
                        selectedDate={fromDate}
                        onChange={(d) => d && setFromDate(d)}
                    />

                    <CustomDateFilter
                        label="Đến ngày"
                        selectedDate={toDate}
                        onChange={(d) => d && setToDate(d)}
                    />
                </div>

                <div className="flex items-center gap-2">
                    <button
                        onClick={() => activeTab === 'shift' ? fetchShiftReport() : fetchLedger()}
                        className="p-2 text-slate-600 hover:bg-slate-100 rounded-lg transition-colors"
                        title="Tải lại dữ liệu"
                    >
                        <RefreshCw size={18} />
                    </button>
                </div>
            </div>

            {/* TAB NAVIGATION */}
            <TabGroup>
                <TabButton
                    active={activeTab === 'shift'}
                    onClick={() => setActiveTab('shift')}
                    icon={FileSpreadsheet}
                    label="1. BẢNG CÂN ĐỐI PHÁT SINH (CHỐT CA KHO)"
                />
                <TabButton
                    active={activeTab === 'ledger'}
                    onClick={() => setActiveTab('ledger')}
                    icon={History}
                    label="2. SỔ CÁI GIAO DỊCH TỒN KHO (AUDIT TRAIL)"
                />
            </TabGroup>

            {/* TAB 1: BẢNG CÂN ĐỐI PHÁT SINH */}
            {activeTab === 'shift' && (
                <div>
                    {/* METRIC OVERVIEW */}
                    {shiftData && (
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                            <div className="p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
                                <div className="text-xs font-bold text-slate-500 uppercase tracking-wider">Tồn Đầu Kỳ</div>
                                <div className="text-2xl font-black text-slate-800 mt-1">{shiftData.totalOpeningItems}</div>
                            </div>
                            <div className="p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
                                <div className="text-xs font-bold text-emerald-600 uppercase tracking-wider flex items-center gap-1">
                                    <ArrowDownRight size={14} /> Tổng Phát Sinh Nhập (+)
                                </div>
                                <div className="text-2xl font-black text-emerald-700 mt-1">+{shiftData.totalInflowItems}</div>
                            </div>
                            <div className="p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
                                <div className="text-xs font-bold text-rose-600 uppercase tracking-wider flex items-center gap-1">
                                    <ArrowUpRight size={14} /> Tổng Phát Sinh Xuất (-)
                                </div>
                                <div className="text-2xl font-black text-rose-700 mt-1">-{shiftData.totalOutflowItems}</div>
                            </div>
                            <div className="p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
                                <div className="text-xs font-bold text-indigo-600 uppercase tracking-wider">Tồn Cuối Kỳ Lý Thuyết</div>
                                <div className="text-2xl font-black text-indigo-800 mt-1">{shiftData.totalClosingItems}</div>
                            </div>
                        </div>
                    )}

                    <ListCard>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left whitespace-nowrap text-xs">
                                <thead className="bg-slate-50 text-slate-600 font-bold uppercase tracking-wider border-b border-slate-200">
                                    <tr>
                                        <th className="px-3 py-3 text-center w-10">#</th>
                                        <th className="px-3 py-3">Mã SKU</th>
                                        <th className="px-3 py-3 min-w-40">Tên Sản Phẩm</th>
                                        <th className="px-3 py-3 text-center">ĐVT</th>
                                        <th className="px-3 py-3 text-center bg-slate-100/60">Tồn Đầu</th>
                                        <th className="px-3 py-3 text-center text-emerald-700 bg-emerald-50/40">Nhập Mua</th>
                                        <th className="px-3 py-3 text-center text-emerald-700 bg-emerald-50/40">Chuyển Đến</th>
                                        <th className="px-3 py-3 text-center text-emerald-700 bg-emerald-50/40">Khách Trả</th>
                                        <th className="px-3 py-3 text-center text-rose-700 bg-rose-50/40">Xuất Bán</th>
                                        <th className="px-3 py-3 text-center text-rose-700 bg-rose-50/40">Chuyển Đi</th>
                                        <th className="px-3 py-3 text-center bg-amber-50/40">Điều Chỉnh (±)</th>
                                        <th className="px-3 py-3 text-center font-black text-indigo-900 bg-indigo-50/60">Tồn Cuối Kỳ</th>
                                        <th className="px-3 py-3 text-center text-emerald-700 font-bold">Khả Dụng</th>
                                        <th className="px-3 py-3 text-center text-indigo-700 font-bold">Giữ Chỗ</th>
                                        <th className="px-3 py-3 text-center text-rose-700 font-bold">Hàng Hỏng</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {shiftLoading ? (
                                        <TableLoading colSpan={15} message="Đang kết xuất bảng cân đối tồn kho..." />
                                    ) : !shiftData || shiftData.items.length === 0 ? (
                                        <TableEmpty colSpan={15} message="Không có phát sinh giao dịch nào trong khoảng thời gian này." />
                                    ) : (
                                        shiftData.items.map((row, idx) => (
                                            <tr key={row.variantId} className="hover:bg-slate-50/70">
                                                <td className="px-3 py-2.5 text-center text-slate-400">{idx + 1}</td>
                                                <td className="px-3 py-2.5 font-bold text-slate-700">{row.variantCode}</td>
                                                <td className="px-3 py-2.5 font-medium text-slate-900">{row.variantName}</td>
                                                <td className="px-3 py-2.5 text-center text-slate-600">{row.uomName}</td>
                                                <td className="px-3 py-2.5 text-center font-bold text-slate-700 bg-slate-50/30">{row.openingStock}</td>
                                                <td className="px-3 py-2.5 text-center text-emerald-600">{row.totalReceipt > 0 ? `+${row.totalReceipt}` : '-'}</td>
                                                <td className="px-3 py-2.5 text-center text-emerald-600">{row.totalTransferIn > 0 ? `+${row.totalTransferIn}` : '-'}</td>
                                                <td className="px-3 py-2.5 text-center text-emerald-600">{row.totalReturn > 0 ? `+${row.totalReturn}` : '-'}</td>
                                                <td className="px-3 py-2.5 text-center text-rose-600">{row.totalIssue > 0 ? `-${row.totalIssue}` : '-'}</td>
                                                <td className="px-3 py-2.5 text-center text-rose-600">{row.totalTransferOut > 0 ? `-${row.totalTransferOut}` : '-'}</td>
                                                <td className="px-3 py-2.5 text-center font-bold text-amber-700">
                                                    {row.totalAdjustment !== 0 ? (row.totalAdjustment > 0 ? `+${row.totalAdjustment}` : row.totalAdjustment) : '-'}
                                                </td>
                                                <td className="px-3 py-2.5 text-center font-black text-indigo-900 bg-indigo-50/30">{row.closingStock}</td>
                                                <td className="px-3 py-2.5 text-center font-bold text-emerald-700">{row.currentAvailable}</td>
                                                <td className="px-3 py-2.5 text-center font-bold text-indigo-700">{row.currentReserved}</td>
                                                <td className="px-3 py-2.5 text-center font-bold text-rose-700">{row.currentDamaged}</td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </ListCard>
                </div>
            )}

            {/* TAB 2: SỔ CÁI GIAO DỊCH AUDIT TRAIL */}
            {activeTab === 'ledger' && (
                <ListCard>
                    <div className="overflow-x-auto">
                        <table className="w-full text-left whitespace-nowrap text-sm">
                            <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                                <tr>
                                    <th className="px-4 py-3.5 text-center w-12">#</th>
                                    <th className="px-4 py-3.5">Thời Gian</th>
                                    <th className="px-4 py-3.5">Mã Giao Dịch</th>
                                    <th className="px-4 py-3.5">Loại Nghiệp Vụ</th>
                                    <th className="px-4 py-3.5">Mã Chứng Từ</th>
                                    <th className="px-4 py-3.5">Sản Phẩm</th>
                                    <th className="px-4 py-3.5">Lô Hàng</th>
                                    <th className="px-4 py-3.5 text-center">Biến Động</th>
                                    <th className="px-4 py-3.5">Người Thực Hiện</th>
                                    <th className="px-4 py-3.5">Ghi Chú</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-100">
                                {ledgerLoading ? (
                                    <TableLoading colSpan={10} message="Đang truy xuất sổ cái giao dịch..." />
                                ) : ledgerEntries.length === 0 ? (
                                    <TableEmpty colSpan={10} message="Chưa có giao dịch sổ cái nào." />
                                ) : (
                                    ledgerEntries.map((entry, idx) => {
                                        const isPositive = entry.quantity > 0;
                                        return (
                                            <tr key={entry.transactionCode || idx} className="hover:bg-slate-50/70">
                                                <td className="px-4 py-3 text-center text-slate-400 text-xs">
                                                    {(ledgerPage - 1) * ledgerPageSize + idx + 1}
                                                </td>
                                                <td className="px-4 py-3 text-slate-600 text-xs">
                                                    <DateTimeCell isoString={entry.transactionDate} />
                                                </td>
                                                <td className="px-4 py-3 font-mono font-bold text-xs text-slate-700">
                                                    {entry.transactionCode}
                                                </td>
                                                <td className="px-4 py-3">
                                                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold bg-slate-100 text-slate-700 border border-slate-200">
                                                        {entry.transactionType}
                                                    </span>
                                                </td>
                                                <td className="px-4 py-3 font-bold text-indigo-600 text-xs">
                                                    {entry.referenceCode || '---'}
                                                </td>
                                                <td className="px-4 py-3 font-medium text-slate-900">
                                                    {entry.variantName}
                                                </td>
                                                <td className="px-4 py-3 font-bold text-indigo-700 text-xs">
                                                    {entry.batchCode}
                                                </td>
                                                <td className="px-4 py-3 text-center font-black">
                                                    <span className={isPositive ? 'text-emerald-600' : 'text-rose-600'}>
                                                        {entry.quantity > 0 ? `+${entry.quantity}` : entry.quantity}
                                                    </span>
                                                </td>
                                                <td className="px-4 py-3 text-slate-600 text-xs">
                                                    {entry.performedBy}
                                                </td>
                                                <td className="px-4 py-3 text-xs italic text-slate-500 max-w-xs truncate" title={entry.note}>
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
                    />
                </ListCard>
            )}
        </ListPageContainer>
    );
};

export default InventoryReconciliation;
