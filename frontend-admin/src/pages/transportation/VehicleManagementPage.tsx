import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Truck,
  Bike,
  Plus,
  ArrowLeft,
  RefreshCw,
  Edit2,
  Trash2,
  CheckCircle2,
  AlertTriangle,
  User,
  Phone,
  Weight,
  Layers,
} from 'lucide-react';
import { vehicleApi } from '../../api/vehicleApi';
import { warehouseApi } from '../../api/warehouseApi';
import { DeliveryVehicle, DeliveryVehiclePayload } from '../../types/vehicle';
import { Toast } from '../../components/commons/Toast';

export default function VehicleManagementPage() {
  const navigate = useNavigate();

  const [vehicles, setVehicles] = useState<DeliveryVehicle[]>([]);
  const [warehouses, setWarehouses] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

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
        showToast('success', 'Cập nhật phương tiện thành công!');
      } else {
        await vehicleApi.createVehicle(formData);
        showToast('success', 'Đăng ký phương tiện mới thành công!');
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
    if (!window.confirm('Bạn có chắc chắn muốn xóa phương tiện này?')) return;
    try {
      await vehicleApi.deleteVehicle(id);
      showToast('success', 'Đã xóa phương tiện khỏi hệ thống');
      loadData();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi khi xóa phương tiện');
    }
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <Toast {...toast} />

      {/* TOP HEADER */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-xs">
        <div>
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/transportation/dashboard')}
              className="p-2 hover:bg-slate-100 rounded-xl text-slate-500 transition-colors"
            >
              <ArrowLeft className="w-5 h-5" />
            </button>
            <div className="flex items-center gap-2">
              <span className="p-2 bg-blue-50 text-blue-600 rounded-xl">
                <Truck className="w-6 h-6" />
              </span>
              <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight">
                Quản Lý Danh Sách Đội Xe Nội Bộ
              </h1>
            </div>
          </div>
          <p className="text-slate-500 text-sm mt-1 ml-11">
            Danh mục xe máy thùng lạnh (B2C Last-mile) và xe tải lạnh chuyên dụng (B2B Transfer)
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadData}
            disabled={refreshing}
            className="flex items-center gap-2 px-4 py-2.5 bg-slate-100 text-slate-700 hover:bg-slate-200 rounded-xl text-sm font-bold transition-all disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
            Làm Mới
          </button>
          <button
            onClick={handleOpenCreate}
            className="flex items-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-sm font-bold shadow-sm shadow-blue-200 transition-all"
          >
            <Plus className="w-4 h-4" />
            Thêm Phương Tiện Mới
          </button>
        </div>
      </div>

      {/* VEHICLE LIST TABLE */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        {loading ? (
          <div className="p-12 text-center text-slate-400 text-sm">Đang tải danh sách xe...</div>
        ) : vehicles.length === 0 ? (
          <div className="p-16 text-center text-slate-400 text-sm">
            Chưa có phương tiện nào trong đội xe. Hãy nhấn nút &quot;Thêm Phương Tiện Mới&quot; để đăng ký.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50 text-slate-500 uppercase font-bold">
                  <th className="py-3.5 px-4">Loại Xe</th>
                  <th className="py-3.5 px-4">Biển Số / Mã</th>
                  <th className="py-3.5 px-4">Tài Xế Phụ Trách</th>
                  <th className="py-3.5 px-4">Kho Trực Thuộc</th>
                  <th className="py-3.5 px-4">Tải Trọng (Kg)</th>
                  <th className="py-3.5 px-4 text-center">Trang Bị Lạnh</th>
                  <th className="py-3.5 px-4 text-center">Trạng Thái</th>
                  <th className="py-3.5 px-4 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {vehicles.map((v) => (
                  <tr key={v.id} className="hover:bg-slate-50/70 transition-colors">
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-2">
                        <span
                          className={`p-1.5 rounded-lg ${
                            v.vehicleType === 'Motorbike'
                              ? 'bg-emerald-50 text-emerald-600'
                              : 'bg-blue-50 text-blue-600'
                          }`}
                        >
                          {v.vehicleType === 'Motorbike' ? (
                            <Bike className="w-4 h-4" />
                          ) : (
                            <Truck className="w-4 h-4" />
                          )}
                        </span>
                        <span className="font-bold text-slate-800">
                          {v.vehicleType === 'Motorbike' ? 'Xe máy thùng lạnh' : 'Xe tải lạnh B2B'}
                        </span>
                      </div>
                    </td>
                    <td className="py-3 px-4">
                      <span className="font-extrabold text-slate-900 font-mono text-sm block">
                        {v.licensePlate}
                      </span>
                      <span className="text-slate-400 text-[10px]">{v.code}</span>
                    </td>
                    <td className="py-3 px-4">
                      <span className="font-bold text-slate-800 block">
                        {v.driverName || 'Chưa gán tài xế'}
                      </span>
                      <span className="text-slate-500 text-[11px]">{v.driverPhone}</span>
                    </td>
                    <td className="py-3 px-4 text-slate-600 font-medium">
                      {v.homeWarehouseName || 'Kho Tổng'}
                    </td>
                    <td className="py-3 px-4 font-bold text-slate-800">{v.maxWeightKg} kg</td>
                    <td className="py-3 px-4 text-center">
                      {v.isColdChainEquipped ? (
                        <span className="inline-flex items-center gap-1 text-blue-700 bg-blue-50 border border-blue-200 px-2 py-0.5 rounded-full font-bold text-[10px]">
                          ❄️ Chuỗi lạnh
                        </span>
                      ) : (
                        <span className="text-slate-400 text-[10px]">Thường</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`inline-flex px-2.5 py-1 rounded-full text-[10px] font-bold border ${
                          v.status === 'Available'
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                            : v.status === 'OnTrip'
                            ? 'bg-amber-50 text-amber-700 border-amber-200 animate-pulse'
                            : 'bg-rose-50 text-rose-700 border-rose-200'
                        }`}
                      >
                        {v.status === 'Available'
                          ? 'Sẵn sàng'
                          : v.status === 'OnTrip'
                          ? 'Đang chạy'
                          : 'Bảo dưỡng'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => handleOpenEdit(v)}
                          className="p-1.5 hover:bg-slate-100 text-slate-600 rounded-lg transition-colors"
                          title="Chỉnh sửa"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleDelete(v.id)}
                          className="p-1.5 hover:bg-rose-50 text-rose-600 rounded-lg transition-colors"
                          title="Xóa"
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
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-xs p-4">
          <div className="bg-white rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100 space-y-5">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <h3 className="font-extrabold text-slate-900 text-base flex items-center gap-2">
                <Truck className="w-5 h-5 text-blue-600" />
                {editingVehicle ? 'Cập Nhật Thông Tin Xe' : 'Đăng Ký Phương Tiện Mới'}
              </h3>
              <button
                onClick={() => setModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSave} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Biển Số Xe *
                  </label>
                  <input
                    type="text"
                    required
                    placeholder="VD: 59A-123.45"
                    value={formData.licensePlate}
                    onChange={(e) =>
                      setFormData({ ...formData, licensePlate: e.target.value.toUpperCase() })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs font-mono font-bold uppercase focus:ring-2 focus:ring-blue-500 outline-none"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Loại Phương Tiện
                  </label>
                  <select
                    value={formData.vehicleType}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        vehicleType: e.target.value,
                        maxWeightKg: e.target.value === 'Motorbike' ? 40 : 1500,
                      })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs font-medium focus:ring-2 focus:ring-blue-500 outline-none"
                  >
                    <option value="Motorbike">🛵 Xe máy thùng lạnh (B2C Last-mile)</option>
                    <option value="RefrigeratedTruck">🚚 Xe tải lạnh liên kho (B2B Transfer)</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Họ Tên Tài Xế
                  </label>
                  <input
                    type="text"
                    placeholder="VD: Nguyễn Văn A"
                    value={formData.driverName || ''}
                    onChange={(e) => setFormData({ ...formData, driverName: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 outline-none"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Số Điện Thoại
                  </label>
                  <input
                    type="text"
                    placeholder="VD: 0901234567"
                    value={formData.driverPhone || ''}
                    onChange={(e) => setFormData({ ...formData, driverPhone: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Kho Trực Thuộc
                  </label>
                  <select
                    value={formData.homeWarehouseId || 1}
                    onChange={(e) =>
                      setFormData({ ...formData, homeWarehouseId: Number(e.target.value) })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 outline-none"
                  >
                    {warehouses.map((wh) => (
                      <option key={wh.id} value={wh.id}>
                        {wh.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Tải Trọng Tối Đa (Kg)
                  </label>
                  <input
                    type="number"
                    value={formData.maxWeightKg}
                    onChange={(e) =>
                      setFormData({ ...formData, maxWeightKg: Number(e.target.value) })
                    }
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-700 uppercase mb-1">
                    Trạng Thái Xe
                  </label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    className="w-full px-3 py-2 border border-slate-300 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 outline-none"
                  >
                    <option value="Available">Sẵn sàng (Available)</option>
                    <option value="OnTrip">Đang chạy chuyến (OnTrip)</option>
                    <option value="Maintenance">Đang bảo dưỡng (Maintenance)</option>
                  </select>
                </div>

                <div className="flex items-center gap-2 pt-6">
                  <input
                    type="checkbox"
                    id="isColdChainEquipped"
                    checked={formData.isColdChainEquipped}
                    onChange={(e) =>
                      setFormData({ ...formData, isColdChainEquipped: e.target.checked })
                    }
                    className="w-4 h-4 rounded text-blue-600"
                  />
                  <label
                    htmlFor="isColdChainEquipped"
                    className="text-xs font-bold text-slate-700 cursor-pointer"
                  >
                    ❄️ Trang bị thùng giữ nhiệt lạnh
                  </label>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="px-4 py-2 text-xs font-bold text-slate-600 hover:bg-slate-100 rounded-xl"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="px-5 py-2 text-xs font-bold bg-blue-600 hover:bg-blue-700 text-white rounded-xl shadow-md transition-all disabled:opacity-50"
                >
                  {saving ? 'Đang lưu...' : 'Lưu Phương Tiện'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
