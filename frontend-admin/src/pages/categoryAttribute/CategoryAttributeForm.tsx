import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Settings2, Save, Plus } from 'lucide-react';

// API & Types
import { categoryAttributeApi } from '../../api/categoryAttributeApi';
import { productCategoryApi } from '../../api/productCategoryApi';
import { attributeDefinitionApi } from '../../api/attributeDefinitionApi';
import { CategoryAttributePayload } from '../../types/categoryAttribute';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormHeader, 
    FormSection, FormSelect, SubmitButton 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: CategoryAttributePayload = {
    categoryId: 0,
    attributeDefinitionId: 0, // Mặc định 0 = Chưa chọn
    isRequired: false // Mặc định là Tùy chọn
};

const REQUIRED_OPTIONS = [
    { label: 'Bắt buộc nhập', value: 1 },
    { label: 'Tùy chọn (Không bắt buộc)', value: 0 }
];

const CategoryAttributeForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<CategoryAttributePayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // Options cho Dropdowns
    const [categoryOptions, setCategoryOptions] = useState<{ label: string, value: number }[]>([]);
    const [attributeOptions, setAttributeOptions] = useState<{ label: string, value: number }[]>([]);

    const [loading, setLoading] = useState(false);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // 1. Tải danh sách Danh mục và Thuộc tính
        Promise.all([
            productCategoryApi.getAllList(),
            attributeDefinitionApi.getAllList()
        ]).then(([categories, attributes]) => {
            setCategoryOptions([
                { label: '-- Chọn danh mục --', value: 0 },
                ...categories.map(c => ({ label: c.name, value: c.id }))
            ]);
            
            setAttributeOptions([
                { label: '-- Chọn thuộc tính --', value: 0 },
                ...attributes.map(a => ({ label: a.name, value: a.id }))
            ]);
        }).catch(() => showToast('warning', 'Không tải được danh sách lựa chọn'));

        // 2. Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            categoryAttributeApi.getById(Number(id)).then(res => {
                setFormData({
                    categoryId: res.categoryId || 0,
                    attributeDefinitionId: res.attributeDefinitionId || 0,
                    isRequired: res.isRequired
                });
            })
            .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU CẤU HÌNH'));
        }
    }, [id, isEditMode]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof CategoryAttributePayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};

        if (!formData.categoryId || formData.categoryId === 0) {
            newErrors.categoryId = 'Vui lòng chọn Danh mục sản phẩm.';
        }
        
        if (!formData.attributeDefinitionId || formData.attributeDefinitionId === 0) {
            newErrors.attributeDefinitionId = 'Vui lòng chọn Thuộc tính cần gắn.';
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
            const cleanPayload: CategoryAttributePayload = {
                categoryId: formData.categoryId,
                attributeDefinitionId: formData.attributeDefinitionId === 0 ? undefined : formData.attributeDefinitionId,
                isRequired: formData.isRequired
            };

            if (isEditMode && id) {
                await categoryAttributeApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT CẤU HÌNH THÀNH CÔNG');
            } else {
                await categoryAttributeApi.create(cleanPayload);
                showToast('success', 'GẮN THUỘC TÍNH THÀNH CÔNG');
            }
            
            // Quay về trang danh sách sau 1 giây
            setTimeout(() => navigate('/category-attributes'), 1000);
        } catch (error: any) {
            // Hứng lỗi trùng lặp từ Backend (VD: "Danh mục này đã được gắn thuộc tính ...")
            const errorMessage = error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU DỮ LIỆU';
            showToast('error', errorMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <PageContainer>
            <Toast {...toast} />

            <FormHeader 
                title={isEditMode ? 'Chỉnh Sửa Cấu Hình' : 'Gắn Thuộc Tính Mới'}
                subtitle={isEditMode ? 'Thay đổi yêu cầu nhập liệu của thuộc tính' : 'Quy định các thuộc tính cần có cho một danh mục (VD: Danh mục Thịt heo -> Cần thuộc tính Sơ chế)'}
                onBack={() => navigate('/category-attributes')}
                icon={Settings2} 
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-8">
                    
                    <FormSection title="Thiết Lập Cấu Hình">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            {/* Cột 1: Danh Mục */}
                            <FormSelect 
                                label="Danh mục sản phẩm" required showSearch
                                placeholder="-- Chọn danh mục --"
                                value={formData.categoryId} options={categoryOptions}
                                error={errors.categoryId}
                                onSelect={val => handleFieldChange('categoryId', val)}
                            />

                            {/* Cột 2: Thuộc Tính */}
                            <FormSelect 
                                label="Thuộc tính (Attribute)" required showSearch
                                placeholder="-- Chọn thuộc tính --"
                                value={formData.attributeDefinitionId || 0} options={attributeOptions}
                                error={errors.attributeDefinitionId}
                                onSelect={val => handleFieldChange('attributeDefinitionId', val)}
                            />

                            {/* Cột 3: Trạng thái bắt buộc */}
                            <FormSelect 
                                label="Yêu cầu khi tạo biến thể" required 
                                value={formData.isRequired ? 1 : 0} options={REQUIRED_OPTIONS}
                                onSelect={val => handleFieldChange('isRequired', val === 1)}
                            />
                        </div>
                        
                        {isEditMode && (
                            <div className="mt-6 px-4 py-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                                <p className="text-sm text-yellow-700 m-0">
                                    <strong className="font-bold">Lưu ý:</strong> Trong chế độ chỉnh sửa, bạn chỉ có thể thay đổi trạng thái <strong>Bắt buộc / Tùy chọn</strong>. Nếu muốn đổi sang danh mục hoặc thuộc tính khác, vui lòng xóa cấu hình này và tạo mới.
                                </p>
                            </div>
                        )}
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

export default CategoryAttributeForm;