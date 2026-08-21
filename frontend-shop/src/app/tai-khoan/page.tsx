'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { User, Package, MapPin, RotateCcw, Award, CheckCircle2, ShieldCheck, Phone, Mail, Calendar } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { apiClient } from '@/lib/api';

export default function TaiKhoanPage() {
    const router = useRouter();
    const { user, isAuthenticated, initAuth, updateUser } = useAuthStore();

    const [name, setName] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [email, setEmail] = useState('');
    const [gender, setGender] = useState('');
    const [isSaving, setIsSaving] = useState(false);
    const [saveMessage, setSaveMessage] = useState('');

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    useEffect(() => {
        if (!isAuthenticated) {
            router.push('/dang-nhap?redirect=/tai-khoan');
            return;
        }

        apiClient.get<any>('/customer/profile')
            .then((prof) => {
                setName(prof.name || '');
                setPhoneNumber(prof.phoneNumber || '');
                setEmail(prof.email || '');
                setGender(prof.gender || '');
            })
            .catch(() => {});
    }, [isAuthenticated, router]);

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        setSaveMessage('');

        try {
            const updated = await apiClient.put<any>('/customer/profile', {
                name,
                phoneNumber,
                email,
                gender
            });

            if (user) {
                updateUser({
                    ...user,
                    name: updated.name,
                    phoneNumber: updated.phoneNumber,
                    email: updated.email
                });
            }

            setSaveMessage('Đã cập nhật thông tin thành công!');
            setTimeout(() => setSaveMessage(''), 3000);
        } catch (error: any) {
            alert(error?.message || 'Không thể cập nhật hồ sơ.');
        } finally {
            setIsSaving(false);
        }
    };

    if (!user) return null;

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
            
            {/* Header */}
            <div>
                <h1 className="text-2xl sm:text-3xl font-black text-slate-900">
                    Hồ Sơ Của Bạn
                </h1>
                <p className="text-xs text-slate-500 mt-1">
                    Quản lý thông tin cá nhân và xem các đặc quyền hội viên
                </p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                
                {/* Navigation Sidebar */}
                <div className="lg:col-span-1 bg-white rounded-3xl border border-slate-200 p-5 space-y-2 shadow-xs">
                    <Link
                        href="/tai-khoan"
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-emerald-50 text-emerald-800 font-bold text-xs"
                    >
                        <User className="w-4 h-4 text-emerald-600" />
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
                        className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-600 hover:bg-slate-50 font-medium text-xs transition-colors"
                    >
                        <MapPin className="w-4 h-4 text-slate-400" />
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

                {/* Main Content Area */}
                <div className="lg:col-span-3 space-y-6">
                    
                    {/* Membership Tier Banner */}
                    <div className="bg-gradient-to-r from-emerald-700 via-teal-700 to-slate-900 text-white rounded-3xl p-6 sm:p-8 shadow-md flex flex-col sm:flex-row sm:items-center justify-between gap-6">
                        <div className="flex items-center gap-4">
                            <div className="w-16 h-16 rounded-2xl bg-white/10 backdrop-blur-md flex items-center justify-center text-3xl shadow-inner border border-white/20">
                                🎖️
                            </div>
                            <div>
                                <span className="px-2.5 py-0.5 bg-emerald-400/30 text-emerald-200 text-[10px] font-extrabold rounded-full uppercase">
                                    Hạng thành viên
                                </span>
                                <h3 className="text-xl font-black mt-1">
                                    {user.customerTierName || 'Thành Viên Mới'}
                                </h3>
                                <p className="text-xs text-emerald-100/80 mt-0.5">
                                    Mã khách hàng: <strong className="text-white">{user.code}</strong>
                                </p>
                            </div>
                        </div>

                        {user.discountPercent > 0 && (
                            <div className="bg-white/10 border border-white/20 backdrop-blur-xs px-5 py-3 rounded-2xl text-center sm:text-right">
                                <span className="text-[10px] text-emerald-200 uppercase font-bold block">Ưu đãi giảm giá</span>
                                <span className="text-2xl font-black text-amber-300">-{user.discountPercent}%</span>
                                <span className="text-[10px] text-emerald-100 block">mọi đơn hàng</span>
                            </div>
                        )}
                    </div>

                    {/* Profile Form */}
                    <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs space-y-6">
                        <h2 className="text-sm font-bold text-slate-900 pb-3 border-b border-slate-100">
                            Cập Nhật Thông Tin Cá Nhân
                        </h2>

                        {saveMessage && (
                            <div className="p-3 bg-emerald-50 border border-emerald-200 rounded-xl text-emerald-700 text-xs font-semibold flex items-center gap-2">
                                <CheckCircle2 className="w-4 h-4" />
                                <span>{saveMessage}</span>
                            </div>
                        )}

                        <form onSubmit={handleSave} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                            <div className="space-y-1">
                                <label className="text-xs font-semibold text-slate-700">Họ và tên *</label>
                                <input
                                    type="text"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                    required
                                />
                            </div>

                            <div className="space-y-1">
                                <label className="text-xs font-semibold text-slate-700">Số điện thoại *</label>
                                <input
                                    type="tel"
                                    value={phoneNumber}
                                    onChange={(e) => setPhoneNumber(e.target.value)}
                                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                    required
                                />
                            </div>

                            <div className="space-y-1">
                                <label className="text-xs font-semibold text-slate-700">Địa chỉ Email</label>
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                />
                            </div>

                            <div className="space-y-1">
                                <label className="text-xs font-semibold text-slate-700">Giới tính</label>
                                <select
                                    value={gender}
                                    onChange={(e) => setGender(e.target.value)}
                                    className="w-full text-xs p-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:ring-2 focus:ring-emerald-500"
                                >
                                    <option value="">Chọn giới tính</option>
                                    <option value="Nam">Nam</option>
                                    <option value="Nữ">Nữ</option>
                                    <option value="Khác">Khác</option>
                                </select>
                            </div>

                            <div className="sm:col-span-2 pt-4">
                                <button
                                    type="submit"
                                    disabled={isSaving}
                                    className="px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs rounded-xl transition-all shadow-md active:scale-98 disabled:opacity-50"
                                >
                                    {isSaving ? 'Đang lưu...' : 'Lưu Thay Đổi'}
                                </button>
                            </div>
                        </form>
                    </div>

                </div>

            </div>

        </div>
    );
}
