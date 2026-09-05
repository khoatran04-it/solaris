"use client";

import React, { useState, useEffect, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import {
  Lock,
  User,
  Phone,
  Mail,
  ArrowRight,
  AlertCircle,
  Sparkles,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import shopAuthApi from "@/api/shopAuthApi";

function DangKyContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectUrl = searchParams.get("redirect") || "/";

  const { login, isAuthenticated, initAuth } = useAuthStore();
  const { syncGuestCartOnLogin } = useCartStore();

  const [name, setName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [email, setEmail] = useState("");

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

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
    setErrorMsg("");

    if (password !== confirmPassword) {
      setErrorMsg("Mật khẩu xác nhận không khớp.");
      return;
    }

    if (password.length < 6) {
      setErrorMsg("Mật khẩu phải có tối thiểu 6 ký tự.");
      return;
    }

    setIsLoading(true);

    try {
      const res = await shopAuthApi.register({
        fullName: name.trim(),
        phoneNumber: phoneNumber.trim(),
        email: email.trim() || undefined,
        password: password.trim(),
      });

      login(res.token, res.customerInfo);
      await syncGuestCartOnLogin();
      router.push(redirectUrl);
    } catch (error: any) {
      const msg =
        error?.message ||
        error?.Message ||
        error?.details ||
        error?.Details ||
        error?.title ||
        (typeof error === "string"
          ? error
          : "Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin.");
      setErrorMsg(msg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="max-w-md w-full bg-white rounded-3xl shadow-[0_10px_35px_-5px_rgba(0,0,0,0.06)] border border-slate-200/80 p-8 sm:p-9 space-y-6">
      <div className="text-center space-y-2.5">
        <div className="w-14 h-14 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center mx-auto shadow-2xs border border-emerald-100/60">
          <Sparkles className="w-7 h-7 text-amber-500" />
        </div>
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight">
            Tạo Tài Khoản Mới
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Đăng ký thành viên để nhận ưu đãi tích điểm và giảm giá
          </p>
        </div>
      </div>

      {errorMsg && (
        <div className="flex items-center gap-2 p-3 bg-rose-50 border border-rose-200 rounded-2xl text-rose-700 text-xs font-semibold animate-in fade-in">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{errorMsg}</span>
        </div>
      )}

      <form onSubmit={handleRegister} className="space-y-3.5">
        <div className="space-y-1.5">
          <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
            Họ và tên *
          </label>
          <div className="relative">
            <input
              type="text"
              placeholder="Nguyễn Văn A"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full pl-11 pr-4 h-11 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
              required
            />
            <User className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
          </div>
        </div>

        <div className="space-y-1.5">
          <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
            Số điện thoại *
          </label>
          <div className="relative">
            <input
              type="tel"
              placeholder="0912345678"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              className="w-full pl-11 pr-4 h-11 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
              required
            />
            <Phone className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
          </div>
        </div>

        <div className="space-y-1.5">
          <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
            Email (Tùy chọn)
          </label>
          <div className="relative">
            <input
              type="email"
              placeholder="email@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full pl-11 pr-4 h-11 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
            />
            <Mail className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
          </div>
        </div>

        <div className="space-y-1.5">
          <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
            Mật khẩu (tối thiểu 6 ký tự) *
          </label>
          <div className="relative">
            <input
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full pl-11 pr-4 h-11 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
              required
            />
            <Lock className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
          </div>
        </div>

        <div className="space-y-1.5">
          <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
            Xác nhận mật khẩu *
          </label>
          <div className="relative">
            <input
              type="password"
              placeholder="••••••••"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="w-full pl-11 pr-4 h-11 rounded-2xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 placeholder-slate-400 font-medium"
              required
            />
            <Lock className="w-4 h-4 text-slate-400 absolute left-4 top-3.5" />
          </div>
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="w-full h-12 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl font-extrabold text-xs transition-all shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 active:scale-98 disabled:opacity-50 cursor-pointer pt-1"
        >
          <span>
            {isLoading ? "Đang tạo tài khoản..." : "Hoàn Tất Đăng Ký"}
          </span>
          <ArrowRight className="w-4 h-4" />
        </button>
      </form>

      <div className="text-center pt-3 border-t border-slate-100 text-xs text-slate-600">
        <p>
          Đã có tài khoản?{" "}
          <Link
            href={`/dang-nhap?redirect=${encodeURIComponent(redirectUrl)}`}
            className="text-emerald-700 hover:text-emerald-800 font-bold underline"
          >
            Đăng nhập ngay
          </Link>
        </p>
      </div>
    </div>
  );
}

export default function DangKyPage() {
  return (
    <div className="min-h-[80vh] flex items-center justify-center px-4 py-12 bg-slate-50/50">
      <Suspense
        fallback={
          <div className="text-xs text-slate-500">
            Đang tải trang đăng ký...
          </div>
        }
      >
        <DangKyContent />
      </Suspense>
    </div>
  );
}
