import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlidersHorizontal, Plus, Trash2 } from 'lucide-react';

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

import { inventoryAdjustmentApi } from '../../api/inventoryAdjustmentApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

import {
  InventoryAdjustmentCreatePayload,
  InventoryAdjustmentReason,
  InventoryAdjustmentReasonLabels,
  InventoryAdjustmentType,
  InventoryAdjustmentTypeLabels
} from '../../types/inventoryAdjustment';

interface DetailRow {
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  adjustmentType: InventoryAdjustmentType;
  quantity: number;
  unitPrice: number;
  reasonDetail: string;
}

const InventoryAdjustmentForm: React.FC = () => {
  const navigate = useNavigate();
  const { userInfo } = useAuthStore();

  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'warning' | 'error'; message: string }>({
    show: false,
    type: 'success',
    message: '',
  });

  // Dropdowns
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
  const [batches, setBatches] = useState<{ id: number; variantId: number; batchCode: string }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // Form States
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [reason, setReason] = useState<InventoryAdjustmentReason>(InventoryAdjustmentReason.Spoilage);
  const [note, setNote] = useState('');

  const [details, setDetails] = useState<DetailRow[]>([
    {
      variantId: '',
      batchId: '',
      uoMId: '',
      adjustmentType: InventoryAdjustmentType.DecreaseAvailable,
      quantity: 1,
      unitPrice: 0,
      reasonDetail: ''
    }
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
  };

  useEffect(() => {
    const loadInit = async () => {
      try {
        const [whList, varList, batchList, uomList] = await Promise.all([
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          productBatchApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
        ]);

        setWarehouses(whList.map((w: any) => ({ value: w.id, label: w.name })));
        setVariants(varList.map((v: any) => ({ value: v.id, label: `${v.code} - ${v.name}`, prices: v.prices || [] })));
        setBatches(batchList.map((b: any) => ({ id: b.id, variantId: b.variantId, batchCode: b.batchCode })));
        setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
      } catch (err) {
        showToast('error', 'Lỗi tải danh mục bổ trợ!');
      }
    };
    loadInit();
  }, []);

  const handleAddRow = () => {
    setDetails([
      ...details,
      {
        variantId: '',
        batchId: '',
        uoMId: '',
        adjustmentType: InventoryAdjustmentType.DecreaseAvailable,
        quantity: 1,
        unitPrice: 0,
        reasonDetail: ''
      }
    ]);
  };

  const handleRemoveRow = (index: number) => {
    const updated = [...details];
    updated.splice(index, 1);
    setDetails(updated);
  };

  const handleDetailChange = (index: number, field: keyof DetailRow, value: any) => {
    const updated = [...details];
    updated[index] = { ...updated[index], [field]: value };

    if (field === 'variantId' && value) {
      const v = variants.find(item => item.value === Number(value));
      if (v && v.prices && v.prices.length > 0) {
        const defPrice = v.prices.find(p => p.isDefault) || v.prices[0];
        if (defPrice) {
          updated[index].uoMId = defPrice.uoMId;
          updated[index].unitPrice = defPrice.price;
        }
      }
    }

    setDetails(updated);
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!warehouseId) errs.warehouseId = 'Vui lòng chọn kho';

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 dòng điều chỉnh';
    } else {
      details.forEach((d, idx) => {
        if (!d.variantId) errs[`variantId_${idx}`] = 'Chọn SP';
        if (!d.batchId) errs[`batchId_${idx}`] = 'Chọn Lô';
        if (!d.uoMId) errs[`uoMId_${idx}`] = 'Chọn ĐVT';
        if (Number(d.quantity) <= 0) errs[`quantity_${idx}`] = '> 0';
      });
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin!');

    try {
      setLoading(true);
      const payload: InventoryAdjustmentCreatePayload = {
        warehouseId: Number(warehouseId),
        reason,
        createdById: userInfo?.id || 1,
        note: note.trim(),
        details: details.map(d => ({
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          adjustmentType: d.adjustmentType,
          quantity: Number(d.quantity),
          unitPrice: Number(d.unitPrice),
          reasonDetail: d.reasonDetail.trim()
        }))
      };

      const res = await inventoryAdjustmentApi.create(payload);
      showToast('success', 'TẠO PHIẾU ĐIỀU CHỈNH THÀNH CÔNG! VUI LÒNG DUYỆT ĐỂ CẬP NHẬT KHO.');
      setTimeout(() => navigate(`/inventory-adjustments/${res.id}`), 1200);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể tạo phiếu điều chỉnh!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Phiếu Điều Chỉnh Tồn Kho"
        subtitle="Xử lý hao hụt, hư hỏng, chuyển hàng hỏng hoặc xuất hủy"
        icon={SlidersHorizontal}
        onBack={() => navigate('/inventory-adjustments')}
      />

      <FormCard>
        <form onSubmit={handleSubmit}>
          <FormSection title="1. Thông Tin Phiếu Điều Chỉnh">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormSelect
                label="Kho hàng"
                value={warehouseId}
                onSelect={(val) => setWarehouseId(val ? Number(val) : '')}
                options={warehouses}
                error={errors.warehouseId}
                required
              />

              <FormSelect
                label="Lý do điều chỉnh"
                value={reason}
                onSelect={(val) => setReason(Number(val) as InventoryAdjustmentReason)}
                options={Object.keys(InventoryAdjustmentReasonLabels).map(key => ({
                  value: Number(key),
                  label: InventoryAdjustmentReasonLabels[Number(key) as InventoryAdjustmentReason]
                }))}
                required
              />

              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú & Biên bản giải trình"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  rows={2}
                  placeholder="Hàng bị dập nát do rơi vỡ trong ca bốc xếp sáng nay..."
                />
              </div>
            </div>
          </FormSection>

          <FormSection title="2. Chi Tiết Mặt Hàng Cần Điều Chỉnh">
            {errors.details && (
              <div className="mb-4 text-rose-500 text-sm font-bold">{errors.details}</div>
            )}

            <div className="mb-4 flex justify-end">
              <button
                type="button"
                onClick={handleAddRow}
                className="flex items-center gap-2 px-4 py-2 bg-indigo-50 text-indigo-600 hover:bg-indigo-100 rounded-lg text-sm font-bold transition-colors"
              >
                <Plus size={16} /> Thêm mặt hàng
              </button>
            </div>

            <div className="overflow-x-auto border border-slate-200 rounded-xl shadow-sm mb-6">
              <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-slate-50 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3 text-center w-12">#</th>
                    <th className="px-4 py-3 min-w-48">Sản phẩm</th>
                    <th className="px-4 py-3 min-w-44">Lô Hàng</th>
                    <th className="px-4 py-3 min-w-56">Loại điều chỉnh</th>
                    <th className="px-4 py-3 min-w-28 text-center">ĐVT</th>
                    <th className="px-4 py-3 w-28 text-center">Số lượng</th>
                    <th className="px-4 py-3 w-32 text-right">Đơn giá</th>
                    <th className="px-4 py-3 min-w-40">Giải trình chi tiết</th>
                    <th className="px-4 py-3 w-16 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches = batches.filter(b => b.variantId === Number(row.variantId));
                    return (
                      <tr key={idx} className="hover:bg-slate-50/50">
                        <td className="px-4 py-3 text-center text-slate-400">{idx + 1}</td>
                        <td className="px-4 py-3">
                          <select
                            value={row.variantId}
                            onChange={(e) => {
                              handleDetailChange(idx, 'variantId', e.target.value);
                              handleDetailChange(idx, 'batchId', '');
                            }}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-indigo-400 outline-none"
                          >
                            <option value="">-- Chọn sản phẩm --</option>
                            {variants.map(v => <option key={v.value} value={v.value}>{v.label}</option>)}
                          </select>
                        </td>
                        <td className="px-4 py-3">
                          <select
                            value={row.batchId}
                            onChange={(e) => handleDetailChange(idx, 'batchId', e.target.value)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm font-bold text-indigo-700 focus:ring-2 focus:ring-indigo-400 outline-none"
                            disabled={!row.variantId}
                          >
                            <option value="">-- Chọn Lô --</option>
                            {rowBatches.map(b => (
                              <option key={b.id} value={b.id}>{b.batchCode}</option>
                            ))}
                          </select>
                        </td>
                        <td className="px-4 py-3">
                          <select
                            value={row.adjustmentType}
                            onChange={(e) => handleDetailChange(idx, 'adjustmentType', Number(e.target.value))}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-xs font-bold text-slate-800 focus:ring-2 focus:ring-indigo-400 outline-none"
                          >
                            {Object.keys(InventoryAdjustmentTypeLabels).map(key => (
                              <option key={key} value={key}>
                                {InventoryAdjustmentTypeLabels[Number(key) as InventoryAdjustmentType]}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-4 py-3">
                          <select
                            value={row.uoMId}
                            onChange={(e) => handleDetailChange(idx, 'uoMId', e.target.value)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:ring-2 focus:ring-indigo-400 outline-none"
                          >
                            <option value="">-- ĐVT --</option>
                            {uoms.map(u => <option key={u.value} value={u.value}>{u.label}</option>)}
                          </select>
                        </td>
                        <td className="px-4 py-3">
                          <input
                            type="number"
                            min="0.01"
                            step="any"
                            value={row.quantity}
                            onChange={(e) => handleDetailChange(idx, 'quantity', parseFloat(e.target.value) || 0)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm text-center font-bold focus:ring-2 focus:ring-indigo-400 outline-none"
                          />
                        </td>
                        <td className="px-4 py-3">
                          <input
                            type="number"
                            min="0"
                            value={row.unitPrice}
                            onChange={(e) => handleDetailChange(idx, 'unitPrice', parseFloat(e.target.value) || 0)}
                            className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm text-right focus:ring-2 focus:ring-indigo-400 outline-none"
                          />
                        </td>
                        <td className="px-4 py-3">
                          <input
                            type="text"
                            placeholder="Chi tiết lý do..."
                            value={row.reasonDetail}
                            onChange={(e) => handleDetailChange(idx, 'reasonDetail', e.target.value)}
                            className="w-full px-2 py-1 border border-slate-200 rounded text-xs focus:ring-2 focus:ring-indigo-400 outline-none"
                          />
                        </td>
                        <td className="px-4 py-3 text-center">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(idx)}
                            className="p-1.5 text-rose-500 hover:bg-rose-50 rounded-lg transition-colors"
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </FormSection>

          <div className="flex justify-end pt-6">
            <SubmitButton loading={loading} isEditMode={false} />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default InventoryAdjustmentForm;
