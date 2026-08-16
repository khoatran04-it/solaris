import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Eye, BookType } from 'lucide-react';

// API & Types (Giả định sếp đã tạo file api tương tự các module trước)
import { attributeDefinitionApi } from '../../api/attributeDefinitionApi';
import { AttributeDefinition } from '../../types/attributeDefinition';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';

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

const AttributeDefinitionList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<AttributeDefinition[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [dataTypeFilter, setDataTypeFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);

    // --- OPTIONS CHO BỘ LỌC ---
    const dataTypeOptions = [
        { label: 'Văn bản (Text)', value: 'TEXT' },
        { label: 'Số (Number)', value: 'NUMBER' },
        { label: 'Danh sách chọn (Options)', value: 'OPTIONS' }
    ];
    const statusOptions = [
        { label: 'Hoạt động', value: 1 },
        { label: 'Tạm khóa', value: 0 }
    ];

    // --- STATE MODAL & TOAST ---
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [deletingRecord, setDeletingRecord] = useState<AttributeDefinition | null>(null);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error' | 'warning', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECT 1: Debounce Search ---
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // --- EFFECT 2: Reset trang khi bộ lọc thay đổi ---
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, dataTypeFilter, statusFilter]);

    // --- EFFECT 3: Fetch Data ---
    const fetchData = async () => {
        setIsLoading(true);
        try {
            const response = await attributeDefinitionApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                // Do datatype filter lưu mảng, ta lấy phần tử đầu tiên nếu có chọn
                dataType: dataTypeFilter.length === 1 ? String(dataTypeFilter[0]) : undefined,
                isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
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
    }, [currentPage, debouncedSearch, dataTypeFilter, statusFilter]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const confirmDelete = async () => {
        if (!deletingRecord) return;
        try {
            await attributeDefinitionApi.delete(deletingRecord.id);
            setIsModalOpen(false);
            fetchData();
            showToast('success', `Xóa thành công thuộc tính "${deletingRecord.name}"`);
        } catch (error: any) {
            showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
        }
    };

    // Hàm phụ: Lấy màu Badge dựa theo kiểu dữ liệu
    const getDataTypeBadge = (type: string) => {
        switch (type.toUpperCase()) {
            case 'NUMBER': return <span className="inline-flex px-2 py-1 rounded bg-blue-50 text-blue-600 border border-blue-100 font-semibold text-[11px]">Số (Number)</span>;
            case 'OPTIONS': return <span className="inline-flex px-2 py-1 rounded bg-fuchsia-50 text-fuchsia-600 border border-fuchsia-100 font-semibold text-[11px]">Chọn sẵn (Options)</span>;
            case 'TEXT': 
            default: return <span className="inline-flex px-2 py-1 rounded bg-slate-100 text-slate-600 border border-slate-200 font-semibold text-[11px]">Văn bản (Text)</span>;
        }
    };

    return (    
        <ListPageContainer>
            <Toast {...toast} />
            
            <ListHeader 
                title="Từ Điển Thuộc Tính (Attribute)"
                subtitle="Quản lý các chuẩn mực của thực phẩm: Quy cách đóng gói, Nguồn gốc, Độ tươi, Size..."
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/attributes/create')}
                icon={BookType} 
                searchPlaceholder="Tìm theo tên thuộc tính..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Tên Thuộc Tính</th>
                                
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KIỂU DỮ LIỆU" options={dataTypeOptions} selectedValues={dataTypeFilter} onApply={setDataTypeFilter} />
                                </th>

                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">NGÀY TẠO</th>
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">CẬP NHẬT</th>
                                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">THAO TÁC</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={6} />
                            ) : data.length > 0 ? (
                                data.map((item) => (
                                    <tr key={item.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* TÊN THUỘC TÍNH */}
                                        <td className="py-3 px-6">
                                            <div className="font-extrabold text-slate-800 text-[14px] leading-tight">
                                                {item.name}
                                            </div>
                                            <div className="text-[11px] text-slate-400 mt-0.5">
                                                ID: {item.id}
                                            </div>
                                        </td>

                                        {/* KIỂU DỮ LIỆU */}
                                        <td className="py-3 px-2">
                                            {getDataTypeBadge(item.dataType)}
                                        </td>

                                        {/* STATUS */}
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

                                        {/* DATES */}
                                        <td className="py-3 px-2">
                                            <DateTimeCell isoString={item.createdAt} />
                                        </td>
                                        <td className="py-3 px-2">
                                            <DateTimeCell isoString={item.updatedAt} />
                                        </td>

                                        {/* ACTIONS */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button 
                                                    onClick={() => navigate(`/attributes/${item.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
                                                <button 
                                                    onClick={() => navigate(`/attributes/edit/${item.id}`)}
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
                                <TableEmpty colSpan={6} message="Chưa có thuộc tính nào. Hãy tạo mới!" />
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

export default AttributeDefinitionList;