import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Hexagon, Save, Plus } from 'lucide-react';

// API & Types
import { customerTierApi } from '../../api/customerTierApi';
import { CustomerTierPayload } from '../../types/customerTier';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormHeader, FormInput, 
    SubmitButton, FormSection, FormSelect
} from '../../components/commons/FormUI';

// Cấu hình giá trị khởi tạo
const INITIAL_STATE: CustomerTierPayload = {
    code: '',
    name: '',
    discountPercent: 0,
    minSpending: 0,
    isActive: true
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const CustomerTierForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<CustomerTierPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<keyof CustomerTierPayload, string>>>({});
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    const [existingNames, setExistingNames] = useState<string[]>([]);
    const [originalName, setOriginalName] = useState('');
    const [loading, setLoading] = useState(false);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof CustomerTierPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => {
                const newErrors = { ...prev };
                delete newErrors[field];
                return newErrors;
            });
        }
    };

    // --- EFFECTS ---
    useEffect(() => {
        customerTierApi.getAllList()
            .then(res => {
                setExistingCodes(res.map(item => item.code.toLowerCase()));
                setExistingNames(res.map(item => item.name.toLowerCase()));
            })
            .catch(() => console.error("Không tải được danh sách kiểm tra trùng lặp"));

        if (isEditMode && id) {
            customerTierApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    discountPercent: res.discountPercent || 0,
                    minSpending: res.minSpending || 0,
                    isActive: res.isActive
                });
                setOriginalCode((res.code || '').toLowerCase());
                setOriginalName((res.name || '').toLowerCase());
            }).catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
        }
    }, [id, isEditMode]);

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Partial<Record<keyof CustomerTierPayload, string>> = {};
        const trimmedCode = formData.code.trim().toLowerCase();
        const trimmedName = formData.name.trim().toLowerCase();

        if (!trimmedCode) {
            newErrors.code = 'Vui lòng nhập mã định danh.';
        } else if (trimmedCode.length < 2) {
            newErrors.code = 'Mã phải chứa ít nhất 2 ký tự.';
        } else {
            const isDuplicate = existingCodes.includes(trimmedCode);
            const isSelf = isEditMode && trimmedCode === originalCode;
            if (isDuplicate && !isSelf) newErrors.code = 'Mã định danh đã tồn tại!';
        }

        if (!trimmedName) {
            newErrors.name = 'Vui lòng nhập tên phân bậc.';
        } else if (trimmedName.length < 2) {
            newErrors.name = 'Tên phải chứa ít nhất 2 ký tự.';
        } else {
            const isDuplicate = existingNames.includes(trimmedName);
            const isSelf = isEditMode && trimmedName === originalName;
            if (isDuplicate && !isSelf) newErrors.name = 'Tên phân bậc đã tồn tại!';
        }

        if (formData.discountPercent < 0 || formData.discountPercent > 100) {
            newErrors.discountPercent = 'Chiết khấu phải từ 0% đến 100%.';
        }

        if (formData.minSpending < 0) {
            newErrors.minSpending = 'Chi tiêu tối thiểu không được âm.';
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
            const cleanPayload: CustomerTierPayload = {
                code: formData.code.trim().toUpperCase(),
                name: formData.name.trim(),
                discountPercent: Number(formData.discountPercent),
                minSpending: Number(formData.minSpending),
                isActive: Boolean(formData.isActive)
            };

            if (isEditMode && id) {
                await customerTierApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await customerTierApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/customer-tiers'), 1000);
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
                icon={Hexagon} 
                title={isEditMode ? 'Chỉnh Sửa Phân Bậc Khách Hàng' : 'Thêm Phân Bậc Khách Hàng'}
                subtitle={isEditMode ? 'Cập nhật thông tin chính sách bậc khách hàng' : 'Thiết lập chính sách bậc khách hàng và chiết khấu mới'}
                onBack={() => navigate('/customer-tiers')}
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-8">
                    <FormSection title="Thông Tin Cơ Bản">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6">
                            <FormInput 
                                label="Mã phân bậc" required placeholder="VANG, BAC, KIM CUONG..."
                                value={formData.code} error={errors.code} disabled={loading}
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên phân bậc" required placeholder="Ví dụ: Khách hàng Vàng"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                        </div>
                    </FormSection>

                    <FormSection title="Cấu Hình Chiết Khấu & Trạng Thái">
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-x-8 gap-y-6">
                            <FormInput 
                                label="Chiết khấu (%)" required type="number" step="0.1" min="0" max="100"
                                value={formData.discountPercent} error={errors.discountPercent} disabled={loading}
                                onChange={e => handleFieldChange('discountPercent', e.target.value)} 
                            />
                            <FormInput 
                                label="Chi tiêu tối thiểu (VNĐ)" required type="number" min="0"
                                value={formData.minSpending} error={errors.minSpending} disabled={loading}
                                onChange={e => handleFieldChange('minSpending', e.target.value)} 
                            />
                            <FormSelect 
                                label="Trạng thái phân bậc" required 
                                value={formData.isActive ? 1 : 0} 
                                options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
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

export default CustomerTierForm;