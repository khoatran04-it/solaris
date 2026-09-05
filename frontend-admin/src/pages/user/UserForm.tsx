import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  UserPlus,
  Save,
  Plus,
  Info,
  Shield,
  KeyRound,
  ChevronRight,
  CheckSquare,
  Warehouse as WarehouseIcon,
} from 'lucide-react';

// API & Types
import { userApi } from '../../api/userApi';
import { roleApi } from '../../api/roleApi';
import { permissionApi } from '../../api/permissionApi';
import { warehouseApi } from '../../api/warehouseApi';
import { UserPayload, CustomPermission } from '../../types/user';
import { Role } from '../../types/role';
import { Warehouse } from '../../types/warehouse';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import {
  PageContainer,
  FormCard,
  FormInput,
  FormHeader,
  FormSection,
  FormSelect,
  SubmitButton,
} from '../../components/commons/FormUI';
import { TabGroup, TabButton } from '../../components/commons/TabUI';
import { MatrixUI, PermissionDef } from '../../components/commons/MatrixUI';

const INITIAL_STATE: UserPayload = {
  citizenId: '',
  username: '',
  password: '',
  fullName: '',
  email: '',
  phoneNumber: '',
  avatarUrl: '',
  isActive: true,
  roleIds: [],
  warehouseIds: [], // Mảng lưu trữ ID Kho hàng
  customPermissions: [],
};

const STATUS_OPTIONS = [
  { label: 'Đang hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

type TabType = 'info' | 'roles' | 'permissions';

const UserForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<UserPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [loading, setLoading] = useState(false);

  // --- TABS & MASTER DATA ---
  const [activeTab, setActiveTab] = useState<TabType>('info');
  const [allRoles, setAllRoles] = useState<Role[]>([]);
  const [allPermissions, setAllPermissions] = useState<PermissionDef[]>([]);
  const [allWarehouses, setAllWarehouses] = useState<Warehouse[]>([]); // State lưu danh sách Kho

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
    // Tải Master Data (Vai trò + Danh mục Quyền + Kho hàng)
    Promise.all([
      roleApi.getAllList(),
      permissionApi.getAll(),
      warehouseApi.getAllList(), // Gọi API lấy danh sách Kho
    ])
      .then(([roles, permissions, warehouses]) => {
        setAllRoles(roles.filter((r) => r.isActive));
        setAllPermissions(permissions);
        setAllWarehouses(warehouses.filter((w) => w.isActive)); // Chỉ lấy kho đang hoạt động
      })
      .catch(() => showToast('warning', 'Không tải được dữ liệu bổ trợ hệ thống'));

    // Lấy chi tiết Edit
    if (isEditMode && id) {
      userApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            citizenId: res.citizenId || '',
            username: res.username || '',
            fullName: res.fullName || '',
            email: res.email || '',
            phoneNumber: res.phoneNumber || '',
            avatarUrl: res.avatarUrl || '',
            isActive: res.isActive,
            roleIds: res.roleIds || [],
            warehouseIds: res.warehouseIds || [],
            customPermissions: res.customPermissions || [],
          });
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU NHÂN VIÊN'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof UserPayload, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const toggleRole = (roleId: number) => {
    setFormData((prev) => {
      const newRoles = prev.roleIds.includes(roleId)
        ? prev.roleIds.filter((id) => id !== roleId)
        : [...prev.roleIds, roleId];
      return { ...prev, roleIds: newRoles };
    });
  };

  const toggleWarehouse = (warehouseId: number) => {
    setFormData((prev) => {
      const newWarehouses = prev.warehouseIds.includes(warehouseId)
        ? prev.warehouseIds.filter((id) => id !== warehouseId)
        : [...prev.warehouseIds, warehouseId];
      return { ...prev, warehouseIds: newWarehouses };
    });
  };

  // Helper map mảng `customPermissions` ra mảng số nguyên `number[]` cho MatrixUI dùng
  const getCustomPermissionIds = () => {
    return formData.customPermissions.filter((p) => p.isGranted).map((p) => p.permissionId);
  };

  // Helper map mảng số nguyên từ MatrixUI ngược lại thành mảng `CustomPermission[]`
  const handleCustomPermissionChange = (newIds: number[]) => {
    const mapped: CustomPermission[] = newIds.map((id) => ({ permissionId: id, isGranted: true }));
    handleFieldChange('customPermissions', mapped);
  };

  // --- VALIDATION ---
  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.fullName.trim()) newErrors.fullName = 'Vui lòng nhập Họ tên.';
    if (!formData.citizenId.trim()) newErrors.citizenId = 'Vui lòng nhập CCCD.';
    if (!formData.email.trim()) newErrors.email = 'Vui lòng nhập Email.';

    if (!isEditMode) {
      if (!formData.username?.trim()) newErrors.username = 'Vui lòng nhập Username.';
      if (!formData.password?.trim()) newErrors.password = 'Vui lòng cấp mật khẩu khởi tạo.';
    }

    setErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      setActiveTab('info');
    }

    return Object.keys(newErrors).length === 0;
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm())
      return showToast('warning', 'Vui lòng kiểm tra lại các thông tin bắt buộc!');

    setLoading(true);
    try {
      const cleanPayload: UserPayload = {
        ...formData,
        citizenId: formData.citizenId.trim(),
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim(),
      };

      // Nếu Edit thì không gửi password và username
      if (isEditMode) {
        delete cleanPayload.password;
        delete cleanPayload.username;
      } else {
        cleanPayload.username = formData.username?.trim();
        cleanPayload.password = formData.password?.trim();
      }

      if (isEditMode && id) {
        await userApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT HỒ SƠ NHÂN VIÊN THÀNH CÔNG');
      } else {
        await userApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI NHÂN VIÊN THÀNH CÔNG');
      }
      setTimeout(() => navigate('/users'), 1000);
    } catch (error: any) {
      const serverMsg = error.response?.data?.message || error.message || 'CÓ LỖI XẢY RA KHI LƯU';
      showToast('error', serverMsg);

      const fieldErrors: Record<string, string> = {};
      if (serverMsg.includes('Email')) fieldErrors.email = serverMsg;
      if (serverMsg.includes('Tên đăng nhập') || serverMsg.includes('Username'))
        fieldErrors.username = serverMsg;
      if (serverMsg.includes('CCCD')) fieldErrors.citizenId = serverMsg;
      if (serverMsg.includes('Số điện thoại') || serverMsg.includes('SĐT'))
        fieldErrors.phoneNumber = serverMsg;

      if (Object.keys(fieldErrors).length > 0) {
        setErrors((prev) => ({ ...prev, ...fieldErrors }));
        setActiveTab('info');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Cập Nhật Hồ Sơ Nhân Viên' : 'Tiếp Nhận Nhân Viên Mới'}
        subtitle="Quản lý thông tin cá nhân, tài khoản hệ thống và thiết lập quyền hạn"
        onBack={() => navigate('/users')}
        icon={UserPlus}
      />

      {/* 🔥 TABS ĐIỀU HƯỚNG 3 BƯỚC */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={activeTab === 'info'}
            onClick={() => setActiveTab('info')}
            label="1. THÔNG TIN CÁ NHÂN"
            icon={Info}
          />
          <TabButton
            active={activeTab === 'roles'}
            onClick={() => setActiveTab('roles')}
            label={`2. VAI TRÒ & KHO (${formData.roleIds.length + formData.warehouseIds.length})`}
            icon={Shield}
          />
          <TabButton
            active={activeTab === 'permissions'}
            onClick={() => setActiveTab('permissions')}
            label={`3. QUYỀN NGOẠI LỆ (${getCustomPermissionIds().length})`}
            icon={KeyRound}
          />
        </TabGroup>
      </div>

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          {/* ================= TAB 1: THÔNG TIN CÁ NHÂN ================= */}
          <div
            className={
              activeTab === 'info'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <div className="flex flex-col gap-8">
              {/* SECTION: TÀI KHOẢN HỆ THỐNG */}
              <FormSection title="Tài Khoản Đăng Nhập">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 p-5 bg-slate-50/50 border border-slate-100 rounded-xl">
                  <FormInput
                    label="Tên đăng nhập (Username)"
                    required
                    placeholder="VD: nguyenvan_a"
                    value={formData.username || ''}
                    error={errors.username}
                    disabled={isEditMode || loading}
                    onChange={(e) => handleFieldChange('username', e.target.value)}
                  />

                  {!isEditMode && (
                    <FormInput
                      label="Mật khẩu khởi tạo"
                      required
                      type="text"
                      placeholder="Nhập mật khẩu..."
                      value={formData.password || ''}
                      error={errors.password}
                      disabled={loading}
                      onChange={(e) => handleFieldChange('password', e.target.value)}
                    />
                  )}

                  <div className={isEditMode ? 'md:col-span-1' : 'md:col-span-2'}>
                    <FormSelect
                      label="Trạng thái hoạt động"
                      required
                      value={formData.isActive ? 1 : 0}
                      options={STATUS_OPTIONS}
                      onSelect={(val) => handleFieldChange('isActive', val === 1)}
                    />
                  </div>
                </div>
              </FormSection>

              {/* SECTION: HỒ SƠ NHÂN SỰ */}
              <FormSection title="Hồ Sơ Nhân Sự">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <FormInput
                    label="Họ và Tên"
                    required
                    placeholder="VD: Nguyễn Văn A"
                    value={formData.fullName}
                    error={errors.fullName}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('fullName', e.target.value)}
                  />
                  <FormInput
                    label="Số CCCD / CMND"
                    required
                    placeholder="Nhập 12 số CCCD..."
                    value={formData.citizenId}
                    error={errors.citizenId}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('citizenId', e.target.value)}
                  />
                  <FormInput
                    label="Email liên hệ"
                    required
                    placeholder="Email công ty hoặc cá nhân..."
                    value={formData.email}
                    error={errors.email}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('email', e.target.value)}
                  />
                  <FormInput
                    label="Số điện thoại"
                    placeholder="Nhập số điện thoại..."
                    value={formData.phoneNumber}
                    error={errors.phoneNumber}
                    disabled={loading}
                    onChange={(e) => handleFieldChange('phoneNumber', e.target.value)}
                  />
                </div>
              </FormSection>
            </div>
          </div>

          {/* ================= TAB 2: VAI TRÒ & KHO ================= */}
          <div
            className={
              activeTab === 'roles'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <div className="flex flex-col gap-10">
              {/* PHẦN 1: GÁN VAI TRÒ */}
              <FormSection title="Cấp Phát Vai Trò (Role)">
                <p className="text-sm text-slate-500 mb-6 -mt-2">
                  Chọn một hoặc nhiều vai trò. Nhân viên sẽ thừa hưởng toàn bộ quyền của các vai trò
                  được chọn.
                </p>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {allRoles.map((role) => {
                    const isChecked = formData.roleIds.includes(role.id);
                    return (
                      <div
                        key={role.id}
                        onClick={() => toggleRole(role.id)}
                        className={`flex flex-col gap-1 p-4 rounded-xl border-2 cursor-pointer transition-all duration-200 
                                                ${
                                                  isChecked
                                                    ? 'border-indigo-500 bg-indigo-50/40 shadow-[0_2px_10px_rgba(99,102,241,0.15)]'
                                                    : 'border-slate-200 bg-white hover:border-indigo-300 hover:bg-indigo-50/20'
                                                }`}
                      >
                        <div className="flex items-start justify-between">
                          <span
                            className={`font-bold text-[14px] ${isChecked ? 'text-indigo-800' : 'text-slate-700'}`}
                          >
                            {role.name}
                          </span>
                          <div
                            className={`shrink-0 transition-transform ${isChecked ? 'scale-110 text-indigo-600' : 'text-slate-300'}`}
                          >
                            {isChecked ? (
                              <CheckSquare size={20} />
                            ) : (
                              <div className="w-5 h-5 border-2 border-slate-300 rounded-md" />
                            )}
                          </div>
                        </div>
                        <span className="text-[12px] text-slate-400 truncate mt-1">
                          Mã: {role.code}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </FormSection>

              {/* PHẦN 2: CẤP QUYỀN TRUY CẬP KHO (DATA-LEVEL SECURITY) */}
              <FormSection title="Phân Quyền Truy Cập Kho (Data-level Security)">
                <p className="text-sm text-slate-500 mb-6 -mt-2">
                  Nhân viên này được phép xem và thao tác dữ liệu (Phiếu nhập, xuất...) ở những kho
                  nào?
                </p>
                <div className="flex flex-wrap gap-3">
                  {allWarehouses.length === 0 ? (
                    <span className="text-sm italic text-slate-400">
                      Chưa có kho hàng nào trong hệ thống.
                    </span>
                  ) : (
                    allWarehouses.map((warehouse) => {
                      const isChecked = formData.warehouseIds.includes(warehouse.id);
                      return (
                        <button
                          key={warehouse.id}
                          type="button"
                          onClick={() => toggleWarehouse(warehouse.id)}
                          className={`flex items-center gap-2.5 px-4 py-2.5 rounded-lg border-2 transition-all duration-200
                                                        ${
                                                          isChecked
                                                            ? 'border-emerald-500 bg-emerald-50 text-emerald-800 shadow-sm'
                                                            : 'border-slate-200 bg-white text-slate-600 hover:border-emerald-300'
                                                        }`}
                        >
                          <WarehouseIcon
                            size={18}
                            className={isChecked ? 'text-emerald-600' : 'text-slate-400'}
                          />
                          <span className="text-[13px] font-bold">{warehouse.name}</span>
                          <div
                            className={`ml-2 w-4 h-4 rounded border flex items-center justify-center transition-colors
                                                        ${isChecked ? 'bg-emerald-500 border-emerald-500' : 'border-slate-300 bg-slate-50'}`}
                          >
                            {isChecked && <CheckSquare size={12} className="text-white" />}
                          </div>
                        </button>
                      );
                    })
                  )}
                </div>
              </FormSection>
            </div>
          </div>

          {/* ================= TAB 3: QUYỀN NGOẠI LỆ ================= */}
          <div
            className={
              activeTab === 'permissions'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <FormSection title="Tặng Thêm Quyền Ngoại Lệ">
              <div className="p-4 mb-6 bg-amber-50 border border-amber-200 rounded-xl flex items-start gap-3">
                <Info className="text-amber-500 shrink-0 mt-0.5" size={20} />
                <p className="text-sm text-amber-800 leading-relaxed">
                  <strong>Tính năng nâng cao:</strong> Bạn chỉ nên cấp quyền tại đây nếu muốn tặng
                  thêm 1 vài quyền lặt vặt cho nhân viên này{' '}
                  <strong>mà không cần phải tạo ra một Vai trò (Role) mới</strong>. <br />
                  <em>Lưu ý: Các quyền chọn ở đây sẽ được cộng dồn với các quyền từ Tab 2.</em>
                </p>
              </div>

              <MatrixUI
                allPermissions={allPermissions}
                selectedIds={getCustomPermissionIds()}
                onChange={handleCustomPermissionChange}
              />
            </FormSection>
          </div>

          {/* ================= FOOTER BUTTONS ================= */}
          <div className="flex justify-between items-center pt-6 border-t border-slate-100 mt-2">
            {activeTab === 'info' ? (
              <div />
            ) : (
              <button
                type="button"
                onClick={() => setActiveTab(activeTab === 'permissions' ? 'roles' : 'info')}
                className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors"
              >
                Lùi lại bước trước
              </button>
            )}

            {activeTab !== 'permissions' ? (
              <button
                type="button"
                onClick={() => setActiveTab(activeTab === 'info' ? 'roles' : 'permissions')}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-800 bg-amber-400 hover:bg-amber-500 transition-colors shadow-sm shadow-amber-200"
              >
                {activeTab === 'info' ? 'Cấp quyền & Kho' : 'Cấu hình ngoại lệ'}{' '}
                <ChevronRight size={18} />
              </button>
            ) : (
              <SubmitButton
                loading={loading}
                isEditMode={isEditMode}
                icon={isEditMode ? Save : Plus}
              />
            )}
          </div>
        </form>
      </FormCard>
    </PageContainer>
  );
};

export default UserForm;
