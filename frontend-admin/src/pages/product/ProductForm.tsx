import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Package, Save, Plus } from 'lucide-react';

// API & Types
import { productApi } from '../../api/productApi';
import { productCategoryApi } from '../../api/productCategoryApi';
import { uomApi } from '../../api/uomApi';
import { ProductPayload } from '../../types/product';

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

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: ProductPayload = {
  code: '',
  name: '',
  description: '',
  imagePath: '',
  categoryId: 0,
  baseUoMId: 0,
  isActive: true,
};

const STATUS_OPTIONS = [
  { label: 'Đang bán', value: 1 },
  { label: 'Ngừng bán', value: 0 },
];

const ProductForm: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);

  // --- STATES ---
  const [formData, setFormData] = useState<ProductPayload>(INITIAL_STATE);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});

  // Kiểm tra trùng lặp mã SKU
  const [existingCodes, setExistingCodes] = useState<string[]>([]);
  const [originalCode, setOriginalCode] = useState('');

  // Options cho Dropdowns
  const [categoryOptions, setCategoryOptions] = useState<{ label: string; value: number }[]>([]);
  const [uomOptions, setUomOptions] = useState<{ label: string; value: number }[]>([]);

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
    // Tải danh sách Danh mục, UoM & Danh sách Product (để check trùng mã)
    Promise.all([productCategoryApi.getAllList(), uomApi.getAllList(), productApi.getAllList()])
      .then(([categories, uoms, products]) => {
        setCategoryOptions([
          { label: '-- Chưa phân loại --', value: 0 },
          ...categories.map((c) => ({ label: c.name, value: c.id })),
        ]);

        setUomOptions([
          { label: '-- Chọn đơn vị tính --', value: 0 },
          ...uoms.map((u) => ({ label: `${u.name} (${u.code})`, value: u.id })),
        ]);

        setExistingCodes(products.map((p) => p.code.toLowerCase()));
      })
      .catch(() => showToast('warning', 'Không tải được danh sách tùy chọn'));

    // Tải dữ liệu chi tiết khi Edit
    if (isEditMode && id) {
      productApi
        .getById(Number(id))
        .then((res) => {
          setFormData({
            code: res.code || '',
            name: res.name || '',
            description: res.description || '',
            imagePath: res.imagePath || '',
            categoryId: res.categoryId || 0,
            baseUoMId: res.baseUoMId || 0,
            isActive: res.isActive,
          });
          setOriginalCode((res.code || '').toLowerCase());
        })
        .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU SẢN PHẨM'));
    }
  }, [id, isEditMode]);

  // --- HELPERS ---
  const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleFieldChange = (field: keyof ProductPayload, value: any) => {
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
    const trimmedCode = formData.code.trim().toLowerCase();

    // 1. Kiểm tra Mã sản phẩm (SKU Gốc)
    if (!trimmedCode) {
      newErrors.code = 'Vui lòng nhập mã sản phẩm.';
    } else if (
      existingCodes.includes(trimmedCode) &&
      (!isEditMode || trimmedCode !== originalCode)
    ) {
      newErrors.code = 'Mã sản phẩm này đã tồn tại!';
    }

    // 2. Kiểm tra Tên sản phẩm
    if (!formData.name.trim()) {
      newErrors.name = 'Vui lòng nhập tên sản phẩm.';
    }

    // 3. Kiểm tra Đơn vị tính (Bắt buộc)
    if (!formData.baseUoMId || formData.baseUoMId === 0) {
      newErrors.baseUoMId = 'Vui lòng chọn đơn vị tính cơ bản.';
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
      const cleanPayload: ProductPayload = {
        code: formData.code.trim().toUpperCase(),
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
        imagePath: formData.imagePath?.trim() || undefined,
        categoryId: formData.categoryId === 0 ? undefined : formData.categoryId,
        baseUoMId: formData.baseUoMId,
        isActive: Boolean(formData.isActive),
      };

      if (isEditMode && id) {
        await productApi.update(Number(id), cleanPayload);
        showToast('success', 'CẬP NHẬT THÀNH CÔNG');
      } else {
        await productApi.create(cleanPayload);
        showToast('success', 'THÊM MỚI THÀNH CÔNG');
      }

      setTimeout(() => navigate('/products'), 1000);
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
        title={isEditMode ? 'Chỉnh Sửa Sản Phẩm Gốc' : 'Thêm Mới Sản Phẩm'}
        subtitle={
          isEditMode
            ? 'Cập nhật thông tin gốc của hàng hóa trước khi cấu hình biến thể'
            : 'Tạo mới một hồ sơ sản phẩm (Parent Product) vào hệ thống'
        }
        onBack={() => navigate('/products')}
        icon={Package}
      />

      <FormCard>
        <form onSubmit={handleSubmit} className="flex flex-col gap-10">
          {/* SECTION 1: THÔNG TIN CƠ BẢN */}
          <FormSection title="Định Danh & Phân Loại">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              <FormInput
                label="Mã Sản Phẩm (SKU)"
                required
                placeholder="VD: IPHONE-15"
                value={formData.code}
                error={errors.code}
                disabled={loading}
                onChange={(e) => handleFieldChange('code', e.target.value)}
              />
              <FormInput
                label="Tên Sản Phẩm"
                required
                placeholder="VD: Apple iPhone 15 Pro Max"
                value={formData.name}
                error={errors.name}
                disabled={loading}
                onChange={(e) => handleFieldChange('name', e.target.value)}
              />
              <FormSelect
                label="Trạng thái hiển thị"
                required
                value={formData.isActive ? 1 : 0}
                options={STATUS_OPTIONS}
                onSelect={(val) => handleFieldChange('isActive', val === 1)}
              />
              <FormSelect
                label="Danh mục sản phẩm"
                placeholder="Chọn danh mục..."
                showSearch
                value={formData.categoryId || 0}
                options={categoryOptions}
                onSelect={(val) => handleFieldChange('categoryId', val)}
              />
              <FormSelect
                label="Đơn vị tính cơ bản"
                required
                placeholder="VD: Cái, Hộp, Chiếc..."
                showSearch
                value={formData.baseUoMId}
                options={uomOptions}
                error={errors.baseUoMId}
                onSelect={(val) => handleFieldChange('baseUoMId', val)}
              />
            </div>
          </FormSection>

          {/* SECTION 2: HÌNH ẢNH & MÔ TẢ */}
          <FormSection title="Hình Ảnh & Mô TẢ">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="flex flex-col gap-6">
                <FormInput
                  label="Link Ảnh Sản Phẩm (URL)"
                  placeholder="https://..."
                  value={formData.imagePath || ''}
                  disabled={loading}
                  onChange={(e) => handleFieldChange('imagePath', e.target.value)}
                />
                {formData.imagePath && (
                  <div className="p-3 border border-slate-200 rounded-xl bg-slate-50 w-max">
                    <img
                      src={formData.imagePath}
                      alt="Preview"
                      className="w-32 h-32 object-contain rounded-lg shadow-sm border border-slate-200 bg-white"
                      onError={(e: any) =>
                        (e.target.src = 'https://placehold.co/128x128?text=Lỗi+Ảnh')
                      }
                    />
                  </div>
                )}
              </div>

              <FormTextarea
                label="Mô tả sản phẩm"
                placeholder="Nhập chi tiết về sản phẩm này..."
                value={formData.description || ''}
                rows={5}
                onChange={(e: any) => handleFieldChange('description', e.target.value)}
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

export default ProductForm;
