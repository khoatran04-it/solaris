import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Warehouse as WarehouseIcon, Save, Plus } from 'lucide-react';

// API & Types
import { warehouseApi } from '../../api/warehouseApi';
import { userApi } from '../../api/userApi';
import { WarehousePayload, WarehouseAddressPayload } from '../../types/warehouse';

// Shared UI Components
import { Toast } from '../../components/commons/Toast';
import { 
    PageContainer, FormCard, FormInput, FormHeader, 
    FormSection, FormSelect, SubmitButton 
} from '../../components/commons/FormUI';

// 1. Cấu hình giá trị khởi tạo
const INITIAL_STATE: WarehousePayload = {
    code: '',
    name: '',
    warehouseType: '',
    managerId: 0, // 0 = Chưa bổ nhiệm
    isActive: true,
    address: {
        province: '',
        district: '',
        ward: '',
        streetAddress: '',
        latitude: 0,
        longitude: 0
    }
};

const STATUS_OPTIONS = [
    { label: 'Hoạt động', value: 1 },
    { label: 'Tạm khóa', value: 0 }
];

const WAREHOUSE_TYPES = [
    { label: '-- Chọn loại kho --', value: '' },
    { label: 'Kho Tổng (Master Hub)', value: 'Kho Tổng' },
    { label: 'Kho Bán Lẻ (Retail)', value: 'Kho Bán Lẻ' },
    { label: 'Trạm Trung Chuyển (Transit)', value: 'Trạm Trung Chuyển' },
    { label: 'Kho Hàng Lỗi (Damaged)', value: 'Kho Hàng Lỗi' }
];

const WarehouseForm: React.FC = () => {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const isEditMode = Boolean(id);

    // --- STATES ---
    const [formData, setFormData] = useState<WarehousePayload>(INITIAL_STATE);
    const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
    
    // Kiểm tra trùng lặp mã Kho
    const [existingCodes, setExistingCodes] = useState<string[]>([]);
    const [originalCode, setOriginalCode] = useState('');
    
    // Options cho Dropdowns
    const [managerOptions, setManagerOptions] = useState<{ label: string, value: number }[]>([]);

    const [loading, setLoading] = useState(false);
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'warning' | 'error', message: string }>({ 
        show: false, type: 'success', message: '' 
    });

    // --- EFFECTS ---
    useEffect(() => {
        // Tải danh sách Nhân viên (làm Trưởng kho) & Danh sách Kho (để check trùng mã)
        Promise.all([
            userApi.getAllList().catch(() => []), // Giả định hàm này trả về danh sách user
            warehouseApi.getAllList()
        ]).then(([users, warehouses]) => {
            
            // Map danh sách nhân sự
            setManagerOptions([
                { label: '-- Chưa bổ nhiệm --', value: 0 },
                ...users.map((u: any) => ({ label: `${u.fullName} (${u.username})`, value: u.id }))
            ]);
            
            // Lưu mã kho hiện tại để check trùng
            setExistingCodes(warehouses.map(w => w.code.toLowerCase()));
        }).catch(() => showToast('warning', 'Không tải được danh sách tùy chọn'));

        // Tải dữ liệu chi tiết khi Edit
        if (isEditMode && id) {
            warehouseApi.getById(Number(id)).then(res => {
                setFormData({
                    code: res.code || '',
                    name: res.name || '',
                    warehouseType: res.warehouseType || '',
                    managerId: res.managerId || 0,
                    isActive: res.isActive,
                    address: {
                        province: res.province || '',
                        district: res.district || '',
                        ward: res.ward || '',
                        streetAddress: res.streetAddress || '',
                        latitude: res.latitude || 0,
                        longitude: res.longitude || 0
                    }
                });
                setOriginalCode((res.code || '').toLowerCase());
            })
            .catch(() => showToast('error', 'KHÔNG TÌM THẤY DỮ LIỆU KHO HÀNG'));
        }
    }, [id, isEditMode]);

    // --- HELPERS ---
    const showToast = (type: 'success' | 'warning' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const handleFieldChange = (field: keyof WarehousePayload, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (errors[field]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[field]; return newErrors; });
        }
    };

    const handleAddressChange = (field: keyof WarehouseAddressPayload, value: any) => {
        setFormData(prev => ({
            ...prev,
            address: { ...prev.address, [field]: value }
        }));
        if (errors[`address_${field}`]) {
            setErrors(prev => { const newErrors = { ...prev }; delete newErrors[`address_${field}`]; return newErrors; });
        }
    };

    // --- VALIDATION ---
    const validateForm = () => {
        const newErrors: Record<string, string> = {};
        
        // 1. Validate Thông tin chung
        const trimmedCode = (formData.code || '').trim().toLowerCase();
        if (!trimmedCode && !isEditMode) {
            newErrors.code = 'Vui lòng nhập mã kho.';
        } else if (existingCodes.includes(trimmedCode) && (!isEditMode || trimmedCode !== originalCode)) {
            newErrors.code = 'Mã kho này đã tồn tại!';
        }

        if (!formData.name.trim()) newErrors.name = 'Vui lòng nhập tên kho hàng.';
        if (!formData.warehouseType) newErrors.warehouseType = 'Vui lòng chọn loại kho.';

        // 2. Validate Địa chỉ
        if (!formData.address.province.trim()) newErrors.address_province = 'Vui lòng nhập Tỉnh/Thành phố.';
        if (!formData.address.district.trim()) newErrors.address_district = 'Vui lòng nhập Quận/Huyện.';
        if (!formData.address.ward.trim()) newErrors.address_ward = 'Vui lòng nhập Phường/Xã.';
        if (!formData.address.streetAddress.trim()) newErrors.address_streetAddress = 'Vui lòng nhập số nhà, tên đường.';

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    // --- SUBMIT ---
    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!validateForm()) return showToast('warning', 'Vui lòng kiểm tra lại các trường báo đỏ!');

        setLoading(true);
        try {
            const cleanPayload: WarehousePayload = {
                ...formData,
                code: formData.code?.trim().toUpperCase(),
                name: formData.name.trim(),
                managerId: formData.managerId === 0 ? null : formData.managerId,
                address: {
                    province: formData.address.province.trim(),
                    district: formData.address.district.trim(),
                    ward: formData.address.ward.trim(),
                    streetAddress: formData.address.streetAddress.trim(),
                    latitude: formData.address.latitude || 0,
                    longitude: formData.address.longitude || 0
                }
            };

            // Nếu Edit thì không gửi mã code (vì mã kho là cố định)
            if (isEditMode) {
                delete cleanPayload.code;
            }

            if (isEditMode && id) {
                await warehouseApi.update(Number(id), cleanPayload);
                showToast('success', 'CẬP NHẬT KHO HÀNG THÀNH CÔNG');
            } else {
                await warehouseApi.create(cleanPayload);
                showToast('success', 'THÊM MỚI KHO HÀNG THÀNH CÔNG');
            }
            
            // Quay về trang danh sách sau 1 giây
            setTimeout(() => navigate('/warehouses'), 1000);
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
                title={isEditMode ? 'Chỉnh Sửa Kho Hàng' : 'Tạo Kho Hàng Mới'}
                subtitle={isEditMode ? 'Cập nhật thông tin địa chỉ và nhân sự quản lý kho' : 'Thiết lập kho lưu trữ vật lý hoặc trạm trung chuyển mới'}
                onBack={() => navigate('/warehouses')}
                icon={WarehouseIcon} 
            />

            <FormCard>
                <form onSubmit={handleSubmit} className="flex flex-col gap-10">
                    
                    {/* ================= SECTION 1: THÔNG TIN CƠ BẢN ================= */}
                    <FormSection title="Định Danh Kho Hàng">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            <FormInput 
                                label="Mã Kho (Warehouse Code)" required placeholder="VD: HUB-HCM-01"
                                value={formData.code || ''} error={errors.code} 
                                disabled={isEditMode || loading} // Mã kho tạo xong không được sửa
                                onChange={e => handleFieldChange('code', e.target.value)} 
                            />
                            <FormInput 
                                label="Tên Kho Hàng" required placeholder="VD: Kho Tổng Miền Nam"
                                value={formData.name} error={errors.name} disabled={loading}
                                onChange={e => handleFieldChange('name', e.target.value)} 
                            />
                            <FormSelect 
                                label="Loại Kho" required 
                                value={formData.warehouseType || ''} options={WAREHOUSE_TYPES} error={errors.warehouseType}
                                onSelect={val => handleFieldChange('warehouseType', val)}
                            />
                            <FormSelect 
                                label="Trưởng Kho Phụ Trách" 
                                placeholder="Chọn nhân viên..." showSearch
                                value={formData.managerId || 0} options={managerOptions}
                                onSelect={val => handleFieldChange('managerId', val)}
                            />
                            <div className="lg:col-span-2">
                                <FormSelect 
                                    label="Trạng thái hoạt động" required 
                                    value={formData.isActive ? 1 : 0} options={STATUS_OPTIONS}
                                    onSelect={val => handleFieldChange('isActive', val === 1)}
                                />
                            </div>
                        </div>
                    </FormSection>

                    {/* ================= SECTION 2: ĐỊA CHỈ CHI TIẾT ================= */}
                    <FormSection title="Thông Tin Địa Chỉ & Tọa Độ">
                        <div className="p-4 mb-2 bg-blue-50 border border-blue-100 rounded-xl">
                            <p className="text-[13px] text-blue-800 font-medium">
                                <strong>Mẹo:</strong> Việc nhập chính xác Vĩ độ (Latitude) và Kinh độ (Longitude) sẽ giúp hệ thống tự động hóa quá trình điều phối đơn hàng đến kho gần nhất cực kỳ hiệu quả.
                            </p>
                        </div>
                        
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                            <FormInput 
                                label="Tỉnh / Thành phố" required placeholder="VD: Hồ Chí Minh"
                                value={formData.address.province} error={errors.address_province} disabled={loading}
                                onChange={e => handleAddressChange('province', e.target.value)} 
                            />
                            <FormInput 
                                label="Quận / Huyện" required placeholder="VD: Quận 1"
                                value={formData.address.district} error={errors.address_district} disabled={loading}
                                onChange={e => handleAddressChange('district', e.target.value)} 
                            />
                            <FormInput 
                                label="Phường / Xã" required placeholder="VD: Phường Bến Nghé"
                                value={formData.address.ward} error={errors.address_ward} disabled={loading}
                                onChange={e => handleAddressChange('ward', e.target.value)} 
                            />
                            <div className="lg:col-span-3">
                                <FormInput 
                                    label="Số nhà, tên đường chi tiết" required placeholder="VD: 123 Đường Lê Lợi..."
                                    value={formData.address.streetAddress} error={errors.address_streetAddress} disabled={loading}
                                    onChange={e => handleAddressChange('streetAddress', e.target.value)} 
                                />
                            </div>
                            
                            {/* Toạ độ GPS */}
                            <FormInput 
                                label="Vĩ độ (Latitude)" type="number" placeholder="VD: 10.7769"
                                value={formData.address.latitude} disabled={loading}
                                onChange={e => handleAddressChange('latitude', parseFloat(e.target.value) || 0)} 
                            />
                            <FormInput 
                                label="Kinh độ (Longitude)" type="number" placeholder="VD: 106.7009"
                                value={formData.address.longitude} disabled={loading}
                                onChange={e => handleAddressChange('longitude', parseFloat(e.target.value) || 0)} 
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

export default WarehouseForm;