'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { MapPin, User, Package, RotateCcw, Plus, Trash2, CheckCircle2, Star } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import shopCustomerApi from '@/api/shopCustomerApi';
import { ShopAddress, ShopAddressPayload } from '@/types/customer';

export default function DiaChiPage() {
    const router = useRouter();
    const { isAuthenticated, initAuth } = useAuthStore();
    const [addresses, setAddresses] = useState<ShopAddress[]>([]);
    const [showAddForm, setShowAddForm] = useState(false);

    // Form
    const [receiverName, setReceiverName] = useState('');
    const [phone, setPhone] = useState('');
    const [province, setProvince] = useState('TP. Hồ Chí Minh');
    const [district, setDistrict] = useState('');
    const [ward, setWard] = useState('');
    const [streetAddress, setStreetAddress] = useState('');
    const [isDefault, setIsDefault] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    const loadAddresses = () => {
        shopCustomerApi.getAddresses()
            .then(setAddresses)
            .catch(() => {});
    };

    useEffect(() => {
        if (!isAuthenticated) {
            router.push('/dang-nhap?redirect=/tai-khoan/dia-chi');
            return;
        }
        loadAddresses();
    }, [isAuthenticated, router]);

    const handleAddAddress = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);

        try {
            await shopCustomerApi.createAddress({
                receiverName: receiverName.trim(),
                phone: phone.trim(),
                province: province.trim(),
                district: district.trim(),
                ward: ward.trim(),
                streetAddress: streetAddress.trim(),
                isDefault,
                latitude: 10.7769,
                longitude: 106.7009
            } as ShopAddressPayload);

            setShowAddForm(false);
            setReceiverName('');
            setPhone('');
            setDistrict('');
            setWard('');
            setStreetAddress('');
            setIsDefault(false);
            loadAddresses();
        } catch (error: any) {
            alert(error?.message || 'Không thể thêm địa chỉ.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (id: number) => {
        if (!confirm('Bạn có chắc muốn xóa địa chỉ này?')) return;
        try {
            await shopCustomerApi.deleteAddress(id);
            loadAddresses();
        } catch (error: any) {
            alert(error?.message || 'Không thể xóa địa chỉ.');
        }
    };

    const handleSetDefault = async (id: number) => {
        try {
            await shopCustomerApi.setDefaultAddress(id);
            loadAddresses();
        } catch (error: any) {
            alert(error?.message || 'Không thể thiết lập địa chỉ mặc định.');
        }
    };

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Sổ Địa Chỉ Nhận Hàng
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Quản lý các địa chỉ giao hàng để thanh toán nhanh chóng hơn
                </p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                
                {/* Sidebar */}
                <div className="lg:col-span-1 bg-white rounded-3xl border border-slate-200 p-5 space-y-2 shadow-xs">
                    <Link
                        href="/tai-khoan"
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                    >
                        <User className="w-4 h-4 text-slate-400" />
                        <span>Hồ sơ cá nhân</span>
                    </Link>

                    <Link
                        href="/tai-khoan/don-hang"
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                    >
                        <Package className="w-4 h-4 text-slate-400" />
                        <span>Lịch sử đơn hàng</span>
                    </Link>

                    <Link
                        href="/tai-khoan/dia-chi"
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-emerald-50 text-emerald-800 font-bold text-xs"
                    >
                        <MapPin className="w-4 h-4 text-emerald-600" />
                        <span>Sổ địa chỉ</span>
                    </Link>

                    <Link
                        href="/tai-khoan/tra-hang"
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                    >
                        <RotateCcw className="w-4 h-4 text-slate-400" />
                        <span>Đổi trả hàng (RMA)</span>
                    </Link>
                </div>

                {/* Content */}
                <div className="lg:col-span-3 space-y-6">
                    <div className="flex items-center justify-between">
                        <h2 className="text-sm font-bold text-slate-900">
                            Danh Sách Địa Chỉ ({addresses.length})
                        </h2>
                        {!showAddForm && (
                            <button
                                onClick={() => setShowAddForm(true)}
                                className="inline-flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition-colors shadow-xs"
                            >
                                <Plus className="w-4 h-4" />
                                <span>Thêm địa chỉ mới</span>
                            </button>
                        )}
                    </div>

                    {showAddForm && (
                        <div className="bg-white rounded-3xl border border-emerald-200 p-6 sm:p-8 shadow-md space-y-4">
                            <h3 className="text-sm font-bold text-slate-900 pb-2 border-b border-slate-100">
                                Thêm Địa Chỉ Nhận Hàng Mới
                            </h3>

                            <form onSubmit={handleAddAddress} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Họ tên người nhận *</label>
                                    <input
                                        type="text"
                                        value={receiverName}
                                        onChange={(e) => setReceiverName(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Số điện thoại *</label>
                                    <input
                                        type="tel"
                                        value={phone}
                                        onChange={(e) => setPhone(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Tỉnh / Thành phố *</label>
                                    <input
                                        type="text"
                                        value={province}
                                        onChange={(e) => setProvince(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Quận / Huyện *</label>
                                    <input
                                        type="text"
                                        placeholder="Quận 1..."
                                        value={district}
                                        onChange={(e) => setDistrict(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-semibold text-slate-700">Phường / Xã</label>
                                    <input
                                        type="text"
                                        placeholder="Phường Bến Nghé..."
                                        value={ward}
                                        onChange={(e) => setWard(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                    />
                                </div>

                                <div className="space-y-1 sm:col-span-2">
                                    <label className="text-xs font-semibold text-slate-700">Địa chỉ cụ thể *</label>
                                    <input
                                        type="text"
                                        placeholder="123 Đường Lê Lợi"
                                        value={streetAddress}
                                        onChange={(e) => setStreetAddress(e.target.value)}
                                        className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl"
                                        required
                                    />
                                </div>

                                <div className="sm:col-span-2 flex items-center gap-2">
                                    <input
                                        type="checkbox"
                                        id="isDefault"
                                        checked={isDefault}
                                        onChange={(e) => setIsDefault(e.target.checked)}
                                        className="w-4 h-4 text-emerald-600 rounded"
                                    />
                                    <label htmlFor="isDefault" className="text-xs text-slate-700 font-medium">
                                        Đặt làm địa chỉ nhận hàng mặc định
                                    </label>
                                </div>

                                <div className="sm:col-span-2 flex items-center gap-3 pt-2">
                                    <button
                                        type="submit"
                                        disabled={isSaving}
                                        className="px-6 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold transition-colors"
                                    >
                                        {isSaving ? 'Đang lưu...' : 'Lưu địa chỉ'}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setShowAddForm(false)}
                                        className="px-4 py-2.5 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold"
                                    >
                                        Hủy
                                    </button>
                                </div>
                            </form>
                        </div>
                    )}

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        {addresses.map((addr) => (
                            <div
                                key={addr.id}
                                className={`bg-white rounded-3xl border p-6 shadow-xs flex flex-col justify-between space-y-4 ${
                                    addr.isDefault ? 'border-emerald-300 ring-1 ring-emerald-300' : 'border-slate-200'
                                }`}
                            >
                                <div className="space-y-2">
                                    <div className="flex items-center justify-between">
                                        <h4 className="font-bold text-xs text-slate-900">{addr.receiverName}</h4>
                                        {addr.isDefault && (
                                            <span className="px-2 py-0.5 bg-emerald-100 text-emerald-800 text-[10px] font-bold rounded-full">
                                                Mặc định
                                            </span>
                                        )}
                                    </div>
                                    <p className="text-xs text-slate-600 font-medium">{addr.phone}</p>
                                    <p className="text-xs text-slate-500 leading-relaxed">
                                        {addr.streetAddress}, {addr.ward}, {addr.district}, {addr.province}
                                    </p>
                                </div>

                                <div className="pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
                                    {!addr.isDefault ? (
                                        <button
                                            onClick={() => handleSetDefault(addr.id)}
                                            className="text-emerald-700 hover:text-emerald-800 font-semibold text-[11px]"
                                        >
                                            Đặt làm mặc định
                                        </button>
                                    ) : <div />}

                                    <button
                                        onClick={() => handleDelete(addr.id)}
                                        className="p-1.5 text-slate-400 hover:text-rose-600 rounded-lg transition-colors"
                                    >
                                        <Trash2 className="w-4 h-4" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

            </div>

        </div>
    );
}

