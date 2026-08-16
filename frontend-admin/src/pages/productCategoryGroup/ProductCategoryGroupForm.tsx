import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Layers, Save, Plus } from 'lucide-react';

// API & Types
import { productCategoryGroupApi } from '../../api/productCategoryGroupApi';
import { ProductCategoryGroupPayload } from '../../types/productCategoryGroup';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, FormTextarea, SubmitButton 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: ProductCategoryGroupPayload = {
    code: '',
    name: '',
    description: '',
    imagePath: '',
    isActive: true
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const ProductCategoryGroupForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<ProductCategoryGroupPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // Kiểm tra trùng lặp mã Code
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    
    const [loading, setLoading] = useState(false);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Lấy danh sách nhóm danh mục để check trùng mã Code
        productCategoryGroupApi.getAllList()
            .then(groups => setExistingCodes(groups.map(g => g.code.toLowerCase())))
            .catch(() => showToast('warning', 'Không tải được danh sách để kiểm tra trùng lặp'));

        // Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            productCategoryGroupApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    description: res.description || '',
                    imagePath: res.imagePath || '',
                    isActive: res.isActive
                });
                setOriginalCode((res.code || '').toLowerCase());
            })
            .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
        }
    }, [id, isEditMode]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof ProductCategoryGroupPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        const trimmedCode = formData.code.trim().toLowerCase();

        // 1. Kiểm tra Mã nhóm
        if (!trimmedCode) {
            newErrors.code = 'Vui lòng nhập mã nhóm danh mục.';
        } else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
            newErrors.code = 'Mã nhóm này đã tồn tại!';
        }

        // 2. Kiểm tra Tên nhóm
        if (!formData.name.trim()) {
            newErrors.name = 'Vui lòng nhập tên nhóm danh mục.';
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
            const cleanPayload: ProductCategoryGroupPayload = {
                ...formData,
                code: formData.code.trim().toUpperCase(), // Tự động viết hoa Mã
                name: formData.name.trim(),
                description: formData.description?.trim() || undefined,
                imagePath: formData.imagePath?.trim() || undefined,
            };

            if (isEditMode && id) {
                await productCategoryGroupApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await productCategoryGroupApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/product-category-groups'), 1000);
        } catch (error: any) {
            showToast('error', error.response?.status === 400 ? 'DỮ LIỆU KHÔNG HỢP LỆ' : 'CÓ LỖI XẢY RA KHI LƯU');
        } finally {
            setLoading(false);
        }
    };

    return (
        <PageContainer>
            <Toast {...toast} />

            <FormHeader 
                title={isEditMode ? 'Chỉnh Sửa Nhóm Danh Mục' : 'Thêm Mới Nhóm Danh Mục'}
                subtitle={isEditMode ? 'Cập nhật thông tin phân loại hàng hóa cấp cao nhất' : 'Tạo mới một cấu trúc nhóm danh mục vào hệ thống'}
                onBack={() => navigate('/product-category-groups')}
                icon={Layers} // Dùng icon Layers cho chuẩn Danh mục
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-10">
                    
                    {/* SECTION 1: THÔNG TIN CƠ BẢN */}
                    <FormSection title="Thông Tin Cơ Bản">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            <FormInput 
                                label="Mã Nhóm" required placeholder="VD: DIENTU"
                                value={formData.code} error={errors.code} disabled={loading}
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên Nhóm" required placeholder="VD: Hàng Điện Tử"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                            <FormSelect 
                                label="Trạng thái hiển thị" required 
                                value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
                        </div>
                    </FormSection>

                    {/* SECTION 2: HÌNH ẢNH & MÔ TẢ */}
                    <FormSection title="Hình Ảnh & Mô Tả">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="flex flex-col gap-6">
                                <FormInput 
                                    label="Link Ảnh Đại Diện (URL)" placeholder="https://..."
                                    value={formData.imagePath || ''} disabled={loading}
                                    onChange={e => handleFieldChange('imagePath', e.target.value)} 
                                />
                                {/* Preview Ảnh (Optional - UX Plus) */}
                                {formData.imagePath && (
                                    <div className="p-3 border border-slate-200 rounded-xl bg-slate-50 w-max">
                                        <img 
                                            src={formData.imagePath} 
                                            alt="Preview" 
                                            className="w-24 h-24 object-cover rounded-lg shadow-sm border border-slate-200"
                                            onError={(e: any) => e.target.src = 'https://placehold.co/100x100?text=Lỗi+Ảnh'}
                                        />
                                    </div>
                                )}
                            </div>
                            
                            <FormTextarea 
                                label="Mô tả nhóm danh mục" placeholder="Nhập chi tiết mô tả..." 
                                value={formData.description || ''} rows={4} 
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

export default ProductCategoryGroupForm;