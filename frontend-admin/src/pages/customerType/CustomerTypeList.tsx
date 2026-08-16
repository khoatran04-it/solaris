import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Hexagon, Edit3, Trash2 } from 'lucide-react';

// API & Types
import { customerTypeApi } from '../../api/customerTypeApi';
import { CustomerType } from '../../types/customerType';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';

// Atomic Components
import {
    ListPageContainer, ListCard, ListHeader,
    TableLoading, TableEmpty, ListPagination, DateTimeCell
} from '../../components/commons/ListUI';

const CustomerTypeList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<CustomerType[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
    const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

    // --- STATE MODAL & TOAST ---
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [deletingRecord, setDeletingRecord] = useState<CustomerType | null>(null);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error' | 'warning', message: string }>({
        show: false, type: 'success', message: ''
    });

    // Debounce Search (chờ 500ms)
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    // Reset trang về 1 khi điều kiện lọc thay đổi
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, createdAtFilter, updatedAtFilter]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // Fetch Data từ API
    const fetchData = async () => {
        setIsLoading(true);
        try {
            const res = await customerTypeApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                createdAt: createdAtFilter ? createdAtFilter.toLocaleDateString('en-CA') : undefined,
                updatedAt: updatedAtFilter ? updatedAtFilter.toLocaleDateString('en-CA') : undefined
            });

            setData(res.items);
            setTotalPages(res.totalPages);
        } catch (error) {
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentPage, debouncedSearch, createdAtFilter, updatedAtFilter]);

    const confirmDelete = async () => {
        if (!deletingRecord) return;
        try {
            await customerTypeApi.delete(deletingRecord.id);
            setIsModalOpen(false);
            fetchData();
            showToast('success', `Xóa thành công phân loại "${deletingRecord.name}"`);
        } catch (error) {
            showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
        }
    };

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                icon={Hexagon}
                title="Phân Loại Khách Hàng"
                subtitle="Quản lý các nhóm khách hàng theo loại hình kinh doanh"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/customer-types/create')}
                searchPlaceholder="Tìm theo mã, tên, ..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã</th>
                                <th className="w-[35%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Tên Phân Loại</th>
                                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter
                                            title="NGÀY TẠO"
                                            selectedDate={createdAtFilter}
                                            onApply={setCreatedAtFilter}
                                        />
                                    </div>
                                </th>
                                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter
                                            title="CẬP NHẬT"
                                            selectedDate={updatedAtFilter}
                                            onApply={setUpdatedAtFilter}
                                        />
                                    </div>
                                </th>
                                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>

                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={5} />
                            ) : data.length > 0 ? (
                                data.map((item) => (
                                    <tr key={item.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        <td className="py-4 px-6">
                                            <span className="inline-flex items-center px-2.5 py-1 rounded-md bg-yellow-50 text-yellow-700 text-sm font-bold border border-yellow-200/50">
                                                {item.code}
                                            </span>
                                        </td>
                                        <td className="py-4 px-6 font-semibold text-slate-800">
                                            {item.name}
                                        </td>
                                        <td className="py-4 px-6 text-center">
                                            <DateTimeCell isoString={item.createdAt} />
                                        </td>
                                        <td className="py-4 px-6 text-center">
                                            <DateTimeCell isoString={item.updatedAt} />
                                        </td>
                                        <td className="py-4 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/customer-types/edit/${item.id}`)}
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
                                <TableEmpty colSpan={5} message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc." />
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

export default CustomerTypeList;