import React, { useEffect, useState } from 'react';
import {
  Package,
  History,
  AlertTriangle,
  CheckCircle,
  Warehouse as WarehouseIcon,
  Search,
  Filter,
  ShieldAlert,
} from 'lucide-react';

// API & Types
import { inventoryApi } from '../../api/inventoryApi';
import { warehouseApi } from '../../api/warehouseApi';
import { Inventory } from '../../types/inventory';
import { Warehouse } from '../../types/warehouse';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { TabGroup, TabButton } from '../../components/commons/TabUI';
import {
  ListPageContainer,
  ListCard,
  TableLoading,
  TableEmpty,
  ListPagination,
} from '../../components/commons/ListUI';

type TabType = 'inventory' | 'history';

const InventoryDashboard: React.FC = () => {
  // --- STATES ---
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<number>(0);
  const [activeTab, setActiveTab] = useState<TabType>('inventory');

  const [data, setData] = useState<Inventory[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Pagination & Filters
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  // Quick Filters Nông Sản
  const [isExpiringSoon, setIsExpiringSoon] = useState(false);
  const [isOutOfStock, setIsOutOfStock] = useState(false);
  const pageSize = 10;

  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- EFFECT 1: Load danh sách Kho (Context) ---
  useEffect(() => {
    warehouseApi
      .getAllList()
      .then((res) => {
        setWarehouses(res);
        // Nếu User có quyền ở ít nhất 1 kho, tự động chọn kho đầu tiên làm mặc định
        if (res.length > 0) {
          setSelectedWarehouseId(res[0].id);
        }
      })
      .catch(() =>
        showToast('error', 'Không tải được danh sách Kho hàng. Kiểm tra lại phân quyền!')
      );
  }, []);

  // --- EFFECT 2: Debounce Search ---
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Reset page về 1 khi đổi bộ lọc
  useEffect(() => {
    setCurrentPage(1);
  }, [debouncedSearch, selectedWarehouseId, isExpiringSoon, isOutOfStock]);

  // --- EFFECT 3: Fetch Data ---
  const fetchData = async () => {
    if (selectedWarehouseId === 0) return; // Chưa chọn kho thì không fetch

    setIsLoading(true);
    try {
      const response = await inventoryApi.getAll({
        search: debouncedSearch,
        warehouseId: selectedWarehouseId,
        isExpiringSoon: isExpiringSoon || undefined,
        isOutOfStock: isOutOfStock || undefined,
        pageIndex: currentPage,
        pageSize: pageSize,
      });

      setData(response.items);
      setTotalPages(response.totalPages);
    } catch (error) {
      showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU TỒN KHO');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage, debouncedSearch, selectedWarehouseId, isExpiringSoon, isOutOfStock]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // Hàm format Date nhanh (DD/MM/YYYY)
  const formatDate = (isoString: string) => {
    if (!isoString) return '--';
    return new Date(isoString).toLocaleDateString('vi-VN');
  };

  // 🔥 LOGIC BÔI MÀU CẢNH BÁO NÔNG SẢN
  const renderExpiryBadge = (days: number) => {
    if (days < 0)
      return (
        <span className="bg-rose-100 text-rose-700 px-2 py-0.5 rounded text-[10px] font-bold border border-rose-200">
          ĐÃ HẾT HẠN
        </span>
      );
    if (days <= 3)
      return (
        <span className="bg-red-500 text-white px-2 py-0.5 rounded text-[10px] font-bold shadow-sm animate-pulse">
          BÁO ĐỘNG ({days} ngày)
        </span>
      );
    if (days <= 7)
      return (
        <span className="bg-amber-100 text-amber-700 px-2 py-0.5 rounded text-[10px] font-bold border border-amber-200">
          CẬN DATE ({days} ngày)
        </span>
      );
    return (
      <span className="bg-emerald-50 text-emerald-600 px-2 py-0.5 rounded text-[10px] font-bold border border-emerald-100">
        AN TOÀN
      </span>
    );
  };

  return (
    <ListPageContainer>
      <Toast {...toast} />

      {/* ================= HEADER & BỘ LỌC CONTEXT (CHỌN KHO) ================= */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 mb-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-800 flex items-center gap-3">
            <WarehouseIcon className="text-indigo-600" size={28} />
            Bảng Điều Khiển Tồn Kho
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Giám sát số lượng vật lý và chất lượng lô hàng theo thời gian thực.
          </p>
        </div>

        <div className="flex items-center gap-3 bg-slate-50 p-2 rounded-xl border border-slate-200 w-full md:w-auto">
          <span className="text-sm font-bold text-slate-600 pl-2 shrink-0">Chọn Kho Xem:</span>
          <select
            className="bg-white border border-slate-300 text-slate-800 text-sm font-bold rounded-lg focus:ring-indigo-500 focus:border-indigo-500 block w-full md:w-64 p-2.5 shadow-sm"
            value={selectedWarehouseId}
            onChange={(e) => setSelectedWarehouseId(Number(e.target.value))}
          >
            {warehouses.length === 0 ? (
              <option value={0}>-- Không có quyền truy cập kho --</option>
            ) : (
              warehouses.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))
            )}
          </select>
        </div>
      </div>

      {/* ================= TABS ĐIỀU HƯỚNG ================= */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={activeTab === 'inventory'}
            onClick={() => setActiveTab('inventory')}
            label="1. TỒN KHO HIỆN TẠI"
            icon={Package}
          />
          <TabButton
            active={activeTab === 'history'}
            onClick={() => setActiveTab('history')}
            label="2. LỊCH SỬ NHẬP/XUẤT (Giai đoạn 3)"
            icon={History}
          />
        </TabGroup>
      </div>

      {/* ================= TAB 1: DANH SÁCH TỒN KHO ================= */}
      <div
        className={activeTab === 'inventory' ? 'block animate-in fade-in duration-300' : 'hidden'}
      >
        <ListCard>
          {/* TOOLBAR TÌM KIẾM & LỌC NHANH */}
          <div className="p-4 border-b border-slate-100 flex flex-col md:flex-row gap-4 items-center justify-between bg-slate-50/50">
            <div className="relative w-full md:w-96">
              <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
                <Search className="w-4 h-4 text-slate-400" />
              </div>
              <input
                type="text"
                className="bg-white border border-slate-200 text-slate-900 text-sm rounded-lg focus:ring-indigo-500 focus:border-indigo-500 block w-full pl-10 p-2.5 shadow-sm"
                placeholder="Tìm Tên, Mã SKU, Mã Lô..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>

            <div className="flex gap-3 w-full md:w-auto">
              <button
                onClick={() => setIsExpiringSoon(!isExpiringSoon)}
                className={`flex items-center gap-2 px-4 py-2 text-sm font-bold rounded-lg border transition-all ${isExpiringSoon ? 'bg-amber-100 border-amber-300 text-amber-800 shadow-sm' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'}`}
              >
                <AlertTriangle
                  size={16}
                  className={isExpiringSoon ? 'text-amber-600' : 'text-slate-400'}
                />
                Hàng Sắp Hết Hạn
              </button>
              <button
                onClick={() => setIsOutOfStock(!isOutOfStock)}
                className={`flex items-center gap-2 px-4 py-2 text-sm font-bold rounded-lg border transition-all ${isOutOfStock ? 'bg-slate-700 border-slate-800 text-white shadow-sm' : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50'}`}
              >
                <Filter size={16} className={isOutOfStock ? 'text-white' : 'text-slate-400'} />
                Lọc Cạn Kho
              </button>
            </div>
          </div>

          {/* BẢNG DỮ LIỆU */}
          <div className="overflow-x-auto flex-1 pb-16">
            <table className="w-full text-left border-collapse min-w-250">
              <thead>
                <tr className="bg-slate-100/50 border-b border-slate-200">
                  <th className="py-4 px-6 text-xs font-bold text-slate-500 uppercase">
                    Thông Tin Mặt Hàng
                  </th>
                  <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase">
                    Chi Tiết Lô & HSD
                  </th>
                  <th className="py-4 px-4 text-xs font-bold text-emerald-600 uppercase text-right bg-emerald-50/50">
                    Khả Dụng
                  </th>
                  <th className="py-4 px-4 text-xs font-bold text-amber-600 uppercase text-right bg-amber-50/50">
                    Giữ Chỗ
                  </th>
                  <th className="py-4 px-4 text-xs font-bold text-rose-600 uppercase text-right bg-rose-50/50">
                    Lỗi/QC
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {isLoading ? (
                  <TableLoading colSpan={5} />
                ) : data.length > 0 ? (
                  data.map((item) => (
                    <tr key={item.id} className="hover:bg-indigo-50/30 transition-colors group">
                      {/* CỘT 1: HÀNG HÓA */}
                      <td className="py-4 px-6">
                        <div className="flex flex-col">
                          <span className="font-bold text-[14px] text-slate-800">
                            {item.variantName}
                          </span>
                          <div className="flex items-center gap-2 mt-1">
                            <span className="text-[11px] font-bold text-indigo-600 bg-indigo-50 px-1.5 py-0.5 rounded border border-indigo-100">
                              {item.variantCode}
                            </span>
                            <span className="text-[11px] font-medium text-slate-500">
                              ĐVT: <strong>{item.baseUoMName}</strong>
                            </span>
                          </div>
                        </div>
                      </td>

                      {/* CỘT 2: LÔ HÀNG & DATE */}
                      <td className="py-4 px-4">
                        <div className="flex flex-col gap-1.5">
                          <div className="flex items-center gap-2">
                            <span className="text-[12px] font-bold text-slate-700">
                              {item.batchCode}
                            </span>
                            {renderExpiryBadge(item.daysToExpiry)}
                          </div>
                          <div className="text-[11px] text-slate-500">
                            NSX: {formatDate(item.manufactureDate)} &nbsp;|&nbsp; HSD:{' '}
                            <strong className="text-slate-700">
                              {formatDate(item.expiryDate)}
                            </strong>
                          </div>
                          {item.supplierName && (
                            <div
                              className="text-[11px] text-slate-400 truncate w-48"
                              title={item.supplierName}
                            >
                              NCC: {item.supplierName}
                            </div>
                          )}
                        </div>
                      </td>

                      {/* CỘT 3: KHẢ DỤNG (XANH) */}
                      <td className="py-4 px-4 text-right bg-emerald-50/10 group-hover:bg-emerald-50/40">
                        <span
                          className={`text-[16px] font-black ${item.quantityAvailable > 0 ? 'text-emerald-600' : 'text-slate-300'}`}
                        >
                          {item.quantityAvailable.toLocaleString('vi-VN')}
                        </span>
                      </td>

                      {/* CỘT 4: GIỮ CHỖ (VÀNG) */}
                      <td className="py-4 px-4 text-right bg-amber-50/10 group-hover:bg-amber-50/40">
                        <span
                          className={`text-[15px] font-bold ${item.quantityReserved > 0 ? 'text-amber-600' : 'text-slate-300'}`}
                        >
                          {item.quantityReserved.toLocaleString('vi-VN')}
                        </span>
                      </td>

                      {/* CỘT 5: LỖI / QC (ĐỎ/CAM) */}
                      <td className="py-4 px-4 text-right bg-rose-50/10 group-hover:bg-rose-50/40">
                        <div className="flex flex-col items-end">
                          <span
                            className={`text-[14px] font-bold ${item.quantityDamaged > 0 ? 'text-rose-600' : 'text-slate-300'}`}
                            title="Hàng hỏng"
                          >
                            {item.quantityDamaged.toLocaleString('vi-VN')}
                          </span>
                          {item.quantityQC > 0 && (
                            <span
                              className="text-[11px] text-orange-500 font-medium mt-0.5"
                              title="Hàng chờ kiểm định (QC)"
                            >
                              + {item.quantityQC} QC
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <TableEmpty
                    colSpan={5}
                    message={
                      selectedWarehouseId === 0
                        ? 'Vui lòng chọn Kho hàng để xem dữ liệu.'
                        : 'Không tìm thấy dữ liệu tồn kho phù hợp.'
                    }
                  />
                )}
              </tbody>
            </table>
          </div>

          <ListPagination
            currentPage={currentPage}
            totalPages={totalPages}
            totalItems={data.length}
            onPageChange={setCurrentPage}
            isLoading={isLoading}
          />
        </ListCard>
      </div>

      {/* ================= TAB 2: LỊCH SỬ GIAO DỊCH (PLACEHOLDER) ================= */}
      <div className={activeTab === 'history' ? 'block animate-in fade-in duration-300' : 'hidden'}>
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-16 flex flex-col items-center justify-center text-center">
          <div className="w-20 h-20 bg-indigo-50 text-indigo-500 rounded-full flex items-center justify-center mb-6 shadow-inner border border-indigo-100">
            <History size={40} strokeWidth={2} />
          </div>
          <h2 className="text-2xl font-black text-slate-800 mb-2">
            Tính năng đang được phát triển
          </h2>
          <p className="text-slate-500 max-w-lg mx-auto leading-relaxed">
            Màn hình sao kê <strong>Lịch sử Nhập / Xuất kho</strong> (kèm chức năng bấm xem chi tiết
            lô hàng nhập trong ngày) sẽ được tích hợp ngay khi hoàn thành{' '}
            <strong>Phase 3 (Module Nhập Kho)</strong>.
          </p>
          <div className="mt-8 flex gap-3">
            <span className="px-4 py-2 bg-slate-100 text-slate-500 font-bold text-sm rounded-lg border border-slate-200">
              Loading Phase 3...
            </span>
          </div>
        </div>
      </div>
    </ListPageContainer>
  );
};

export default InventoryDashboard;
