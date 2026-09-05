import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClipboardList, Eye, Plus } from 'lucide-react';
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

import { inventoryAuditApi } from '../../api/inventoryAuditApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
  InventoryAudit,
  InventoryAuditStatus,
  InventoryAuditStatusLabels,
  InventoryAuditStatusColors,
  InventoryAuditType,
  InventoryAuditTypeLabels,
} from '../../types/inventoryAudit';

const InventoryAuditList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES ---
  const [audits, setAudits] = useState<InventoryAudit[]>([]);
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
  const [typeFilter, setTypeFilter] = useState<(string | number)[]>([]);
  const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
  const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

  // --- OPTIONS ---
  const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

  const statusOptions = Object.keys(InventoryAuditStatusLabels).map((key) => ({
    label: InventoryAuditStatusLabels[Number(key) as InventoryAuditStatus],
    value: Number(key),
  }));

  const typeOptions = Object.keys(InventoryAuditTypeLabels).map((key) => ({
    label: InventoryAuditTypeLabels[Number(key) as InventoryAuditType],
    value: Number(key),
  }));

  // --- TOAST ---
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadOptions = async () => {
      try {
        // Fail-safe
        const whRes = await warehouseApi.getAllList().catch(() => []);
        setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
      } catch (err) {
        console.error('Error loading warehouses:', err);
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

  // Reset về trang 1 khi đổi filter
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, warehouseFilter, statusFilter, typeFilter, fromDateFilter, toDateFilter]);

  // --- FETCH DATA ---
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const response = await inventoryAuditApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
        status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
        auditType: typeFilter.length > 0 ? Number(typeFilter[0]) : undefined,
        startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setAudits(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      console.error('Error fetching inventory audits:', error);
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DANH SÁCH KIỂM KÊ!');
      setAudits([]);
    } finally {
      setLoading(false);
    }
  }, [
    debouncedSearch,
    currentPage,
    warehouseFilter,
    statusFilter,
    typeFilter,
    fromDateFilter,
    toDateFilter,
  ]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      amount || 0
    );
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Kiểm Kê Kho Hàng (Stocktake)"
        subtitle="Đối chiếu số liệu sổ sách với thực tế vật lý & Xử lý chênh lệch"
        icon={ClipboardList}
        searchPlaceholder="Tìm mã phiếu kiểm kê..."
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/inventory-audits/create')}
      />

      <ListCard>
        {/* --- BẢNG DỮ LIỆU CHUẨN CONVENTION --- */}
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-325">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã Kiểm Kê
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHO HÀNG"
                    options={warehouseOptions}
                    selectedValues={warehouseFilter}
                    onApply={setWarehouseFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="HÌNH THỨC"
                    options={typeOptions}
                    selectedValues={typeFilter}
                    onApply={setTypeFilter}
                  />
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="NGÀY KIỂM KÊ"
                      selectedDate={fromDateFilter}
                      onApply={setFromDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
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

                <th className="w-[8%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                  Lệch SL
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">
                  Giá Trị Lệch
                </th>

                <th className="w-[8%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider hidden lg:table-cell text-center">
                  Người Lập
                </th>

                <th className="w-[6%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 text-sm">
              {loading ? (
                <TableLoading colSpan={10} />
              ) : audits.length === 0 ? (
                <TableEmpty
                  colSpan={10}
                  message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc."
                />
              ) : (
                audits.map((audit) => {
                  const isNegative = audit.totalVarianceQty < 0;
                  const isPositive = audit.totalVarianceQty > 0;

                  return (
                    <tr
                      key={audit.id}
                      className="hover:bg-slate-50/80 transition-colors duration-200 group"
                    >
                      {/* CELL 1: MÃ KIỂM KÊ */}
                      <td className="py-3 px-6">
                        <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-sm">
                          {audit.auditCode}
                        </span>
                      </td>

                      {/* CELL 2: KHO HÀNG */}
                      <td className="py-3 px-2">
                        <div className="text-[13px] font-bold text-slate-800">
                          {audit.warehouseName}
                        </div>
                      </td>

                      {/* CELL 3: HÌNH THỨC */}
                      <td className="py-3 px-2">
                        <span className="text-[13px] font-medium text-slate-700 bg-slate-100 px-2 py-1 rounded-md border border-slate-200">
                          {InventoryAuditTypeLabels[audit.auditType]}
                        </span>
                      </td>

                      {/* CELL 4: NGÀY KIỂM KÊ */}
                      <td className="py-3 px-2 text-center">
                        <DateCell isoString={audit.auditDate} />
                      </td>

                      {/* CELL 5: THỜI GIAN LẬP */}
                      <td className="py-3 px-2 text-center">
                        <DateTimeCell isoString={audit.completedAt || audit.createdAt} />
                      </td>

                      {/* CELL 6: TRẠNG THÁI */}
                      <td className="py-3 px-2 text-center">
                        <div className="flex justify-center">
                          <span
                            className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${InventoryAuditStatusColors[audit.status]}`}
                          >
                            {InventoryAuditStatusLabels[audit.status]}
                          </span>
                        </div>
                      </td>

                      {/* CELL 7: LỆCH SL */}
                      <td className="py-3 px-2 text-right">
                        <span
                          className={`text-[14px] font-black ${isNegative ? 'text-rose-600' : isPositive ? 'text-emerald-600' : 'text-slate-500'}`}
                        >
                          {audit.totalVarianceQty > 0
                            ? `+${audit.totalVarianceQty}`
                            : audit.totalVarianceQty}
                        </span>
                      </td>

                      {/* CELL 8: GIÁ TRỊ LỆCH */}
                      <td className="py-3 px-2 text-right">
                        <span
                          className={`text-[14px] font-black ${isNegative ? 'text-rose-600' : isPositive ? 'text-emerald-600' : 'text-slate-500'}`}
                        >
                          {audit.totalVarianceAmount > 0 ? '+' : ''}
                          {formatCurrency(audit.totalVarianceAmount)}
                        </span>
                      </td>

                      {/* CELL 9: NGƯỜI LẬP */}
                      <td className="py-3 px-2 text-center hidden lg:table-cell">
                        <div
                          className="text-[12px] text-slate-600 font-medium truncate max-w-30"
                          title={audit.auditorName}
                        >
                          {audit.auditorName}
                        </div>
                      </td>

                      {/* CELL 10: THAO TÁC */}
                      <td className="py-3 px-6">
                        <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                          <button
                            onClick={() => navigate(`/inventory-audits/${audit.id}`)}
                            className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                            title="Xem chi tiết"
                          >
                            <Eye size={17} strokeWidth={2.5} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
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
    </ListPageContainer>
  );
};

export default InventoryAuditList;
