import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Package, Save, Plus, Layers, Info, ChevronRight, Banknote, Trash2, Star } from 'lucide-react';

// API & Types
import { productVariantApi } from '../../api/productVariantApi';
import { productApi } from '../../api/productApi'; 
import { uomApi } from '../../api/uomApi'; 
import { ProductVariantPayload, VariantPriceInput } from '../../types/productVariant';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, FormTextarea, SubmitButton 
} from '../../components/commons/FormUI';
import { TabGroup, TabButton } from '../../components/commons/TabUI';

const INITIAL_STATE: ProductVariantPayload = {
    code: '',
    name: '',
    description: '',
    imagePath: '',
    inventoryGuideline: 0,
    isActive: true,
    productId: 0,
    attributes: [],
    prices: [] // 🔥 Khởi tạo mảng giá rỗng
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

interface AttributeDef {
    id: number;
    name: string;
    isRequired?: boolean;
}

type TabType = 'info' | 'attributes' | 'pricing';

const ProductVariantForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<ProductVariantPayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    const [loading, setLoading] = useState(false);
    
    // --- TABS & DROPDOWN OPTIONS ---
    const [activeTab, setActiveTab] = useState<TabType>('info');
    const [productOptions, setProductOptions] = useState<{ label: string, value: number }[]>([]);
    const [uomOptions, setUomOptions] = useState<{ label: string, value: number }[]>([]); // Data Đơn vị tính
    const [dynamicAttributes, setDynamicAttributes] = useState<AttributeDef[]>([]);

    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECT 1: Init Data ---
    useEffect(() => {
        // Tải SP gốc và Đơn vị tính song song
        Promise.all([
            productApi.getAllList(),
            uomApi.getAllList() // API lấy danh sách UoM (Kg, Thùng, Hộp...)
        ]).then(([products, uoms]) => {
            setProductOptions(products.map((p: any) => ({ label: p.name, value: p.id })));
            setUomOptions(uoms.map((u: any) => ({ label: u.name, value: u.id })));
        }).catch(() => showToast('warning', 'Không tải được danh mục bổ trợ (Sản phẩm / Đơn vị tính)'));

        // Lấy chi tiết Edit
        if (isEditMode && id) {
            productVariantApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    description: res.description || '',
                    imagePath: res.imagePath || '',
                    inventoryGuideline: res.inventoryGuideline || 0,
                    isActive: res.isActive,
                    productId: res.productId,
                    attributes: res.attributes.map(a => ({
                        attributeDefinitionId: a.attributeDefinitionId!,
                        attributeValue: a.attributeValue
                    })),
                    // Map bảng giá
                    prices: res.prices.map(p => ({
                        uoMId: p.uoMId,
                        price: p.price,
                        isDefault: p.isDefault
                    }))
                });
            }).catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU BIẾN THỂ'));
        }
    }, [id, isEditMode]);

    // --- EFFECT 2: Xử lý Form Động (EAV Pattern) ---
    useEffect(() => {
        if (formData.productId > 0) {
            productApi.getAttributesConfig(formData.productId)
                .then(attrs => setDynamicAttributes(attrs))
                .catch(() => setDynamicAttributes([]));
        } else {
            setDynamicAttributes([]);
        }
    }, [formData.productId]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof ProductVariantPayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    // Helper: Cập nhật Thuộc tính
    const handleDynamicAttrChange = (definitionId: number, value: string) => {
        setFormData(prev => {
            const newAttrs = [...prev.attributes];
            const index = newAttrs.findIndex(a => a.attributeDefinitionId === definitionId);
            if (index >= 0) newAttrs[index].attributeValue = value; 
            else newAttrs.push({ attributeDefinitionId: definitionId, attributeValue: value }); 
            return { ...prev, attributes: newAttrs };
        });
    };
    const getDynamicAttrValue = (definitionId: number) => formData.attributes.find(a => a.attributeDefinitionId === definitionId)?.attributeValue || '';

    // 🔥 Helpers: XỬ LÝ BẢNG GIÁ (TAB 3)
    const handleAddPriceRow = () => {
        setFormData(prev => ({
            ...prev,
            prices: [...prev.prices, { uoMId: 0, price: 0, isDefault: prev.prices.length === 0 }] // Dòng đầu tiên tự auto làm Mặc định
        }));
    };

    const handleRemovePriceRow = (index: number) => {
        setFormData(prev => {
            const newPrices = prev.prices.filter((_, i) => i !== index);
            // Nếu lỡ xóa trúng dòng mặc định, gán tạm dòng đầu làm mặc định
            if (newPrices.length > 0 && !newPrices.some(p => p.isDefault)) {
                newPrices[0].isDefault = true;
            }
            return { ...prev, prices: newPrices };
        });
    };

    const handleUpdatePriceRow = (index: number, field: keyof VariantPriceInput, value: any) => {
        setFormData(prev => {
            const newPrices = [...prev.prices];
            newPrices[index] = { ...newPrices[index], [field]: value };
            return { ...prev, prices: newPrices };
        });
    };

    const handleSetDefaultPrice = (index: number) => {
        setFormData(prev => ({
            ...prev,
            prices: prev.prices.map((p, i) => ({ ...p, isDefault: i === index }))
        }));
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        
        // Tab 1 Validate
        if (!formData.productId) newErrors.productId = 'Vui lòng chọn Sản phẩm gốc.';
        if (!formData.code.trim()) newErrors.code = 'Vui lòng nhập mã SKU.';
        if (!formData.name.trim()) newErrors.name = 'Vui lòng nhập tên biến thể.';
        if (formData.inventoryGuideline < 0) newErrors.inventoryGuideline = 'Tồn kho không hợp lệ.';

        // Tab 2 Validate
        dynamicAttributes.forEach(def => {
            if (def.isRequired && !getDynamicAttrValue(def.id).trim()) {
                newErrors[`attr_${def.id}`] = `Vui lòng nhập ${def.name}.`;
            }
        });

        // Tab 3 Validate
        if (formData.prices.length === 0) {
            newErrors.prices = 'Vui lòng thiết lập ít nhất 1 quy cách bán hàng.';
        } else {
            const hasInvalidRow = formData.prices.some(p => p.uoMId === 0 || p.price < 0);
            if (hasInvalidRow) newErrors.prices = 'Vui lòng chọn Đơn vị tính và nhập giá tiền hợp lệ.';
            
            const hasDefault = formData.prices.some(p => p.isDefault);
            if (!hasDefault) newErrors.prices = 'Vui lòng chọn 1 đơn vị tính làm mặc định hiển thị.';

            // Optional: Kiểm tra trùng Đơn vị tính (Chống vụ 1 SP có 2 dòng giá Kg khác nhau)
            const uomSet = new Set(formData.prices.map(p => p.uoMId));
            if (uomSet.size !== formData.prices.length) {
                newErrors.prices = 'Có đơn vị tính đang bị trùng lặp. Vui lòng kiểm tra lại.';
            }
        }

        setErrors(newErrors);

        // Auto Focus chuyển Tab khi có lỗi
        if (Object.keys(newErrors).length > 0) {
            if (newErrors.prices) setActiveTab('pricing');
            else if (Object.keys(newErrors).some(k => k.startsWith('attr_'))) setActiveTab('attributes');
            else setActiveTab('info');
        }

        return Object.keys(newErrors).length === 0;
    };

    // --- SUBMIT ---
    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validateForm()) return showToast('warning', errors.prices || 'Vui lòng kiểm tra lại các trường báo đỏ!');

        setLoading(true);
        try {
            const cleanPayload: ProductVariantPayload = {
                ...formData,
                code: formData.code.trim().toUpperCase(),
                name: formData.name.trim(),
                imagePath: formData.imagePath?.trim() || null,
                description: formData.description?.trim() || null,
                attributes: formData.attributes.filter(a => a.attributeValue.trim() !== ''),
                prices: formData.prices 
            };

            if (isEditMode && id) {
                await productVariantApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT BIẾN THỂ THÀNH CÔNG');
            } else {
                await productVariantApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI BIẾN THỂ THÀNH CÔNG');
            }
            setTimeout(() => navigate('/product-variants'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Biến Thể' : 'Tạo Mới Biến Thể'}
                subtitle="Cấu hình chi tiết mã hàng, thuộc tính và bảng giá đa quy cách"
                onBack={() => navigate('/product-variants')}
                icon={Package}
            />

            {/* 🔥 TABS ĐIỀU HƯỚNG 3 BƯỚC */}
            <div className="mb-5 flex items-center justify-between">
                <TabGroup>
                    <TabButton 
                        active={activeTab === 'info'} 
                        onClick={() => setActiveTab('info')} 
                        label="1. THÔNG TIN CƠ BẢN" 
                        icon={Info} 
                    />
                    <TabButton 
                        active={activeTab === 'attributes'} 
                        onClick={() => setActiveTab('attributes')} 
                        label="2. THUỘC TÍNH CHI TIẾT" 
                        icon={Layers} 
                    />
                    <TabButton 
                        active={activeTab === 'pricing'} 
                        onClick={() => setActiveTab('pricing')} 
                        label={`3. QUY CÁCH BÁN HÀNG (${formData.prices.length})`} 
                        icon={Banknote} 
                    />
                </TabGroup>
            </div>

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-6">
                    
                    {/* ================= TAB 1: THÔNG TIN CƠ BẢN ================= */}
                    <div className={activeTab === 'info' ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300' : 'hidden'}>
                        <div className="flex flex-col gap-10">
                            <FormSection title="Định Danh Biến Thể">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    <div className="md:col-span-2 mb-2 p-4 bg-blue-50/50 border border-blue-100 rounded-xl">
                                        <FormSelect 
                                            label="Sản Phẩm Gốc (Cha)" required placeholder="Chọn sản phẩm..." showSearch
                                            options={productOptions} value={formData.productId} error={errors.productId}
                                            onSelect={val => handleFieldChange('productId', val)}
                                            disabled={isEditMode} 
                                        />
                                        {!isEditMode && <p className="text-xs text-blue-600 mt-2 font-medium italic">* Thuộc tính động sẽ tự tải dựa trên Sản phẩm gốc bạn chọn.</p>}
                                    </div>
                                    <FormInput 
                                        label="Mã SKU (Barcode)" required placeholder="VD: TH-500G"
                                        value={formData.code} error={errors.code} disabled={loading}
                                        onChange={e => handleFieldChange('code', e.target.value)} 
                                    />
                                    <FormInput 
                                        label="Tên hiển thị biến thể" required placeholder="Thịt Heo Ba Chỉ - Khay 500g"
                                        value={formData.name} error={errors.name} disabled={loading}
                                        onChange={e => handleFieldChange('name', e.target.value)} 
                                    />
                                    <FormSelect 
                                        label="Trạng thái kinh doanh" required 
                                        value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                        onSelect={val => handleFieldChange('isActive', val === 1)}
                                    />
                                    <FormInput 
                                        label="Tồn kho định mức (Guide)" required type="number"
                                        value={formData.inventoryGuideline} error={errors.inventoryGuideline} disabled={loading}
                                        onChange={e => handleFieldChange('inventoryGuideline', parseInt(e.target.value) || 0)} 
                                    />
                                </div>
                            </FormSection>
                            <FormSection title="Hình Ảnh & Bổ Sung">
                                <div className="flex flex-col gap-6">
                                    <FormInput 
                                        label="Đường dẫn Ảnh (URL)" placeholder="https://..."
                                        value={formData.imagePath || ''} onChange={e => handleFieldChange('imagePath', e.target.value)} 
                                    />
                                    {formData.imagePath && (
                                        <div className="w-24 h-24 rounded-xl border border-slate-200 overflow-hidden shadow-sm">
                                            <img src={formData.imagePath} alt="Preview" className="w-full h-full object-cover" onError={(e) => (e.currentTarget.style.display = 'none')} />
                                        </div>
                                    )}
                                    <FormTextarea 
                                        label="Mô tả thêm (Tùy chọn)" placeholder="Nhập mô tả riêng cho biến thể này..." 
                                        value={formData.description || ''} rows={3} 
                                        onChange={(e: any) => handleFieldChange('description', e.target.value)} 
                                    />
                                </div>
                            </FormSection>
                        </div>
                    </div>

                    {/* ================= TAB 2: THUỘC TÍNH (EAV) ================= */}
                    <div className={activeTab === 'attributes' ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300' : 'hidden'}>
                        <FormSection title="Thuộc Tính Bổ Sung">
                            {!formData.productId ? (
                                <div className="p-12 text-center bg-slate-50 border border-slate-200 border-dashed rounded-xl">
                                    <Layers className="mx-auto text-slate-300 mb-3" size={40} />
                                    <h4 className="text-base font-bold text-slate-600 mb-1">Chưa chọn Sản Phẩm Gốc</h4>
                                    <p className="text-slate-400 font-medium text-sm">Vui lòng quay lại Tab 1 và chọn Sản Phẩm Gốc.</p>
                                </div>
                            ) : dynamicAttributes.length === 0 ? (
                                <div className="p-8 text-center bg-slate-50 border border-slate-200 border-dashed rounded-xl">
                                    <p className="text-slate-500 font-medium">Sản phẩm này không yêu cầu cấu hình thuộc tính động.</p>
                                </div>
                            ) : (
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 p-6 bg-indigo-50/30 border border-indigo-100 rounded-xl">
                                    {dynamicAttributes.map(def => (
                                        <FormInput 
                                            key={def.id} label={def.name} required={def.isRequired}
                                            placeholder={`Nhập ${def.name.toLowerCase()}...`}
                                            value={getDynamicAttrValue(def.id)} error={errors[`attr_${def.id}`]}
                                            onChange={e => handleDynamicAttrChange(def.id, e.target.value)} 
                                        />
                                    ))}
                                </div>
                            )}
                        </FormSection>
                    </div>

                    {/* ================= TAB 3: BẢNG GIÁ ĐA QUY CÁCH ================= */}
                    <div className={activeTab === 'pricing' ? 'block animate-in fade-in slide-in-from-bottom-4 duration-300' : 'hidden'}>
                        <FormSection title="Thiết Lập Quy Cách Bán Hàng">
                            <p className="text-sm text-slate-500 mb-4 -mt-2">
                                Khai báo các đơn vị tính khách hàng có thể mua (VD: Bán theo Kg, bán theo Thùng). Dòng được tích <strong>Mặc định</strong> sẽ hiển thị trên mặt tiền của website.
                            </p>
                            
                            {errors.prices && (
                                <div className="mb-4 p-3 bg-rose-50 text-rose-600 text-sm font-bold rounded-lg border border-rose-200/50">
                                    {errors.prices}
                                </div>
                            )}

                            {/* TABLE NHẬP LIỆU */}
                            <div className="border border-slate-200 rounded-xl overflow-hidden bg-white">
                                <table className="w-full text-left border-collapse">
                                    <thead className="bg-slate-50/80 border-b border-slate-200">
                                        <tr>
                                            <th className="w-[35%] py-3 px-4 text-xs font-bold text-slate-500 uppercase">Đơn Vị Tính <span className="text-red-500">*</span></th>
                                            <th className="w-[35%] py-3 px-4 text-xs font-bold text-slate-500 uppercase">Giá Bán Niêm Yết (VNĐ) <span className="text-red-500">*</span></th>
                                            <th className="w-[15%] py-3 px-4 text-xs font-bold text-slate-500 uppercase text-center">Mặc Định</th>
                                            <th className="w-[15%] py-3 px-4 text-xs font-bold text-slate-500 uppercase text-center">Thao Tác</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {formData.prices.length === 0 ? (
                                            <tr>
                                                <td colSpan={4} className="py-8 text-center text-slate-400 text-sm italic">
                                                    Chưa có bảng giá nào. Vui lòng thêm quy cách bán.
                                                </td>
                                            </tr>
                                        ) : (
                                            formData.prices.map((row, idx) => (
                                                <tr key={idx} className={row.isDefault ? 'bg-yellow-50/20' : 'hover:bg-slate-50/50 transition-colors'}>
                                                    <td className="p-3">
                                                        <FormSelect 
                                                            label="" // Ẩn label đi vì đã có Header bảng
                                                            options={uomOptions}
                                                            value={row.uoMId}
                                                            onSelect={val => handleUpdatePriceRow(idx, 'uoMId', val)}
                                                            placeholder="Chọn UoM..."
                                                        />
                                                    </td>
                                                    <td className="p-3">
                                                        <FormInput 
                                                            label="" type="number"
                                                            value={row.price}
                                                            onChange={e => handleUpdatePriceRow(idx, 'price', parseFloat(e.target.value) || 0)}
                                                        />
                                                    </td>
                                                    <td className="p-3 text-center">
                                                        <button 
                                                            type="button"
                                                            onClick={() => handleSetDefaultPrice(idx)}
                                                            className={`p-2 rounded-full transition-all mx-auto ${row.isDefault ? 'text-yellow-500 bg-yellow-100' : 'text-slate-300 hover:bg-slate-100 hover:text-slate-500'}`}
                                                            title={row.isDefault ? "Đang làm mặc định" : "Đặt làm mặc định"}
                                                        >
                                                            <Star size={20} className={row.isDefault ? "fill-current" : ""} />
                                                        </button>
                                                    </td>
                                                    <td className="p-3 text-center">
                                                        <button 
                                                            type="button"
                                                            onClick={() => handleRemovePriceRow(idx)}
                                                            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors mx-auto"
                                                            title="Xóa quy cách"
                                                        >
                                                            <Trash2 size={18} strokeWidth={2.5} />
                                                        </button>
                                                    </td>
                                                </tr>
                                            ))
                                        )}
                                    </tbody>
                                </table>
                                
                                {/* NÚT THÊM DÒNG NẰM DƯỚI ĐÁY BẢNG */}
                                <div className="p-3 bg-slate-50/30 border-t border-slate-100 flex justify-center">
                                    <button 
                                        type="button" 
                                        onClick={handleAddPriceRow}
                                        className="flex items-center gap-2 px-4 py-2 text-sm font-bold text-blue-600 bg-blue-50 hover:bg-blue-100 border border-blue-200/50 rounded-lg transition-colors"
                                    >
                                        <Plus size={16} strokeWidth={3} /> THÊM QUY CÁCH BÁN
                                    </button>
                                </div>
                            </div>
                        </FormSection>
                    </div>

                    {/* ================= FOOTER BUTTONS ================= */}
                    <div className="flex justify-between items-center pt-6 border-t border-slate-100 mt-2">
                        {activeTab === 'info' ? <div/> : (
                            <button 
                                type="button" 
                                onClick={() => setActiveTab(activeTab === 'pricing' ? 'attributes' : 'info')}
                                className="px-6 py-2.5 rounded-xl font-bold text-sm text-slate-600 bg-slate-100 hover:bg-slate-200 transition-colors"
                            >
                                Lùi lại bước trước
                            </button>
                        )}

                        {activeTab !== 'pricing' ? (
                            <button 
                                type="button" 
                                onClick={() => setActiveTab(activeTab === 'info' ? 'attributes' : 'pricing')}
                                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-800 bg-yellow-400 hover:bg-yellow-500 transition-colors shadow-sm shadow-yellow-200"
                            >
                                Tiếp tục <ChevronRight size={18} />
                            </button>
                        ) : (
                            <SubmitButton loading={loading} isEditMode={isEditMode} icon={isEditMode ? Save : Plus} />
                        )}
                    </div>
                </form>
            </FormCard>
        </PageContainer>
    );
};

export default ProductVariantForm;