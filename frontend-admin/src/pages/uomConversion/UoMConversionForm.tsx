import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Repeat, Save, Plus, ArrowRight, PackageOpen, LayoutTemplate } from 'lucide-react';

// API & Types
import { uomConversionApi } from '../../api/uomConversionApi';
import { uomApi } from '../../api/uomApi';
import { productApi } from '../../api/productApi';
import { UoMConversionPayload } from '../../types/uomConversion';

// Shared UI
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, SubmitButton 
} from '../../components/commons/FormUI';

const INITIAL_STATE: UoMConversionPayload = {
    fromUoMId: 0,
    toUoMId: 0,
    conversionFactor: 1,
    productId: null,
    isActive: true
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const UoMConversionForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<UoMConversionPayload>(INITIAL_STATE);
    const [isStandardMode, setIsStandardMode] = useState<boolean>(true); // Trạng thái Tabs
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    const [loading, setLoading] = useState(false);
    
    // --- DROPDOWN OPTIONS ---
    const [uomOptions, setUomOptions] = useState<{ label: string, value: number }[]>([]); 
    const [productOptions, setProductOptions] = useState<{ label: string, value: number }[]>([]); 

    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Tải danh mục UoM và Product
        Promise.all([
            uomApi.getAllList(),
            productApi.getAllList()
        ]).then(([uoms, products]) => {
            setUomOptions(uoms.map(u => ({ label: `${u.name} (${u.code})`, value: u.id })));
            setProductOptions(products.map(p => ({ label: `${p.code} - ${p.name}`, value: p.id })));
        }).catch(() => showToast('warning', 'Không tải được dữ liệu hệ thống'));

        // Tải chi tiết Edit
        if (isEditMode && id) {
            uomConversionApi.getById(Number(id)).then(res => {
                setFormData({
                    fromUoMId: res.fromUoMId,
                    toUoMId: res.toUoMId,
                    conversionFactor: res.conversionFactor,
                    productId: res.productId,
                    isActive: res.isActive
                });
                setIsStandardMode(res.productId === null);
            }).catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU'));
        }
    }, [id, isEditMode]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof UoMConversionPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};

        if (!formData.fromUoMId) newErrors.fromUoMId = 'Chọn đơn vị nguồn.';
        if (!formData.toUoMId) newErrors.toUoMId = 'Chọn đơn vị đích (gốc).';
        
        if (formData.fromUoMId && formData.toUoMId && formData.fromUoMId === formData.toUoMId) {
            newErrors.toUoMId = 'Đơn vị đích không được trùng đơn vị nguồn.';
        }

        if (!formData.conversionFactor || formData.conversionFactor <= 0) {
            newErrors.conversionFactor = 'Hệ số phải lớn hơn 0.';
        }

        if (!isStandardMode && !formData.productId) {
            newErrors.productId = 'Vui lòng chọn sản phẩm áp dụng.';
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
            const cleanPayload: UoMConversionPayload = {
                ...formData,
                productId: isStandardMode ? null : formData.productId
            };

            if (isEditMode && id) {
                await uomConversionApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT THÀNH CÔNG');
            } else {
                await uomConversionApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI THÀNH CÔNG');
            }
            setTimeout(() => navigate('/uom-conversions'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Tỷ Lệ' : 'Thêm Mới Tỷ Lệ Quy Đổi'}
                subtitle="Định nghĩa công thức toán học giữa các đơn vị tính"
                onBack={() => navigate('/uom-conversions')}
                icon={Repeat}
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-10">
                    
                    {/* KHU VỰC CHỌN TABS (UX Đỉnh) */}
                    <FormSection title="Loại Hình Quy Đổi">
                        <div className="flex bg-slate-100/70 p-1.5 rounded-2xl w-full lg:w-1/2">
                            <button
                                type="button"
                                onClick={() => { setIsStandardMode(true); handleFieldChange('productId', null); }}
                                className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-xl font-bold transition-all duration-300 ${
                                    isStandardMode 
                                    ? 'bg-white text-indigo-700 shadow-sm border border-indigo-100' 
                                    : 'text-slate-500 hover:text-slate-700 hover:bg-slate-200/50'
                                }`}
                            >
                                <LayoutTemplate size={18} />
                                Tiêu chuẩn toàn cục
                            </button>
                            <button
                                type="button"
                                onClick={() => setIsStandardMode(false)}
                                className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-xl font-bold transition-all duration-300 ${
                                    !isStandardMode 
                                    ? 'bg-white text-purple-700 shadow-sm border border-purple-100' 
                                    : 'text-slate-500 hover:text-slate-700 hover:bg-slate-200/50'
                                }`}
                            >
                                <PackageOpen size={18} />
                                Đặc thù sản phẩm
                            </button>
                        </div>

                        {/* NẾU LÀ ĐẶC THÙ THÌ HIỆN Ô CHỌN SẢN PHẨM */}
                        {!isStandardMode && (
                            <div className="mt-6 w-full lg:w-1/2 animate-in fade-in slide-in-from-top-4 duration-300">
                                <FormSelect 
                                    label="Chọn sản phẩm áp dụng" required showSearch
                                    placeholder="Tìm sản phẩm..."
                                    options={productOptions} value={formData.productId || 0} error={errors.productId}
                                    onSelect={val => handleFieldChange('productId', val)} 
                                />
                            </div>
                        )}
                    </FormSection>

                    {/* KHU VỰC CÔNG THỨC TOÁN HỌC */}
                    <FormSection title="Công Thức Toán Học">
                        <div className="flex flex-col lg:flex-row items-start lg:items-center gap-4 bg-slate-50 p-6 rounded-2xl border border-slate-200">
                            
                            <div className="flex-1 w-full">
                                <FormSelect 
                                    label="Từ (1 Đơn vị này...)" required showSearch
                                    placeholder="VD: Thùng"
                                    options={uomOptions} value={formData.fromUoMId} error={errors.fromUoMId}
                                    onSelect={val => handleFieldChange('fromUoMId', val)} 
                                />
                            </div>

                            <div className="hidden lg:flex pt-6 opacity-40">
                                <ArrowRight size={24} strokeWidth={3} />
                            </div>

                            <div className="w-full lg:w-32 shrink-0">
                                <FormInput 
                                    label="Hệ số" required placeholder="VD: 24" type="number"
                                    value={formData.conversionFactor} error={errors.conversionFactor} disabled={loading}
                                    onChange={e => handleFieldChange('conversionFactor', Number(e.target.value))} 
                                />
                            </div>

                            <div className="flex-1 w-full">
                                <FormSelect 
                                    label="Đến (...bằng bao nhiêu Đơn vị này)" required showSearch
                                    placeholder="VD: Lon"
                                    options={uomOptions} value={formData.toUoMId} error={errors.toUoMId}
                                    onSelect={val => handleFieldChange('toUoMId', val)} 
                                />
                            </div>

                        </div>
                    </FormSection>

                    <FormSection title="Cài Đặt">
                        <div className="w-full lg:w-1/2">
                            <FormSelect 
                                label="Trạng thái áp dụng" required 
                                value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                onSelect={val => handleFieldChange('isActive', val === 1)}
                            />
                        </div>
                    </FormSection>

                    <div className="flex justify-end pt-6 border-t border-slate-100 mt-2">
                        <SubmitButton 
                            loading={loading} isEditMode={isEditMode} icon={isEditMode ? Save : Plus} 
                        />
                    </div>
                </form>
            </FormCard>
        </PageContainer>
    );
};

export default UoMConversionForm;