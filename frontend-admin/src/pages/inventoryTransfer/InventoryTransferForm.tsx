import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeftRight, Plus, Trash2, Save } from 'lucide-react';

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
import { orderApi } from '../../api/orderApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// 🔥 IMPORT TYPE CHUẨN TỪ FILE TYPES
import { InventoryTransferCreatePayload } from '../../types/inventoryTransfer';

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
  const [formData, setFormData] = useState<TransferFormState>({
    ...INITIAL_FORM_STATE,
    orderId: orderIdParam ? Number(orderIdParam) : '',
  });
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- DROPDOWN OPTIONS ---
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string }[]>([]);
  const [batches, setBatches] = useState<{ id: number; variantId: number; batchCode: string }[]>(
    []
  );
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
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
        setVariants(varList.map((v: any) => ({ value: v.id, label: `${v.code} - ${v.name}` })));
        setBatches(
          batchList.map((b: any) => ({ id: b.id, variantId: b.variantId, batchCode: b.batchCode }))
        );
        setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
      } catch (err) {
        showToast('error', 'Lỗi tải danh mục bổ trợ!');
      }
    };
    loadInit();
  }, []);

  const loadOrder = useCallback(async (id: number) => {
    try {
      const ord = await orderApi.getById(id);
      if (ord) {
        setFormData((prev) => ({
          ...prev,
          orderId: ord.id,
          toWarehouseId: ord.warehouseId || '',
        }));

        if (ord.details && ord.details.length > 0) {
          const rows: DetailRow[] = ord.details.map((d) => ({
            id: crypto.randomUUID(),
            variantId: d.variantId,
            batchId: '',
            uoMId: d.uoMId,
            quantity: d.quantity,
          }));
          setDetails(rows);
        }
      }
    } catch (err) {
      showToast('error', 'Không thể tải thông tin đơn hàng!');
    }
  }, []);

  useEffect(() => {
    if (orderIdParam) {
      loadOrder(Number(orderIdParam));
    }
  }, [orderIdParam, loadOrder]);

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

  const handleDetailChange = (id: string, field: keyof DetailRow, value: any) => {
    setDetails((prev) =>
      prev.map((row) => {
        if (row.id !== id) return row;
        return { ...row, [field]: value };
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
                options={warehouses}
                error={errors.fromWarehouseId}
                required
              />

              <FormSelect
                label="Kho đích (Tiếp nhận)"
                value={formData.toWarehouseId}
                onSelect={(val) => handleFieldChange('toWarehouseId', val ? Number(val) : '')}
                options={warehouses}
                error={errors.toWarehouseId}
                required
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
          <FormSection title="2. Danh Sách Mặt Hàng & Lô Hàng Chuyển Đi">
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3 rounded-lg border border-rose-200">
                {errors.details}
              </div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-xl bg-white shadow-sm mb-2">
              <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3 text-center w-12">#</th>
                    <th className="px-4 py-3 min-w-60">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-4 py-3 min-w-50">
                      Lô Hàng (Batch) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-4 py-3 min-w-30">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-4 py-3 w-32 text-center bg-indigo-50/40">
                      Số lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="px-4 py-3 w-16 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches = batches.filter((b) => b.variantId === Number(row.variantId));
                    return (
                      <tr key={row.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-4 py-2 text-center text-slate-400 font-medium">
                          {idx + 1}
                        </td>

                        <td className="p-2">
                          <FormSelect
                            label=""
                            showSearch
                            placeholder="Chọn sản phẩm..."
                            options={variants}
                            value={row.variantId}
                            error={errors[`variantId_${row.id}`]}
                            onSelect={(val) => {
                              handleDetailChange(row.id, 'variantId', val ? Number(val) : '');
                              handleDetailChange(row.id, 'batchId', '');
                            }}
                          />
                        </td>

                        <td className="p-2">
                          <FormSelect
                            label=""
                            placeholder="-- Chọn Lô --"
                            options={rowBatches.map((b) => ({ value: b.id, label: b.batchCode }))}
                            value={row.batchId}
                            error={errors[`batchId_${row.id}`]}
                            disabled={!row.variantId}
                            onSelect={(val) =>
                              handleDetailChange(row.id, 'batchId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        <td className="p-2">
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

                        <td className="p-2 bg-indigo-50/20 border-l border-indigo-100">
                          <FormInput
                            label=""
                            type="number"
                            className="text-center font-bold text-indigo-700"
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

                        <td className="p-2 text-center border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors disabled:opacity-20 mx-auto"
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
              <div className="p-3 bg-slate-50/80 border-t border-slate-200 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-indigo-600 hover:bg-indigo-100 rounded-lg transition-colors"
                >
                  <Plus size={16} /> THÊM MẶT HÀNG
                </button>
              </div>
            </div>
          </FormSection>

          {/* ================= FOOTER & ACTION BUTTONS ================= */}
          <div className="flex justify-end gap-3 pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/inventory-transfers')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-xl hover:bg-slate-50 transition-colors shadow-sm"
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

export default InventoryTransferForm;
