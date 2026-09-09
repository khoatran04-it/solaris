import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Package, Image as ImageIcon } from 'lucide-react';

// API & Types
import { productApi } from '../../api/productApi';
import { productCategoryApi } from '../../api/productCategoryApi';
import { uomApi } from '../../api/uomApi';
import { Product } from '../../types/product';

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

const ProductList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [categoryFilter, setCategoryFilter] = useState<(string | number)[]>([]);
  const [uomFilter, setUomFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const [categoryOptions, setCategoryOptions] = useState<{ label: string; value: number }[]>([]);
  const [uomOptions, setUomOptions] = useState<{ label: string; value: number }[]>([]);
  const statusOptions = [
    { label: 'Đang bán', value: 1 },
    { label: 'Ngừng bán', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<Product | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Tải danh sách filter (Category & UoM) ---
  useEffect(() => {
    Promise.all([productCategoryApi.getAllList(), uomApi.getAllList()])
      .then(([categories, uoms]) => {
        setCategoryOptions(categories.map((c) => ({ label: c.name, value: c.id })));
        setUomOptions(uoms.map((u) => ({ label: u.name, value: u.id })));
      })
      .catch(() => showToast('warning', 'Không tải được bộ lọc Danh mục / ĐVT'));
  }, []);

  // --- EFFECT 2: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT 3: Reset trang khi bộ lọc thay đổi ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, categoryFilter, uomFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- EFFECT 4: Fetch Data ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await productApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        categoryId: categoryFilter.length > 0 ? categoryFilter.join(',') : undefined,
        baseUoMId: uomFilter.length > 0 ? uomFilter.join(',') : undefined,
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
    uomFilter,
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
      await productApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công sản phẩm "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await productApi.toggleActive(id);
      fetchData();
      showToast(
        'success',
        `Đã chuyển sản phẩm sang trạng thái ${currentStatus ? 'Ngừng bán' : 'Đang bán'}`
      );
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái sản phẩm');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Sản Phẩm Khung (Sản Phẩm Gốc)"
        subtitle="Quản lý danh sách sản phẩm gốc và thông tin cơ bản trước khi chia biến thể"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/products/create')}
        icon={Package}
        searchPlaceholder="Tìm kiếm theo mã, tên sản phẩm..."
        action={
          <ExportCsvButton
            filename={`Danh-sach-san-pham_${new Date().toISOString().slice(0, 10)}`}
            label="Xuất CSV"
            currentPageData={data}
            totalItems={totalItems}
            filterSnapshots={[
              { label: 'Từ khóa', value: debouncedSearch },
              {
                label: 'Danh mục',
                value: categoryFilter
                  .map((id) => categoryOptions.find((o) => o.value === id)?.label)
                  .filter(Boolean)
                  .join(', '),
              },
              {
                label: 'Đơn vị tính',
                value: uomFilter
                  .map((id) => uomOptions.find((o) => o.value === id)?.label)
                  .filter(Boolean)
                  .join(', '),
              },
              {
                label: 'Trạng thái',
                value:
                  statusFilter.length === 1
                    ? statusFilter[0] === 1
                      ? 'Đang bán'
                      : 'Ngừng bán'
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
              const response = await productApi.getAll({
                search: debouncedSearch,
                pageIndex: 1,
                pageSize: totalItems || 1000,
                categoryId: categoryFilter.length > 0 ? categoryFilter.join(',') : undefined,
                baseUoMId: uomFilter.length > 0 ? uomFilter.join(',') : undefined,
                isActive: statusFilter.length === 1 ? statusFilter[0] === 1 : undefined,
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
              { header: 'Mã Sản Phẩm', accessor: (p) => p.code },
              { header: 'Tên Sản Phẩm', accessor: (p) => p.name },
              { header: 'Danh Mục', accessor: (p) => p.categoryName || '' },
              { header: 'ĐVT Cơ Sở', accessor: (p) => p.baseUoMName || '' },
              { header: 'Trạng Thái', accessor: (p) => (p.isActive ? 'Đang bán' : 'Ngừng bán') },
              {
                header: 'Ngày Tạo',
                accessor: (p) =>
                  p.createdAt ? new Date(p.createdAt).toLocaleDateString('vi-VN') : '',
              },
              {
                header: 'Ngày Cập Nhật',
                accessor: (p) =>
                  p.updatedAt ? new Date(p.updatedAt).toLocaleDateString('vi-VN') : '',
              },
            ]}
          />
        }
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Sản Phẩm
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="DANH MỤC"
                    options={categoryOptions}
                    selectedValues={categoryFilter}
                    onApply={setCategoryFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="ĐƠN VỊ TÍNH"
                    options={uomOptions}
                    selectedValues={uomFilter}
                    onApply={setUomFilter}
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

                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TẠO"
                      selectedDate={createdAtFilter}
                      onApply={setCreatedAtFilter}
                    />
                  </div>
                </th>

                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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
                    {/* CELL 1: THÔNG TIN SẢN PHẨM */}
                    <td className="py-3 px-6">
                      <div className="flex items-center gap-3.5">
                        <div className="w-12 h-12 rounded-xl bg-white border border-slate-200 shadow-sm flex items-center justify-center shrink-0 overflow-hidden">
                          {item.imagePath ? (
                            <img
                              src={item.imagePath}
                              alt={item.name}
                              className="w-full h-full object-cover"
                            />
                          ) : (
                            <ImageIcon size={20} className="text-slate-300" />
                          )}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-extrabold text-slate-800 text-[14px] truncate max-w-56 leading-tight">
                            {item.name}
                          </span>
                          <div className="flex items-center gap-2 mt-1.5">
                            <span className="text-[10px] font-bold bg-slate-800 text-white px-1.5 py-0.5 rounded shadow-sm uppercase tracking-widest">
                              {item.code}
                            </span>
                            {item.description && (
                              <span
                                className="text-[11px] font-medium text-slate-500 truncate max-w-32"
                                title={item.description}
                              >
                                {item.description}
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </td>

                    {/* CELL 2: DANH MỤC */}
                    <td className="py-3 px-2">
                      {item.categoryName ? (
                        <span
                          className="inline-flex px-2 py-1 rounded-md text-[11px] font-bold bg-blue-50 text-blue-700 border border-blue-200/60 truncate max-w-full"
                          title={item.categoryName}
                        >
                          {item.categoryName}
                        </span>
                      ) : (
                        <span className="text-[11px] text-slate-400 italic">Chưa xếp loại</span>
                      )}
                    </td>

                    {/* CELL 3: ĐƠN VỊ TÍNH CƠ BẢN */}
                    <td className="py-3 px-2">
                      <span className="inline-flex px-2.5 py-1 rounded-md text-[11px] font-extrabold bg-amber-50 text-amber-700 border border-amber-200/60">
                        {item.baseUoMName || '---'}
                      </span>
                    </td>

                    {/* CELL 4: STATUS VỚI TOGGLE */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        <StatusBadge
                          label={item.isActive ? 'Đang bán' : 'Ngừng bán'}
                          variant={item.isActive ? 'emerald' : 'rose'}
                          onClick={() => handleToggleActive(item.id, item.isActive)}
                          title="Nhấn để đổi trạng thái"
                        />
                      </div>
                    </td>

                    {/* CELL 5 & 6: DATES */}
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.createdAt} />
                    </td>
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    {/* CELL 7: ACTIONS */}
                    <td className="py-3 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/products/edit/${item.id}`)}
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

export default ProductList;
