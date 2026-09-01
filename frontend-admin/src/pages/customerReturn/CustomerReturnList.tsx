import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { RotateCcw, Eye, Trash2 } from 'lucide-react';
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
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';

import { customerReturnApi } from '../../api/customerReturnApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
  CustomerReturn,
  CustomerReturnStatus,
  CustomerReturnStatusLabels,
  CustomerReturnStatusColors,
} from '../../types/customerReturn';

const CustomerReturnList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES DỮ LIỆU ---
  const [returns, setReturns] = useState<CustomerReturn[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(0);
  const [totalItems, setTotalItems] = useState<number>(0);
  const pageSize = 10;

  // --- BỘ LỌC (FILTERS) ---
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [debouncedSearch, setDebouncedSearch] = useState<string>('');
  const [warehouseFilter, setWarehouseFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
  const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

  // --- DELETE MODAL ---
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [deleteCode, setDeleteCode] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);

  // --- OPTIONS CHO DROPDOWN ---
  const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

  const statusOptions = Object.keys(CustomerReturnStatusLabels).map((key) => ({
    label: CustomerReturnStatusLabels[Number(key) as CustomerReturnStatus],
    value: Number(key),
  }));

  // --- TOAST ---
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>(
    {
      show: false,
      type: 'success',
      message: '',
    }
  );

  const showToast = (type: 'success' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadOptions = async () => {
      try {
        const whRes = await warehouseApi.getAllList().catch(() => []);
        setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
      } catch (err) {
        console.error('Lỗi tải danh mục kho:', err);
      }
    };
    loadOptions();
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, warehouseFilter, statusFilter, fromDateFilter, toDateFilter]);

  // --- FETCH DATA ---
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const response = await customerReturnApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
        status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
        startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setReturns(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      console.error('Lỗi tải dữ liệu khách trả hàng:', error);
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DANH SÁCH!');
      setReturns([]);
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, currentPage, warehouseFilter, statusFilter, fromDateFilter, toDateFilter]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      setIsDeleting(true);
      await customerReturnApi.delete(deleteId);
      showToast('success', 'XÓA PHIẾU TRẢ HÀNG THÀNH CÔNG');
      setDeleteId(null);
      fetchData();
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Không thể xóa phiếu trả hàng!');
    } finally {
      setIsDeleting(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      amount || 0
    );
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Khách Hàng Trả Hàng (RMA)"
        subtitle="Tiếp nhận hàng hoàn trả từ khách, kiểm định phân loại chất lượng & Nhập lại tồn kho"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/customer-returns/create')}
        icon={RotateCcw}
        searchPlaceholder="Tìm kiếm theo mã phiếu, mã đơn hàng..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-[400px] pb-24">
          <table className="w-full text-left border-collapse min-w-[1050px]">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[4%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  #
                </th>
                <th className="w-[12%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Mã Phiếu
                </th>
                <th className="w-[12%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Đơn Hàng Gốc
                </th>
                <th className="w-[14%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Khách Hàng
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHO TIẾP NHẬN"
                    options={warehouseOptions}
                    selectedValues={warehouseFilter}
                    onApply={setWarehouseFilter}
                  />
                </th>

                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY TIẾP NHẬN"
                      selectedDate={fromDateFilter}
                      onApply={setFromDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="THỜI GIAN LẬP"
                      selectedDate={toDateFilter}
                      onApply={setToDateFilter}
                    />
                  </div>
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

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                  Tiền Hoàn
                </th>

                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 text-sm">
              {loading ? (
                <TableLoading colSpan={10} />
              ) : returns.length === 0 ? (
                <TableEmpty
                  colSpan={10}
                  message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc."
                />
              ) : (
                returns.map((ret, idx) => (
                  <tr
                    key={ret.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    {/* CELL 1: # */}
                    <td className="px-4 py-3.5 text-center text-slate-400 text-xs">
                      {(currentPage - 1) * pageSize + idx + 1}
                    </td>

                    {/* CELL 2: MÃ PHIẾU */}
                    <td className="px-4 py-3.5 font-bold text-indigo-600">{ret.returnCode}</td>

                    {/* CELL 3: ĐƠN HÀNG GỐC */}
                    <td className="px-4 py-3.5 font-medium text-slate-800">
                      <button
                        onClick={() => navigate(`/orders/${ret.orderId}`)}
                        className="text-indigo-600 hover:underline font-bold"
                      >
                        {ret.orderCode}
                      </button>
                    </td>

                    {/* CELL 4: KHÁCH HÀNG */}
                    <td className="px-4 py-3.5 font-medium text-slate-700">{ret.customerName}</td>

                    {/* CELL 5: KHO TIẾP NHẬN */}
                    <td className="px-4 py-3.5 text-slate-600">{ret.warehouseName}</td>

                    {/* CELL 6: NGÀY TIẾP NHẬN */}
                    <td className="py-3 px-2 text-center">
                      <DateCell isoString={ret.returnDate} />
                    </td>

                    {/* CELL 7: THỜI GIAN LẬP */}
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={ret.createdAt} />
                    </td>

                    {/* CELL 8: TRẠNG THÁI */}
                    <td className="px-4 py-3.5 text-center">
                      <div className="flex justify-center">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-bold border shadow-sm ${CustomerReturnStatusColors[ret.status]}`}
                        >
                          {CustomerReturnStatusLabels[ret.status]}
                        </span>
                      </div>
                    </td>

                    {/* CELL 9: TIỀN HOÀN LẠI */}
                    <td className="px-4 py-3.5 text-right font-bold text-rose-600">
                      {formatCurrency(ret.refundAmount)}
                    </td>

                    {/* CELL 10: THAO TÁC */}
                    <td className="px-4 py-3.5 text-center">
                      <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/customer-returns/${ret.id}`)}
                          className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                          title="Xem chi tiết"
                        >
                          <Eye size={18} strokeWidth={2.5} />
                        </button>
                        {(ret.status === CustomerReturnStatus.Pending ||
                          ret.status === CustomerReturnStatus.Rejected) && (
                          <button
                            onClick={() => {
                              setDeleteId(ret.id);
                              setDeleteCode(ret.returnCode);
                            }}
                            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                            title="Xóa phiếu trả hàng"
                          >
                            <Trash2 size={17} strokeWidth={2.5} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <ListPagination
          currentPage={currentPage}
          totalPages={totalPages}
          totalItems={totalItems}
          onPageChange={setCurrentPage}
          isLoading={loading}
        />
      </ListCard>

      <ConfirmDeleteModal
        isOpen={Boolean(deleteId)}
        onClose={() => setDeleteId(null)}
        onConfirm={handleDelete}
        loading={isDeleting}
        title="Xóa Phiếu Khách Trả Hàng"
        message={`Bạn có chắc chắn muốn xóa phiếu trả hàng "${deleteCode}" không? Thao tác này không thể hoàn tác.`}
      />
    </ListPageContainer>
  );
};

export default CustomerReturnList;
