import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Layers, Save, Plus } from 'lucide-react';

// API & Types
import { uomCategoryApi } from '../../api/uomCategoryApi';
import { UoMCategoryPayload } from '../../types/uomCategory';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, SubmitButton 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: UoMCategoryPayload = {
    code: '',
    name: '',
    isActive: true,
    baseUoMId: null 
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const UoMCategoryForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<UoMCategoryPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // Kiểm tra trùng lặp
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    const [existingNames, setExistingNames] = useState<string[]>([]);
    const [originalName, setOriginalName] = useState('');
    
    const [loading, setLoading] = useState(false);
    
    // Tạm thời để trống. Sau này làm API UoM xong sẽ map data vào đây.
    const [uomOptions, setUomOptions] = useState<{ label: string, value: number }[]>([]); 

    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Lấy danh sách để check trùng lặp tên/mã
        uomCategoryApi.getAllList()
            .then(res => {
                setExistingCodes(res.map(c => c.code.toLowerCase()));
                setExistingNames(res.map(c => c.name.toLowerCase()));
            })
            .catch(() => showToast('warning', 'Không tải được danh sách kiểm tra trùng lặp'));

        // TODO: Chỗ này sau này thêm logic gọi `uomApi.getAllList()` để setUomOptions

        // Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            uomCategoryApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    isActive: res.isActive ?? true,
                    baseUoMId: res.baseUoMId || null
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

    const handleFieldChange = (field: keyof UoMCategoryPayload, value: any) => {
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

        // Kiểm tra Mã
        if (!trimmedCode) newErrors.code = 'Vui lòng nhập mã định danh.';
        else if (trimmedCode.length < 2) newErrors.code = 'Mã phải chứa ít nhất 2 ký tự.';
        else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
            newErrors.code = 'Mã định danh đã tồn tại!';
        }

        // Kiểm tra Tên
        if (!trimmedName) newErrors.name = 'Vui lòng nhập tên nhóm.';
        else if (trimmedName.length < 2) newErrors.name = 'Tên phải chứa ít nhất 2 ký tự.';
        else if (existingNames.includes(trimmedName) && (!isEditMode || trimmedName !== originalName)) {
            newErrors.name = 'Tên nhóm đã tồn tại!';
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
            const cleanPayload: UoMCategoryPayload = {
                ...formData,
                code: formData.code.trim().toUpperCase(), // Tự động viết hoa mã
                name: formData.name.trim(),
            };

            if (isEditMode && id) {
                await uomCategoryApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await uomCategoryApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/uom-categories'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Nhóm Đơn Vị' : 'Thêm Nhóm Đơn Vị Tính'}
                subtitle="Cấu hình nhóm quy đổi chuẩn mực cho kho bãi"
                onBack={() => navigate('/uom-categories')}
                icon={Layers}
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-10">
                    
                    {/* SECTION 1: THÔNG TIN CƠ BẢN */}
                    <FormSection title="Thông Tin Nhóm">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <FormInput 
                                label="Mã nhóm" required placeholder="VD: WEIGHT, LENGTH"
                                value={formData.code} error={errors.code} disabled={loading}
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên nhóm" required placeholder="VD: Khối lượng, Chiều dài"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                        </div>
                    </FormSection>

                    {/* SECTION 2: CẤU HÌNH HỆ THỐNG */}
                    <FormSection title="Cấu Hình Hệ Thống">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <FormSelect 
                                label="Trạng thái hệ thống" required 
                                value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
                            
                            <FormSelect 
                                label="Đơn vị gốc (Base UoM)" 
                                placeholder="Chưa thiết lập..." 
                                value={formData.baseUoMId || 0} // Hiển thị 0 nếu chưa có
                                options={uomOptions}
                                onSelect={val => handleFieldChange('baseUoMId', val === 0 ? null : val)}
                            />
                        </div>
                        <p className="mt-2 text-xs text-slate-500 italic">
                            * Lưu ý: Đơn vị gốc (Base UoM) dùng làm trạm trung chuyển để tính toán hệ số. Bạn có thể thiết lập sau khi đã tạo xong các Đơn vị tính con.
                        </p>
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

export default UoMCategoryForm;