import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Shield, Save, Plus, Info, CheckSquare, ChevronRight } from 'lucide-react';

// API & Types
import { roleApi } from '../../api/roleApi';
import { permissionApi } from '../../api/permissionApi';
import { RolePayload } from '../../types/role';

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
import { TabGroup, TabButton } from '../../components/commons/TabUI';
import { MatrixUI, PermissionDef } from '../../components/commons/MatrixUI';

const INITIAL_STATE: RolePayload = {
  code: '',
  name: '',
  description: '',
  isActive: true,
  permissionIds: [],
};

const STATUS_OPTIONS = [
  { label: 'Hoạt động', value: 1 },
  { label: 'Tạm khóa', value: 0 },
];

type TabType = 'info' | 'permissions';

const RoleForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<RolePayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [loading, setLoading] = useState(false);

  // --- TABS & PERMISSIONS ---
  const [activeTab, setActiveTab] = useState<TabType>('info');
  const [allPermissions, setAllPermissions] = useState<PermissionDef[]>([]);

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
    // Tải danh sách toàn bộ quyền từ Backend để nhét vào MatrixUI
    permissionApi
      .getAll()
      .then((res) => setAllPermissions(res))
      .catch(() => showToast('warning', 'Không tải được danh mục phân quyền.'));

    // Lấy chi tiết Edit
    if (isEditMode && id) {
      roleApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            description: res.description || '',
            isActive: res.isActive,
            permissionIds: res.permissionIds || [],
          });
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU VAI TRÒ'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof RolePayload, value: any) => {
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
    const newErrors: Record<string, string> = {};

    if (!isEditMode && !formData.code?.trim()) newErrors.code = 'Vui lòng nhập Mã Vai Trò.';
    if (!formData.name.trim()) newErrors.name = 'Vui lòng nhập Tên Vai Trò.';

    setErrors(newErrors);

    if (Object.keys(newErrors).length > 0) {
      setActiveTab('info');
    }

    return Object.keys(newErrors).length === 0;
  };

  // --- SUBMIT ---
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại các trường báo đỏ!');

    setLoading(true);
    try {
      const cleanPayload: RolePayload = {
        ...formData,
        code: formData.code?.trim().toUpperCase(),
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
      };

      if (isEditMode && id) {
        await roleApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT VAI TRÒ THÀNH CÔNG');
      } else {
        await roleApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI VAI TRÒ THÀNH CÔNG');
      }
      setTimeout(() => navigate('/roles'), 1000);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageContainer>
      <Toast {...toast} />

      <FormHeader
        title={isEditMode ? 'Chỉnh Sửa & Phân Quyền' : 'Tạo Mới Vai Trò'}
        subtitle="Cấu hình thông tin vai trò và ma trận quyền hạn mặc định"
        onBack={() => navigate('/roles')}
        icon={Shield}
      />

      {/* 🔥 TABS ĐIỀU HƯỚNG */}
      <div className="mb-5 flex items-center justify-between">
        <TabGroup>
          <TabButton
            active={activeTab === 'info'}
            onClick={() => setActiveTab('info')}
            label="1. THÔNG TIN CƠ BẢN"
            icon={Info}
          />
          <TabButton
            active={activeTab === 'permissions'}
            onClick={() => setActiveTab('permissions')}
            label={`2. MA TRẬN PHÂN QUYỀN (${formData.permissionIds.length})`}
            icon={CheckSquare}
          />
        </TabGroup>
      </div>

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-6">
          {/* ================= TAB 1: THÔNG TIN ================= */}
          <div
            className={
              activeTab === 'info'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <FormSection title="Định Danh Vai Trò">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <FormInput
                  label="Mã Vai Trò (Định danh)"
                  required
                  placeholder="VD: SALE_MANAGER"
                  value={formData.code || ''}
                  error={errors.code}
                  disabled={isEditMode || loading}
                  onChange={(e) => handleFieldChange('code', e.target.value)}
                />
                <FormInput
                  label="Tên Vai Trò"
                  required
                  placeholder="VD: Trưởng phòng Kinh Doanh"
                  value={formData.name}
                  error={errors.name}
                  disabled={loading}
                  onChange={(e) => handleFieldChange('name', e.target.value)}
                />
                <div className="md:col-span-2">
                  <FormSelect
                    label="Trạng thái"
                    required
                    value={formData.isActive ? 1 : 0}
                    options={STATUS_OPTIONS}
                    onSelect={(val) => handleFieldChange('isActive', val === 1)}
                  />
                </div>
                <div className="md:col-span-2">
                  <FormTextarea
                    label="Mô tả Vai trò"
                    placeholder="Mô tả chức năng nhiệm vụ của vai trò này..."
                    value={formData.description || ''}
                    rows={3}
                    onChange={(e: any) => handleFieldChange('description', e.target.value)}
                  />
                </div>
              </div>
            </FormSection>
          </div>

          {/* ================= TAB 2: MA TRẬN QUYỀN ================= */}
          <div
            className={
              activeTab === 'permissions'
                ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300'
                : 'hidden'
            }
          >
            <FormSection title="Phân Quyền Hệ Thống">
              <p className="text-sm text-slate-500 mb-6 -mt-2">
                Tích chọn các quyền mà vai trò này được phép thao tác. Các User thuộc vai trò này sẽ
                tự động thừa hưởng toàn bộ quyền được cấp dưới đây.
              </p>

              {/* 🔥 GỌI MATRIX UI */}
              <MatrixUI
                allPermissions={allPermissions}
                selectedIds={formData.permissionIds}
                onChange={(newIds) => handleFieldChange('permissionIds', newIds)}
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
                onClick={() => setActiveTab('info')}
                className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors"
              >
                Lùi lại bước trước
              </button>
            )}

            {activeTab !== 'permissions' ? (
              <button
                type="button"
                onClick={() => setActiveTab('permissions')}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-800 bg-amber-400 hover:bg-amber-500 transition-colors shadow-sm shadow-amber-200"
              >
                Cấp quyền hạn <ChevronRight size={18} />
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

export default RoleForm;
