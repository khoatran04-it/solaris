import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Users, Shield, Mail, Phone, KeyRound, User as UserIcon } from 'lucide-react';

// API & Types
import { userApi } from '../../api/userApi';
import { roleApi } from '../../api/roleApi';
import { User } from '../../types/user';

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

const UserList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
    const [data, setData] = useState<User[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const pageSize = 10;

    // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [roleFilter, setRoleFilter] = useState<(string | number)[]>([]);
    
    // Lưu danh sách Role để làm Filter và Map Tên Vai Trò
    const [roleOptions, setRoleOptions] = useState<{ label: string, value: number }[]>([]);

    const statusOptions = [
        { label: 'Hoạt động', value: 1 },
        { label: 'Tạm khóa', value: 0 }
    ];

    // --- STATE MODAL & TOAST ---
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [deletingRecord, setDeletingRecord] = useState<User | null>(null);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error' | 'warning', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Tải danh sách Vai trò để làm bộ lọc
        roleApi.getAllList()
            .then(res => setRoleOptions(res.map(r => ({ label: r.name, value: r.id }))))
            .catch(() => showToast('warning', 'Không tải được bộ lọc Vai trò'));
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, statusFilter, roleFilter]);

    const fetchData = async () => {
        setIsLoading(true);
        try {
            const response = await userApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
                roleId: roleFilter.length > 0 ? Number(roleFilter[0]) : undefined, // Truyền 1 roleId hoặc xử lý mảng tùy Backend
            });
            
            setData(response.items);
            setTotalPages(response.totalPages);
        } catch (error) {
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU NHÂN VIÊN');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => { 
        fetchData(); 
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [currentPage, debouncedSearch, statusFilter, roleFilter]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const confirmDelete = async () => {
        if (!deletingRecord) return;
        try {
            await userApi.delete(deletingRecord.id);
            setIsModalOpen(false);
            fetchData();
            showToast('success', `Xóa thành công nhân viên "${deletingRecord.fullName}"`);
        } catch (error) {
            showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
        }
    };

    const handleToggleActive = async (id: number, currentStatus: boolean) => {
        try {
            await userApi.toggleActive(id);
            fetchData();
            showToast('success', `Đã ${currentStatus ? 'khóa' : 'kích hoạt'} tài khoản`);
        } catch (error) {
            showToast('error', 'Không thể thay đổi trạng thái');
        }
    };

    // Helper: Map Role Ids ra Tên Role
    const renderRoles = (roleIds: number[]) => {
        if (!roleIds || roleIds.length === 0) return <span className="text-slate-400 italic">Chưa cấp quyền</span>;
        
        const names = roleIds
            .map(id => roleOptions.find(r => r.value === id)?.label)
            .filter(Boolean);

        if (names.length === 0) return `${roleIds.length} Vai trò`;
        
        return (
            <div className="flex flex-wrap gap-1.5">
                {names.slice(0, 2).map((name, idx) => (
                    <span key={idx} className="px-2 py-0.5 bg-indigo-50 text-indigo-700 border border-indigo-200/60 rounded text-[11px] font-bold">
                        {name}
                    </span>
                ))}
                {names.length > 2 && (
                    <span className="px-2 py-0.5 bg-slate-100 text-slate-500 rounded text-[11px] font-bold">
                        +{names.length - 2}
                    </span>
                )}
            </div>
        );
    };

    return (    
        <ListPageContainer>
            <Toast {...toast} />
            
            <ListHeader 
                title="Danh Sách Nhân Sự"
                subtitle="Quản lý tài khoản đăng nhập, thông tin liên hệ và phân quyền người dùng"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/users/create')}
                icon={Users}
                searchPlaceholder="Tìm theo tên, sđt, CCCD, username..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">Nhân Viên</th>
                                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">Liên Hệ</th>
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="VAI TRÒ" options={roleOptions} selectedValues={roleFilter} onApply={setRoleFilter} />
                                </th>
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                <th className="w-[13%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Đăng Nhập Cuối</th>
                                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {isLoading ? (
                                <TableLoading colSpan={6} />
                            ) : data.length > 0 ? (
                                data.map((item) => (
                                    <tr key={item.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* CỘT 1: THÔNG TIN NHÂN VIÊN */}
                                        <td className="py-3 px-6">
                                            <div className="flex items-center gap-3">
                                                <div className="w-10 h-10 rounded-full border border-slate-200 flex items-center justify-center bg-slate-100 shrink-0 overflow-hidden">
                                                    {item.avatarUrl ? (
                                                        <img src={item.avatarUrl} alt={item.fullName} className="w-full h-full object-cover" />
                                                    ) : (
                                                        <UserIcon size={20} className="text-slate-400" />
                                                    )}
                                                </div>
                                                <div className="flex flex-col">
                                                    <span className="font-extrabold text-slate-800 text-[14px]">{item.fullName}</span>
                                                    <div className="flex items-center gap-2 mt-1">
                                                        <span className="text-[11px] font-bold text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
                                                            @{item.username}
                                                        </span>
                                                        <span className="text-[11px] text-slate-400">
                                                            CCCD: {item.citizenId}
                                                        </span>
                                                    </div>
                                                </div>
                                            </div>
                                        </td>

                                        {/* CỘT 2: LIÊN HỆ */}
                                        <td className="py-3 px-6">
                                            <div className="flex flex-col gap-1.5">
                                                <div className="flex items-center gap-2 text-[12px] text-slate-600">
                                                    <Phone size={13} className="text-slate-400 shrink-0" />
                                                    {item.phoneNumber || <span className="text-slate-300 italic">Trống</span>}
                                                </div>
                                                <div className="flex items-center gap-2 text-[12px] text-slate-600 truncate pr-2">
                                                    <Mail size={13} className="text-slate-400 shrink-0" />
                                                    {item.email || <span className="text-slate-300 italic">Trống</span>}
                                                </div>
                                            </div>
                                        </td>

                                        {/* CỘT 3: VAI TRÒ */}
                                        <td className="py-3 px-2">
                                            {renderRoles(item.roleIds)}
                                            
                                            {/* Hiển thị thêm badge nếu có quyền ngoại lệ */}
                                            {item.customPermissions && item.customPermissions.length > 0 && (
                                                <div className="mt-1.5 flex items-center gap-1 text-[10px] font-medium text-amber-600">
                                                    <Shield size={10} /> +{item.customPermissions.length} quyền riêng
                                                </div>
                                            )}
                                        </td>

                                        {/* CỘT 4: TRẠNG THÁI */}
                                        <td className="py-3 px-2 text-center">
                                            <button 
                                                onClick={() => handleToggleActive(item.id, item.isActive)}
                                                className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold border transition-colors ${
                                                    item.isActive 
                                                        ? 'bg-emerald-50 text-emerald-700 border-emerald-200/40 hover:bg-red-50 hover:text-red-600 hover:border-red-200' 
                                                        : 'bg-slate-100 text-slate-400 border-slate-200/50 hover:bg-emerald-50 hover:text-emerald-600 hover:border-emerald-200'
                                                }`}>
                                                <span className={`w-1.5 h-1.5 rounded-full mr-1.5 ${item.isActive ? 'bg-emerald-500' : 'bg-slate-300'}`}></span>
                                                {item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                                            </button>
                                        </td>

                                        {/* CỘT 5: LẦN CUỐI ĐĂNG NHẬP */}
                                        <td className="py-3 px-2 text-center">
                                            {item.lastLoginAt ? (
                                                <DateTimeCell isoString={item.lastLoginAt} />
                                            ) : (
                                                <span className="text-[12px] text-slate-400 italic">Chưa đăng nhập</span>
                                            )}
                                        </td>

                                        {/* CỘT 6: THAO TÁC */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button 
                                                    onClick={() => navigate(`/users/edit/${item.id}`)}
                                                    className="p-2 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                                                    title="Chỉnh sửa hồ sơ & Phân quyền"
                                                >
                                                    <Edit3 size={18} strokeWidth={2.5} />
                                                </button>
                                                <button 
                                                    onClick={() => { setDeletingRecord(item); setIsModalOpen(true); }}
                                                    className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                                                    title="Xóa nhân viên"
                                                >
                                                    <Trash2 size={18} strokeWidth={2.5} />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <TableEmpty colSpan={6} message="Không tìm thấy nhân viên nào phù hợp." />
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
                itemName={deletingRecord?.fullName || ''} 
                onClose={() => setIsModalOpen(false)} 
                onConfirm={confirmDelete} 
            />
        </ListPageContainer>
    );
};

export default UserList;