import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ShoppingCart, Plus, Trash2, Save, Filter, AlertCircle, CheckCircle2 } from 'lucide-react';

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
import { Toast } from '../../components/commons/Toast';

// API & Types
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import { supplierApi } from '../../api/supplierApi';
import { supplierProductApi } from '../../api/supplierProductApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
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
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'warning' | 'error'; message: string }>({
    show: false, type: 'success', message: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  // --- FORM STATES ---
  const [supplierId, setSupplierId] = useState<number | ''>('');
  const [orderDate, setOrderDate] = useState('');
  const [expectedDeliveryDate, setExpectedDeliveryDate] = useState('');
  const [notes, setNotes] = useState('');
  const [details, setDetails] = useState<DetailRow[]>([]);

  // --- FILTER THEO NCC ---
  const [onlySupplierProducts, setOnlySupplierProducts] = useState(true);
  const [supplierProducts, setSupplierProducts] = useState<SupplierProduct[]>([]);
  const [loadingSupplierProducts, setLoadingSupplierProducts] = useState(false);

  // --- DROPDOWN OPTIONS ---
  const [suppliers, setSuppliers] = useState<{ value: number; label: string }[]>([]);
  const [allVariants, setAllVariants] = useState<{ value: number; label: string }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // --- EFFECTS ---
  const loadDropdownData = useCallback(async () => {
    try {
      const [supplierRes, variantRes, uomRes] = await Promise.all([
        supplierApi.getAllList(),
        productVariantApi.getAllList(),
        uomApi.getAllList(),
      ]);

      setSuppliers(supplierRes.map(s => ({ value: s.id, label: `${s.code} - ${s.name}` })));
      setAllVariants(variantRes.map(v => ({ value: v.id, label: `${v.code} - ${v.name}` })));
      setUoms(uomRes.map(u => ({ value: u.id, label: u.name })));
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
      setOrderDate(data.orderDate ? data.orderDate.split('T')[0] : '');
      setExpectedDeliveryDate(data.expectedDeliveryDate ? data.expectedDeliveryDate.split('T')[0] : '');
      setNotes(data.note || '');
      
      if (data.details && data.details.length > 0) {
        setDetails(
          data.details.map(d => ({
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
    } else {
      setOrderDate(new Date().toISOString().split('T')[0]);
    }
  }, [isEditMode, loadDropdownData, loadPurchaseOrder]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
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
      const sp = supplierProducts.find(p => p.variantId === Number(value));
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
      setErrors(prev => { const e = { ...prev }; delete e[`${field}_${index}`]; return e; });
    }
  };

  const calculateTotal = () => {
    return details.reduce((sum, row) => sum + (Number(row.orderQuantity || 0) * Number(row.unitPrice || 0)), 0);
  };

  // Tính toán danh sách Options Sản Phẩm hiển thị cho Dropdown
  const getVariantOptions = () => {
    if (onlySupplierProducts && supplierId && supplierProducts.length > 0) {
      return supplierProducts.map(sp => ({
        value: sp.variantId,
        label: `${sp.variantCode || sp.variantSKU || `#${sp.variantId}`} - ${sp.variantName} (${sp.lastImportPrice.toLocaleString('vi-VN')} ₫/${sp.purchaseUoMName || 'ĐVT'})`
      }));
    }
    return allVariants;
  };

  // Tìm thông tin MOQ của SP đang chọn
  const getSupplierProductInfo = (variantId: number | '') => {
    if (!variantId || !supplierProducts.length) return null;
    return supplierProducts.find(sp => sp.variantId === Number(variantId));
  };

  // --- VALIDATION & SUBMIT ---
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!supplierId) newErrors.supplierId = 'Vui lòng chọn nhà cung cấp';
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
      
      const safeOrderDate = `${orderDate}T00:00:00Z`;
      const safeDeliveryDate = expectedDeliveryDate ? `${expectedDeliveryDate}T00:00:00Z` : undefined;

      const payload: PurchaseOrderCreatePayload = {
        supplierId: Number(supplierId),
        orderDate: safeOrderDate,
        expectedDeliveryDate: safeDeliveryDate,
        note: notes.trim(),
        createdById: userInfo?.id || 1,
        details: details.map(d => ({
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

      setTimeout(() => navigate('/purchase-orders'), 1500);
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
        subtitle="Lập danh sách hàng hóa và đơn giá dự kiến trước khi gửi Nhà cung cấp"
        icon={ShoppingCart}
        onBack={() => navigate('/purchase-orders')}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          
          {/* --- SECTION 1: THÔNG TIN CHUNG --- */}
          <FormSection title="1. Thông Tin Chung">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormSelect
                label="Nhà Cung Cấp"
                value={supplierId}
                onSelect={(val) => { 
                  setSupplierId(val); 
                  setErrors(prev => ({...prev, supplierId: ''}));
                }}
                options={suppliers}
                error={errors.supplierId}
                required
                showSearch
                placeholder="-- Chọn Nhà cung cấp --"
                disabled={isEditMode}
              />
              <FormInput
                label="Ngày Lập Đơn"
                type="date"
                value={orderDate}
                onChange={(e) => { setOrderDate(e.target.value); setErrors(prev => ({...prev, orderDate: ''})) }}
                error={errors.orderDate}
                required
              />
              <FormInput
                label="Ngày Giao Dự Kiến"
                type="date"
                value={expectedDeliveryDate}
                onChange={(e) => setExpectedDeliveryDate(e.target.value)}
              />
              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú thêm"
                  placeholder="Yêu cầu xe cộ, bốc vác, lưu ý đóng gói..."
                  value={notes}
                  onChange={(e: any) => setNotes(e.target.value)}
                  rows={3}
                />
              </div>
            </div>
          </FormSection>

          {/* --- SECTION 2: CHI TIẾT HÀNG HÓA --- */}
          <FormSection title="2. Chi Tiết Đặt Hàng">
            
            {/* Thanh điều khiển bộ lọc sản phẩm NCC */}
            {supplierId ? (
              <div className="mb-4 p-3 bg-slate-50 border border-slate-200 rounded-xl flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <Filter size={16} className="text-yellow-600" />
                  <span className="text-xs font-bold text-slate-700">
                    Bảng giá NCC: {loadingSupplierProducts ? 'Đang tải...' : `${supplierProducts.length} mặt hàng đã liên kết`}
                  </span>
                </div>

                {supplierProducts.length > 0 ? (
                  <label className="inline-flex items-center gap-2 cursor-pointer text-xs font-semibold text-slate-700">
                    <input 
                      type="checkbox" 
                      checked={onlySupplierProducts}
                      onChange={(e) => setOnlySupplierProducts(e.target.checked)}
                      className="rounded text-yellow-500 focus:ring-yellow-400"
                    />
                    <span>Chỉ hiển thị sản phẩm trong bảng giá NCC</span>
                  </label>
                ) : (
                  <div className="flex items-center gap-1.5 text-xs text-amber-700 bg-amber-50 px-2.5 py-1 rounded-lg border border-amber-200">
                    <AlertCircle size={14} />
                    <span>NCC chưa có bảng giá riêng, đang hiển thị tất cả sản phẩm hệ thống</span>
                  </div>
                )}
              </div>
            ) : null}

            {errors.details && (
              <div className="mb-4 p-3 bg-rose-50 text-rose-600 text-sm font-bold rounded-lg border border-rose-200">
                {errors.details}
              </div>
            )}
            
            <div className="border border-slate-200 rounded-xl overflow-hidden bg-white">
              <table className="w-full text-left border-collapse">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="py-3 px-4 text-xs font-bold text-slate-500 uppercase w-[35%]">Sản Phẩm (Mã SKU) <span className="text-red-500">*</span></th>
                    <th className="py-3 px-2 text-xs font-bold text-slate-500 uppercase w-[15%]">Đơn Vị <span className="text-red-500">*</span></th>
                    <th className="py-3 px-2 text-xs font-bold text-slate-500 uppercase w-[15%]">Số Lượng <span className="text-red-500">*</span></th>
                    <th className="py-3 px-2 text-xs font-bold text-slate-500 uppercase w-[15%]">Đơn Giá Nhập <span className="text-red-500">*</span></th>
                    <th className="py-3 px-4 text-xs font-bold text-slate-500 uppercase w-[15%] text-right">Thành Tiền</th>
                    <th className="py-3 px-2 text-xs font-bold text-slate-500 uppercase w-[5%] text-center">Xóa</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {details.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-slate-400 text-sm italic">
                        Chưa có mặt hàng nào. Vui lòng bấm "Thêm Mặt Hàng".
                      </td>
                    </tr>
                  ) : (
                    details.map((row, idx) => {
                      const spInfo = getSupplierProductInfo(row.variantId);
                      const isBelowMoq = spInfo && row.orderQuantity > 0 && row.orderQuantity < spInfo.minimumOrderQuantity;

                      return (
                        <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                          <td className="p-2">
                            <FormSelect 
                              label="" 
                              options={getVariantOptions()} 
                              value={row.variantId} 
                              showSearch 
                              placeholder="Chọn SP..."
                              onSelect={val => handleDetailChange(idx, 'variantId', val)}
                              error={errors[`variantId_${idx}`]}
                            />
                            {spInfo ? (
                              <div className="flex items-center gap-2 mt-1 px-1">
                                <span className="text-[10px] text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-200">
                                  MOQ: {spInfo.minimumOrderQuantity} {spInfo.purchaseUoMName || ''}
                                </span>
                                {spInfo.supplierSKU && (
                                  <span className="text-[10px] text-slate-500">
                                    SKU NCC: {spInfo.supplierSKU}
                                  </span>
                                )}
                              </div>
                            ) : null}
                          </td>
                          <td className="p-2">
                            <FormSelect 
                              label="" 
                              options={uoms} 
                              value={row.uoMId} 
                              placeholder="Chọn UoM"
                              onSelect={val => handleDetailChange(idx, 'uoMId', val)}
                              error={errors[`uoMId_${idx}`]}
                            />
                          </td>
                          <td className="p-2">
                            <FormInput 
                              label="" 
                              type="number" 
                              value={row.orderQuantity} 
                              placeholder="0"
                              onChange={e => handleDetailChange(idx, 'orderQuantity', parseFloat(e.target.value) || 0)}
                              error={errors[`orderQuantity_${idx}`]}
                            />
                            {isBelowMoq ? (
                              <span className="text-[10px] text-rose-600 font-bold block mt-0.5 px-1">
                                ⚠️ Dưới MOQ ({spInfo.minimumOrderQuantity})
                              </span>
                            ) : null}
                          </td>
                          <td className="p-2">
                            <FormInput 
                              label="" 
                              type="number" 
                              value={row.unitPrice} 
                              placeholder="0"
                              onChange={e => handleDetailChange(idx, 'unitPrice', parseFloat(e.target.value) || 0)}
                              error={errors[`unitPrice_${idx}`]}
                            />
                          </td>
                          <td className="p-2 text-right">
                            <span className="font-bold text-slate-700 text-[14px]">
                              {((row.orderQuantity || 0) * (row.unitPrice || 0)).toLocaleString('vi-VN')} ₫
                            </span>
                          </td>
                          <td className="p-2 text-center">
                            <button
                              type="button"
                              onClick={() => handleRemoveRow(idx)}
                              className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors mx-auto cursor-pointer"
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
                {details.length > 0 && (
                  <tfoot className="bg-slate-50/80 font-medium border-t border-slate-200">
                    <tr>
                      <td colSpan={4} className="px-4 py-4 text-right text-slate-600 text-[13px] font-bold uppercase tracking-wider">
                        Tổng Tiền Đơn Hàng:
                      </td>
                      <td className="px-4 py-4 text-right text-blue-700 font-black text-[18px]">
                        {calculateTotal().toLocaleString('vi-VN')} ₫
                      </td>
                      <td></td>
                    </tr>
                  </tfoot>
                )}
              </table>
              
              <div className="p-3 bg-slate-50/50 border-t border-slate-200 flex justify-center">
                <button 
                  type="button" 
                  onClick={handleAddRow}
                  className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 border border-blue-200/50 rounded-lg transition-colors cursor-pointer"
                >
                  <Plus size={16} strokeWidth={3} /> THÊM MẶT HÀNG
                </button>
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-4 border-t border-slate-100">
            <SubmitButton loading={isLoading} isEditMode={isEditMode} icon={isEditMode ? Save : Plus} />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default PurchaseOrderForm;