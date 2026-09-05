import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Warehouse as WarehouseIcon, Edit3, Trash2, MapPin, User as UserIcon } from 'lucide-react';

// API & Types
import { warehouseApi } from '../../api/warehouseApi';
import { Warehouse } from '../../types/warehouse';

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
  StatusBadge,
} from '../../components/commons/ListUI';

const WarehouseList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<Warehouse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [provinceFilter, setProvinceFilter] = useState<(string | number)[]>([]);

  // --- OPTIONS CHO BỘ LỌC ---
  const [provinceOptions, setProvinceOptions] = useState<{ label: string; value: string }[]>([]);

  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<Warehouse | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECTS ---
  useEffect(() => {
    warehouseApi
      .getAllList()
      .then((res) => {
        const uniqueProvinces = Array.from(new Set(res.map((w) => w.province).filter(Boolean)));
        setProvinceOptions(uniqueProvinces.map((p) => ({ label: p, value: p })));
      })
      .catch(() => showToast('warning', 'Không tải được danh mục Khu vực'));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, statusFilter, provinceFilter]);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await warehouseApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
        province: provinceFilter.length > 0 ? provinceFilter[0].toString() : undefined,
      });

      setData(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU KHO HÀNG');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage, debouncedSearch, statusFilter, provinceFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await warehouseApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công kho hàng "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await warehouseApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} kho hàng`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Danh Sách Kho Hàng"
        subtitle="Quản lý thông tin mạng lưới kho vận, địa chỉ và nhân sự phụ trách"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/warehouses/create')}
        icon={WarehouseIcon}
        searchPlaceholder="Tìm kiếm theo mã, tên kho..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[25%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Thông Tin Kho
                </th>

                <th className="w-[20%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Địa Chỉ Chi Tiết
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHU VỰC (TỈNH/THÀNH)"
                    options={provinceOptions}
                    selectedValues={provinceFilter}
                    onApply={setProvinceFilter}
                  />
                </th>

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Trưởng Kho
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
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
                <TableLoading colSpan={6} />
              ) : data.length > 0 ? (
                data.map((item) => {
                  return (
                    <tr
                      key={item.id}
                      className="hover:bg-slate-50/80 transition-colors duration-200 group"
                    >
                      {/* CELL 1: THÔNG TIN KHO */}
                      <td className="py-3 px-6">
                        <div className="flex flex-col">
                          <span
                            className="font-extrabold text-slate-800 text-[14px] leading-tight truncate max-w-56"
                            title={item.name}
                          >
                            {item.name}
                          </span>
                          <div className="flex items-center gap-2 mt-1.5">
                            <span className="text-[10px] font-bold bg-blue-50 text-blue-700 px-1.5 py-0.5 rounded border border-blue-200/50 uppercase tracking-widest">
                              {item.code}
                            </span>
                            {item.warehouseType && (
                              <span className="text-[10px] font-medium text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
                                {item.warehouseType}
                              </span>
                            )}
                            <span className="text-[10px] font-semibold text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-200/60">
                              📦 {item.totalCapacityCbm ?? 500} m³
                            </span>
                          </div>
                        </div>
                      </td>

                      {/* CELL 2: ĐỊA CHỈ CHI TIẾT */}
                      <td className="py-3 px-2">
                        <div className="flex items-start gap-1.5">
                          <MapPin size={14} className="text-slate-400 mt-0.5 shrink-0" />
                          <span
                            className="text-[12px] font-medium text-slate-600 line-clamp-2 pr-4"
                            title={item.fullAddress}
                          >
                            {item.fullAddress}
                          </span>
                        </div>
                      </td>

                      {/* CELL 3: KHU VỰC */}
                      <td className="py-3 px-2">
                        <span className="text-[12px] font-semibold text-slate-700 bg-slate-100 px-2 py-1 rounded-md">
                          {item.province}
                        </span>
                      </td>

                      {/* CELL 4: TRƯỞNG KHO */}
                      <td className="py-3 px-2">
                        <div className="flex items-center gap-2">
                          <div className="w-6 h-6 rounded-full bg-indigo-50 border border-indigo-100 flex items-center justify-center shrink-0">
                            <UserIcon size={12} className="text-indigo-400" />
                          </div>
                          {item.managerName ? (
                            <span className="text-[13px] font-bold text-slate-700">
                              {item.managerName}
                            </span>
                          ) : (
                            <span className="text-[12px] italic text-slate-400">Chưa bổ nhiệm</span>
                          )}
                        </div>
                      </td>

                      {/* CELL 5: TRẠNG THÁI */}
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

                      {/* CELL 6: THAO TÁC */}
                      <td className="py-3 px-6">
                        <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                          <button
                            onClick={() => navigate(`/warehouses/edit/${item.id}`)}
                            className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors cursor-pointer"
                            title="Chỉnh sửa"
                          >
                            <Edit3 size={17} strokeWidth={2.5} />
                          </button>
                          <button
                            onClick={() => {
                              setDeletingRecord(item);
                              setIsModalOpen(true);
                            }}
                            className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors cursor-pointer"
                            title="Xóa"
                          >
                            <Trash2 size={17} strokeWidth={2.5} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              ) : (
                <TableEmpty
                  colSpan={6}
                  message="Thử thay đổi từ khóa tìm kiếm hoặc bộ lọc khu vực."
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

export default WarehouseList;
