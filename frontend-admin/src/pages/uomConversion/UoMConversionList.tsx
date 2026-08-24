import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Repeat, Edit3, Trash2, ArrowRight } from 'lucide-react';

// API & Types
import { uomConversionApi } from '../../api/uomConversionApi';
import { productApi } from '../../api/productApi';
import { UoMConversion } from '../../types/uomConversion';

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

const UoMConversionList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES ---
  const [data, setData] = useState<UoMConversion[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- FILTERS ---
  const [standardFilter, setStandardFilter] = useState<(string | number)[]>([]);
  const [productFilter, setProductFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  // --- OPTIONS ---
  const [productOptions, setProductOptions] = useState<{ label: string; value: number }[]>([]);

  const standardOptions = [
    { label: 'Quy đổi tiêu chuẩn', value: 1 },
    { label: 'Quy đổi đặc thù', value: 0 },
  ];

  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<UoMConversion | null>(null);
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
    // Tải danh sách sản phẩm cho bộ lọc
    productApi
      .getAllList()
      .then((res) => setProductOptions(res.map((p) => ({ label: p.name, value: p.id }))))
      .catch(() => showToast('warning', 'Không tải được bộ lọc sản phẩm'));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [
    debouncedSearch,
    standardFilter,
    productFilter,
    statusFilter,
    createdAtFilter,
    updatedAtFilter,
  ]);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await uomConversionApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        isStandard: standardFilter.length === 1 ? standardFilter[0] === 1 : undefined,
        productId: productFilter.length === 1 ? Number(productFilter[0]) : undefined,
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
    standardFilter,
    productFilter,
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
      await uomConversionApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công dữ liệu quy đổi.`);
    } catch (error) {
      showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await uomConversionApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} quy tắc quy đổi`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái hoạt động');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Tỷ Lệ Quy Đổi"
        subtitle="Cấu hình hệ số chuyển đổi giữa các đơn vị đo lường"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/uom-conversions/create')}
        icon={Repeat}
        searchPlaceholder="Tìm theo đơn vị hoặc tên sản phẩm..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="PHÂN LOẠI"
                    options={standardOptions}
                    selectedValues={standardFilter}
                    onApply={setStandardFilter}
                  />
                </th>

                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Công thức quy đổi
                </th>

                <th className="w-[20%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="ÁP DỤNG CHO"
                    options={productOptions}
                    selectedValues={productFilter}
                    onApply={setProductFilter}
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

                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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
                <TableLoading colSpan={6} />
              ) : data.length > 0 ? (
                data.map((item) => (
                  <tr
                    key={item.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    <td className="py-4 px-4">
                      {item.productId ? (
                        <span className="inline-flex px-2 py-1 rounded-md bg-purple-50 text-purple-700 text-[11px] font-bold border border-purple-200">
                          Đặc thù SP
                        </span>
                      ) : (
                        <span className="inline-flex px-2 py-1 rounded-md bg-blue-50 text-blue-700 text-[11px] font-bold border border-blue-200">
                          Tiêu chuẩn
                        </span>
                      )}
                    </td>

                    <td className="py-4 px-6">
                      <div className="flex items-center gap-3 font-extrabold text-slate-800 text-sm">
                        <span className="px-2 py-1 bg-slate-100 rounded text-slate-600 font-bold border border-slate-200">
                          1 {item.fromUoMName}
                        </span>
                        <ArrowRight size={16} className="text-slate-400" />
                        <span className="px-2 py-1 bg-indigo-50 text-indigo-700 rounded font-bold border border-indigo-100 shadow-sm">
                          {item.conversionFactor.toLocaleString()} {item.toUoMName}
                        </span>
                      </div>
                    </td>

                    <td className="py-4 px-4">
                      {item.productId ? (
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-700 text-sm line-clamp-1">
                            {item.productName}
                          </span>
                          <span className="text-[11px] font-semibold text-slate-400">
                            {item.productCode}
                          </span>
                        </div>
                      ) : (
                        <span className="text-sm font-semibold text-slate-400 italic">
                          Mọi sản phẩm
                        </span>
                      )}
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
                      <DateTimeCell isoString={item.updatedAt} />
                    </td>

                    <td className="py-4 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/uom-conversions/edit/${item.id}`)}
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
                <TableEmpty colSpan={6} message="Thử thay đổi điều kiện lọc." />
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
        itemName="dữ liệu quy đổi này"
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default UoMConversionList;
