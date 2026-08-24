import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../stores/useAuthStore';
import axiosClient from '../../api/axiosClient';
import { Sun, User, Lock, Loader2, AlertCircle } from 'lucide-react';

const LoginPage = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useNavigate();
  const { login } = useAuthStore();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const response: any = await axiosClient.post('/auth/login', {
        username: username.trim(),
        password: password.trim(),
      });

      login(response.token, response.userInfo);
      navigate('/');
    } catch (err: any) {
      const serverMsg = err.response?.data?.details || err.response?.data?.message;
      setError(
        serverMsg ||
          (err.message === 'Network Error'
            ? 'Không thể kết nối đến máy chủ backend (Network Error).'
            : 'Đăng nhập thất bại. Vui lòng kiểm tra lại tài khoản hoặc mật khẩu!')
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="relative min-h-screen w-full flex items-center justify-center font-sans overflow-hidden bg-slate-900">
      {/* LỚP NỀN (BACKGROUND EFFECTS) */}
      <div className="absolute inset-0 z-0">
        {/* Gradient nền chính */}
        <div className="absolute inset-0 bg-linear-to-br from-slate-900 via-slate-800 to-amber-900/30" />

        {/* Abstract Blobs (Các khối sáng mờ ảo) */}
        <div className="absolute top-[-20%] left-[-10%] w-160 h-160 bg-amber-500/20 rounded-full mix-blend-screen filter blur-[100px] animate-pulse" />
        <div
          className="absolute bottom-[-20%] right-[-10%] w-140 h-140 bg-amber-400/20 rounded-full mix-blend-screen filter blur-[100px] animate-pulse"
          style={{ animationDelay: '2s' }}
        />

        {/* Lớp phủ họa tiết mềm */}
        <div className="absolute inset-0 bg-radial from-transparent to-black/40 pointer-events-none" />
      </div>

      {/* LỚP NỔI: FORM ĐĂNG NHẬP */}
      <div className="relative z-10 w-full max-w-105 p-6">
        {/* Branding phía trên Form */}
        <div className="flex flex-col items-center mb-8">
          <div className="bg-amber-400 p-3 rounded-xl mb-4 shadow-[0_0_30px_rgba(251,191,36,0.3)]">
            <Sun size={36} className="text-white fill-white" />
          </div>
          <h1 className="text-3xl font-black tracking-[0.2em] text-white italic">SOLARIS</h1>
          <p className="text-slate-400 mt-2 text-sm font-medium">
            Hệ Thống Quản Trị Chuỗi Cung Ứng Nông Sản
          </p>
        </div>

        {/* Khung Form (Glassmorphism Effect) */}
        <div className="bg-white/95 backdrop-blur-xl p-8 rounded-3xl shadow-[0_20px_60px_-15px_rgba(0,0,0,0.5)] border border-white/20">
          <h2 className="text-xl font-bold text-slate-800 text-center mb-6">Đăng nhập tài khoản</h2>

          {error && (
            <div className="mb-6 flex items-start gap-3 bg-red-50 p-4 rounded-xl border border-red-100 animate-in fade-in slide-in-from-top-2">
              <AlertCircle size={20} className="text-red-500 shrink-0 mt-0.5" />
              <p className="text-sm text-red-600 font-medium leading-relaxed">{error}</p>
            </div>
          )}

          <form onSubmit={handleLogin} className="space-y-5">
            {/* Input Tài khoản */}
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-1.5">Tài khoản</label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                  <User size={18} className="text-slate-400" />
                </div>
                <input
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  placeholder="Nhập tên tài khoản..."
                  className="w-full pl-10 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl text-slate-800 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500 transition-all placeholder:text-slate-400 font-medium"
                />
              </div>
            </div>

            {/* Input Mật khẩu */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="block text-sm font-semibold text-slate-700">Mật khẩu</label>
              </div>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none">
                  <Lock size={18} className="text-slate-400" />
                </div>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  placeholder="Nhập mật khẩu..."
                  className="w-full pl-10 pr-4 py-3 bg-slate-50 border border-slate-200 rounded-xl text-slate-800 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500 transition-all placeholder:text-slate-400 font-medium"
                />
              </div>
            </div>

            {/* Quên mật khẩu (Chuyển xuống dưới input cho gọn) */}
            <div className="flex justify-end">
              <a
                href="#"
                className="text-sm font-semibold text-amber-600 hover:text-amber-700 transition-colors"
              >
                Quên mật khẩu?
              </a>
            </div>

            {/* Nút Submit */}
            <button
              type="submit"
              disabled={isLoading}
              className={`w-full flex items-center justify-center gap-2 py-3 px-4 bg-amber-500 hover:bg-amber-600 text-white rounded-xl font-bold text-sm transition-all duration-300 shadow-[0_8px_20px_-6px_rgba(245,158,11,0.6)] mt-4
                                ${isLoading ? 'opacity-75 cursor-not-allowed' : 'hover:-translate-y-0.5 hover:shadow-[0_12px_24px_-6px_rgba(245,158,11,0.7)]'}`}
            >
              {isLoading ? (
                <>
                  <Loader2 size={18} className="animate-spin" />
                  <span>Đang xác thực...</span>
                </>
              ) : (
                <span>Đăng Nhập</span>
              )}
            </button>
          </form>
        </div>

        {/* Copyright/Footer Info */}
        <p className="text-center text-slate-500 text-xs mt-8">
          © 2026 Solaris Platform. All rights reserved.
        </p>
      </div>
    </div>
  );
};

export default LoginPage;
