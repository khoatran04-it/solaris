import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlidersHorizontal, Eye, Plus } from 'lucide-react';
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

import { inventoryAdjustmentApi } from '../../api/inventoryAdjustmentApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
    InventoryAdjustment,
    InventoryAdjustmentStatus,
    InventoryAdjustmentStatusLabels,
    InventoryAdjustmentStatusColors,
    InventoryAdjustmentReason,
    InventoryAdjustmentReasonLabels
} from '../../types/inventoryAdjustment';

const InventoryAdjustmentList: React.FC = () => {
    const navigate = useNavigate();

    // Data States
    const [adjustments, setAdjustments] = useState<InventoryAdjustment[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [currentPage, setCurrentPage] = useState<number>(1);
    const [totalPages, setTotalPages] = useState<number>(0);
    const [totalItems, setTotalItems] = useState<number>(0);
    const pageSize = 10;

    // Filters
    const [searchTerm, setSearchTerm] = useState<string>('');
    const [debouncedSearch, setDebouncedSearch] = useState<string>('');
    const [warehouseFilter, setWarehouseFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [reasonFilter, setReasonFilter] = useState<(string | number)[]>([]);
    const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
    const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

    // Dropdowns
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

    const statusOptions = Object.keys(InventoryAdjustmentStatusLabels).map(key => ({
        label: InventoryAdjustmentStatusLabels[Number(key) as InventoryAdjustmentStatus],
        value: Number(key)
    }));

    const reasonOptions = Object.keys(InventoryAdjustmentReasonLabels).map(key => ({
        label: InventoryAdjustmentReasonLabels[Number(key) as InventoryAdjustmentReason],
        value: Number(key)
    }));

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

    // Load warehouse options
    useEffect(() => {
        const loadWarehouses = async () => {
            try {
                const whRes = await warehouseApi.getAllList().catch(() => []);
                setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
            } catch (err) {
                console.error('Error loading warehouses:', err);
            }
        };
        loadWarehouses();
    }, []);

    // Debounce search
    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(searchTerm);
            setCurrentPage(1);
        }, 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // Fetch data
    const fetchData = useCallback(async () => {
        try {
            setLoading(true);
            const response = await inventoryAdjustmentApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                reason: reasonFilter.length > 0 ? Number(reasonFilter[0]) : undefined,
                startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined
            });

            setAdjustments(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            console.error('Error fetching adjustments:', error);
            showToast('error', 'Không thể tải danh sách phiếu điều chỉnh!');
            setAdjustments([]);
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, currentPage, warehouseFilter, statusFilter, reasonFilter, fromDateFilter, toDateFilter]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    const formatCurrency = (amount: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount || 0);
    };

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                title="Điều Chỉnh & Xuất Hủy Tồn Kho"
                subtitle="Xử lý hao hụt, hư hỏng, mất cắp & Cân bằng số liệu sau kiểm kê"
                icon={SlidersHorizontal}
                searchPlaceholder="Tìm mã phiếu điều chỉnh, mã kiểm kê..."
                searchValue={searchTerm}
                onSearchChange={setSearchTerm}
                actionButton={{
                    label: "Tạo Phiếu Điều Chỉnh",
                    icon: Plus,
                    onClick: () => navigate('/inventory-adjustments/create')
                }}
            />

            <div className="flex flex-wrap items-center gap-3 mb-6">
                <CustomFilter
                    label="Kho hàng"
                    options={warehouseOptions}
                    selectedValues={warehouseFilter}
                    onChange={(val) => { setWarehouseFilter(val); setCurrentPage(1); }}
                    isMulti={false}
                />
                <CustomFilter
                    label="Lý do điều chỉnh"
                    options={reasonOptions}
                    selectedValues={reasonFilter}
                    onChange={(val) => { setReasonFilter(val); setCurrentPage(1); }}
                    isMulti={false}
                />
                <CustomFilter
                    label="Trạng thái"
                    options={statusOptions}
                    selectedValues={statusFilter}
                    onChange={(val) => { setStatusFilter(val); setCurrentPage(1); }}
                    isMulti={false}
                />
                <CustomDateFilter
                    label="Từ ngày"
                    selectedDate={fromDateFilter}
                    onChange={(date) => { setFromDateFilter(date); setCurrentPage(1); }}
                />
                <CustomDateFilter
                    label="Đến ngày"
                    selectedDate={toDateFilter}
                    onChange={(date) => { setToDateFilter(date); setCurrentPage(1); }}
                />
            </div>

            <ListCard>
                <div className="overflow-x-auto">
                    <table className="w-full text-left whitespace-nowrap">
                        <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                            <tr>
                                <th className="px-4 py-3.5 text-center w-12">#</th>
                                <th className="px-4 py-3.5">Mã Điều Chỉnh</th>
                                <th className="px-4 py-3.5">Kho Hàng</th>
                                <th className="px-4 py-3.5">Đợt Kiểm Kê Gốc</th>
                                <th className="px-4 py-3.5">Lý Do Điều Chỉnh</th>
                                <th className="px-4 py-3.5">Người Lập</th>
                                <th className="px-4 py-3.5">Ngày Lập</th>
                                <th className="px-4 py-3.5 text-center">Trạng Thái</th>
                                <th className="px-4 py-3.5 text-right">Tổng Giá Trị Lệch</th>
                                <th className="px-4 py-3.5 text-center w-24">Thao Tác</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 text-sm">
                            {loading ? (
                                <TableLoading colSpan={10} message="Đang tải danh sách phiếu điều chỉnh..." />
                            ) : adjustments.length === 0 ? (
                                <TableEmpty colSpan={10} message="Chưa có phiếu điều chỉnh nào." />
                            ) : (
                                adjustments.map((adj, idx) => (
                                    <tr key={adj.id} className="hover:bg-slate-50/80 transition-colors">
                                        <td className="px-4 py-3.5 text-center text-slate-400 text-xs">
                                            {(currentPage - 1) * pageSize + idx + 1}
                                        </td>
                                        <td className="px-4 py-3.5 font-bold text-indigo-600">
                                            {adj.adjustmentCode}
                                        </td>
                                        <td className="px-4 py-3.5 font-medium text-slate-800">
                                            {adj.warehouseName}
                                        </td>
                                        <td className="px-4 py-3.5 text-slate-600">
                                            {adj.auditCode ? (
                                                <button
                                                    onClick={() => navigate(`/inventory-audits/${adj.auditId}`)}
                                                    className="text-indigo-600 hover:underline font-bold"
                                                >
                                                    {adj.auditCode}
                                                </button>
                                            ) : (
                                                <span className="italic text-slate-400">Điều chỉnh thủ công</span>
                                            )}
                                        </td>
                                        <td className="px-4 py-3.5 font-medium text-slate-700">
                                            {InventoryAdjustmentReasonLabels[adj.reason]}
                                        </td>
                                        <td className="px-4 py-3.5 text-slate-600">
                                            {adj.createdByName}
                                        </td>
                                        <td className="px-4 py-3.5 text-slate-600">
                                            <DateTimeCell isoString={adj.adjustmentDate} />
                                        </td>
                                        <td className="px-4 py-3.5 text-center">
                                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold border ${InventoryAdjustmentStatusColors[adj.status]}`}>
                                                {InventoryAdjustmentStatusLabels[adj.status]}
                                            </span>
                                        </td>
                                        <td className="px-4 py-3.5 text-right font-bold text-slate-800">
                                            {formatCurrency(adj.totalVarianceAmount)}
                                        </td>
                                        <td className="px-4 py-3.5 text-center">
                                            <button
                                                onClick={() => navigate(`/inventory-adjustments/${adj.id}`)}
                                                className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                                                title="Xem chi tiết"
                                            >
                                                <Eye size={18} />
                                            </button>
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
                />
            </ListCard>
        </ListPageContainer>
    );
};

export default InventoryAdjustmentList;
