import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Hexagon, Edit3, Trash2 } from 'lucide-react';

// API & Types
import { supplierTypeApi } from '../../api/supplierTypeApi';
import { SupplierType } from '../../types/supplierType';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';

// Hạt nhân UI (Atomic Components)
import {
  ListPageContainer,
  ListHeader,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateTimeCell,
  StatusBadge,
} from '../../components/commons/ListUI';

const SupplierTypeList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<SupplierType[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<SupplierType | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT: Debounce Search (chờ 500ms) ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT: Reset trang về 1 khi điều kiện lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- EFFECT: Fetch Data từ API ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await supplierTypeApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
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
  }, [currentPage, debouncedSearch, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await supplierTypeApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Đã xóa phân loại "${deletingRecord.name}"`);
    } catch (error) {
      showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await supplierTypeApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'khóa' : 'kích hoạt'} phân loại`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Phân Loại Nhà Cung Cấp"
        subtitle="Thiết lập và quản lý các nhóm đối tác chiến lược"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/supplier-types/create')}
        icon={Hexagon}
        searchPlaceholder="Tìm kiếm theo mã, tên phân loại..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-250">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã ID
                </th>

                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Phân Loại
                </th>

                <th className="w-[24%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Mô Tả Chi Tiết
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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
                <TableLoading colSpan={7} />
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

                    <td className="py-4 px-6 text-slate-500 text-sm font-medium">
                      {item.description || '-'}
                    </td>

                    {/* CELL: TRẠNG THÁI (TOGGLE) */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        <StatusBadge
                          label={item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                          variant={item.isActive ? 'emerald' : 'rose'}
                          onClick={() => handleToggleActive(item.id, item.isActive)}
                          title="Nhấn để đổi trạng thái"
                        />
                      </div>
                    </td>

                    <td className="py-4 px-6 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>

                    <td className="py-4 px-6 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    <td className="py-4 px-6">
                      <div className="flex justify-center gap-2 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/supplier-types/edit/${item.id}`)}
                          className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                          title="Chỉnh sửa"
                        >
                          <Edit3 size={18} strokeWidth={2.5} />
                        </button>
                        <button
                          onClick={() => {
                            setDeletingRecord(item);
                            setIsModalOpen(true);
                          }}
                          className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                          title="Xóa"
                        >
                          <Trash2 size={18} strokeWidth={2.5} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <TableEmpty
                  colSpan={7}
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

export default SupplierTypeList;
