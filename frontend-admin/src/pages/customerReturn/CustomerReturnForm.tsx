import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { RotateCcw, Plus, Trash2, ShoppingBag, Info, AlertCircle, CheckCircle } from 'lucide-react';

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
import CustomDatePicker from '../../components/commons/CustomDatePicker';

import { customerReturnApi } from '../../api/customerReturnApi';
import { orderApi } from '../../api/orderApi';
import { customerApi } from '../../api/customerApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productBatchApi } from '../../api/productBatchApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';

// Types
import { CustomerReturnCreatePayload } from '../../types/customerReturn';
import { Order, OrderStatusLabels } from '../../types/order';

interface DetailRow {
  id: string;
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
  reason: '',
};

const createEmptyDetailRow = (): DetailRow => ({
  id: crypto.randomUUID(),
  variantId: '',
  batchId: '',
  uoMId: '',
  returnedQuantity: 1,
  unitPrice: 0,
});

const CustomerReturnForm: React.FC = () => {
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
  const [formData, setFormData] = useState<ReturnFormState>({
    ...INITIAL_FORM_STATE,
    orderId: orderIdParam ? Number(orderIdParam) : '',
  });
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- DROPDOWN OPTIONS ---
  const [orderOptions, setOrderOptions] = useState<
    { value: number; label: string; order: Order }[]
  >([]);
  const [customers, setCustomers] = useState<{ value: number; label: string }[]>([]);
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

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val || 0);
  };

  // --- 1. TẢI TOÀN BỘ DANH MỤC BỔ TRỢ & DANH SÁCH ĐƠN BÁN HÀNG ---
  useEffect(() => {
    const loadInit = async () => {
      try {
        const [orderRes, custList, whList, varList, batchList, uomList] = await Promise.all([
          orderApi.getAll({ pageSize: 100 }).catch(() => ({ items: [] })),
          customerApi.getAllList().catch(() => []),
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          productBatchApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
        ]);

        const orderOpts = (orderRes.items || []).map((o: Order) => ({
          value: o.id,
          label: `${o.orderCode} - ${o.customerName} (${formatCurrency(o.totalAmount)} - ${OrderStatusLabels[o.status]})`,
          order: o,
        }));

        setOrderOptions(orderOpts);
        setCustomers(custList.map((c: any) => ({ value: c.id, label: `${c.code} - ${c.name}` })));
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

  // --- 2. HÀM TỰ ĐỘNG SỔ THÔNG TIN TỪ ĐƠN HÀNG GỐC ---
  const loadOrderAndPopulate = useCallback(async (id: number) => {
    try {
      setLoading(true);
      const ord = await orderApi.getById(id);
      if (ord) {
        setSelectedOrder(ord);
        setFormData((prev) => ({
          ...prev,
          orderId: ord.id,
          customerId: ord.customerId,
          warehouseId: ord.warehouseId || '',
        }));

        // 1. Ưu tiên cao nhất: Tự động trích xuất các Lô hàng (Batch) thực tế đã xuất kho theo thuật toán FEFO
        if (ord.issuedItems && ord.issuedItems.length > 0) {
          const rows: DetailRow[] = ord.issuedItems.map((item) => ({
            id: crypto.randomUUID(),
            variantId: item.variantId,
            batchId: item.batchId, // Tự động điền chính xác Lô hàng đã xuất cho khách!
            uoMId: item.uoMId,
            returnedQuantity: item.quantityIssued,
            unitPrice: item.unitPrice,
          }));
          setDetails(rows);
          showToast(
            'success',
            `Đã liên kết đơn hàng ${ord.orderCode} & tự động điền ${rows.length} mặt hàng theo đúng Lô hàng đã xuất kho!`
          );
        } else if (ord.details && ord.details.length > 0) {
          // 2. Fallback nếu đơn chưa hoàn tất phiếu xuất: Lấy theo danh sách mặt hàng đặt
          const rows: DetailRow[] = ord.details.map((d) => {
            const vBatches = batches.filter((b) => b.variantId === d.variantId);
            return {
              id: crypto.randomUUID(),
              variantId: d.variantId,
              batchId: vBatches.length > 0 ? vBatches[0].id : '',
              uoMId: d.uoMId,
              returnedQuantity: d.issuedQuantity > 0 ? d.issuedQuantity : d.quantity,
              unitPrice: d.unitPrice,
            };
          });
          setDetails(rows);
          showToast(
            'success',
            `Đã liên kết đơn hàng ${ord.orderCode} & tự động điền ${rows.length} mặt hàng!`
          );
        } else {
          setDetails([createEmptyDetailRow()]);
        }
      }
    } catch (err) {
      showToast('error', 'Không thể tải thông tin đơn hàng gốc!');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (orderIdParam) {
      loadOrderAndPopulate(Number(orderIdParam));
    }
  }, [orderIdParam, loadOrderAndPopulate]);

  // --- FORM HANDLERS ---
  const handleFieldChange = (field: keyof ReturnFormState, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[field];
        return newErr;
      });
    }
  };

  const handleSelectOrder = (val: string | number) => {
    const numId = Number(val);
    handleFieldChange('orderId', numId || '');
    if (numId) {
      loadOrderAndPopulate(numId);
    } else {
      setSelectedOrder(null);
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

  // --- TÍNH TOÁN TỔNG TIỀN DỰ KIẾN HOÀN ---
  const totalEstimatedRefund = details.reduce((sum, d) => {
    const qty = Number(d.returnedQuantity) || 0;
    const price = Number(d.unitPrice) || 0;
    return sum + qty * price;
  }, 0);

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.orderId) errs.orderId = 'Vui lòng chọn đơn bán hàng gốc';
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
        returnDate: `${formData.returnDate}T00:00:00Z`,
        reason: formData.reason.trim(),
        details: details.map((d) => ({
          variantId: Number(d.variantId),
          batchId: Number(d.batchId),
          uoMId: Number(d.uoMId),
          returnedQuantity: Number(d.returnedQuantity),
          unitPrice: Number(d.unitPrice),
        })),
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
        title="Tạo Phiếu Đổi Trả Hàng (RMA)"
        subtitle="Liên kết đơn bán hàng & Tự động trích xuất thông tin khách hàng, kho và mặt hàng hoàn trả"
        icon={RotateCcw}
        onBack={() => navigate('/customer-returns')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* ================= SECTION 1: LIÊN KẾT ĐƠN HÀNG GỐC ================= */}
          <FormSection title="1. Đơn Bán Hàng Gốc (Sales Order Reference)">
            <div className="flex flex-col gap-4">
              <FormSelect
                label="Chọn Đơn Bán Hàng gốc cần hoàn trả"
                value={formData.orderId}
                onSelect={handleSelectOrder}
                options={orderOptions}
                error={errors.orderId}
                required
                showSearch
                searchPlaceholder="Tìm kiếm theo mã đơn (Ví dụ: ORD-20260817...), tên khách hàng..."
                placeholder="-- Chọn Đơn Bán Hàng gốc để tự động điền --"
              />

              {/* THẺ TỔNG QUAN ĐƠN HÀNG GỐC KHI ĐƯỢC CHỌN */}
              {selectedOrder && (
                <div className="p-4 bg-amber-50/50 border border-amber-200/80 rounded-2xl flex flex-col md:flex-row items-start md:items-center justify-between gap-4 animate-in fade-in duration-300 shadow-2xs">
                  <div className="flex items-center gap-3.5">
                    <div className="p-3 bg-amber-500 text-slate-900 rounded-xl shadow-xs">
                      <ShoppingBag size={22} strokeWidth={2.5} />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-black text-slate-900 text-base">
                          {selectedOrder.orderCode}
                        </span>
                        <span className="px-2.5 py-0.5 bg-amber-100 text-amber-900 border border-amber-300 text-xs font-bold rounded-lg">
                          {OrderStatusLabels[selectedOrder.status]}
                        </span>
                      </div>
                      <div className="text-xs text-slate-600 mt-1">
                        Khách hàng:{' '}
                        <strong className="text-slate-900 font-bold">
                          {selectedOrder.customerName}
                        </strong>
                        {selectedOrder.customerPhone && (
                          <span className="text-slate-500 font-medium">
                            {' '}
                            • SĐT: {selectedOrder.customerPhone}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-8 text-right pr-2">
                    <div>
                      <div className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Kho xuất hàng
                      </div>
                      <div className="text-xs font-bold text-slate-800 mt-0.5">
                        {selectedOrder.warehouseName || 'Chưa gán kho'}
                      </div>
                    </div>
                    <div className="border-l border-amber-200 pl-8">
                      <div className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                        Tổng tiền đơn gốc
                      </div>
                      <div className="text-base font-black text-slate-900 mt-0.5">
                        {formatCurrency(selectedOrder.totalAmount)}
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </FormSection>

          {/* ================= SECTION 2: THÔNG TIN PHIẾU TIẾP NHẬN ================= */}
          <FormSection title="2. Thông Tin Phiếu Tiếp Nhận">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormSelect
                label="Khách hàng hoàn trả"
                value={formData.customerId}
                onSelect={(val) => handleFieldChange('customerId', val ? Number(val) : '')}
                options={customers}
                error={errors.customerId}
                required
                showSearch
                searchPlaceholder="Tìm khách hàng..."
                placeholder="-- Chọn khách hàng --"
              />

              <FormSelect
                label="Kho tiếp nhận hàng về"
                value={formData.warehouseId}
                onSelect={(val) => handleFieldChange('warehouseId', val ? Number(val) : '')}
                options={warehouses}
                error={errors.warehouseId}
                required
                showSearch
                searchPlaceholder="Tìm kho..."
                placeholder="-- Chọn kho tiếp nhận --"
              />

              <CustomDatePicker
                label="Ngày tiếp nhận"
                value={formData.returnDate ? new Date(formData.returnDate) : null}
                onChange={(date) =>
                  handleFieldChange('returnDate', date ? date.toLocaleDateString('en-CA') : '')
                }
                required
              />

              <div className="md:col-span-2 lg:col-span-3">
                <FormTextarea
                  label="Lý do khách trả hàng"
                  value={formData.reason}
                  onChange={(e: any) => handleFieldChange('reason', e.target.value)}
                  error={errors.reason}
                  rows={2}
                  placeholder="Sản phẩm dập nát khi giao, giao sai quy cách, lỗi chất lượng..."
                  required
                />
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 3: CHI TIẾT MẶT HÀNG HOÀN TRẢ ================= */}
          <FormSection title="3. Chi Tiết Mặt Hàng Hoàn Trả">
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3.5 rounded-xl border border-rose-200 text-xs flex items-center gap-1.5">
                <AlertCircle size={16} /> {errors.details}
              </div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-2xs mb-4 min-h-[300px]">
              <table className="w-full text-sm text-left border-collapse min-w-[850px]">
                <thead className="bg-slate-50/80 border-b border-slate-200 font-bold text-slate-500 uppercase text-xs">
                  <tr>
                    <th className="px-3 py-3.5 w-10 text-center">#</th>
                    <th className="px-3 py-3.5 w-[30%] min-w-[190px]">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-[26%] min-w-[170px]">
                      Lô Hàng (Batch) <span className="text-red-500">*</span>
                    </th>
                    <th className="px-2 py-3.5 w-24 min-w-[85px]">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-28 text-center bg-amber-50/70 text-amber-900">
                      SL Trả <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-36 text-right">Đơn giá</th>
                    <th className="px-4 py-3.5 w-40 text-right">Thành tiền</th>
                    <th className="px-2 py-3.5 w-10 text-center"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const rowBatches = batches.filter((b) => b.variantId === Number(row.variantId));
                    const rowTotal =
                      (Number(row.returnedQuantity) || 0) * (Number(row.unitPrice) || 0);

                    return (
                      <tr
                        key={row.id}
                        className="hover:bg-slate-50/60 transition-colors"
                        style={{ zIndex: 50 - idx }}
                      >
                        <td className="px-3 py-3 text-center text-slate-400 font-medium">
                          {idx + 1}
                        </td>

                        <td className="p-2">
                          <FormSelect
                            label=""
                            showSearch
                            searchPlaceholder="Tìm sản phẩm..."
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
                            showSearch
                            searchPlaceholder="Tìm mã lô..."
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

                        <td className="p-2 bg-amber-50/30 border-l border-amber-100 text-center">
                          <input
                            type="number"
                            min="1"
                            className="w-full text-center font-bold text-amber-950 bg-white border border-amber-300 rounded-xl py-2 px-2 text-xs focus:ring-2 focus:ring-amber-400 outline-none shadow-2xs"
                            value={row.returnedQuantity}
                            onChange={(e) =>
                              handleDetailChange(
                                row.id,
                                'returnedQuantity',
                                Math.max(1, parseFloat(e.target.value) || 0)
                              )
                            }
                          />
                          {errors[`quantity_${row.id}`] && (
                            <span className="text-red-500 text-[10px] block mt-1">
                              {errors[`quantity_${row.id}`]}
                            </span>
                          )}
                        </td>

                        <td className="p-2 text-right">
                          <input
                            type="number"
                            min="0"
                            className="w-full text-right font-medium text-slate-800 bg-white border border-slate-200 rounded-xl py-2 px-3 text-xs focus:ring-2 focus:ring-amber-400 outline-none shadow-2xs"
                            value={row.unitPrice}
                            onChange={(e) =>
                              handleDetailChange(
                                row.id,
                                'unitPrice',
                                Math.max(0, parseFloat(e.target.value) || 0)
                              )
                            }
                          />
                        </td>

                        <td className="px-4 py-2 text-right font-black text-slate-900 text-xs">
                          {formatCurrency(rowTotal)}
                        </td>

                        <td className="px-2 py-2 text-center">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            disabled={details.length === 1}
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors disabled:opacity-20 cursor-pointer"
                            title="Xóa dòng"
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
                <tfoot className="bg-slate-50/80 border-t border-slate-200">
                  <tr>
                    <td
                      colSpan={5}
                      className="px-4 py-3.5 text-right font-bold text-slate-600 uppercase text-xs tracking-wider"
                    >
                      Tổng tiền dự kiến hoàn trả:
                    </td>
                    <td
                      colSpan={2}
                      className="px-4 py-3.5 text-right font-black text-rose-600 text-base"
                    >
                      {formatCurrency(totalEstimatedRefund)}
                    </td>
                    <td></td>
                  </tr>
                </tfoot>
              </table>
            </div>

            <div className="flex justify-start">
              <button
                type="button"
                onClick={handleAddRow}
                className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-amber-300 bg-amber-50/50 text-amber-800 rounded-xl text-xs font-bold hover:bg-amber-100/60 hover:border-amber-400 transition-all cursor-pointer shadow-2xs"
              >
                <Plus size={16} strokeWidth={2.5} /> Thêm Mặt Hàng Khác
              </button>
            </div>
          </FormSection>

          {/* ================= SUBMIT BUTTON ================= */}
          <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-100">
            <button
              type="button"
              onClick={() => navigate('/customer-returns')}
              className="px-6 py-3 bg-slate-100 text-slate-600 font-bold rounded-xl hover:bg-slate-200 transition-colors text-sm cursor-pointer"
            >
              Hủy Bỏ
            </button>

            <SubmitButton loading={loading} isEditMode={false} />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default CustomerReturnForm;
