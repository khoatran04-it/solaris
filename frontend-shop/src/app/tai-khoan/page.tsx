'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { User, Package, MapPin, RotateCcw, CheckCircle2, Award, Sparkles, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import shopCustomerApi from '@/api/shopCustomerApi';

export default function TaiKhoanPage() {
    const router = useRouter();
    const { user, isAuthenticated, initAuth, updateUser } = useAuthStore();

    const [profile, setProfile] = useState<any>(null);
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

        shopCustomerApi.getProfile()
            .then((prof: any) => {
                setProfile(prof);
                setName(prof.name || '');
                setPhoneNumber(prof.phoneNumber || '');
                setEmail(prof.email || '');
                setGender(prof.gender === true ? 'Nam' : prof.gender === false ? 'Nữ' : '');
            })
            .catch(() => {});
    }, [isAuthenticated, router]);

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSaving(true);
        setSaveMessage('');

        try {
            const genderValue: boolean | null | undefined = gender === 'Nam' ? true : gender === 'Nữ' ? false : null;
            const updated: any = await shopCustomerApi.updateProfile({
                name,
                phoneNumber,
                email,
                gender: genderValue
            });

            if (user) {
                updateUser({
                    ...user,
                    name: updated.name,
                    phoneNumber: updated.phoneNumber,
                    email: updated.email
                });
            }

            setSaveMessage('Đã cập nhật thông tin hồ sơ thành công!');
            setTimeout(() => setSaveMessage(''), 3000);
        } catch (error: any) {
            alert(error?.message || 'Không thể cập nhật hồ sơ.');
        } finally {
            setIsSaving(false);
        }
    };

    if (!user) return null;

    return (
        <div className="min-h-[85vh] bg-slate-50/40">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
                
                {/* Header */}
                <div>
                    <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
                        Hồ Sơ Của Bạn
                    </h1>
                    <p className="text-xs text-slate-500 mt-1 font-medium">
                        Quản lý thông tin tài khoản và theo dõi tiến trình ưu đãi thành viên
                    </p>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
                    
                    {/* Navigation Sidebar */}
                    <div className="lg:col-span-4 bg-white rounded-3xl border border-slate-200/80 p-5 space-y-1.5 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)]">
                        <Link
                            href="/tai-khoan"
                            className="flex items-center gap-3 px-4 py-3 rounded-2xl bg-emerald-600 text-white font-extrabold text-xs shadow-xs"
                        >
                            <User className="w-4 h-4" />
                            <span>Hồ sơ cá nhân & Thẻ VIP</span>
                        </Link>

                        <Link
                            href="/tai-khoan/don-hang"
                            className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-bold text-xs transition-colors"
                        >
                            <Package className="w-4 h-4 text-slate-400" />
                            <span>Lịch sử đơn hàng</span>
                        </Link>

                        <Link
                            href="/tai-khoan/dia-chi"
                            className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-bold text-xs transition-colors"
                        >
                            <MapPin className="w-4 h-4 text-slate-400" />
                            <span>Sổ địa chỉ nhận hàng</span>
                        </Link>

                        <Link
                            href="/tai-khoan/tra-hang"
                            className="flex items-center gap-3 px-4 py-3 rounded-2xl text-slate-700 hover:bg-emerald-50 hover:text-emerald-800 font-bold text-xs transition-colors"
                        >
                            <RotateCcw className="w-4 h-4 text-slate-400" />
                            <span>Yêu cầu đổi trả nông sản (RMA)</span>
                        </Link>
                    </div>

                    {/* Main Content Area */}
                    <div className="lg:col-span-8 space-y-6">
                        
                        {/* Membership Tier VIP Card */}
                        <div className="bg-gradient-to-br from-emerald-900 via-emerald-800 to-teal-950 text-white rounded-3xl p-6 sm:p-8 shadow-xl space-y-6 relative overflow-hidden">
                            <div className="absolute top-0 right-0 w-80 h-80 bg-white/5 rounded-full blur-3xl pointer-events-none" />

                            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-6 relative z-10">
                                <div className="flex items-center gap-4">
                                    <div className="w-16 h-16 rounded-2xl bg-white/10 backdrop-blur-md flex items-center justify-center text-amber-300 shadow-inner border border-white/20 shrink-0">
                                        <Award className="w-8 h-8" />
                                    </div>
                                    <div>
                                        <div className="flex items-center gap-2">
                                            <span className="px-2.5 py-0.5 bg-emerald-400/30 text-emerald-200 text-[10px] font-extrabold rounded-full uppercase tracking-wider">
                                                Hạng thành viên
                                            </span>
                                            {profile?.discountPercent > 0 && (
                                                <span className="px-2 py-0.5 bg-amber-400 text-slate-950 text-[10px] font-black rounded-md">
                                                    Giảm -{profile.discountPercent}%
                                                </span>
                                            )}
                                        </div>
                                        <h3 className="text-xl font-black mt-1 tracking-tight">
                                            {profile?.customerTierName || user.customerTierName || 'Thành Viên Mới'}
                                        </h3>
                                        <p className="text-xs text-emerald-100/80 mt-0.5">
                                            Mã khách hàng: <strong className="text-white font-mono">{user.code}</strong>
                                        </p>
                                    </div>
                                </div>

                                <div className="bg-white/10 border border-white/20 backdrop-blur-xs px-5 py-3 rounded-2xl flex items-center gap-6 sm:text-right shrink-0">
                                    <div>
                                        <span className="text-[10px] text-emerald-200 uppercase font-bold block">Tổng chi tiêu</span>
                                        <span className="text-lg font-black text-amber-300">
                                            {(profile?.totalSpent || 0).toLocaleString('vi-VN')} ₫
                                        </span>
                                    </div>
                                    <div className="border-l border-white/20 pl-4">
                                        <span className="text-[10px] text-emerald-200 uppercase font-bold block">Đơn hoàn tất</span>
                                        <span className="text-lg font-black text-white">
                                            {profile?.totalOrders || 0} đơn
                                        </span>
                                    </div>
                                </div>
                            </div>

                            {/* Loyalty Progress Bar */}
                            <div className="pt-4 border-t border-white/10 space-y-2 relative z-10">
                                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1 text-xs">
                                    <span className="text-emerald-100 font-medium">
                                        {profile?.nextTierName ? (
                                            <>
                                                Mua thêm <strong className="text-amber-300 font-black">{(profile.amountToNextTier || 0).toLocaleString('vi-VN')} ₫</strong> để thăng cấp lên <strong className="text-white font-extrabold">{profile.nextTierName}</strong>
                                            </>
                                        ) : (
                                            <span className="text-amber-300 font-bold flex items-center gap-1.5">
                                                <Sparkles className="w-3.5 h-3.5" />
                                                Bạn đã đạt thứ hạng thành viên VIP cao nhất của Solaris!
                                            </span>
                                        )}
                                    </span>
                                    <span className="text-emerald-200 font-bold text-[11px] self-end sm:self-auto">
                                        Tiến độ: {profile?.tierProgressPercent || 0}%
                                    </span>
                                </div>
                                <div className="w-full h-2.5 bg-white/20 rounded-full overflow-hidden p-0.5 backdrop-blur-xs">
                                    <div
                                        className="h-full bg-gradient-to-r from-amber-400 to-emerald-300 rounded-full transition-all duration-500 shadow-sm"
                                        style={{ width: `${Math.min(100, Math.max(0, profile?.tierProgressPercent || 0))}%` }}
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Profile Form */}
                        <div className="bg-white rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] border border-slate-200/80 p-6 sm:p-8 space-y-6">
                            <div className="flex items-center gap-2 pb-3.5 border-b border-slate-100">
                                <div className="w-1.5 h-4 bg-emerald-600 rounded-full"></div>
                                <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider">
                                    Cập Nhật Thông Tin Cá Nhân
                                </h2>
                            </div>

                            {saveMessage && (
                                <div className="p-3.5 bg-emerald-50 border border-emerald-200 rounded-2xl text-emerald-800 text-xs font-bold flex items-center gap-2 animate-in fade-in">
                                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                                    <span>{saveMessage}</span>
                                </div>
                            )}

                            <form onSubmit={handleSave} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <div className="space-y-1">
                                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Họ và tên *</label>
                                    <input
                                        type="text"
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Số điện thoại *</label>
                                    <input
                                        type="tel"
                                        value={phoneNumber}
                                        onChange={(e) => setPhoneNumber(e.target.value)}
                                        className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                                        required
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Địa chỉ Email</label>
                                    <input
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                                    />
                                </div>

                                <div className="space-y-1">
                                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Giới tính</label>
                                    <select
                                        value={gender}
                                        onChange={(e) => setGender(e.target.value)}
                                        className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                                    >
                                        <option value="">Chọn giới tính</option>
                                        <option value="Nam">Nam</option>
                                        <option value="Nữ">Nữ</option>
                                        <option value="Khác">Khác</option>
                                    </select>
                                </div>

                                <div className="sm:col-span-2 pt-3">
                                    <button
                                        type="submit"
                                        disabled={isSaving}
                                        className="px-6 h-11 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-extrabold text-xs rounded-2xl transition-all shadow-md shadow-emerald-600/20 active:scale-98 disabled:opacity-50 cursor-pointer"
                                    >
                                        {isSaving ? 'Đang lưu...' : 'Lưu Thay Đổi'}
                                    </button>
                                </div>
                            </form>
                        </div>

                    </div>

                </div>

            </div>
        </div>
    );
}
