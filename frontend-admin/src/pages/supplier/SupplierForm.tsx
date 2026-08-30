import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Hexagon, Save, Plus, MapPin, Building2 } from 'lucide-react';

// API & Types (Sếp nhớ tạo các file API này tương tự bên Customer nhé)
import { supplierApi } from '../../api/supplierApi';
import { supplierTypeApi } from '../../api/supplierTypeApi';
import { SupplierPayload } from '../../types/supplier';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormInput,
  FormHeader,
  FormSection,
  FormSelect,
  FormTextarea,
  SubmitButton,
} from '../../components/commons/FormUI';
import { GhnAddressSelect } from '../../components/commons/GhnAddressSelect';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: SupplierPayload = {
  code: '',
  name: '',
  logoPath: '',
  phone: '',
  email: '',
  taxCode: '',
  website: '',
  socialLink: '',
  bankAccount: '',
  bankName: '',
  note: '',
  isActive: true,
  supplierTypeId: 0,
  // Gắn sẵn 1 object rỗng cho địa chỉ mặc định khi tạo mới
  addresses: [
    {
      contactName: '',
      contactPhone: '',
      province: '',
      district: '',
      ward: '',
      streetAddress: '',
      isDefault: true,
    },
  ],
};

const STATUS_OPTIONS = [
  { label: 'Hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

const SupplierForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<SupplierPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});

  // Kiểm tra trùng lặp
  const [existingCodes, setExistingCodes] = useState<string[]>([]);
  const [originalCode, setOriginalCode] = useState('');
  const [existingPhones, setExistingPhones] = useState<string[]>([]);
  const [originalPhone, setOriginalPhone] = useState('');

  const [loading, setLoading] = useState(false);

  // Options cho Dropdown
  const [typeOptions, setTypeOptions] = useState<{ label: string; value: number }[]>([]);

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
    // Lấy danh sách Loại NCC và Danh sách NCC (để check trùng)
    Promise.all([supplierTypeApi.getAllList(), supplierApi.getAllList()])
      .then(([types, suppliers]) => {
        setTypeOptions(types.map((item: any) => ({ label: item.name, value: item.id })));
        setExistingCodes(suppliers.map((s: any) => s.code.toLowerCase()));
        setExistingPhones(suppliers.map((s: any) => s.phone.toLowerCase()));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách tùy chọn'));

    // Tải dữ liệu chi tiết khi Edit
    if (isEditMode && id) {
      supplierApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            phone: res.phone || '',
            logoPath: res.logoPath || '',
            email: res.email || '',
            taxCode: res.taxCode || '',
            website: res.website || '',
            socialLink: res.socialLink || '',
            bankAccount: res.bankAccount || '',
            bankName: res.bankName || '',
            note: res.note || '',
            isActive: res.isActive,
            supplierTypeId: res.supplierTypeId,
            addresses: [], // Không dùng trường này khi Edit (Quản lý ở tab riêng)
          });
          setOriginalCode((res.code || '').toLowerCase());
          setOriginalPhone((res.phone || '').toLowerCase());
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof SupplierPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const handleAddressChange = (field: string, value: string) => {
    setFormData((prev) => {
      const addr = prev.addresses?.[0] || INITIAL_STATE.addresses![0];
      return { ...prev, addresses: [{ ...addr, [field]: value }] };
    });
    if (errors[`address_${field}`]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[`address_${field}`];
        return newErrors;
      });
    }
  };

  // --- VALIDATION ---
  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    const trimmedCode = formData.code.trim().toLowerCase();
    const trimmedPhone = formData.phone.trim().toLowerCase();

    // 1. Kiểm tra Thông tin cơ bản
    if (!trimmedCode) newErrors.code = 'Mã nhà cung cấp không được để trống';
    else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
      newErrors.code = 'Mã nhà cung cấp này đã tồn tại';
    }

    if (!formData.name.trim()) newErrors.name = 'Tên nhà cung cấp không được để trống';

    if (!trimmedPhone) newErrors.phone = 'Số điện thoại không được để trống';
    else if (
      existingPhones.includes(trimmedPhone) &&
      (!isEditMode || trimmedPhone !== originalPhone)
    ) {
      newErrors.phone = 'Số điện thoại này đã tồn tại';
    }

    if (!formData.email?.trim()) {
      newErrors.email = 'Email không được để trống';
    } else {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(formData.email.trim())) newErrors.email = 'Email không đúng định dạng';
    }

    if (!formData.supplierTypeId) newErrors.supplierTypeId = 'Vui lòng chọn loại nhà cung cấp';

    // 2. Kiểm tra Địa chỉ (Chỉ khi Tạo mới)
    if (!isEditMode && formData.addresses && formData.addresses.length > 0) {
      const addr = formData.addresses[0];
      // Nếu có gõ bất kỳ trường nào, bắt buộc phải nhập đủ
      if (addr.contactName || addr.contactPhone || addr.province || addr.streetAddress) {
        if (!addr.contactName.trim())
          newErrors.address_contactName = 'Người liên hệ không được để trống';
        if (!addr.contactPhone.trim())
          newErrors.address_contactPhone = 'Số điện thoại liên hệ không được để trống';
        if (!addr.province.trim()) newErrors.address_province = 'Tỉnh/Thành phố không được để trống';
        if (!addr.district.trim()) newErrors.address_district = 'Quận/Huyện không được để trống';
        if (!addr.ward.trim()) newErrors.address_ward = 'Phường/Xã không được để trống';
        if (!addr.streetAddress.trim())
          newErrors.address_streetAddress = 'Địa chỉ chi tiết không được để trống';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại thông tin bị lỗi!');

    setLoading(true);
    try {
      const cleanPayload: SupplierPayload = {
        ...formData,
        code: formData.code.trim().toUpperCase(),
        name: formData.name.trim(),
        phone: formData.phone.trim(),
        email: formData.email.trim(),
        taxCode: formData.taxCode?.trim() || null,
        website: formData.website?.trim() || null,
        socialLink: formData.socialLink?.trim() || null,
        bankAccount: formData.bankAccount?.trim() || null,
        bankName: formData.bankName?.trim() || null,
        note: formData.note?.trim() || null,
        // Lọc bỏ địa chỉ rỗng
        addresses:
          !isEditMode && formData.addresses![0].streetAddress.trim()
            ? formData.addresses
            : undefined,
      };

      if (isEditMode && id) {
        await supplierApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT THÀNH CÔNG');
      } else {
        await supplierApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI THÀNH CÔNG');
      }
      setTimeout(() => navigate('/suppliers'), 1000);
    } catch (error: any) {
      showToast(
        'error',
        error.response?.status === 400 ? 'DỮ LIỆU KHÔNG HỢP LỆ' : 'CÓ LỖI XẢY RA KHI LƯU'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa Nhà Cung Cấp' : 'Thêm Mới Nhà Cung Cấp'}
        subtitle={
          isEditMode ? 'Cập nhật thông tin đối tác' : 'Thêm mới đối tác cung ứng vào hệ thống'
        }
        onBack={() => navigate('/suppliers')}
        icon={Building2}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-10">
          {/* SECTION 1: THÔNG TIN CÔNG TY */}
          <FormSection title="Thông Tin Doanh Nghiệp">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormInput
                label="Mã NCC"
                required
                placeholder="NCC-001"
                value={formData.code}
                error={errors.code}
                disabled={loading || isEditMode}
                onChange={(e) => handleFieldChange('code', e.target.value)}
              />
              <FormInput
                label="Tên Công Ty/Đối tác"
                required
                placeholder="Công ty TNHH..."
                value={formData.name}
                error={errors.name}
                disabled={loading}
                onChange={(e) => handleFieldChange('name', e.target.value)}
              />
              <FormSelect
                label="Phân Loại"
                required
                placeholder="Chọn loại..."
                showSearch
                options={typeOptions}
                value={formData.supplierTypeId}
                error={errors.supplierTypeId}
                onSelect={(val) => handleFieldChange('supplierTypeId', val)}
              />
              <FormInput
                label="Số điện thoại chính"
                required
                placeholder="0901234567"
                value={formData.phone}
                error={errors.phone}
                disabled={loading}
                onChange={(e) => handleFieldChange('phone', e.target.value)}
              />
              <FormInput
                label="Email công ty"
                required
                placeholder="contact@company.com"
                value={formData.email || ''}
                error={errors.email}
                disabled={loading}
                onChange={(e) => handleFieldChange('email', e.target.value)}
              />
              <FormInput
                label="Mã số thuế"
                placeholder="VD: 0312..."
                value={formData.taxCode || ''}
                onChange={(e) => handleFieldChange('taxCode', e.target.value)}
              />
              <FormInput
                label="Website"
                placeholder="https://..."
                value={formData.website || ''}
                onChange={(e) => handleFieldChange('website', e.target.value)}
              />
              <FormSelect
                label="Trạng thái hợp tác"
                required
                value={formData.isActive ? 1 : 0}
                options={STATUS_OPTIONS}
                onSelect={(val) => handleFieldChange('isActive', val === 1)}
              />
              <FormInput
                label="Logo"
                placeholder="https://..."
                value={formData.logoPath || ''}
                onChange={(e) => handleFieldChange('logoPath', e.target.value)}
              />
            </div>
          </FormSection>

          {/* SECTION 2: ĐỊA CHỈ TRỤ SỞ/KHO CHÍNH */}
          {!isEditMode && (
            <FormSection title="Địa Chỉ Kho Chính (Tùy chọn)">
              <div className="p-6 rounded-2xl border border-blue-100 bg-blue-50/30">
                <div className="flex items-center gap-2 mb-4 text-blue-800">
                  <MapPin size={18} />
                  <span className="text-sm font-bold">Thiết lập địa chỉ lấy/trả hàng mặc định</span>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="lg:col-span-2">
                    <FormInput
                      label="Người phụ trách kho"
                      placeholder="Tên thủ kho..."
                      value={formData.addresses![0].contactName}
                      error={errors.address_contactName}
                      onChange={(e) => handleAddressChange('contactName', e.target.value)}
                    />
                  </div>
                  <div className="lg:col-span-2">
                    <FormInput
                      label="SĐT phụ trách"
                      placeholder="09..."
                      value={formData.addresses![0].contactPhone}
                      error={errors.address_contactPhone}
                      onChange={(e) => handleAddressChange('contactPhone', e.target.value)}
                    />
                  </div>
                  <GhnAddressSelect
                    province={formData.addresses![0].province}
                    district={formData.addresses![0].district}
                    ward={formData.addresses![0].ward}
                    onProvinceChange={(p) => handleAddressChange('province', p)}
                    onDistrictChange={(d) => handleAddressChange('district', d)}
                    onWardChange={(w) => handleAddressChange('ward', w)}
                    errors={{
                      province: errors.address_province,
                      district: errors.address_district,
                      ward: errors.address_ward,
                    }}
                    required={false}
                  />
                  <div className="lg:col-span-4">
                    <FormInput
                      label="Địa chỉ chi tiết (Số nhà, đường)"
                      placeholder="Số 12, Lê Duẩn..."
                      value={formData.addresses![0].streetAddress}
                      error={errors.address_streetAddress}
                      onChange={(e) => handleAddressChange('streetAddress', e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </FormSection>
          )}

          {/* SECTION 3: THÔNG TIN THANH TOÁN & GHI CHÚ */}
          <FormSection title="Thanh Toán & Ghi Chú">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormInput
                label="Số tài khoản (VNĐ)"
                placeholder="1903..."
                value={formData.bankAccount || ''}
                onChange={(e) => handleFieldChange('bankAccount', e.target.value)}
              />
              <FormInput
                label="Ngân hàng & Chi nhánh"
                placeholder="Techcombank Chi nhánh..."
                value={formData.bankName || ''}
                onChange={(e) => handleFieldChange('bankName', e.target.value)}
              />
              <div className="md:col-span-2">
                <FormTextarea
                  label="Ghi chú nội bộ"
                  placeholder="Chính sách công nợ, đánh giá NCC..."
                  value={formData.note || ''}
                  rows={2}
                  onChange={(e: any) => handleFieldChange('note', e.target.value)}
                />
              </div>
            </div>
          </FormSection>

          {/* FOOTER & BUTTON */}
          <div className="flex justify-end pt-6 border-t border-slate-100 mt-2">
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

export default SupplierForm;
