import React, { useEffect, useState } from 'react';
import { X, Package, Save } from 'lucide-react';
import { FormInput, FormSelect } from '../commons/FormUI';
import { SupplierProductPayload, SupplierProduct } from '../../types/supplierProduct';
import { supplierApi } from '../../api/supplierApi';
import { productVariantApi } from '../../api/productVariantApi';
import { uomApi } from '../../api/uomApi';

interface ModalSupplierProductProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: SupplierProductPayload) => Promise<void>;
  initialData?: SupplierProduct | null;
  fixedSupplierId?: number; // Khi mở modal từ trang SupplierDetail, khóa cố định NCC
}

const INITIAL_STATE: SupplierProductPayload = {
  supplierId: 0,
  variantId: 0,
  purchaseUoMId: 0,
  lastImportPrice: 0,
  minimumOrderQuantity: 1,
  leadTimeDays: 0,
  supplierSKU: '',
  isActive: true,
};

export const ModalSupplierProduct: React.FC<ModalSupplierProductProps> = ({
  isOpen,
  onClose,
  onSave,
  initialData,
  fixedSupplierId,
}) => {
  const [formData, setFormData] = useState<SupplierProductPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<keyof SupplierProductPayload, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Options dropdowns
  const [supplierOptions, setSupplierOptions] = useState<{ value: number; label: string }[]>([]);
  const [variantOptions, setVariantOptions] = useState<{ value: number; label: string }[]>([]);
  const [uomOptions, setUomOptions] = useState<{ value: number; label: string }[]>([]);

  useEffect(() => {
    if (isOpen) {
      Promise.all([supplierApi.getAllList(), productVariantApi.getAllList(), uomApi.getAllList()])
        .then(([suppliers, variants, uoms]) => {
          setSupplierOptions(
            suppliers.map((s) => ({ value: s.id, label: `${s.code} - ${s.name}` }))
          );
          setVariantOptions(variants.map((v) => ({ value: v.id, label: `${v.code} - ${v.name}` })));
          setUomOptions(uoms.map((u) => ({ value: u.id, label: u.name })));
        })
        .catch((err) => console.error('Lỗi tải danh mục bổ trợ:', err));

      if (initialData) {
        setFormData({
          supplierId: initialData.supplierId,
          variantId: initialData.variantId,
          purchaseUoMId: initialData.purchaseUoMId,
          lastImportPrice: initialData.lastImportPrice,
          minimumOrderQuantity: initialData.minimumOrderQuantity,
          leadTimeDays: initialData.leadTimeDays,
          supplierSKU: initialData.supplierSKU || '',
          isActive: initialData.isActive,
        });
      } else {
        setFormData({
          ...INITIAL_STATE,
          supplierId: fixedSupplierId || 0,
        });
      }
      setErrors({});
    }
  }, [isOpen, initialData, fixedSupplierId]);

  if (!isOpen) return null;

  const handleFieldChange = (field: keyof SupplierProductPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    const newErrors: Partial<Record<keyof SupplierProductPayload, string>> = {};

    if (!formData.supplierId || formData.supplierId <= 0) {
      newErrors.supplierId = 'Vui lòng chọn nhà cung cấp.';
    }
    if (!formData.variantId || formData.variantId <= 0) {
      newErrors.variantId = 'Vui lòng chọn sản phẩm biến thể.';
    }
    if (!formData.purchaseUoMId || formData.purchaseUoMId <= 0) {
      newErrors.purchaseUoMId = 'Vui lòng chọn đơn vị tính mua hàng.';
    }
    if (formData.lastImportPrice === undefined || formData.lastImportPrice < 0) {
      newErrors.lastImportPrice = 'Đơn giá nhập phải >= 0.';
    }
    if (!formData.minimumOrderQuantity || formData.minimumOrderQuantity <= 0) {
      newErrors.minimumOrderQuantity = 'MOQ phải lớn hơn 0.';
    }
    if (formData.leadTimeDays === undefined || formData.leadTimeDays < 0) {
      newErrors.leadTimeDays = 'Thời gian giao hàng phải >= 0 ngày.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      const cleanData: SupplierProductPayload = {
        supplierId: Number(formData.supplierId),
        variantId: Number(formData.variantId),
        purchaseUoMId: Number(formData.purchaseUoMId),
        lastImportPrice: Number(formData.lastImportPrice),
        minimumOrderQuantity: Number(formData.minimumOrderQuantity),
        leadTimeDays: Number(formData.leadTimeDays),
        supplierSKU: formData.supplierSKU?.trim() || undefined,
        isActive: formData.isActive,
      };

      await onSave(cleanData);
      onClose();
    } catch (error) {
      console.error('Lỗi khi lưu bảng giá NCC:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-100 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity"
        onClick={!isSubmitting ? onClose : undefined}
      ></div>

      {/* Modal Box */}
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center gap-2.5 text-slate-800">
            <Package size={20} className="text-yellow-500" />
            <h3 className="font-bold text-[16px] uppercase tracking-wide">
              {initialData ? 'Cập Nhật Sản Phẩm & Bảng Giá NCC' : 'Thêm Sản Phẩm Vào Danh Mục NCC'}
            </h3>
          </div>
          <button
            onClick={onClose}
            disabled={isSubmitting}
            className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200 rounded-lg transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        {/* Body */}
        <div className="p-6">
          <form id="supplierProductForm" onSubmit={handleSubmit} className="flex flex-col gap-6">
            {/* Row 1: Nhà cung cấp & Biến thể */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {!fixedSupplierId ? (
                <FormSelect
                  label="Nhà Cung Cấp"
                  required
                  showSearch
                  placeholder="Chọn nhà cung cấp..."
                  value={formData.supplierId || ''}
                  options={supplierOptions}
                  error={errors.supplierId}
                  disabled={Boolean(initialData) || isSubmitting}
                  onSelect={(val) => handleFieldChange('supplierId', val)}
                />
              ) : null}

              <div className={fixedSupplierId ? 'md:col-span-2' : ''}>
                <FormSelect
                  label="Sản Phẩm (Biến Thể SKU)"
                  required
                  showSearch
                  placeholder="Chọn sản phẩm biến thể..."
                  value={formData.variantId || ''}
                  options={variantOptions}
                  error={errors.variantId}
                  disabled={Boolean(initialData) || isSubmitting}
                  onSelect={(val) => handleFieldChange('variantId', val)}
                />
              </div>
            </div>

            {/* Row 2: Đơn giá nhập & ĐVT Mua hàng */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormInput
                label="Đơn Giá Nhập (VNĐ)"
                required
                type="number"
                placeholder="VD: 50000"
                value={formData.lastImportPrice}
                error={errors.lastImportPrice}
                disabled={isSubmitting}
                onChange={(e) =>
                  handleFieldChange('lastImportPrice', parseFloat(e.target.value) || 0)
                }
              />

              <FormSelect
                label="Đơn Vị Tính Mua Hàng"
                required
                placeholder="Chọn ĐVT..."
                value={formData.purchaseUoMId || ''}
                options={uomOptions}
                error={errors.purchaseUoMId}
                disabled={isSubmitting}
                onSelect={(val) => handleFieldChange('purchaseUoMId', val)}
              />
            </div>

            {/* Row 3: MOQ, Lead time & Mã SKU NCC */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <FormInput
                label="Số Lượng Tối Thiểu (MOQ)"
                required
                type="number"
                placeholder="VD: 10"
                value={formData.minimumOrderQuantity}
                error={errors.minimumOrderQuantity}
                disabled={isSubmitting}
                onChange={(e) =>
                  handleFieldChange('minimumOrderQuantity', parseFloat(e.target.value) || 1)
                }
              />

              <FormInput
                label="Thời Gian Giao (Ngày)"
                type="number"
                placeholder="VD: 3"
                value={formData.leadTimeDays}
                error={errors.leadTimeDays}
                disabled={isSubmitting}
                onChange={(e) => handleFieldChange('leadTimeDays', parseInt(e.target.value) || 0)}
              />

              <FormInput
                label="Mã SKU Của NCC (Tùy chọn)"
                placeholder="VD: NCC-SP-01"
                value={formData.supplierSKU || ''}
                disabled={isSubmitting}
                onChange={(e) => handleFieldChange('supplierSKU', e.target.value)}
              />
            </div>

            {/* Switch Hoạt động */}
            <div className="flex items-center gap-3 mt-1 bg-slate-50 p-4 rounded-xl border border-slate-100">
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  className="sr-only peer"
                  checked={formData.isActive}
                  disabled={isSubmitting}
                  onChange={(e) => handleFieldChange('isActive', e.target.checked)}
                />
                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-yellow-400"></div>
              </label>
              <div className="flex flex-col">
                <span className="text-[13px] font-bold text-slate-700">
                  Đang Cung Ứng (Kích hoạt)
                </span>
                <span className="text-[11px] text-slate-500">
                  Cho phép chọn sản phẩm này khi tạo đơn mua hàng từ NCC.
                </span>
              </div>
            </div>
          </form>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 bg-slate-50 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors cursor-pointer"
          >
            Hủy Bỏ
          </button>
          <button
            type="submit"
            form="supplierProductForm"
            disabled={isSubmitting}
            className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-slate-900 bg-yellow-400 rounded-xl hover:bg-yellow-500 shadow-sm shadow-yellow-200 transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
          >
            {isSubmitting ? (
              <div className="w-5 h-5 border-2 border-slate-900/30 border-t-slate-900 rounded-full animate-spin"></div>
            ) : (
              <Save size={18} strokeWidth={2.5} />
            )}
            <span>{initialData ? 'Lưu Cập Nhật' : 'Thêm Vào Bảng Giá'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
