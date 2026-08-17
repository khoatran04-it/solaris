import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { PackageCheck, Eye, Plus } from 'lucide-react';
import {
    ListPageContainer,
    ListHeader,
    ListCard,
    TableLoading,
    TableEmpty,
    ListPagination,
    DateTimeCell
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
    InventoryIssue,
    InventoryIssueStatus,
    InventoryIssueStatusLabels,
    InventoryIssueStatusColors
} from '../../types/inventoryIssue';

const InventoryIssueList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATES DỮ LIỆU ---
    const [issues, setIssues] = useState<InventoryIssue[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [currentPage, setCurrentPage] = useState<number>(1);
    const [totalPages, setTotalPages] = useState<number>(0);
    const [totalItems, setTotalItems] = useState<number>(0);
    const pageSize = 10;

    // --- BỘ LỌC (FILTERS) ---
    const [searchTerm, setSearchTerm] = useState<string>('');
    const [debouncedSearch, setDebouncedSearch] = useState<string>('');
    const [warehouseFilter, setWarehouseFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
    const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

    // --- OPTIONS CHO DROPDOWN ---
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

    const statusOptions = Object.keys(InventoryIssueStatusLabels).map(key => ({
        label: InventoryIssueStatusLabels[Number(key) as InventoryIssueStatus],
        value: Number(key)
    }));

    // --- TOAST ---
    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>({
        show: false,
        type: 'success',
        message: '',
    });

    const showToast = (type: 'success' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // --- EFFECTS ---
    useEffect(() => {
        const loadOptions = async () => {
            try {
                // Fail-safe chống sập chùm API
                const whRes = await warehouseApi.getAllList().catch(() => []);
                setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
            } catch (err) {
                console.error('Lỗi tải danh mục kho:', err);
            }
        };
        loadOptions();
    }, []);

    // Debounce search
    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(searchTerm);
        }, 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // Đưa trang về 1 mỗi khi đổi filter
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, warehouseFilter, statusFilter, fromDateFilter, toDateFilter]);

    // --- FETCH DATA ---
    const fetchData = useCallback(async () => {
        try {
            setLoading(true);
            const response = await inventoryIssueApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined
            });

            setIssues(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            console.error('Lỗi tải dữ liệu phiếu xuất kho:', error);
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DANH SÁCH!');
            setIssues([]);
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, currentPage, warehouseFilter, statusFilter, fromDateFilter, toDateFilter]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                title="Phiếu Xuất Kho"
                subtitle="Quản lý xuất hàng bán & đóng gói theo Lô hàng (FEFO)"
                icon={PackageCheck}
                searchPlaceholder="Tìm mã phiếu xuất, mã đơn..."
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/inventory-issues/create')}
            />

            <ListCard>
                {/* --- BẢNG DỮ LIỆU CHUẨN CONVENTION --- */}
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse min-w-275">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[4%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">#</th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã Phiếu</th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">Đơn Hàng Gốc</th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHO XUẤT" options={warehouseOptions} selectedValues={warehouseFilter} onApply={setWarehouseFilter} />
                                </th>
                                
                                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">Người Nhận / SĐT</th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="TỪ NGÀY" selectedDate={fromDateFilter} onApply={setFromDateFilter} />
                                    </div>
                                </th>

                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="ĐẾN NGÀY" selectedDate={toDateFilter} onApply={setToDateFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100 text-sm">
                            {loading ? (
                                <TableLoading colSpan={9} />
                            ) : issues.length === 0 ? (
                                <TableEmpty colSpan={9} message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc." />
                            ) : (
                                issues.map((issue, idx) => (
                                    <tr key={issue.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        {/* CELL 1: # */}
                                        <td className="px-4 py-3.5 text-center text-slate-400 text-xs">
                                            {(currentPage - 1) * pageSize + idx + 1}
                                        </td>
                                        
                                        {/* CELL 2: MÃ PHIẾU */}
                                        <td className="px-4 py-3.5 font-bold text-indigo-600">
                                            {issue.issueCode}
                                        </td>
                                        
                                        {/* CELL 3: ĐƠN HÀNG GỐC */}
                                        <td className="px-4 py-3.5 font-medium text-slate-800">
                                            {issue.orderCode ? (
                                                <button
                                                    onClick={() => navigate(`/orders/${issue.orderId}`)}
                                                    className="text-indigo-600 hover:underline font-bold"
                                                >
                                                    {issue.orderCode}
                                                </button>
                                            ) : (
                                                <span className="italic text-slate-400">Xuất nội bộ</span>
                                            )}
                                        </td>
                                        
                                        {/* CELL 4: KHO XUẤT */}
                                        <td className="px-4 py-3.5 font-medium text-slate-700">
                                            {issue.warehouseName}
                                        </td>
                                        
                                        {/* CELL 5: NGƯỜI NHẬN / SĐT */}
                                        <td className="px-4 py-3.5 text-slate-600">
                                            <div>{issue.receiverName || '---'}</div>
                                            <div className="text-xs text-slate-400">{issue.receiverPhone}</div>
                                        </td>
                                        
                                        {/* CELL 6 & 7: NGÀY XUẤT (Gộp Từ ngày - Đến ngày) */}
                                        <td className="py-3 px-2 text-center" colSpan={2}>
                                            <DateTimeCell isoString={issue.issueDate} />
                                        </td>
                                        
                                        {/* CELL 8: TRẠNG THÁI */}
                                        <td className="px-4 py-3.5 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-bold border shadow-sm ${InventoryIssueStatusColors[issue.status]}`}>
                                                    {InventoryIssueStatusLabels[issue.status]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 9: THAO TÁC */}
                                        <td className="px-4 py-3.5 text-center">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/inventory-issues/${issue.id}`)}
                                                    className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={18} strokeWidth={2.5} />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                <ListPagination
                    currentPage={currentPage}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    onPageChange={setCurrentPage}
                    isLoading={loading}
                />
            </ListCard>
        </ListPageContainer>
    );
};

export default InventoryIssueList;