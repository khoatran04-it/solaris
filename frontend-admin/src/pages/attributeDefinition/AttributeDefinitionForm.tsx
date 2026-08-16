import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { BookType, Save, Plus } from 'lucide-react';

// API & Types
import { attributeDefinitionApi } from '../../api/attributeDefinitionApi';
import { AttributeDefinitionPayload } from '../../types/attributeDefinition';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, SubmitButton 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: AttributeDefinitionPayload = {
    name: '',
    dataType: 'TEXT',
    isActive: true
};

const DATA_TYPE_OPTIONS = [
    { label: 'Văn bản (TEXT) - Thường dùng cho Quy cách đóng gói, Độ tươi...', value: 'TEXT' },
    { label: 'Số (NUMBER) - Thường dùng cho Trọng lượng (gram), Kích thước...', value: 'NUMBER' },
    { label: 'Chọn sẵn (OPTIONS) - Thường dùng cho Nguồn gốc (VietGAP, Organic)...', value: 'OPTIONS' }
];

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const AttributeDefinitionForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<AttributeDefinitionPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // 🔥 BỔ SUNG STATE KIỂM TRA TRÙNG LẶP
    const [existingNames, setExistingNames] = useState<string[]>([]);
    const [originalName, setOriginalName] = useState('');
    
    const [loading, setLoading] = useState(false);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        attributeDefinitionApi.getAllList()
            .then(res => {
                setExistingNames(res.map((item: any) => item.name.toLowerCase()));
            })
            .catch(() => showToast('warning', 'Không tải được danh sách để kiểm tra trùng lặp'));

        // Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            attributeDefinitionApi.getById(Number(id)).then(res => {
                setFormData({
                    name: res.name || '',
                    dataType: res.dataType || 'TEXT',
                    isActive: res.isActive
                });
                setOriginalName((res.name || '').toLowerCase());
            })
            .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU THUỘC TÍNH'));
        }
    }, [id, isEditMode]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof AttributeDefinitionPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        const trimmedName = formData.name.trim().toLowerCase();

        if (!trimmedName) {
            newErrors.name = 'Vui lòng nhập tên thuộc tính.';
        } else if (existingNames.includes(trimmedName) && (!isEditMode || trimmedName !== originalName)) {
            newErrors.name = 'Tên thuộc tính này đã tồn tại trong hệ thống!';
        }
        
        if (!formData.dataType) {
            newErrors.dataType = 'Vui lòng chọn kiểu dữ liệu.';
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
            const cleanPayload: AttributeDefinitionPayload = {
                name: formData.name.trim(),
                dataType: formData.dataType,
                isActive: formData.isActive
            };

            if (isEditMode && id) {
                await attributeDefinitionApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await attributeDefinitionApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            
            // Quay về trang danh sách sau 1 giây
            setTimeout(() => navigate('/attributes'), 1000);
        } catch (error: any) {
            const errorMessage = error.response?.data?.message || 'CÓ LỖI XẢY RA KHI LƯU';
            showToast('error', errorMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <PageContainer>
            <Toast {...toast} />

            <FormHeader 
                title={isEditMode ? 'Chỉnh Sửa Từ Điển Thuộc Tính' : 'Thêm Thuộc Tính Mới'}
                subtitle={isEditMode ? 'Cập nhật chuẩn mực thuộc tính cho sản phẩm' : 'Định nghĩa một tiêu chuẩn mới (VD: VietGAP, Khay 500g...) vào hệ thống'}
                onBack={() => navigate('/attributes')}
                icon={BookType} 
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-8">
                    
                    <FormSection title="Thông Tin Thuộc Tính">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            {/* Cột 1: Tên Thuộc Tính */}
                            <FormInput 
                                label="Tên thuộc tính" required 
                                placeholder="VD: Quy cách đóng gói, Nguồn gốc..."
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />

                            {/* Cột 2: Kiểu dữ liệu */}
                            <FormSelect 
                                label="Kiểu dữ liệu" required 
                                value={formData.dataType} options={DATA_TYPE_OPTIONS}
                                error={errors.dataType}
                                onSelect={val => handleFieldChange('dataType', val)}
                            />

                            {/* Cột 3: Trạng thái */}
                            <FormSelect 
                                label="Trạng thái hiển thị" required 
                                value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
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

export default AttributeDefinitionForm;