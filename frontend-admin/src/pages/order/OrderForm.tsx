import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingBag, Plus, Trash2, Compass, AlertTriangle, CheckCircle, Save } from 'lucide-react';

// Common UI Components
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

// API & Types
import { orderApi } from '../../api/orderApi';
import { customerApi } from '../../api/customerApi';
import { customerAddressApi } from '../../api/customerAddressApi';
import { warehouseApi } from '../../api/warehouseApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
import { uomConversionApi } from '../../api/uomConversionApi';

import {
  OrderCreatePayload,
  PaymentMethod,
  PaymentMethodLabels,
  RoutingPreviewResult,
} from '../../types/order';

interface DetailRow {
  id: string; // Khóa tạm thời cho React list mapping
  variantId: number | '';
  uoMId: number | '';
  quantity: number;
  unitPrice: number;
  discountAmount: number;
}

interface OrderFormState {
  customerId: number | '';
  customerAddressId: number | '';
  receiverName: string;
  receiverPhone: string;
  deliveryAddress: string;
  warehouseId: number | '';
  paymentMethod: PaymentMethod;
  shippingFee: number;
  note: string;
}

const INITIAL_FORM_STATE: OrderFormState = {
  customerId: '',
  customerAddressId: '',
  receiverName: '',
  receiverPhone: '',
  deliveryAddress: '',
  warehouseId: '',
  paymentMethod: PaymentMethod.COD,
  shippingFee: 0,
  note: '',
};

const createEmptyDetailRow = (): DetailRow => ({
  id: crypto.randomUUID(),
  variantId: '',
  uoMId: '',
  quantity: 1,
  unitPrice: 0,
  discountAmount: 0,
});

const OrderForm: React.FC = () => {
  const navigate = useNavigate();

  // --- STATES ---
  const [formData, setFormData] = useState<OrderFormState>(INITIAL_FORM_STATE);
  const [details, setDetails] = useState<DetailRow[]>([createEmptyDetailRow()]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  // Dropdown Options
  const [customers, setCustomers] = useState<{ value: number; label: string }[]>([]);
  const [addresses, setAddresses] = useState<{ value: number; label: string; raw: any }[]>([]);
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string; prices: any[] }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);
  const [variantUoMsMap, setVariantUoMsMap] = useState<Record<number, { value: number; label: string }[]>>({});

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

  useEffect(() => {
    details.forEach((d) => {
      if (d.variantId) {
        fetchValidUoMs(Number(d.variantId));
      }
    });
  }, [details, fetchValidUoMs]);

  // Smart Routing State
  const [routingPreview, setRoutingPreview] = useState<RoutingPreviewResult | null>(null);
  const [isRoutingLoading, setIsRoutingLoading] = useState(false);

  // Toast
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- EFFECTS ---
  useEffect(() => {
    const loadInitData = async () => {
      try {
        const [custList, whList, varList, uomList] = await Promise.all([
          customerApi.getAllList().catch(() => []),
          warehouseApi.getAllList().catch(() => []),
          productVariantApi.getAllList().catch(() => []),
          uomApi.getAllList().catch(() => []),
        ]);

        setCustomers(
          custList.map((c: any) => ({
            value: c.id,
            label: `${c.code} - ${c.name} (${c.phoneNumber})`,
          }))
        );
        // Nghiệp vụ SCM: Chỉ Kho Bán Lẻ mới được phép xuất bán trực tiếp cho khách hàng
        const retailWarehouses = whList.filter(
          (w: any) => !w.warehouseType || w.warehouseType === 'Kho Bán Lẻ'
        );
        setWarehouses(retailWarehouses.map((w: any) => ({ value: w.id, label: w.name })));
        setVariants(
          varList.map((v: any) => ({
            value: v.id,
            label: `${v.code} - ${v.name}`,
            prices: v.prices || [],
          }))
        );
        setUoms(uomList.map((u: any) => ({ value: u.id, label: u.name })));
      } catch (err) {
        showToast('error', 'Không thể tải danh mục bổ trợ!');
      }
    };
    loadInitData();
  }, []);

  // Load addresses when Customer changes
  useEffect(() => {
    if (!formData.customerId) {
      setAddresses([]);
      setFormData((prev) => ({
        ...prev,
        customerAddressId: '',
        receiverName: '',
        receiverPhone: '',
        deliveryAddress: '',
      }));
      return;
    }

    const loadCustomerAddresses = async () => {
      try {
        const addrList = await customerAddressApi.getByCustomerId(Number(formData.customerId));
        const formatted = addrList.map((a) => ({
          value: a.id,
          label: `${a.receiverName} (${a.phone}) - ${a.streetAddress}, ${a.ward}, ${a.district}, ${a.province}`,
          raw: a,
        }));
        setAddresses(formatted);

        // Auto select default address
        const def = addrList.find((a) => a.isDefault) || addrList[0];
        if (def) {
          setFormData((prev) => ({
            ...prev,
            customerAddressId: def.id,
            receiverName: def.receiverName || '',
            receiverPhone: def.phone || '',
            deliveryAddress: `${def.streetAddress}, ${def.ward}, ${def.district}, ${def.province}`,
          }));
        }
      } catch (err) {
        console.error('Lỗi tải danh sách địa chỉ:', err);
      }
    };
    loadCustomerAddresses();
  }, [formData.customerId]);

  // --- FORM FIELD HANDLERS ---
  const handleFieldChange = (field: keyof OrderFormState, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErr = { ...prev };
        delete newErr[field];
        return newErr;
      });
    }
  };

  const handleAddressSelect = (addrId: string | number) => {
    const idNum = Number(addrId);
    const selected = addresses.find((a) => a.value === idNum);
    if (selected && selected.raw) {
      setFormData((prev) => ({
        ...prev,
        customerAddressId: idNum,
        receiverName: selected.raw.receiverName || '',
        receiverPhone: selected.raw.phone || '',
        deliveryAddress: `${selected.raw.streetAddress}, ${selected.raw.ward}, ${selected.raw.district}, ${selected.raw.province}`,
      }));
    } else {
      handleFieldChange('customerAddressId', idNum);
    }
  };

  // --- TABLE ROW HANDLERS ---
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

        // Auto-fill UoM & Giá mặc định khi chọn Sản phẩm
        if (field === 'variantId' && value) {
          fetchValidUoMs(Number(value));
          const v = variants.find((item) => item.value === Number(value));
          if (v && v.prices && v.prices.length > 0) {
            const defPrice = v.prices.find((p) => p.isDefault) || v.prices[0];
            if (defPrice) {
              updated.uoMId = defPrice.uoMId;
              updated.unitPrice = defPrice.price;
            }
          }
        }

        // Cập nhật lại đơn giá tương ứng khi đổi ĐVT
        if (field === 'uoMId' && value) {
          const v = variants.find((item) => item.value === Number(updated.variantId));
          if (v && v.prices) {
            const matchingPrice = v.prices.find((p) => p.uoMId === Number(value));
            if (matchingPrice) {
              updated.unitPrice = matchingPrice.price;
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

  // --- TÍNH TOÁN TỔNG TIỀN ---
  const subTotal = details.reduce((sum, row) => {
    const qty = Number(row.quantity) || 0;
    const price = Number(row.unitPrice) || 0;
    const discount = Number(row.discountAmount) || 0;
    return sum + (qty * price - discount);
  }, 0);
  const totalAmount = subTotal + Number(formData.shippingFee || 0);

  // --- SMART ROUTING PREVIEW ---
  const handlePreviewRouting = async () => {
    const validItems = details.filter((d) => d.variantId && d.uoMId && Number(d.quantity) > 0);
    if (validItems.length === 0) {
      showToast('warning', 'Vui lòng chọn ít nhất 1 mặt hàng hợp lệ để định tuyến kho!');
      return;
    }

    try {
      setIsRoutingLoading(true);
      const payload: OrderCreatePayload = {
        customerId: Number(formData.customerId) || 1,
        customerAddressId: formData.customerAddressId
          ? Number(formData.customerAddressId)
          : undefined,
        receiverName: formData.receiverName,
        receiverPhone: formData.receiverPhone,
        deliveryAddress: formData.deliveryAddress,
        paymentMethod: formData.paymentMethod,
        details: validItems.map((d) => ({
          variantId: Number(d.variantId),
          uoMId: Number(d.uoMId),
          quantity: Number(d.quantity),
          unitPrice: Number(d.unitPrice),
          discountAmount: Number(d.discountAmount),
        })),
      };

      const result = await orderApi.previewRouting(payload);
      setRoutingPreview(result);
      if (result.optimalWarehouseId) {
        handleFieldChange('warehouseId', result.optimalWarehouseId);
        showToast(
          'success',
          `Đã chọn kho tối ưu: ${result.warehouseName} (Cách ${result.distanceKm} km)`
        );
      }
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể định tuyến kho!');
    } finally {
      setIsRoutingLoading(false);
    }
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const errs: Record<string, string> = {};
    if (!formData.customerId) errs.customerId = 'Vui lòng chọn khách hàng';
    if (!formData.deliveryAddress.trim()) errs.deliveryAddress = 'Vui lòng nhập địa chỉ giao hàng';

    if (details.length === 0) {
      errs.details = 'Cần ít nhất 1 mặt hàng trong đơn!';
    } else {
      details.forEach((row) => {
        if (!row.variantId) errs[`variantId_${row.id}`] = 'Bắt buộc';
        if (!row.uoMId) errs[`uoMId_${row.id}`] = 'Bắt buộc';
        if (Number(row.quantity) <= 0) errs[`quantity_${row.id}`] = '> 0';
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
      const payload: OrderCreatePayload = {
        customerId: Number(formData.customerId),
        customerAddressId: formData.customerAddressId
          ? Number(formData.customerAddressId)
          : undefined,
        receiverName: formData.receiverName.trim(),
        receiverPhone: formData.receiverPhone.trim(),
        deliveryAddress: formData.deliveryAddress.trim(),
        warehouseId: formData.warehouseId ? Number(formData.warehouseId) : undefined,
        paymentMethod: formData.paymentMethod,
        shippingFee: Number(formData.shippingFee),
        note: formData.note.trim(),
        details: details.map((d) => ({
          variantId: Number(d.variantId),
          uoMId: Number(d.uoMId),
          quantity: Number(d.quantity),
          unitPrice: Number(d.unitPrice),
          discountAmount: Number(d.discountAmount),
        })),
      };

      const res = await orderApi.create(payload);
      showToast('success', 'TẠO ĐƠN HÀNG THÀNH CÔNG!');
      setTimeout(() => navigate(`/orders/${res.id}`), 1000);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Có lỗi xảy ra khi tạo đơn hàng!');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title="Tạo Đơn Hàng Mới"
        subtitle="Lên đơn bán hàng & Tự động định tuyến kho tối ưu"
        icon={ShoppingBag}
        onBack={() => navigate('/orders')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* ================= SECTION 1: KHÁCH HÀNG & GIAO HÀNG ================= */}
          <FormSection title="1. Thông Tin Khách Hàng & Giao Hàng">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormSelect
                label="Khách hàng"
                required
                showSearch
                placeholder="Chọn khách hàng..."
                value={formData.customerId}
                options={customers}
                error={errors.customerId}
                onSelect={(val) => handleFieldChange('customerId', val ? Number(val) : '')}
              />

              <FormSelect
                label="Sổ địa chỉ đã lưu"
                placeholder="-- Chọn địa chỉ đã lưu --"
                value={formData.customerAddressId}
                options={addresses}
                disabled={!formData.customerId || addresses.length === 0}
                onSelect={handleAddressSelect}
              />

              <FormInput
                label="Người nhận hàng"
                placeholder="Nguyễn Văn A"
                value={formData.receiverName}
                onChange={(e) => handleFieldChange('receiverName', e.target.value)}
              />

              <FormInput
                label="Số điện thoại nhận"
                placeholder="0901234567"
                value={formData.receiverPhone}
                onChange={(e) => handleFieldChange('receiverPhone', e.target.value)}
              />

              <div className="md:col-span-2">
                <FormInput
                  label="Địa chỉ giao hàng chi tiết"
                  required
                  placeholder="Số nhà, tên đường, phường/xã, quận/huyện..."
                  value={formData.deliveryAddress}
                  error={errors.deliveryAddress}
                  onChange={(e) => handleFieldChange('deliveryAddress', e.target.value)}
                />
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 2: CHI TIẾT MẶT HÀNG ================= */}
          <FormSection title="2. Chi Tiết Mặt Hàng">
            {errors.details && (
              <div className="mb-4 text-rose-600 font-bold bg-rose-50 p-3 rounded-lg border border-rose-200">
                {errors.details}
              </div>
            )}

            <div className="overflow-x-auto border border-slate-200 rounded-2xl bg-white shadow-sm mb-2">
              <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3.5 w-10 text-center">#</th>
                    <th className="px-4 py-3.5 min-w-60">
                      Sản phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="px-4 py-3.5 min-w-30">
                      ĐVT <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-28 text-center bg-amber-50/50">
                      Số lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="px-3 py-3.5 w-36 text-right">Đơn giá</th>
                    <th className="px-3 py-3.5 w-32 text-right">Chiết khấu</th>
                    <th className="px-4 py-3.5 w-36 text-right">Thành tiền</th>
                    <th className="px-4 py-3.5 w-12 text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.map((row, idx) => {
                    const lineTotal =
                      Number(row.quantity || 0) * Number(row.unitPrice || 0) -
                      Number(row.discountAmount || 0);
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
                            onSelect={(val) =>
                              handleDetailChange(row.id, 'variantId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        <td className="p-2">
                          <FormSelect
                            label=""
                            placeholder="ĐVT"
                            options={
                              row.variantId && variantUoMsMap[Number(row.variantId)]
                                ? variantUoMsMap[Number(row.variantId)]
                                : uoms
                            }
                            value={row.uoMId}
                            error={errors[`uoMId_${row.id}`]}
                            onSelect={(val) =>
                              handleDetailChange(row.id, 'uoMId', val ? Number(val) : '')
                            }
                          />
                        </td>

                        <td className="p-2 bg-amber-50/30 border-l border-amber-100/80">
                          <FormInput
                            label=""
                            type="number"
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

                        <td className="p-2">
                          <FormInput
                            label=""
                            type="number"
                            className="text-right text-slate-600"
                            value={row.discountAmount}
                            onChange={(e) =>
                              handleDetailChange(
                                row.id,
                                'discountAmount',
                                parseFloat(e.target.value) || 0
                              )
                            }
                          />
                        </td>

                        <td className="px-4 py-2 text-right font-black text-slate-800">
                          {lineTotal.toLocaleString('vi-VN')} đ
                        </td>

                        <td className="p-2 text-center border-l border-slate-100">
                          <button
                            type="button"
                            onClick={() => handleRemoveRow(row.id)}
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors disabled:opacity-20 mx-auto cursor-pointer"
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
                  className="flex items-center gap-2 px-5 py-2.5 text-xs font-bold uppercase tracking-wider text-amber-950 bg-amber-50 hover:bg-amber-100 border border-amber-200/80 rounded-xl transition-all shadow-2xs cursor-pointer hover:scale-[1.02] active:scale-[0.98]"
                >
                  <Plus size={16} className="text-amber-600" />
                  <span>Thêm Mặt Hàng</span>
                </button>
              </div>
            </div>
          </FormSection>

          {/* ================= SECTION 3: ĐỊNH TUYẾN KHO & THANH TOÁN ================= */}
          <FormSection title="3. Định Tuyến Kho & Thanh Toán">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Cột Trái: Smart Routing Box */}
              <div className="p-6 bg-gradient-to-br from-amber-50/40 via-yellow-50/20 to-slate-50/50 border-2 border-amber-200/80 rounded-3xl flex flex-col justify-between shadow-sm">
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-extrabold text-amber-950 flex items-center gap-2 text-sm uppercase tracking-wide">
                      <Compass size={18} className="text-amber-600" />
                      Định Tuyến Kho Tự Động (Smart Routing)
                    </h4>
                    <button
                      type="button"
                      onClick={handlePreviewRouting}
                      disabled={isRoutingLoading}
                      className="px-4 py-2 bg-yellow-400 hover:bg-yellow-500 text-slate-950 rounded-xl text-xs font-black uppercase tracking-wider transition-all shadow-sm shadow-yellow-400/25 border border-yellow-400 disabled:opacity-50 flex items-center gap-1.5 cursor-pointer hover:scale-[1.02] active:scale-[0.98]"
                    >
                      <span>🧠 {isRoutingLoading ? 'Đang tính...' : 'Chạy Định Tuyến'}</span>
                    </button>
                  </div>
                  <p className="text-xs text-slate-600 mb-4 leading-relaxed">
                    Hệ thống sẽ tính khoảng cách Haversine từ địa chỉ khách hàng tới các kho và kiểm
                    tra tồn kho khả dụng để tự động chọn kho tối ưu nhất.
                  </p>

                  {routingPreview && (
                    <div className="p-4 bg-white/95 rounded-2xl border border-amber-200/90 text-xs flex flex-col gap-2.5 shadow-sm">
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-600">Kho tối ưu được chọn:</span>
                        <span className="font-black text-amber-950 text-sm">
                          {routingPreview.warehouseName} ({routingPreview.distanceKm} km)
                        </span>
                      </div>
                      <div className="flex items-center justify-between">
                        <span className="font-bold text-slate-600">Tình trạng tồn kho:</span>
                        {routingPreview.isFullyStocked ? (
                          <span className="text-emerald-700 font-bold flex items-center gap-1 bg-emerald-50 px-2.5 py-1 rounded-lg border border-emerald-200">
                            <CheckCircle size={13} /> Đủ 100% hàng xuất
                          </span>
                        ) : (
                          <span className="text-rose-700 font-bold flex items-center gap-1 bg-rose-50 px-2.5 py-1 rounded-lg border border-rose-200">
                            <AlertTriangle size={13} /> Thiếu {routingPreview.missingItems.length}{' '}
                            mặt hàng
                          </span>
                        )}
                      </div>
                      {routingPreview.suggestedSourceWarehouseName && (
                        <div className="pt-2 border-t border-amber-100 text-amber-950 font-medium bg-amber-50/50 p-2 rounded-xl">
                          💡 Gợi ý điều phối: Có thể lập lệnh chuyển kho từ{' '}
                          <strong className="text-amber-950 font-extrabold">
                            {routingPreview.suggestedSourceWarehouseName}
                          </strong>{' '}
                          về.
                        </div>
                      )}
                    </div>
                  )}
                </div>

                <div className="mt-4">
                  <FormSelect
                    label="Kho Xuất Bán (Chỉ áp dụng Kho Bán Lẻ)"
                    placeholder="-- Chọn Kho Bán Lẻ xuất hàng --"
                    options={warehouses}
                    value={formData.warehouseId}
                    error={errors.warehouseId}
                    onSelect={(val) => handleFieldChange('warehouseId', val ? Number(val) : '')}
                  />
                </div>
              </div>

              {/* Cột Phải: Thanh toán & Tổng tiền */}
              <div className="p-6 bg-slate-50/70 border border-slate-200 rounded-3xl flex flex-col justify-between shadow-sm">
                <div className="flex flex-col gap-4">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
                      Phương thức thanh toán
                    </label>
                    <div className="grid grid-cols-2 gap-2.5">
                      {Object.keys(PaymentMethodLabels).map((key) => {
                        const m = Number(key) as PaymentMethod;
                        const isSelected = formData.paymentMethod === m;
                        return (
                          <button
                            key={m}
                            type="button"
                            onClick={() => handleFieldChange('paymentMethod', m)}
                            className={`px-3 py-2.5 rounded-xl text-xs font-bold border transition-all text-center cursor-pointer ${
                              isSelected
                                ? 'bg-yellow-400 text-slate-950 border-yellow-400 font-black shadow-xs'
                                : 'bg-white text-slate-700 border-slate-200 hover:bg-slate-100'
                            }`}
                          >
                            {PaymentMethodLabels[m]}
                          </button>
                        );
                      })}
                    </div>
                  </div>

                  <FormInput
                    label="Phí vận chuyển (VND)"
                    type="number"
                    value={formData.shippingFee}
                    onChange={(e) =>
                      handleFieldChange('shippingFee', parseFloat(e.target.value) || 0)
                    }
                  />

                  <FormTextarea
                    label="Ghi chú đơn hàng"
                    placeholder="Giao giờ hành chính, gọi trước khi giao..."
                    rows={2}
                    value={formData.note}
                    onChange={(e: any) => handleFieldChange('note', e.target.value)}
                  />
                </div>

                <div className="pt-4 border-t border-slate-200 flex flex-col gap-2 mt-4">
                  <div className="flex justify-between text-sm text-slate-600">
                    <span>Tiền hàng:</span>
                    <span className="font-bold text-slate-800">
                      {subTotal.toLocaleString('vi-VN')} đ
                    </span>
                  </div>
                  <div className="flex justify-between text-sm text-slate-600">
                    <span>Phí vận chuyển:</span>
                    <span className="font-bold text-slate-800">
                      {Number(formData.shippingFee || 0).toLocaleString('vi-VN')} đ
                    </span>
                  </div>
                  <div className="flex justify-between text-base font-black text-slate-900 pt-2 border-t border-slate-200">
                    <span>TỔNG THANH TOÁN:</span>
                    <span className="text-xl text-emerald-600 font-black">
                      {totalAmount.toLocaleString('vi-VN')} đ
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </FormSection>

          {/* ================= FOOTER & ACTION BUTTONS ================= */}
          <div className="flex justify-end gap-3 pt-6 border-t border-slate-100 mt-2">
            <button
              type="button"
              onClick={() => navigate('/orders')}
              className="px-6 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 hover:text-slate-900 transition-colors shadow-2xs cursor-pointer"
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

export default OrderForm;
