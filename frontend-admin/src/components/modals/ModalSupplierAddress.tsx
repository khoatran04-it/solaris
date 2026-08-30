import React, { useEffect, useState } from 'react';
import { X, MapPin, Save } from 'lucide-react';
import { FormInput } from '../commons/FormUI';
import { GhnAddressSelect } from '../commons/GhnAddressSelect';
import { SupplierAddressPayload } from '../../types/supplierAddress'; // Sếp nhớ tạo type này nhé

interface ModalSupplierAddressProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: SupplierAddressPayload) => Promise<void>;
  initialData?: SupplierAddressPayload | null; // Dùng khi muốn Edit địa chỉ
}

const INITIAL_STATE: SupplierAddressPayload = {
  contactName: '',
  contactPhone: '',
  province: '',
  district: '',
  ward: '',
  streetAddress: '',
  isDefault: false,
};

export const ModalSupplierAddress: React.FC<ModalSupplierAddressProps> = ({
  isOpen,
  onClose,
  onSave,
  initialData,
}) => {
  const [formData, setFormData] = useState<SupplierAddressPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<keyof SupplierAddressPayload, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset hoặc Load dữ liệu mỗi khi Modal mở
  useEffect(() => {
    if (isOpen) {
      setFormData(initialData || INITIAL_STATE);
      setErrors({});
    }
  }, [isOpen, initialData]);

  // Không render HTML nếu Modal đang đóng (Tối ưu hiệu suất)
  if (!isOpen) return null;

  const handleFieldChange = (field: keyof SupplierAddressPayload, value: any) => {
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
    const newErrors: Partial<Record<keyof SupplierAddressPayload, string>> = {};

    if (!formData.contactName.trim())
      newErrors.contactName = 'Vui lòng nhập tên người liên hệ kho.';
    if (!formData.contactPhone.trim()) newErrors.contactPhone = 'Vui lòng nhập SĐT liên hệ kho.';
    if (!formData.province.trim()) newErrors.province = 'Vui lòng nhập Tỉnh/Thành phố.';
    if (!formData.streetAddress.trim())
      newErrors.streetAddress = 'Vui lòng nhập số nhà, tên đường.';
    if (!formData.district.trim()) newErrors.district = 'Vui lòng nhập Quận/huyện.';
    if (!formData.ward.trim()) newErrors.ward = 'Vui lòng nhập Phường/xã.';

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    try {
      const cleanData: SupplierAddressPayload = {
        contactName: formData.contactName.trim(),
        contactPhone: formData.contactPhone.trim(),
        province: formData.province.trim(),
        district: formData.district?.trim(),
        ward: formData.ward?.trim(),
        streetAddress: formData.streetAddress.trim(),
        isDefault: formData.isDefault,
      };

      await onSave(cleanData);
      onClose(); // Thành công thì tự động đóng Modal
    } catch (error) {
      // Lỗi API sẽ được Component cha (SupplierDetail) catch và hiện Toast
      console.error(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-100 flex items-center justify-center p-4">
      {/* Lớp Overlay làm mờ nền */}
      <div
        className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity"
        onClick={!isSubmitting ? onClose : undefined}
      ></div>

      {/* Hộp Modal */}
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center gap-2.5 text-slate-800">
            <MapPin size={20} className="text-yellow-500" />
            <h3 className="font-bold text-[16px] uppercase tracking-wide">
              {initialData ? 'Cập Nhật Địa Chỉ Kho' : 'Thêm Địa Chỉ Kho Mới'}
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

        {/* Modal Body (Form) */}
        <div className="p-6">
          <form id="addressForm" onSubmit={handleSubmit} className="flex flex-col gap-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormInput
                label="Người liên hệ tại kho"
                required
                placeholder="Nguyễn Văn B..."
                value={formData.contactName}
                error={errors.contactName}
                disabled={isSubmitting}
                onChange={(e) => handleFieldChange('contactName', e.target.value)}
              />
              <FormInput
                label="Số điện thoại kho"
                required
                placeholder="09..."
                value={formData.contactPhone}
                error={errors.contactPhone}
                disabled={isSubmitting}
                onChange={(e) => handleFieldChange('contactPhone', e.target.value)}
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <GhnAddressSelect
                province={formData.province}
                district={formData.district || ''}
                ward={formData.ward || ''}
                onProvinceChange={(p) => handleFieldChange('province', p)}
                onDistrictChange={(d) => handleFieldChange('district', d)}
                onWardChange={(w) => handleFieldChange('ward', w)}
                errors={{
                  province: errors.province,
                  district: errors.district,
                  ward: errors.ward,
                }}
                disabled={isSubmitting}
              />
            </div>

            <FormInput
              label="Địa chỉ chi tiết (Số nhà, đường)"
              required
              placeholder="Số 12, Lê Duẩn..."
              value={formData.streetAddress}
              error={errors.streetAddress}
              disabled={isSubmitting}
              onChange={(e) => handleFieldChange('streetAddress', e.target.value)}
            />

            {/* Nút Toggle "Đặt làm mặc định" chuẩn UI/UX */}
            <div className="flex items-center gap-3 mt-2 bg-slate-50 p-4 rounded-xl border border-slate-100">
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  className="sr-only peer"
                  checked={formData.isDefault}
                  disabled={isSubmitting}
                  onChange={(e) => handleFieldChange('isDefault', e.target.checked)}
                />
                <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-yellow-400"></div>
              </label>
              <div className="flex flex-col">
                <span className="text-[13px] font-bold text-slate-700">
                  Đặt làm địa chỉ lấy hàng mặc định
                </span>
                <span className="text-[11px] text-slate-500">
                  Hệ thống sẽ ưu tiên kho này khi tạo đơn nhập hàng (PO).
                </span>
              </div>
            </div>
          </form>
        </div>

        {/* Modal Footer */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 bg-slate-50 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors"
          >
            Hủy Bỏ
          </button>
          <button
            type="submit"
            form="addressForm" // Liên kết nút với form ở trên
            disabled={isSubmitting}
            className="flex items-center gap-2 px-5 py-2.5 text-sm font-bold text-slate-900 bg-yellow-400 rounded-xl hover:bg-yellow-500 shadow-sm shadow-yellow-200 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? (
              <div className="w-5 h-5 border-2 border-slate-900/30 border-t-slate-900 rounded-full animate-spin"></div>
            ) : (
              <Save size={18} strokeWidth={2.5} />
            )}
            <span>{initialData ? 'Lưu Cập Nhật' : 'Thêm Địa Chỉ'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
