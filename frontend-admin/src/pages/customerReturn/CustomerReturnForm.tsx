import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { RotateCcw, Plus, Trash2, Save } from 'lucide-react';

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

import { customerReturnApi } from '../../api/customerReturnApi';
import { orderApi } from '../../api/orderApi';
import { customerApi } from '../../api/customerApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// 🔥 IMPORT TYPE CHUẨN TỪ API
import { CustomerReturnCreatePayload } from '../../types/customerReturn';

// --- FORM STATE TYPES ---
interface DetailRow {
  id: string; // Khóa tạm thời cho React list mapping
  variantId: number | '';
  batchId: number | '';
  uoMId: number | '';
  returnedQuantity: number;
  unitPrice: number;
}

interface ReturnFormState {
  orderId: number | '';
  customerId: number | '';
  warehouseId: number | '';
  returnDate: string;
  reason: string;
}

const INITIAL_FORM_STATE: ReturnFormState = {
  orderId: '',
  customerId: '',
  warehouseId: '',
  returnDate: new Date().toLocaleDateString('en-CA'),
  reason: ''
};

const createEmptyDetailRow = (): DetailRow => ({
  id: crypto.randomUUID(),
  variantId: '',
  batchId: '',
  uoMId: '',
  returnedQuantity: 1,
  unitPrice: 0
});

const CustomerReturnForm: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const orderIdParam = searchParams.get('orderId');
  const { userInfo } = useAuthStore();

  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'warning' | 'error'; message: string }>({
    show: false,
    type: 'success',
    message: '',
  });

  // --- STATES ---
  const [formData, setFormData] = useState<ReturnFormState>({
      ...INITIAL_FORM_STATE,
      orderId: orderIdParam ? Number(orderIdParam) : ''
  });
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- DROPDOWN OPTIONS ---
  const [customers, setCustomers] = useState<{ value: number; label: string }[]>([]);
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
  const [batches, setBatches] = useState<{ id: number; variantId: number; batchCode: string }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadInit = async () => {
      try {
        const [custList, whList, varList, batchList, uomList] = await Promise.all([
          customerApi.getAllList().catch(() => []),
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          productBatchApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
        ]);

        setCustomers(custList.map((c: any) => ({ value: c.id, label: `${c.code} - ${c.name}` })));
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

  const loadOrder = useCallback(async (id: number) => {
    try {
      const ord = await orderApi.getById(id);
      if (ord) {
        setFormData(prev => ({
            ...prev,
            orderId: ord.id,
            customerId: ord.customerId,
            warehouseId: ord.warehouseId || ''
        }));

        if (ord.details && ord.details.length > 0) {
          const rows: DetailRow[] = ord.details.map(d => ({
            id: crypto.randomUUID(),
            variantId: d.variantId,
            batchId: '',
            uoMId: d.uoMId,
            returnedQuantity: d.quantity, // Default to full quantity
            unitPrice: d.unitPrice
          }));
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
  const handleFieldChange = (field: keyof ReturnFormState, value: any) => {
      setFormData(prev => ({ ...prev, [field]: value }));
      if (errors[field]) {
          setErrors(prev => {
              const newErr = { ...prev };
              delete newErr[field];
              return newErr;
          });
      }
  };

  const handleAddRow = () => {
    setDetails(prev => [...prev, createEmptyDetailRow()]);
  };

  const handleRemoveRow = (id: string) => {
      if (details.length > 1) {
          setDetails(prev => prev.filter(row => row.id !== id));
      }
  };

  const handleDetailChange = (id: string, field: keyof DetailRow, value: any) => {
      setDetails(prev => prev.map(row => {
          if (row.id !== id) return row;
          const updated = { ...row, [field]: value };

          // Auto-fill UoM & Giá khi chọn SP
          if (field === 'variantId' && value) {
              const v = variants.find(item => item.value === Number(value));
              if (v && v.prices && v.prices.length > 0) {
                  const defPrice = v.prices.find((p: any) => p.isDefault) || v.prices[0];
                  if (defPrice) {
                      updated.uoMId = defPrice.uoMId;
                      updated.unitPrice = defPrice.price;
                  }
              }
          }
          return updated;
      }));

      if (errors[`${field}_${id}`]) {
          setErrors(prev => {
              const newErr = { ...prev };
              delete newErr[`${field}_${id}`];
              return newErr;
          });
      }
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.orderId) errs.orderId = 'Vui lòng nhập/chọn mã đơn hàng gốc';
    if (!formData.customerId) errs.customerId = 'Vui lòng chọn khách hàng';
    if (!formData.warehouseId) errs.warehouseId = 'Vui lòng chọn kho tiếp nhận';
    if (!formData.reason.trim()) errs.reason = 'Vui lòng nhập lý do trả hàng';

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 mặt hàng trả lại';
    } else {
      details.forEach((d) => {
        if (!d.variantId) errs[`variantId_${d.id}`] = 'Bắt buộc';
        if (!d.batchId) errs[`batchId_${d.id}`] = 'Bắt buộc';
        if (!d.uoMId) errs[`uoMId_${d.id}`] = 'Bắt buộc';
        if (Number(d.returnedQuantity) <= 0) errs[`quantity_${d.id}`] = '> 0';
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
      const payload: CustomerReturnCreatePayload = {
        orderId: Number(formData.orderId),
        customerId: Number(formData.customerId),
        warehouseId: Number(formData.warehouseId),
        receivedById: userInfo?.id || 1,
        // Ép múi giờ UTC an toàn
        returnDate: new Date(`${formData.returnDate}T12:00:00Z`).toISOString(),
        reason: formData.reason.trim(),
        details: details.map(d => ({
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          returnedQuantity: Number(d.returnedQuantity),
          unitPrice: Number(d.unitPrice)
        }))
      };

      const res = await customerReturnApi.create(payload);
      showToast('success', 'TẠO PHIẾU TRẢ HÀNG THÀNH CÔNG! CHỜ BỘ PHẬN QC KIỂM ĐỊNH.');
      setTimeout(() => navigate(`/customer-returns/${res.id}`), 1200);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể tạo phiếu trả hàng!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Phiếu Trả Hàng (RMA)"
        subtitle="Tiếp nhận hàng hoàn trả từ khách hàng & Chờ kiểm định QC"
        icon={RotateCcw}
        onBack={() => navigate('/customer-returns')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* ================= SECTION 1: THÔNG TIN PHIẾU ================= */}
          <FormSection title="1. Thông Tin Phiếu Trả Hàng">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormInput
                label="Mã ID đơn hàng gốc (OrderId)"
                type="number"
                value={formData.orderId}
                onChange={(e) => {
                  const val = e.target.value;
                  handleFieldChange('orderId', val ? Number(val) : '');
                  if (val) loadOrder(Number(val));
                }}
                error={errors.orderId}
                required
              />

              <FormSelect
                label="Khách hàng hoàn trả"
                value={formData.customerId}
                onSelect={(val) => handleFieldChange('customerId', val ? Number(val) : '')}
                options={customers}
                error={errors.customerId}
                required
                showSearch
              />

              <FormSelect
                label="Kho tiếp nhận hàng về"
                value={formData.warehouseId}
                onSelect={(val) => handleFieldChange('warehouseId', val ? Number(val) : '')}
                options={warehouses}
                error={errors.warehouseId}
                required
              />

              <FormInput
                label="Ngày tiếp nhận"
                type="date"
                value={formData.returnDate}
                onChange={(e) => handleFieldChange('returnDate', e.target.value)}
                required
              />

              <div className="md:col-span-2">
                <FormTextarea
                  label="Lý do khách trả hàng"
                  value={formData.reason}
                  onChange={(e: any) => handleFieldChange('reason', e.target.value)}
                  error={errors.reason}
                  rows={2}
                  placeholder="Sản phẩm dập nát khi giao, giao sai quy cách..."
                  required
                />
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 2: CHI TIẾT MẶT HÀNG ================= */}
          <FormSection title="2. Chi Tiết Mặt Hàng Hoàn Trả">
            {errors.details && (
              <div className="mb-4 text-rose-500 text-sm font-bold">{errors.details}</div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-xl bg-white shadow-sm mb-2">
              <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3 text-center w-12">#</th>
                    <th className="px-4 py-3 min-w-60">Sản phẩm <span className="text-red-500">*</span></th>
                    <th className="px-4 py-3 min-w-50">Lô Hàng (Batch) <span className="text-red-500">*</span></th>
                    <th className="px-4 py-3 min-w-30">ĐVT <span className="text-red-500">*</span></th>
                    <th className="px-4 py-3 w-32 text-center bg-indigo-50/40">SL Trả <span className="text-red-500">*</span></th>
                    <th className="px-4 py-3 w-36 text-right">Đơn giá</th>
                    <th className="px-4 py-3 w-36 text-right">Thành tiền</th>
                    <th className="px-4 py-3 w-16 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches = batches.filter(b => b.variantId === Number(row.variantId));
                    return (
                      <tr key={row.id} className="hover:bg-slate-50/50 transition-colors">
                        <td className="px-4 py-2 text-center text-slate-400 font-medium">{idx + 1}</td>
                        
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
                              options={rowBatches.map(b => ({ value: b.id, label: b.batchCode }))}
                              value={row.batchId}
                              error={errors[`batchId_${row.id}`]}
                              disabled={!row.variantId}
                              onSelect={(val) => handleDetailChange(row.id, 'batchId', val ? Number(val) : '')}
                          />
                        </td>
                        
                        <td className="p-2">
                          <FormSelect
                              label=""
                              placeholder="ĐVT"
                              options={uoms}
                              value={row.uoMId}
                              error={errors[`uoMId_${row.id}`]}
                              onSelect={(val) => handleDetailChange(row.id, 'uoMId', val ? Number(val) : '')}
                          />
                        </td>
                        
                        <td className="p-2 bg-indigo-50/20 border-l border-indigo-100">
                          <FormInput
                              label=""
                              type="number"
                              className="text-center font-bold text-indigo-700"
                              value={row.returnedQuantity}
                              error={errors[`quantity_${row.id}`]}
                              onChange={(e) => handleDetailChange(row.id, 'returnedQuantity', parseFloat(e.target.value) || 0)}
                          />
                        </td>

                        <td className="p-2">
                          <FormInput
                              label=""
                              type="number"
                              className="text-right font-medium text-slate-700"
                              value={row.unitPrice}
                              onChange={(e) => handleDetailChange(row.id, 'unitPrice', parseFloat(e.target.value) || 0)}
                          />
                        </td>

                        <td className="px-4 py-2 text-right font-black text-slate-800">
                            {((Number(row.returnedQuantity || 0) * Number(row.unitPrice || 0))).toLocaleString('vi-VN')} đ
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
                  onClick={() => navigate('/customer-returns')}
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

export default CustomerReturnForm;