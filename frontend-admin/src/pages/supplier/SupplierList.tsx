import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Hexagon, Edit3, Trash2, Eye, Building2, Mail, Phone } from 'lucide-react';

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
  StatusBadge,
} from '../../components/commons/ListUI';
import { ExportCsvButton } from '../../utils/exportUtils';

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
        supplierTypeIds: typeFilter.length > 0 ? typeFilter.join(',') : undefined,
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

  // Cập nhật trạng thái hoạt động
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
        searchPlaceholder="Tìm kiếm theo mã, tên, số điện thoại..."
        action={
          <ExportCsvButton
            filename={`Danh-sach-nha-cung-cap_${new Date().toISOString().slice(0, 10)}`}
            label="Xuất CSV"
            currentPageData={data}
            totalItems={totalItems}
            filterSnapshots={[
              { label: 'Từ khóa', value: debouncedSearch },
              {
                label: 'Phân loại',
                value: typeFilter
                  .map((id) => typeOptions.find((o) => o.value === id)?.label)
                  .filter(Boolean)
                  .join(', '),
              },
              {
                label: 'Trạng thái',
                value:
                  statusFilter.length === 1
                    ? statusFilter[0] === 1
                      ? 'Hoạt động'
                      : 'Ngừng hoạt động'
                    : '',
              },
              {
                label: 'Ngày tạo',
                value: createdAtFilter ? createdAtFilter.toLocaleDateString('vi-VN') : '',
              },
              {
                label: 'Ngày sửa',
                value: updatedAtFilter ? updatedAtFilter.toLocaleDateString('vi-VN') : '',
              },
            ]}
            onFetchAll={async () => {
              const isActiveParam = statusFilter.length === 1 ? statusFilter[0] === 1 : undefined;
              const response = await supplierApi.getAll({
                search: debouncedSearch,
                pageIndex: 1,
                pageSize: totalItems || 1000,
                supplierTypeIds: typeFilter.length > 0 ? typeFilter.join(',') : undefined,
                isActive: isActiveParam,
                createdAt: createdAtFilter
                  ? createdAtFilter.toLocaleDateString('en-CA')
                  : undefined,
                updatedAt: updatedAtFilter
                  ? updatedAtFilter.toLocaleDateString('en-CA')
                  : undefined,
              });
              return response.items || [];
            }}
            columns={[
              { header: 'Mã NCC', accessor: (s) => s.code },
              { header: 'Tên Đối Tác', accessor: (s) => s.name },
              { header: 'Phân Loại', accessor: (s) => s.supplierTypeName || '' },
              { header: 'Mã Số Thuế', accessor: (s) => s.taxCode || '' },
              { header: 'Số Điện Thoại', accessor: (s) => s.phone || '' },
              { header: 'Email', accessor: (s) => s.email || '' },
              { header: 'Địa Chỉ', accessor: (s) => s.address || '' },
              {
                header: 'Trạng Thái',
                accessor: (s) => (s.isActive ? 'Hoạt động' : 'Ngừng hoạt động'),
              },
              {
                header: 'Ngày Hợp Tác',
                accessor: (s) =>
                  s.createdAt ? new Date(s.createdAt).toLocaleDateString('vi-VN') : '',
              },
            ]}
          />
        }
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-300">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100 text-center">
                <th className="w-[28%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  NHÀ CUNG CẤP
                </th>

                <th className="w-[18%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  LIÊN HỆ & THUẾ
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  <CustomFilter
                    title="PHÂN LOẠI"
                    options={typeOptions}
                    selectedValues={typeFilter}
                    onApply={setTypeFilter}
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

                <th className="w-[9%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[9%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="CẬP NHẬT"
                      selectedDate={updatedAtFilter}
                      onApply={setUpdatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[9%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
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
                    {/* CELL 1: PROFILE NHÀ CUNG CẤP THÔNG MINH */}
                    <td className="py-3 px-6">
                      <div className="flex items-center gap-3.5">
                        <div className="w-10 h-10 rounded-full bg-white border border-slate-200 shadow-sm flex items-center justify-center shrink-0 overflow-hidden">
                          {item.logoPath ? (
                            <img
                              src={item.logoPath}
                              alt={item.name}
                              className="w-full h-full object-cover"
                            />
                          ) : (
                            <Building2 size={18} className="text-slate-400" />
                          )}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-extrabold text-slate-800 text-[13px] truncate max-w-56 leading-tight">
                            {item.name}
                          </span>
                          <div className="flex items-center gap-2 mt-1">
                            <span className="text-[10px] font-bold bg-yellow-50 text-yellow-700 px-1.5 py-0.5 rounded border border-yellow-200/50 uppercase tracking-widest">
                              {item.code}
                            </span>
                            <span className="text-[11px] font-medium text-slate-500 tracking-wide flex items-center gap-1">
                              <Phone size={11} className="text-slate-400" />
                              {item.phone}
                            </span>
                          </div>
                        </div>
                      </div>
                    </td>

                    {/* CELL 2: LIÊN HỆ & THUẾ */}
                    <td className="py-3 px-2">
                      <div className="flex flex-col gap-1">
                        {item.email ? (
                          <div
                            className="flex items-center gap-1.5 text-[12px] text-slate-600 truncate max-w-44"
                            title={item.email}
                          >
                            <Mail size={12} className="text-slate-400 shrink-0" />
                            <span className="truncate">{item.email}</span>
                          </div>
                        ) : (
                          <span className="text-[11px] text-slate-300 italic">Chưa có email</span>
                        )}
                        {item.taxCode ? (
                          <span className="text-[11px] text-slate-500 font-medium">
                            MST:{' '}
                            <span className="font-semibold text-slate-700">{item.taxCode}</span>
                          </span>
                        ) : (
                          <span className="text-[11px] text-slate-300 italic">Chưa có MST</span>
                        )}
                      </div>
                    </td>

                    {/* CELL 3: PHÂN LOẠI */}
                    <td className="py-3 px-2">
                      <span className="inline-flex px-2.5 py-1 rounded-lg text-xs font-semibold bg-indigo-50 text-indigo-700 border border-indigo-200/60">
                        {item.supplierTypeName || '---'}
                      </span>
                    </td>

                    {/* CELL 4: TRẠNG THÁI (TOGGLE BUTTON) */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        <StatusBadge
                          label={item.isActive ? 'Hoạt động' : 'Ngừng hoạt động'}
                          variant={item.isActive ? 'emerald' : 'rose'}
                          onClick={() => handleToggleActive(item.id, item.isActive)}
                          title="Nhấn để đổi trạng thái"
                        />
                      </div>
                    </td>

                    {/* CELL 5 & 6: NGÀY TẠO & CẬP NHẬT */}
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    {/* CELL 7: THAO TÁC */}
                    <td className="py-3 px-6">
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
                  colSpan={7}
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
