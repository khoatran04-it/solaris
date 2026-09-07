import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Scale, Edit3, Trash2 } from 'lucide-react';

// API & Types
import { uomApi } from '../../api/uomApi';
import { uomCategoryApi } from '../../api/uomCategoryApi';
import { UoM } from '../../types/uom';

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
  StatusBadge,
} from '../../components/commons/ListUI';

const UoMList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<UoM[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [categoryFilter, setCategoryFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const [categoryOptions, setCategoryOptions] = useState<{ label: string; value: number }[]>([]);
  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<UoM | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Tải danh sách Category cho Dropdown Lọc ---
  useEffect(() => {
    uomCategoryApi
      .getAllList()
      .then((categories) => {
        setCategoryOptions(categories.map((c) => ({ label: c.name, value: c.id })));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách nhóm đơn vị'));
  }, []);

  // --- EFFECT 2: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT 3: Reset trang khi bộ lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, categoryFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- EFFECT 4: Fetch Data ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await uomApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        categoryId: categoryFilter.length === 1 ? Number(categoryFilter[0]) : undefined,
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
    categoryFilter,
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
      await uomApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công đơn vị "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await uomApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} đơn vị tính`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái hoạt động');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Đơn Vị Tính"
        subtitle="Quản lý chi tiết các đơn vị tính và từ khóa hỗ trợ AI Chatbot"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/uoms/create')}
        icon={Scale}
        searchPlaceholder="Tìm theo mã, tên hoặc từ khóa..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Đơn vị tính
                </th>
                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Từ đồng nghĩa (AI)
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="THUỘC NHÓM"
                    options={categoryOptions}
                    selectedValues={categoryFilter}
                    onApply={setCategoryFilter}
                  />
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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

                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
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
                      <div className="flex flex-col">
                        <span className="font-bold text-slate-800 text-sm">{item.name}</span>
                        <span className="font-mono text-[11px] font-bold text-slate-700 bg-slate-100 border border-slate-200 px-1.5 py-0.5 rounded w-fit mt-1">
                          {item.code}
                        </span>
                      </div>
                    </td>

                    <td className="py-4 px-6">
                      {item.synonyms ? (
                        <div className="flex flex-wrap gap-1.5">
                          {item.synonyms.split(',').map((syn, idx) => (
                            <span
                              key={idx}
                              className="inline-flex px-1.5 py-0.5 rounded text-[11px] font-medium bg-slate-100 text-slate-500 border border-slate-200"
                            >
                              {syn.trim()}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400 italic">Chưa cấu hình</span>
                      )}
                    </td>

                    <td className="py-4 px-2">
                      <span className="inline-flex px-2 py-1 rounded-lg text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-100">
                        {item.categoryName || '---'}
                      </span>
                    </td>

                    <td className="py-4 px-2 text-center">
                      <div className="flex justify-center">
                        <StatusBadge
                          label={item.isActive ? 'Hoạt động' : 'Tạm khóa'}
                          variant={item.isActive ? 'emerald' : 'rose'}
                          onClick={() => handleToggleActive(item.id, item.isActive)}
                          title="Nhấn để đổi trạng thái"
                        />
                      </div>
                    </td>

                    <td className="py-4 px-2 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>

                    <td className="py-4 px-2 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    <td className="py-4 px-6">
                      <div className="flex justify-center gap-1.5 opacity-70 group-hover:opacity-100 transition-all duration-200">
                        <button
                          onClick={() => navigate(`/uoms/edit/${item.id}`)}
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

export default UoMList;
