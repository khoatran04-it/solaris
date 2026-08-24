import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Hexagon, Edit3, Trash2, Eye } from 'lucide-react';

// API & Types
import { supplierApi } from '../../api/supplierApi';
import { supplierTypeApi } from '../../api/supplierTypeApi';
import { Supplier } from '../../types/supplier';

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

const SupplierList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<Supplier[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [typeFilter, setTypeFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  const [typeOptions, setTypeOptions] = useState<{ label: string; value: number }[]>([]);
  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Ngừng hoạt động', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<Supplier | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Tải danh sách filter (SupplierType) ---
  useEffect(() => {
    supplierTypeApi
      .getAllList()
      .then((types) => {
        setTypeOptions(types.map((t) => ({ label: t.name, value: t.id })));
      })
      .catch(() => showToast('warning', 'Không tải được bộ lọc Phân loại NCC'));
  }, []);

  // --- EFFECT 2: Debounce Search (chờ 500ms) ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT 3: Reset trang về 1 khi điều kiện lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, typeFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- EFFECT 4: Fetch Data từ API ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const isActiveParam = statusFilter.length === 1 ? statusFilter[0] === 1 : undefined;

      const response = await supplierApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        supplierTypesId: typeFilter.length > 0 ? typeFilter.join(',') : undefined,
        isActive: isActiveParam,
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
  }, [currentPage, debouncedSearch, typeFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await supplierApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công nhà cung cấp "${deletingRecord.name}"`);
    } catch (error) {
      showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  // 🔥 XỬ LÝ TOGGLE TRẠNG THÁI
  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await supplierApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'khóa' : 'kích hoạt'} nhà cung cấp`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Danh Sách Nhà Cung Cấp"
        subtitle="Quản lý hồ sơ đối tác, trạng thái giao dịch và phân loại cung ứng"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/suppliers/create')}
        icon={Hexagon}
        searchPlaceholder="Tìm theo mã, tên, số điện thoại..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-300">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100 text-center">
                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã ID
                </th>
                <th className="w-[18%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Nhà Cung Cấp
                </th>

                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  <CustomFilter
                    title="PHÂN LOẠI"
                    options={typeOptions}
                    selectedValues={typeFilter}
                    onApply={setTypeFilter}
                  />
                </th>

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  SĐT
                </th>

                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="CẬP NHẬT"
                      selectedDate={updatedAtFilter}
                      onApply={setUpdatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[11%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
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
                    <td className="py-4 px-6">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-md bg-yellow-50 text-yellow-700 text-sm font-bold border border-yellow-200/50">
                        {item.code}
                      </span>
                    </td>
                    <td className="py-4 px-6 font-semibold text-slate-800">{item.name}</td>
                    <td className="py-4 px-6">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-lg bg-slate-100 text-slate-700 text-xs font-semibold border border-slate-200/30">
                        {item.supplierTypeName}
                      </span>
                    </td>
                    <td className="py-4 px-6 text-center text-slate-600 font-medium text-sm">
                      {item.phone}
                    </td>

                    {/* 🔥 CELL: TRẠNG THÁI (TOGGLE BUTTON) */}
                    <td className="py-4 px-6 text-center">
                      <div className="flex justify-center">
                        <button
                          onClick={() => handleToggleActive(item.id, item.isActive)}
                          className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold border transition-colors ${
                            item.isActive
                              ? 'bg-emerald-50 text-emerald-700 border-emerald-200/40 hover:bg-red-50 hover:text-red-600 hover:border-red-200'
                              : 'bg-slate-100 text-slate-400 border-slate-200/50 hover:bg-emerald-50 hover:text-emerald-600 hover:border-emerald-200'
                          }`}
                        >
                          <span
                            className={`w-1.5 h-1.5 rounded-full mr-1.5 ${item.isActive ? 'bg-emerald-500' : 'bg-slate-300'}`}
                          ></span>
                          {item.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
                        </button>
                      </div>
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
                          onClick={() => navigate(`/suppliers/${item.id}`)}
                          className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors"
                          title="Xem chi tiết"
                        >
                          <Eye size={17} strokeWidth={2.5} />
                        </button>
                        <button
                          onClick={() => navigate(`/suppliers/edit/${item.id}`)}
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
                  message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc đối tác."
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

export default SupplierList;
