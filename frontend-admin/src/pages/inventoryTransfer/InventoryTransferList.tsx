import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeftRight, Eye, Plus } from 'lucide-react';
import {
  ListPageContainer,
  ListHeader,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
  DateTimeCell,
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';

import { inventoryTransferApi } from '../../api/inventoryTransferApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
  InventoryTransfer,
  InventoryTransferStatus,
  InventoryTransferStatusLabels,
  InventoryTransferStatusColors,
} from '../../types/inventoryTransfer';

const InventoryTransferList: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES ---
  const [transfers, setTransfers] = useState<InventoryTransfer[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(0);
  const [totalItems, setTotalItems] = useState<number>(0);
  const pageSize = 10;

  // --- BỘ LỌC (FILTERS) ---
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [debouncedSearch, setDebouncedSearch] = useState<string>('');
  const [fromWhFilter, setFromWhFilter] = useState<(string | number)[]>([]);
  const [toWhFilter, setToWhFilter] = useState<(string | number)[]>([]);
  const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
  const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
  const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

  // --- OPTIONS ---
  const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

  // 🔥 Dùng Object.keys để lấy Enum an toàn, tránh lỗi Reverse Mapping
  const statusOptions = Object.keys(InventoryTransferStatusLabels).map((key) => ({
    label: InventoryTransferStatusLabels[Number(key) as InventoryTransferStatus],
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
    const loadWarehouses = async () => {
      try {
        // Đón đầu lỗi giống như form nhập kho lúc nãy
        const whRes = await warehouseApi.getAllList().catch(() => []);
        setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
      } catch (err) {
        console.error('Lỗi khi tải danh sách kho:', err);
      }
    };
    loadWarehouses();
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Đưa trang về 1 mỗi khi đổi filter
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, fromWhFilter, toWhFilter, statusFilter, fromDateFilter, toDateFilter]);

  // --- FETCH DATA ---
  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const response = await inventoryTransferApi.getAll({
        search: debouncedSearch,
        pageIndex: currentPage,
        pageSize: pageSize,
        fromWarehouseId: fromWhFilter.length > 0 ? Number(fromWhFilter[0]) : undefined,
        toWarehouseId: toWhFilter.length > 0 ? Number(toWhFilter[0]) : undefined,
        status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
        startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
        endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
      });

      setTransfers(response.items || []);
      setTotalPages(response.totalPages || 0);
      setTotalItems(response.totalRecords || 0);
    } catch (error) {
      console.error('Lỗi tải dữ liệu chuyển kho:', error);
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DANH SÁCH!');
      setTransfers([]);
    } finally {
      setLoading(false);
    }
  }, [
    debouncedSearch,
    currentPage,
    fromWhFilter,
    toWhFilter,
    statusFilter,
    fromDateFilter,
    toDateFilter,
  ]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return (
    <ListPageContainer>
      <Toast {...toast} />

      <ListHeader
        title="Phiếu Chuyển Kho"
        subtitle="Điều phối luân chuyển hàng hóa nội bộ (InTransit)"
        icon={ArrowLeftRight}
        searchPlaceholder="Tìm mã phiếu..."
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        onAdd={() => navigate('/inventory-transfers/create')}
      />

      <ListCard>
        {/* --- BẢNG DỮ LIỆU CHUẨN CONVENTION --- */}
        <div className="overflow-x-auto flex-1 min-h-100 pb-24">
          <table className="w-full text-left border-collapse min-w-250">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100">
                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                  Mã Phiếu
                </th>

                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHO XUẤT"
                    options={warehouseOptions}
                    selectedValues={fromWhFilter}
                    onApply={setFromWhFilter}
                  />
                </th>

                <th className="w-[16%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <CustomFilter
                    title="KHO NHẬN"
                    options={warehouseOptions}
                    selectedValues={toWhFilter}
                    onApply={setToWhFilter}
                  />
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="TỪ NGÀY"
                      selectedDate={fromDateFilter}
                      onApply={setFromDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomDateFilter
                      title="ĐẾN NGÀY"
                      selectedDate={toDateFilter}
                      onApply={setToDateFilter}
                    />
                  </div>
                </th>

                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                  <div className="flex justify-center">
                    <CustomFilter
                      title="TRẠNG THÁI"
                      options={statusOptions}
                      selectedValues={statusFilter}
                      onApply={setStatusFilter}
                    />
                  </div>
                </th>

                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider hidden lg:table-cell">
                  Người Lập
                </th>

                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                  Thao Tác
                </th>
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <TableLoading colSpan={8} />
              ) : transfers.length === 0 ? (
                <TableEmpty
                  colSpan={8}
                  message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc."
                />
              ) : (
                transfers.map((transfer) => (
                  <tr
                    key={transfer.id}
                    className="hover:bg-slate-50/80 transition-colors duration-200 group"
                  >
                    {/* CELL 1: MÃ PHIẾU */}
                    <td className="py-3 px-6">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-sm">
                        {transfer.transferCode}
                      </span>
                    </td>

                    {/* CELL 2: KHO XUẤT */}
                    <td className="py-3 px-2">
                      <div className="text-[13px] font-bold text-slate-800">
                        {transfer.fromWarehouseName}
                      </div>
                    </td>

                    {/* CELL 3: KHO NHẬN */}
                    <td className="py-3 px-2">
                      <div className="text-[13px] font-bold text-slate-800">
                        {transfer.toWarehouseName}
                      </div>
                    </td>

                    {/* CELL 4 & 5: NGÀY TẠO */}
                    <td className="py-3 px-2 text-center" colSpan={2}>
                      <DateTimeCell isoString={transfer.createdAt} />
                    </td>

                    {/* CELL 6: TRẠNG THÁI */}
                    <td className="py-3 px-2 text-center">
                      <div className="flex justify-center">
                        <span
                          className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${InventoryTransferStatusColors[transfer.status]}`}
                        >
                          {InventoryTransferStatusLabels[transfer.status]}
                        </span>
                      </div>
                    </td>

                    {/* CELL 7: NGƯỜI LẬP */}
                    <td className="py-3 px-2 hidden lg:table-cell">
                      <div className="text-[12px] text-slate-600 font-medium bg-slate-50 px-2 py-1 rounded-md border border-slate-100 inline-block">
                        {transfer.createdByName || '-'}
                      </div>
                    </td>

                    {/* CELL 8: THAO TÁC */}
                    <td className="py-3 px-6">
                      <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                        <button
                          onClick={() => navigate(`/inventory-transfers/${transfer.id}`)}
                          className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                          title="Xem chi tiết"
                        >
                          <Eye size={17} strokeWidth={2.5} />
                        </button>
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
    </ListPageContainer>
  );
};

export default InventoryTransferList;
