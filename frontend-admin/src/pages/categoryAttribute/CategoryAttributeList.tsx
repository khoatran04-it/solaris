import React, { useEffect, useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Settings2, Tag, Layers } from 'lucide-react';

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

interface GroupedCategoryRow {
  categoryId: number;
  categoryName: string;
  attributes: {
    id: number;
    attributeDefinitionId?: number;
    attributeDefinitionName?: string;
    isRequired: boolean;
  }[];
}

const CategoryAttributeList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU ---
  const [rawData, setRawData] = useState<CategoryAttribute[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
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
  const [deletingCategory, setDeletingCategory] = useState<{
    categoryId: number;
    categoryName: string;
    firstRecordId?: number;
  } | null>(null);

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
        pageIndex: 1,
        pageSize: 1000, // Lấy toàn bộ để nhóm theo danh mục
        categoryId: categoryFilter.length > 0 ? categoryFilter.join(',') : undefined,
        attributeDefinitionId: attributeFilter.length > 0 ? attributeFilter.join(',') : undefined,
      });

      setRawData(response.items || []);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch, categoryFilter, attributeFilter]);

  // --- NHÓM DỮ LIỆU THEO DANH MỤC (1 DÒNG / 1 DANH MỤC) ---
  const groupedData: GroupedCategoryRow[] = useMemo(() => {
    const map = new Map<number, GroupedCategoryRow>();

    rawData.forEach((item) => {
      if (!map.has(item.categoryId)) {
        map.set(item.categoryId, {
          categoryId: item.categoryId,
          categoryName: item.categoryName || `Danh mục #${item.categoryId}`,
          attributes: [],
        });
      }

      map.get(item.categoryId)!.attributes.push({
        id: item.id,
        attributeDefinitionId: item.attributeDefinitionId,
        attributeDefinitionName: item.attributeDefinitionName || `Thuộc tính #${item.attributeDefinitionId}`,
        isRequired: item.isRequired,
      });
    });

    return Array.from(map.values());
  }, [rawData]);

  // Phân trang trên groupedData
  const totalItems = groupedData.length;
  const totalPages = Math.ceil(totalItems / pageSize) || 1;
  const paginatedData = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    return groupedData.slice(startIndex, startIndex + pageSize);
  }, [groupedData, currentPage, pageSize]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingCategory) return;
    try {
      if (deletingCategory.firstRecordId) {
        // Hỗ trợ xóa qua delete API nếu có
        await categoryAttributeApi.delete(deletingCategory.firstRecordId);
      } else {
        await categoryAttributeApi.sync({
          categoryId: deletingCategory.categoryId,
          attributes: [],
        });
      }
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Đã gỡ tất cả thuộc tính khỏi danh mục!`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Cấu Hình Thuộc Tính Danh Mục"
        subtitle="Quản lý và gán các thuộc tính chất lượng (Brix, Vùng trồng, Tiêu chuẩn) cho từng danh mục nông sản"
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

                {/* Cột 2: Thuộc tính gắn kèm (Tags chung 1 hàng) */}
                <th className="w-[55%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="CÁC THUỘC TÍNH ÁP DỤNG (TAGS)"
                    options={attributeOptions}
                    selectedValues={attributeFilter}
                    onApply={setAttributeFilter}
                  />
                </th>

                {/* Cột 3: Thao tác */}
                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  THAO TÁC
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 text-sm">
              {isLoading ? (
                <TableLoading colSpan={3} />
              ) : paginatedData.length > 0 ? (
                paginatedData.map((group) => {
                  const visibleAttrs = group.attributes.slice(0, 4);
                  const extraCount = group.attributes.length - 4;
                  const extraAttrsText = group.attributes
                    .slice(4)
                    .map((a) => `${a.isRequired ? '*' : ''}${a.attributeDefinitionName}`)
                    .join(', ');

                  return (
                    <tr
                      key={group.categoryId}
                      className="hover:bg-slate-50/80 transition-colors duration-200 group"
                    >
                      {/* DANH MỤC */}
                      <td className="py-4 px-6 align-middle">
                        <div className="flex flex-col gap-1">
                          <span className="font-bold text-slate-900 text-sm">
                            {group.categoryName}
                          </span>
                          <span className="text-[11px] font-semibold text-amber-700 bg-amber-50 border border-amber-200/60 px-2.5 py-0.5 rounded-md w-fit inline-flex items-center gap-1">
                            <Layers size={12} className="text-amber-500" />
                            {group.attributes.length} thuộc tính
                          </span>
                        </div>
                      </td>

                      {/* DANH SÁCH THẺ THUỘC TÍNH (GOM 1 HÀNG) */}
                      <td className="py-4 px-4 align-middle">
                        <div className="flex flex-wrap items-center gap-2">
                          {visibleAttrs.map((attr) => (
                            <span
                              key={attr.id}
                              className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all ${
                                attr.isRequired
                                  ? 'bg-rose-50 text-rose-700 border-rose-200 shadow-2xs'
                                  : 'bg-slate-50 text-slate-700 border-slate-200'
                              }`}
                              title={
                                attr.isRequired
                                  ? 'Bắt buộc nhập khi tạo sản phẩm'
                                  : 'Tùy chọn nhập'
                              }
                            >
                              <Tag
                                size={12}
                                className={attr.isRequired ? 'text-rose-500' : 'text-slate-400'}
                              />
                              {attr.attributeDefinitionName}
                              {attr.isRequired && (
                                <span className="text-rose-600 font-black text-xs">*</span>
                              )}
                            </span>
                          ))}

                          {/* THẺ +N NẾU VƯỢT QUÁ */}
                          {extraCount > 0 && (
                            <span
                              className="inline-flex items-center px-2.5 py-1.5 rounded-xl text-xs font-bold bg-amber-100 text-amber-900 border border-amber-300/80 cursor-help hover:bg-amber-200 transition-colors"
                              title={`Các thuộc tính khác: ${extraAttrsText}`}
                            >
                              +{extraCount} thuộc tính
                            </span>
                          )}
                        </div>
                      </td>

                      {/* THAO TÁC */}
                      <td className="py-4 px-6 text-center align-middle">
                        <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                          <button
                            onClick={() =>
                              navigate(
                                `/category-attributes/create?categoryId=${group.categoryId}`
                              )
                            }
                            className="p-2 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-xl transition-colors cursor-pointer"
                            title="Chỉnh sửa ma trận thuộc tính của danh mục này"
                          >
                            <Edit3 size={17} strokeWidth={2.5} />
                          </button>
                          <button
                            onClick={() => {
                              setDeletingCategory({
                                categoryId: group.categoryId,
                                categoryName: group.categoryName,
                                firstRecordId: group.attributes[0]?.id,
                              });
                              setIsModalOpen(true);
                            }}
                            className="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-colors cursor-pointer"
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
                  colSpan={3}
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
          deletingCategory
            ? `toàn bộ thuộc tính của danh mục "${deletingCategory.categoryName}"`
            : ''
        }
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default CategoryAttributeList;
