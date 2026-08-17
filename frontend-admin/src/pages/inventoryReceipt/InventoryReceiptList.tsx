import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClipboardCheck, Eye, Trash2 } from 'lucide-react';

// API & Types
import { inventoryReceiptApi } from '../../api/inventoryReceiptApi';
import { warehouseApi } from '../../api/warehouseApi';
import { supplierApi } from '../../api/supplierApi';
import { 
    InventoryReceipt, 
    InventoryReceiptStatus, 
    InventoryReceiptStatusLabels, 
    InventoryReceiptStatusColors
} from '../../types/inventoryReceipt';

// Common UI
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

const InventoryReceiptList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<InventoryReceipt[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [totalItems, setTotalItems] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [warehouseFilter, setWarehouseFilter] = useState<(string | number)[]>([]);
    const [supplierFilter, setSupplierFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [startDateFilter, setStartDateFilter] = useState<Date | null>(null);
    const [endDateFilter, setEndDateFilter] = useState<Date | null>(null);

    // --- STATE DELETE MODAL ---
    const [deleteId, setDeleteId] = useState<number | null>(null);
    const [deleteCode, setDeleteCode] = useState('');
    const [isDeleting, setIsDeleting] = useState(false);

    // --- STATE TOAST ---
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error' | 'warning', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- OPTIONS CHO BỘ LỌC ---
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string, value: number }[]>([]);
    const [supplierOptions, setSupplierOptions] = useState<{ label: string, value: number }[]>([]);
    
    const statusOptions = Object.keys(InventoryReceiptStatusLabels).map(key => ({
        label: InventoryReceiptStatusLabels[Number(key) as InventoryReceiptStatus],
        value: Number(key)
    }));

    // --- EFFECTS ---
    useEffect(() => {
        Promise.all([
            warehouseApi.getAllList(),
            supplierApi.getAllList()
        ]).then(([warehouses, suppliers]) => {
            setWarehouseOptions(warehouses.map(wh => ({ label: wh.name, value: wh.id })));
            setSupplierOptions(suppliers.map(sup => ({ label: sup.name, value: sup.id })));
        }).catch(err => {
            console.error("Lỗi khi tải dữ liệu bộ lọc:", err);
            showToast('warning', 'Không tải được dữ liệu bộ lọc!');
        });
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // Reset về trang 1 khi thay đổi bất kỳ bộ lọc nào
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, warehouseFilter, supplierFilter, statusFilter, startDateFilter, endDateFilter]);

    const fetchData = useCallback(async () => {
        setIsLoading(true);
        try {
            const response = await inventoryReceiptApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                supplierId: supplierFilter.length > 0 ? Number(supplierFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                startDate: startDateFilter ? startDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: endDateFilter ? endDateFilter.toLocaleDateString('en-CA') : undefined,
            });
            
            setData(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            console.error("Lỗi khi tải danh sách phiếu nhập kho:", error);
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
        } finally {
            setIsLoading(false);
        }
    }, [currentPage, debouncedSearch, warehouseFilter, supplierFilter, statusFilter, startDateFilter, endDateFilter]);

    useEffect(() => { 
        fetchData(); 
    }, [fetchData]);

    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleDelete = async () => {
        if (!deleteId) return;
        try {
            setIsDeleting(true);
            await inventoryReceiptApi.delete(deleteId);
            showToast('success', 'XÓA PHIẾU NHẬP KHO CHỜ XỬ LÝ THÀNH CÔNG');
            setDeleteId(null);
            fetchData();
        } catch (error: any) {
            showToast('error', error.response?.data?.message || 'Không thể xóa phiếu nhập kho!');
        } finally {
            setIsDeleting(false);
        }
    };

    return (
        <ListPageContainer>
            <Toast {...toast} />
            
            <ListHeader 
                title="Phiếu Nhập Kho"
                subtitle="Quản lý chứng từ kiểm đếm và nhập kho vật lý"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/inventory-receipts/create')}
                icon={ClipboardCheck}
                searchPlaceholder="Tìm kiếm theo mã phiếu..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-[400px] pb-24">
                    <table className="w-full text-left border-collapse min-w-[950px]">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[14%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã Phiếu</th>
                                
                                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHO NHẬP" options={warehouseOptions} selectedValues={warehouseFilter} onApply={setWarehouseFilter} />
                                </th>
                                
                                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="NHÀ CUNG CẤP" options={supplierOptions} selectedValues={supplierFilter} onApply={setSupplierFilter} />
                                </th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="TỪ NGÀY" selectedDate={startDateFilter} onApply={setStartDateFilter} />
                                    </div>
                                </th>

                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="ĐẾN NGÀY" selectedDate={endDateFilter} onApply={setEndDateFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider hidden lg:table-cell">Người Kiểm</th>
                                
                                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={8} />
                            ) : data.length > 0 ? (
                                data.map((receipt) => (
                                    <tr key={receipt.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* CELL 1: MÃ PHIẾU */}
                                        <td className="py-3 px-6">
                                            <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-slate-100 text-slate-700 border border-slate-200 shadow-sm">
                                                {receipt.receiptCode}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 2: KHO NHẬP */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-bold text-slate-800">{receipt.warehouseName}</div>
                                        </td>
                                        
                                        {/* CELL 3: NHÀ CUNG CẤP */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] text-slate-600 truncate max-w-[180px]" title={receipt.supplierName}>
                                                {receipt.supplierName || <span className="italic text-slate-400">Không có</span>}
                                            </div>
                                        </td>
                                        
                                        {/* CELL 4 & 5: NGÀY NHẬP */}
                                        <td className="py-3 px-2 text-center" colSpan={2}>
                                            <DateCell isoString={receipt.receiptDate} />
                                        </td>
                                        
                                        {/* CELL 6: TRẠNG THÁI */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${InventoryReceiptStatusColors[receipt.status]}`}>
                                                    {InventoryReceiptStatusLabels[receipt.status]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 7: NGƯỜI KIỂM */}
                                        <td className="py-3 px-2 hidden lg:table-cell">
                                            <div className="text-[12px] text-slate-600 font-medium bg-slate-50 px-2 py-1 rounded-md border border-slate-100 inline-block">
                                                {receipt.receivedByName || '-'}
                                            </div>
                                        </td>
                                        
                                        {/* CELL 8: THAO TÁC */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/inventory-receipts/${receipt.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
                                                {receipt.status === InventoryReceiptStatus.Pending && (
                                                    <button
                                                        onClick={() => {
                                                            setDeleteId(receipt.id);
                                                            setDeleteCode(receipt.receiptCode);
                                                        }}
                                                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                                                        title="Xóa phiếu chờ"
                                                    >
                                                        <Trash2 size={17} strokeWidth={2.5} />
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <TableEmpty 
                                    colSpan={8} 
                                    message="Không tìm thấy phiếu nhập kho nào phù hợp." 
                                />
                            )}
                        </tbody>
                    </table>
                </div>

                <ListPagination 
                    currentPage={currentPage}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    onPageChange={setCurrentPage}
                    isLoading={isLoading}
                />
            </ListCard>

            <ConfirmDeleteModal
                isOpen={Boolean(deleteId)}
                onClose={() => setDeleteId(null)}
                onConfirm={handleDelete}
                loading={isDeleting}
                title="Xóa Phiếu Nhập Kho Chờ Xử Lý"
                message={`Bạn có chắc chắn muốn xóa phiếu nhập kho "${deleteCode}" không? Thao tác này không thể hoàn tác.`}
            />
        </ListPageContainer>
    );
};

export default InventoryReceiptList;