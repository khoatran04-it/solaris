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
import { Toast } from '../../components/commons/Toast';

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { orderApi } from '../../api/orderApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// 🔥 IMPORT TYPE CHUẨN
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
  issueDate: string;
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  note: string;
}

const INITIAL_FORM_STATE: IssueFormState = {
  orderId: '',
  warehouseId: '',
  issueDate: new Date().toLocaleDateString('en-CA'),
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

  // --- DROPDOWN OPTIONS ---
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
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
        setVariants(
          varList.map((v: any) => ({
            value: v.id,
            label: `${v.code} - ${v.name}`,
            prices: v.prices || [],
          }))
        );
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
          warehouseId: ord.warehouseId || '',
          receiverName: ord.receiverName || ord.customerName,
          receiverPhone: ord.receiverPhone || ord.customerPhone,
          deliveryAddress: ord.deliveryAddress || '',
        }));

        if (ord.details && ord.details.length > 0) {
          const rows: DetailRow[] = ord.details.map((d) => {
            const unissued = Math.max(0, d.quantity - (d.issuedQuantity || 0));
            return {
              id: crypto.randomUUID(),
              orderDetailId: d.id,
              variantId: d.variantId,
              batchId: '',
              uoMId: d.uoMId,
              quantity: unissued > 0 ? unissued : d.quantity,
              unitPrice: d.unitPrice,
            };
          });
          setDetails(rows);
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
    if (!formData.warehouseId)
      return showToast('warning', 'Vui lòng chọn Kho xuất ở mục (1) trước!');
    if (!variantId) return showToast('warning', 'Vui lòng chọn Sản phẩm trước!');

    const targetRow = details.find((d) => d.id === rowId);
    if (!targetRow) return;

    try {
      const suggestions: SuggestedBatch[] = await inventoryIssueApi.getSuggestedBatches(
        Number(formData.warehouseId),
        Number(variantId),
        Number(targetRow.quantity)
      );

      if (suggestions && suggestions.length > 0) {
        const topBatch = suggestions[0];
        handleDetailChange(rowId, 'batchId', topBatch.batchId);
        showToast('success', `Đã tự động chọn Lô tối ưu FEFO: ${topBatch.batchCode}`);
      } else {
        showToast('warning', 'Không tìm thấy Lô hàng nào có sẵn trong kho này!');
      }
    } catch (err) {
      showToast('error', 'Lỗi hệ thống: Không thể lấy gợi ý Lô hàng!');
    }
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.warehouseId) errs.warehouseId = 'Vui lòng chọn kho xuất';
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
      const payload: InventoryIssueCreatePayload = {
        orderId: formData.orderId ? Number(formData.orderId) : undefined,
        warehouseId: Number(formData.warehouseId),
        issuedById: userInfo?.id || 1,
        issueDate: `${formData.issueDate}T00:00:00Z`,
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
              <FormSelect
                label="Kho xuất hàng"
                value={formData.warehouseId}
                onSelect={(val) => handleFieldChange('warehouseId', val ? Number(val) : '')}
                options={warehouses}
                error={errors.warehouseId}
                required
              />

              <FormInput
                label="Ngày xuất hàng"
                type="date"
                value={formData.issueDate}
                onChange={(e) => handleFieldChange('issueDate', e.target.value)}
                required
              />

              <FormInput
                label="Người nhận hàng"
                value={formData.receiverName}
                onChange={(e) => handleFieldChange('receiverName', e.target.value)}
                placeholder="Nguyễn Văn A"
              />

              <FormInput
                label="Số điện thoại nhận"
                value={formData.receiverPhone}
                onChange={(e) => handleFieldChange('receiverPhone', e.target.value)}
                placeholder="0901234567"
              />

              <div className="md:col-span-2">
                <FormInput
                  label="Địa chỉ giao hàng"
                  value={formData.deliveryAddress}
                  onChange={(e) => handleFieldChange('deliveryAddress', e.target.value)}
                  error={errors.deliveryAddress}
                  required
                />
              </div>

              <div className="md:col-span-2 lg:col-span-3">
                <FormTextarea
                  label="Ghi chú xuất kho"
                  value={formData.note}
                  onChange={(e: any) => handleFieldChange('note', e.target.value)}
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
                    <th className="px-4 py-3 w-36 text-right">Đơn giá</th>
                    <th className="px-4 py-3 w-36 text-right">Thành tiền</th>
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
                            disabled={Boolean(row.orderDetailId)}
                          />
                        </td>

                        <td className="p-2">
                          <div className="flex items-center gap-1.5">
                            <div className="flex-1">
                              <FormSelect
                                label=""
                                placeholder="-- Chọn Lô --"
                                options={rowBatches.map((b) => ({
                                  value: b.id,
                                  label: b.batchCode,
                                }))}
                                value={row.batchId}
                                error={errors[`batchId_${row.id}`]}
                                disabled={!row.variantId}
                                onSelect={(val) =>
                                  handleDetailChange(row.id, 'batchId', val ? Number(val) : '')
                                }
                              />
                            </div>
                            <button
                              type="button"
                              onClick={() => handleAutoSuggestBatch(row.id, row.variantId)}
                              disabled={!row.variantId || !formData.warehouseId}
                              className="p-2 bg-indigo-50 text-indigo-600 hover:bg-indigo-100 rounded-lg transition-colors shrink-0 disabled:opacity-40"
                              title="Tự động chọn Lô hết hạn trước (FEFO)"
                            >
                              <Sparkles size={16} />
                            </button>
                          </div>
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
                            disabled={Boolean(row.orderDetailId)}
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

                        <td className="p-2">
                          <FormInput
                            label=""
                            type="number"
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

                        <td className="px-4 py-2 text-right font-black text-slate-800">
                          {(Number(row.quantity || 0) * Number(row.unitPrice || 0)).toLocaleString(
                            'vi-VN'
                          )}{' '}
                          đ
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

export default InventoryIssueForm;
