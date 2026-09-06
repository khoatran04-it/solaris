import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeftRight, Plus, Trash2, Save, Sparkles, Loader2 } from 'lucide-react';

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

import { inventoryTransferApi } from '../../api/inventoryTransferApi';
import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { orderApi } from '../../api/orderApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// Import types
import { InventoryTransferCreatePayload } from '../../types/inventoryTransfer';
import { SuggestedBatch } from '../../types/inventoryIssue';

// --- FORM STATE TYPES ---
interface DetailRow {
  id: string; // Khóa tạm thời cho React list mapping
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  quantity: number;
}

interface TransferFormState {
  fromWarehouseId: number | '';
  toWarehouseId: number | '';
  orderId: number | '';
  note: string;
}

const INITIAL_FORM_STATE: TransferFormState = {
  fromWarehouseId: '',
  toWarehouseId: '',
  orderId: '',
  note: '',
};

const createEmptyDetailRow = (): DetailRow => ({
  id: crypto.randomUUID(),
  variantId: '',
  batchId: '',
  uoMId: '',
  quantity: 1,
});

const InventoryTransferForm: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const orderIdParam = searchParams.get('orderId');
  const fromWarehouseIdParam = searchParams.get('fromWarehouseId');
  const toWarehouseIdParam = searchParams.get('toWarehouseId');
  const missingOnlyParam = searchParams.get('missingOnly');
  const { userInfo } = useAuthStore();

  const [loading, setLoading] = useState(false);
  const [allocatingFEFO, setAllocatingFEFO] = useState(false);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- STATES ---
  const [formData, setFormData] = useState<TransferFormState>({
    ...INITIAL_FORM_STATE,
    fromWarehouseId: fromWarehouseIdParam ? Number(fromWarehouseIdParam) : '',
    toWarehouseId: toWarehouseIdParam ? Number(toWarehouseIdParam) : '',
    orderId: orderIdParam ? Number(orderIdParam) : '',
  });
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Map lưu danh sách lô theo: key = `${fromWarehouseId}_${variantId}`
  const [variantBatchesMap, setVariantBatchesMap] = useState<Record<string, SuggestedBatch[]>>({});

  // --- DROPDOWN OPTIONS ---
  const [sourceWarehouses, setSourceWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [destWarehouses, setDestWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<
    { value: number; label: string; prices?: any[]; baseUoMId?: number }[]
  >([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadInit = async () => {
      try {
        const [whList, varList, uomList] = await Promise.all([
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
        ]);

        // Nghiệp vụ SCM: Cấm điều chuyển xuất phát từ Kho Hàng Lỗi
        const validSourceWhs = whList.filter((w: any) => w.warehouseType !== 'Kho Hàng Lỗi');
        setSourceWarehouses(validSourceWhs.map((w: any) => ({ value: w.id, label: w.name })));
        setDestWarehouses(whList.map((w: any) => ({ value: w.id, label: w.name })));
        setVariants(
          varList.map((v: any) => ({
            value: v.id,
            label: `${v.code} - ${v.name}`,
            prices: v.prices || [],
            baseUoMId: v.baseUoMId,
          }))
        );
        setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
      } catch (err) {
        showToast('error', 'Lỗi tải danh mục bổ trợ!');
      }
    };
    loadInit();
  }, []);

  // Fetch batches cho 1 variant tại kho nguồn
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

  const loadOrder = useCallback(
    async (id: number) => {
      try {
        setLoading(true);
        const ord = await orderApi.getById(id);
        if (ord) {
          let fromWh = fromWarehouseIdParam ? Number(fromWarehouseIdParam) : '';
          let toWh = toWarehouseIdParam ? Number(toWarehouseIdParam) : ord.warehouseId || '';

          let itemsToTransfer: { variantId: number; uoMId: number; quantity: number }[] = [];

          // Nếu missingOnly=true, phân tích định tuyến để lấy đúng các mặt hàng thiếu
          if (missingOnlyParam === 'true') {
            try {
              const routing = await orderApi.previewRouting({
                customerId: ord.customerId,
                customerAddressId: ord.customerAddressId,
                deliveryAddress: ord.deliveryAddress || '',
                warehouseId: ord.warehouseId,
                paymentMethod: ord.paymentMethod,
                details: ord.details.map((d) => ({
                  variantId: d.variantId,
                  uoMId: d.uoMId,
                  quantity: d.quantity,
                })),
              });

              if (routing.suggestedSourceWarehouseId && !fromWh) {
                fromWh = routing.suggestedSourceWarehouseId;
              }

              if (routing.missingItems && routing.missingItems.length > 0) {
                itemsToTransfer = routing.missingItems.map((m) => {
                  const originalDetail = ord.details.find((d) => d.variantId === m.variantId);
                  return {
                    variantId: m.variantId,
                    uoMId: originalDetail?.uoMId || 0,
                    quantity: m.missingQuantity,
                  };
                });
              }
            } catch (rErr) {
              console.error('Lỗi khi phân tích hàng thiếu:', rErr);
            }
          }

          // Fallback: nếu không có missingOnly hoặc không thiếu gì, load toàn bộ
          if (itemsToTransfer.length === 0 && ord.details && ord.details.length > 0) {
            itemsToTransfer = ord.details.map((d) => ({
              variantId: d.variantId,
              uoMId: d.uoMId,
              quantity: d.quantity,
            }));
          }

          setFormData((prev) => ({
            ...prev,
            orderId: ord.id,
            fromWarehouseId: fromWh,
            toWarehouseId: toWh,
            note: `Điều phối bổ sung hàng thiếu cho đơn hàng ${ord.orderCode}`,
          }));

          if (itemsToTransfer.length > 0) {
            const newRows: DetailRow[] = [];
            for (const item of itemsToTransfer) {
              let autoBatchId: number | '' = '';
              if (fromWh && item.variantId) {
                const suggestions = await fetchBatchesForVariant(
                  Number(fromWh),
                  item.variantId,
                  item.quantity
                );
                if (suggestions && suggestions.length > 0) {
                  autoBatchId = suggestions[0].batchId;
                }
              }
              newRows.push({
                id: crypto.randomUUID(),
                variantId: item.variantId,
                batchId: autoBatchId,
                uoMId: item.uoMId,
                quantity: item.quantity,
              });
            }
            setDetails(newRows);
          }
        }
      } catch (err) {
        showToast('error', 'Không thể tải thông tin đơn hàng!');
      } finally {
        setLoading(false);
      }
    },
    [fromWarehouseIdParam, toWarehouseIdParam, missingOnlyParam, fetchBatchesForVariant]
  );

  useEffect(() => {
    if (orderIdParam) {
      loadOrder(Number(orderIdParam));
    }
  }, [orderIdParam, loadOrder]);

  // Khi Kho Nguồn thay đổi: tự động fetch lại lô hàng cho các mặt hàng đã chọn
  useEffect(() => {
    if (formData.fromWarehouseId) {
      const whId = Number(formData.fromWarehouseId);
      details.forEach(async (row) => {
        if (row.variantId) {
          const suggestions = await fetchBatchesForVariant(
            whId,
            Number(row.variantId),
            row.quantity
          );
          if (suggestions && suggestions.length > 0 && !row.batchId) {
            setDetails((prev) =>
              prev.map((r) => (r.id === row.id ? { ...r, batchId: suggestions[0].batchId } : r))
            );
          }
        }
      });
    }
  }, [formData.fromWarehouseId, fetchBatchesForVariant]);

  // --- FORM HANDLERS ---
  const handleFieldChange = (field: keyof TransferFormState, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[field];
        return newErr;
      });
    }
  };

  const handleAddRow = () => {
    setDetails((prev) => [...prev, createEmptyDetailRow()]);
  };

  const handleRemoveRow = (id: string) => {
    if (details.length > 1) {
      setDetails((prev) => prev.filter((row) => row.id !== id));
    }
  };

  const handleDetailChange = async (id: string, field: keyof DetailRow, value: any) => {
    setDetails((prev) =>
      prev.map((row) => {
        if (row.id !== id) return row;
        const updated = { ...row, [field]: value };

        // Auto-fill UoM khi chọn SP
        if (field === 'variantId' && value) {
          const v = variants.find((item) => item.value === Number(value));
          if (v) {
            if (v.prices && v.prices.length > 0) {
              const defPrice = v.prices.find((p: any) => p.isDefault) || v.prices[0];
              if (defPrice && defPrice.uoMId) {
                updated.uoMId = defPrice.uoMId;
              }
            } else if (v.baseUoMId) {
              updated.uoMId = v.baseUoMId;
            }
          }
          updated.batchId = ''; // Reset batch khi đổi SP
        }

        return updated;
      })
    );

    // Tự động load Lô FEFO của đúng SP đó tại kho nguồn
    if (field === 'variantId' && value && formData.fromWarehouseId) {
      const suggestions = await fetchBatchesForVariant(
        Number(formData.fromWarehouseId),
        Number(value),
        1
      );
      if (suggestions && suggestions.length > 0) {
        setDetails((prev) =>
          prev.map((row) => (row.id === id ? { ...row, batchId: suggestions[0].batchId } : row))
        );
      }
    }

    if (errors[`${field}_${id}`]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[`${field}_${id}`];
        return newErr;
      });
    }
  };

  // Nút tự động phân bổ lô FEFO cho toàn bộ mặt hàng
  const handleAutoAllocateAllFEFO = async () => {
    if (!formData.fromWarehouseId) {
      return showToast('warning', 'Vui lòng chọn Kho nguồn xuất phát trước!');
    }
    try {
      setAllocatingFEFO(true);
      const whId = Number(formData.fromWarehouseId);
      const updatedDetails = [...details];

      for (let i = 0; i < updatedDetails.length; i++) {
        const row = updatedDetails[i];
        if (!row.variantId) continue;
        const suggestions = await fetchBatchesForVariant(whId, Number(row.variantId), row.quantity);
        if (suggestions && suggestions.length > 0) {
          row.batchId = suggestions[0].batchId;
        }
      }

      setDetails(updatedDetails);
      showToast('success', 'ĐÃ TỰ ĐỘNG PHÂN BỔ LÔ FEFO CHO TẤT CẢ MẶT HÀNG!');
    } catch {
      showToast('error', 'Không thể phân bổ lô tự động!');
    } finally {
      setAllocatingFEFO(false);
    }
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.fromWarehouseId) errs.fromWarehouseId = 'Vui lòng chọn kho xuất phát';
    if (!formData.toWarehouseId) errs.toWarehouseId = 'Vui lòng chọn kho đích đến';
    if (
      formData.fromWarehouseId &&
      formData.toWarehouseId &&
      formData.fromWarehouseId === formData.toWarehouseId
    ) {
      errs.toWarehouseId = 'Kho đích không được trùng kho nguồn';
    }

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 mặt hàng chuyển kho';
    } else {
      details.forEach((d) => {
        if (!d.variantId) errs[`variantId_${d.id}`] = 'Bắt buộc';
        if (!d.batchId) errs[`batchId_${d.id}`] = 'Bắt buộc';
        if (!d.uoMId) errs[`uoMId_${d.id}`] = 'Bắt buộc';
        if (Number(d.quantity) <= 0) errs[`quantity_${d.id}`] = '> 0';
      });
    }

    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin bị lỗi!');

    try {
      setLoading(true);
      const payload: InventoryTransferCreatePayload = {
        fromWarehouseId: Number(formData.fromWarehouseId),
        toWarehouseId: Number(formData.toWarehouseId),
        orderId: formData.orderId ? Number(formData.orderId) : undefined,
        createdById: userInfo?.id || 1,
        note: formData.note.trim(),
        details: details.map((d) => ({
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          quantity: Number(d.quantity),
        })),
      };

      const res = await inventoryTransferApi.create(payload);
      showToast('success', 'TẠO LỆNH CHUYỂN KHO THÀNH CÔNG!');
      setTimeout(() => navigate(`/inventory-transfers/${res.id}`), 1200);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể tạo phiếu chuyển kho!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Lệnh Chuyển Kho"
        subtitle="Luân chuyển hàng hóa giữa các kho nội bộ"
        icon={ArrowLeftRight}
        onBack={() => navigate('/inventory-transfers')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* ================= SECTION 1: THÔNG TIN TUYẾN ================= */}
          <FormSection title="1. Thông Tin Tuyến Chuyển Kho">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormSelect
                label="Kho nguồn (Xuất phát)"
                value={formData.fromWarehouseId}
                onSelect={(val) => handleFieldChange('fromWarehouseId', val ? Number(val) : '')}
                options={sourceWarehouses}
                error={errors.fromWarehouseId}
                placeholder="-- Chọn Kho xuất phát --"
                required
                showSearch
                searchPlaceholder="Tìm kho nguồn..."
              />

              <FormSelect
                label="Kho đích (Tiếp nhận)"
                value={formData.toWarehouseId}
                onSelect={(val) => handleFieldChange('toWarehouseId', val ? Number(val) : '')}
                options={destWarehouses}
                error={errors.toWarehouseId}
                placeholder="-- Chọn Kho tiếp nhận --"
                required
                showSearch
                searchPlaceholder="Tìm kho đích..."
              />

              <div className="md:col-span-2 lg:col-span-3">
                <FormTextarea
                  label="Lý do & Ghi chú chuyển kho"
                  value={formData.note}
                  onChange={(e: any) => handleFieldChange('note', e.target.value)}
                  rows={2}
                  placeholder="Điều phối bổ sung hàng thiếu cho đơn ORD-..., cân bằng kho..."
                />
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 2: CHI TIẾT MẶT HÀNG ================= */}
          <FormSection
            title="2. Danh Sách Mặt Hàng & Lô Hàng Chuyển Đi"
            customAction={
              <button
                type="button"
                onClick={handleAutoAllocateAllFEFO}
                disabled={allocatingFEFO || !formData.fromWarehouseId}
                className="flex items-center gap-1.5 px-3.5 py-1.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-slate-950 rounded-xl font-extrabold text-xs shadow-sm transition-all disabled:opacity-50"
              >
                {allocatingFEFO ? (
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                ) : (
                  <Sparkles className="w-3.5 h-3.5" />
                )}
                <span>✨ Tự Động Phân Bổ Lô FEFO Tất Cả</span>
              </button>
            }
          >
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3 rounded-lg border border-rose-200">
                {errors.details}
              </div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[380px] pb-24">
              <table className="w-full text-left text-sm whitespace-nowrap min-w-[850px]">
                <thead className="bg-slate-50/80 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-3 py-3.5 text-center w-12">#</th>
                    <th className="px-3 py-3.5 min-w-[240px]">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-28 min-w-[90px]">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 min-w-[340px]">
                      Lô Hàng Tại Kho Nguồn (Batch) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-24 text-center bg-amber-50/50 text-amber-900 border-x border-amber-100/70">
                      Số lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-12 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches =
                      formData.fromWarehouseId && row.variantId
                        ? variantBatchesMap[`${formData.fromWarehouseId}_${row.variantId}`] || []
                        : [];
                    return (
                      <tr
                        key={row.id}
                        className="hover:bg-slate-50/60 transition-colors"
                        style={{ zIndex: 50 - idx }}
                      >
                        <td className="px-3 py-3 text-center text-slate-400 font-medium">
                          {idx + 1}
                        </td>

                        {/* Cột 1: Sản phẩm */}
                        <td className="p-2 min-w-[240px]">
                          <FormSelect
                            label=""
                            showSearch
                            searchPlaceholder="Tìm kiếm SP..."
                            placeholder="Chọn sản phẩm..."
                            options={variants}
                            value={row.variantId}
                            error={errors[`variantId_${row.id}`]}
                            onSelect={(val) => {
                              handleDetailChange(row.id, 'variantId', val ? Number(val) : '');
                            }}
                          />
                        </td>

                        {/* Cột 2: Đơn vị tính */}
                        <td className="p-2 w-28">
                          <FormSelect
                            label=""
                            placeholder="ĐVT"
                            options={uoms}
                            value={row.uoMId}
                            error={errors[`uoMId_${row.id}`]}
                            onSelect={(val) =>
                              handleDetailChange(row.id, 'uoMId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        {/* Cột 3: Lô Hàng (Được lọc chính xác theo đúng SP & Kho Nguồn) */}
                        <td className="p-2 min-w-[340px]">
                          <FormSelect
                            label=""
                            placeholder={
                              !formData.fromWarehouseId
                                ? '-- Chọn Kho nguồn trước --'
                                : !row.variantId
                                  ? '-- Chọn Sản phẩm trước --'
                                  : rowBatches.length === 0
                                    ? '-- Kho nguồn hết hàng cho SP này --'
                                    : '-- Chọn Lô FEFO --'
                            }
                            showSearch
                            searchPlaceholder="Tìm mã lô..."
                            options={rowBatches.map((b) => ({
                              value: b.batchId,
                              label: `${b.batchCode} ${
                                b.expiryDate
                                  ? `(HSD: ${new Date(b.expiryDate).toLocaleDateString('vi-VN')})`
                                  : ''
                              } - [Khả dụng: ${b.quantityAvailable}]`,
                            }))}
                            value={row.batchId}
                            error={errors[`batchId_${row.id}`]}
                            disabled={!row.variantId || !formData.fromWarehouseId}
                            onSelect={(val) =>
                              handleDetailChange(row.id, 'batchId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        {/* Cột 4: Số lượng */}
                        <td className="p-2 bg-amber-50/20 border-x border-amber-100/50 w-24">
                          <FormInput
                            label=""
                            type="number"
                            min="1"
                            onFocus={(e) => e.target.select()}
                            className="text-center font-black text-amber-950"
                            value={row.quantity}
                            error={errors[`quantity_${row.id}`]}
                            onChange={(e) =>
                              handleDetailChange(
                                row.id,
                                'quantity',
                                Math.max(1, parseInt(e.target.value, 10) || 1)
                              )
                            }
                          />
                        </td>

                        {/* Cột 5: Xóa */}
                        <td className="p-2 text-center">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            disabled={details.length === 1}
                            className="p-1.5 text-slate-400 hover:text-red-500 rounded-lg hover:bg-red-50 transition-colors disabled:opacity-30 disabled:hover:bg-transparent"
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
                onClick={handleAddRow}
                className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-yellow-700 bg-yellow-50 hover:bg-yellow-100 border border-yellow-200 rounded-xl transition-all shadow-xs"
              >
                <Plus size={16} strokeWidth={2.5} /> Thêm Mặt Hàng
              </button>
            </div>
          </FormSection>

          {/* ================= ACTIONS ================= */}
          <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-100">
            <button
              type="button"
              onClick={() => navigate('/inventory-transfers')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shadow-2xs"
            >
              Hủy Bỏ
            </button>
            <SubmitButton
              loading={loading}
              isEditMode={false}
              icon={Save}
              className="px-8 py-2.5 text-sm font-bold text-slate-900 bg-yellow-400 hover:bg-yellow-500 rounded-xl transition-all shadow-xs shadow-yellow-200 flex items-center gap-2"
            />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default InventoryTransferForm;
