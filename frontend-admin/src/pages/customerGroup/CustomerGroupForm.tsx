import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Hexagon, Save, Plus } from 'lucide-react';

// API & Types
import { customerGroupApi } from '../../api/customerGroupApi';
import { CustomerGroupPayload } from '../../types/customerGroup';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormHeader, FormInput, 
    FormTextarea, SubmitButton, FormSelect, FormSection
} from '../../components/commons/FormUI';

// Cấu hình giá trị khởi tạo
const INITIAL_STATE: CustomerGroupPayload = {
    code: '',
    name: '',
    description: '',
    isActive: true
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Ngừng hoạt động', value: 0 }
];

const CustomerGroupForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<CustomerGroupPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<keyof CustomerGroupPayload, string>>>({});
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    const [existingNames, setExistingNames] = useState<string[]>([]);
    const [originalName, setOriginalName] = useState('');
    const [loading, setLoading] = useState(false);
    
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Tối ưu: Chỉ gọi API 1 lần để lấy cả mảng code và name kiểm tra trùng lặp
        customerGroupApi.getAllList()
            .then(res => {
                setExistingCodes(res.map(item => item.code.toLowerCase()));
                setExistingNames(res.map(item => item.name.toLowerCase()));
            })
            .catch(() => console.error("Không tải được danh sách kiểm tra trùng lặp"));
    
        // Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            customerGroupApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    description: res.description || '',
                    isActive: res.isActive,
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

    const handleFieldChange = (field: keyof CustomerGroupPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => {
                const newErrors = { ...prev };
                delete newErrors[field];
                return newErrors;
            });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Partial<Record<keyof CustomerGroupPayload, string>> = {};
        const trimmedCode = formData.code.trim().toLowerCase();
        const trimmedName = formData.name.trim().toLowerCase();

        // Kiểm tra Mã
        if (!trimmedCode) {
            newErrors.code = 'Vui lòng nhập mã định danh.';
        } else if (trimmedCode.length < 2) {
            newErrors.code = 'Mã phải chứa ít nhất 2 ký tự.';
        } else {
            const isDuplicate = existingCodes.includes(trimmedCode);
            const isSelf = isEditMode && trimmedCode === originalCode;
            if (isDuplicate && !isSelf) newErrors.code = 'Mã định danh đã tồn tại!';
        }

        // Kiểm tra Tên
        if (!trimmedName) {
            newErrors.name = 'Vui lòng nhập tên nhóm khách hàng.';
        } else if (trimmedName.length < 2) {
            newErrors.name = 'Tên phải chứa ít nhất 2 ký tự.';
        } else {
            const isDuplicate = existingNames.includes(trimmedName);
            const isSelf = isEditMode && trimmedName === originalName;
            if (isDuplicate && !isSelf) newErrors.name = 'Tên nhóm khách hàng đã tồn tại!';
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
            const cleanPayload: CustomerGroupPayload = {
                ...formData,
                code: formData.code.trim().toUpperCase(),
                name: formData.name.trim(),
                description: formData.description?.trim() || ''
            };

            if (isEditMode && id) {
                await customerGroupApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await customerGroupApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/customer-groups'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Nhóm Khách Hàng' : 'Thêm Mới Nhóm Khách Hàng'}
                subtitle={isEditMode ? 'Cập nhật thông tin nhóm khách hàng' : 'Thiết lập thông tin nhóm khách hàng mới'}
                onBack={() => navigate('/customer-groups')}
                icon={Hexagon}
            />  

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-8">
                    <FormSection title="Thông Tin Cơ Bản">
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                            <FormInput 
                                label="Mã nhóm" required placeholder="G-001"
                                value={formData.code} error={errors.code} disabled={loading}
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên nhóm khách hàng" required placeholder="VD: Nhóm Lâu Năm"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                            <FormSelect 
                                label="Trạng thái" required value={formData.isActive ? 1 : 0} 
                                options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
                        </div>    
                    </FormSection> 

                    <FormSection title="Mô tả">
                        <FormTextarea 
                            label="Mô tả nhóm" placeholder="Ghi chú về nhóm khách hàng này..." 
                            value={formData.description || ''} rows={4} 
                            onChange={e => handleFieldChange('description', e.target.value)} 
                        />
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
}

export default CustomerGroupForm;