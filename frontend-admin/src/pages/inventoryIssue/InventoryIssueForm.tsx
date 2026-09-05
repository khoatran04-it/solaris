import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { PackageCheck, Plus, Trash2, Sparkles, Save } from 'lucide-react';

import {
  PageContainer,
  FormCard,
  FormInput,
  FormSelect,
  FormTextarea,
  SubmitButton,
  FormHeader,
  FormSection,
} from '../../components/commons/FormUI';
import CustomDatePicker from '../../components/commons/CustomDatePicker';
import { Toast } from '../../components/commons/Toast';

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { orderApi } from '../../api/orderApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// Import types
import { InventoryIssueCreatePayload, SuggestedBatch } from '../../types/inventoryIssue';

// --- FORM STATE TYPES ---
interface DetailRow {
  id: string; // Khóa tạm thời cho React list mapping
  orderDetailId?: number;
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  quantity: number;
  unitPrice: number;
}

interface IssueFormState {
  orderId: number | '';
  warehouseId: number | '';
  issueDate: Date;
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  note: string;
}

const INITIAL_FORM_STATE: IssueFormState = {
  orderId: '',
  warehouseId: '',
  issueDate: new Date(),
  receiverName: '',
  receiverPhone: '',
  deliveryAddress: '',
  note: '',
};

const createEmptyDetailRow = (): DetailRow => ({
  id: crypto.randomUUID(),
  variantId: '',
  batchId: '',
  uoMId: '',
  quantity: 1,
  unitPrice: 0,
});

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

const InventoryIssueForm: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const orderIdParam = searchParams.get('orderId');
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

  // --- STATES ---
  const [formData, setFormData] = useState<IssueFormState>({
    ...INITIAL_FORM_STATE,
    orderId: orderIdParam ? Number(orderIdParam) : '',
  });
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Dropdown Options
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [orders, setOrders] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // Map lưu danh sách lô theo: key = `${warehouseId}_${variantId}`
  const [variantBatchesMap, setVariantBatchesMap] = useState<Record<string, SuggestedBatch[]>>({});

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadInit = async () => {
      try {
        const [whList, varList, uomList, orderRes] = await Promise.all([
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
          orderApi.getAll({ pageSize: 50 }).catch(() => ({ items: [] })),
        ]);

        const whOpts = whList.map((w: any) => ({ value: w.id, label: w.name }));
        setWarehouses(whOpts);

        setVariants(
          varList.map((v: any) => ({
            value: v.id,
            label: `${v.code} - ${v.name}`,
            prices: v.prices || [],
          }))
        );
        setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
        setOrders(
          (orderRes.items || []).map((o: any) => ({
            value: o.id,
            label: `${o.orderCode} - ${o.receiverName || o.customerName || 'Khách hàng'} (${o.totalAmount?.toLocaleString('vi-VN')} đ)`,
          }))
        );
      } catch (err) {
        showToast('error', 'Lỗi tải danh mục bổ trợ!');
      }
    };
    loadInit();
  }, []);

  // Fetch batches cho 1 variant tại kho xuất
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

  const loadOrder = useCallback(async (id: number) => {
    try {
      const ord = await orderApi.getById(id);
      if (ord) {
        const whId = ord.warehouseId;
        setFormData((prev) => ({
          ...prev,
          orderId: ord.id,
          warehouseId: whId || prev.warehouseId,
          receiverName: ord.receiverName || ord.customerName || '',
          receiverPhone: ord.receiverPhone || ord.customerPhone || '',
          deliveryAddress: ord.deliveryAddress || '',
        }));

        if (ord.details && ord.details.length > 0) {
          const batchMapUpdates: Record<string, SuggestedBatch[]> = {};

          const rows: DetailRow[] = await Promise.all(
            ord.details.map(async (d) => {
              const unissued = Math.max(0, d.quantity - (d.issuedQuantity || 0));
              const qty = unissued > 0 ? unissued : d.quantity;
              let autoBatchId: number | '' = '';

              if (whId) {
                try {
                  const suggestions = await inventoryIssueApi.getSuggestedBatches(
                    whId,
                    d.variantId,
                    qty
                  );
                  if (suggestions && suggestions.length > 0) {
                    autoBatchId = suggestions[0].batchId;
                    batchMapUpdates[`${whId}_${d.variantId}`] = suggestions;
                  } else {
                    batchMapUpdates[`${whId}_${d.variantId}`] = [];
                  }
                } catch {
                  batchMapUpdates[`${whId}_${d.variantId}`] = [];
                }
              }

              return {
                id: crypto.randomUUID(),
                orderDetailId: d.id,
                variantId: d.variantId,
                batchId: autoBatchId,
                uoMId: d.uoMId,
                quantity: qty,
                unitPrice: d.unitPrice,
              };
            })
          );

          setVariantBatchesMap((prev) => ({ ...prev, ...batchMapUpdates }));
          setDetails(rows);
          showToast('success', 'Đã tải thông tin đơn hàng và tự động phân bổ Lô FEFO tối ưu!');
        }
      }
    } catch (err) {
      showToast('error', 'Không thể tải thông tin đơn hàng gốc!');
    }
  }, []);

  useEffect(() => {
    if (orderIdParam) {
      loadOrder(Number(orderIdParam));
    }
  }, [orderIdParam, loadOrder]);

  // Khi Kho Xuất thay đổi: tự động fetch lại lô hàng cho các mặt hàng đã chọn
  useEffect(() => {
    if (formData.warehouseId) {
      const whId = Number(formData.warehouseId);
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
  }, [formData.warehouseId, fetchBatchesForVariant]);

  const handleAutoAllocateAllFEFO = async () => {
    if (!formData.warehouseId) {
      return showToast('warning', 'Vui lòng chọn Kho xuất ở mục (1) trước!');
    }
    try {
      let successCount = 0;
      const updatedDetails = await Promise.all(
        details.map(async (row) => {
          if (!row.variantId) return row;
          try {
            const suggestions = await fetchBatchesForVariant(
              Number(formData.warehouseId),
              Number(row.variantId),
              Number(row.quantity || 1)
            );
            if (suggestions && suggestions.length > 0) {
              successCount++;
              return { ...row, batchId: suggestions[0].batchId };
            }
          } catch {}
          return row;
        })
      );
      setDetails(updatedDetails);
      if (successCount > 0) {
        showToast('success', `Đã tự động phân bổ Lô FEFO tối ưu cho ${successCount} mặt hàng!`);
      } else {
        showToast('warning', 'Không tìm thấy Lô hàng có sẵn trong kho!');
      }
    } catch (err) {
      showToast('error', 'Lỗi hệ thống khi phân bổ lô FEFO!');
    }
  };

  // --- FORM HANDLERS ---
  const handleFieldChange = (field: keyof IssueFormState, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[field];
        return newErr;
      });
    }
  };

  const handleSelectOrder = (orderIdVal: string | number) => {
    const id = Number(orderIdVal);
    if (!id) {
      setFormData((prev) => ({ ...prev, orderId: '' }));
      return;
    }
    loadOrder(id);
  };

  const handleAddRow = () => {
    setDetails((prev) => [...prev, createEmptyDetailRow()]);
  };

  const handleRemoveRow = (id: string) => {
    if (details.length > 1) {
      setDetails((prev) => prev.filter((row) => row.id !== id));
    }
  };

  const handleDetailChange = (id: string, field: keyof DetailRow, value: any) => {
    setDetails((prev) =>
      prev.map((row) => {
        if (row.id !== id) return row;
        const updated = { ...row, [field]: value };

        // Auto-fill UoM & Giá khi chọn SP
        if (field === 'variantId' && value) {
          const v = variants.find((item) => item.value === Number(value));
          if (v && v.prices && v.prices.length > 0) {
            const defPrice = v.prices.find((p: any) => p.isDefault) || v.prices[0];
            if (defPrice) {
              updated.uoMId = defPrice.uoMId;
              updated.unitPrice = defPrice.price;
            }
          }
        }
        return updated;
      })
    );

    if (errors[`${field}_${id}`]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[`${field}_${id}`];
        return newErr;
      });
    }
  };

  // --- FEFO BATCH SUGGESTION ---
  const handleAutoSuggestBatch = async (rowId: string, variantId: number | '') => {
    if (!formData.warehouseId) {
      return showToast('warning', 'Vui lòng chọn Kho xuất ở mục (1) trước!');
    }
    if (!variantId) {
      return showToast('warning', 'Vui lòng chọn Sản phẩm trước khi bấm FEFO!');
    }

    const targetRow = details.find((d) => d.id === rowId);
    if (!targetRow) return;

    try {
      const suggestions = await fetchBatchesForVariant(
        Number(formData.warehouseId),
        Number(variantId),
        Number(targetRow.quantity || 1)
      );

      if (suggestions && suggestions.length > 0) {
        const topBatch = suggestions[0];
        handleDetailChange(rowId, 'batchId', topBatch.batchId);
        showToast('success', `Đã tự động gán Lô tối ưu FEFO: ${topBatch.batchCode}`);
      } else {
        showToast('warning', 'Không tìm thấy Lô hàng nào có sẵn trong kho này!');
      }
    } catch (err) {
      console.error('Error in FEFO auto suggest:', err);
      showToast('error', 'Lỗi hệ thống: Không thể lấy gợi ý Lô hàng!');
    }
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.warehouseId) errs.warehouseId = 'Vui lòng chọn kho xuất';
    if (!formData.receiverName.trim()) errs.receiverName = 'Vui lòng nhập tên người nhận';
    if (!formData.receiverPhone.trim()) errs.receiverPhone = 'Vui lòng nhập số điện thoại nhận';
    if (!formData.deliveryAddress.trim()) errs.deliveryAddress = 'Vui lòng nhập địa chỉ giao hàng';

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 dòng xuất kho';
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
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin báo đỏ!');

    try {
      setLoading(true);

      // Chuẩn hóa ngày ISO theo local date
      const year = formData.issueDate.getFullYear();
      const month = String(formData.issueDate.getMonth() + 1).padStart(2, '0');
      const day = String(formData.issueDate.getDate()).padStart(2, '0');
      const issueDateIso = `${year}-${month}-${day}T00:00:00Z`;

      const payload: InventoryIssueCreatePayload = {
        orderId: formData.orderId ? Number(formData.orderId) : undefined,
        warehouseId: Number(formData.warehouseId),
        issuedById: userInfo?.id || 1,
        issueDate: issueDateIso,
        receiverName: formData.receiverName.trim(),
        receiverPhone: formData.receiverPhone.trim(),
        deliveryAddress: formData.deliveryAddress.trim(),
        note: formData.note.trim(),
        details: details.map((d) => ({
          orderDetailId: d.orderDetailId,
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          quantity: Number(d.quantity),
          unitPrice: Number(d.unitPrice),
        })),
      };

      const res = await inventoryIssueApi.create(payload);
      showToast('success', 'TẠO PHIẾU XUẤT KHO THÀNH CÔNG!');
      setTimeout(() => navigate(`/inventory-issues/${res.id}`), 1200);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể tạo phiếu xuất kho!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Phiếu Xuất Kho"
        subtitle="Xuất hàng từ kho & Gán đích danh Lô hàng (FEFO)"
        icon={PackageCheck}
        onBack={() => navigate('/inventory-issues')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* ================= SECTION 1: THÔNG TIN PHIẾU ================= */}
          <FormSection title="1. Thông Tin Phiếu Xuất">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {/* Đơn hàng tham chiếu */}
              <FormSelect
                label="Đơn hàng tham chiếu (Tùy chọn)"
                value={formData.orderId}
                onSelect={handleSelectOrder}
                options={[{ value: '', label: '-- Xuất trực tiếp / Bán lẻ --' }, ...orders]}
                showSearch
                searchPlaceholder="Tìm kiếm mã đơn hàng..."
              />

              {/* Kho xuất hàng */}
              <FormSelect
                label="Kho xuất hàng"
                value={formData.warehouseId}
                onSelect={(val) => handleFieldChange('warehouseId', val ? Number(val) : '')}
                options={warehouses}
                error={errors.warehouseId}
                placeholder="-- Chọn Kho xuất hàng --"
                required
                showSearch
                searchPlaceholder="Tìm kiếm kho xuất..."
              />

              {/* Ngày xuất hàng */}
              <CustomDatePicker
                label="Ngày xuất hàng"
                value={formData.issueDate}
                onChange={(d) => d && handleFieldChange('issueDate', d)}
                required
              />

              {/* Người nhận hàng */}
              <FormInput
                label="Người nhận hàng"
                value={formData.receiverName}
                onChange={(e) => handleFieldChange('receiverName', e.target.value)}
                error={errors.receiverName}
                placeholder="Nguyễn Văn A"
                required
              />

              {/* Số điện thoại nhận */}
              <FormInput
                label="Số điện thoại nhận"
                value={formData.receiverPhone}
                onChange={(e) => handleFieldChange('receiverPhone', e.target.value)}
                error={errors.receiverPhone}
                placeholder="0901234567"
                required
              />

              {/* Địa chỉ giao hàng */}
              <FormInput
                label="Địa chỉ giao hàng"
                value={formData.deliveryAddress}
                onChange={(e) => handleFieldChange('deliveryAddress', e.target.value)}
                error={errors.deliveryAddress}
                placeholder="Số 123 Đường ABC, Quận XYZ..."
                required
              />

              <div className="md:col-span-2 lg:col-span-3">
                <FormTextarea
                  label="Ghi chú xuất kho"
                  value={formData.note}
                  onChange={(e: any) => handleFieldChange('note', e.target.value)}
                  placeholder="Ghi chú thêm về quy cách đóng gói, đơn vị vận chuyển..."
                  rows={2}
                />
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 2: CHI TIẾT ĐÓNG GÓI ================= */}
          <FormSection title="2. Chi Tiết Đóng Gói Theo Lô Hàng">
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3 rounded-lg border border-rose-200">
                {errors.details}
              </div>
            )}

            <div className="flex flex-wrap items-center justify-between gap-3 mb-3">
              <div className="text-xs text-slate-500 font-medium">
                Gán lô hàng tự động theo hạn dùng gần nhất (FEFO) hoặc chọn thủ công từng lô
              </div>
              <button
                type="button"
                onClick={handleAutoAllocateAllFEFO}
                className="flex items-center gap-2 px-3.5 py-2 bg-gradient-to-r from-amber-50 to-orange-50 hover:from-amber-100 hover:to-orange-100 text-amber-900 border border-amber-300 rounded-xl text-xs font-bold transition-all shadow-2xs hover:shadow-xs active:scale-95"
              >
                <Sparkles size={15} className="text-amber-600 animate-pulse" />
                <span>Tự Động Phân Bổ Lô FEFO Tất Cả</span>
              </button>
            </div>

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[380px] pb-24">
              <table className="w-full text-left text-sm whitespace-nowrap min-w-[920px]">
                <thead className="bg-slate-50/80 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-3 py-3.5 text-center w-12">#</th>
                    <th className="px-3 py-3.5 min-w-[240px]">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-28 min-w-[90px]">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 min-w-[310px]">
                      Lô Hàng (FEFO) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-24 text-center bg-amber-50/50 text-amber-900 border-x border-amber-100/70">
                      Số lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-32 text-right">Đơn giá</th>
                    <th className="px-3 py-3.5 w-36 text-right">Thành tiền</th>
                    <th className="px-3 py-3.5 w-12 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches =
                      formData.warehouseId && row.variantId
                        ? variantBatchesMap[`${formData.warehouseId}_${row.variantId}`] || []
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
                              const vId = val ? Number(val) : '';
                              handleDetailChange(row.id, 'variantId', vId);
                              handleDetailChange(row.id, 'batchId', '');
                              // Tự động gợi ý Lô FEFO ngay khi chọn SP nếu đã có kho
                              if (vId && formData.warehouseId) {
                                fetchBatchesForVariant(
                                  Number(formData.warehouseId),
                                  Number(vId),
                                  Number(row.quantity || 1)
                                )
                                  .then((sugs) => {
                                    if (sugs && sugs.length > 0) {
                                      handleDetailChange(row.id, 'batchId', sugs[0].batchId);
                                    }
                                  })
                                  .catch(() => {});
                              }
                            }}
                            disabled={Boolean(row.orderDetailId)}
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
                            disabled={Boolean(row.orderDetailId)}
                          />
                        </td>

                        {/* Cột 3: Lô Hàng (FEFO) + Nút AI FEFO */}
                        <td className="p-2 min-w-[310px]">
                          <div className="flex items-center gap-1.5">
                            <div className="flex-1 min-w-0">
                              <FormSelect
                                label=""
                                placeholder={
                                  !formData.warehouseId
                                    ? '-- Chọn Kho xuất trước --'
                                    : !row.variantId
                                      ? '-- Chọn Sản phẩm trước --'
                                      : rowBatches.length === 0
                                        ? '-- Kho xuất hết hàng cho SP này --'
                                        : '-- Chọn Lô FEFO --'
                                }
                                showSearch
                                searchPlaceholder="Tìm mã lô..."
                                options={rowBatches.map((b) => {
                                  const expiryStr = b.expiryDate
                                    ? `(HSD: ${new Date(b.expiryDate).toLocaleDateString('vi-VN')})`
                                    : '';
                                  const stockInfo =
                                    b.quantityReserved > 0 && b.quantityAvailable === 0
                                      ? `[Đã giữ chỗ cho đơn: ${b.quantityReserved}]`
                                      : `[Khả dụng: ${b.quantityAvailable}${
                                          b.quantityReserved
                                            ? `, Giữ chỗ: ${b.quantityReserved}`
                                            : ''
                                        }]`;

                                  return {
                                    value: b.batchId,
                                    label: `${b.batchCode} ${expiryStr} - ${stockInfo}`,
                                  };
                                })}
                                value={row.batchId}
                                error={errors[`batchId_${row.id}`]}
                                disabled={!row.variantId || !formData.warehouseId}
                                onSelect={(val) =>
                                  handleDetailChange(row.id, 'batchId', val ? Number(val) : '')
                                }
                              />
                            </div>
                            <button
                              type="button"
                              onClick={() => handleAutoSuggestBatch(row.id, row.variantId)}
                              className="px-2.5 py-2.5 bg-amber-50 hover:bg-amber-100 text-amber-900 border border-amber-200 rounded-xl transition-all font-bold text-xs flex items-center gap-1 shrink-0 cursor-pointer shadow-2xs"
                              title="Tự động chọn Lô hết hạn trước (FEFO)"
                            >
                              <Sparkles size={14} className="text-amber-600" />
                              <span className="hidden xl:inline">FEFO</span>
                            </button>
                          </div>
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
                                parseFloat(e.target.value) || 0
                              )
                            }
                          />
                        </td>

                        {/* Cột 5: Đơn giá */}
                        <td className="p-2 w-32">
                          <FormInput
                            label=""
                            type="number"
                            min="0"
                            onFocus={(e) => e.target.select()}
                            className="text-right font-medium text-slate-700"
                            value={row.unitPrice}
                            onChange={(e) =>
                              handleDetailChange(
                                row.id,
                                'unitPrice',
                                parseFloat(e.target.value) || 0
                              )
                            }
                          />
                        </td>

                        {/* Cột 6: Thành tiền */}
                        <td className="px-3 py-3 text-right font-black text-amber-900 w-36">
                          {(Number(row.quantity || 0) * Number(row.unitPrice || 0)).toLocaleString(
                            'vi-VN'
                          )}{' '}
                          đ
                        </td>

                        {/* Cột 7: Xóa dòng */}
                        <td className="p-2 text-center w-12 border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
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
              </table>

              {/* Nút Thêm dòng xuất */}
              <div className="p-3 bg-slate-50/60 border-t border-slate-200/80 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-amber-900 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-xl transition-all shadow-2xs cursor-pointer"
                >
                  <Plus size={16} /> THÊM DÒNG XUẤT
                </button>
              </div>
            </div>
          </FormSection>

          {/* ================= FOOTER & ACTION BUTTONS ================= */}
          <div className="flex justify-end gap-3 pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/inventory-issues')}
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

export default InventoryIssueForm;
