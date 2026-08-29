import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Users, Edit3, Trash2, Eye, User } from 'lucide-react';

// API & Types
import { customerApi } from '../../api/customerApi';
import { customerTypeApi } from '../../api/customerTypeApi';
import { customerTierApi } from '../../api/customerTierApi';
import { customerGroupApi } from '../../api/customerGroupApi';
import { Customer } from '../../types/customer';

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
  DateTimeCell,
} from '../../components/commons/ListUI';

const CustomerList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<Customer[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [typeFilter, setTypeFilter] = useState<(string | number)[]>([]);
  const [tierFilter, setTierFilter] = useState<(string | number)[]>([]);
  const [groupFilter, setGroupFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const [typeOptions, setTypeOptions] = useState<{ label: string; value: number }[]>([]);
  const [tierOptions, setTierOptions] = useState<{ label: string; value: number }[]>([]);
  const [groupOptions, setGroupOptions] = useState<{ label: string; value: number }[]>([]);
  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<Customer | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Tải các tuỳ chọn cho Bộ lọc (Dropdown) ---
  useEffect(() => {
    Promise.all([
      customerTypeApi.getAllList(),
      customerTierApi.getAllList(),
      customerGroupApi.getAllList(false),
    ])
      .then(([types, tiers, groups]) => {
        setTypeOptions(types.map((t) => ({ label: t.name, value: t.id })));
        setTierOptions(tiers.map((t) => ({ label: t.name, value: t.id })));
        setGroupOptions(groups.map((g) => ({ label: g.name, value: g.id })));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách bộ lọc'));
  }, []);

  // --- EFFECT 2: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT 3: Reset trang khi bộ lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [
    debouncedSearch,
    typeFilter,
    tierFilter,
    groupFilter,
    statusFilter,
    createdAtFilter,
    updatedAtFilter,
  ]);

  // --- EFFECT 4: Fetch Data ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await customerApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        customerTypeId: typeFilter.length > 0 ? typeFilter.join(',') : undefined,
        customerTierId: tierFilter.length > 0 ? tierFilter.join(',') : undefined,
        customerGroupId: groupFilter.length > 0 ? groupFilter.join(',') : undefined,
        isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
        createdAt: createdAtFilter ? createdAtFilter.toLocaleDateString('en-CA') : undefined,
        updatedAt: updatedAtFilter ? updatedAtFilter.toLocaleDateString('en-CA') : undefined,
      });

      setData(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    currentPage,
    debouncedSearch,
    typeFilter,
    tierFilter,
    groupFilter,
    statusFilter,
    createdAtFilter,
    updatedAtFilter,
  ]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await customerApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Đã xóa khách hàng "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await customerApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} tài khoản khách hàng`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái hoạt động');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Danh Sách Khách Hàng"
        subtitle="Quản lý hồ sơ trung tâm, theo dõi phân hạng và lịch sử mua hàng"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/customers/create')}
        icon={Users}
        searchPlaceholder="Tìm kiếm theo mã, tên, số điện thoại..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-260">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                {/* Cột Profile siêu cấp (Gộp Tên, Mã, SĐT, Avatar) */}
                <th className="w-[24%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Hồ Sơ Khách Hàng
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="BẬC HẠNG"
                    options={tierOptions}
                    selectedValues={tierFilter}
                    onApply={setTierFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="PHÂN LOẠI"
                    options={typeOptions}
                    selectedValues={typeFilter}
                    onApply={setTypeFilter}
                  />
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="NHÓM MARKETING"
                    options={groupOptions}
                    selectedValues={groupFilter}
                    onApply={setGroupFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
                    />
                  </div>
                </th>

                <th className="w-[8%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[8%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="CẬP NHẬT"
                      selectedDate={updatedAtFilter}
                      onApply={setUpdatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <TableLoading colSpan={8} />
              ) : data.length > 0 ? (
                data.map((item) => (
                  <tr
                    key={item.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    {/* CELL 1: PROFILE THÔNG MINH */}
                    <td className="py-3 px-6">
                      <div className="flex items-center gap-3.5">
                        <div className="w-10 h-10 rounded-full bg-white border border-slate-200 shadow-sm flex items-center justify-center shrink-0 overflow-hidden">
                          {item.avatarPath ? (
                            <img
                              src={item.avatarPath}
                              alt={item.name}
                              className="w-full h-full object-cover"
                            />
                          ) : (
                            <User size={18} className="text-slate-400" />
                          )}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-extrabold text-slate-800 text-[13px] truncate max-w-50 leading-tight">
                            {item.name}
                          </span>
                          <div className="flex items-center gap-2 mt-1">
                            <span className="text-[10px] font-bold bg-yellow-50 text-yellow-700 px-1.5 py-0.5 rounded border border-yellow-200/50 uppercase tracking-widest">
                              {item.code}
                            </span>
                            <span className="text-[11px] font-medium text-slate-500 tracking-wide">
                              {item.phoneNumber}
                            </span>
                          </div>
                        </div>
                      </div>
                    </td>

                    {/* CELL 2: TIER (BẬC) */}
                    <td className="py-3 px-2">
                      <span className="inline-flex px-2.5 py-1 rounded-lg text-xs font-bold bg-amber-50 text-amber-700 border border-amber-200/60 shadow-sm">
                        {item.customerTierName || '---'}
                      </span>
                    </td>

                    {/* CELL 3: TYPE (PHÂN LOẠI) */}
                    <td className="py-3 px-2">
                      <span className="inline-flex px-2.5 py-1 rounded-lg text-xs font-semibold bg-indigo-50 text-indigo-700 border border-indigo-200/60">
                        {item.customerTypeName || '---'}
                      </span>
                    </td>

                    {/* CELL 4: GROUPS (TAG THÔNG MINH CO DÃN) */}
                    <td className="py-3 px-2">
                      <div className="flex flex-wrap gap-1.5 items-center">
                        {item.groups && item.groups.length > 0 ? (
                          <>
                            {item.groups.slice(0, 2).map((groupName, idx) => (
                              <span
                                key={idx}
                                className="inline-flex px-2 py-0.5 rounded text-[11px] font-semibold bg-blue-50 text-blue-600 border border-blue-100 truncate max-w-22.5"
                              >
                                {groupName}
                              </span>
                            ))}
                            {item.groups.length > 2 && (
                              <span
                                className="inline-flex px-1.5 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-500 border border-slate-200 cursor-help"
                                title={item.groups.slice(2).join(', ')}
                              >
                                +{item.groups.length - 2}
                              </span>
                            )}
                          </>
                        ) : (
                          <span className="text-[11px] text-slate-400 italic">Chưa phân nhóm</span>
                        )}
                      </div>
                    </td>

                    {/* CELL 5: STATUS WITH TOGGLE */}
                    <td className="py-3 px-2 text-center">
                      <button
                        onClick={() => handleToggleActive(item.id, item.isActive)}
                        className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold transition-all cursor-pointer ${
                          item.isActive
                            ? 'bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100'
                            : 'bg-slate-100 text-slate-600 border border-slate-200 hover:bg-slate-200'
                        }`}
                        title="Nhấn để đổi trạng thái"
                      >
                        <span
                          className={`w-1.5 h-1.5 rounded-full ${item.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`}
                        ></span>
                        {item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                      </button>
                    </td>

                    {/* CELL 6 & 7: DATES */}
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    {/* CELL 8: ACTIONS */}
                    <td className="py-3 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/customers/${item.id}`)}
                          className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors"
                          title="Xem chi tiết"
                        >
                          <Eye size={17} strokeWidth={2.5} />
                        </button>
                        <button
                          onClick={() => navigate(`/customers/edit/${item.id}`)}
                          className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                          title="Chỉnh sửa"
                        >
                          <Edit3 size={17} strokeWidth={2.5} />
                        </button>
                        <button
                          onClick={() => {
                            setDeletingRecord(item);
                            setIsModalOpen(true);
                          }}
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
                <TableEmpty
                  colSpan={8}
                  message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc."
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
        isOpen={isModalOpen}
        itemName={deletingRecord?.name || ''}
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default CustomerList;
