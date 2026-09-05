import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { PackageCheck, Eye, Trash2 } from 'lucide-react';
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

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
  InventoryIssue,
  InventoryIssueStatus,
  InventoryIssueStatusLabels,
  InventoryIssueStatusColors,
} from '../../types/inventoryIssue';

const InventoryIssueList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES DỮ LIỆU ---
  const [issues, setIssues] = useState<InventoryIssue[]>([]);
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

  const statusOptions = Object.keys(InventoryIssueStatusLabels).map((key) => ({
    label: InventoryIssueStatusLabels[Number(key) as InventoryIssueStatus],
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

  // Debounce search
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
      const response = await inventoryIssueApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
        status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
        startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setIssues(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      console.error('Lỗi tải danh sách phiếu xuất kho:', error);
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU');
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
      await inventoryIssueApi.delete(deleteId);
      showToast('success', 'XÓA PHIẾU XUẤT KHO THÀNH CÔNG');
      setDeleteId(null);
      fetchData();
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Không thể xóa phiếu xuất kho!');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Phiếu Xuất Kho"
        subtitle="Quản lý xuất kho giao hàng, xuất chuyển kho & Đóng gói sản phẩm"
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/inventory-issues/create')}
        icon={PackageCheck}
        searchPlaceholder="Tìm kiếm theo mã phiếu, người nhận..."
      />

      <ListCard>
        <div className="overflow-x-auto flex-1 min-h-[400px] pb-24">
          <table className="w-full text-left border-collapse min-w-[950px]">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[14%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã Phiếu
                </th>
                <th className="w-[12%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Đơn Hàng Gốc
                </th>
                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHO XUẤT"
                    options={warehouseOptions}
                    selectedValues={warehouseFilter}
                    onApply={setWarehouseFilter}
                  />
                </th>
                <th className="w-[16%] py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Người Nhận
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY XUẤT"
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

                <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <TableLoading colSpan={8} />
              ) : issues.length === 0 ? (
                <TableEmpty colSpan={8} message="Không tìm thấy phiếu xuất kho nào phù hợp." />
              ) : (
                issues.map((issue) => (
                  <tr
                    key={issue.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    {/* CELL 1: MÃ PHIẾU */}
                    <td className="py-3.5 px-6">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-slate-100 text-slate-700 border border-slate-200 shadow-sm">
                        {issue.issueCode}
                      </span>
                    </td>

                    {/* CELL 2: ĐƠN HÀNG GỐC */}
                    <td className="px-4 py-3.5">
                      {issue.orderCode ? (
                        <span className="text-[13px] font-bold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded border border-indigo-100">
                          {issue.orderCode}
                        </span>
                      ) : (
                        <span className="text-[12px] italic text-slate-400">Xuất nội bộ</span>
                      )}
                    </td>

                    {/* CELL 3: KHO XUẤT */}
                    <td className="py-3 px-2">
                      <div className="text-[13px] font-bold text-slate-800">
                        {issue.warehouseName}
                      </div>
                    </td>

                    {/* CELL 4: NGƯỜI NHẬN */}
                    <td className="px-4 py-3.5 text-slate-700 text-sm">
                      <div className="font-semibold">{issue.receiverName || '---'}</div>
                      <div className="text-xs text-slate-400">{issue.receiverPhone}</div>
                    </td>

                    {/* CELL 5: NGÀY XUẤT */}
                    <td className="py-3 px-2 text-center">
                      <DateCell isoString={issue.issueDate} />
                    </td>

                    {/* CELL 6: THỜI GIAN LẬP */}
                    <td className="py-3 px-2 text-center">
                      <DateTimeCell isoString={issue.createdAt} />
                    </td>

                    {/* CELL 7: TRẠNG THÁI */}
                    <td className="px-4 py-3.5 text-center">
                      <div className="flex justify-center">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-bold border shadow-sm ${InventoryIssueStatusColors[issue.status]}`}
                        >
                          {InventoryIssueStatusLabels[issue.status]}
                        </span>
                      </div>
                    </td>

                    {/* CELL 9: THAO TÁC */}
                    <td className="px-4 py-3.5 text-center">
                      <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/inventory-issues/${issue.id}`)}
                          className="p-1.5 text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                          title="Xem chi tiết"
                        >
                          <Eye size={18} strokeWidth={2.5} />
                        </button>
                        {issue.status === InventoryIssueStatus.Pending && (
                          <button
                            onClick={() => {
                              setDeleteId(issue.id);
                              setDeleteCode(issue.issueCode);
                            }}
                            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                            title="Xóa phiếu chờ"
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
        title="Xóa Phiếu Xuất Kho"
        message={`Bạn có chắc chắn muốn xóa phiếu xuất kho "${deleteCode}" không? Thao tác này không thể hoàn tác.`}
      />
    </ListPageContainer>
  );
};

export default InventoryIssueList;
