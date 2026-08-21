'use client';

import React, { useState, useEffect, Suspense } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Lock, User, Phone, Mail, ArrowRight, AlertCircle } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import shopAuthApi from '@/api/shopAuthApi';
import { ShopAuthResponse } from '@/types/auth';

function DangKyContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const redirectUrl = searchParams.get('redirect') || '/';

    const { login, isAuthenticated, initAuth } = useAuthStore();
    const { syncGuestCartOnLogin } = useCartStore();

    const [name, setName] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');
    const [email, setEmail] = useState('');
    
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [errorMsg, setErrorMsg] = useState('');

    useEffect(() => {
        initAuth();
    }, [initAuth]);

    useEffect(() => {
        if (isAuthenticated) {
            router.push(redirectUrl);
        }
    }, [isAuthenticated, redirectUrl, router]);

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrorMsg('');

        if (password !== confirmPassword) {
            setErrorMsg('Mật khẩu xác nhận không khớp.');
            return;
        }

        if (password.length < 6) {
            setErrorMsg('Mật khẩu phải có tối thiểu 6 ký tự.');
            return;
        }

        setIsLoading(true);

        try {
            const res = await shopAuthApi.register({
                fullName: name.trim(),
                phoneNumber: phoneNumber.trim(),
                email: email.trim() || undefined,
                password: password.trim()
            });

            login(res.token, res.customerInfo);
            await syncGuestCartOnLogin();
            router.push(redirectUrl);
        } catch (error: any) {
            const msg = error?.message || error?.Message || error?.details || error?.Details || error?.title || (typeof error === 'string' ? error : 'Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin.');
            setErrorMsg(msg);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="max-w-md w-full bg-white rounded-3xl border border-slate-200 p-8 sm:p-10 shadow-xl space-y-6">
            <div className="text-center space-y-2">
                <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-emerald-500 to-teal-600 text-white flex items-center justify-center text-2xl mx-auto shadow-lg shadow-emerald-600/30">
                    🌱
                </div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">
                    Tạo Tài Khoản Mới
                </h1>
                <p className="text-xs text-slate-500">
                    Đăng ký thành viên để nhận ưu đãi tích điểm và giảm giá
                </p>
            </div>

            {errorMsg && (
                <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-2xl flex items-center gap-2.5 text-rose-700 text-xs font-semibold">
                    <AlertCircle className="w-4 h-4 shrink-0" />
                    <span>{errorMsg}</span>
                </div>
            )}

            <form onSubmit={handleRegister} className="space-y-3.5">
                <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700 block">Họ và tên *</label>
                    <div className="relative">
                        <input
                            type="text"
                            placeholder="Nguyễn Văn A"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    </div>
                </div>

                <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700 block">Số điện thoại *</label>
                    <div className="relative">
                        <input
                            type="tel"
                            placeholder="0912345678"
                            value={phoneNumber}
                            onChange={(e) => setPhoneNumber(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <Phone className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    </div>
                </div>

                <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700 block">Email (Tùy chọn)</label>
                    <div className="relative">
                        <input
                            type="email"
                            placeholder="email@example.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:ring-2 focus:ring-emerald-500 font-medium"
                        />
                        <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    </div>
                </div>



                <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700 block">Mật khẩu (tối thiểu 6 ký tự) *</label>
                    <div className="relative">
                        <input
                            type="password"
                            placeholder="••••••••"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    </div>
                </div>

                <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700 block">Xác nhận mật khẩu *</label>
                    <div className="relative">
                        <input
                            type="password"
                            placeholder="••••••••"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            className="w-full pl-10 pr-4 py-2.5 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={isLoading}
                    className="w-full py-3.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-2xl text-xs font-bold transition-all shadow-lg shadow-emerald-600/30 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50 pt-2"
                >
                    <span>{isLoading ? 'Đang khởi tạo tài khoản...' : 'Hoàn Tất Đăng Ký'}</span>
                    <ArrowRight className="w-4 h-4" />
                </button>
            </form>

            <div className="text-center pt-2 border-t border-slate-100 text-xs text-slate-600">
                <p>
                    Đã có tài khoản?{' '}
                    <Link href={`/dang-nhap?redirect=${encodeURIComponent(redirectUrl)}`} className="font-bold text-emerald-600 hover:text-emerald-700">
                        Đăng nhập ngay
                    </Link>
                </p>
            </div>
        </div>
    );
}

export default function DangKyPage() {
    return (
        <div className="min-h-[85vh] flex items-center justify-center px-4 py-12">
            <Suspense fallback={<div className="text-xs text-slate-500">Đang tải...</div>}>
                <DangKyContent />
            </Suspense>
        </div>
    );
}


