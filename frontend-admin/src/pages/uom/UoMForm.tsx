import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Scale, Save, Plus } from 'lucide-react';

// API & Types
import { uomApi } from '../../api/uomApi';
import { uomCategoryApi } from '../../api/uomCategoryApi';
import { UoMPayload } from '../../types/uom';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, SubmitButton, FormTextarea 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: UoMPayload = {
    code: '',
    name: '',
    categoryId: 0,
    synonyms: '',
    isActive: true
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const UoMForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<UoMPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // Kiểm tra trùng lặp
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    const [existingNames, setExistingNames] = useState<string[]>([]);
    const [originalName, setOriginalName] = useState('');
    
    const [loading, setLoading] = useState(false);
    
    // Options
    const [categoryOptions, setCategoryOptions] = useState<{ label: string, value: number }[]>([]); 

    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Tải Category và List UoM để check duplicate
        Promise.all([
            uomCategoryApi.getAllList(),
            uomApi.getAllList()
        ]).then(([categories, uoms]) => {
            setCategoryOptions(categories.map(c => ({ label: c.name, value: c.id })));
            setExistingCodes(uoms.map(u => u.code.toLowerCase()));
            setExistingNames(uoms.map(u => u.name.toLowerCase()));
        }).catch(() => showToast('warning', 'Không tải được dữ liệu hệ thống'));

        // Tải chi tiết khi Edit
        if (isEditMode && id) {
            uomApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    categoryId: res.categoryId || 0,
                    synonyms: res.synonyms || '',
                    isActive: res.isActive ?? true
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
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof UoMPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        const trimmedCode = formData.code.trim().toLowerCase();
        const trimmedName = formData.name.trim().toLowerCase();

        if (!trimmedCode) newErrors.code = 'Vui lòng nhập mã ĐVT.';
        else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
            newErrors.code = 'Mã ĐVT đã tồn tại!';
        }

        if (!trimmedName) newErrors.name = 'Vui lòng nhập tên ĐVT.';
        else if (existingNames.includes(trimmedName) && (!isEditMode || trimmedName !== originalName)) {
            newErrors.name = 'Tên ĐVT đã tồn tại!';
        }

        if (!formData.categoryId) {
            newErrors.categoryId = 'Vui lòng chọn nhóm ĐVT.';
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
            const cleanPayload: UoMPayload = {
                ...formData,
                code: formData.code.trim().toUpperCase(),
                name: formData.name.trim(),
                synonyms: formData.synonyms?.trim() || null
            };

            if (isEditMode && id) {
                await uomApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await uomApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/uoms'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Đơn Vị Tính' : 'Thêm Đơn Vị Tính'}
                subtitle="Định nghĩa đơn vị và hỗ trợ từ khóa cho AI"
                onBack={() => navigate('/uoms')}
                icon={Scale}
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-10">
                    
                    <FormSection title="Thông Tin Cơ Bản">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <FormInput 
                                label="Mã ĐVT" required placeholder="VD: KG, BOX"
                                value={formData.code} error={errors.code} disabled={loading}
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên ĐVT" required placeholder="VD: Kilogram, Thùng"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                            <div className="md:col-span-2">
                                <FormTextarea 
                                    label="Từ đồng nghĩa (AI Keywords)" 
                                    placeholder="Cách nhau bằng dấu phẩy. VD: kg, kí, kí lô, kilogram"
                                    value={formData.synonyms || ''} rows={2}
                                    onChange={(e: any) => handleFieldChange('synonyms', e.target.value)} 
                                />
                                <p className="mt-1.5 text-[11px] text-indigo-500 italic font-medium">
                                    * Mẹo: Cung cấp càng nhiều từ đồng nghĩa thì AI Chatbot càng dễ hiểu lệnh của bạn.
                                </p>
                            </div>
                        </div>
                    </FormSection>

                    <FormSection title="Cấu Hình Hệ Thống">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <FormSelect 
                                label="Thuộc nhóm" required showSearch
                                placeholder="Chọn nhóm quy đổi..."
                                value={formData.categoryId} error={errors.categoryId}
                                options={categoryOptions}
                                onSelect={val => handleFieldChange('categoryId', val)}
                            />

                            <FormSelect 
                                label="Trạng thái hệ thống" required 
                                value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
                        </div>
                    </FormSection>

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

export default UoMForm;