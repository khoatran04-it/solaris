    import React, { useEffect, useState } from 'react';
    import { useParams, useNavigate } from 'react-router-dom';
    import { Building2, MapPin, History, Hexagon, Loader2, Plus, Star, Edit3, Trash2, Truck } from 'lucide-react';

    // API & Types
    import { supplierApi } from '../../api/supplierApi';
    import { supplierAddressApi } from '../../api/supplierAddressApi';
    import { Supplier } from '../../types/supplier';
    import { SupplierAddress, SupplierAddressPayload } from '../../types/supplierAddress';

    // Components
    import { Toast } from '../../components/commons/Toast';
    import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
    import { ModalSupplierAddress } from '../../components/modals/ModalSupplierAddress';
    import { 
        DetailPageContainer, DetailHeader, TabGroup, TabButton, 
        DetailCard, DetailSection, InfoField, DetailProfileCard, TabEmptyPlaceholder 
    } from '../../components/commons/TabUI';
    import { DateTimeCell, TableEmpty } from '../../components/commons/ListUI';

    type TabType = 'detail' | 'addresses' | 'history';

    const SupplierDetail: React.FC = () => {
        const { id } = useParams<{ id: string }>();
        const navigate = useNavigate();
        
        // --- STATES DỮ LIỆU ---
        const [supplier, setSupplier] = useState<Supplier | null>(null);
        const [loading, setLoading] = useState(true);
        const [activeTab, setActiveTab] = useState<TabType>('detail');
        const [toast, setToast] = useState<{ show: boolean, type: 'error' | 'success', message: string }>({ show: false, type: 'error', message: '' });

        // --- STATES CHO MODAL ĐỊA CHỈ KHO ---
        const [isAddressModalOpen, setIsAddressModalOpen] = useState(false);
        const [editingAddress, setEditingAddress] = useState<SupplierAddressPayload | null>(null);
        const [editingAddressId, setEditingAddressId] = useState<number | null>(null);
        
        // --- STATES CHO MODAL XÓA ---
        const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
        const [deletingAddressId, setDeletingAddressId] = useState<number | null>(null);

        // --- EFFECT ---
        const fetchSupplier = () => {
            setLoading(true);
            supplierApi.getById(Number(id))
                .then(res => setSupplier(res))
                .catch(() => showToast('error', 'Không thể tải thông tin nhà cung cấp!'))
                .finally(() => setLoading(false));
        };

        useEffect(() => {
            if (id) fetchSupplier();
        // eslint-disable-next-line react-hooks/exhaustive-deps
        }, [id]);

        const showToast = (type: 'success' | 'error', message: string) => {
            setToast({ show: true, type, message });
            setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
        };

        // --- HANDLERS: ĐỊA CHỈ ---
        const openAddAddressModal = () => {
            setEditingAddress(null);
            setEditingAddressId(null);
            setIsAddressModalOpen(true);
        };

        const openEditAddressModal = (address: SupplierAddress) => {
            setEditingAddressId(address.id);
            setEditingAddress({
                contactName: address.contactName,
                contactPhone: address.contactPhone,
                province: address.province,
                district: address.district || '',
                ward: address.ward || '',
                streetAddress: address.streetAddress,
                isDefault: address.isDefault
            });
            setIsAddressModalOpen(true);
        };

        const handleSaveAddress = async (payload: SupplierAddressPayload) => {
            try {
                if (editingAddressId) {
                    await supplierAddressApi.update(editingAddressId, payload);
                    showToast('success', 'Cập nhật địa chỉ kho thành công!');
                } else {
                    await supplierAddressApi.create(Number(id), payload);
                    showToast('success', 'Thêm địa chỉ kho mới thành công!');
                }
                fetchSupplier(); // Reload lại danh sách sau khi lưu
            } catch (error) {
                showToast('error', 'Có lỗi xảy ra khi lưu địa chỉ.');
                throw error;
            }
        };

        const handleSetDefault = async (addressId: number) => {
            try {
                await supplierAddressApi.setDefault(addressId, Number(id));
                showToast('success', 'Đã thay đổi địa chỉ kho mặc định!');
                fetchSupplier();
            } catch (error) {
                showToast('error', 'Lỗi khi thiết lập mặc định.');
            }
        };

        const confirmDeleteAddress = async () => {
            if (!deletingAddressId) return;
            try {
                await supplierAddressApi.delete(deletingAddressId);
                setIsDeleteModalOpen(false);
                showToast('success', 'Đã xóa địa chỉ kho thành công!');
                fetchSupplier();
            } catch (error) {
                showToast('error', 'Không thể xóa địa chỉ này.');
            }
        };

        // --- RENDER ---
        if (loading) {
            return (
                <div className="h-full flex items-center justify-center bg-slate-50/30">
                    <Loader2 className="w-10 h-10 animate-spin text-yellow-500" />
                </div>
            );
        }

        if (!supplier) return null;

        return (
            <DetailPageContainer>
                <Toast {...toast} />
                <ModalSupplierAddress 
                    isOpen={isAddressModalOpen} 
                    onClose={() => setIsAddressModalOpen(false)}
                    onSave={handleSaveAddress}
                    initialData={editingAddress}
                />
                <ConfirmDeleteModal 
                    isOpen={isDeleteModalOpen} 
                    itemName="địa chỉ kho này" 
                    onClose={() => setIsDeleteModalOpen(false)} 
                    onConfirm={confirmDeleteAddress} 
                />
                
                <DetailHeader 
                    icon={Hexagon} title="Hồ Sơ Nhà Cung Cấp"
                    subtitle={<>Mã hệ thống: <span className="text-slate-800 font-bold">{supplier.code}</span></>}
                    onBack={() => navigate('/suppliers')}
                />

                <TabGroup>
                    <TabButton active={activeTab === 'detail'} onClick={() => setActiveTab('detail')} label="THÔNG TIN DOANH NGHIỆP" icon={Building2} />
                    <TabButton active={activeTab === 'addresses'} onClick={() => setActiveTab('addresses')} label="DANH SÁCH ĐỊA CHỈ KHO" icon={MapPin} />
                    <TabButton active={activeTab === 'history'} onClick={() => setActiveTab('history')} label="LỊCH SỬ NHẬP HÀNG" icon={Truck} />
                </TabGroup>

                <DetailCard>
                    {/* ================= TAB 1: THÔNG TIN CHI TIẾT ================= */}
                    {activeTab === 'detail' && (
                        <div className="p-8 grid grid-cols-1 lg:grid-cols-3 gap-10 items-start">
                            <div className="lg:col-span-2 flex flex-col gap-9">
                                <DetailSection title="Tổng Quan Doanh Nghiệp" dotColor="bg-yellow-400">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-y-5 gap-x-8">
                                        <InfoField label="Tên công ty/Đối tác" value={supplier.name} />
                                        <InfoField label="Phân loại" value={<span className="inline-flex px-2.5 py-0.5 rounded-lg bg-indigo-50 text-indigo-700 text-xs font-bold border border-indigo-200/30">{supplier.supplierTypeName || 'Chưa cập nhật'}</span>} />
                                        <InfoField label="Mã số thuế" value={supplier.taxCode || '---'} />
                                        <InfoField label="Website" value={supplier.website ? <a href={supplier.website.startsWith('http') ? supplier.website : `https://${supplier.website}`} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">{supplier.website}</a> : '---'} />
                                        <div className="md:col-span-2 grid grid-cols-2 border-l-2 border-slate-100 pl-4 py-0.5 gap-2 mt-2">
                                            <div className="flex flex-col items-start"><span className="text-[11px] font-bold text-slate-400 uppercase mb-1">Ngày Khởi Tạo</span><DateTimeCell isoString={supplier.createdAt} /></div>
                                            <div className="flex flex-col items-start"><span className="text-[11px] font-bold text-slate-400 uppercase mb-1">Cập Nhật Gần Nhất</span><DateTimeCell isoString={supplier.updatedAt} /></div>
                                        </div>
                                    </div>
                                </DetailSection>

                                <DetailSection title="Liên Hệ & Thanh Toán" dotColor="bg-blue-400">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-y-5 gap-x-8">
                                        <InfoField label="Số điện thoại chính" value={supplier.phone} />
                                        <InfoField label="Email công ty" value={supplier.email || '---'} />
                                        <InfoField label="Số tài khoản" value={supplier.bankAccount || '---'} />
                                        <InfoField label="Ngân hàng" value={supplier.bankName || '---'} />
                                    </div>
                                </DetailSection>

                                <DetailSection title="Ghi Chú Nội Bộ" dotColor="bg-slate-400">
                                    <p className="text-sm text-slate-600 leading-relaxed bg-slate-50/70 p-4 rounded-2xl border border-slate-100 min-h-24">
                                        {supplier.note || <span className="italic text-slate-400 font-normal">Không có ghi chú đặc biệt cho nhà cung cấp này.</span>}
                                    </p>
                                </DetailSection>
                            </div>

                            <div className="lg:col-span-1">
                                <DetailProfileCard 
                                    title={supplier.name} subTitle={supplier.code} logoPath={supplier.logoPath} fallbackIcon={Building2} statusActive={supplier.isActive}
                                />
                            </div>
                        </div>
                    )}

                    {/* ================= TAB 2: SỔ ĐỊA CHỈ KHO ================= */}
                    {activeTab === 'addresses' && (
                        <div className="p-8 flex flex-col gap-6">
                            <div className="flex items-center justify-between">
                                <div>
                                    <h3 className="text-lg font-bold text-slate-800">Danh Sách Địa Chỉ Kho</h3>
                                    <p className="text-sm text-slate-500 mt-1">Quản lý các địa điểm lấy hàng, trả hàng của đối tác</p>
                                </div>
                                <button 
                                    onClick={openAddAddressModal}
                                    className="flex items-center gap-2 px-4 py-2.5 text-sm font-bold text-slate-900 bg-yellow-400 rounded-xl hover:bg-yellow-500 shadow-sm shadow-yellow-200 transition-all"
                                >
                                    <Plus size={18} strokeWidth={2.5} /> Thêm Địa Chỉ
                                </button>
                            </div>

                            <div className="overflow-x-auto border border-slate-100 rounded-xl">
                                <table className="w-full text-left border-collapse">
                                    <thead>
                                        <tr className="bg-slate-50/70 border-b border-slate-100">
                                            <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Mặc Định</th>
                                            <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Người Liên Hệ</th>
                                            <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">SĐT Liên Hệ</th>
                                            <th className="w-[40%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Địa Chỉ Kho</th>
                                            <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {supplier.addresses && supplier.addresses.length > 0 ? (
                                            supplier.addresses.map((addr) => (
                                                <tr key={addr.id} className={`transition-colors duration-200 group ${addr.isDefault ? 'bg-yellow-50/30' : 'hover:bg-slate-50/80'}`}>
                                                    
                                                    {/* Icon Mặc định (Ngôi sao) */}
                                                    <td className="py-4 px-6 text-center">
                                                        <button 
                                                            onClick={() => !addr.isDefault && handleSetDefault(addr.id)}
                                                            disabled={addr.isDefault}
                                                            className={`p-2 rounded-full transition-all ${
                                                                addr.isDefault 
                                                                ? 'text-yellow-500 bg-yellow-100 cursor-default' 
                                                                : 'text-slate-300 hover:text-yellow-500 hover:bg-yellow-50'
                                                            }`}
                                                            title={addr.isDefault ? 'Đang là địa chỉ kho mặc định' : 'Đặt làm địa chỉ kho mặc định'}
                                                        >
                                                            <Star size={20} className={addr.isDefault ? 'fill-current' : ''} />
                                                        </button>
                                                    </td>

                                                    <td className="py-4 px-6 font-bold text-slate-700">{addr.contactName}</td>
                                                    <td className="py-4 px-6 font-medium text-slate-600">{addr.contactPhone}</td>
                                                    
                                                    {/* Gộp địa chỉ đầy đủ */}
                                                    <td className="py-4 px-6 text-sm text-slate-600">
                                                        {addr.streetAddress}, {addr.ward ? `${addr.ward}, ` : ''}{addr.district ? `${addr.district}, ` : ''}{addr.province}
                                                    </td>

                                                    {/* Thao tác */}
                                                    <td className="py-4 px-6">
                                                        <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                            <button 
                                                                onClick={() => openEditAddressModal(addr)}
                                                                className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                                                                title="Chỉnh sửa"
                                                            >
                                                                <Edit3 size={17} strokeWidth={2.5} />
                                                            </button>
                                                            <button 
                                                                onClick={() => { setDeletingAddressId(addr.id); setIsDeleteModalOpen(true); }}
                                                                className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                                                                title="Xóa"
                                                            >
                                                                <Trash2 size={17} strokeWidth={2.5} />
                                                            </button>
                                                        </div>
                                                    </td>
                                                </tr>
                                            ))
                                        ) : (
                                            <TableEmpty colSpan={5} message="Nhà cung cấp này chưa có dữ liệu địa chỉ kho." />
                                        )}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    )}

                    {/* ================= TAB 3: LỊCH SỬ NHẬP HÀNG ================= */}
                    {activeTab === 'history' && (
                        <TabEmptyPlaceholder icon={Truck} title="Lịch sử nhập hàng trống" desc="Nhà cung cấp này chưa phát sinh bất kỳ đơn nhập hàng hay giao dịch nào trên hệ thống." />
                    )}
                </DetailCard>
            </DetailPageContainer>
        );
    };

    export default SupplierDetail;