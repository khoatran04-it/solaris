import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Settings2, ShieldAlert, ShieldCheck } from 'lucide-react';

// API & Types
import { categoryAttributeApi } from '../../api/categoryAttributeApi';
import { productCategoryApi } from '../../api/productCategoryApi';
import { attributeDefinitionApi } from '../../api/attributeDefinitionApi';
import { CategoryAttribute } from '../../types/categoryAttribute';

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
} from '../../components/commons/ListUI';

const CategoryAttributeList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<CategoryAttribute[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const pageSize = 10;

  // --- STATE QUẢN LÝ TÌM KIẾM & BỘ LỌC (FILTERS) ---
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<(string | number)[]>([]);
  const [attributeFilter, setAttributeFilter] = useState<(string | number)[]>([]);

  // --- OPTIONS CHO BỘ LỌC ---
  const [categoryOptions, setCategoryOptions] = useState<{ label: string; value: number }[]>([]);
  const [attributeOptions, setAttributeOptions] = useState<{ label: string; value: number }[]>([]);

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<CategoryAttribute | null>(null);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Tải dữ liệu cho Bộ lọc ---
  useEffect(() => {
    Promise.all([productCategoryApi.getAllList(), attributeDefinitionApi.getAllList()])
      .then(([categories, attributes]) => {
        setCategoryOptions(categories.map((c) => ({ label: c.name, value: c.id })));
        setAttributeOptions(attributes.map((a) => ({ label: a.name, value: a.id })));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách bộ lọc'));
  }, []);

  // --- EFFECT 2: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // --- EFFECT 3: Reset trang khi thay đổi điều kiện lọc ---
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, categoryFilter, attributeFilter]);

  // --- EFFECT 4: Fetch Data ---
  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await categoryAttributeApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        categoryId: categoryFilter.length > 0 ? categoryFilter.join(',') : undefined,
        attributeDefinitionId: attributeFilter.length > 0 ? attributeFilter.join(',') : undefined,
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
  }, [currentPage, debouncedSearch, categoryFilter, attributeFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await categoryAttributeApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Gỡ thuộc tính thành công!`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Cấu Hình Thuộc Tính Danh Mục"
        subtitle="Gắn kết từ điển thuộc tính vào từng danh mục cụ thể (VD: Rau củ phải có độ tươi, Thịt cá phải có quy cách sơ chế)"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/category-attributes/create')}
        icon={Settings2}
        searchPlaceholder="Tìm kiếm theo tên danh mục, thuộc tính..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                {/* Cột 1: Danh Mục (Có Filter) */}
                <th className="w-[30%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  <CustomFilter
                    title="DANH MỤC SẢN PHẨM"
                    options={categoryOptions}
                    selectedValues={categoryFilter}
                    onApply={setCategoryFilter}
                  />
                </th>

                {/* Cột 2: Thuộc tính gắn kèm (Có Filter) */}
                <th className="w-[30%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="THUỘC TÍNH (ATTRIBUTE)"
                    options={attributeOptions}
                    selectedValues={attributeFilter}
                    onApply={setAttributeFilter}
                  />
                </th>

                {/* Cột 3: Trạng thái bắt buộc */}
                <th className="w-[20%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  YÊU CẦU NHẬP LIỆU
                </th>

                {/* Cột 4: Thao tác */}
                <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  THAO TÁC
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <TableLoading colSpan={4} />
              ) : data.length > 0 ? (
                data.map((item) => (
                  <tr
                    key={item.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    {/* TÊN DANH MỤC */}
                    <td className="py-3 px-6">
                      <div className="font-extrabold text-slate-800 text-[14px]">
                        {item.categoryName || (
                          <span className="text-slate-400 italic">Lỗi: Danh mục đã bị xóa</span>
                        )}
                      </div>
                    </td>

                    {/* TÊN THUỘC TÍNH */}
                    <td className="py-3 px-2">
                      <div className="inline-flex items-center px-2.5 py-1 rounded bg-indigo-50 text-indigo-700 border border-indigo-100 font-semibold text-[12px]">
                        {item.attributeDefinitionName || (
                          <span className="italic text-slate-400">---</span>
                        )}
                      </div>
                    </td>

                    {/* IS REQUIRED */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        {item.isRequired ? (
                          <span
                            className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-rose-50 text-rose-600 border border-rose-200/50 font-bold text-[11px]"
                            title="Bắt buộc nhập khi tạo Biến thể"
                          >
                            <ShieldAlert size={14} />
                            Bắt buộc
                          </span>
                        ) : (
                          <span
                            className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-slate-100 text-slate-500 border border-slate-200 font-medium text-[11px]"
                            title="Không bắt buộc nhập khi tạo Biến thể"
                          >
                            <ShieldCheck size={14} />
                            Tùy chọn
                          </span>
                        )}
                      </div>
                    </td>

                    {/* ACTIONS */}
                    <td className="py-3 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/category-attributes/edit/${item.id}`)}
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
                  colSpan={4}
                  message="Chưa có cấu hình nào khớp với tìm kiếm. Hãy thêm mới!"
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
        itemName={
          deletingRecord
            ? `thuộc tính ${deletingRecord.attributeDefinitionName} khỏi ${deletingRecord.categoryName}`
            : ''
        }
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default CategoryAttributeList;
