import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Save, Plus, Hexagon } from 'lucide-react';

// API & Types
import { supplierTypeApi } from '../../api/supplierTypeApi';
import { SupplierTypePayload } from '../../types/supplierType';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormInput,
  FormTextarea,
  FormSelect,
  FormSection,
  SubmitButton,
  FormHeader,
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: SupplierTypePayload = {
  code: '',
  name: '',
  description: '',
  isActive: true,
};

// Options cho trạng thái
const STATUS_OPTIONS = [
  { label: 'Hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

const SupplierTypeForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<SupplierTypePayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<keyof SupplierTypePayload, string>>>({});
  const [existingCodes, setExistingCodes] = useState<string[]>([]);
  const [originalCode, setOriginalCode] = useState('');
  const [existingNames, setExistingNames] = useState<string[]>([]);
  const [originalName, setOriginalName] = useState('');
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

  // --- EFFECTS ---
  useEffect(() => {
    // Tải danh sách mã để kiểm tra trùng
    supplierTypeApi
      .getAllList()
      .then((res) => setExistingCodes(res.map((x) => x.code.toLowerCase())))
      .catch(() => console.error('Không tải được danh sách mã kiểm tra'));

    // Tải danh sách tên để kiểm tra trùng
    supplierTypeApi
      .getAllList()
      .then((res) => setExistingNames(res.map((x) => x.name.toLowerCase())))
      .catch(() => console.error('Không tải được danh sách tên kiểm tra'));

    // Tải dữ liệu chi tiết khi ở chế độ chỉnh sửa
    if (isEditMode && id) {
      supplierTypeApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            description: res.description || '',
            isActive: res.isActive !== false,
          });
          setOriginalCode((res.code || '').toLowerCase());
          setOriginalName((res.name || '').toLowerCase());
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof SupplierTypePayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  // --- VALIDATION ---
  const validateForm = () => {
    const newErrors: Partial<Record<keyof SupplierTypePayload, string>> = {};
    const trimmedCode = formData.code.trim().toLowerCase();
    const trimmedName = formData.name.trim().toLowerCase();

    // Kiểm tra Mã
    if (!trimmedCode) {
      newErrors.code = 'Mã phân loại không được để trống';
    } else if (trimmedCode.length < 2) {
      newErrors.code = 'Mã phân loại phải có ít nhất 2 ký tự';
    } else {
      const isDuplicate = existingCodes.includes(trimmedCode);
      const isSelf = isEditMode && trimmedCode === originalCode;
      if (isDuplicate && !isSelf) newErrors.code = 'Mã phân loại này đã tồn tại';
    }

    // Kiểm tra Tên
    if (!trimmedName) {
      newErrors.name = 'Tên phân loại không được để trống';
    } else if (trimmedName.length < 2) {
      newErrors.name = 'Tên phân loại phải có ít nhất 2 ký tự';
    } else {
      const isDuplicate = existingNames.includes(trimmedName);
      const isSelf = isEditMode && trimmedName === originalName;
      if (isDuplicate && !isSelf) newErrors.name = 'Tên phân loại này đã tồn tại';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin!');

    setLoading(true);
    try {
      const cleanPayload: SupplierTypePayload = {
        ...formData,
        code: formData.code.trim().toUpperCase(),
        name: formData.name.trim(),
        description: formData.description?.trim() || '',
        isActive: Boolean(formData.isActive),
      };

      if (isEditMode && id) {
        await supplierTypeApi.update(Number(id), cleanPayload);
        showToast('success', `CẬP NHẬT THÀNH CÔNG`);
      } else {
        await supplierTypeApi.create(cleanPayload);
        showToast('success', `THÊM MỚI THÀNH CÔNG`);
      }
      setTimeout(() => navigate('/supplier-types'), 1000);
    } catch (error: any) {
      showToast('error', error.response?.status === 400 ? 'DỮ LIỆU KHÔNG HỢP LỆ' : 'CÓ LỖI XẢY RA');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Loại Nhà Cung Cấp' : 'Thêm Mới Loại Nhà Cung Cấp'}
        subtitle={
          isEditMode
            ? 'Cập nhật thông tin danh mục nhà cung cấp'
            : 'Thiết lập danh mục nhà cung cấp mới cho hệ thống'
        }
        onBack={() => navigate('/supplier-types')}
        icon={Hexagon}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-8">
          <FormSection title="Thông Tin Phân Loại">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6">
              <FormInput
                label="Mã định danh"
                required
                placeholder="VD: FARM, COOP, IMPORT"
                value={formData.code}
                error={errors.code}
                disabled={loading}
                onChange={(e) => handleFieldChange('code', e.target.value)}
              />

              <FormInput
                label="Tên phân loại"
                required
                placeholder="VD: Nhà vườn, Hợp tác xã, Nhập khẩu..."
                value={formData.name}
                error={errors.name}
                disabled={loading}
                onChange={(e) => handleFieldChange('name', e.target.value)}
              />

              {/* Ô Select chọn Trạng Thái */}
              <FormSelect
                label="Trạng thái phân loại"
                required
                value={formData.isActive ? 1 : 0}
                options={STATUS_OPTIONS}
                onSelect={(val) => handleFieldChange('isActive', val === 1)}
              />

              <div className="md:col-span-2">
                <FormTextarea
                  label="Mô tả chi tiết"
                  placeholder="Nhập ghi chú hoặc mô tả chi tiết về loại nhà cung cấp này..."
                  value={formData.description}
                  rows={4}
                  disabled={loading}
                  onChange={(e) => handleFieldChange('description', e.target.value)}
                />
              </div>
            </div>
          </FormSection>

          <div className="flex justify-end pt-6 border-t border-slate-100">
            <SubmitButton
              loading={loading}
              isEditMode={isEditMode}
              icon={isEditMode ? Save : Plus}
            />
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default SupplierTypeForm;
