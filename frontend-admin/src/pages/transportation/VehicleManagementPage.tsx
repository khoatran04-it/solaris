import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Truck,
  Bike,
  Plus,
  ArrowLeft,
  RefreshCw,
  Edit2,
  Trash2,
  Search,
  Snowflake,
  Building2,
  User,
} from 'lucide-react';
import { vehicleApi } from '../../api/vehicleApi';
import { warehouseApi } from '../../api/warehouseApi';
import { DeliveryVehicle, DeliveryVehiclePayload } from '../../types/vehicle';
import { Toast } from '../../components/commons/Toast';
import { StatusBadge } from '../../components/commons/Badge';
import { FormLabel, FormSelect } from '../../components/commons/FormUI';

export default function VehicleManagementPage() {
  const navigate = useNavigate();

  const [vehicles, setVehicles] = useState<DeliveryVehicle[]>([]);
  const [warehouses, setWarehouses] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Search & Filter
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<'ALL' | 'Motorbike' | 'RefrigeratedTruck'>('ALL');
  const [filterStatus, setFilterStatus] = useState<'ALL' | 'Available' | 'OnTrip' | 'Maintenance'>(
    'ALL'
  );

  // Form Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<DeliveryVehicle | null>(null);
  const [formData, setFormData] = useState<DeliveryVehiclePayload>({
    code: '',
    licensePlate: '',
    vehicleType: 'Motorbike',
    maxWeightKg: 40,
    isColdChainEquipped: true,
    status: 'Available',
    driverName: '',
    driverPhone: '',
    homeWarehouseId: 1,
    isActive: true,
  });
  const [saving, setSaving] = useState(false);

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
      const [vehiclesData, whData] = await Promise.all([
        vehicleApi.getAllVehicles(),
        warehouseApi.getAllList(),
      ]);
      setVehicles(vehiclesData);
      setWarehouses(whData.map((w) => ({ id: w.id, name: w.name })));
    } catch (err) {
      console.error(err);
      showToast('error', 'Lỗi khi tải danh sách phương tiện');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleOpenCreate = () => {
    setEditingVehicle(null);
    setFormData({
      code: `VEH-${Date.now().toString().slice(-4)}`,
      licensePlate: '',
      vehicleType: 'Motorbike',
      maxWeightKg: 40,
      isColdChainEquipped: true,
      status: 'Available',
      driverName: '',
      driverPhone: '',
      homeWarehouseId: warehouses[0]?.id || 1,
      isActive: true,
    });
    setModalOpen(true);
  };

  const handleOpenEdit = (v: DeliveryVehicle) => {
    setEditingVehicle(v);
    setFormData({
      code: v.code,
      licensePlate: v.licensePlate,
      vehicleType: v.vehicleType,
      maxWeightKg: v.maxWeightKg,
      isColdChainEquipped: v.isColdChainEquipped,
      status: v.status,
      driverName: v.driverName || '',
      driverPhone: v.driverPhone || '',
      homeWarehouseId: v.homeWarehouseId || 1,
      isActive: v.isActive,
    });
    setModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.licensePlate.trim()) {
      showToast('warning', 'Vui lòng nhập biển số xe');
      return;
    }

    try {
      setSaving(true);
      if (editingVehicle) {
        await vehicleApi.updateVehicle(editingVehicle.id, formData);
        showToast('success', 'Cập nhật thông tin phương tiện thành công');
      } else {
        await vehicleApi.createVehicle(formData);
        showToast('success', 'Đăng ký phương tiện mới vào đội xe thành công');
      }
      setModalOpen(false);
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi lưu thông tin phương tiện');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa phương tiện này khỏi đội xe?')) return;
    try {
      await vehicleApi.deleteVehicle(id);
      showToast('success', 'Đã xóa phương tiện khỏi hệ thống');
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi xóa phương tiện');
    }
  };

  // Filtered vehicles
  const filteredVehicles = useMemo(() => {
    return vehicles.filter((v) => {
      const matchSearch =
        v.licensePlate.toLowerCase().includes(searchTerm.toLowerCase()) ||
        v.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (v.driverName && v.driverName.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (v.driverPhone && v.driverPhone.includes(searchTerm));

      const matchType = filterType === 'ALL' || v.vehicleType === filterType;
      const matchStatus = filterStatus === 'ALL' || v.status === filterStatus;

      return matchSearch && matchType && matchStatus;
    });
  }, [vehicles, searchTerm, filterType, filterStatus]);

  return (
    <div className="h-full flex flex-col p-6 bg-slate-50/50 overflow-y-auto font-sans">
      <div className="max-w-7xl w-full mx-auto flex flex-col gap-6 pb-12">
        <Toast {...toast} />

        {/* TOP HEADER */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)]">
          <div>
            <div className="flex items-center gap-3">
              <button
                onClick={() => navigate('/transportation/dashboard')}
                className="p-2 hover:bg-slate-100 rounded-xl text-slate-500 hover:text-slate-800 transition-colors"
                title="Quay lại Trung tâm Điều phối"
              >
                <ArrowLeft className="w-5 h-5" />
              </button>
              <div className="flex items-center gap-2.5">
                <span className="p-2.5 bg-yellow-50 text-yellow-600 rounded-xl border border-yellow-200/60">
                  <Truck className="w-6 h-6" />
                </span>
                <div>
                  <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight">
                    Quản Lý Đội Xe Vận Tải
                  </h1>
                  <p className="text-slate-500 text-xs mt-0.5 font-medium">
                    Danh mục xe máy thùng lạnh (B2C Last-mile) và xe tải lạnh chuyên dụng (B2B
                    Transfer)
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={loadData}
              disabled={refreshing}
              className="flex items-center gap-2 px-4 py-2.5 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 rounded-xl text-xs font-bold transition-all shadow-xs disabled:opacity-50"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin' : ''}`} />
              Làm Mới
            </button>
            <button
              onClick={handleOpenCreate}
              className="group bg-yellow-400 hover:bg-yellow-500 text-slate-900 rounded-xl px-5 py-2.5 text-xs flex items-center gap-2 font-extrabold transition-all duration-200 shadow-[0_4px_14px_0_rgba(250,204,21,0.39)] hover:shadow-[0_6px_20px_rgba(250,204,21,0.23)] hover:-translate-y-0.5"
            >
              <Plus className="w-4 h-4" />
              Thêm Phương Tiện
            </button>
          </div>
        </div>

        {/* FILTER & SEARCH BAR */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-4 rounded-2xl border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)]">
          {/* Search Box */}
          <div className="flex items-center bg-slate-50 border border-slate-200 rounded-xl px-3.5 py-2 w-full md:w-80 focus-within:border-yellow-400 focus-within:bg-white focus-within:ring-4 focus-within:ring-yellow-400/20 transition-all duration-200">
            <Search className="w-4 h-4 text-slate-400 mr-2 shrink-0" />
            <input
              type="text"
              placeholder="Tìm theo biển số, tài xế, SĐT..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="border-none outline-none w-full text-xs bg-transparent placeholder-slate-400 text-slate-700 font-medium"
            />
          </div>

          {/* Filter Pills */}
          <div className="flex items-center gap-2 flex-wrap">
            <div className="flex items-center bg-slate-100 p-1 rounded-xl text-xs font-semibold">
              <button
                onClick={() => setFilterType('ALL')}
                className={`px-3 py-1.5 rounded-lg transition-all ${
                  filterType === 'ALL'
                    ? 'bg-white text-slate-900 shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Tất cả ({vehicles.length})
              </button>
              <button
                onClick={() => setFilterType('Motorbike')}
                className={`px-3 py-1.5 rounded-lg transition-all flex items-center gap-1.5 ${
                  filterType === 'Motorbike'
                    ? 'bg-white text-slate-900 shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <Bike className="w-3.5 h-3.5 text-emerald-600" />
                Xe máy
              </button>
              <button
                onClick={() => setFilterType('RefrigeratedTruck')}
                className={`px-3 py-1.5 rounded-lg transition-all flex items-center gap-1.5 ${
                  filterType === 'RefrigeratedTruck'
                    ? 'bg-white text-slate-900 shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                <Truck className="w-3.5 h-3.5 text-amber-600" />
                Xe tải lạnh
              </button>
            </div>

            <div className="flex items-center bg-slate-100 p-1 rounded-xl text-xs font-semibold">
              <button
                onClick={() => setFilterStatus('ALL')}
                className={`px-3 py-1.5 rounded-lg transition-all ${
                  filterStatus === 'ALL'
                    ? 'bg-white text-slate-900 shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Mọi trạng thái
              </button>
              <button
                onClick={() => setFilterStatus('Available')}
                className={`px-3 py-1.5 rounded-lg transition-all text-emerald-700 ${
                  filterStatus === 'Available'
                    ? 'bg-white shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Sẵn sàng
              </button>
              <button
                onClick={() => setFilterStatus('OnTrip')}
                className={`px-3 py-1.5 rounded-lg transition-all text-amber-700 ${
                  filterStatus === 'OnTrip'
                    ? 'bg-white shadow-xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Đang chạy
              </button>
            </div>
          </div>
        </div>

        {/* VEHICLE LIST TABLE */}
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-[0_1px_8px_-2px_rgba(0,0,0,0.05)] overflow-hidden">
          {loading ? (
            <div className="p-16 text-center text-slate-400 text-xs flex flex-col items-center justify-center gap-3">
              <div className="w-7 h-7 border-2 border-slate-900 border-t-transparent rounded-full animate-spin" />
              <span>Đang tải danh sách phương tiện...</span>
            </div>
          ) : filteredVehicles.length === 0 ? (
            <div className="p-16 text-center text-slate-400 text-xs font-medium">
              Không tìm thấy phương tiện nào phù hợp với bộ lọc.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/70 text-slate-500 uppercase font-bold text-[11px] tracking-wider">
                    <th className="py-3.5 px-4">Loại Xe</th>
                    <th className="py-3.5 px-4">Biển Số / Mã</th>
                    <th className="py-3.5 px-4">Tài Xế Phụ Trách</th>
                    <th className="py-3.5 px-4">Kho Trực Thuộc</th>
                    <th className="py-3.5 px-4 text-right">Tải Trọng (Kg)</th>
                    <th className="py-3.5 px-4 text-center">Chuỗi Lạnh</th>
                    <th className="py-3.5 px-4 text-center">Trạng Thái</th>
                    <th className="py-3.5 px-4 text-right">Thao Tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-medium">
                  {filteredVehicles.map((v) => (
                    <tr key={v.id} className="hover:bg-slate-50/70 transition-colors">
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-2">
                          <span
                            className={`p-2 rounded-xl ${
                              v.vehicleType === 'Motorbike'
                                ? 'bg-emerald-50 text-emerald-600 border border-emerald-200/60'
                                : 'bg-amber-50 text-amber-600 border border-amber-200/60'
                            }`}
                          >
                            {v.vehicleType === 'Motorbike' ? (
                              <Bike className="w-4 h-4" />
                            ) : (
                              <Truck className="w-4 h-4" />
                            )}
                          </span>
                          <div>
                            <span className="font-bold text-slate-800 block">
                              {v.vehicleType === 'Motorbike'
                                ? 'Xe máy thùng lạnh'
                                : 'Xe tải lạnh liên kho'}
                            </span>
                            <span className="text-[10px] text-slate-400">
                              {v.vehicleType === 'Motorbike' ? 'B2C Last-mile' : 'B2B Transfer'}
                            </span>
                          </div>
                        </div>
                      </td>
                      <td className="py-3.5 px-4">
                        <span className="font-mono font-black text-slate-900 text-xs block">
                          {v.licensePlate}
                        </span>
                        <span className="text-slate-400 text-[11px] font-mono">{v.code}</span>
                      </td>
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-1.5">
                          <User className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                          <div>
                            <span className="font-bold text-slate-800 block">
                              {v.driverName || 'Chưa gán tài xế'}
                            </span>
                            {v.driverPhone && (
                              <span className="text-slate-400 text-[11px] font-medium block">
                                {v.driverPhone}
                              </span>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-1.5 text-slate-600 font-medium">
                          <Building2 className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                          <span>{v.homeWarehouseName || 'Kho Tổng'}</span>
                        </div>
                      </td>
                      <td className="py-3.5 px-4 text-right">
                        <span className="font-bold text-slate-800 text-xs">
                          {v.maxWeightKg.toLocaleString('vi-VN')} kg
                        </span>
                      </td>
                      <td className="py-3.5 px-4 text-center">
                        {v.isColdChainEquipped ? (
                          <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-blue-50 text-blue-700 border border-blue-200">
                            <Snowflake className="w-3 h-3 text-blue-500" /> Chuỗi lạnh
                          </span>
                        ) : (
                          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-slate-100 text-slate-500">
                            Tiêu chuẩn
                          </span>
                        )}
                      </td>
                      <td className="py-3.5 px-4 text-center">
                        <StatusBadge
                          variant={
                            v.status === 'Available'
                              ? 'emerald'
                              : v.status === 'OnTrip'
                                ? 'amber'
                                : 'rose'
                          }
                          label={
                            v.status === 'Available'
                              ? 'Sẵn sàng'
                              : v.status === 'OnTrip'
                                ? 'Đang chạy'
                                : 'Bảo dưỡng'
                          }
                        />
                      </td>
                      <td className="py-3.5 px-4 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => handleOpenEdit(v)}
                            className="p-1.5 hover:bg-yellow-50 text-slate-500 hover:text-yellow-700 rounded-lg transition-colors"
                            title="Chỉnh sửa phương tiện"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleDelete(v.id)}
                            className="p-1.5 hover:bg-rose-50 text-slate-400 hover:text-rose-600 rounded-lg transition-colors"
                            title="Xóa phương tiện"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* CREATE / EDIT MODAL */}
        {modalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="bg-white rounded-3xl max-w-lg w-full p-6 sm:p-7 shadow-2xl border border-slate-100 space-y-6 animate-in zoom-in-95 duration-200">
              <div className="flex items-center justify-between border-b border-slate-100 pb-4">
                <div className="flex items-center gap-3">
                  <span className="p-2 bg-yellow-50 text-yellow-600 rounded-xl border border-yellow-200/60">
                    <Truck className="w-5 h-5" />
                  </span>
                  <div>
                    <h3 className="font-extrabold text-slate-900 text-base">
                      {editingVehicle ? 'Cập Nhật Phương Tiện' : 'Đăng Ký Phương Tiện Mới'}
                    </h3>
                    <p className="text-slate-400 text-xs">
                      {editingVehicle
                        ? `Mã xe: ${editingVehicle.code}`
                        : 'Khai báo phương tiện vào đội xe vận tải'}
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setModalOpen(false)}
                  className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center text-sm font-bold transition-all"
                >
                  ✕
                </button>
              </div>

              <form onSubmit={handleSave} className="space-y-4 text-xs">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <FormLabel label="Biển Số Xe" required />
                    <input
                      type="text"
                      required
                      placeholder="VD: 59A-123.45"
                      value={formData.licensePlate}
                      onChange={(e) =>
                        setFormData({ ...formData, licensePlate: e.target.value.toUpperCase() })
                      }
                      className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 text-xs font-mono font-bold uppercase outline-none transition-all"
                    />
                  </div>

                  <FormSelect
                    label="Loại Phương Tiện"
                    required
                    value={formData.vehicleType}
                    onSelect={(val) =>
                      setFormData({
                        ...formData,
                        vehicleType: val,
                        maxWeightKg: val === 'Motorbike' ? 40 : 1500,
                      })
                    }
                    options={[
                      { label: 'Xe máy thùng lạnh (B2C)', value: 'Motorbike' },
                      { label: 'Xe tải lạnh liên kho (B2B)', value: 'RefrigeratedTruck' },
                    ]}
                    placeholder="Chọn loại phương tiện..."
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <FormLabel label="Họ Tên Tài Xế" />
                    <input
                      type="text"
                      placeholder="VD: Nguyễn Văn A"
                      value={formData.driverName || ''}
                      onChange={(e) => setFormData({ ...formData, driverName: e.target.value })}
                      className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 text-xs font-medium outline-none transition-all"
                    />
                  </div>

                  <div>
                    <FormLabel label="Số Điện Thoại" />
                    <input
                      type="text"
                      placeholder="VD: 0901234567"
                      value={formData.driverPhone || ''}
                      onChange={(e) => setFormData({ ...formData, driverPhone: e.target.value })}
                      className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 text-xs font-medium outline-none transition-all"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <FormSelect
                    label="Kho Trực Thuộc"
                    required
                    value={formData.homeWarehouseId || 1}
                    onSelect={(val) => setFormData({ ...formData, homeWarehouseId: Number(val) })}
                    options={warehouses.map((wh) => ({
                      label: wh.name,
                      value: wh.id,
                    }))}
                    placeholder="Chọn kho..."
                    showSearch={warehouses.length > 5}
                    searchPlaceholder="Tìm kho..."
                  />

                  <div>
                    <FormLabel label="Tải Trọng Tối Đa (Kg)" required />
                    <input
                      type="number"
                      value={formData.maxWeightKg}
                      onChange={(e) =>
                        setFormData({ ...formData, maxWeightKg: Number(e.target.value) })
                      }
                      className="w-full px-3.5 h-[42px] rounded-xl border border-slate-200 bg-slate-50/50 hover:bg-white focus:bg-white focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20 text-xs font-bold outline-none transition-all"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <FormSelect
                    label="Trạng Thái Xe"
                    required
                    value={formData.status}
                    onSelect={(val) => setFormData({ ...formData, status: val })}
                    options={[
                      { label: 'Sẵn sàng (Available)', value: 'Available' },
                      { label: 'Đang chạy chuyến (OnTrip)', value: 'OnTrip' },
                      { label: 'Đang bảo dưỡng (Maintenance)', value: 'Maintenance' },
                    ]}
                    placeholder="Chọn trạng thái..."
                  />

                  <div className="flex items-center gap-2 pt-6">
                    <input
                      type="checkbox"
                      id="isColdChainEquipped"
                      checked={formData.isColdChainEquipped}
                      onChange={(e) =>
                        setFormData({ ...formData, isColdChainEquipped: e.target.checked })
                      }
                      className="w-4 h-4 rounded text-yellow-500 focus:ring-yellow-400 cursor-pointer accent-amber-500"
                    />
                    <label
                      htmlFor="isColdChainEquipped"
                      className="text-xs font-bold text-slate-700 cursor-pointer select-none flex items-center gap-1"
                    >
                      <Snowflake className="w-3.5 h-3.5 text-blue-500" /> Thùng bảo ôn giữ lạnh
                    </label>
                  </div>
                </div>

                <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
                  <button
                    type="button"
                    onClick={() => setModalOpen(false)}
                    className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl transition-all"
                  >
                    Hủy
                  </button>
                  <button
                    type="submit"
                    disabled={saving}
                    className="group bg-yellow-400 hover:bg-yellow-500 text-slate-900 px-6 py-2.5 rounded-xl text-xs font-extrabold shadow-[0_4px_14px_0_rgba(250,204,21,0.39)] transition-all hover:-translate-y-0.5 disabled:opacity-50"
                  >
                    {saving ? 'Đang lưu...' : editingVehicle ? 'Lưu Thay Đổi' : 'Đăng Ký Xe'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
