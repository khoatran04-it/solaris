import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Hexagon, Edit3, Trash2 } from 'lucide-react';

// API & Types
import { customerTierApi } from '../../api/customerTierApi';
import { CustomerTier } from '../../types/customerTier';

// Commons Components
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';

// Atomic Components
import {
  ListPageContainer,
  ListCard,
  ListHeader,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateTimeCell,
} from '../../components/commons/ListUI';

const CustomerTierList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<CustomerTier[]>([]);
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
  const [deletingRecord, setDeletingRecord] = useState<CustomerTier | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // Debounce Search (chờ 500ms)
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Reset trang về 1 khi điều kiện lọc thay đổi
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const isActiveParam = statusFilter.length === 1 ? statusFilter[0] === 1 : undefined;

      const res = await customerTierApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        isActive: isActiveParam,
        createdAt: createdAtFilter ? createdAtFilter.toLocaleDateString('en-CA') : undefined,
        updatedAt: updatedAtFilter ? updatedAtFilter.toLocaleDateString('en-CA') : undefined,
      });

      setData(res.items || []);
      setTotalPages(res.totalPages || 0);
      setTotalItems(res.totalRecords || 0);
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

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await customerTierApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Đã xóa phân bậc "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await customerTierApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} bậc hạng`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        icon={Hexagon}
        title="Phân Bậc Khách Hàng"
        subtitle="Quản lý bậc khách hàng, chiết khấu và điều kiện áp dụng"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/customer-tiers/create')}
        searchPlaceholder="Tìm theo mã, tên..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-240">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã
                </th>
                <th className="w-[18%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Tên Phân Bậc
                </th>
                <th className="w-[10%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Chiết Khấu
                </th>
                <th className="w-[16%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                  Chi tiêu tối thiểu
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
                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>
                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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
                    <td className="py-4 px-6">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-md bg-yellow-50 text-yellow-700 text-sm font-bold border border-yellow-200/50">
                        {item.code}
                      </span>
                    </td>
                    <td className="py-4 px-6 font-semibold text-slate-800">{item.name}</td>
                    <td className="py-4 px-4 text-center font-bold text-emerald-600">
                      {Number(item.discountPercent).toFixed(1)}%
                    </td>
                    <td className="py-4 px-4 text-right font-semibold text-slate-700">
                      {Number(item.minSpending).toLocaleString('vi-VN')} đ
                    </td>
                    <td className="py-4 px-2 text-center">
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
                    <td className="py-4 px-2 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>
                    <td className="py-4 px-2 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>
                    <td className="py-4 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/customer-tiers/edit/${item.id}`)}
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
                <TableEmpty colSpan={8} message="Không tìm thấy bậc khách hàng phù hợp." />
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

export default CustomerTierList;
