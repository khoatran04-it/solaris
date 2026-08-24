import React from 'react';
import { Bell, User, ChevronDown } from 'lucide-react';

export const Header: React.FC = () => {
  return (
    <header className="h-16 bg-white border-b border-slate-100 flex items-center justify-end px-8 sticky top-0 z-40 shadow-[0_1px_2px_rgba(0,0,0,0.03)]">
      <div className="flex items-center gap-8">
        {/* --- KHU VỰC THÔNG BÁO --- */}
        <button className="relative group p-2 rounded-full hover:bg-slate-50 transition-all duration-300 cursor-pointer">
          <Bell
            size={22}
            className="text-slate-400 group-hover:text-amber-500 transition-colors duration-300"
            strokeWidth={2}
          />

          {/* Chấm thông báo với hiệu ứng ping */}
          <span className="absolute top-2 right-2 flex h-2.5 w-2.5">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500 border-2 border-white"></span>
          </span>

          {/* Tooltip nhỏ khi hover */}
          <div className="absolute top-full right-0 mt-2 scale-0 group-hover:scale-100 transition-all duration-200 origin-top-right bg-slate-800 text-white text-[10px] px-2 py-1 rounded font-bold uppercase tracking-wider shadow-lg">
            Thông báo
          </div>
        </button>

        {/* --- KHU VỰC THÔNG TIN NGƯỜI DÙNG --- */}
        <div className="flex items-center gap-4 group cursor-pointer p-1.5 pr-3 rounded-2xl hover:bg-slate-50 transition-all duration-300 border border-transparent hover:border-slate-100">
          {/* Nội dung Text */}
          <div className="flex flex-col items-end">
            <span className="font-black text-[13px] text-slate-800 tracking-tight leading-none group-hover:text-amber-600 transition-colors">
              ĐĂNG KHOA
            </span>
            <div className="flex items-center gap-1.5 mt-1">
              <span className="inline-block w-1 h-1 rounded-full bg-emerald-500"></span>
              <span className="text-[10px] font-bold text-slate-400 uppercase tracking-widest italic leading-none">
                Super Admin
              </span>
            </div>
          </div>

          {/* Avatar Container */}
          <div className="relative">
            <div className="w-10 h-10 rounded-xl border-2 border-slate-100 flex items-center justify-center bg-white shadow-sm group-hover:border-amber-400 group-hover:shadow-md transition-all duration-300 overflow-hidden">
              <div className="bg-slate-800 w-full h-full flex items-center justify-center text-amber-400 group-hover:bg-amber-400 group-hover:text-slate-900 transition-all duration-300">
                <User size={20} strokeWidth={2.5} />
              </div>
            </div>
            {/* Status dot */}
            <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-emerald-500 border-2 border-white rounded-full shadow-sm"></div>
          </div>

          {/* Nút dropdown nhỏ */}
          <ChevronDown
            size={14}
            className="text-slate-300 group-hover:text-slate-500 transition-all duration-300"
          />
        </div>
      </div>
    </header>
  );
};
