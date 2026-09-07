import React, { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { X, Package, Save } from 'lucide-react';
import { FormInput, FormSelect } from '../commons/FormUI';
import { SupplierProductPayload, SupplierProduct } from '../../types/supplierProduct';
import { ProductVariant } from '../../types/productVariant';
import { Product } from '../../types/product';
import { supplierApi } from '../../api/supplierApi';
import { productVariantApi } from '../../api/productVariantApi';
import { productApi } from '../../api/productApi';
import { uomApi } from '../../api/uomApi';
import { uomConversionApi } from '../../api/uomConversionApi';
import { ValidUoMOption } from '../../types/uomConversion';

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

  // Options dropdowns & data caches
  const [supplierOptions, setSupplierOptions] = useState<{ value: number; label: string }[]>([]);
  const [variantOptions, setVariantOptions] = useState<{ value: number; label: string }[]>([]);
  const [uomOptions, setUomOptions] = useState<{ value: number; label: string }[]>([]);
  const [variantsList, setVariantsList] = useState<ProductVariant[]>([]);
  const [productsList, setProductsList] = useState<Product[]>([]);
  const [validUoMOptions, setValidUoMOptions] = useState<ValidUoMOption[]>([]);

  // UX: Đóng modal khi bấm phím Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen && !isSubmitting) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, isSubmitting]);

  // UX: Khóa cuộn trang (scroll) khi mở modal
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  // Tải danh mục bổ trợ: Nhà cung cấp, Biến thể SKU, Đơn vị tính, Sản phẩm gốc
  useEffect(() => {
    if (isOpen) {
      Promise.all([
        supplierApi.getAllList(),
        productVariantApi.getAllList(),
        uomApi.getAllList(),
        productApi.getAllList().catch(() => []),
      ])
        .then(([suppliers, variants, uoms, prods]) => {
          setVariantsList(variants || []);
          setProductsList(prods || []);
          setSupplierOptions(
            (suppliers || []).map((s) => ({ value: s.id, label: `${s.code} - ${s.name}` }))
          );
          setVariantOptions(
            (variants || []).map((v) => ({ value: v.id, label: `${v.code} - ${v.name}` }))
          );
          setUomOptions((uoms || []).map((u) => ({ value: u.id, label: u.name })));
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

  // Tải danh sách ĐVT hợp lệ theo biến thể đã chọn
  useEffect(() => {
    if (formData.variantId) {
      uomConversionApi
        .getValidUoMs(Number(formData.variantId))
        .then((opts) => {
          setValidUoMOptions(opts || []);
          if (opts && opts.length > 0) {
            setFormData((prev) => {
              const hasSelected = opts.some((u) => u.uoMId === prev.purchaseUoMId);
              if (!hasSelected) {
                const base = opts.find((u) => u.isBaseUoM) || opts[0];
                return { ...prev, purchaseUoMId: base.uoMId };
              }
              return prev;
            });
          }
        })
        .catch(() => {
          setValidUoMOptions([]);
        });
    } else {
      setValidUoMOptions([]);
    }
  }, [formData.variantId]);

  if (!isOpen) return null;

  const selectedVariant = variantsList.find((v) => v.id === Number(formData.variantId));
  const parentProduct = productsList.find((p) => p.id === selectedVariant?.productId);
  const defaultPrice = selectedVariant?.prices?.find((p) => p.isDefault) || selectedVariant?.prices?.[0];

  // Nhận diện ĐVT của biến thể:
  // 1. Từ BaseUoMId của biến thể (BE map sẵn)
  // 2. Từ BaseUoMId của Product cha
  // 3. Từ uoMId của dòng giá mặc định
  const detectedUoMId =
    selectedVariant?.baseUoMId ||
    parentProduct?.baseUoMId ||
    defaultPrice?.uoMId ||
    0;

  const detectedUoMName =
    selectedVariant?.baseUoMName ||
    parentProduct?.baseUoMName ||
    defaultPrice?.uoMName ||
    uomOptions.find((u) => u.value === (formData.purchaseUoMId || detectedUoMId))?.label ||
    initialData?.purchaseUoMName ||
    '';

  const variantImage =
    selectedVariant?.imagePath || initialData?.variantImagePath || initialData?.variantImage;

  // Xử lý khi người dùng chọn Biến thể SKU -> Tự động điền và khóa cứng ĐVT
  const handleVariantSelect = (variantIdVal: any) => {
    const vId = Number(variantIdVal);
    const variant = variantsList.find((v) => v.id === vId);
    const prod = productsList.find((p) => p.id === variant?.productId);
    const defPrice = variant?.prices?.find((p) => p.isDefault) || variant?.prices?.[0];
    const autoUoMId = variant?.baseUoMId || prod?.baseUoMId || defPrice?.uoMId || 0;

    setFormData((prev) => ({
      ...prev,
      variantId: vId,
      purchaseUoMId: autoUoMId,
    }));

    if (errors.variantId || errors.purchaseUoMId) {
      setErrors((prev) => {
        const next = { ...prev };
        delete next.variantId;
        delete next.purchaseUoMId;
        return next;
      });
    }
  };

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
      newErrors.purchaseUoMId = 'Vui lòng chọn sản phẩm biến thể để xác định đơn vị tính.';
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

  const modalContent = (
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-in fade-in"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isSubmitting) {
          onClose();
        }
      }}
    >
      <div className="relative bg-white rounded-3xl shadow-2xl w-full max-w-2xl overflow-visible animate-in zoom-in-95 duration-200 border border-slate-100">
        {/* MODAL HEADER THEME SOLARIS */}
        <div className="flex items-center justify-between px-6 py-4.5 bg-amber-50/80 border-b border-amber-200/60 rounded-t-3xl">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-amber-400 text-slate-900 rounded-xl shadow-xs">
              <Package size={20} strokeWidth={2.5} />
            </div>
            <div>
              <h3 className="text-base font-extrabold text-slate-900 leading-tight">
                {initialData ? 'Cập Nhật Sản Phẩm & Bảng Giá NCC' : 'Thêm Sản Phẩm Vào Danh Mục NCC'}
              </h3>
              <p className="text-xs text-slate-500 font-medium">
                Thiết lập đơn giá nhập và chính sách cung ứng từ nhà cung cấp
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200/60 rounded-xl transition-colors cursor-pointer"
          >
            <X size={20} />
          </button>
        </div>

        {/* MODAL BODY */}
        <div className="p-6">
          <form id="supplierProductForm" onSubmit={handleSubmit} className="flex flex-col gap-6">
            {/* ROW 1: NHÀ CUNG CẤP & BIẾN THỂ */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
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
                  onSelect={(val) => handleFieldChange('supplierId', Number(val))}
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
                  onSelect={handleVariantSelect}
                />

                {/* THẺ PREVIEW BIẾN THỂ ĐƯỢC CHỌN */}
                {Boolean(formData.variantId) && (selectedVariant || initialData) && (
                  <div className="flex items-center gap-3.5 p-3.5 rounded-2xl bg-slate-50/80 border border-slate-200 mt-3 shadow-2xs animate-in fade-in">
                    <div className="w-13 h-13 rounded-xl bg-white border border-slate-200 shadow-2xs flex items-center justify-center shrink-0 overflow-hidden">
                      {variantImage ? (
                        <img
                          src={variantImage}
                          alt="Ảnh sản phẩm"
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        <Package size={22} className="text-slate-300" />
                      )}
                    </div>
                    <div className="flex flex-col min-w-0 flex-1">
                      <span className="font-extrabold text-slate-800 text-[13px] truncate">
                        {selectedVariant?.name ||
                          initialData?.variantName ||
                          `Biến thể #${formData.variantId}`}
                      </span>
                      <div className="flex flex-wrap items-center gap-2 mt-1">
                        <span className="text-[10px] font-bold bg-amber-100 text-amber-900 px-2 py-0.5 rounded-md border border-amber-300 uppercase tracking-wider">
                          {selectedVariant?.code ||
                            initialData?.variantCode ||
                            `#${formData.variantId}`}
                        </span>
                        {detectedUoMName && (
                          <span className="text-[11px] font-bold text-slate-600 bg-slate-200/60 px-2 py-0.5 rounded-md">
                            ĐVT: <b className="text-slate-900">{detectedUoMName}</b>
                          </span>
                        )}
                        {selectedVariant?.prices && selectedVariant.prices.length > 0 && (
                          <span className="text-[11px] text-slate-500 font-medium ml-auto">
                            Giá niêm yết:{' '}
                            <strong className="text-slate-800 font-bold">
                              {selectedVariant.prices[0].price.toLocaleString('vi-VN')} ₫
                            </strong>
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* ROW 2: ĐƠN GIÁ NHẬP & ĐVT MUA HÀNG (TỰ ĐỘNG ĐIỀN & KHÓA CỨNG) */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
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

              <div>
                <FormSelect
                  label="Đơn Vị Tính Mua Hàng"
                  required
                  placeholder={
                    formData.variantId ? 'Chọn ĐVT mua hàng...' : 'Vui lòng chọn sản phẩm trước...'
                  }
                  value={formData.purchaseUoMId || ''}
                  options={
                    validUoMOptions.length > 0
                      ? validUoMOptions.map((u) => ({
                          value: u.uoMId,
                          label: `${u.uoMName} (${u.description})`,
                        }))
                      : formData.variantId && (formData.purchaseUoMId || detectedUoMId > 0)
                      ? [
                          {
                            value: formData.purchaseUoMId || detectedUoMId,
                            label: `${detectedUoMName || 'Đơn vị tính'} (ĐVT của sản phẩm)`,
                          },
                        ]
                      : uomOptions
                  }
                  error={errors.purchaseUoMId}
                  disabled={isSubmitting || !formData.variantId}
                  onSelect={(val) => handleFieldChange('purchaseUoMId', Number(val))}
                />
                {Boolean(formData.variantId && (detectedUoMName || formData.purchaseUoMId > 0)) && (
                  <p className="text-[11px] text-amber-800 font-semibold mt-1.5 flex items-center gap-1.5 bg-amber-50/80 px-3 py-1.5 rounded-xl border border-amber-200">
                    <span>{validUoMOptions.length > 1 ? '💡' : '🔒'}</span>
                    <span>
                      {validUoMOptions.length > 1
                        ? `Đã nạp ${validUoMOptions.length} ĐVT hợp lệ. Hệ thống tự động quy đổi về ĐVT cơ sở (${detectedUoMName || 'Chuẩn'}) khi nhập kho.`
                        : `Cố định theo đơn vị tính của sản phẩm (${detectedUoMName || 'Chuẩn'}). Đơn mua hàng từ NCC sẽ áp dụng đơn vị này.`}
                    </span>
                  </p>
                )}
              </div>
            </div>

            {/* ROW 3: MOQ, LEAD TIME & MÃ SKU NCC */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
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

            {/* SWITCH HOẠT ĐỘNG */}
            <div className="flex items-center gap-3.5 bg-slate-50/80 p-4 rounded-2xl border border-slate-200/80">
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  className="sr-only peer"
                  checked={formData.isActive}
                  disabled={isSubmitting}
                  onChange={(e) => handleFieldChange('isActive', e.target.checked)}
                />
                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-amber-400"></div>
              </label>
              <div className="flex flex-col">
                <span className="text-[13px] font-bold text-slate-800">
                  Đang Cung Ứng (Kích hoạt)
                </span>
                <span className="text-[11px] text-slate-500 font-medium">
                  Cho phép chọn sản phẩm này khi tạo đơn mua hàng từ NCC.
                </span>
              </div>
            </div>
          </form>
        </div>

        {/* MODAL FOOTER THEME SOLARIS */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 bg-slate-50 border-t border-slate-100 rounded-b-3xl">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-5 py-2.5 rounded-xl border border-slate-200 text-slate-600 font-bold text-sm hover:bg-slate-100 transition-colors cursor-pointer"
          >
            Hủy Bỏ
          </button>
          <button
            type="submit"
            form="supplierProductForm"
            disabled={isSubmitting}
            className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-900 bg-amber-400 hover:bg-amber-500 transition-all shadow-sm shadow-amber-200 active:scale-98 disabled:opacity-50 cursor-pointer"
          >
            {isSubmitting ? (
              <div className="w-4 h-4 border-2 border-slate-900/30 border-t-slate-900 rounded-full animate-spin"></div>
            ) : (
              <Save size={16} strokeWidth={2.5} />
            )}
            <span>{initialData ? 'Lưu Cập Nhật' : 'Thêm Vào Bảng Giá'}</span>
          </button>
        </div>
      </div>
    </div>
  );

  if (typeof document !== 'undefined') {
    return createPortal(modalContent, document.body);
  }
  return modalContent;
};
