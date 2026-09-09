import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ClipboardList, AlertCircle, Save, Plus, Trash2 } from 'lucide-react';

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
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';
import {
  InventoryAuditCreatePayload,
  InventoryAuditType,
  InventoryAuditTypeLabels,
} from '../../types/inventoryAudit';
import { ProductBatch } from '../../types/productBatch';
import { SuggestedBatch } from '../../types/inventoryIssue';

interface SpecificItemRow {
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
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

  // Dropdown lists
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<
    { value: number; label: string; prices: any[]; baseUoMId?: number }[]
  >([]);
  const [batches, setBatches] = useState<ProductBatch[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);
  const [variantBatchesMap, setVariantBatchesMap] = useState<Record<string, SuggestedBatch[]>>({});

  // Form States
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [auditType, setAuditType] = useState<InventoryAuditType>(InventoryAuditType.Full);
  const [note, setNote] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Specific items for Cycle / Spot
  const [specificItems, setSpecificItems] = useState<SpecificItemRow[]>([
    { variantId: '', batchId: '', uoMId: '' },
  ]);

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const fetchBatchesForVariant = useCallback(async (whId: number, varId: number) => {
    if (!whId || !varId) return [];
    const key = `${whId}_${varId}`;
    try {
      const res = await inventoryIssueApi.getSuggestedBatches(whId, varId, 999999);
      setVariantBatchesMap((prev) => ({ ...prev, [key]: res || [] }));
      return res || [];
    } catch {
      setVariantBatchesMap((prev) => ({ ...prev, [key]: [] }));
      return [];
    }
  }, []);

  useEffect(() => {
    const loadInitData = async () => {
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
        showToast('error', 'Không thể tải danh mục bổ trợ!');
      }
    };
    loadInitData();
  }, []);

  // Khi Kho hàng thay đổi: fetch lại lô cho các mặt hàng đã chọn
  useEffect(() => {
    if (warehouseId) {
      const whId = Number(warehouseId);
      specificItems.forEach(async (row) => {
        if (row.variantId) {
          const suggestions = await fetchBatchesForVariant(whId, Number(row.variantId));
          if (suggestions && suggestions.length > 0 && !row.batchId) {
            setSpecificItems((prev) =>
              prev.map((r) => (r === row ? { ...r, batchId: suggestions[0].batchId } : r))
            );
          }
        }
      });
    }
  }, [warehouseId, fetchBatchesForVariant]);

  const handleAddSpecificItem = () => {
    setSpecificItems([...specificItems, { variantId: '', batchId: '', uoMId: '' }]);
  };

  const handleRemoveSpecificItem = (index: number) => {
    if (specificItems.length === 1) {
      setSpecificItems([{ variantId: '', batchId: '', uoMId: '' }]);
      return;
    }
    const updated = [...specificItems];
    updated.splice(index, 1);
    setSpecificItems(updated);
  };

  const handleSpecificItemChange = async (
    index: number,
    field: keyof SpecificItemRow,
    value: any
  ) => {
    const updated = [...specificItems];
    updated[index] = { ...updated[index], [field]: value };

    if (field === 'variantId' && value) {
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
      }
      updated[index].batchId = '';
    }

    setSpecificItems(updated);

    if (field === 'variantId' && value && warehouseId) {
      const suggestions = await fetchBatchesForVariant(Number(warehouseId), Number(value));
      if (suggestions && suggestions.length > 0) {
        setSpecificItems((prev) => {
          const next = [...prev];
          if (next[index] && !next[index].batchId) {
            next[index] = { ...next[index], batchId: suggestions[0].batchId };
          }
          return next;
        });
      }
    }

    if (errors[`${field}_${index}`]) {
      setErrors((prev) => {
        const e = { ...prev };
        delete e[`${field}_${index}`];
        return e;
      });
    }
  };

  const validate = (): boolean => {
    const errs: Record<string, string> = {};
    if (!warehouseId) errs.warehouseId = 'Vui lòng chọn kho kiểm kê';

    if (auditType !== InventoryAuditType.Full) {
      const validItems = specificItems.filter((i) => Boolean(i.variantId));
      if (validItems.length === 0) {
        errs.specificItems = 'Kiểm kê đột xuất hoặc cuốn chiếu cần chọn ít nhất 1 mặt hàng';
      } else {
        specificItems.forEach((item, idx) => {
          if (!item.variantId) errs[`variantId_${idx}`] = 'Vui lòng chọn sản phẩm';
        });
      }
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return showToast('warning', 'Vui lòng điền đầy đủ các thông tin bắt buộc!');

    try {
      setLoading(true);
      const payload: InventoryAuditCreatePayload = {
        warehouseId: Number(warehouseId),
        auditType,
        auditorId: userInfo?.id || 1,
        note: note.trim(),
        specificItems:
          auditType !== InventoryAuditType.Full
            ? specificItems
                .filter((i) => Boolean(i.variantId))
                .map((i) => ({
                  variantId: Number(i.variantId),
                  batchId: Number(i.batchId || 0),
                  uoMId: Number(i.uoMId || 1),
                }))
            : undefined,
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
          <FormSection title="1. Thông Tin Đợt Kiểm Kê">
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
                        onClick={() => {
                          setAuditType(t);
                          if (errors.specificItems) {
                            setErrors((prev) => {
                              const e = { ...prev };
                              delete e.specificItems;
                              return e;
                            });
                          }
                        }}
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
                  rows={2}
                  placeholder="Kiểm kê định kỳ cuối tháng, chú ý đếm kỹ các Lô hàng sát hạn sử dụng..."
                />
              </div>
            </div>

            <div className="mt-6 p-4 bg-amber-50 border border-amber-200 rounded-2xl flex gap-3 items-start text-xs text-amber-900 leading-relaxed shadow-2xs">
              <AlertCircle className="w-5 h-5 shrink-0 text-amber-600 mt-0.5" />
              <div>
                <span className="font-bold">Lưu ý quản trị:</span> Khi nhấn "TẠO MỚI", hệ thống sẽ
                lưu lại toàn bộ số dư tồn kho khả dụng hiện tại làm mốc so sánh (System Quantity).
                Nhân viên đi đếm có thể sử dụng chế độ <strong>Đếm Mù (Blind Count)</strong> để ghi
                nhận số liệu khách quan nhất.
              </div>
            </div>
          </FormSection>

          {/* 2. DANH SÁCH MẶT HÀNG (DÀNH CHO CYCLE & SPOT) */}
          {auditType !== InventoryAuditType.Full && (
            <FormSection
              title={`2. Danh Sách Mặt Hàng Kiểm Kê (${InventoryAuditTypeLabels[auditType]})`}
            >
              {errors.specificItems && (
                <div className="mb-4 p-3 bg-rose-50 text-rose-600 text-sm font-bold rounded-lg border border-rose-200 flex items-center gap-2">
                  <AlertCircle size={16} />
                  <span>{errors.specificItems}</span>
                </div>
              )}

              <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[220px]">
                <table className="w-full text-left text-sm whitespace-nowrap min-w-[700px]">
                  <thead className="bg-slate-50/80 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                    <tr>
                      <th className="px-3 py-3.5 text-center w-10">#</th>
                      <th className="px-3 py-3.5 min-w-[280px]">
                        Sản Phẩm (SKU) <span className="text-red-500">*</span>
                      </th>
                      <th className="px-3 py-3.5 min-w-[240px]">Lô Hàng Nông Sản</th>
                      <th className="px-3 py-3.5 w-28 text-center">ĐVT Chuẩn</th>
                      <th className="px-3 py-3.5 w-12 text-center">Xóa</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {specificItems.map((row, idx) => {
                      const rowBatches = batches.filter(
                        (b) => b.variantId === Number(row.variantId)
                      );
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
                        <tr key={idx} className="hover:bg-slate-50/60 transition-colors">
                          <td className="px-3 py-3 text-center text-slate-400 font-medium">
                            {idx + 1}
                          </td>

                          {/* SẢN PHẨM */}
                          <td className="p-2 min-w-[280px]">
                            <FormSelect
                              label=""
                              showSearch
                              placeholder="-- Chọn sản phẩm cần kiểm kê --"
                              searchPlaceholder="Tìm sản phẩm..."
                              options={variants}
                              value={row.variantId}
                              error={errors[`variantId_${idx}`]}
                              onSelect={(val) =>
                                handleSpecificItemChange(idx, 'variantId', val ? Number(val) : '')
                              }
                            />
                          </td>

                          {/* LÔ HÀNG */}
                          <td className="p-2 min-w-[240px]">
                            <FormSelect
                              label=""
                              placeholder="-- Toàn bộ các lô (mặc định) --"
                              showSearch
                              searchPlaceholder="Tìm mã lô..."
                              options={batchOptions}
                              value={row.batchId}
                              disabled={!row.variantId}
                              onSelect={(val) =>
                                handleSpecificItemChange(idx, 'batchId', val ? Number(val) : '')
                              }
                            />
                            {currentBatchObj !== undefined && (
                              <div className="mt-1 flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md w-fit border border-emerald-200/60">
                                <span>Tồn khả dụng: {currentBatchObj.quantityAvailable}</span>
                              </div>
                            )}
                          </td>

                          {/* ĐVT: Khóa cứng theo chuẩn kho */}
                          <td className="p-2 w-28 text-center">
                            <FormSelect
                              label=""
                              placeholder="ĐVT"
                              options={uoms}
                              value={row.uoMId}
                              disabled={Boolean(row.variantId)}
                              onSelect={(val) =>
                                handleSpecificItemChange(idx, 'uoMId', val ? Number(val) : '')
                              }
                            />
                          </td>

                          {/* XÓA DÒNG */}
                          <td className="p-2 text-center w-12">
                            <button
                              type="button"
                              onClick={() => handleRemoveSpecificItem(idx)}
                              className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors cursor-pointer"
                              title="Xóa dòng"
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

              <div className="flex justify-start">
                <button
                  type="button"
                  onClick={handleAddSpecificItem}
                  className="flex items-center gap-2 px-4 py-2 bg-amber-50 hover:bg-amber-100 text-amber-900 border border-amber-300 rounded-xl text-xs font-bold transition-all shadow-2xs cursor-pointer"
                >
                  <Plus size={16} /> Thêm Mặt Hàng Kiểm Kê
                </button>
              </div>
            </FormSection>
          )}

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
