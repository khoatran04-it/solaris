import React, { useEffect, useState } from 'react';
import { Package, Edit3, Trash2, Building2, Layers, Clock } from 'lucide-react';

// API & Types
import { supplierProductApi } from '../../api/supplierProductApi';
import { supplierApi } from '../../api/supplierApi';
import { SupplierProduct, SupplierProductPayload } from '../../types/supplierProduct';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { ModalSupplierProduct } from '../../components/modals/ModalSupplierProduct';

// Atomic Components
import { 
    ListPageContainer, 
    ListHeader, 
    ListCard, 
    TableLoading, 
    TableEmpty, 
    ListPagination 
} from '../../components/commons/ListUI';

const SupplierProductList: React.FC = () => {
    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<SupplierProduct[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [totalItems, setTotalItems] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [supplierFilter, setSupplierFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);

    // --- OPTIONS CHO BỘ LỌC ---
    const [supplierOptions, setSupplierOptions] = useState<{ label: string; value: number }[]>([]);
    
    const statusOptions = [
        { label: 'Đang cung ứng', value: 1 },
        { label: 'Tạm ngưng', value: 0 }
    ];

    // --- STATE MODAL & TOAST ---
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingRecord, setEditingRecord] = useState<SupplierProduct | null>(null);

    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [deletingRecord, setDeletingRecord] = useState<SupplierProduct | null>(null);

    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error' | 'warning'; message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        supplierApi.getAllList()
            .then(res => {
                setSupplierOptions(res.map(s => ({ label: `${s.code} - ${s.name}`, value: s.id })));
            })
            .catch(() => showToast('warning', 'Không tải được danh mục Nhà cung cấp'));
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, supplierFilter, statusFilter]);

    const fetchData = async () => {
        setIsLoading(true);
        try {
            const response = await supplierProductApi.getAll({
                pageIndex: currentPage,
                pageSize: pageSize,
                search: debouncedSearch || undefined,
                supplierId: supplierFilter.length === 1 ? Number(supplierFilter[0]) : undefined,
                isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
            });
            
            setData(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI BẢNG GIÁ NCC');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => { 
        fetchData(); 
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentPage, debouncedSearch, supplierFilter, statusFilter]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleOpenCreate = () => {
        setEditingRecord(null);
        setIsModalOpen(true);
    };

    const handleOpenEdit = (item: SupplierProduct) => {
        setEditingRecord(item);
        setIsModalOpen(true);
    };

    const handleSave = async (payload: SupplierProductPayload) => {
        try {
            if (editingRecord) {
                await supplierProductApi.update(editingRecord.id, payload);
                showToast('success', 'Cập nhật bảng giá NCC thành công!');
            } else {
                await supplierProductApi.create(payload);
                showToast('success', 'Thêm sản phẩm vào danh mục NCC thành công!');
            }
            fetchData();
        } catch (error: any) {
            showToast('error', error?.response?.data?.message || 'Có lỗi xảy ra khi lưu dữ liệu');
            throw error;
        }
    };

    const confirmDelete = async () => {
        if (!deletingRecord) return;
        try {
            await supplierProductApi.delete(deletingRecord.id);
            setIsDeleteModalOpen(false);
            fetchData();
            showToast('success', `Đã xóa sản phẩm khỏi danh mục của NCC "${deletingRecord.supplierName}"`);
        } catch (error: any) {
            showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
        }
    };

    const handleToggleActive = async (id: number, currentStatus: boolean) => {
        try {
            await supplierProductApi.toggleActive(id);
            fetchData();
            showToast('success', `Đã ${currentStatus ? 'tạm ngưng' : 'kích hoạt'} cung ứng sản phẩm này`);
        } catch (error) {
            showToast('error', 'Không thể thay đổi trạng thái');
        }
    };

    const formatCurrency = (val: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
    };

    return (    
        <ListPageContainer>
            <Toast {...toast} />
            
            <ListHeader 
                title="Bảng Giá & Danh Mục NCC"
                subtitle="Quản lý các mặt hàng cung ứng, đơn giá nhập tham chiếu và MOQ từ nhà cung cấp"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={handleOpenCreate}
                icon={Package}
                searchPlaceholder="Tìm theo tên SP, mã SKU, tên NCC..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="NHÀ CUNG CẤP" options={supplierOptions} selectedValues={supplierFilter} onApply={setSupplierFilter} />
                                </th>
                                
                                <th className="w-[25%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    Sản Phẩm Cung Ứng
                                </th>

                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    Mã SKU NCC
                                </th>

                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                                    Đơn Giá Nhập
                                </th>

                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                                    MOQ & Giao
                                </th>

                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                                    Thao Tác
                                </th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={7} />
                            ) : data.length > 0 ? (
                                data.map((item) => {
                                    return (
                                        <tr key={item.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                            
                                            {/* CELL 1: NHÀ CUNG CẤP */}
                                            <td className="py-3 px-6">
                                                <div className="flex items-center gap-2">
                                                    <div className="w-8 h-8 rounded-lg bg-yellow-50 border border-yellow-100 flex items-center justify-center shrink-0">
                                                        <Building2 size={16} className="text-yellow-600" />
                                                    </div>
                                                    <div className="flex flex-col min-w-0">
                                                        <span className="font-bold text-slate-800 text-[13px] truncate" title={item.supplierName}>
                                                            {item.supplierName}
                                                        </span>
                                                        <span className="text-[10px] font-bold text-slate-400">
                                                            {item.supplierCode}
                                                        </span>
                                                    </div>
                                                </div>
                                            </td>

                                            {/* CELL 2: SẢN PHẨM */}
                                            <td className="py-3 px-4">
                                                <div className="flex flex-col">
                                                    <span className="font-bold text-slate-800 text-[13px] leading-tight" title={item.variantName}>
                                                        {item.variantName || `Biến thể #${item.variantId}`}
                                                    </span>
                                                    <div className="flex items-center gap-2 mt-1">
                                                        <span className="text-[10px] font-bold bg-blue-50 text-blue-700 px-1.5 py-0.5 rounded border border-blue-200/50 uppercase tracking-widest">
                                                            {item.variantCode || item.variantSKU || `#${item.variantId}`}
                                                        </span>
                                                        {item.purchaseUoMName && (
                                                            <span className="text-[10px] font-medium text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
                                                                ĐVT: {item.purchaseUoMName}
                                                            </span>
                                                        )}
                                                    </div>
                                                </div>
                                            </td>

                                            {/* CELL 3: MÃ SKU NCC */}
                                            <td className="py-3 px-2">
                                                {item.supplierSKU ? (
                                                    <span className="text-[12px] font-mono font-semibold text-slate-700 bg-slate-100 px-2 py-0.5 rounded">
                                                        {item.supplierSKU}
                                                    </span>
                                                ) : (
                                                    <span className="text-[12px] text-slate-400 italic">--</span>
                                                )}
                                            </td>

                                            {/* CELL 4: ĐƠN GIÁ NHẬP */}
                                            <td className="py-3 px-2 text-right">
                                                <span className="text-[14px] font-extrabold text-slate-900">
                                                    {formatCurrency(item.lastImportPrice)}
                                                </span>
                                            </td>

                                            {/* CELL 5: MOQ & LEAD TIME */}
                                            <td className="py-3 px-2 text-center">
                                                <div className="flex flex-col items-center gap-0.5">
                                                    <span className="text-[11px] font-bold text-slate-700">
                                                        Min: {item.minimumOrderQuantity} {item.purchaseUoMName || ''}
                                                    </span>
                                                    <span className="text-[10px] text-slate-400 flex items-center gap-1">
                                                        <Clock size={10} /> {item.leadTimeDays} ngày
                                                    </span>
                                                </div>
                                            </td>

                                            {/* CELL 6: TRẠNG THÁI */}
                                            <td className="py-3 px-2 text-center">
                                                <div className="flex justify-center">
                                                    <button 
                                                        onClick={() => handleToggleActive(item.id, item.isActive)}
                                                        className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold transition-all cursor-pointer ${
                                                            item.isActive 
                                                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100' 
                                                                : 'bg-slate-100 text-slate-600 border border-slate-200 hover:bg-slate-200'
                                                        }`}
                                                        title="Nhấn để đổi trạng thái"
                                                    >
                                                        <span className={`w-1.5 h-1.5 rounded-full ${item.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`}></span>
                                                        {item.isActive ? 'Đang cung ứng' : 'Tạm ngưng'}
                                                    </button>
                                                </div>
                                            </td>
                                            
                                            {/* CELL 7: THAO TÁC */}
                                            <td className="py-3 px-6">
                                                <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                    <button 
                                                        onClick={() => handleOpenEdit(item)}
                                                        className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors cursor-pointer"
                                                        title="Chỉnh sửa bảng giá"
                                                    >
                                                        <Edit3 size={17} strokeWidth={2.5} />
                                                    </button>
                                                    <button 
                                                        onClick={() => { setDeletingRecord(item); setIsDeleteModalOpen(true); }}
                                                        className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                                                        title="Xóa khỏi bảng giá"
                                                    >
                                                        <Trash2 size={17} strokeWidth={2.5} />
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })
                            ) : (
                                <TableEmpty colSpan={7} message="Chưa có cấu hình bảng giá nào hoặc không khớp với bộ lọc." />
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

            <ModalSupplierProduct
                isOpen={isModalOpen}
                initialData={editingRecord}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSave}
            />

            <ConfirmDeleteModal 
                isOpen={isDeleteModalOpen} 
                itemName={`${deletingRecord?.variantName || 'Sản phẩm'} của NCC ${deletingRecord?.supplierName || ''}`} 
                onClose={() => setIsDeleteModalOpen(false)} 
                onConfirm={confirmDelete} 
            />
        </ListPageContainer>
    );
};

export default SupplierProductList;
