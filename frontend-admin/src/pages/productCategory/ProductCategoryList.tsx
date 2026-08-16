import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Eye, FolderTree, Image as ImageIcon } from 'lucide-react';

// API & Types
import { productCategoryApi } from '../../api/productCategoryApi';
import { productCategoryGroupApi } from '../../api/productCategoryGroupApi'; // Import thêm API này để lấy List Filter
import { ProductCategory } from '../../types/productCategory';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';

// Atomic Components
import { 
    ListPageContainer, 
    ListHeader, 
    ListCard, 
    TableLoading, 
    TableEmpty, 
    ListPagination, 
    DateTimeCell 
} from '../../components/commons/ListUI';

const ProductCategoryList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<ProductCategory[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [groupFilter, setGroupFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
    const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

    // --- OPTIONS CHO BỘ LỌC ---
    const [groupOptions, setGroupOptions] = useState<{ label: string, value: number }[]>([]);
    const statusOptions = [
        { label: 'Hoạt động', value: 1 },
        { label: 'Tạm khóa', value: 0 }
    ];

    // --- STATE MODAL & TOAST ---
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [deletingRecord, setDeletingRecord] = useState<ProductCategory | null>(null);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error' | 'warning', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECT 1: Tải danh sách Nhóm danh mục cho Bộ lọc ---
    useEffect(() => {
        productCategoryGroupApi.getAllList()
            .then((groups) => {
                setGroupOptions(groups.map(g => ({ label: g.name, value: g.id })));
            })
            .catch(() => showToast('warning', 'Không tải được bộ lọc Nhóm danh mục'));
    }, []);

    // --- EFFECT 2: Debounce Search ---
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // --- EFFECT 3: Reset trang khi bộ lọc thay đổi ---
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, groupFilter, statusFilter, createdAtFilter, updatedAtFilter]);

    // --- EFFECT 4: Fetch Data ---
    const fetchData = async () => {
        setIsLoading(true);
        try {
            const response = await productCategoryApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                categoryGroupId: groupFilter.length > 0 ? groupFilter.join(',') : undefined,
                isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
                createdAt: createdAtFilter ? createdAtFilter.toLocaleDateString('en-CA') : undefined,
                updatedAt: updatedAtFilter ? updatedAtFilter.toLocaleDateString('en-CA') : undefined
            });
            
            setData(response.items);
            setTotalPages(response.totalPages);
        } catch (error) {
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => { 
        fetchData(); 
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentPage, debouncedSearch, groupFilter, statusFilter, createdAtFilter, updatedAtFilter]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const confirmDelete = async () => {
        if (!deletingRecord) return;
        try {
            await productCategoryApi.delete(deletingRecord.id);
            setIsModalOpen(false);
            fetchData();
            showToast('success', `Xóa thành công danh mục "${deletingRecord.name}"`);
        } catch (error: any) {
            showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
        }
    };

    return (    
        <ListPageContainer>
            <Toast {...toast} />
            
            <ListHeader 
                title="Danh Mục Sản Phẩm"
                subtitle="Quản lý chi tiết phân loại hàng hóa thuộc các Nhóm danh mục"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/product-categories/create')}
                icon={FolderTree} // Icon cây thư mục đặc trưng
                searchPlaceholder="Tìm theo tên, mã danh mục..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                {/* Cột Profile */}
                                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Thông Tin Danh Mục</th>
                                
                                {/* Cột Filter Nhóm Danh Mục */}
                                <th className="w-[18%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="NHÓM DANH MỤC" options={groupOptions} selectedValues={groupFilter} onApply={setGroupFilter} />
                                </th>

                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="NGÀY TẠO" selectedDate={createdAtFilter} onApply={setCreatedAtFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="CẬP NHẬT" selectedDate={updatedAtFilter} onApply={setUpdatedAtFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={6} /> // Chỉnh colSpan = 6
                            ) : data.length > 0 ? (
                                data.map((item) => (
                                    <tr key={item.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* CELL 1: THÔNG TIN DANH MỤC */}
                                        <td className="py-3 px-6">
                                            <div className="flex items-center gap-3.5">
                                                <div className="w-12 h-12 rounded-lg bg-white border border-slate-200 shadow-sm flex items-center justify-center shrink-0 overflow-hidden">
                                                    {item.imagePath ? (
                                                        <img src={item.imagePath} alt={item.name} className="w-full h-full object-cover" />
                                                    ) : (
                                                        <ImageIcon size={20} className="text-slate-300" />
                                                    )}
                                                </div>
                                                <div className="flex flex-col">
                                                    <span className="font-extrabold text-slate-800 text-[14px] truncate max-w-48 leading-tight">{item.name}</span>
                                                    <div className="flex items-center gap-2 mt-1.5">
                                                        <span className="text-[10px] font-bold bg-indigo-50 text-indigo-700 px-1.5 py-0.5 rounded border border-indigo-200/50 uppercase tracking-widest">{item.code}</span>
                                                        {item.description && (
                                                            <span className="text-[11px] font-medium text-slate-500 truncate max-w-32" title={item.description}>
                                                                {item.description}
                                                            </span>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        </td>

                                        {/* CELL 2: NHÓM DANH MỤC (BADGE) */}
                                        <td className="py-3 px-2">
                                            {item.categoryGroupName ? (
                                                <span className="inline-flex px-2.5 py-1 rounded-lg text-xs font-semibold bg-violet-50 text-violet-700 border border-violet-200/60 truncate max-w-full" title={item.categoryGroupName}>
                                                    {item.categoryGroupName}
                                                </span>
                                            ) : (
                                                <span className="text-[11px] text-slate-400 italic">Chưa phân nhóm</span>
                                            )}
                                        </td>

                                        {/* CELL 3: STATUS */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold border ${
                                                    item.isActive 
                                                    ? 'bg-emerald-50 text-emerald-700 border-emerald-200/40' 
                                                    : 'bg-slate-100 text-slate-400 border-slate-200/50'
                                                }`}>
                                                    <span className={`w-1.5 h-1.5 rounded-full mr-1.5 ${item.isActive ? 'bg-emerald-500' : 'bg-slate-300'}`}></span>
                                                    {item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                                                </span>
                                            </div>
                                        </td>

                                        {/* CELL 4 & 5: DATES */}
                                        <td className="py-3 px-2">
                                            <DateTimeCell isoString={item.createdAt} />
                                        </td>
                                        <td className="py-3 px-2">
                                            <DateTimeCell isoString={item.updatedAt} />
                                        </td>

                                        {/* CELL 6: ACTIONS */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button 
                                                    onClick={() => navigate(`/product-categories/${item.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
                                                <button 
                                                    onClick={() => navigate(`/product-categories/edit/${item.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                                                    title="Chỉnh sửa"
                                                >
                                                    <Edit3 size={17} strokeWidth={2.5} />
                                                </button>
                                                <button 
                                                    onClick={() => { setDeletingRecord(item); setIsModalOpen(true); }}
                                                    className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                                                    title="Xóa"
                                                >
                                                    <Trash2 size={17} strokeWidth={2.5} />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <TableEmpty colSpan={6} message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc." />
                            )}
                        </tbody>
                    </table>
                </div>

                <ListPagination 
                    currentPage={currentPage} 
                    totalPages={totalPages} 
                    totalItems={data.length} 
                    onPageChange={setCurrentPage}
                    isLoading={isLoading}
                />
            </ListCard>

            <ConfirmDeleteModal 
                isOpen={isModalOpen} 
                itemName={deletingRecord?.name || ''} 
                onClose={() => setIsModalOpen(false)} 
                onConfirm={confirmDelete} 
            />
        </ListPageContainer>
    );
};

export default ProductCategoryList;