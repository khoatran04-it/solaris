import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ShoppingCart, Plus, Trash2, Save, Filter, AlertCircle } from 'lucide-react';

// Common UI
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
import DatePicker from '../../components/commons/CustomDatePicker';
import { Toast } from '../../components/commons/Toast';

// API & Types
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import { supplierApi } from '../../api/supplierApi';
import { supplierProductApi } from '../../api/supplierProductApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
import { warehouseApi } from '../../api/warehouseApi';
import { useAuthStore } from '../../stores/useAuthStore';
import { PurchaseOrderCreatePayload, PurchaseOrderStatus } from '../../types/purchaseOrder';
import { SupplierProduct } from '../../types/supplierProduct';

interface DetailRow {
  variantId: number | '';
  uoMId: number | '';
  orderQuantity: number;
  unitPrice: number;
}

const PurchaseOrderForm: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEditMode = Boolean(id);
  const { userInfo } = useAuthStore();

  const [isLoading, setIsLoading] = useState(false);
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'warning' | 'error';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- FORM STATES ---
  const [supplierId, setSupplierId] = useState<number | ''>('');
  const [warehouseId, setWarehouseId] = useState<number | ''>('');
  const [orderDate, setOrderDate] = useState<Date | null>(new Date());
  const [expectedDeliveryDate, setExpectedDeliveryDate] = useState<Date | null>(null);
  const [notes, setNotes] = useState('');
  const [details, setDetails] = useState<DetailRow[]>([]);

  // --- FILTER THEO NCC ---
  const [onlySupplierProducts, setOnlySupplierProducts] = useState(true);
  const [supplierProducts, setSupplierProducts] = useState<SupplierProduct[]>([]);
  const [loadingSupplierProducts, setLoadingSupplierProducts] = useState(false);

  // --- DROPDOWN OPTIONS ---
  const [suppliers, setSuppliers] = useState<{ value: number; label: string }[]>([]);
  const [warehouses, setWarehouses] = useState<{ value: number; label: string }[]>([]);
  const [allVariants, setAllVariants] = useState<{ value: number; label: string }[]>([]);
  const [rawVariants, setRawVariants] = useState<any[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // --- EFFECTS: LOAD TẤT CẢ OPTIONS ---
  const loadDropdownData = useCallback(async () => {
    try {
      const [supplierRes, variantRes, uomRes, whRes] = await Promise.all([
        supplierApi.getAllList().catch(() => []),
        productVariantApi.getAllList().catch(() => []),
        uomApi.getAllList().catch(() => []),
        warehouseApi.getAllList().catch(() => []),
      ]);

      setSuppliers(supplierRes.map((s: any) => ({ value: s.id, label: `${s.code} - ${s.name}` })));
      setRawVariants(variantRes);
      setAllVariants(variantRes.map((v: any) => ({ value: v.id, label: v.name })));
      setUoms(uomRes.map((u: any) => ({ value: u.id, label: u.name })));
      setWarehouses(whRes.map((w: any) => ({ value: w.id, label: `${w.code || 'KHO'} - ${w.name}` })));
    } catch (error) {
      showToast('error', 'Không thể tải dữ liệu danh mục bổ trợ');
    }
  }, []);

  // Khi chọn NCC: Tải danh mục sản phẩm & bảng giá của NCC đó
  const loadSupplierProducts = useCallback(async (supId: number) => {
    setLoadingSupplierProducts(true);
    try {
      const products = await supplierProductApi.getBySupplierId(supId, true);
      setSupplierProducts(products);
    } catch (error) {
      console.error('Lỗi khi tải bảng giá NCC:', error);
      setSupplierProducts([]);
    } finally {
      setLoadingSupplierProducts(false);
    }
  }, []);

  useEffect(() => {
    if (supplierId) {
      loadSupplierProducts(Number(supplierId));
    } else {
      setSupplierProducts([]);
    }
  }, [supplierId, loadSupplierProducts]);

  const loadPurchaseOrder = useCallback(async () => {
    if (!id) return;
    setIsLoading(true);
    try {
      const data = await purchaseOrderApi.getById(Number(id));

      if (data.status !== PurchaseOrderStatus.Draft) {
        showToast('warning', 'Chỉ được phép chỉnh sửa đơn hàng đang ở trạng thái Nháp!');
        setTimeout(() => navigate('/purchase-orders'), 1500);
        return;
      }

      setSupplierId(data.supplierId);
      setOrderDate(data.orderDate ? new Date(data.orderDate) : null);
      setExpectedDeliveryDate(
        data.expectedDeliveryDate ? new Date(data.expectedDeliveryDate) : null
      );
      setNotes(data.note || '');

      if (data.details && data.details.length > 0) {
        setDetails(
          data.details.map((d) => ({
            variantId: d.variantId,
            uoMId: d.uoMId,
            orderQuantity: d.orderQuantity,
            unitPrice: d.unitPrice,
          }))
        );
      }
    } catch (error) {
      showToast('error', 'Không thể tải dữ liệu đơn mua hàng');
    } finally {
      setIsLoading(false);
    }
  }, [id, navigate]);

  useEffect(() => {
    loadDropdownData();
    if (isEditMode) {
      loadPurchaseOrder();
    }
  }, [isEditMode, loadDropdownData, loadPurchaseOrder]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleAddRow = () => {
    setDetails([...details, { variantId: '', uoMId: '', orderQuantity: 1, unitPrice: 0 }]);
  };

  const handleRemoveRow = (index: number) => {
    const newDetails = [...details];
    newDetails.splice(index, 1);
    setDetails(newDetails);
  };

  const handleDetailChange = (index: number, field: keyof DetailRow, value: any) => {
    const newDetails = [...details];
    newDetails[index] = { ...newDetails[index], [field]: value };

    // 🔥 SMART AUTO-FILL: Khi chọn Sản phẩm -> Tự động điền Giá nhập và ĐVT mua từ bảng giá của NCC
    if (field === 'variantId' && value && supplierProducts.length > 0) {
      const sp = supplierProducts.find((p) => p.variantId === Number(value));
      if (sp) {
        newDetails[index].unitPrice = sp.lastImportPrice || 0;
        newDetails[index].uoMId = sp.purchaseUoMId || newDetails[index].uoMId;
        if (sp.minimumOrderQuantity && newDetails[index].orderQuantity < sp.minimumOrderQuantity) {
          newDetails[index].orderQuantity = sp.minimumOrderQuantity;
        }
      }
    }

    setDetails(newDetails);

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
      (sum, row) => sum + Number(row.orderQuantity || 0) * Number(row.unitPrice || 0),
      0
    );
  };

  // Tính toán danh sách Options Sản Phẩm hiển thị cho Dropdown (Chỉ hiển thị Tên sản phẩm)
  const getVariantOptions = () => {
    if (onlySupplierProducts && supplierId && supplierProducts.length > 0) {
      return supplierProducts.map((sp) => ({
        value: sp.variantId,
        label: sp.variantName || `Sản phẩm #${sp.variantId}`,
      }));
    }
    return allVariants;
  };

  // Tìm thông tin MOQ của SP đang chọn
  const getSupplierProductInfo = (variantId: number | '') => {
    if (!variantId || !supplierProducts.length) return null;
    return supplierProducts.find((sp) => sp.variantId === Number(variantId));
  };

  const formatDateToIso = (d: Date | null) => {
    if (!d) return undefined;
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}T00:00:00Z`;
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!supplierId) newErrors.supplierId = 'Vui lòng chọn nhà cung cấp';
    if (!warehouseId) newErrors.warehouseId = 'Vui lòng chọn kho nhận hàng';
    if (!orderDate) newErrors.orderDate = 'Vui lòng chọn ngày đặt hàng';

    if (details.length === 0) {
      newErrors.details = 'Cần ít nhất 1 mặt hàng trong đơn!';
    } else {
      details.forEach((row, idx) => {
        if (!row.variantId) newErrors[`variantId_${idx}`] = 'Bắt buộc chọn';
        if (!row.uoMId) newErrors[`uoMId_${idx}`] = 'Bắt buộc chọn';
        if (row.orderQuantity <= 0) newErrors[`orderQuantity_${idx}`] = 'Số lượng > 0';
        if (row.unitPrice < 0) newErrors[`unitPrice_${idx}`] = 'Giá trị không hợp lệ';
      });
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại các trường báo đỏ!');

    try {
      setIsLoading(true);

      const safeOrderDate = formatDateToIso(orderDate) || new Date().toISOString();
      const safeDeliveryDate = formatDateToIso(expectedDeliveryDate);

      // Lưu kèm thông tin Kho nhận hàng vào Note nếu có
      let finalNote = notes.trim();
      if (warehouseId) {
        const whObj = warehouses.find((w) => w.value === Number(warehouseId));
        if (whObj && !finalNote.includes(whObj.label)) {
          finalNote = finalNote
            ? `[Kho nhận: ${whObj.label}] ${finalNote}`
            : `[Kho nhận: ${whObj.label}]`;
        }
      }

      const payload: PurchaseOrderCreatePayload = {
        supplierId: Number(supplierId),
        orderDate: safeOrderDate,
        expectedDeliveryDate: safeDeliveryDate,
        note: finalNote,
        createdById: userInfo?.id || 1,
        details: details.map((d) => ({
          variantId: Number(d.variantId),
          uoMId: Number(d.uoMId),
          orderQuantity: Number(d.orderQuantity),
          unitPrice: Number(d.unitPrice),
        })),
      };

      if (isEditMode && id) {
        await purchaseOrderApi.update(Number(id), payload as any);
        showToast('success', 'CẬP NHẬT ĐƠN MUA HÀNG THÀNH CÔNG');
      } else {
        await purchaseOrderApi.create(payload);
        showToast('success', 'TẠO MỚI ĐƠN MUA HÀNG THÀNH CÔNG');
      }

      setTimeout(() => navigate('/purchase-orders'), 1200);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU DỮ LIỆU');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Đơn Mua Hàng (Nháp)' : 'Tạo Đơn Mua Hàng Mới'}
        subtitle="Lập danh sách đặt mua nông sản từ Nhà vườn / Nhà cung cấp trước khi nhập kho"
        icon={ShoppingCart}
        onBack={() => navigate('/purchase-orders')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          {/* --- SECTION 1: THÔNG TIN CHUNG --- */}
          <FormSection title="1. Thông Tin Chung">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormSelect
                label="Nhà Cung Cấp"
                value={supplierId}
                onSelect={(val) => {
                  setSupplierId(val);
                  setErrors((prev) => ({ ...prev, supplierId: '' }));
                }}
                options={suppliers}
                error={errors.supplierId}
                required
                showSearch
                searchPlaceholder="Tìm Nhà cung cấp..."
                placeholder="-- Chọn Nhà cung cấp --"
                disabled={isEditMode}
              />

              <FormSelect
                label="Kho Nhận Hàng Dự Kiến"
                value={warehouseId}
                onSelect={(val) => {
                  setWarehouseId(val);
                  setErrors((prev) => ({ ...prev, warehouseId: '' }));
                }}
                options={warehouses}
                error={errors.warehouseId}
                required
                showSearch
                searchPlaceholder="Tìm Kho nhận hàng..."
                placeholder="-- Chọn Kho nhận hàng lưu trữ --"
              />

              <DatePicker
                label="Ngày Lập Đơn"
                required
                value={orderDate}
                onChange={(date) => {
                  setOrderDate(date);
                  setErrors((prev) => ({ ...prev, orderDate: '' }));
                }}
                error={errors.orderDate}
                placeholder="Chọn ngày lập đơn..."
              />

              <DatePicker
                label="Ngày Hẹn Giao Hàng Dự Kiến"
                value={expectedDeliveryDate}
                onChange={(date) => setExpectedDeliveryDate(date)}
                placeholder="Chọn ngày hẹn giao..."
              />

              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú thêm & Yêu cầu vận chuyển"
                  placeholder="Ví dụ: Yêu cầu xe tải bảo ôn lạnh, đóng thùng xốp lót rơm, giao trước 08h00 sáng..."
                  value={notes}
                  onChange={(e: any) => setNotes(e.target.value)}
                  rows={2}
                />
              </div>
            </div>
          </FormSection>

          {/* --- SECTION 2: CHI TIẾT HÀNG HÓA --- */}
          <FormSection title="2. Chi Tiết Đặt Hàng">
            {/* Thanh điều khiển bộ lọc sản phẩm NCC */}
            {supplierId ? (
              <div className="mb-4 p-3.5 bg-yellow-50/50 border border-yellow-200/70 rounded-2xl flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2.5">
                  <Filter size={16} className="text-yellow-600" />
                  <span className="text-xs font-bold text-slate-800">
                    Bảng giá NCC:{' '}
                    {loadingSupplierProducts
                      ? 'Đang tải...'
                      : `${supplierProducts.length} mặt hàng đã liên kết`}
                  </span>
                </div>

                {supplierProducts.length > 0 ? (
                  <label className="inline-flex items-center gap-2 cursor-pointer text-xs font-bold text-slate-700 select-none">
                    <input
                      type="checkbox"
                      checked={onlySupplierProducts}
                      onChange={(e) => setOnlySupplierProducts(e.target.checked)}
                      className="rounded w-4 h-4 text-yellow-500 focus:ring-yellow-400 cursor-pointer"
                    />
                    <span>Chỉ hiển thị các mặt hàng trong Bảng giá của NCC này</span>
                  </label>
                ) : (
                  <div className="flex items-center gap-1.5 text-xs text-amber-800 bg-amber-100/70 px-3 py-1 rounded-xl border border-amber-300/60 font-semibold">
                    <AlertCircle size={14} className="text-amber-700 shrink-0" />
                    <span>NCC này chưa có bảng giá riêng, hệ thống đang mở toàn bộ danh mục sản phẩm.</span>
                  </div>
                )}
              </div>
            ) : null}

            {errors.details && (
              <div className="mb-4 p-3.5 bg-rose-50 text-rose-600 text-xs font-bold rounded-xl border border-rose-200">
                {errors.details}
              </div>
            )}

            {/* BẢNG MẶT HÀNG ĐẶT MUA */}
            <div className="border border-slate-200 rounded-2xl bg-white shadow-2xs overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[760px]">
                <thead className="bg-slate-50/80 border-b border-slate-200">
                  <tr>
                    <th className="py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider w-[36%] min-w-[200px]">
                      Sản Phẩm <span className="text-red-500">*</span>
                    </th>
                    <th className="py-3.5 px-3 text-xs font-bold text-slate-500 uppercase tracking-wider w-[15%] min-w-[110px]">
                      Đơn Vị Tính <span className="text-red-500">*</span>
                    </th>
                    <th className="py-3.5 px-3 text-xs font-bold text-slate-500 uppercase tracking-wider w-[13%] min-w-[90px]">
                      Số Lượng <span className="text-red-500">*</span>
                    </th>
                    <th className="py-3.5 px-3 text-xs font-bold text-slate-500 uppercase tracking-wider w-[17%] min-w-[120px]">
                      Đơn Giá Nhập (VNĐ) <span className="text-red-500">*</span>
                    </th>
                    <th className="py-3.5 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider w-[15%] min-w-[110px] text-right">
                      Thành Tiền
                    </th>
                    <th className="py-3.5 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider w-[4%] text-center">
                      Xóa
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-10 text-center text-slate-400 text-sm italic">
                        Chưa có mặt hàng nào. Vui lòng bấm "Thêm Mặt Hàng".
                      </td>
                    </tr>
                  ) : (
                    details.map((row, idx) => {
                      const spInfo = getSupplierProductInfo(row.variantId);
                      const isBelowMoq =
                        spInfo &&
                        row.orderQuantity > 0 &&
                        row.orderQuantity < spInfo.minimumOrderQuantity;

                      return (
                        <tr
                          key={idx}
                          className="hover:bg-slate-50/60 transition-colors"
                          style={{ zIndex: 50 - idx }}
                        >
                          {/* CỘT SẢN PHẨM */}
                          <td className="p-3 align-top">
                            <FormSelect
                              label=""
                              options={getVariantOptions()}
                              value={row.variantId}
                              showSearch
                              searchPlaceholder="Tìm tên sản phẩm..."
                              placeholder="Chọn sản phẩm..."
                              onSelect={(val) => handleDetailChange(idx, 'variantId', val)}
                              error={errors[`variantId_${idx}`]}
                            />
                            {row.variantId ? (
                              <div className="flex flex-wrap items-center gap-1.5 mt-2 px-0.5">
                                {spInfo?.minimumOrderQuantity ? (
                                  <span className="text-[10px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200/80">
                                    MOQ: {spInfo.minimumOrderQuantity} {spInfo.purchaseUoMName || ''}
                                  </span>
                                ) : null}
                                <span className="text-[10px] font-bold text-blue-700 bg-blue-50 px-2 py-0.5 rounded-md border border-blue-200/80">
                                  SKU: {spInfo?.variantCode || spInfo?.variantSKU || rawVariants.find((v) => v.id === Number(row.variantId))?.code || `SKU-${row.variantId}`}
                                </span>
                                {spInfo?.supplierSKU ? (
                                  <span className="text-[10px] font-medium text-slate-600 bg-slate-100 px-2 py-0.5 rounded-md border border-slate-200">
                                    SKU NCC: {spInfo.supplierSKU}
                                  </span>
                                ) : null}
                              </div>
                            ) : null}
                          </td>

                          {/* CỘT ĐƠN VỊ TÍNH */}
                          <td className="p-3 align-top">
                            <FormSelect
                              label=""
                              options={uoms}
                              value={row.uoMId}
                              showSearch
                              searchPlaceholder="Tìm ĐVT..."
                              placeholder="Chọn ĐVT..."
                              onSelect={(val) => handleDetailChange(idx, 'uoMId', val)}
                              error={errors[`uoMId_${idx}`]}
                            />
                          </td>

                          {/* CỘT SỐ LƯỢNG */}
                          <td className="p-3 align-top">
                            <FormInput
                              label=""
                              type="number"
                              value={row.orderQuantity}
                              placeholder="0"
                              onChange={(e) =>
                                handleDetailChange(
                                  idx,
                                  'orderQuantity',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              error={errors[`orderQuantity_${idx}`]}
                            />
                            {isBelowMoq ? (
                              <span className="text-[10px] text-rose-600 font-bold block mt-1 px-1 leading-tight">
                                ⚠️ Dưới MOQ ({spInfo.minimumOrderQuantity})
                              </span>
                            ) : null}
                          </td>

                          {/* CỘT ĐƠN GIÁ NHẬP */}
                          <td className="p-3 align-top">
                            <FormInput
                              label=""
                              type="number"
                              value={row.unitPrice}
                              placeholder="0"
                              onChange={(e) =>
                                handleDetailChange(
                                  idx,
                                  'unitPrice',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              error={errors[`unitPrice_${idx}`]}
                            />
                          </td>

                          {/* CỘT THÀNH TIỀN */}
                          <td className="p-3 text-right align-middle">
                            <span className="font-extrabold text-slate-800 text-sm">
                              {((row.orderQuantity || 0) * (row.unitPrice || 0)).toLocaleString(
                                'vi-VN'
                              )}{' '}
                              ₫
                            </span>
                          </td>

                          {/* CỘT XÓA */}
                          <td className="p-3 text-center align-middle">
                            <button
                              type="button"
                              onClick={() => handleRemoveRow(idx)}
                              className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors mx-auto cursor-pointer"
                              title="Xóa mặt hàng"
                            >
                              <Trash2 size={18} strokeWidth={2.5} />
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>

                {/* FOOTER TỔNG TIỀN */}
                {details.length > 0 && (
                  <tfoot className="bg-slate-50/80 font-medium border-t border-slate-200">
                    <tr>
                      <td
                        colSpan={4}
                        className="px-6 py-4 text-right text-slate-600 text-xs font-extrabold uppercase tracking-wider"
                      >
                        Tổng Tiền Đơn Hàng:
                      </td>
                      <td className="px-4 py-4 text-right text-amber-700 font-black text-lg">
                        {calculateTotal().toLocaleString('vi-VN')} ₫
                      </td>
                      <td></td>
                    </tr>
                  </tfoot>
                )}
              </table>

              {/* NÚT THÊM DÒNG DƯỚI ĐÁY */}
              <div className="p-3.5 bg-slate-50/50 border-t border-slate-200 flex justify-center">
                <button
                  type="button"
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-amber-700 bg-amber-50 hover:bg-amber-100 border border-amber-200 rounded-xl transition-all cursor-pointer shadow-2xs hover:shadow-xs active:scale-98"
                >
                  <Plus size={16} strokeWidth={3} /> THÊM MẶT HÀNG
                </button>
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-4 border-t border-slate-100">
            <SubmitButton
              loading={isLoading}
              isEditMode={isEditMode}
              icon={isEditMode ? Save : Plus}
            />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default PurchaseOrderForm;
