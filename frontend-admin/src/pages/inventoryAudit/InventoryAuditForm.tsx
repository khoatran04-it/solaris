import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClipboardList, AlertCircle, Save } from 'lucide-react';

import {
  PageContainer,
  FormCard,
  FormSelect,
  FormTextarea,
  SubmitButton,
  FormHeader,
  FormSection,
} from '../../components/commons/FormUI';
import { Toast } from '../../components/commons/Toast';

import { inventoryAuditApi } from '../../api/inventoryAuditApi';
import { warehouseApi } from '../../api/warehouseApi';
import { useAuthStore } from '../../stores/useAuthStore';
import {
  InventoryAuditCreatePayload,
  InventoryAuditType,
  InventoryAuditTypeLabels,
} from '../../types/inventoryAudit';

const InventoryAuditForm: React.FC = () => {
  const navigate = useNavigate();
  const { userInfo } = useAuthStore();

  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [auditType, setAuditType] = useState<InventoryAuditType>(InventoryAuditType.Full);
  const [note, setNote] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  useEffect(() => {
    const loadWarehouses = async () => {
      try {
        const whList = await warehouseApi.getAllList().catch(() => []);
        setWarehouses(whList.map((w: any) => ({ value: w.id, label: w.name })));
      } catch (err) {
        showToast('error', 'Không thể tải danh sách kho!');
      }
    };
    loadWarehouses();
  }, []);

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!warehouseId) errs.warehouseId = 'Vui lòng chọn kho kiểm kê';
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return showToast('warning', 'Vui lòng chọn kho kiểm kê!');

    try {
      setLoading(true);
      const payload: InventoryAuditCreatePayload = {
        warehouseId: Number(warehouseId),
        auditType,
        auditorId: userInfo?.id || 1,
        note: note.trim(),
      };

      const res = await inventoryAuditApi.create(payload);
      showToast('success', 'ĐÃ KHỞI TẠO ĐỢT KIỂM KÊ & CHỤP ẢNH TỒN KHO HỆ THỐNG THÀNH CÔNG!');
      setTimeout(() => navigate(`/inventory-audits/${res.id}`), 1200);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể tạo đợt kiểm kê!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Đợt Kiểm Kê Kho Mới"
        subtitle="Hệ thống sẽ tự động chốt số liệu tồn kho (Snapshot) tại thời điểm này"
        icon={ClipboardList}
        onBack={() => navigate('/inventory-audits')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          <FormSection title="Thông Tin Đợt Kiểm Kê">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormSelect
                label="Kho cần kiểm kê"
                value={warehouseId}
                onSelect={(val) => {
                  setWarehouseId(val ? Number(val) : '');
                  setErrors((prev) => ({ ...prev, warehouseId: '' }));
                }}
                options={warehouses}
                error={errors.warehouseId}
                placeholder="-- Chọn Kho kiểm kê --"
                showSearch
                searchPlaceholder="Tìm kiếm kho..."
                required
              />

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
                  Hình thức kiểm kê <span className="text-red-500">*</span>
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {Object.keys(InventoryAuditTypeLabels).map((key) => {
                    const t = Number(key) as InventoryAuditType;
                    const isSelected = auditType === t;
                    return (
                      <button
                        key={t}
                        type="button"
                        onClick={() => setAuditType(t)}
                        className={`px-3 py-2.5 rounded-xl text-xs font-bold border transition-all text-center cursor-pointer ${
                          isSelected
                            ? 'bg-amber-400 text-slate-950 border-amber-400 shadow-2xs font-black'
                            : 'bg-white text-slate-700 border-slate-200 hover:bg-slate-50'
                        }`}
                      >
                        {InventoryAuditTypeLabels[t]}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú & Chỉ đạo kiểm kê"
                  value={note}
                  onChange={(e: any) => setNote(e.target.value)}
                  rows={3}
                  placeholder="Kiểm kê định kỳ cuối tháng, chú ý đếm kỹ các Lô hàng sát hạn sử dụng..."
                />
              </div>
            </div>

            <div className="mt-6 p-4 bg-amber-50 border border-amber-200 rounded-2xl flex gap-3 items-start text-xs text-amber-900 leading-relaxed shadow-2xs">
              <AlertCircle className="w-5 h-5 shrink-0 text-amber-600 mt-0.5" />
              <div>
                <span className="font-bold">Lưu ý quản trị:</span> Khi nhấn "TẠO MỚI", hệ
                thống sẽ lưu lại toàn bộ số dư tồn kho khả dụng hiện tại làm mốc so sánh (System
                Quantity). Nhân viên đi đếm có thể sử dụng chế độ{' '}
                <strong>Đếm Mù (Blind Count)</strong> để ghi nhận số liệu khách quan nhất.
              </div>
            </div>
          </FormSection>

          {/* FOOTER ACTION BUTTONS */}
          <div className="flex justify-end gap-3 pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/inventory-audits')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-xl hover:bg-slate-50 transition-colors shadow-xs cursor-pointer"
            >
              Hủy Bỏ
            </button>
            <SubmitButton loading={loading} isEditMode={false} icon={Save} />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default InventoryAuditForm;
