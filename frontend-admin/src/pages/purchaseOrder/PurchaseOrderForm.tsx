import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ShoppingCart, Plus, Trash2, Save } from 'lucide-react';

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
import { supplierProductApi } from '../../api/supplierProductApi'; // Sẽ tạo ở Phase 3
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';
import { useAuthStore } from '../../stores/useAuthStore';
import { PurchaseOrderCreatePayload, PurchaseOrderStatus } from '../../types/purchaseOrder';

// Dùng kiểu number | '' để input số không bị lỗi số 0 ở đầu
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

  // --- DROPDOWN OPTIONS ---
  const [suppliers, setSuppliers] = useState<{ value: number; label: string }[]>([]);
  const [variants, setVariants] = useState<{ value: number; label: string }[]>([]);
  const [uoms, setUoms] = useState<{ value: number; label: string }[]>([]);

  // --- EFFECTS ---
  const loadDropdownData = useCallback(async () => {
    try {
      const [supplierRes, variantRes, uomRes] = await Promise.all([
        supplierApi.getAllList(), // Dùng hàm getAllList thay vì getAll phân trang cho nhanh
        productVariantApi.getAllList(),
        uomApi.getAllList(),
      ]);

      setSuppliers(supplierRes.map(s => ({ value: s.id, label: s.name })));
      setVariants(variantRes.map(v => ({ value: v.id, label: `${v.code} - ${v.name}` })));
      setUoms(uomRes.map(u => ({ value: u.id, label: u.name })));
    } catch (error) {
      showToast('error', 'Không thể tải dữ liệu danh mục bổ trợ');
    }
  }, []);

  const loadPurchaseOrder = useCallback(async () => {
    if (!id) return;
    setIsLoading(true);
    try {
      const data = await purchaseOrderApi.getById(Number(id));
      
      // 🔥 FIX BUGS: Ép đúng Enum status để kiểm tra
      if (data.status !== PurchaseOrderStatus.Draft) {
        showToast('warning', 'Chỉ được phép chỉnh sửa đơn hàng đang ở trạng thái Nháp!');
        setTimeout(() => navigate('/purchase-orders'), 1500);
        return;
      }

      setSupplierId(data.supplierId);
      setOrderDate(data.orderDate ? data.orderDate.split('T')[0] : '');
      setExpectedDeliveryDate(data.expectedDeliveryDate ? data.expectedDeliveryDate.split('T')[0] : '');
      setNotes(data.note || ''); // FIX: Backend trả về 'note', ko phải 'notes'
      
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
      // Mặc định ngày đặt là hôm nay nếu tạo mới
      setOrderDate(new Date().toISOString().split('T')[0]);
    }
  }, [isEditMode, loadDropdownData, loadPurchaseOrder]);

  // --- HANDLERS ---
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

  const handleDetailChange = async (index: number, field: keyof DetailRow, value: any) => {
    const newDetails = [...details];
    newDetails[index] = { ...newDetails[index], [field]: value };

    // 🔥 AUTO-FILL GIÁ: Khi chọn SP + Đã có Nhà cung cấp
    if (field === 'variantId' && value && supplierId) {
  try {
    const productRes = await supplierProductApi.getAll({
      supplierId: Number(supplierId),
      variantId: Number(value),
      pageSize: 1
    });
    
    if (productRes.items && productRes.items.length > 0) {
      // SỬA LẠI TÊN BIẾN THEO ĐÚNG MODEL MỚI CỦA SẾP
      newDetails[index].unitPrice = productRes.items[0].lastImportPrice ?? 0; 
      
      // Khuyến mãi thêm: Tự động điền luôn Đơn vị tính mua hàng (UoM)
      newDetails[index].uoMId = productRes.items[0].purchaseUoMId; 
    }
  } catch (error) {
    console.error('Không tìm thấy bảng giá của NCC cho sản phẩm này:', error);
  }
}

    setDetails(newDetails);
    
    // Clear error
    if (errors[`${field}_${index}`]) {
      setErrors(prev => { const e = { ...prev }; delete e[`${field}_${index}`]; return e; });
    }
  };

  const calculateTotal = () => {
    return details.reduce((sum, row) => sum + (Number(row.orderQuantity || 0) * Number(row.unitPrice || 0)), 0);
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
      
      // 🔥 FIX BUGS TIMEZONE: Đẩy an toàn ngày vào buổi trưa UTC để không bị lùi ngày
      const safeOrderDate = new Date(`${orderDate}T12:00:00Z`).toISOString();
      const safeDeliveryDate = expectedDeliveryDate ? new Date(`${expectedDeliveryDate}T12:00:00Z`).toISOString() : undefined;

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
        // Dùng Update Payload nếu API quy định Update riêng (Tùy cấu trúc API sếp viết)
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
                onSelect={(val) => { setSupplierId(val); setErrors(prev => ({...prev, supplierId: ''})) }}
                options={suppliers}
                error={errors.supplierId}
                required
                showSearch
                placeholder="-- Chọn Nhà cung cấp --"
                disabled={isEditMode} // Không cho đổi NCC khi Edit tránh sai giá
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
                    details.map((row, idx) => (
                      <tr key={idx} className="hover:bg-slate-50/50 transition-colors">
                        <td className="p-2">
                          <FormSelect 
                            label="" options={variants} value={row.variantId} showSearch placeholder="Chọn SP..."
                            onSelect={val => handleDetailChange(idx, 'variantId', val)}
                            error={errors[`variantId_${idx}`]}
                          />
                        </td>
                        <td className="p-2">
                          <FormSelect 
                            label="" options={uoms} value={row.uoMId} placeholder="Chọn UoM"
                            onSelect={val => handleDetailChange(idx, 'uoMId', val)}
                            error={errors[`uoMId_${idx}`]}
                          />
                        </td>
                        <td className="p-2">
                          <FormInput 
                            label="" type="number" value={row.orderQuantity} placeholder="0"
                            onChange={e => handleDetailChange(idx, 'orderQuantity', parseFloat(e.target.value) || 0)}
                            error={errors[`orderQuantity_${idx}`]}
                          />
                        </td>
                        <td className="p-2">
                          <FormInput 
                            label="" type="number" value={row.unitPrice} placeholder="0"
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
                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors mx-auto"
                            title="Xóa mặt hàng"
                          >
                            <Trash2 size={18} strokeWidth={2.5} />
                          </button>
                        </td>
                      </tr>
                    ))
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
                  className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 border border-blue-200/50 rounded-lg transition-colors"
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