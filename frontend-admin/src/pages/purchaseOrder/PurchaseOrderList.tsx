import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingCart, Edit3, Trash2, Eye } from 'lucide-react';

// Common UI Components
import {
  ListPageContainer,
  ListHeader,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateCell,
  DateTimeCell,
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { Toast } from '../../components/commons/Toast';

// API & Types
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import { supplierApi } from '../../api/supplierApi';
import {
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseOrderStatusLabels,
  PurchaseOrderStatusColors,
} from '../../types/purchaseOrder';

const PurchaseOrderList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATE QUẢN LÝ DỮ LIỆU & PHÂN TRANG ---
  const [data, setData] = useState<PurchaseOrder[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const pageSize = 10;

  // --- STATE QUẢN LÝ BỘ LỌC (FILTERS) ---
  const [supplierFilter, setSupplierFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
  const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

  // --- OPTIONS CHO BỘ LỌC ---
  const [supplierOptions, setSupplierOptions] = useState<{ label: string; value: number }[]>([]);

  // 🔥 FIX: Lấy keys từ object Labels để tránh lỗi Reverse Mapping của Enum trong TypeScript
  const statusOptions = Object.keys(PurchaseOrderStatusLabels).map((key) => ({
    label: PurchaseOrderStatusLabels[Number(key) as PurchaseOrderStatus],
    value: Number(key),
  }));

  // --- STATE MODAL & TOAST ---
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [deletingRecord, setDeletingRecord] = useState<PurchaseOrder | null>(null);
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
    supplierApi
      .getAllList()
      .then((res) => setSupplierOptions(res.map((s) => ({ label: s.name, value: s.id }))))
      .catch(() => showToast('warning', 'Không tải được danh sách nhà cung cấp'));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Reset về trang 1 khi thay đổi bất kỳ bộ lọc nào
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, supplierFilter, statusFilter, fromDateFilter, toDateFilter]);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await purchaseOrderApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        supplierId: supplierFilter.length > 0 ? Number(supplierFilter[0]) : undefined,
        status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
        startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setData(response.items);
      setTotalPages(response.totalPages);
      setTotalItems(response.totalRecords);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU ĐƠN MUA HÀNG');
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, debouncedSearch, supplierFilter, statusFilter, fromDateFilter, toDateFilter]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const confirmDelete = async () => {
    if (!deletingRecord) return;
    try {
      await purchaseOrderApi.delete(deletingRecord.id);
      setIsModalOpen(false);
      fetchData();
      showToast('success', `Xóa thành công đơn mua hàng "${deletingRecord.orderCode}"`);
    } catch (error) {
      showToast('error', 'Lỗi khi thực hiện xóa dữ liệu');
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Đơn Mua Hàng"
        subtitle="Quản lý đơn đặt hàng từ nhà cung cấp"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/purchase-orders/create')}
        icon={ShoppingCart}
        searchPlaceholder="Tìm theo mã PO..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-250">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã PO
                </th>

                <th className="w-[18%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="NHÀ CUNG CẤP"
                    options={supplierOptions}
                    selectedValues={supplierFilter}
                    onApply={setSupplierFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY ĐẶT"
                      selectedDate={fromDateFilter}
                      onApply={setFromDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY GIAO DỰ KIẾN"
                      selectedDate={toDateFilter}
                      onApply={setToDateFilter}
                    />
                  </div>
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

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                  Tổng tiền
                </th>

                <th className="w-[9%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Người tạo
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
                    {/* CELL 1: MÃ PO */}
                    <td className="py-3 px-6">
                      <span className="inline-block px-2.5 py-1 bg-slate-100 text-slate-700 rounded-md font-bold text-[13px] border border-slate-200 shadow-sm">
                        {item.orderCode}
                      </span>
                    </td>

                    {/* CELL 2: NHÀ CUNG CẤP */}
                    <td className="py-3 px-2">
                      <span className="text-[13px] font-semibold text-slate-700">
                        {item.supplierName}
                      </span>
                    </td>

                    {/* CELL 3: NGÀY ĐẶT */}
                    <td className="py-3 px-2 text-center">
                      <DateCell isoString={item.orderDate} />
                    </td>

                    {/* CELL 4: NGÀY GIAO DỰ KIẾN */}
                    <td className="py-3 px-2 text-center">
                      <DateCell isoString={item.expectedDeliveryDate} />
                    </td>

                    {/* CELL 5: TRẠNG THÁI */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        <span
                          className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${PurchaseOrderStatusColors[item.status]}`}
                        >
                          {PurchaseOrderStatusLabels[item.status]}
                        </span>
                      </div>
                    </td>

                    {/* CELL 6: TỔNG TIỀN */}
                    <td className="py-3 px-2 text-right">
                      <span className="text-[14px] font-black text-slate-700">
                        {formatCurrency(item.totalAmount)}
                      </span>
                    </td>

                    {/* CELL 7: NGƯỜI TẠO */}
                    <td className="py-3 px-2">
                      <span className="text-[12px] font-medium text-slate-500 bg-slate-50 px-2 py-1 rounded-md border border-slate-100">
                        {item.createdByName || item.createdById}
                      </span>
                    </td>

                    {/* CELL 8: THAO TÁC */}
                    <td className="py-3 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/purchase-orders/${item.id}`)}
                          className="p-1.5 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors"
                          title="Xem chi tiết"
                        >
                          <Eye size={17} strokeWidth={2.5} />
                        </button>
                        {item.status === PurchaseOrderStatus.Draft && (
                          <>
                            <button
                              onClick={() => navigate(`/purchase-orders/edit/${item.id}`)}
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
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <TableEmpty
                  colSpan={8}
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
        itemName={deletingRecord?.orderCode || ''}
        onClose={() => setIsModalOpen(false)}
        onConfirm={confirmDelete}
      />
    </ListPageContainer>
  );
};

export default PurchaseOrderList;
