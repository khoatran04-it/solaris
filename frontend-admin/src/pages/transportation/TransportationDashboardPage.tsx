import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { vehicleApi } from '../../api/vehicleApi';
import {
  TransportationDashboardStats,
  DeliveryTrip,
  DeliveryVehicle,
  TransferWaitingDispatch,
  OrderWaitingDispatch,
} from '../../types/vehicle';
import { KpiMetricCard, DashCard, fmtVnd } from '../../components/dashboard/DashboardCharts';
import { Toast } from '../../components/commons/Toast';
import { StatusBadge } from '../../components/commons/Badge';
import { FormLabel, FormSelect } from '../../components/commons/FormUI';

export default function TransportationDashboardPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab =
    searchParams.get('tab') === 'b2b' ? 'b2b' : searchParams.get('tab') === 'rma' ? 'rma' : 'b2c';
  const highlightOrderId = searchParams.get('highlightOrderId');
  const highlightReturnId = searchParams.get('highlightReturnId');

  const handleTabChange = (tab: 'b2c' | 'b2b' | 'rma') => {
    setSearchParams({ tab });
  };

  // --- STATES ---
  const [stats, setStats] = useState<TransportationDashboardStats | null>(null);
  const [vehicles, setVehicles] = useState<DeliveryVehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Filter States for B2C Order Pooling
  const [filterWarehouseId, setFilterWarehouseId] = useState<number | 'all'>('all');
  const [filterProvince, setFilterProvince] = useState<string>('all');
  const [filterDistrict, setFilterDistrict] = useState<string>('all');

  // Multi-select for Batch Dispatch (B2C)
  const [selectedOrderIds, setSelectedOrderIds] = useState<number[]>([]);
  const [batchModalOpen, setBatchModalOpen] = useState(false);
  const [selectedVehicleId, setSelectedVehicleId] = useState<number>(0);
  const [batchNote, setBatchNote] = useState('');
  const [dispatchLoading, setDispatchLoading] = useState(false);

  // B2B Transfer Dispatch Modal
  const [b2bModalOpen, setB2bModalOpen] = useState(false);
  const [selectedTransfer, setSelectedTransfer] = useState<TransferWaitingDispatch | null>(null);
  const [selectedB2bVehicleId, setSelectedB2bVehicleId] = useState<number>(0);
  const [b2bNote, setB2bNote] = useState('');
  const [b2bDispatchLoading, setB2bDispatchLoading] = useState(false);

  // Tab 3: RMA Return Dispatch Modal
  const [rmaModalOpen, setRmaModalOpen] = useState(false);
  const [selectedReturn, setSelectedReturn] = useState<any | null>(null);
  const [selectedRmaVehicleId, setSelectedRmaVehicleId] = useState<number>(0);
  const [rmaNote, setRmaNote] = useState('');
  const [rmaDispatchLoading, setRmaDispatchLoading] = useState(false);

  // Refusal Modal (Thay thế window.prompt chuẩn Theme Solaris)
  const [refusalModalOpen, setRefusalModalOpen] = useState(false);
  const [refusalTarget, setRefusalTarget] = useState<{
    tripId: number;
    orderId: number;
    orderCode: string;
  } | null>(null);
  const [refusalReason, setRefusalReason] = useState('Khách đổi ý không muốn nhận');
  const [customRefusalNote, setCustomRefusalNote] = useState('');
  const [refusalLoading, setRefusalLoading] = useState(false);

  // Trip Detail / Action Modal
  const [selectedTrip, setSelectedTrip] = useState<DeliveryTrip | null>(null);
  const [tripModalOpen, setTripModalOpen] = useState(false);

  // Toast
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

  const loadData = useCallback(async () => {
    try {
      setRefreshing(true);
      const [statsData, vehiclesData] = await Promise.all([
        vehicleApi.getDashboardStats(),
        vehicleApi.getAllVehicles(),
      ]);
      setStats(statsData);
      setVehicles(vehiclesData);
    } catch (err) {
      console.error(err);
      showToast('error', 'Lỗi khi tải dữ liệu trung tâm vận tải');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 10000); // Auto-refresh mỗi 10s
    return () => clearInterval(interval);
  }, [loadData]);

  // Live Tracking Realtime khi mở Modal chi tiết chuyến xe
  useEffect(() => {
    if (!tripModalOpen || !selectedTrip) return;
    const interval = setInterval(async () => {
      try {
        const full = await vehicleApi.getTripById(selectedTrip.id);
        if (full) setSelectedTrip(full);
      } catch (e) {
        // ignore
      }
    }, 4000);
    return () => clearInterval(interval);
  }, [tripModalOpen, selectedTrip?.id]);

  // Handle URL highlightOrderId param
  useEffect(() => {
    if (highlightOrderId && stats?.ordersWaitingDispatch) {
      const orderIdNum = Number(highlightOrderId);
      const target = stats.ordersWaitingDispatch.find((o) => o.orderId === orderIdNum);
      if (target) {
        setSelectedOrderIds((prev) => (prev.includes(orderIdNum) ? prev : [...prev, orderIdNum]));
        if (target.warehouseId) setFilterWarehouseId(target.warehouseId);
        if (target.province) setFilterProvince(target.province.trim());
        if (target.district) setFilterDistrict(target.district.trim());
      }
    }
  }, [highlightOrderId, stats]);

  // --- DYNAMIC FILTER OPTIONS FOR B2C ---
  const waitingOrders = useMemo(() => stats?.ordersWaitingDispatch || [], [stats]);

  const warehouseOptions = useMemo(() => {
    const map = new Map<number, string>();
    waitingOrders.forEach((o) => {
      if (o.warehouseId && o.warehouseName) {
        map.set(o.warehouseId, o.warehouseName);
      }
    });
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [waitingOrders]);

  const provinceOptions = useMemo(() => {
    const set = new Set<string>();
    waitingOrders.forEach((o) => {
      if (filterWarehouseId === 'all' || o.warehouseId === filterWarehouseId) {
        if (o.province && o.province.trim()) {
          set.add(o.province.trim());
        }
      }
    });
    return Array.from(set).sort();
  }, [waitingOrders, filterWarehouseId]);

  const districtCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    waitingOrders.forEach((o) => {
      if (filterWarehouseId !== 'all' && o.warehouseId !== filterWarehouseId) return;
      if (filterProvince !== 'all' && o.province?.trim() !== filterProvince) return;
      const d = o.district?.trim();
      if (d) {
        counts[d] = (counts[d] || 0) + 1;
      }
    });
    return counts;
  }, [waitingOrders, filterWarehouseId, filterProvince]);

  const districtOptions = useMemo(() => Object.keys(districtCounts).sort(), [districtCounts]);

  const filteredWaitingOrders = useMemo(() => {
    return waitingOrders.filter((order) => {
      if (filterWarehouseId !== 'all' && order.warehouseId !== filterWarehouseId) return false;
      if (filterProvince !== 'all' && order.province?.trim() !== filterProvince) return false;
      if (filterDistrict !== 'all' && order.district?.trim() !== filterDistrict) return false;
      return true;
    });
  }, [waitingOrders, filterWarehouseId, filterProvince, filterDistrict]);

  // --- HANDLERS ---
  const toggleSelectOrder = (id: number) => {
    setSelectedOrderIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const selectAllFilteredOrders = () => {
    const filteredIds = filteredWaitingOrders.map((o) => o.orderId);
    const allSelected =
      filteredIds.length > 0 && filteredIds.every((id) => selectedOrderIds.includes(id));
    if (allSelected) {
      setSelectedOrderIds((prev) => prev.filter((id) => !filteredIds.includes(id)));
    } else {
      setSelectedOrderIds((prev) => Array.from(new Set([...prev, ...filteredIds])));
    }
  };

  const selectAllInDistrict = (districtName: string) => {
    const idsInDistrict = waitingOrders
      .filter((o) => o.district?.trim() === districtName)
      .map((o) => o.orderId);
    setSelectedOrderIds((prev) => Array.from(new Set([...prev, ...idsInDistrict])));
    showToast('success', `Đã chọn tất cả ${idsInDistrict.length} đơn tại ${districtName}`);
  };

  const handleCreateBatchTrip = async () => {
    if (selectedOrderIds.length === 0) {
      showToast('warning', 'Vui lòng chọn ít nhất 1 đơn hàng để tạo chuyến');
      return;
    }
    if (!selectedVehicleId) {
      showToast('warning', 'Vui lòng chọn phương tiện giao hàng');
      return;
    }

    try {
      setDispatchLoading(true);
      const vehicle = vehicles.find((v) => v.id === selectedVehicleId);
      const homeWarehouseId = vehicle?.homeWarehouseId || 1;

      await vehicleApi.createTrip({
        vehicleId: selectedVehicleId,
        warehouseId: homeWarehouseId,
        tripType: 'B2C_Delivery',
        note: batchNote || `Gom ${selectedOrderIds.length} đơn nội thành`,
        orderIds: selectedOrderIds,
      });

      showToast('success', `Đã khởi tạo chuyến xe mới cho ${selectedOrderIds.length} đơn hàng`);
      setBatchModalOpen(false);
      setSelectedOrderIds([]);
      setBatchNote('');
      setSelectedVehicleId(0);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi tạo chuyến xe gom đơn');
    } finally {
      setDispatchLoading(false);
    }
  };

  const handleCreateB2bTrip = async () => {
    if (!selectedTransfer) return;
    if (!selectedB2bVehicleId) {
      showToast('warning', 'Vui lòng chọn xe tải lạnh cho chuyến chuyển kho');
      return;
    }

    try {
      setB2bDispatchLoading(true);
      await vehicleApi.createTrip({
        vehicleId: selectedB2bVehicleId,
        warehouseId: selectedTransfer.fromWarehouseId,
        tripType: 'B2B_Transfer',
        inventoryTransferId: selectedTransfer.transferId,
        note:
          b2bNote ||
          `Chuyển kho ${selectedTransfer.transferCode} từ ${selectedTransfer.fromWarehouseName} đến ${selectedTransfer.toWarehouseName}`,
        orderIds: [],
      });

      showToast(
        'success',
        `ĐÃ XUẤT BẾN THÀNH CÔNG! Phiếu chuyển ${selectedTransfer.transferCode} chuyển sang Đang vận chuyển (In-Transit).`
      );
      setB2bModalOpen(false);
      setSelectedTransfer(null);
      setSelectedB2bVehicleId(0);
      setB2bNote('');
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi tạo chuyến chuyển kho B2B');
    } finally {
      setB2bDispatchLoading(false);
    }
  };

  const handleStartTrip = async (tripId: number) => {
    try {
      await vehicleApi.startTrip(tripId);
      showToast('success', 'Chuyến xe đã xuất phát (Đang lăn bánh)');
      loadData();
      if (selectedTrip && selectedTrip.id === tripId) {
        const updated = await vehicleApi.getTripById(tripId);
        setSelectedTrip(updated);
      }
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi bắt đầu chuyến xe');
    }
  };

  const handleMarkOrderDelivered = async (tripId: number, orderId: number) => {
    try {
      const updated = await vehicleApi.markOrderDelivered(tripId, orderId, 'Giao thành công');
      showToast('success', 'Đã ghi nhận giao đơn hàng thành công');
      setSelectedTrip(updated);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi cập nhật trạng thái đơn');
    }
  };

  const openRefusalModal = (tripId: number, orderId: number, orderCode: string) => {
    setRefusalTarget({ tripId, orderId, orderCode });
    setRefusalReason('Khách đổi ý không muốn nhận');
    setCustomRefusalNote('');
    setRefusalModalOpen(true);
  };

  const handleConfirmRefusal = async () => {
    if (!refusalTarget) return;
    const finalReason = customRefusalNote.trim()
      ? `${refusalReason}: ${customRefusalNote.trim()}`
      : refusalReason;

    try {
      setRefusalLoading(true);
      const updated = await vehicleApi.markOrderFailed(
        refusalTarget.tripId,
        refusalTarget.orderId,
        finalReason
      );
      showToast('warning', 'Đã ghi nhận khách từ chối nhận hàng (Hàng sẽ theo xe hoàn về kho)');
      setSelectedTrip(updated);
      setRefusalModalOpen(false);
      setRefusalTarget(null);
      setCustomRefusalNote('');
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi cập nhật trạng thái đơn');
    } finally {
      setRefusalLoading(false);
    }
  };

  const handleDispatchReturn = async () => {
    if (!selectedReturn || !selectedRmaVehicleId) {
      showToast('error', 'Vui lòng chọn phương tiện để thu hồi đơn hàng');
      return;
    }

    try {
      setRmaDispatchLoading(true);
      await vehicleApi.createTrip({
        vehicleId: selectedRmaVehicleId,
        warehouseId: selectedReturn.warehouseId,
        tripType: 'B2C_Return',
        note:
          rmaNote.trim() ||
          `Thu hồi đơn trả ${selectedReturn.returnCode} cho khách ${selectedReturn.customerName}`,
        customerReturnIds: [selectedReturn.returnId],
        orderIds: [],
      });

      showToast(
        'success',
        `ĐÃ ĐIỀU PHỐI XE THU HỒI! Phiếu trả ${selectedReturn.returnCode} chuyển sang Đang thu hồi.`
      );
      setRmaModalOpen(false);
      setSelectedReturn(null);
      setSelectedRmaVehicleId(0);
      setRmaNote('');
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi điều phối xe thu hồi');
    } finally {
      setRmaDispatchLoading(false);
    }
  };

  const handleCompleteTrip = async (tripId: number) => {
    try {
      await vehicleApi.completeTrip(tripId);
      showToast('success', 'Chuyến xe đã hoàn tất! Phương tiện đã sẵn sàng cho chuyến tiếp theo');
      setTripModalOpen(false);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi kết thúc chuyến');
    }
  };

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        <Toast {...toast} />

        {/* TOP HEADER */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs">
          <div>
            <div className="flex items-center gap-3">
              <span className="px-2.5 py-1 bg-amber-50 text-amber-800 rounded-lg border border-amber-200 text-xs font-black tracking-wide">
                TMS
              </span>
              <div>
                <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight">
                  Trung Tâm Điều Phối & Quản Lý Vận Tải
                </h1>
                <p className="text-slate-500 text-xs mt-0.5 font-medium">
                  Điều phối Last-mile xe máy thùng lạnh nội thành & Giám sát Chuyển kho liên chi
                  nhánh B2B
                </p>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/transportation/vehicles')}
              className="px-4 py-2.5 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 rounded-xl text-xs font-bold transition-all shadow-xs cursor-pointer"
            >
              Danh Sách Đội Xe ({vehicles.length})
            </button>
            <button
              onClick={loadData}
              disabled={refreshing}
              className="px-4 py-2.5 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 rounded-xl text-xs font-bold transition-all shadow-xs disabled:opacity-50 cursor-pointer"
            >
              {refreshing ? 'Đang Tải...' : 'Làm Mới Dữ Liệu'}
            </button>
          </div>
        </div>

        {/* 4 TOP KPI CARDS */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <KpiMetricCard
            title="Xe Máy Thùng Lạnh Sẵn Sàng"
            value={stats ? `${stats.availableBikes} / ${stats.totalBikes}` : '...'}
            subtitle="Xe rảnh chờ nhận đơn nội thành"
            badge="Sẵn sàng"
            badgePositive={true}
          />
          <KpiMetricCard
            title="Xe Tải Lạnh Đang Chạy"
            value={stats ? `${stats.activeTrucks} / ${stats.totalTrucks}` : '...'}
            subtitle="Vận chuyển chuyển kho B2B"
            badge="Vận hành"
            badgePositive={true}
          />
          <KpiMetricCard
            title="Đơn Chuỗi Lạnh Chờ Xe"
            value={stats ? String(stats.pendingColdChainOrders) : '...'}
            subtitle="Cần điều phối giao nhanh"
            badge="Chờ điều phối"
            badgePositive={false}
          />
          <KpiMetricCard
            title="Chuyến Xe Đang Lăn Bánh"
            value={stats ? String(stats.activeTripsCount) : '...'}
            subtitle="Chuyến đang giao hoặc chuẩn bị"
            badge="Đang chạy"
            badgePositive={true}
          />
        </div>

        {/* MAIN CONTENT: 2 COLUMNS */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* CỘT TRÁI (7 COLS): HÀNG ĐỢI ĐIỀU PHỐI & GOM ĐƠN */}
          <div className="lg:col-span-7 space-y-4">
            <DashCard
              title="Hàng Đợi Điều Phối Vận Tải (Dispatch Queue)"
              subtitle="Chỉ hiển thị đơn đã hoàn tất phiếu xuất kho. Gom đơn theo cụm Quận/Huyện để gán xe máy."
              action={
                activeTab === 'b2c' &&
                selectedOrderIds.length > 0 && (
                  <button
                    onClick={() => setBatchModalOpen(true)}
                    className="bg-amber-400 hover:bg-amber-500 text-slate-900 rounded-xl px-4 py-2 text-xs font-black transition-all shadow-xs cursor-pointer"
                  >
                    Tạo Chuyến ({selectedOrderIds.length} Đơn)
                  </button>
                )
              }
            >
              {/* TABS CHUYỂN ĐỔI B2C / B2B / RMA */}
              <div className="flex items-center border-b border-slate-200 mb-4 overflow-x-auto">
                <button
                  type="button"
                  onClick={() => handleTabChange('b2c')}
                  className={`px-5 py-2.5 text-xs font-bold border-b-2 transition-all cursor-pointer shrink-0 ${
                    activeTab === 'b2c'
                      ? 'border-amber-400 text-slate-900 bg-amber-50/40 font-extrabold'
                      : 'border-transparent text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Giao Đơn B2C ({stats?.ordersWaitingDispatch?.length || 0})
                </button>
                <button
                  type="button"
                  onClick={() => handleTabChange('b2b')}
                  className={`px-5 py-2.5 text-xs font-bold border-b-2 transition-all cursor-pointer shrink-0 ${
                    activeTab === 'b2b'
                      ? 'border-amber-400 text-slate-900 bg-amber-50/40 font-extrabold'
                      : 'border-transparent text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Chuyển Kho B2B ({stats?.transfersWaitingDispatch?.length || 0})
                </button>
                <button
                  type="button"
                  onClick={() => handleTabChange('rma')}
                  className={`px-5 py-2.5 text-xs font-bold border-b-2 transition-all cursor-pointer shrink-0 ${
                    activeTab === 'rma'
                      ? 'border-amber-400 text-slate-900 bg-amber-50/40 font-extrabold'
                      : 'border-transparent text-slate-500 hover:text-slate-800'
                  }`}
                >
                  Thu Hồi Đơn Trả (RMA) ({stats?.returnsWaitingDispatch?.length || 0})
                </button>
              </div>

              {/* TAB 1: GIAO ĐƠN B2C */}
              {activeTab === 'b2c' &&
                (loading ? (
                  <div className="p-12 text-center text-slate-400 text-xs flex flex-col items-center justify-center gap-3">
                    <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
                    <span>Đang tải danh sách đơn...</span>
                  </div>
                ) : !waitingOrders.length ? (
                  <div className="py-14 text-center text-slate-400 text-xs font-medium">
                    Hiện không có đơn hàng nào đang chờ điều phối xe (Lưu ý: Chỉ đơn đã hoàn tất
                    phiếu xuất kho mới hiển thị ở đây).
                  </div>
                ) : (
                  <div className="space-y-3">
                    {/* BỘ LỌC GOM ĐƠN 2 CẤP THEO TỈNH -> QUẬN */}
                    <div className="p-3.5 bg-slate-50/90 rounded-2xl border border-slate-200 space-y-2.5">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <span className="text-[11px] font-extrabold uppercase tracking-wider text-slate-700">
                            Bộ Lọc Gom Đơn Theo Địa Bàn:
                          </span>
                          <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-100 text-amber-900 border border-amber-200">
                            {filteredWaitingOrders.length} / {waitingOrders.length} đơn hiển thị
                          </span>
                        </div>

                        {(filterWarehouseId !== 'all' ||
                          filterProvince !== 'all' ||
                          filterDistrict !== 'all') && (
                          <button
                            type="button"
                            onClick={() => {
                              setFilterWarehouseId('all');
                              setFilterProvince('all');
                              setFilterDistrict('all');
                            }}
                            className="text-[11px] font-bold text-rose-600 hover:text-rose-800 transition-colors cursor-pointer"
                          >
                            Đặt lại bộ lọc
                          </button>
                        )}
                      </div>

                      <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
                        {/* Dropdown 1: Kho Xuất Hàng */}
                        <div>
                          <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                            Kho xuất hàng
                          </label>
                          <select
                            value={filterWarehouseId}
                            onChange={(e) => {
                              const val = e.target.value === 'all' ? 'all' : Number(e.target.value);
                              setFilterWarehouseId(val);
                              setFilterProvince('all');
                              setFilterDistrict('all');
                            }}
                            className="w-full h-9 px-2.5 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-800 outline-none focus:border-amber-400"
                          >
                            <option value="all">Tất cả kho ({warehouseOptions.length})</option>
                            {warehouseOptions.map((wh) => (
                              <option key={wh.id} value={wh.id}>
                                {wh.name}
                              </option>
                            ))}
                          </select>
                        </div>

                        {/* Dropdown 2: Tỉnh / Thành Phố */}
                        <div>
                          <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                            Tỉnh / Thành phố
                          </label>
                          <select
                            value={filterProvince}
                            onChange={(e) => {
                              setFilterProvince(e.target.value);
                              setFilterDistrict('all');
                            }}
                            className="w-full h-9 px-2.5 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-800 outline-none focus:border-amber-400"
                          >
                            <option value="all">
                              Tất cả Tỉnh/Thành ({provinceOptions.length})
                            </option>
                            {provinceOptions.map((p) => (
                              <option key={p} value={p}>
                                {p}
                              </option>
                            ))}
                          </select>
                        </div>

                        {/* Dropdown 3: Quận / Huyện */}
                        <div>
                          <label className="block text-[10px] font-bold text-slate-500 uppercase mb-1">
                            Quận / Huyện (Cụm giao)
                          </label>
                          <select
                            value={filterDistrict}
                            onChange={(e) => setFilterDistrict(e.target.value)}
                            className="w-full h-9 px-2.5 bg-white border border-slate-200 rounded-xl text-xs font-semibold text-slate-800 outline-none focus:border-amber-400"
                          >
                            <option value="all">
                              Tất cả Quận/Huyện ({districtOptions.length})
                            </option>
                            {districtOptions.map((d) => (
                              <option key={d} value={d}>
                                {d} ({districtCounts[d]} đơn)
                              </option>
                            ))}
                          </select>
                        </div>
                      </div>

                      {/* Nút Gom Nhanh Theo Cụm Quận */}
                      {filterDistrict !== 'all' && (
                        <div className="flex items-center justify-between pt-2 border-t border-slate-200/60">
                          <span className="text-[11px] text-slate-600 font-medium">
                            Địa bàn đang chọn:{' '}
                            <strong className="text-slate-900">{filterDistrict}</strong> (
                            {districtCounts[filterDistrict] || 0} đơn)
                          </span>
                          <button
                            type="button"
                            onClick={() => selectAllInDistrict(filterDistrict)}
                            className="px-3 py-1.5 bg-amber-400 hover:bg-amber-500 text-slate-900 rounded-xl text-xs font-extrabold shadow-xs transition-all cursor-pointer"
                          >
                            Chọn Tất Cả Đơn {filterDistrict} ({districtCounts[filterDistrict] || 0}{' '}
                            đơn)
                          </button>
                        </div>
                      )}
                    </div>

                    {/* BẢNG DANH SÁCH ĐƠN B2C */}
                    <div className="overflow-x-auto">
                      <table className="w-full text-left text-xs">
                        <thead>
                          <tr className="border-b border-slate-100 bg-slate-50/70 text-slate-500 uppercase font-bold text-[11px] tracking-wider">
                            <th className="py-3 px-3 w-10 text-center">
                              <input
                                type="checkbox"
                                checked={
                                  filteredWaitingOrders.length > 0 &&
                                  filteredWaitingOrders.every((o) =>
                                    selectedOrderIds.includes(o.orderId)
                                  )
                                }
                                onChange={selectAllFilteredOrders}
                                className="w-4 h-4 rounded text-yellow-500 focus:ring-yellow-400 cursor-pointer accent-amber-500"
                              />
                            </th>
                            <th className="py-3 px-3">Mã Đơn / Khách Hàng</th>
                            <th className="py-3 px-3">Địa Chỉ Giao Hàng</th>
                            <th className="py-3 px-3">Tổng Tiền</th>
                            <th className="py-3 px-3 text-center">Đặc Tính</th>
                            <th className="py-3 px-3 text-right">Thao Tác</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 font-medium">
                          {filteredWaitingOrders.map((order) => {
                            const isSelected = selectedOrderIds.includes(order.orderId);
                            const isHighlighted =
                              highlightOrderId && Number(highlightOrderId) === order.orderId;
                            return (
                              <tr
                                key={order.orderId}
                                className={`hover:bg-amber-50/30 transition-colors ${
                                  isSelected ? 'bg-amber-50/60' : ''
                                } ${isHighlighted ? 'ring-2 ring-amber-400 ring-inset' : ''}`}
                              >
                                <td className="py-3 px-3 text-center">
                                  <input
                                    type="checkbox"
                                    checked={isSelected}
                                    onChange={() => toggleSelectOrder(order.orderId)}
                                    className="w-4 h-4 rounded text-yellow-500 focus:ring-yellow-400 cursor-pointer accent-amber-500"
                                  />
                                </td>
                                <td className="py-3 px-3">
                                  <span className="font-extrabold text-slate-900 text-xs block">
                                    {order.orderCode}
                                  </span>
                                  <span className="text-slate-500 text-[11px]">
                                    {order.receiverName} • {order.receiverPhone}
                                  </span>
                                </td>
                                <td className="py-3 px-3 max-w-[200px]">
                                  <span
                                    className="text-slate-700 line-clamp-2 text-[11px] font-medium"
                                    title={order.deliveryAddress}
                                  >
                                    {order.deliveryAddress}
                                  </span>
                                  {order.warehouseName && (
                                    <span className="text-[10px] text-slate-400 block mt-0.5 font-normal">
                                      Kho xuất: {order.warehouseName}
                                    </span>
                                  )}
                                </td>
                                <td className="py-3 px-3 font-bold text-slate-800">
                                  {fmtVnd(order.totalAmount)}
                                </td>
                                <td className="py-3 px-3 text-center">
                                  {order.requiresColdChain ? (
                                    <span className="px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-blue-50 text-blue-700 border border-blue-200">
                                      Chuỗi Lạnh
                                    </span>
                                  ) : (
                                    <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-slate-100 text-slate-500">
                                      Tiêu Chuẩn
                                    </span>
                                  )}
                                </td>
                                <td className="py-3 px-3 text-right">
                                  <button
                                    onClick={() => {
                                      setSelectedOrderIds([order.orderId]);
                                      setBatchModalOpen(true);
                                    }}
                                    className="px-2.5 py-1 bg-slate-100 hover:bg-amber-400 hover:text-slate-900 text-slate-700 rounded-lg text-[11px] font-bold transition-all shadow-xs cursor-pointer"
                                  >
                                    Gán Xe
                                  </button>
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ))}

              {/* TAB 2: CHUYỂN KHO B2B */}
              {activeTab === 'b2b' &&
                (loading ? (
                  <div className="p-12 text-center text-slate-400 text-xs flex flex-col items-center justify-center gap-3">
                    <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
                    <span>Đang tải danh sách chuyển kho...</span>
                  </div>
                ) : !stats?.transfersWaitingDispatch?.length ? (
                  <div className="py-14 text-center text-slate-400 text-xs font-medium">
                    Hiện không có lệnh chuyển kho nào đang chờ điều phối xe.
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-xs">
                      <thead>
                        <tr className="border-b border-slate-100 bg-slate-50/70 text-slate-500 uppercase font-bold text-[11px] tracking-wider">
                          <th className="py-3 px-3">Mã Lệnh Chuyển</th>
                          <th className="py-3 px-3">Tuyến Chuyển Kho</th>
                          <th className="py-3 px-3 text-center">Mặt Hàng</th>
                          <th className="py-3 px-3">Người Lập / Ngày Tạo</th>
                          <th className="py-3 px-3 text-right">Thao Tác</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 font-medium">
                        {stats.transfersWaitingDispatch.map((transfer) => (
                          <tr
                            key={transfer.transferId}
                            className="hover:bg-amber-50/30 transition-colors"
                          >
                            <td className="py-3 px-3">
                              <span className="font-extrabold text-slate-900 text-xs block">
                                {transfer.transferCode}
                              </span>
                              {transfer.note && (
                                <span className="text-slate-500 text-[11px] line-clamp-1">
                                  {transfer.note}
                                </span>
                              )}
                            </td>
                            <td className="py-3 px-3">
                              <div className="flex items-center gap-1.5 text-xs">
                                <span className="font-bold text-amber-700">
                                  {transfer.fromWarehouseName}
                                </span>
                                <span className="text-slate-400">→</span>
                                <span className="font-bold text-emerald-700">
                                  {transfer.toWarehouseName}
                                </span>
                              </div>
                            </td>
                            <td className="py-3 px-3 text-center font-bold text-slate-800">
                              {transfer.totalItems} SP
                            </td>
                            <td className="py-3 px-3 text-slate-600 text-[11px]">
                              <span className="font-semibold block">
                                {transfer.createdByName || 'Thủ kho'}
                              </span>
                              <span className="text-slate-400">
                                {new Date(transfer.createdAt).toLocaleDateString('vi-VN')}
                              </span>
                            </td>
                            <td className="py-3 px-3 text-right">
                              <button
                                onClick={() => {
                                  setSelectedTransfer(transfer);
                                  setSelectedB2bVehicleId(0);
                                  setB2bNote('');
                                  setB2bModalOpen(true);
                                }}
                                className="px-3 py-1.5 bg-amber-400 hover:bg-amber-300 text-slate-900 rounded-xl text-xs font-bold transition-all shadow-xs cursor-pointer"
                              >
                                Gán Xe & Lăn Bánh
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}

              {/* TAB 3: THU HỒI ĐƠN TRẢ (RMA) */}
              {activeTab === 'rma' &&
                (loading ? (
                  <div className="p-12 text-center text-slate-400 text-xs flex flex-col items-center justify-center gap-3">
                    <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
                    <span>Đang tải danh sách đơn trả...</span>
                  </div>
                ) : !stats?.returnsWaitingDispatch?.length ? (
                  <div className="py-14 text-center text-slate-400 text-xs font-medium">
                    Hiện không có phiếu trả hàng (RMA) nào đang chờ điều phối xe thu hồi.
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-xs">
                      <thead>
                        <tr className="border-b border-slate-100 bg-slate-50/70 text-slate-500 uppercase font-bold text-[11px] tracking-wider">
                          <th className="py-3 px-3">Mã RMA / Đơn Gốc</th>
                          <th className="py-3 px-3">Khách Hàng & SĐT</th>
                          <th className="py-3 px-3">Địa Chỉ Thu Hồi</th>
                          <th className="py-3 px-3 text-center">Mặt Hàng</th>
                          <th className="py-3 px-3">Lý Do Đổi Trả</th>
                          <th className="py-3 px-3 text-right">Thao Tác</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 font-medium">
                        {stats.returnsWaitingDispatch.map((retItem) => {
                          const isHighlighted =
                            highlightReturnId && Number(highlightReturnId) === retItem.returnId;
                          return (
                            <tr
                              key={retItem.returnId}
                              className={`transition-colors ${
                                isHighlighted
                                  ? 'bg-amber-50/80 border-2 border-amber-300'
                                  : 'hover:bg-amber-50/30'
                              }`}
                            >
                              <td className="py-3 px-3">
                                <span className="font-extrabold text-slate-900 text-xs block">
                                  {retItem.returnCode}
                                </span>
                                <span className="text-slate-500 text-[11px] block">
                                  Đơn: #{retItem.orderCode}
                                </span>
                                <span className="inline-block mt-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-50 text-amber-700 border border-amber-200">
                                  Thu hồi tại nhà khách
                                </span>
                              </td>
                              <td className="py-3 px-3">
                                <span className="font-bold text-slate-800 text-xs block">
                                  {retItem.customerName}
                                </span>
                                <span className="text-slate-500 text-[11px]">
                                  {retItem.customerPhone}
                                </span>
                              </td>
                              <td className="py-3 px-3 text-slate-600 text-[11px] max-w-[200px]">
                                <span className="line-clamp-2">{retItem.pickupAddress}</span>
                                <span className="text-amber-700 font-bold block mt-0.5">
                                  Về: {retItem.warehouseName}
                                </span>
                              </td>
                              <td className="py-3 px-3 text-center font-bold text-slate-800">
                                {retItem.totalItems} SP
                              </td>
                              <td className="py-3 px-3 text-slate-600 text-[11px] max-w-[150px]">
                                <span className="line-clamp-2">{retItem.reason}</span>
                              </td>
                              <td className="py-3 px-3 text-right">
                                <button
                                  onClick={() => {
                                    setSelectedReturn(retItem);
                                    setSelectedRmaVehicleId(0);
                                    setRmaNote('');
                                    setRmaModalOpen(true);
                                  }}
                                  className="px-3 py-1.5 bg-amber-400 hover:bg-amber-300 text-slate-900 rounded-xl text-xs font-bold transition-all shadow-xs cursor-pointer"
                                >
                                  Gán Xe Thu Hồi
                                </button>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                ))}
            </DashCard>
          </div>

          {/* CỘT PHẢI (5 COLS): GIÁM SÁT CHUYẾN XE ĐANG LĂN BÁNH */}
          <div className="lg:col-span-5 space-y-4">
            <DashCard
              title="Chuyến Xe Đang Vận Hành (Active Trips)"
              subtitle="Theo dõi tiến độ giao hàng và lộ trình thực tế của tài xế"
            >
              {loading ? (
                <div className="p-12 text-center text-slate-400 text-xs flex flex-col items-center justify-center gap-3">
                  <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
                  <span>Đang tải chuyến xe...</span>
                </div>
              ) : !stats?.recentActiveTrips.length ? (
                <div className="py-14 text-center text-slate-400 text-xs font-medium">
                  Hiện không có chuyến xe nào đang lăn bánh.
                </div>
              ) : (
                <div className="space-y-3">
                  {stats.recentActiveTrips.map((trip) => {
                    const isB2b = trip.tripType === 'B2B_Transfer';
                    const isReturn = trip.tripType === 'B2C_Return';
                    const isReturning =
                      trip.status === 'Returning' ||
                      (trip.status !== 'Completed' &&
                        trip.status !== 'Cancelled' &&
                        trip.deliveredOrders === trip.totalOrders &&
                        trip.totalOrders > 0);

                    const progressPct =
                      isB2b || isReturn
                        ? trip.status === 'Completed'
                          ? 100
                          : trip.status === 'InTransit'
                            ? 50
                            : 10
                        : trip.totalOrders > 0
                          ? Math.round((trip.deliveredOrders / trip.totalOrders) * 100)
                          : 0;

                    return (
                      <div
                        key={trip.id}
                        className="p-4 rounded-2xl border border-slate-200/80 bg-white hover:border-amber-300 hover:shadow-md transition-all cursor-pointer"
                        onClick={async () => {
                          setSelectedTrip(trip);
                          setTripModalOpen(true);
                          try {
                            const full = await vehicleApi.getTripById(trip.id);
                            if (full) setSelectedTrip(full);
                          } catch (e) {
                            console.error(e);
                          }
                        }}
                      >
                        <div className="flex items-center justify-between gap-2 mb-2">
                          <div className="flex items-center gap-2">
                            <span
                              className={`px-2 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider ${
                                isReturn
                                  ? 'bg-amber-50 text-amber-800 border border-amber-200'
                                  : trip.vehicleType === 'Motorbike'
                                    ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                                    : isB2b
                                      ? 'bg-blue-50 text-blue-700 border border-blue-200'
                                      : 'bg-amber-50 text-amber-700 border border-amber-200'
                              }`}
                            >
                              {isReturn
                                ? 'Thu Hồi'
                                : trip.vehicleType === 'Motorbike'
                                  ? 'Xe Máy'
                                  : 'Xe Tải'}
                            </span>
                            <div>
                              <div className="flex items-center gap-2">
                                <span className="font-extrabold text-slate-900 text-xs">
                                  {trip.tripCode}
                                </span>
                                {isB2b && (
                                  <span className="inline-flex items-center px-1.5 py-0.2 rounded text-[10px] font-bold bg-blue-100 text-blue-800">
                                    Chuyển kho B2B
                                  </span>
                                )}
                                {isReturn && (
                                  <span className="inline-flex items-center px-1.5 py-0.2 rounded text-[10px] font-bold bg-amber-100 text-amber-900 border border-amber-300">
                                    Thu hồi RMA
                                  </span>
                                )}
                              </div>
                              <span className="text-slate-400 text-[10px] font-mono font-bold">
                                [{trip.licensePlate}]
                              </span>
                            </div>
                          </div>

                          <StatusBadge
                            variant={
                              isReturning
                                ? 'amber'
                                : trip.status === 'InTransit'
                                  ? 'amber'
                                  : trip.status === 'Preparing'
                                    ? 'indigo'
                                    : 'emerald'
                            }
                            label={
                              isReturning
                                ? 'Chờ xe về kho'
                                : trip.status === 'InTransit'
                                  ? 'Đang lăn bánh'
                                  : trip.status === 'Preparing'
                                    ? 'Chuẩn bị xuất bến'
                                    : 'Hoàn tất'
                            }
                          />
                        </div>

                        <div className="flex items-center justify-between text-xs text-slate-600 mb-2">
                          <span className="font-medium">
                            Tài xế: {trip.driverName} ({trip.driverPhone})
                          </span>
                          <span className="text-[11px] font-bold text-slate-800">
                            {isB2b
                              ? trip.transferCode
                                ? `Phiếu ${trip.transferCode}`
                                : 'Chuyển kho'
                              : isReturn
                                ? `${trip.deliveredOrders || 0} / ${trip.totalOrders || trip.returns?.length || 1} đơn thu hồi`
                                : `${trip.deliveredOrders} / ${trip.totalOrders} đơn`}
                          </span>
                        </div>

                        {/* PROGRESS BAR */}
                        <div className="w-full bg-slate-100 rounded-full h-1.5 overflow-hidden mb-2">
                          <div
                            className={`h-full transition-all duration-500 rounded-full ${
                              progressPct === 100 ? 'bg-emerald-500' : 'bg-amber-500'
                            }`}
                            style={{ width: `${progressPct}%` }}
                          />
                        </div>

                        <div className="flex items-center justify-between text-[11px] text-slate-400">
                          <span>
                            {isB2b
                              ? `Tuyến: ${trip.fromWarehouseName || trip.warehouseName} → ${trip.toWarehouseName || 'Kho đích'}`
                              : isReturn
                                ? `Thu hồi về: ${trip.warehouseName}`
                                : `Kho: ${trip.warehouseName}`}
                          </span>
                          <span className="text-amber-600 font-bold hover:underline">
                            Chi tiết lộ trình →
                          </span>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </DashCard>
          </div>
        </div>

        {/* MODAL 1: TẠO CHUYẾN XE GOM ĐƠN HÀNG LOẠT (B2C) */}
        {batchModalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2.5">
                  <span className="px-2.5 py-1 bg-amber-50 text-amber-800 rounded-lg border border-amber-200 text-xs font-black">
                    B2C
                  </span>
                  <h3 className="font-extrabold text-slate-900 text-base">
                    Khởi Tạo Chuyến Xe Giao Hàng
                  </h3>
                </div>
                <button
                  onClick={() => setBatchModalOpen(false)}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="space-y-4 text-xs">
                <div className="p-3 bg-amber-50/70 rounded-xl border border-amber-200/60 text-amber-800">
                  Bạn đang tạo chuyến xe gom cho{' '}
                  <span className="font-black text-amber-900">{selectedOrderIds.length}</span> đơn
                  hàng đã chọn.
                </div>

                <FormSelect
                  label="Chọn Phương Tiện & Tài Xế"
                  required
                  value={selectedVehicleId}
                  onSelect={(val) => setSelectedVehicleId(Number(val))}
                  placeholder="-- Chọn xe từ đội xe nội bộ --"
                  options={[
                    { label: '-- Chọn xe từ đội xe nội bộ --', value: 0 },
                    ...vehicles
                      .filter((v) => v.isActive && v.status === 'Available')
                      .map((v) => ({
                        label: `${v.vehicleType === 'Motorbike' ? '[Xe máy] ' : '[Xe tải] '}${v.licensePlate} - ${v.driverName || 'Chưa gán tài xế'} (${v.driverPhone || 'Chưa có SĐT'}) [Kho: ${v.homeWarehouseName || 'Kho Tổng'}]`,
                        value: v.id,
                      })),
                  ]}
                  showSearch={true}
                  searchPlaceholder="Tìm kiếm xe hoặc tài xế..."
                />

                <div>
                  <FormLabel label="Ghi chú lộ trình (Tùy chọn)" />
                  <input
                    type="text"
                    placeholder="Ví dụ: Giao tuyến Quận 1 trước 11h..."
                    value={batchNote}
                    onChange={(e) => setBatchNote(e.target.value)}
                    className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-amber-400 focus:ring-4 focus:ring-amber-400/20 text-xs outline-none transition-all"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setBatchModalOpen(false)}
                  className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={handleCreateBatchTrip}
                  disabled={dispatchLoading || !selectedVehicleId}
                  className="bg-amber-400 hover:bg-amber-500 text-slate-900 px-5 py-2.5 rounded-xl text-xs font-extrabold shadow-xs transition-all disabled:opacity-50 cursor-pointer"
                >
                  {dispatchLoading ? 'Đang khởi tạo...' : 'Xác Nhận Tạo Chuyến'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* MODAL 2: CHI TIẾT CHUYẾN XE & CẬP NHẬT ĐIỂM DỪNG */}
        {tripModalOpen && selectedTrip && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-2xl w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-5 max-h-[90vh] overflow-y-auto animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div>
                  <div className="flex items-center gap-2">
                    <span
                      className={`px-2.5 py-0.5 rounded-lg border text-xs font-black ${
                        selectedTrip.tripType === 'B2C_Return'
                          ? 'bg-amber-50 text-amber-900 border-amber-300'
                          : selectedTrip.tripType === 'B2B_Transfer'
                            ? 'bg-blue-50 text-blue-800 border-blue-200'
                            : 'bg-amber-50 text-amber-800 border-amber-200'
                      }`}
                    >
                      {selectedTrip.tripType === 'B2C_Return'
                        ? 'THU HỒI HÀNG (RMA)'
                        : selectedTrip.tripType === 'B2B_Transfer'
                          ? 'CHUYỂN KHO B2B'
                          : 'CHUYẾN XE GIAO HÀNG'}
                    </span>
                    <h3 className="font-extrabold text-slate-900 text-base">
                      {selectedTrip.tripCode}
                    </h3>
                  </div>
                  <div className="text-xs text-slate-500 mt-1">
                    Biển số: <strong className="text-slate-800">{selectedTrip.licensePlate}</strong>{' '}
                    • Tài xế: <strong className="text-slate-800">{selectedTrip.driverName}</strong>{' '}
                    ({selectedTrip.driverPhone})
                  </div>
                </div>
                <button
                  onClick={() => setTripModalOpen(false)}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all cursor-pointer"
                >
                  ✕
                </button>
              </div>

              {/* ACTION BANNER CHO CHUYẾN */}
              <div className="p-4 bg-slate-50 rounded-2xl border border-slate-200/80 text-xs space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <span className="text-slate-500">Trạng thái: </span>
                    <span className="font-extrabold text-slate-900 mr-3">
                      {selectedTrip.status === 'Returning'
                        ? 'Chờ xe về kho'
                        : selectedTrip.status === 'InTransit'
                          ? selectedTrip.tripType === 'B2C_Return'
                            ? 'Tài xế đang đến nhà khách thu hồi'
                            : 'Đang lăn bánh'
                          : selectedTrip.status === 'Preparing'
                            ? 'Chuẩn bị xuất bến'
                            : selectedTrip.status === 'Completed'
                              ? 'Hoàn tất (Hàng đã về kho)'
                              : selectedTrip.status}
                    </span>
                    {selectedTrip.tripType === 'B2C_Return' ? (
                      <>
                        <span className="text-slate-500">Tiến độ thu hồi: </span>
                        <span className="font-bold text-amber-700">
                          {selectedTrip.status === 'Completed'
                            ? `${selectedTrip.returns?.length || 1} / ${selectedTrip.returns?.length || 1} đơn đã về kho`
                            : `Đang thu hồi ${selectedTrip.returns?.length || 1} đơn hoàn trả`}
                        </span>
                      </>
                    ) : selectedTrip.tripType === 'B2B_Transfer' ? (
                      <span className="font-bold text-amber-700">
                        Tuyến: {selectedTrip.fromWarehouseName || selectedTrip.warehouseName} →{' '}
                        {selectedTrip.toWarehouseName || 'Kho đích'}
                      </span>
                    ) : (
                      <>
                        <span className="text-slate-500">Tiến độ giao: </span>
                        <span className="font-bold text-emerald-600">
                          {selectedTrip.deliveredOrders} / {selectedTrip.totalOrders} điểm dừng hoàn
                          tất
                        </span>
                      </>
                    )}
                  </div>

                  <div className="flex items-center gap-2">
                    {selectedTrip.status === 'Preparing' && (
                      <button
                        onClick={() => handleStartTrip(selectedTrip.id)}
                        className="bg-amber-400 hover:bg-amber-500 text-slate-900 px-4 py-2 rounded-xl text-xs font-extrabold shadow-xs transition-all cursor-pointer"
                      >
                        {selectedTrip.tripType === 'B2C_Return'
                          ? 'Xuất Bến Đi Thu Hồi Hàng'
                          : 'Xuất Bến & Bắt Đầu Giao'}
                      </button>
                    )}
                    {selectedTrip.tripType === 'B2C_Return' &&
                      (selectedTrip.status === 'InTransit' ||
                        selectedTrip.status === 'Returning') && (
                        <button
                          onClick={() => handleCompleteTrip(selectedTrip.id)}
                          className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-bold text-xs shadow-xs transition-all cursor-pointer"
                        >
                          Xác Nhận Xe Đã Về Kho & Chốt Chuyến
                        </button>
                      )}
                    {selectedTrip.tripType === 'B2C_Delivery' &&
                      (selectedTrip.status === 'Returning' ||
                        (selectedTrip.deliveredOrders === selectedTrip.totalOrders &&
                          selectedTrip.totalOrders > 0 &&
                          selectedTrip.status !== 'Completed')) && (
                        <button
                          onClick={() => handleCompleteTrip(selectedTrip.id)}
                          className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-bold text-xs shadow-xs transition-all cursor-pointer"
                        >
                          Xác Nhận Xe Đã Về Kho & Chốt Chuyến
                        </button>
                      )}
                  </div>
                </div>
              </div>

              {/* NỘI DUNG THEO 3 LOẠI CHUYẾN: B2C_Return | B2B_Transfer | B2C_Delivery */}
              {selectedTrip.tripType === 'B2C_Return' ? (
                <div className="space-y-4">
                  <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                    <span className="font-extrabold text-xs text-slate-800 uppercase tracking-wide">
                      Danh Sách Phiếu Trả Hàng Cần Thu Hồi ({selectedTrip.returns?.length || 0})
                    </span>
                    <span className="text-[11px] font-bold text-amber-700 bg-amber-50 px-2.5 py-0.5 rounded-full border border-amber-200">
                      Thu hồi tại nhà khách
                    </span>
                  </div>

                  {!selectedTrip.returns || selectedTrip.returns.length === 0 ? (
                    <div className="py-8 text-center text-xs text-slate-400 font-medium">
                      Đang tải danh sách đơn hoàn trả...
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {selectedTrip.returns.map((retItem, idx) => (
                        <div
                          key={retItem.returnId || idx}
                          className="p-4 bg-slate-50/70 hover:bg-white rounded-2xl border border-slate-200/80 hover:border-amber-300 hover:shadow-sm transition-all space-y-3"
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <div className="flex items-center gap-2">
                                <span className="font-extrabold text-slate-900 text-sm">
                                  {retItem.returnCode}
                                </span>
                                <span className="text-slate-500 text-xs">
                                  (Đơn hàng:{' '}
                                  <strong className="text-indigo-600">#{retItem.orderCode}</strong>)
                                </span>
                              </div>
                              <div className="text-xs text-slate-600 mt-1">
                                Khách hàng:{' '}
                                <strong className="text-slate-800">{retItem.customerName}</strong> •
                                SĐT:{' '}
                                <strong className="text-slate-800">{retItem.customerPhone}</strong>
                              </div>
                            </div>

                            <span
                              className={`px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-xs ${
                                retItem.status === 'Inspecting' ||
                                selectedTrip.status === 'Completed'
                                  ? 'bg-purple-50 text-purple-700 border-purple-200'
                                  : 'bg-indigo-50 text-indigo-700 border-indigo-200'
                              }`}
                            >
                              {retItem.status === 'Inspecting' ||
                              selectedTrip.status === 'Completed'
                                ? 'Đã về kho (Chờ kiểm định QC)'
                                : 'Tài xế đang đi thu hồi'}
                            </span>
                          </div>

                          <div className="p-3 bg-white rounded-xl border border-slate-200/60 text-xs space-y-1.5 text-slate-600">
                            <div>
                              <span className="font-bold text-slate-500 block text-[11px] uppercase tracking-wider">
                                Địa chỉ lấy hàng tại nhà khách:
                              </span>
                              <span className="font-semibold text-slate-800 block mt-0.5">
                                {retItem.pickupAddress}
                              </span>
                            </div>
                            <div className="flex items-center justify-between pt-1.5 border-t border-slate-100">
                              <span>
                                <span className="text-slate-500">Mặt hàng: </span>
                                <span className="font-bold text-slate-800">
                                  {retItem.totalItems} SP
                                </span>
                              </span>
                              <span>
                                <span className="text-slate-500">Lý do hoàn trả: </span>
                                <span className="text-slate-700 font-medium italic">
                                  {retItem.reason || 'Khách yêu cầu đổi trả'}
                                </span>
                              </span>
                            </div>
                          </div>

                          <div className="flex justify-end pt-1">
                            <button
                              onClick={() => {
                                setTripModalOpen(false);
                                navigate(`/customer-returns/${retItem.returnId}`);
                              }}
                              className="px-3 py-1.5 bg-white text-slate-800 border border-slate-300 rounded-xl text-xs font-bold hover:bg-slate-50 transition-all cursor-pointer shadow-xs"
                            >
                              Xem Chi Tiết Phiếu Trả Hàng (RMA)
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="p-3.5 bg-amber-50/70 rounded-2xl border border-amber-200/70 text-xs text-amber-900 leading-relaxed">
                    <strong>Hướng dẫn nghiệp vụ:</strong> Sau khi tài xế lấy hàng và chở về kho, bấm{' '}
                    <strong>[Xác Nhận Xe Đã Về Kho & Chốt Chuyến]</strong>. Toàn bộ đơn thu hồi trên
                    chuyến xe sẽ tự động chuyển sang trạng thái <strong>Chờ kiểm định QC</strong> để
                    nhân viên kho tiến hành kiểm tra chất lượng nông sản.
                  </div>
                </div>
              ) : selectedTrip.tripType === 'B2B_Transfer' ? (
                <div className="space-y-4">
                  <div className="p-4 bg-slate-50/80 rounded-2xl border border-slate-200">
                    <span className="text-xs font-black text-slate-700 uppercase tracking-wider block mb-3">
                      Mốc Lộ Trình Vận Chuyển Liên Kho
                    </span>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                      <div className="p-3 bg-white rounded-xl border border-slate-200">
                        <span className="text-[10px] font-bold text-emerald-700 uppercase block">
                          1. Xuất bến
                        </span>
                        <span className="font-bold text-slate-800 text-xs block">
                          {selectedTrip.fromWarehouseName || selectedTrip.warehouseName}
                        </span>
                        <span className="text-[11px] text-slate-400 mt-0.5 block">
                          {selectedTrip.startedAt
                            ? new Date(selectedTrip.startedAt).toLocaleString('vi-VN')
                            : 'Đã xuất kho'}
                        </span>
                      </div>
                      <div className="p-3 bg-white rounded-xl border-2 border-amber-400">
                        <span className="text-[10px] font-bold text-amber-700 uppercase block">
                          2. Đang trên đường
                        </span>
                        <span className="font-mono font-bold text-slate-900 text-xs block">
                          {selectedTrip.licensePlate}
                        </span>
                        <span className="text-[11px] text-slate-600 mt-0.5 block">
                          {selectedTrip.driverName} ({selectedTrip.driverPhone})
                        </span>
                      </div>
                      <div className="p-3 bg-white rounded-xl border border-slate-200 opacity-80">
                        <span className="text-[10px] font-bold text-slate-500 uppercase block">
                          3. Tiếp nhận tại đích
                        </span>
                        <span className="font-bold text-slate-800 text-xs block">
                          {selectedTrip.toWarehouseName || 'Kho đích'}
                        </span>
                        <span className="text-[11px] text-slate-400 mt-0.5 block">
                          {selectedTrip.status === 'Completed'
                            ? 'Đã hoàn tất nhập kho'
                            : 'Chờ kiểm đếm'}
                        </span>
                      </div>
                    </div>
                  </div>

                  {selectedTrip.inventoryTransferId && (
                    <div className="flex items-center justify-between p-4 bg-amber-50/60 rounded-2xl border border-amber-200/60">
                      <div>
                        <span className="text-xs font-bold text-amber-900 block">
                          Phiếu chuyển kho liên kết:{' '}
                          {selectedTrip.transferCode || `#${selectedTrip.inventoryTransferId}`}
                        </span>
                        <span className="text-[11px] text-amber-700">
                          Thủ kho tại đích sẽ kiểm đếm và nhận hàng để hoàn tất chuyến xe.
                        </span>
                      </div>
                      <button
                        onClick={() => {
                          setTripModalOpen(false);
                          navigate(`/transfers/${selectedTrip.inventoryTransferId}`);
                        }}
                        className="px-3 py-1.5 bg-white text-slate-800 border border-slate-300 rounded-xl text-xs font-bold hover:bg-slate-50 transition-all cursor-pointer"
                      >
                        Xem Chi Tiết Phiếu Chuyển
                      </button>
                    </div>
                  )}
                </div>
              ) : (
                <div>
                  <FormLabel label="Danh Sách Các Điểm Dừng / Đơn Hàng" />

                  <div className="space-y-2 mt-2">
                    {selectedTrip.orders.map((stop, idx) => {
                      const isOnlinePaid = stop.isPaid || stop.codAmount === 0;
                      return (
                        <div
                          key={stop.id}
                          className={`p-3.5 rounded-2xl border flex items-center justify-between gap-3 text-xs ${
                            stop.status === 'Delivered'
                              ? 'bg-emerald-50/50 border-emerald-200'
                              : stop.status === 'Failed'
                                ? 'bg-rose-50/50 border-rose-200'
                                : 'bg-white border-slate-200/80'
                          }`}
                        >
                          <div className="flex items-start gap-3">
                            <span className="w-6 h-6 rounded-full bg-slate-100 font-bold flex items-center justify-center text-slate-600 shrink-0 text-[11px]">
                              {idx + 1}
                            </span>
                            <div>
                              <div className="flex items-center gap-2">
                                <span className="font-extrabold text-slate-900">
                                  {stop.orderCode}
                                </span>
                                <span className="text-slate-500">
                                  • {stop.receiverName} ({stop.receiverPhone})
                                </span>
                              </div>
                              <p className="text-slate-500 text-[11px] mt-0.5">
                                {stop.deliveryAddress}
                              </p>
                              {stop.status === 'Failed' ? (
                                <span className="text-[11px] font-bold text-rose-700 mt-0.5 block">
                                  Thu tiền COD: 0 ₫{' '}
                                  <span className="text-slate-400 font-normal line-through">
                                    ({fmtVnd(stop.totalAmount)})
                                  </span>{' '}
                                  - Khách từ chối nhận
                                </span>
                              ) : isOnlinePaid ? (
                                <span className="text-[11px] font-bold text-emerald-700 mt-0.5 block">
                                  Đã thanh toán Online (Thu COD: 0 ₫)
                                </span>
                              ) : (
                                <span className="text-[11px] font-semibold text-slate-800 mt-0.5 block">
                                  Thu tiền COD:{' '}
                                  <span className="text-amber-700 font-bold">
                                    {fmtVnd(stop.codAmount ?? stop.totalAmount)}
                                  </span>
                                </span>
                              )}
                            </div>
                          </div>

                          <div className="flex items-center gap-2">
                            {stop.status === 'Delivered' ? (
                              <StatusBadge variant="emerald" label="Đã giao (Khách đã nhận)" />
                            ) : stop.status === 'Failed' ? (
                              <div className="text-right">
                                <StatusBadge variant="rose" label="Khách từ chối nhận (Hoàn về)" />
                                {stop.failureReason && (
                                  <span
                                    className="text-[10px] text-rose-600 font-medium block mt-0.5 max-w-[200px] truncate"
                                    title={stop.failureReason}
                                  >
                                    {stop.failureReason}
                                  </span>
                                )}
                              </div>
                            ) : (
                              <div className="flex items-center gap-1.5">
                                <button
                                  onClick={() =>
                                    handleMarkOrderDelivered(selectedTrip.id, stop.orderId)
                                  }
                                  className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-bold text-xs shadow-xs transition-all cursor-pointer"
                                >
                                  Xác Nhận Đã Giao
                                </button>
                                <button
                                  onClick={() =>
                                    openRefusalModal(selectedTrip.id, stop.orderId, stop.orderCode)
                                  }
                                  className="px-3 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-700 border border-rose-200 rounded-xl font-bold text-xs transition-all cursor-pointer"
                                >
                                  Khách Từ Chối
                                </button>
                              </div>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                  {/* TỔNG TIỀN COD PHẢI NỘP VỀ KHO */}
                  {selectedTrip.tripType === 'B2C_Delivery' && (
                    <div className="mt-3 p-3.5 bg-slate-50 border border-slate-200/80 rounded-2xl flex items-center justify-between text-xs">
                      <span className="text-slate-600 font-medium">
                        Tổng tiền COD tài xế phải nộp về kho (Đã trừ các đơn từ chối):
                      </span>
                      <span className="font-extrabold text-amber-700 text-sm">
                        {fmtVnd(
                          selectedTrip.orders
                            .filter((s) => s.status === 'Delivered' && !s.isPaid)
                            .reduce((acc, s) => acc + (s.codAmount ?? s.totalAmount), 0)
                        )}
                      </span>
                    </div>
                  )}
                </div>
              )}

              <div className="flex justify-end pt-3 border-t border-slate-100">
                <button
                  onClick={() => setTripModalOpen(false)}
                  className="px-4 py-2 bg-slate-100 text-slate-700 rounded-xl text-xs font-bold hover:bg-slate-200 transition-all cursor-pointer"
                >
                  Đóng
                </button>
              </div>
            </div>
          </div>
        )}

        {/* MODAL 3: GÁN XE TẢI LẠNH ĐIỀU PHỐI B2B */}
        {b2bModalOpen && selectedTransfer && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2.5">
                  <span className="px-2.5 py-1 bg-blue-50 text-blue-700 rounded-lg border border-blue-200 text-xs font-black">
                    B2B
                  </span>
                  <div>
                    <h3 className="font-extrabold text-slate-900 text-base">
                      Điều Phối Xe Tải Lạnh B2B
                    </h3>
                    <p className="text-xs text-slate-500">
                      Lệnh chuyển:{' '}
                      <strong className="text-slate-800">{selectedTransfer.transferCode}</strong>
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setB2bModalOpen(false)}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="space-y-4">
                <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200/80 text-xs space-y-1.5">
                  <div className="flex justify-between">
                    <span className="text-slate-500">Kho nguồn (Xuất):</span>
                    <span className="font-bold text-amber-700">
                      {selectedTransfer.fromWarehouseName}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Kho đích (Nhập):</span>
                    <span className="font-bold text-emerald-700">
                      {selectedTransfer.toWarehouseName}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Tổng số mặt hàng:</span>
                    <span className="font-bold text-slate-900">
                      {selectedTransfer.totalItems} mặt hàng
                    </span>
                  </div>
                </div>

                <FormSelect
                  label="Chọn Xe Tải Lạnh Chuyên Dụng"
                  required
                  value={selectedB2bVehicleId}
                  onSelect={(val) => setSelectedB2bVehicleId(Number(val))}
                  placeholder="-- Chọn xe tải lạnh sẵn sàng --"
                  options={[
                    { label: '-- Chọn xe tải lạnh sẵn sàng --', value: 0 },
                    ...vehicles
                      .filter(
                        (v) =>
                          v.isActive &&
                          v.vehicleType === 'RefrigeratedTruck' &&
                          v.status === 'Available'
                      )
                      .map((v) => ({
                        label: `${v.licensePlate} - ${v.driverName || 'Chưa gán tài xế'} (${v.driverPhone || '---'}) [Tải trọng: ${v.maxWeightKg} kg]`,
                        value: v.id,
                      })),
                  ]}
                  showSearch={true}
                  searchPlaceholder="Tìm kiếm biển số hoặc tài xế..."
                />

                {vehicles.filter(
                  (v) =>
                    v.isActive && v.vehicleType === 'RefrigeratedTruck' && v.status === 'Available'
                ).length === 0 && (
                  <p className="text-xs text-rose-600 font-medium">
                    Hiện không có xe tải lạnh nào ở trạng thái sẵn sàng (Available). Vui lòng kiểm
                    tra lại đội xe.
                  </p>
                )}

                <div>
                  <FormLabel label="Ghi chú điều phối (Tùy chọn)" />
                  <input
                    type="text"
                    placeholder="Ví dụ: Vận chuyển hàng lạnh nhiệt độ 2-8 độ C..."
                    value={b2bNote}
                    onChange={(e) => setB2bNote(e.target.value)}
                    className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-amber-400 focus:ring-4 focus:ring-amber-400/20 text-xs outline-none transition-all"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setB2bModalOpen(false)}
                  className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={handleCreateB2bTrip}
                  disabled={b2bDispatchLoading || !selectedB2bVehicleId}
                  className="bg-amber-400 hover:bg-amber-300 text-slate-900 px-5 py-2.5 rounded-xl text-xs font-bold shadow-xs transition-all disabled:opacity-50 cursor-pointer"
                >
                  {b2bDispatchLoading ? 'Đang xuất bến...' : 'Xác Nhận Xuất Bến & Khởi Hành'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* MODAL 4: GÁN XE THU HỒI ĐƠN TRẢ RMA */}
        {rmaModalOpen && selectedReturn && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2.5">
                  <span className="px-2.5 py-1 bg-amber-50 text-amber-700 rounded-lg border border-amber-200 text-xs font-bold">
                    Thu hồi tại nhà khách
                  </span>
                  <div>
                    <h3 className="font-extrabold text-slate-900 text-base">
                      Điều Phối Xe Thu Hồi Đơn Trả
                    </h3>
                    <p className="text-xs text-slate-500">
                      Phiếu trả:{' '}
                      <strong className="text-slate-800">{selectedReturn.returnCode}</strong> • Đơn
                      #{selectedReturn.orderCode}
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setRmaModalOpen(false)}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="space-y-4">
                <div className="p-3.5 bg-slate-50 rounded-2xl border border-slate-200/80 text-xs space-y-1.5">
                  <div className="flex justify-between">
                    <span className="text-slate-500">Khách hàng:</span>
                    <span className="font-bold text-slate-900">
                      {selectedReturn.customerName} ({selectedReturn.customerPhone})
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-500 block">Địa chỉ thu hồi:</span>
                    <span className="font-semibold text-slate-800 block mt-0.5">
                      {selectedReturn.pickupAddress}
                    </span>
                  </div>
                  <div className="flex justify-between pt-1 border-t border-slate-200/60">
                    <span className="text-slate-500">Kho tiếp nhận hoàn:</span>
                    <span className="font-bold text-emerald-700">
                      {selectedReturn.warehouseName}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Lý do đổi trả:</span>
                    <span className="font-medium text-slate-700">{selectedReturn.reason}</span>
                  </div>
                </div>

                <FormSelect
                  label="Chọn Phương Tiện Đi Thu Hồi"
                  required
                  value={selectedRmaVehicleId}
                  onSelect={(val) => setSelectedRmaVehicleId(Number(val))}
                  placeholder="-- Chọn xe máy hoặc xe tải sẵn sàng --"
                  options={[
                    { label: '-- Chọn xe máy hoặc xe tải sẵn sàng --', value: 0 },
                    ...vehicles
                      .filter((v) => v.isActive && v.status === 'Available')
                      .map((v) => ({
                        label: `${v.vehicleType === 'Motorbike' ? '[Xe Máy]' : '[Xe Tải]'} ${v.licensePlate} - ${v.driverName || 'Chưa gán tài xế'} (${v.driverPhone || '---'})`,
                        value: v.id,
                      })),
                  ]}
                  showSearch={true}
                  searchPlaceholder="Tìm kiếm biển số hoặc tài xế..."
                />

                {vehicles.filter((v) => v.isActive && v.status === 'Available').length === 0 && (
                  <p className="text-xs text-rose-600 font-medium">
                    Hiện không có phương tiện nào ở trạng thái sẵn sàng (Available). Vui lòng đợi
                    chuyến xe hoàn tất.
                  </p>
                )}

                <div>
                  <FormLabel label="Ghi chú điều phối thu hồi (Tùy chọn)" />
                  <input
                    type="text"
                    placeholder="Ví dụ: Kiểm tra kỹ tình trạng dập nát khi nhận hàng từ khách..."
                    value={rmaNote}
                    onChange={(e) => setRmaNote(e.target.value)}
                    className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-amber-400 focus:ring-4 focus:ring-amber-400/20 text-xs outline-none transition-all"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setRmaModalOpen(false)}
                  className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={handleDispatchReturn}
                  disabled={rmaDispatchLoading || !selectedRmaVehicleId}
                  className="bg-amber-400 hover:bg-amber-300 text-slate-900 px-5 py-2.5 rounded-xl text-xs font-bold shadow-xs transition-all disabled:opacity-50 cursor-pointer"
                >
                  {rmaDispatchLoading ? 'Đang điều phối...' : 'Xác Nhận Xuất Bến Thu Hồi'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* MODAL 5: XÁC NHẬN KHÁCH TỪ CHỐI NHẬN HÀNG (THEME SOLARIS NO-ICON) */}
        {refusalModalOpen && refusalTarget && (
          <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-5 animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                <div className="flex items-center gap-2.5">
                  <span className="px-2.5 py-1 bg-rose-50 text-rose-700 rounded-lg border border-rose-200 text-xs font-black">
                    TỪ CHỐI
                  </span>
                  <div>
                    <h3 className="font-extrabold text-slate-900 text-base">
                      Khách Từ Chối Nhận Hàng
                    </h3>
                    <p className="text-xs text-slate-500">
                      Đơn hàng:{' '}
                      <strong className="text-slate-800">{refusalTarget.orderCode}</strong>
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => {
                    setRefusalModalOpen(false);
                    setRefusalTarget(null);
                  }}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="space-y-4 text-xs">
                <div>
                  <FormLabel label="Chọn Lý Do Phổ Biến" />
                  <div className="flex flex-wrap gap-1.5 mt-2">
                    {[
                      'Khách đổi ý không muốn nhận',
                      'Hàng dập nát / không đạt độ tươi',
                      'Giao trễ hẹn, khách đi vắng',
                      'Không thể liên lạc được (gọi 3 lần)',
                      'Giao sai sản phẩm / quy cách',
                    ].map((chip) => (
                      <button
                        key={chip}
                        type="button"
                        onClick={() => setRefusalReason(chip)}
                        className={`px-3 py-1.5 rounded-xl border font-bold text-[11px] transition-all cursor-pointer ${
                          refusalReason === chip
                            ? 'bg-rose-50 text-rose-800 border-rose-300 shadow-2xs'
                            : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'
                        }`}
                      >
                        {chip}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <FormLabel label="Chi tiết ghi chú thêm (Tùy chọn)" />
                  <textarea
                    rows={3}
                    placeholder="Nhập thêm chi tiết thực tế của shipper..."
                    value={customRefusalNote}
                    onChange={(e) => setCustomRefusalNote(e.target.value)}
                    className="w-full p-3 rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-rose-400 focus:ring-4 focus:ring-rose-400/20 text-xs outline-none transition-all resize-none"
                  />
                </div>

                <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-amber-800 text-[11px] leading-relaxed">
                  Lưu ý: Tiền COD sẽ ghi nhận 0 ₫. Khi chuyến xe hoàn tất và về kho, hệ thống sẽ tự
                  động sinh phiếu trả hàng (RMA) trạng thái Chờ duyệt để thủ kho kiểm định nông sản.
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => {
                    setRefusalModalOpen(false);
                    setRefusalTarget(null);
                  }}
                  disabled={refusalLoading}
                  className="px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
                >
                  Bỏ Qua
                </button>
                <button
                  type="button"
                  onClick={handleConfirmRefusal}
                  disabled={refusalLoading}
                  className="bg-rose-600 hover:bg-rose-700 text-white px-5 py-2.5 rounded-xl text-xs font-bold shadow-xs transition-all disabled:opacity-50 cursor-pointer"
                >
                  {refusalLoading ? 'Đang xử lý...' : 'Xác Nhận Trả Hàng Về Kho'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
