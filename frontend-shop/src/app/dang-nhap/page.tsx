'use client';

import React, { useState, useEffect, Suspense } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Lock, User, ArrowRight, AlertCircle, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import shopAuthApi from '@/api/shopAuthApi';

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
        <div className="max-w-md w-full bg-white rounded-3xl shadow-[0_10px_35px_-5px_rgba(0,0,0,0.06)] border border-slate-200/80 p-8 sm:p-9 space-y-7">
            <div className="text-center space-y-2.5">
                <div className="w-14 h-14 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center mx-auto shadow-2xs border border-emerald-100/60">
                    <ShieldCheck className="w-7 h-7" />
                </div>
                <div>
                    <h1 className="text-2xl font-black text-slate-900 tracking-tight">
                        Đăng Nhập Khách Hàng
                    </h1>
                    <p className="text-xs text-slate-500 mt-1 font-medium">
                        Mua sắm nông sản sạch với ưu đãi tích điểm hội viên
                    </p>
                </div>
            </div>

            {errorMsg && (
                <div className="flex items-center gap-2 p-3 bg-rose-50 border border-rose-200 rounded-2xl text-rose-700 text-xs font-semibold animate-in fade-in">
                    <AlertCircle className="w-4 h-4 shrink-0" />
                    <span>{errorMsg}</span>
                </div>
            )}

            <form onSubmit={handleLogin} className="space-y-4">
                <div className="space-y-1.5">
                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Tên đăng nhập hoặc SĐT</label>
                    <div className="relative">
                        <input
                            type="text"
                            placeholder="Nhập username hoặc số điện thoại..."
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="w-full pl-11 pr-4 h-12 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
                            required
                        />
                        <User className="w-4 h-4 text-slate-400 absolute left-4 top-4" />
                    </div>
                </div>

                <div className="space-y-1.5">
                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">Mật khẩu</label>
                    <div className="relative">
                        <input
                            type="password"
                            placeholder="••••••••"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full pl-11 pr-4 h-12 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
                            required
                        />
                        <Lock className="w-4 h-4 text-slate-400 absolute left-4 top-4" />
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={isLoading}
                    className="w-full h-12 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl font-extrabold text-xs transition-all shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50 cursor-pointer"
                >
                    <span>{isLoading ? 'Đang xác thực tài khoản...' : 'Đăng Nhập Ngay'}</span>
                    <ArrowRight className="w-4 h-4" />
                </button>
            </form>

            <div className="text-center pt-3 border-t border-slate-100 text-xs text-slate-600 space-y-2">
                <p>
                    Chưa có tài khoản?{' '}
                    <Link href={`/dang-ky?redirect=${encodeURIComponent(redirectUrl)}`} className="text-emerald-700 hover:text-emerald-800 font-bold underline">
                        Đăng ký thành viên mới
                    </Link>
                </p>
            </div>
        </div>
    );
}

export default function DangNhapPage() {
    return (
        <div className="min-h-[75vh] flex items-center justify-center px-4 py-12 bg-slate-50/50">
            <Suspense fallback={<div className="text-xs text-slate-500">Đang tải trang đăng nhập...</div>}>
                <DangNhapContent />
            </Suspense>
        </div>
    );
}
