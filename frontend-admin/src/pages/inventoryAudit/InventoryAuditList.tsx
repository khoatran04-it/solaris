import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClipboardList, Eye, Plus } from 'lucide-react';
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

import { inventoryAuditApi } from '../../api/inventoryAuditApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
    InventoryAudit,
    InventoryAuditStatus,
    InventoryAuditStatusLabels,
    InventoryAuditStatusColors,
    InventoryAuditType,
    InventoryAuditTypeLabels
} from '../../types/inventoryAudit';

const InventoryAuditList: React.FC = () => {
    const navigate = useNavigate();

    // Data States
    const [audits, setAudits] = useState<InventoryAudit[]>([]);
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
    const [typeFilter, setTypeFilter] = useState<(string | number)[]>([]);
    const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
    const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

    // Dropdowns
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

    const statusOptions = Object.keys(InventoryAuditStatusLabels).map(key => ({
        label: InventoryAuditStatusLabels[Number(key) as InventoryAuditStatus],
        value: Number(key)
    }));

    const typeOptions = Object.keys(InventoryAuditTypeLabels).map(key => ({
        label: InventoryAuditTypeLabels[Number(key) as InventoryAuditType],
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

    // Load filter options
    useEffect(() => {
        const loadOptions = async () => {
            try {
                const whRes = await warehouseApi.getAllList().catch(() => []);
                setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
            } catch (err) {
                console.error('Error loading warehouses:', err);
            }
        };
        loadOptions();
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
            const response = await inventoryAuditApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                auditType: typeFilter.length > 0 ? Number(typeFilter[0]) : undefined,
                startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined
            });

            setAudits(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            console.error('Error fetching inventory audits:', error);
            showToast('error', 'Không thể tải danh sách phiếu kiểm kê!');
            setAudits([]);
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, currentPage, warehouseFilter, statusFilter, typeFilter, fromDateFilter, toDateFilter]);

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
                title="Kiểm Kê Kho Hàng (Stocktake)"
                subtitle="Đối chiếu số liệu sổ sách với thực tế vật lý & Xử lý chênh lệch"
                icon={ClipboardList}
                searchPlaceholder="Tìm mã phiếu kiểm kê, nhân viên..."
                searchValue={searchTerm}
                onSearchChange={setSearchTerm}
                actionButton={{
                    label: "Tạo Đợt Kiểm Kê",
                    icon: Plus,
                    onClick: () => navigate('/inventory-audits/create')
                }}
            />

            <div className="flex flex-wrap items-center gap-3 mb-6">
                <CustomFilter
                    label="Kho kiểm kê"
                    options={warehouseOptions}
                    selectedValues={warehouseFilter}
                    onChange={(val) => { setWarehouseFilter(val); setCurrentPage(1); }}
                    isMulti={false}
                />
                <CustomFilter
                    label="Loại kiểm kê"
                    options={typeOptions}
                    selectedValues={typeFilter}
                    onChange={(val) => { setTypeFilter(val); setCurrentPage(1); }}
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
                                <th className="px-4 py-3.5">Mã Kiểm Kê</th>
                                <th className="px-4 py-3.5">Kho Hàng</th>
                                <th className="px-4 py-3.5">Hình Thức</th>
                                <th className="px-4 py-3.5">Người Kiểm Đếm</th>
                                <th className="px-4 py-3.5">Ngày Bắt Đầu</th>
                                <th className="px-4 py-3.5 text-center">Trạng Thái</th>
                                <th className="px-4 py-3.5 text-right">Lệch SL</th>
                                <th className="px-4 py-3.5 text-right">Giá Trị Lệch</th>
                                <th className="px-4 py-3.5 text-center w-24">Thao Tác</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 text-sm">
                            {loading ? (
                                <TableLoading colSpan={10} message="Đang tải danh sách phiếu kiểm kê..." />
                            ) : audits.length === 0 ? (
                                <TableEmpty colSpan={10} message="Chưa có đợt kiểm kê nào." />
                            ) : (
                                audits.map((audit, idx) => {
                                    const isNegative = audit.totalVarianceQty < 0;
                                    const isPositive = audit.totalVarianceQty > 0;
                                    return (
                                        <tr key={audit.id} className="hover:bg-slate-50/80 transition-colors">
                                            <td className="px-4 py-3.5 text-center text-slate-400 text-xs">
                                                {(currentPage - 1) * pageSize + idx + 1}
                                            </td>
                                            <td className="px-4 py-3.5 font-bold text-indigo-600">
                                                {audit.auditCode}
                                            </td>
                                            <td className="px-4 py-3.5 font-medium text-slate-800">
                                                {audit.warehouseName}
                                            </td>
                                            <td className="px-4 py-3.5 text-slate-600">
                                                {InventoryAuditTypeLabels[audit.auditType]}
                                            </td>
                                            <td className="px-4 py-3.5 text-slate-600">
                                                {audit.auditorName}
                                            </td>
                                            <td className="px-4 py-3.5 text-slate-600">
                                                <DateTimeCell isoString={audit.auditDate} />
                                            </td>
                                            <td className="px-4 py-3.5 text-center">
                                                <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold border ${InventoryAuditStatusColors[audit.status]}`}>
                                                    {InventoryAuditStatusLabels[audit.status]}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3.5 text-right font-bold">
                                                <span className={isNegative ? 'text-rose-600' : isPositive ? 'text-emerald-600' : 'text-slate-600'}>
                                                    {audit.totalVarianceQty > 0 ? `+${audit.totalVarianceQty}` : audit.totalVarianceQty}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3.5 text-right font-bold">
                                                <span className={isNegative ? 'text-rose-600' : isPositive ? 'text-emerald-600' : 'text-slate-600'}>
                                                    {formatCurrency(audit.totalVarianceAmount)}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3.5 text-center">
                                                <button
                                                    onClick={() => navigate(`/inventory-audits/${audit.id}`)}
                                                    className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={18} />
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })
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

export default InventoryAuditList;
