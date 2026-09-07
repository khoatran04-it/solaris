import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Truck,
  Bike,
  Package,
  RefreshCw,
  Navigation,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Plus,
  ArrowRight,
  User,
  Phone,
  MapPin,
  Calendar,
  Layers,
  Check,
  ChevronRight,
  ExternalLink,
} from 'lucide-react';
import { vehicleApi } from '../../api/vehicleApi';
import {
  TransportationDashboardStats,
  DeliveryTrip,
  DeliveryVehicle,
  OrderWaitingDispatch,
} from '../../types/vehicle';
import { KpiMetricCard, DashCard, fmtVnd } from '../../components/dashboard/DashboardCharts';
import { Toast } from '../../components/commons/Toast';

export default function TransportationDashboardPage() {
  const navigate = useNavigate();

  // --- STATES ---
  const [stats, setStats] = useState<TransportationDashboardStats | null>(null);
  const [vehicles, setVehicles] = useState<DeliveryVehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Multi-select for Batch Dispatch
  const [selectedOrderIds, setSelectedOrderIds] = useState<number[]>([]);
  const [batchModalOpen, setBatchModalOpen] = useState(false);
  const [selectedVehicleId, setSelectedVehicleId] = useState<number>(0);
  const [batchNote, setBatchNote] = useState('');
  const [dispatchLoading, setDispatchLoading] = useState(false);

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
    const interval = setInterval(loadData, 30000); // Auto-refresh mỗi 30s
    return () => clearInterval(interval);
  }, [loadData]);

  // --- HANDLERS ---
  const toggleSelectOrder = (id: number) => {
    setSelectedOrderIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const selectAllOrders = () => {
    if (!stats) return;
    if (selectedOrderIds.length === stats.ordersWaitingDispatch.length) {
      setSelectedOrderIds([]);
    } else {
      setSelectedOrderIds(stats.ordersWaitingDispatch.map((o) => o.orderId));
    }
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

      showToast('success', `Đã khởi tạo chuyến xe mới cho ${selectedOrderIds.length} đơn hàng!`);
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

  const handleStartTrip = async (tripId: number) => {
    try {
      await vehicleApi.startTrip(tripId);
      showToast('success', 'Chuyến xe đã xuất phát! Trạng thái chuyển sang Đang giao (In-Transit)');
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
      showToast('success', 'Đã ghi nhận giao đơn hàng thành công!');
      setSelectedTrip(updated);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi cập nhật trạng thái đơn');
    }
  };

  const handleCompleteTrip = async (tripId: number) => {
    try {
      await vehicleApi.completeTrip(tripId);
      showToast('success', 'Chuyến xe đã hoàn tất 100%! Xe đã sẵn sàng cho chuyến tiếp theo.');
      setTripModalOpen(false);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi kết thúc chuyến');
    }
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <Toast {...toast} />

      {/* TOP HEADER */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs">
        <div>
          <div className="flex items-center gap-2">
            <span className="p-2 bg-blue-50 text-blue-600 rounded-xl">
              <Navigation className="w-6 h-6" />
            </span>
            <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight">
              Trung Tâm Điều Phối & Quản Lý Vận Tải Chuỗi Lạnh
            </h1>
          </div>
          <p className="text-slate-500 text-sm mt-1">
            Điều phối Last-mile xe máy thùng lạnh nội thành & Giám sát Chuyển kho liên chi nhánh B2B
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/transportation/vehicles')}
            className="flex items-center gap-2 px-4 py-2.5 bg-slate-100 text-slate-700 hover:bg-slate-200 rounded-xl text-sm font-bold transition-all"
          >
            <Truck className="w-4 h-4 text-slate-600" />
            Danh Sách Đội Xe ({vehicles.length})
          </button>
          <button
            onClick={loadData}
            disabled={refreshing}
            className="flex items-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-sm font-bold shadow-sm shadow-blue-200 transition-all disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
            Làm Mới
          </button>
        </div>
      </div>

      {/* 4 TOP KPI CARDS */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiMetricCard
          title="Xe Máy Thùng Lạnh Sẵn Sàng"
          value={stats ? `${stats.availableBikes} / ${stats.totalBikes}` : '...'}
          subtext="Xe rảnh chờ nhận đơn nội thành"
          icon={<Bike className="w-6 h-6 text-emerald-600" />}
          trend="positive"
        />
        <KpiMetricCard
          title="Xe Tải Lạnh Đang Chạy"
          value={stats ? `${stats.activeTrucks} / ${stats.totalTrucks}` : '...'}
          subtext="Vận chuyển chuyển kho B2B"
          icon={<Truck className="w-6 h-6 text-blue-600" />}
          trend="neutral"
        />
        <KpiMetricCard
          title="Đơn Chuỗi Lạnh Chờ Xe"
          value={stats ? stats.pendingColdChainOrders : '...'}
          subtext="Cần điều phối giao nhanh"
          icon={<AlertTriangle className="w-6 h-6 text-amber-500" />}
          trend="warning"
        />
        <KpiMetricCard
          title="Chuyến Xe Đang Lăn Bánh"
          value={stats ? stats.activeTripsCount : '...'}
          subtext="Chuyến đang giao hoặc chuẩn bị"
          icon={<Navigation className="w-6 h-6 text-purple-600" />}
          trend="positive"
        />
      </div>

      {/* MAIN CONTENT: 2 COLUMNS */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        
        {/* CỘT TRÁI (7 COLS): HÀNG ĐỢI ĐIỀU PHỐI & GOM ĐƠN */}
        <div className="lg:col-span-7 space-y-4">
          <DashCard
            title="Hàng Đợi Đơn Chờ Điều Phối (Dispatch Queue)"
            subtitle="Tích chọn các đơn cùng khu vực để gom chuyến xe máy giao hàng"
            action={
              selectedOrderIds.length > 0 && (
                <button
                  onClick={() => setBatchModalOpen(true)}
                  className="flex items-center gap-2 px-3.5 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-bold hover:bg-blue-700 shadow-sm transition-all animate-pulse"
                >
                  <Layers className="w-3.5 h-3.5" />
                  Tạo Chuyến ({selectedOrderIds.length} Đơn)
                </button>
              )
            }
          >
            {loading ? (
              <div className="p-8 text-center text-slate-400 text-sm">Đang tải danh sách đơn...</div>
            ) : !stats?.ordersWaitingDispatch.length ? (
              <div className="p-12 text-center text-slate-400 text-sm">
                🎉 Tuyệt vời! Hiện không có đơn hàng nào đang chờ điều phối xe.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/70 text-slate-500 uppercase font-bold">
                      <th className="py-3 px-3 w-10 text-center">
                        <input
                          type="checkbox"
                          checked={
                            selectedOrderIds.length === stats.ordersWaitingDispatch.length &&
                            stats.ordersWaitingDispatch.length > 0
                          }
                          onChange={selectAllOrders}
                          className="w-4 h-4 rounded border-slate-300 text-blue-600 cursor-pointer"
                        />
                      </th>
                      <th className="py-3 px-3">Mã Đơn / Khách Hàng</th>
                      <th className="py-3 px-3">Địa Chỉ Giao Hàng</th>
                      <th className="py-3 px-3">Tổng Tiền</th>
                      <th className="py-3 px-3 text-center">Đặc Tính</th>
                      <th className="py-3 px-3 text-right">Thao Tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {stats.ordersWaitingDispatch.map((order) => {
                      const isSelected = selectedOrderIds.includes(order.orderId);
                      return (
                        <tr
                          key={order.orderId}
                          className={`hover:bg-blue-50/40 transition-colors ${
                            isSelected ? 'bg-blue-50/60' : ''
                          }`}
                        >
                          <td className="py-3 px-3 text-center">
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => toggleSelectOrder(order.orderId)}
                              className="w-4 h-4 rounded border-slate-300 text-blue-600 cursor-pointer"
                            />
                          </td>
                          <td className="py-3 px-3">
                            <span className="font-extrabold text-slate-800 text-xs block">
                              {order.orderCode}
                            </span>
                            <span className="text-slate-500 text-[11px]">
                              {order.receiverName} • {order.receiverPhone}
                            </span>
                          </td>
                          <td className="py-3 px-3 max-w-[180px]">
                            <span
                              className="text-slate-600 line-clamp-2 text-[11px]"
                              title={order.deliveryAddress}
                            >
                              <MapPin className="w-3 h-3 inline mr-1 text-slate-400 shrink-0" />
                              {order.deliveryAddress}
                            </span>
                          </td>
                          <td className="py-3 px-3 font-bold text-slate-800">
                            {fmtVnd(order.totalAmount)}
                          </td>
                          <td className="py-3 px-3 text-center">
                            {order.requiresColdChain ? (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-extrabold bg-blue-100 text-blue-700 border border-blue-200">
                                ❄️ Chuỗi Lạnh
                              </span>
                            ) : (
                              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-slate-100 text-slate-600">
                                Tiêu chuẩn
                              </span>
                            )}
                          </td>
                          <td className="py-3 px-3 text-right">
                            <button
                              onClick={() => {
                                setSelectedOrderIds([order.orderId]);
                                setBatchModalOpen(true);
                              }}
                              className="px-2.5 py-1 bg-slate-100 hover:bg-blue-600 hover:text-white text-slate-700 rounded text-[11px] font-bold transition-all shadow-xs"
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
            )}
          </DashCard>
        </div>

        {/* CỘT PHẢI (5 COLS): GIÁM SÁT CHUYẾN XE ĐANG LĂN BÁNH */}
        <div className="lg:col-span-5 space-y-4">
          <DashCard
            title="Chuyến Xe Đang Vận Hành (Active Trips)"
            subtitle="Theo dõi tiến độ giao hàng và lộ trình thực tế của tài xế"
          >
            {loading ? (
              <div className="p-8 text-center text-slate-400 text-sm">Đang tải chuyến xe...</div>
            ) : !stats?.recentActiveTrips.length ? (
              <div className="p-12 text-center text-slate-400 text-sm">
                Hiện không có chuyến xe nào đang lăn bánh.
              </div>
            ) : (
              <div className="space-y-3">
                {stats.recentActiveTrips.map((trip) => {
                  const progressPct =
                    trip.totalOrders > 0
                      ? Math.round((trip.deliveredOrders / trip.totalOrders) * 100)
                      : 0;

                  return (
                    <div
                      key={trip.id}
                      className="p-4 rounded-xl border border-slate-200 bg-white hover:border-blue-300 hover:shadow-md transition-all cursor-pointer"
                      onClick={() => {
                        setSelectedTrip(trip);
                        setTripModalOpen(true);
                      }}
                    >
                      <div className="flex items-center justify-between gap-2 mb-2">
                        <div className="flex items-center gap-2">
                          <span
                            className={`p-1.5 rounded-lg ${
                              trip.vehicleType === 'Motorbike'
                                ? 'bg-emerald-50 text-emerald-600'
                                : 'bg-blue-50 text-blue-600'
                            }`}
                          >
                            {trip.vehicleType === 'Motorbike' ? (
                              <Bike className="w-4 h-4" />
                            ) : (
                              <Truck className="w-4 h-4" />
                            )}
                          </span>
                          <div>
                            <span className="font-extrabold text-slate-900 text-xs">
                              {trip.tripCode}
                            </span>
                            <span className="text-slate-400 text-[10px] ml-2 font-mono">
                              [{trip.licensePlate}]
                            </span>
                          </div>
                        </div>

                        <span
                          className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${
                            trip.status === 'InTransit'
                              ? 'bg-amber-50 text-amber-700 border-amber-200 animate-pulse'
                              : trip.status === 'Preparing'
                              ? 'bg-indigo-50 text-indigo-700 border-indigo-200'
                              : 'bg-emerald-50 text-emerald-700 border-emerald-200'
                          }`}
                        >
                          {trip.status === 'InTransit'
                            ? 'Đang lăn bánh'
                            : trip.status === 'Preparing'
                            ? 'Chuẩn bị xuất bến'
                            : 'Hoàn tất'}
                        </span>
                      </div>

                      <div className="flex items-center justify-between text-xs text-slate-600 mb-2">
                        <span className="flex items-center gap-1 font-medium">
                          <User className="w-3 h-3 text-slate-400" />
                          {trip.driverName} ({trip.driverPhone})
                        </span>
                        <span className="text-[11px] font-bold text-slate-800">
                          {trip.deliveredOrders} / {trip.totalOrders} đơn
                        </span>
                      </div>

                      {/* PROGRESS BAR */}
                      <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden mb-2">
                        <div
                          className={`h-full transition-all duration-500 rounded-full ${
                            progressPct === 100 ? 'bg-emerald-500' : 'bg-blue-600'
                          }`}
                          style={{ width: `${progressPct}%` }}
                        />
                      </div>

                      <div className="flex items-center justify-between text-[11px] text-slate-400">
                        <span>Kho: {trip.warehouseName}</span>
                        <span className="text-blue-600 font-bold flex items-center gap-0.5 group-hover:underline">
                          Chi tiết lộ trình <ChevronRight className="w-3 h-3" />
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

      {/* MODAL 1: TẠO CHUYẾN XE GOM ĐƠN HÀNG LOẠT */}
      {batchModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-xs p-4">
          <div className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100 space-y-5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-slate-900 text-base flex items-center gap-2">
                <Truck className="w-5 h-5 text-blue-600" />
                Khởi Tạo Chuyến Xe Giao Hàng
              </h3>
              <button
                onClick={() => setBatchModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                ✕
              </button>
            </div>

            <div className="space-y-4">
              <div className="p-3 bg-blue-50 rounded-xl border border-blue-100 text-xs text-blue-800">
                Bạn đang tạo chuyến xe gom cho{' '}
                <span className="font-black text-blue-900">{selectedOrderIds.length}</span> đơn
                hàng đã chọn.
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1.5">
                  Chọn Phương Tiện & Tài Xế:
                </label>
                <select
                  value={selectedVehicleId}
                  onChange={(e) => setSelectedVehicleId(Number(e.target.value))}
                  className="w-full px-3 py-2.5 border border-slate-300 rounded-xl text-xs font-medium focus:ring-2 focus:ring-blue-500 outline-none"
                >
                  <option value={0}>-- Chọn xe từ đội xe nội bộ --</option>
                  {vehicles
                    .filter((v) => v.isActive && v.status === 'Available')
                    .map((v) => (
                      <option key={v.id} value={v.id}>
                        {v.vehicleType === 'Motorbike' ? '🛵 Xe máy: ' : '🚚 Xe tải: '}
                        {v.licensePlate} - {v.driverName} ({v.driverPhone}) [Kho:{' '}
                        {v.homeWarehouseName || 'Kho Tổng'}]
                      </option>
                    ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase mb-1.5">
                  Ghi chú lộ trình (Tùy chọn):
                </label>
                <input
                  type="text"
                  placeholder="Ví dụ: Giao tuyến Quận 1 trước 11h..."
                  value={batchNote}
                  onChange={(e) => setBatchNote(e.target.value)}
                  className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setBatchModalOpen(false)}
                className="px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleCreateBatchTrip}
                disabled={dispatchLoading || !selectedVehicleId}
                className="px-5 py-2 text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white rounded-xl shadow-md transition-all disabled:opacity-50"
              >
                {dispatchLoading ? 'Đang khởi tạo...' : 'Xác Nhận Tạo Chuyến'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: CHI TIẾT CHUYẾN XE & CẬP NHẬT ĐIỂM DỪNG */}
      {tripModalOpen && selectedTrip && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-xs p-4">
          <div className="bg-white rounded-2xl max-w-2xl w-full p-6 shadow-2xl border border-slate-100 space-y-5 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div>
                <h3 className="font-extrabold text-slate-900 text-base flex items-center gap-2">
                  <Navigation className="w-5 h-5 text-blue-600" />
                  Chi Tiết Chuyến Xe: {selectedTrip.tripCode}
                </h3>
                <span className="text-xs text-slate-500">
                  Biển số: <strong className="text-slate-800">{selectedTrip.licensePlate}</strong> •
                  Tài xế:{' '}
                  <strong className="text-slate-800">{selectedTrip.driverName}</strong> (
                  {selectedTrip.driverPhone})
                </span>
              </div>
              <button
                onClick={() => setTripModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                ✕
              </button>
            </div>

            {/* ACTION BANNER CHO CHUYẾN */}
            <div className="flex items-center justify-between p-3.5 bg-slate-50 rounded-xl border border-slate-200 text-xs">
              <div>
                <span className="text-slate-500">Trạng thái chuyến: </span>
                <span className="font-extrabold text-slate-900">{selectedTrip.status}</span>
                <span className="mx-2">•</span>
                <span className="text-slate-500">Tiến độ: </span>
                <span className="font-bold text-emerald-600">
                  {selectedTrip.deliveredOrders} / {selectedTrip.totalOrders} điểm dừng hoàn tất
                </span>
              </div>

              <div className="flex items-center gap-2">
                {selectedTrip.status === 'Preparing' && (
                  <button
                    onClick={() => handleStartTrip(selectedTrip.id)}
                    className="px-3 py-1.5 bg-blue-600 text-white rounded-lg font-bold hover:bg-blue-700 transition-all text-xs"
                  >
                    Bắt Đầu Chuyến (Xuất Bến)
                  </button>
                )}
                {selectedTrip.status === 'InTransit' &&
                  selectedTrip.deliveredOrders === selectedTrip.totalOrders && (
                    <button
                      onClick={() => handleCompleteTrip(selectedTrip.id)}
                      className="px-3 py-1.5 bg-emerald-600 text-white rounded-lg font-bold hover:bg-emerald-700 transition-all text-xs"
                    >
                      Chốt Hoàn Tất Chuyến
                    </button>
                  )}
              </div>
            </div>

            {/* DANH SÁCH ĐIỂM DỪNG */}
            <div>
              <h4 className="text-xs font-bold text-slate-700 uppercase mb-2">
                Danh Sách Các Điểm Dừng / Đơn Hàng:
              </h4>

              <div className="space-y-2">
                {selectedTrip.orders.map((stop, idx) => (
                  <div
                    key={stop.id}
                    className={`p-3 rounded-xl border flex items-center justify-between gap-3 text-xs ${
                      stop.status === 'Delivered'
                        ? 'bg-emerald-50/50 border-emerald-200'
                        : 'bg-white border-slate-200'
                    }`}
                  >
                    <div className="flex items-start gap-3">
                      <span className="w-6 h-6 rounded-full bg-slate-100 font-bold flex items-center justify-center text-slate-600 shrink-0 text-[11px]">
                        {idx + 1}
                      </span>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-extrabold text-slate-900">{stop.orderCode}</span>
                          <span className="text-slate-500">
                            • {stop.receiverName} ({stop.receiverPhone})
                          </span>
                        </div>
                        <p className="text-slate-500 text-[11px] mt-0.5">{stop.deliveryAddress}</p>
                        <span className="text-[11px] font-semibold text-slate-700 mt-0.5 block">
                          Thu tiền COD: {fmtVnd(stop.totalAmount)}
                        </span>
                      </div>
                    </div>

                    <div>
                      {stop.status === 'Delivered' ? (
                        <span className="inline-flex items-center gap-1 text-emerald-700 font-bold text-xs bg-emerald-100 px-2.5 py-1 rounded-full">
                          <CheckCircle2 className="w-3.5 h-3.5" /> Đã Giao
                        </span>
                      ) : (
                        <button
                          onClick={() => handleMarkOrderDelivered(selectedTrip.id, stop.orderId)}
                          className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-bold text-xs shadow-xs transition-all"
                        >
                          Xác Nhận Đã Giao
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end pt-3 border-t border-slate-100">
              <button
                onClick={() => setTripModalOpen(false)}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-xl text-xs font-bold hover:bg-slate-200"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
