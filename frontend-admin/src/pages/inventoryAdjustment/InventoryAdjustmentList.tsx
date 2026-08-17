import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlidersHorizontal, Eye, Trash2 } from 'lucide-react';
import {
    ListPageContainer,
    ListHeader,
    ListCard,
    TableLoading,
    TableEmpty,
    ListPagination,
    DateCell,
    DateTimeCell
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';

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

    // --- STATES ---
    const [adjustments, setAdjustments] = useState<InventoryAdjustment[]>([]);
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
    const [reasonFilter, setReasonFilter] = useState<(string | number)[]>([]);
    const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
    const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

    // --- DELETE MODAL ---
    const [deleteId, setDeleteId] = useState<number | null>(null);
    const [deleteCode, setDeleteCode] = useState<string>('');
    const [isDeleting, setIsDeleting] = useState<boolean>(false);

    // --- OPTIONS ---
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

    const statusOptions = Object.keys(InventoryAdjustmentStatusLabels).map(key => ({
        label: InventoryAdjustmentStatusLabels[Number(key) as InventoryAdjustmentStatus],
        value: Number(key)
    }));

    const reasonOptions = Object.keys(InventoryAdjustmentReasonLabels).map(key => ({
        label: InventoryAdjustmentReasonLabels[Number(key) as InventoryAdjustmentReason],
        value: Number(key)
    }));

    // --- TOAST ---
    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error' | 'warning'; message: string }>({
        show: false,
        type: 'success',
        message: '',
    });

    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // --- EFFECTS ---
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
        }, 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // Reset về trang 1 khi đổi filter
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, warehouseFilter, statusFilter, reasonFilter, fromDateFilter, toDateFilter]);

    // --- FETCH DATA ---
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
            showToast('error', 'CÓ LỖI KHI TẢI DANH SÁCH PHIẾU ĐIỀU CHỈNH!');
            setAdjustments([]);
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, currentPage, warehouseFilter, statusFilter, reasonFilter, fromDateFilter, toDateFilter]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    const handleDelete = async () => {
        if (!deleteId) return;
        try {
            setIsDeleting(true);
            await inventoryAdjustmentApi.delete(deleteId);
            showToast('success', 'XÓA PHIẾU ĐIỀU CHỈNH NHÁP THÀNH CÔNG');
            setDeleteId(null);
            fetchData();
        } catch (err: any) {
            showToast('error', err.response?.data?.message || 'Không thể xóa phiếu điều chỉnh!');
        } finally {
            setIsDeleting(false);
        }
    };

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
                searchPlaceholder="Tìm mã phiếu điều chỉnh, mã kiểm kê gốc..."
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/inventory-adjustments/create')}
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse min-w-250">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[14%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã Điều Chỉnh</th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHO HÀNG" options={warehouseOptions} selectedValues={warehouseFilter} onApply={setWarehouseFilter} />
                                </th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="LÝ DO ĐIỀU CHỈNH" options={reasonOptions} selectedValues={reasonFilter} onApply={setReasonFilter} />
                                </th>
                                
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
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Tổng Giá Trị</th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider hidden lg:table-cell text-center">Người Lập</th>
                                
                                <th className="w-[6%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100 text-sm">
                            {loading ? (
                                <TableLoading colSpan={9}/>
                            ) : adjustments.length === 0 ? (
                                <TableEmpty colSpan={9} message="Không có phiếu điều chỉnh nào phù hợp điều kiện tìm kiếm." />
                            ) : (
                                adjustments.map((adj) => (
                                    <tr key={adj.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* CELL 1: MÃ ĐIỀU CHỈNH */}
                                        <td className="py-3 px-6">
                                            <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-sm">
                                                {adj.adjustmentCode}
                                            </span>
                                            {adj.auditCode && (
                                                <div className="mt-1 text-[11px] text-slate-500">
                                                    Từ kiểm kê: <button onClick={() => navigate(`/inventory-audits/${adj.auditId}`)} className="text-indigo-600 hover:underline font-bold">{adj.auditCode}</button>
                                                </div>
                                            )}
                                        </td>
                                        
                                        {/* CELL 2: KHO HÀNG */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-bold text-slate-800">{adj.warehouseName}</div>
                                        </td>
                                        
                                        {/* CELL 3: LÝ DO */}
                                        <td className="py-3 px-2">
                                            <span className="text-[12px] font-medium text-slate-700 bg-slate-100 px-2 py-1 rounded-md border border-slate-200">
                                                {InventoryAdjustmentReasonLabels[adj.reason]}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 4 & 5: NGÀY LẬP */}
                                        <td className="py-3 px-2 text-center" colSpan={2}>
                                            <DateCell isoString={adj.adjustmentDate} />
                                        </td>
                                        
                                        {/* CELL 6: TRẠNG THÁI */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${InventoryAdjustmentStatusColors[adj.status]}`}>
                                                    {InventoryAdjustmentStatusLabels[adj.status]}
                                                </span>
                                            </div>
                                        </td>

                                        {/* CELL 7: TỔNG GIÁ TRỊ LỆCH */}
                                        <td className="py-3 px-2 text-right">
                                            <span className="text-[13px] font-black text-slate-800">
                                                {formatCurrency(adj.totalVarianceAmount)}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 8: NGƯỜI LẬP */}
                                        <td className="py-3 px-2 text-center hidden lg:table-cell">
                                            <div className="text-[12px] text-slate-600 font-medium truncate max-w-30" title={adj.createdByName}>
                                                {adj.createdByName}
                                            </div>
                                        </td>
                                        
                                        {/* CELL 9: THAO TÁC */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/inventory-adjustments/${adj.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
                                                {adj.status === InventoryAdjustmentStatus.Draft && (
                                                    <button
                                                        onClick={() => {
                                                            setDeleteId(adj.id);
                                                            setDeleteCode(adj.adjustmentCode);
                                                        }}
                                                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                                                        title="Xóa phiếu nháp"
                                                    >
                                                        <Trash2 size={17} strokeWidth={2.5} />
                                                    </button>
                                                )}
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

            <ConfirmDeleteModal
                isOpen={Boolean(deleteId)}
                onClose={() => setDeleteId(null)}
                onConfirm={handleDelete}
                loading={isDeleting}
                title="Xóa Phiếu Điều Chỉnh Nháp"
                message={`Bạn có chắc chắn muốn xóa phiếu điều chỉnh "${deleteCode}" không? Thao tác này không thể hoàn tác.`}
            />
        </ListPageContainer>
    );
};

export default InventoryAdjustmentList;