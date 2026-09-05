import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Edit3, Trash2, Box, Tag } from 'lucide-react';

// API & Types
import { productVariantApi } from '../../api/productVariantApi';
import { productApi } from '../../api/productApi';
import { ProductVariant } from '../../types/productVariant';

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

const ProductVariantList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<ProductVariant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [productFilter, setProductFilter] = useState<(string | number)[]>([]);
  const [createdAtFilter, setCreatedAtFilter] = useState<Date | null>(null);
  const [updatedAtFilter, setUpdatedAtFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const [productOptions, setProductOptions] = useState<{ label: string; value: number }[]>([]);

  const statusOptions = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 },
  ];

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<ProductVariant | null>(null);
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
    productApi
      .getAllList()
      .then((res) => setProductOptions(res.map((p: any) => ({ label: p.name, value: p.id }))))
      .catch(() => showToast('warning', 'Không tải được bộ lọc Sản phẩm'));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, productFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const response = await productVariantApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        productId: productFilter.length > 0 ? productFilter.join(',') : undefined,
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
  }, [currentPage, debouncedSearch, productFilter, statusFilter, createdAtFilter, updatedAtFilter]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await productVariantApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công biến thể "${deletingRecord.name}"`);
    } catch (error: any) {
      showToast('error', error?.response?.data?.message || 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const handleToggleActive = async (id: number, currentStatus: boolean) => {
    try {
      await productVariantApi.toggleActive(id);
      fetchData();
      showToast('success', `Đã ${currentStatus ? 'tạm khóa' : 'kích hoạt'} biến thể`);
    } catch (error) {
      showToast('error', 'Không thể thay đổi trạng thái biến thể');
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Danh Sách Biến Thể (SKU)"
        subtitle="Quản lý chi tiết từng quy cách, bao bì, đóng gói và đơn giá bán của sản phẩm"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/product-variants/create')}
        icon={Box}
        searchPlaceholder="Tìm kiếm theo mã SKU, tên biến thể..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[24%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Thông Tin Biến Thể
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="SẢN PHẨM GỐC"
                    options={productOptions}
                    selectedValues={productFilter}
                    onApply={setProductFilter}
                  />
                </th>

                <th className="w-[13%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Giá Mặc Định
                </th>

                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Tồn Kho
                </th>

                <th className="w-[13%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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

                <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <TableLoading colSpan={7} />
              ) : data.length > 0 ? (
                data.map((item) => {
                  const defaultPriceInfo =
                    item.prices?.find((p) => p.isDefault) || item.prices?.[0];
                  const isDiscounted =
                    defaultPriceInfo &&
                    defaultPriceInfo.promotionalPrice != null &&
                    defaultPriceInfo.promotionalPrice < defaultPriceInfo.price;

                  return (
                    <tr
                      key={item.id}
                      className="hover:bg-slate-50/80 transition-colors duration-200 group"
                    >
                      {/* CELL 1: THÔNG TIN BIẾN THỂ */}
                      <td className="py-3 px-6">
                        <div className="flex items-center gap-4">
                          <div className="w-12 h-12 rounded-lg border border-slate-200 shadow-sm flex items-center justify-center shrink-0 overflow-hidden bg-white">
                            {item.imagePath ? (
                              <img
                                src={item.imagePath}
                                alt={item.name}
                                className="w-full h-full object-cover"
                              />
                            ) : (
                              <Box size={24} className="text-slate-300" />
                            )}
                          </div>
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
                              {item.prices?.length > 1 && (
                                <span className="text-[10px] font-medium text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
                                  +{item.prices.length - 1} UoM
                                </span>
                              )}
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* CELL 2: SẢN PHẨM GỐC */}
                      <td className="py-3 px-2">
                        <span className="text-[12px] font-semibold text-slate-600 bg-slate-100 px-2 py-1 rounded-md line-clamp-2">
                          {item.productName}
                        </span>
                      </td>

                      {/* CELL 3: GIÁ BÁN MẶC ĐỊNH */}
                      <td className="py-3 px-2">
                        {defaultPriceInfo ? (
                          <div className="flex flex-col justify-center">
                            {isDiscounted ? (
                              <>
                                <div className="flex items-baseline gap-1.5">
                                  <span className="text-[15px] font-bold text-rose-600">
                                    {defaultPriceInfo.promotionalPrice?.toLocaleString('vi-VN')} ₫
                                  </span>
                                  <span className="text-[11px] text-slate-400 font-medium">
                                    / {defaultPriceInfo.uoMName}
                                  </span>
                                  <Tag size={12} className="text-rose-500 fill-rose-100 shrink-0" />
                                </div>
                                <span className="text-[11px] font-medium text-slate-400 line-through mt-0.5">
                                  {defaultPriceInfo.price.toLocaleString('vi-VN')} ₫
                                </span>
                              </>
                            ) : (
                              <div className="flex items-baseline gap-1.5">
                                <span className="text-[14px] font-bold text-slate-700">
                                  {defaultPriceInfo.price.toLocaleString('vi-VN')} ₫
                                </span>
                                <span className="text-[11px] text-slate-400 font-medium">
                                  / {defaultPriceInfo.uoMName}
                                </span>
                              </div>
                            )}
                          </div>
                        ) : (
                          <span className="text-xs italic text-amber-500 bg-amber-50 px-2 py-1 rounded border border-amber-200/50">
                            Chưa cài giá
                          </span>
                        )}
                      </td>

                      {/* CELL 4: TỒN KHO */}
                      <td className="py-3 px-2 text-center">
                        <span className="inline-flex px-2 py-1 rounded-md text-[13px] font-bold bg-slate-100 text-slate-600">
                          {item.inventoryGuideline.toLocaleString('vi-VN')}
                        </span>
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

                      {/* CELL 6: NGÀY TẠO */}
                      <td className="py-3 px-2 text-center">
                        <DateTimeCell isoString={item.createdAt} />
                      </td>

                      {/* CELL 7: THAO TÁC */}
                      <td className="py-3 px-6">
                        <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                          <button
                            onClick={() => navigate(`/product-variants/edit/${item.id}`)}
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
                  );
                })
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

export default ProductVariantList;
