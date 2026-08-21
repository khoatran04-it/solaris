'use client';

import React, { useState, useEffect, Suspense } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Lock, User, ArrowRight, AlertCircle } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import shopAuthApi from '@/api/shopAuthApi';
import { ShopAuthResponse } from '@/types/auth';

function DangNhapContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const redirectUrl = searchParams.get('redirect') || '/';

    const { login, isAuthenticated, initAuth } = useAuthStore();
    const { syncGuestCartOnLogin } = useCartStore();

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
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

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setErrorMsg('');

        try {
            const res = await shopAuthApi.login({ username: username.trim(), password: password.trim() });

            login(res.token, res.customerInfo);
            await syncGuestCartOnLogin();
            router.push(redirectUrl);
        } catch (error: any) {
            const msg = error?.message || error?.Message || error?.details || error?.Details || error?.title || (typeof error === 'string' ? error : 'Tên đăng nhập hoặc mật khẩu không chính xác.');
            setErrorMsg(msg);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="max-w-md w-full bg-white rounded-3xl border border-slate-200 p-8 sm:p-10 shadow-xl space-y-6">
            <div className="text-center space-y-2">
                <div className="w-14 h-14 rounded-2xl bg-emerald-600 text-white flex items-center justify-center text-2xl mx-auto shadow-lg shadow-emerald-600/30">
                    🌿
                </div>
                <h1 className="text-2xl font-black text-slate-900 tracking-tight">
                    Đăng Nhập Khách Hàng
                </h1>
                <p className="text-xs text-slate-500">
                    Trải nghiệm mua sắm nông sản sạch với ưu đãi hội viên
                </p>
            </div>

            {errorMsg && (
                <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-2xl flex items-center gap-2.5 text-rose-700 text-xs font-semibold">
                    <AlertCircle className="w-4 h-4 shrink-0" />
                    <span>{errorMsg}</span>
                </div>
            )}

            <form onSubmit={handleLogin} className="space-y-4">
                <div className="space-y-1.5">
                    <label className="text-xs font-bold text-slate-700 block">Tên đăng nhập hoặc SĐT</label>
                    <div className="relative">
                        <input
                            type="text"
                            placeholder="Nhập username hoặc số điện thoại..."
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="w-full pl-10 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                    </div>
                </div>

                <div className="space-y-1.5">
                    <label className="text-xs font-bold text-slate-700 block">Mật khẩu</label>
                    <div className="relative">
                        <input
                            type="password"
                            placeholder="••••••••"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full pl-10 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-2xl text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500 font-medium"
                            required
                        />
                        <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={isLoading}
                    className="w-full py-3.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-2xl text-xs font-bold transition-all shadow-lg shadow-emerald-600/30 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50"
                >
                    <span>{isLoading ? 'Đang xác thực...' : 'Đăng Nhập Ngay'}</span>
                    <ArrowRight className="w-4 h-4" />
                </button>
            </form>

            <div className="text-center pt-2 border-t border-slate-100 text-xs text-slate-600 space-y-2">
                <p>
                    Chưa có tài khoản?{' '}
                    <Link href={`/dang-ky?redirect=${encodeURIComponent(redirectUrl)}`} className="font-bold text-emerald-600 hover:text-emerald-700">
                        Đăng ký miễn phí
                    </Link>
                </p>
            </div>
        </div>
    );
}

export default function DangNhapPage() {
    return (
        <div className="min-h-[80vh] flex items-center justify-center px-4 py-12">
            <Suspense fallback={<div className="text-xs text-slate-500">Đang tải...</div>}>
                <DangNhapContent />
            </Suspense>
        </div>
    );
}

