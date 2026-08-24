import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlidersHorizontal, Plus, Trash2, Save, AlertCircle, Calendar } from 'lucide-react';

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
  InventoryAdjustmentTypeLabels,
} from '../../types/inventoryAdjustment';
import { ProductBatch } from '../../types/productBatch';

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
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // Dropdowns
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
  const [batches, setBatches] = useState<ProductBatch[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // Form States
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [reason, setReason] = useState<InventoryAdjustmentReason>(
    InventoryAdjustmentReason.Spoilage
  );
  const [note, setNote] = useState('');

  const [details, setDetails] = useState<DetailRow[]>([
    {
      variantId: '',
      batchId: '',
      uoMId: '',
      adjustmentType: InventoryAdjustmentType.MoveToDamaged,
      quantity: 1,
      unitPrice: 0,
      reasonDetail: '',
    },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const loadInitData = useCallback(async () => {
    try {
      const [whList, varList, batchList, uomList] = await Promise.all([
        warehouseApi.getAllList().catch(() => []),
        productVariantApi.getAllList().catch(() => []),
        productBatchApi.getAllList().catch(() => []),
        uomApi.getAllList().catch(() => []),
      ]);

      setWarehouses(whList.map((w: any) => ({ value: w.id, label: w.name })));
      setVariants(
        varList.map((v: any) => ({
          value: v.id,
          label: `${v.code} - ${v.name}`,
          prices: v.prices || [],
        }))
      );
      setBatches(batchList || []);
      setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
    } catch (err) {
      showToast('error', 'Lỗi tải danh mục bổ trợ!');
    }
  }, []);

  useEffect(() => {
    loadInitData();
  }, [loadInitData]);

  const handleAddRow = () => {
    setDetails([
      ...details,
      {
        variantId: '',
        batchId: '',
        uoMId: '',
        adjustmentType: InventoryAdjustmentType.MoveToDamaged,
        quantity: 1,
        unitPrice: 0,
        reasonDetail: '',
      },
    ]);
  };

  const handleRemoveRow = (index: number) => {
    if (details.length === 1) {
      showToast('warning', 'Phiếu điều chỉnh phải có ít nhất 1 dòng chi tiết!');
      return;
    }
    const updated = [...details];
    updated.splice(index, 1);
    setDetails(updated);
  };

  const handleDetailChange = (index: number, field: keyof DetailRow, value: any) => {
    const updated = [...details];
    updated[index] = { ...updated[index], [field]: value };

    // Tự động gán ĐVT và Đơn giá khi chọn Biến thể
    if (field === 'variantId' && value) {
      const v = variants.find((item) => item.value === Number(value));
      if (v && v.prices && v.prices.length > 0) {
        const defPrice = v.prices.find((p) => p.isDefault) || v.prices[0];
        if (defPrice) {
          updated[index].uoMId = defPrice.uoMId || '';
          updated[index].unitPrice = defPrice.price || 0;
        }
      }
      // Reset Lô khi đổi SP
      updated[index].batchId = '';
    }

    setDetails(updated);

    // Xóa lỗi trường tương ứng
    if (errors[`${field}_${index}`]) {
      setErrors((prev) => {
        const e = { ...prev };
        delete e[`${field}_${index}`];
        return e;
      });
    }
  };

  const calculateTotal = () => {
    return details.reduce(
      (sum, row) => sum + Number(row.quantity || 0) * Number(row.unitPrice || 0),
      0
    );
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!warehouseId) errs.warehouseId = 'Vui lòng chọn kho xảy ra biến động';

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 dòng điều chỉnh';
    } else {
      details.forEach((d, idx) => {
        if (!d.variantId) errs[`variantId_${idx}`] = 'Chọn SP';
        if (!d.batchId) errs[`batchId_${idx}`] = 'Chọn Lô';
        if (!d.uoMId) errs[`uoMId_${idx}`] = 'Chọn ĐVT';
        if (Number(d.quantity) <= 0) errs[`quantity_${idx}`] = 'SL > 0';
        if (Number(d.unitPrice) < 0) errs[`unitPrice_${idx}`] = 'Giá >= 0';
      });
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate())
      return showToast('warning', 'Vui lòng điền đầy đủ các trường bắt buộc màu đỏ!');

    try {
      setLoading(true);
      const payload: InventoryAdjustmentCreatePayload = {
        warehouseId: Number(warehouseId),
        reason: Number(reason),
        createdById: userInfo?.id || 1,
        note: (note || '').trim(),
        details: details.map((d) => ({
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          adjustmentType: Number(d.adjustmentType),
          quantity: Number(d.quantity),
          unitPrice: Number(d.unitPrice),
          reasonDetail: (d.reasonDetail || '').trim(),
        })),
      };

      const res = await inventoryAdjustmentApi.create(payload);
      showToast('success', 'TẠO PHIẾU ĐIỀU CHỈNH THÀNH CÔNG! VUI LÒNG DUYỆT ĐỂ CẬP NHẬT KHO.');

      const targetId = res?.id || (res as any)?.Id;
      if (targetId) {
        setTimeout(() => navigate(`/inventory-adjustments/${targetId}`), 1200);
      } else {
        setTimeout(() => navigate('/inventory-adjustments'), 1200);
      }
    } catch (err: any) {
      console.error('Create adjustment error:', err);
      showToast('error', err.response?.data?.message || 'Không thể tạo phiếu điều chỉnh!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho"
        subtitle="Xử lý hao hụt tự nhiên, hư hỏng, cách ly hàng hỏng, hoặc tiêu hủy nông sản"
        icon={SlidersHorizontal}
        onBack={() => navigate('/inventory-adjustments')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          {/* 1. THÔNG TIN CHUNG */}
          <FormSection title="1. Thông Tin Phiếu Điều Chỉnh">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormSelect
                label="Kho hàng xảy ra biến động"
                value={warehouseId}
                onSelect={(val) => {
                  setWarehouseId(val ? Number(val) : '');
                  setErrors((prev) => ({ ...prev, warehouseId: '' }));
                }}
                options={warehouses}
                error={errors.warehouseId}
                required
                showSearch
                placeholder="-- Chọn kho hàng --"
              />

              <FormSelect
                label="Lý do điều chỉnh"
                value={reason}
                onSelect={(val) => setReason(Number(val) as InventoryAdjustmentReason)}
                options={Object.keys(InventoryAdjustmentReasonLabels).map((key) => ({
                  value: Number(key),
                  label: InventoryAdjustmentReasonLabels[Number(key) as InventoryAdjustmentReason],
                }))}
                required
              />

              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú & Biên bản giải trình"
                  value={note}
                  onChange={(e: any) => setNote(e.target.value)}
                  rows={2}
                  placeholder="Diễn giải nguyên nhân (VD: Hàng bị dập nát do rơi vỡ trong ca bốc dỡ sáng nay...)"
                />
              </div>
            </div>
          </FormSection>

          {/* 2. CHI TIẾT BIẾN ĐỘNG */}
          <FormSection title="2. Danh Sách Mặt Hàng Cần Điều Chỉnh">
            {errors.details && (
              <div className="mb-4 p-3 bg-rose-50 text-rose-600 text-sm font-bold rounded-lg border border-rose-200 flex items-center gap-2">
                <AlertCircle size={16} />
                <span>{errors.details}</span>
              </div>
            )}

            <div className="border border-slate-200 rounded-xl overflow-hidden bg-white shadow-sm">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm border-collapse min-w-237.5">
                  <thead className="bg-slate-50 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                    <tr>
                      <th className="py-3 px-3 text-center w-10">#</th>
                      <th className="py-3 px-3 w-[26%]">
                        Sản Phẩm (SKU) <span className="text-red-500">*</span>
                      </th>
                      <th className="py-3 px-3 w-[22%]">
                        Lô Hàng Nông Sản <span className="text-red-500">*</span>
                      </th>
                      <th className="py-3 px-3 w-[22%]">
                        Loại Điều Chỉnh <span className="text-red-500">*</span>
                      </th>
                      <th className="py-3 px-2 w-[10%] text-center">
                        ĐVT <span className="text-red-500">*</span>
                      </th>
                      <th className="py-3 px-2 w-[10%] text-center">
                        Số Lượng <span className="text-red-500">*</span>
                      </th>
                      <th className="py-3 px-3 w-[10%] text-right">Đơn Giá</th>
                      <th className="py-3 px-2 text-center w-12">Xóa</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {details.map((row, idx) => {
                      const rowBatches = batches.filter(
                        (b) => b.variantId === Number(row.variantId)
                      );
                      const selectedBatch = batches.find((b) => b.id === Number(row.batchId));

                      return (
                        <tr key={idx} className="hover:bg-slate-50/60 transition-colors">
                          <td className="py-3 px-3 text-center text-slate-400 font-medium">
                            {idx + 1}
                          </td>

                          {/* SẢN PHẨM */}
                          <td className="py-3 px-3">
                            <select
                              value={row.variantId}
                              onChange={(e) => handleDetailChange(idx, 'variantId', e.target.value)}
                              className={`w-full px-3 py-2 border rounded-lg text-xs font-semibold focus:ring-2 focus:ring-yellow-400 outline-none bg-white ${
                                errors[`variantId_${idx}`]
                                  ? 'border-rose-400 bg-rose-50/30'
                                  : 'border-slate-300'
                              }`}
                            >
                              <option value="">-- Chọn sản phẩm --</option>
                              {variants.map((v) => (
                                <option key={v.value} value={v.value}>
                                  {v.label}
                                </option>
                              ))}
                            </select>
                            {errors[`variantId_${idx}`] && (
                              <span className="text-[11px] text-rose-500 font-bold block mt-0.5">
                                {errors[`variantId_${idx}`]}
                              </span>
                            )}
                          </td>

                          {/* LÔ HÀNG */}
                          <td className="py-3 px-3">
                            <select
                              value={row.batchId}
                              onChange={(e) => handleDetailChange(idx, 'batchId', e.target.value)}
                              disabled={!row.variantId}
                              className={`w-full px-3 py-2 border rounded-lg text-xs font-bold focus:ring-2 focus:ring-yellow-400 outline-none disabled:bg-slate-100 ${
                                errors[`batchId_${idx}`]
                                  ? 'border-rose-400 bg-rose-50/30 text-rose-700'
                                  : 'border-slate-300 text-indigo-700'
                              }`}
                            >
                              <option value="">
                                {row.variantId ? '-- Chọn Lô Hàng --' : '-- Chọn SP trước --'}
                              </option>
                              {rowBatches.map((b) => (
                                <option key={b.id} value={b.id}>
                                  {b.batchCode} (HSD:{' '}
                                  {b.expiryDate
                                    ? new Date(b.expiryDate).toLocaleDateString('vi-VN')
                                    : 'N/A'}
                                  )
                                </option>
                              ))}
                            </select>
                            {selectedBatch?.expiryDate && (
                              <div className="flex items-center gap-1 mt-1 text-[11px] text-slate-500">
                                <Calendar size={12} className="text-amber-600" />
                                <span>
                                  HSD:{' '}
                                  {new Date(selectedBatch.expiryDate).toLocaleDateString('vi-VN')}
                                </span>
                              </div>
                            )}
                            {errors[`batchId_${idx}`] && (
                              <span className="text-[11px] text-rose-500 font-bold block mt-0.5">
                                {errors[`batchId_${idx}`]}
                              </span>
                            )}
                          </td>

                          {/* LOẠI ĐIỀU CHỈNH */}
                          <td className="py-3 px-3">
                            <select
                              value={row.adjustmentType}
                              onChange={(e) =>
                                handleDetailChange(idx, 'adjustmentType', Number(e.target.value))
                              }
                              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs font-bold text-slate-800 focus:ring-2 focus:ring-yellow-400 outline-none bg-white"
                            >
                              {Object.keys(InventoryAdjustmentTypeLabels).map((key) => (
                                <option key={key} value={key}>
                                  {
                                    InventoryAdjustmentTypeLabels[
                                      Number(key) as InventoryAdjustmentType
                                    ]
                                  }
                                </option>
                              ))}
                            </select>
                            <input
                              type="text"
                              placeholder="Lý do chi tiết dòng..."
                              value={row.reasonDetail}
                              onChange={(e) =>
                                handleDetailChange(idx, 'reasonDetail', e.target.value)
                              }
                              className="w-full px-2 py-1 mt-1 border border-slate-200 rounded text-[11px] focus:ring-1 focus:ring-yellow-400 outline-none"
                            />
                          </td>

                          {/* ĐVT */}
                          <td className="py-3 px-2 text-center">
                            <select
                              value={row.uoMId}
                              onChange={(e) => handleDetailChange(idx, 'uoMId', e.target.value)}
                              className={`w-full px-2 py-2 border rounded-lg text-xs font-medium focus:ring-2 focus:ring-yellow-400 outline-none bg-white ${
                                errors[`uoMId_${idx}`] ? 'border-rose-400' : 'border-slate-300'
                              }`}
                            >
                              <option value="">-- ĐVT --</option>
                              {uoms.map((u) => (
                                <option key={u.value} value={u.value}>
                                  {u.label}
                                </option>
                              ))}
                            </select>
                            {errors[`uoMId_${idx}`] && (
                              <span className="text-[10px] text-rose-500 font-bold block mt-0.5">
                                {errors[`uoMId_${idx}`]}
                              </span>
                            )}
                          </td>

                          {/* SỐ LƯỢNG */}
                          <td className="py-3 px-2 text-center">
                            <input
                              type="number"
                              min="0.01"
                              step="any"
                              value={row.quantity}
                              onChange={(e) =>
                                handleDetailChange(idx, 'quantity', parseFloat(e.target.value) || 0)
                              }
                              className={`w-full px-2 py-2 border rounded-lg text-xs text-center font-black focus:ring-2 focus:ring-yellow-400 outline-none bg-white ${
                                errors[`quantity_${idx}`]
                                  ? 'border-rose-400 text-rose-700'
                                  : 'border-slate-300 text-slate-800'
                              }`}
                            />
                            {errors[`quantity_${idx}`] && (
                              <span className="text-[10px] text-rose-500 font-bold block mt-0.5">
                                {errors[`quantity_${idx}`]}
                              </span>
                            )}
                          </td>

                          {/* ĐƠN GIÁ */}
                          <td className="py-3 px-3 text-right">
                            <input
                              type="number"
                              min="0"
                              value={row.unitPrice}
                              onChange={(e) =>
                                handleDetailChange(
                                  idx,
                                  'unitPrice',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              className="w-full px-2 py-2 border border-slate-300 rounded-lg text-xs text-right font-medium focus:ring-2 focus:ring-yellow-400 outline-none bg-white"
                            />
                            <div className="text-[11px] font-bold text-slate-600 mt-1">
                              ={' '}
                              {((row.quantity || 0) * (row.unitPrice || 0)).toLocaleString('vi-VN')}{' '}
                              ₫
                            </div>
                          </td>

                          {/* XÓA DÒNG */}
                          <td className="py-3 px-2 text-center">
                            <button
                              type="button"
                              onClick={() => handleRemoveRow(idx)}
                              className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                              title="Xóa dòng"
                            >
                              <Trash2 size={16} />
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                  <tfoot className="bg-slate-50/80 font-medium border-t border-slate-200">
                    <tr>
                      <td
                        colSpan={5}
                        className="px-4 py-3.5 text-right text-slate-600 text-xs font-bold uppercase tracking-wider"
                      >
                        Tổng Giá Trị Điều Chỉnh Dự Kiến:
                      </td>
                      <td
                        colSpan={3}
                        className="px-4 py-3.5 text-left text-blue-700 font-black text-base"
                      >
                        {calculateTotal().toLocaleString('vi-VN')} ₫
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <div className="p-3 bg-slate-50/50 border-t border-slate-200 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-4 py-2 text-xs font-bold text-indigo-600 bg-indigo-50 hover:bg-indigo-100 border border-indigo-200/60 rounded-lg transition-colors cursor-pointer"
                >
                  <Plus size={15} strokeWidth={2.5} /> THÊM MẶT HÀNG
                </button>
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-4 border-t border-slate-100">
            <SubmitButton loading={loading} isEditMode={false} icon={Save} />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default InventoryAdjustmentForm;
