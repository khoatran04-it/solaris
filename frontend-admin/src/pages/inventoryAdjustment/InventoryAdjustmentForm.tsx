import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { SlidersHorizontal, Plus, Trash2, Save, AlertCircle, Calendar } from 'lucide-react';

import {
  PageContainer,
  FormCard,
  FormSelect,
  FormInput,
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
import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { uomApi } from '../../api/uomApi';
import { uomConversionApi } from '../../api/uomConversionApi';
import { useAuthStore } from '../../stores/useAuthStore';

import {
  InventoryAdjustmentCreatePayload,
  InventoryAdjustmentReason,
  InventoryAdjustmentReasonLabels,
  InventoryAdjustmentType,
  InventoryAdjustmentTypeLabels,
} from '../../types/inventoryAdjustment';
import { ProductBatch } from '../../types/productBatch';
import { SuggestedBatch } from '../../types/inventoryIssue';

interface DetailRow {
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  adjustmentType: InventoryAdjustmentType;
  quantity: number;
  unitPrice: number;
  reasonDetail: string;
}

const formatBatchLabel = (batchCode: string, expiryDate?: string) => {
  if (!expiryDate) return batchCode;
  try {
    const d = new Date(expiryDate);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${batchCode} (HSD: ${day}/${month}/${year})`;
  } catch {
    return batchCode;
  }
};

const REASON_NOTE_TEMPLATES: Record<number, string> = {
  [InventoryAdjustmentReason.PhysicalAuditSurplus]:
    'Ghi nhận chênh lệch thừa hàng thực tế so với sổ sách sau đợt kiểm kê.',
  [InventoryAdjustmentReason.LossOrTheft]:
    'Ghi nhận hao hụt do thất thoát hoặc mất cắp trong quá trình lưu kho.',
  [InventoryAdjustmentReason.QualityDegradation]:
    'Nông sản bị biến đổi phẩm chất, thối hỏng do điều kiện bảo quản nhiệt độ/độ ẩm.',
  [InventoryAdjustmentReason.DamagedOrBroken]:
    'Hàng bị dập nát, bục rách bao bì trong ca bốc dỡ/vận chuyển nội bộ.',
  [InventoryAdjustmentReason.Expired]:
    'Nông sản/Hàng hóa quá hạn sử dụng (HSD), cần cách ly và tiêu hủy.',
  [InventoryAdjustmentReason.NaturalShrinkage]:
    'Hao hụt trọng lượng tự nhiên theo định mức quy định (bay hơi nước, khô héo).',
  [InventoryAdjustmentReason.DataEntryError]:
    'Điều chỉnh do sai sót nhầm lẫn số liệu trong các kỳ nhập/xuất trước đó.',
};

const QUICK_NOTE_SUGGESTIONS = [
  'Hàng bị dập nát do rơi vỡ trong ca bốc xếp',
  'Hao hụt tự nhiên do bay hơi nước sau 3 ngày lưu kho',
  'Trái cây chín quá độ, xuất hiện đốm nâu hỏng',
  'Bao bì rách hở chân không trong quá trình vận chuyển',
  'Hàng cận date hết hạn theo biên bản kiểm kê',
];

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
  const [variants, setVariants] = useState<
    { value: number; label: string; prices: any[]; baseUoMId?: number }[]
  >([]);
  const [batches, setBatches] = useState<ProductBatch[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);
  const [variantUoMsMap, setVariantUoMsMap] = useState<Record<number, { value: number; label: string }[]>>({});
  const [variantBatchesMap, setVariantBatchesMap] = useState<Record<string, SuggestedBatch[]>>({});

  const fetchValidUoMs = useCallback(async (vId: number) => {
    if (!vId || variantUoMsMap[vId]) return;
    try {
      const opts = await uomConversionApi.getValidUoMs(vId);
      if (opts && opts.length > 0) {
        setVariantUoMsMap((prev) => ({
          ...prev,
          [vId]: opts.map((u) => ({
            value: u.uoMId,
            label: `${u.uoMName} (${u.description})`,
          })),
        }));
      }
    } catch (e) {
      console.error('Lỗi tải ĐVT hợp lệ:', e);
    }
  }, [variantUoMsMap]);

  // Fetch batches cho 1 variant tại kho được chọn
  const fetchBatchesForVariant = useCallback(
    async (whId: number, varId: number, neededQty = 999999) => {
      if (!whId || !varId) return [];
      const key = `${whId}_${varId}`;
      try {
        const res = await inventoryIssueApi.getSuggestedBatches(whId, varId, neededQty);
        setVariantBatchesMap((prev) => ({ ...prev, [key]: res || [] }));
        return res || [];
      } catch {
        setVariantBatchesMap((prev) => ({ ...prev, [key]: [] }));
        return [];
      }
    },
    []
  );

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

  useEffect(() => {
    details.forEach((d) => {
      if (d.variantId) {
        fetchValidUoMs(Number(d.variantId));
      }
    });
  }, [details, fetchValidUoMs]);

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
          baseUoMId: v.baseUoMId,
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

  // Khi Kho hàng thay đổi: tự động fetch lại lô hàng cho các mặt hàng đã chọn
  useEffect(() => {
    if (warehouseId) {
      const whId = Number(warehouseId);
      details.forEach(async (row) => {
        if (row.variantId) {
          const suggestions = await fetchBatchesForVariant(whId, Number(row.variantId));
          if (suggestions && suggestions.length > 0 && !row.batchId) {
            setDetails((prev) =>
              prev.map((r) => (r === row ? { ...r, batchId: suggestions[0].batchId } : r))
            );
          }
        }
      });
    }
  }, [warehouseId, fetchBatchesForVariant]);

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

  const handleDetailChange = async (index: number, field: keyof DetailRow, value: any) => {
    const updated = [...details];
    updated[index] = { ...updated[index], [field]: value };

    // Tự động gán ĐVT và Đơn giá khi chọn Biến thể: Ưu tiên BaseUoMId của sản phẩm
    if (field === 'variantId' && value) {
      fetchValidUoMs(Number(value));
      const v = variants.find((item) => item.value === Number(value));
      if (v) {
        const defaultUoM =
          v.baseUoMId ||
          (v.prices && v.prices.length > 0
            ? (v.prices.find((p: any) => p.isDefault) || v.prices[0])?.uoMId
            : uoms[0]?.value || '');

        if (defaultUoM) {
          updated[index].uoMId = defaultUoM;
        }

        if (v.prices && v.prices.length > 0) {
          const priceObj =
            v.prices.find((p: any) => p.uoMId === defaultUoM) ||
            v.prices.find((p: any) => p.isDefault) ||
            v.prices[0];
          updated[index].unitPrice = priceObj?.price || 0;
        }
      }
      // Reset Lô khi đổi SP
      updated[index].batchId = '';
    }

    setDetails(updated);

    // Tự động load Lô FEFO của đúng SP đó tại kho
    if (field === 'variantId' && value && warehouseId) {
      const suggestions = await fetchBatchesForVariant(Number(warehouseId), Number(value));
      if (suggestions && suggestions.length > 0) {
        setDetails((prev) => {
          const next = [...prev];
          if (next[index]) {
            next[index] = { ...next[index], batchId: suggestions[0].batchId };
          }
          return next;
        });
      }
    }

    // Kiểm tra tồn kho khả dụng ngay khi nhập số lượng hoặc chọn lô hoặc chọn loại điều chỉnh
    const currentRow = details[index];
    const targetBatchId = field === 'batchId' ? Number(value) : Number(currentRow?.batchId);
    const targetQty = field === 'quantity' ? Number(value) : Number(currentRow?.quantity);
    const targetVarId = field === 'variantId' ? Number(value) : Number(currentRow?.variantId);
    const targetAdjType = field === 'adjustmentType' ? Number(value) : Number(currentRow?.adjustmentType);

    if (targetBatchId && warehouseId && targetVarId) {
      const batchesList = variantBatchesMap[`${warehouseId}_${targetVarId}`] || [];
      const batchObj = batchesList.find((b) => b.batchId === targetBatchId);
      const maxAvailable = batchObj ? batchObj.quantityAvailable : 0;

      if (
        targetAdjType === InventoryAdjustmentType.DecreaseAvailable ||
        targetAdjType === InventoryAdjustmentType.MoveToDamaged
      ) {
        if (maxAvailable <= 0) {
          setErrors((prev) => ({ ...prev, [`quantity_${index}`]: 'Lô này đã hết tồn kho khả dụng' }));
        } else if (targetQty > maxAvailable) {
          setErrors((prev) => ({
            ...prev,
            [`quantity_${index}`]: `Tối đa ${maxAvailable} (tồn khả dụng)`,
          }));
        } else {
          setErrors((prev) => {
            const e = { ...prev };
            delete e[`quantity_${index}`];
            return e;
          });
        }
      } else {
        setErrors((prev) => {
          const e = { ...prev };
          delete e[`quantity_${index}`];
          return e;
        });
      }
    }

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

        // Kiểm tra tồn kho khả dụng
        if (warehouseId && d.variantId && d.batchId) {
          const batchesList = variantBatchesMap[`${warehouseId}_${d.variantId}`] || [];
          const batchObj = batchesList.find((b) => b.batchId === Number(d.batchId));
          const maxAvailable = batchObj ? batchObj.quantityAvailable : 0;

          if (
            d.adjustmentType === InventoryAdjustmentType.DecreaseAvailable ||
            d.adjustmentType === InventoryAdjustmentType.MoveToDamaged
          ) {
            if (maxAvailable <= 0) {
              errs[`quantity_${idx}`] = 'Lô này đã hết tồn kho khả dụng';
            } else if (Number(d.quantity) > maxAvailable) {
              errs[`quantity_${idx}`] = `Tối đa ${maxAvailable} (tồn khả dụng)`;
            }
          }
        }
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
                searchPlaceholder="Tìm kiếm kho..."
              />

              <FormSelect
                label="Lý do điều chỉnh"
                value={reason}
                onSelect={(val) => {
                  const newReason = Number(val) as InventoryAdjustmentReason;
                  setReason(newReason);
                  // Tự động điền template ghi chú nếu chưa nhập hoặc đang dùng template cũ
                  if (!note || Object.values(REASON_NOTE_TEMPLATES).includes(note)) {
                    setNote(REASON_NOTE_TEMPLATES[newReason] || '');
                  }
                }}
                options={Object.keys(InventoryAdjustmentReasonLabels).map((key) => ({
                  value: Number(key),
                  label: InventoryAdjustmentReasonLabels[Number(key) as InventoryAdjustmentReason],
                }))}
                required
              />

              <div className="md:col-span-2 space-y-2">
                <FormTextarea
                  label="Ghi chú & Biên bản giải trình"
                  value={note}
                  onChange={(e: any) => setNote(e.target.value)}
                  rows={2}
                  placeholder="Diễn giải nguyên nhân (VD: Hàng bị dập nát do rơi vỡ trong ca bốc dỡ sáng nay...)"
                />

                {/* Gợi ý giải trình mẫu */}
                <div className="flex flex-wrap items-center gap-1.5 pt-1">
                  <span className="text-[11px] font-bold text-slate-500 mr-1">Gợi ý nhanh:</span>
                  {QUICK_NOTE_SUGGESTIONS.map((suggestion, sIdx) => (
                    <button
                      key={sIdx}
                      type="button"
                      onClick={() => {
                        if (!note) {
                          setNote(suggestion);
                        } else {
                          setNote(`${note}; ${suggestion}`);
                        }
                      }}
                      className="px-2.5 py-1 bg-slate-100 hover:bg-amber-100 hover:text-amber-950 border border-slate-200 hover:border-amber-300 rounded-lg text-[11px] font-medium text-slate-700 transition-colors cursor-pointer shadow-2xs"
                    >
                      + {suggestion}
                    </button>
                  ))}
                </div>
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

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[380px] pb-24">
              <table className="w-full text-left text-sm whitespace-nowrap min-w-[950px]">
                <thead className="bg-slate-50/80 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-3 py-3.5 text-center w-10">#</th>
                    <th className="px-3 py-3.5 min-w-[220px]">
                      Sản Phẩm (SKU) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 min-w-[260px]">
                      Lô Hàng Nông Sản <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 min-w-[190px]">
                      Loại Điều Chỉnh <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-24 text-center">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-24 text-center bg-amber-50/50 text-amber-900 border-x border-amber-100/70">
                      Số Lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-28 text-right">Đơn Giá</th>
                    <th className="px-3 py-3.5 w-32 text-right">Thành Tiền</th>
                    <th className="px-3 py-3.5 min-w-[160px]">Lý do chi tiết</th>
                    <th className="px-3 py-3.5 w-12 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches = batches.filter((b) => b.variantId === Number(row.variantId));
                    const suggestedBatches =
                      warehouseId && row.variantId
                        ? variantBatchesMap[`${warehouseId}_${row.variantId}`] || []
                        : [];
                    const currentBatchObj = suggestedBatches.find(
                      (b) => b.batchId === Number(row.batchId)
                    );
                    const batchOptions =
                      suggestedBatches.length > 0
                        ? suggestedBatches.map((b) => ({
                            value: b.batchId,
                            label: `${formatBatchLabel(b.batchCode, b.expiryDate)} (Tồn: ${b.quantityAvailable})`,
                          }))
                        : rowBatches.map((b) => ({
                            value: b.id,
                            label: formatBatchLabel(b.batchCode, b.expiryDate),
                          }));

                    return (
                      <tr
                        key={idx}
                        className="hover:bg-slate-50/60 transition-colors"
                        style={{ zIndex: 50 - idx }}
                      >
                        <td className="px-3 py-3 text-center text-slate-400 font-medium">
                          {idx + 1}
                        </td>

                        {/* SẢN PHẨM */}
                        <td className="p-2 min-w-[220px]">
                          <FormSelect
                            label=""
                            showSearch
                            placeholder="-- Chọn sản phẩm --"
                            searchPlaceholder="Tìm sản phẩm..."
                            options={variants}
                            value={row.variantId}
                            error={errors[`variantId_${idx}`]}
                            onSelect={(val) =>
                              handleDetailChange(idx, 'variantId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        {/* LÔ HÀNG */}
                        <td className="p-2 min-w-[260px]">
                          <FormSelect
                            label=""
                            placeholder="-- Chọn Lô Hàng --"
                            showSearch
                            searchPlaceholder="Tìm mã lô..."
                            options={batchOptions}
                            value={row.batchId}
                            error={errors[`batchId_${idx}`]}
                            disabled={!row.variantId}
                            onSelect={(val) =>
                              handleDetailChange(idx, 'batchId', val ? Number(val) : '')
                            }
                          />
                          {currentBatchObj !== undefined && (
                            <div className="mt-1 flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md w-fit border border-emerald-200/60">
                              <span>Tồn khả dụng: {currentBatchObj.quantityAvailable}</span>
                            </div>
                          )}
                        </td>

                        {/* LOẠI ĐIỀU CHỈNH */}
                        <td className="p-2 min-w-[190px]">
                          <FormSelect
                            label=""
                            options={Object.keys(InventoryAdjustmentTypeLabels).map((key) => ({
                              value: Number(key),
                              label:
                                InventoryAdjustmentTypeLabels[
                                  Number(key) as InventoryAdjustmentType
                                ],
                            }))}
                            value={row.adjustmentType}
                            onSelect={(val) =>
                              handleDetailChange(idx, 'adjustmentType', Number(val))
                            }
                          />
                        </td>

                        {/* ĐVT */}
                        <td className="p-2 w-24">
                          <FormSelect
                            label=""
                            placeholder="ĐVT"
                            options={row.variantId && variantUoMsMap[Number(row.variantId)] ? variantUoMsMap[Number(row.variantId)] : uoms}
                            value={row.uoMId}
                            disabled={Boolean(row.variantId)}
                            error={errors[`uoMId_${idx}`]}
                            onSelect={(val) =>
                              handleDetailChange(idx, 'uoMId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        {/* SỐ LƯỢNG */}
                        <td className="p-2 bg-amber-50/20 border-x border-amber-100/50 w-24">
                          <FormInput
                            label=""
                            type="number"
                            min="0.01"
                            step="any"
                            onFocus={(e) => e.target.select()}
                            className="text-center font-black text-amber-950"
                            value={row.quantity}
                            error={errors[`quantity_${idx}`]}
                            onChange={(e) =>
                              handleDetailChange(idx, 'quantity', parseFloat(e.target.value) || 0)
                            }
                          />
                        </td>

                        {/* ĐƠN GIÁ */}
                        <td className="p-2 w-28">
                          <FormInput
                            label=""
                            type="number"
                            min="0"
                            onFocus={(e) => e.target.select()}
                            className="text-right font-medium text-slate-700"
                            value={row.unitPrice}
                            onChange={(e) =>
                              handleDetailChange(idx, 'unitPrice', parseFloat(e.target.value) || 0)
                            }
                          />
                        </td>

                        {/* THÀNH TIỀN */}
                        <td className="px-3 py-3 text-right font-black text-amber-900 w-32 truncate">
                          {((row.quantity || 0) * (row.unitPrice || 0)).toLocaleString('vi-VN')} đ
                        </td>

                        {/* LÝ DO CHI TIẾT */}
                        <td className="p-2 min-w-[160px]">
                          <FormInput
                            label=""
                            placeholder="Dập nát, hỏng..."
                            value={row.reasonDetail}
                            onChange={(e) =>
                              handleDetailChange(idx, 'reasonDetail', e.target.value)
                            }
                          />
                        </td>

                        {/* XÓA DÒNG */}
                        <td className="p-2 text-center w-12 border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(idx)}
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors disabled:opacity-20 mx-auto cursor-pointer"
                            disabled={details.length === 1}
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
                      colSpan={6}
                      className="px-4 py-3.5 text-right text-slate-600 text-xs font-bold uppercase tracking-wider"
                    >
                      Tổng Giá Trị Điều Chỉnh Dự Kiến:
                    </td>
                    <td
                      colSpan={4}
                      className="px-4 py-3.5 text-left text-amber-900 font-black text-base"
                    >
                      {calculateTotal().toLocaleString('vi-VN')} ₫
                    </td>
                  </tr>
                </tfoot>
              </table>

              {/* Nút Thêm dòng điều chỉnh */}
              <div className="p-3 bg-slate-50/60 border-t border-slate-200/80 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-amber-900 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-xl transition-all shadow-2xs cursor-pointer"
                >
                  <Plus size={16} /> THÊM MẶT HÀNG
                </button>
              </div>
            </div>
          </FormSection>

          {/* ACTION BUTTONS */}
          <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => navigate('/inventory-adjustments')}
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

export default InventoryAdjustmentForm;
