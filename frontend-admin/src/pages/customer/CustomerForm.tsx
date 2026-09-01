import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Hexagon, Save, Plus, MapPin, Eye, EyeOff, Lock } from 'lucide-react';

// API & Types
import { customerApi } from '../../api/customerApi';
import { customerTypeApi } from '../../api/customerTypeApi';
import { customerTierApi } from '../../api/customerTierApi';
import { customerGroupApi } from '../../api/customerGroupApi';
import { CustomerPayload } from '../../types/customer';

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
import CustomDatePicker from '../../components/commons/CustomDatePicker'; // Component Custom của bạn

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: CustomerPayload = {
  code: '',
  name: '',
  phoneNumber: '',
  email: '',
  password: '',
  taxCode: '',
  avatarPath: '',
  birthday: null,
  gender: null,
  note: '',
  isActive: true,
  customerTypeId: 0,
  customerTierId: 0,
  groupIds: [],
  // Gắn sẵn 1 object rỗng cho địa chỉ mặc định khi tạo mới
  addresses: [
    {
      receiverName: '',
      phone: '',
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

const GENDER_OPTIONS = [
  { label: 'Nam', value: 1 },
  { label: 'Nữ', value: 0 },
];

const CustomerForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<CustomerPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});

  // Kiểm tra trùng lặp (Mã và SĐT là 2 trường dễ trùng nhất)
  const [existingCodes, setExistingCodes] = useState<string[]>([]);
  const [originalCode, setOriginalCode] = useState('');
  const [existingPhones, setExistingPhones] = useState<string[]>([]);
  const [originalPhone, setOriginalPhone] = useState('');

  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  // Options cho các Dropdown/Badges
  const [typeOptions, setTypeOptions] = useState<{ label: string; value: number }[]>([]);
  const [tierOptions, setTierOptions] = useState<{ label: string; value: number }[]>([]);
  const [groupOptions, setGroupOptions] = useState<{ label: string; value: number }[]>([]);

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
    // Gọi đồng loạt các API để lấy data cho form
    Promise.all([
      customerTypeApi.getAllList(),
      customerTierApi.getAllList(),
      customerGroupApi.getAllList(true), // Chỉ lấy nhóm đang hoạt động
      customerApi.getAllList(),
    ])
      .then(([types, tiers, groups, customers]) => {
        setTypeOptions(types.map((item) => ({ label: item.name, value: item.id })));
        setTierOptions(tiers.map((item) => ({ label: item.name, value: item.id })));
        setGroupOptions(groups.map((item) => ({ label: item.name, value: item.id })));

        // Map danh sách để kiểm tra trùng
        setExistingCodes(customers.map((c) => c.code.toLowerCase()));
        setExistingPhones(customers.map((c) => c.phoneNumber.toLowerCase()));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách tùy chọn'));

    // Tải dữ liệu chi tiết khi Edit
    if (isEditMode && id) {
      customerApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            phoneNumber: res.phoneNumber || '',
            email: res.email || '',
            taxCode: res.taxCode || '',
            avatarPath: res.avatarPath || '',
            birthday: res.birthday || null,
            gender: res.gender ?? null,
            note: res.note || '',
            isActive: res.isActive,
            customerTypeId: res.customerTypeId,
            customerTierId: res.customerTierId,
            groupIds: res.groupIds || [],
            addresses: [], // Không dùng trường này khi Edit
          });
          setOriginalCode((res.code || '').toLowerCase());
          setOriginalPhone((res.phoneNumber || '').toLowerCase());
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof CustomerPayload, value: any) => {
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

  // Hàm chuyển đổi nhóm bằng cách Bấm (Toggle)
  const toggleGroup = (groupId: number) => {
    setFormData((prev) => {
      const currentGroups = prev.groupIds || [];
      const newGroups = currentGroups.includes(groupId)
        ? currentGroups.filter((id) => id !== groupId)
        : [...currentGroups, groupId];
      return { ...prev, groupIds: newGroups };
    });
  };

  // --- VALIDATION ---
  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    const trimmedCode = formData.code.trim().toLowerCase();
    const trimmedPhone = formData.phoneNumber.trim().toLowerCase();

    // 1. Kiểm tra Thông tin cơ bản
    if (!trimmedCode) newErrors.code = 'Vui lòng nhập mã định danh.';
    else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
      newErrors.code = 'Mã định danh đã tồn tại!';
    }

    if (!formData.name.trim()) newErrors.name = 'Vui lòng nhập tên khách hàng.';

    if (!trimmedPhone) newErrors.phoneNumber = 'Vui lòng nhập số điện thoại.';
    else if (
      existingPhones.includes(trimmedPhone) &&
      (!isEditMode || trimmedPhone !== originalPhone)
    ) {
      newErrors.phoneNumber = 'Số điện thoại này đã được đăng ký!';
    }

    if (formData.email?.trim()) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(formData.email.trim())) newErrors.email = 'Email không hợp lệ.';
    }

    if (formData.password?.trim() && formData.password.trim().length < 6) {
      newErrors.password = 'Mật khẩu phải có tối thiểu 6 ký tự.';
    }

    // 2. Kiểm tra Phân quyền
    if (!formData.customerTypeId) newErrors.customerTypeId = 'Vui lòng chọn loại khách hàng.';
    if (!formData.customerTierId) newErrors.customerTierId = 'Vui lòng chọn bậc xếp hạng.';

    // 3. Kiểm tra Địa chỉ (Chỉ khi Tạo mới)
    if (!isEditMode && formData.addresses && formData.addresses.length > 0) {
      const addr = formData.addresses[0];
      // Nếu người dùng có gõ bất cứ trường địa chỉ nào, bắt buộc phải gõ đủ các trường quan trọng
      if (addr.receiverName || addr.phone || addr.province || addr.streetAddress) {
        if (!addr.receiverName.trim())
          newErrors.address_receiverName = 'Vui lòng nhập tên người nhận.';
        if (!addr.phone.trim()) newErrors.address_phone = 'Vui lòng nhập SĐT người nhận.';
        if (!addr.province.trim()) newErrors.address_province = 'Thiếu thông tin Tỉnh/Thành.';
        if (!addr.district.trim()) newErrors.address_district = 'Thiếu Quận/Huyện.';
        if (!addr.ward.trim()) newErrors.address_ward = 'Thiếu Phường/Xã.';
        if (!addr.streetAddress.trim())
          newErrors.address_streetAddress = 'Vui lòng nhập số nhà, tên đường.';
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
      const cleanPayload: CustomerPayload = {
        ...formData,
        code: formData.code.trim().toUpperCase(),
        name: formData.name.trim(),
        phoneNumber: formData.phoneNumber.trim(),
        email: formData.email?.trim() || null,
        password: formData.password?.trim() || null,
        taxCode: formData.taxCode?.trim() || null,
        avatarPath: formData.avatarPath?.trim() || null,
        note: formData.note?.trim() || null,
        // Lọc bỏ địa chỉ rỗng nếu người dùng không nhập gì lúc Tạo mới
        addresses:
          !isEditMode && formData.addresses![0].streetAddress.trim()
            ? formData.addresses
            : undefined,
      };

      if (isEditMode && id) {
        await customerApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT THÀNH CÔNG');
      } else {
        await customerApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI THÀNH CÔNG');
      }
      setTimeout(() => navigate('/customers'), 1000);
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
        title={isEditMode ? 'Chỉnh Sửa Khách Hàng' : 'Tạo Hồ Sơ Khách Hàng'}
        subtitle={
          isEditMode ? 'Cập nhật thông tin và phân hạng' : 'Tạo mới hồ sơ khách hàng vào hệ thống'
        }
        onBack={() => navigate('/customers')}
        icon={Hexagon}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-10">
          {/* SECTION 1: THÔNG TIN CÁ NHÂN */}
          <FormSection title="Thông Tin Cá Nhân">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormInput
                label="Mã KH"
                required
                placeholder="KH-001"
                value={formData.code}
                error={errors.code}
                disabled={loading}
                onChange={(e) => handleFieldChange('code', e.target.value)}
              />
              <FormInput
                label="Họ và tên"
                required
                placeholder="Nguyễn Văn A"
                value={formData.name}
                error={errors.name}
                disabled={loading}
                onChange={(e) => handleFieldChange('name', e.target.value)}
              />
              <FormInput
                label="Số điện thoại"
                required
                placeholder="0901234567"
                value={formData.phoneNumber}
                error={errors.phoneNumber}
                disabled={loading}
                onChange={(e) => handleFieldChange('phoneNumber', e.target.value)}
              />
              <div className="relative">
                <FormInput
                  label={isEditMode ? 'Đặt lại Mật khẩu' : 'Mật khẩu Web/App (Tùy chọn)'}
                  type={showPassword ? 'text' : 'password'}
                  placeholder={
                    isEditMode
                      ? 'Để trống nếu giữ nguyên...'
                      : 'Nhập nếu muốn cấp quyền đăng nhập Shop...'
                  }
                  value={formData.password || ''}
                  error={errors.password}
                  disabled={loading}
                  onChange={(e) => handleFieldChange('password', e.target.value)}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 top-[38px] text-slate-400 hover:text-slate-600 transition-colors p-1"
                  tabIndex={-1}
                  title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              <FormInput
                label="Email"
                placeholder="email@example.com"
                value={formData.email || ''}
                error={errors.email}
                disabled={loading}
                onChange={(e) => handleFieldChange('email', e.target.value)}
              />

              {/* CUSTOM DATE PICKER */}
              <CustomDatePicker
                label="Ngày sinh"
                value={formData.birthday ? new Date(formData.birthday) : null}
                onChange={(date) =>
                  handleFieldChange('birthday', date ? date.toLocaleDateString('en-CA') : null)
                }
              />

              <FormSelect
                label="Giới tính"
                placeholder="Chọn giới tính"
                value={formData.gender !== null ? (formData.gender ? 1 : 0) : null}
                options={GENDER_OPTIONS}
                onSelect={(val) => handleFieldChange('gender', val === 1)}
              />
            </div>
          </FormSection>

          {/* SECTION 2: ĐỊNH DANH HỆ THỐNG & MARKETING */}
          <FormSection title="Phân Quyền & Marketing">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              <div className="lg:col-span-2 flex flex-col gap-6">
                <div className="grid grid-cols-2 gap-6">
                  <FormSelect
                    label="Loại khách hàng"
                    required
                    placeholder="Chọn loại..."
                    showSearch
                    options={typeOptions}
                    value={formData.customerTypeId}
                    error={errors.customerTypeId}
                    onSelect={(val) => handleFieldChange('customerTypeId', val)}
                  />
                  <FormSelect
                    label="Bậc xếp hạng"
                    required
                    placeholder="Chọn bậc hạng..."
                    showSearch
                    options={tierOptions}
                    value={formData.customerTierId}
                    error={errors.customerTierId}
                    onSelect={(val) => handleFieldChange('customerTierId', val)}
                  />
                </div>

                {/* UX Đỉnh Cao: Chọn Nhóm Marketing bằng Badge */}
                <div className="flex flex-col gap-3 p-4 bg-slate-50/50 rounded-2xl border border-slate-100">
                  <label className="font-bold text-[13px] text-slate-700 uppercase tracking-wide">
                    Gắn Nhóm Tiếp Thị (Tùy chọn)
                  </label>
                  <div className="flex flex-wrap gap-2.5">
                    {groupOptions.map((group) => {
                      const isActive = formData.groupIds.includes(group.value);
                      return (
                        <button
                          key={group.value}
                          type="button"
                          onClick={() => toggleGroup(group.value)}
                          className={`px-4 py-2 rounded-xl text-sm font-semibold transition-all duration-300 border ${
                            isActive
                              ? 'bg-yellow-400 text-slate-900 border-yellow-500 shadow-sm shadow-yellow-200'
                              : 'bg-white text-slate-500 border-slate-200 hover:border-yellow-400/50 hover:bg-slate-50'
                          }`}
                        >
                          {group.label}
                        </button>
                      );
                    })}
                    {groupOptions.length === 0 && (
                      <span className="text-sm text-slate-400 italic">
                        Chưa có nhóm nào trong hệ thống.
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <div className="flex flex-col gap-6">
                <FormSelect
                  label="Trạng thái tài khoản"
                  required
                  value={formData.isActive ? 1 : 0}
                  options={STATUS_OPTIONS}
                  onSelect={(val) => handleFieldChange('isActive', val === 1)}
                />
                <FormInput
                  label="Mã số thuế"
                  placeholder="VD: 5800..."
                  value={formData.taxCode || ''}
                  onChange={(e) => handleFieldChange('taxCode', e.target.value)}
                />
              </div>
            </div>
          </FormSection>

          {/* SECTION 3: ĐỊA CHỈ GIAO HÀNG (CHỈ XUẤT HIỆN KHI TẠO MỚI) */}
          {!isEditMode && (
            <FormSection title="Địa Chỉ Mặc Định (Tùy chọn)">
              <div className="p-6 rounded-2xl border border-blue-100 bg-blue-50/30">
                <div className="flex items-center gap-2 mb-4 text-blue-800">
                  <MapPin size={18} />
                  <span className="text-sm font-bold">
                    Thiết lập địa chỉ giao hàng đầu tiên cho khách
                  </span>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="lg:col-span-2">
                    <FormInput
                      label="Người nhận"
                      placeholder="Tên người nhận..."
                      value={formData.addresses![0].receiverName}
                      error={errors.address_receiverName}
                      onChange={(e) => handleAddressChange('receiverName', e.target.value)}
                    />
                  </div>
                  <div className="lg:col-span-2">
                    <FormInput
                      label="SĐT nhận hàng"
                      placeholder="09..."
                      value={formData.addresses![0].phone}
                      error={errors.address_phone}
                      onChange={(e) => handleAddressChange('phone', e.target.value)}
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

          {/* SECTION 4: GHI CHÚ */}
          <FormSection title="Ghi Chú & Khác">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormInput
                label="Link Ảnh Đại Diện (Avatar URL)"
                placeholder="https://..."
                value={formData.avatarPath || ''}
                onChange={(e) => handleFieldChange('avatarPath', e.target.value)}
              />
              <FormTextarea
                label="Ghi chú nội bộ"
                placeholder="Khách hàng khó tính, cần bọc hàng kỹ..."
                value={formData.note || ''}
                rows={1}
                onChange={(e: any) => handleFieldChange('note', e.target.value)}
              />
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

export default CustomerForm;
