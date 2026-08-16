import React from 'react';
import { ChevronLeft, LucideIcon } from 'lucide-react';

// 1. Container bọc toàn bộ trang chi tiết
export const DetailPageContainer: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className="h-full flex flex-col p-6 bg-slate-50/30 overflow-y-auto font-sans">
        <div className="max-w-6xl w-full mx-auto flex flex-col gap-6 pb-12">
            {children}
        </div>
    </div>
);

// 2. Header dùng riêng cho trang Detail (Đồng bộ cấu trúc FormHeader và ListHeader)
interface DetailHeaderProps {
    title: string;
    subtitle: string | React.ReactNode;
    onBack: () => void;
    icon: LucideIcon;
}
export const DetailHeader: React.FC<DetailHeaderProps> = ({ title, subtitle, onBack, icon: Icon }) => (
    <div className="flex justify-between items-end mb-2">
        <div>
            <div className="flex items-center gap-2 mb-1.5">
                <Icon className="w-6 h-6 text-yellow-500 fill-yellow-100" />
                <h2 className="m-0 font-bold text-3xl text-slate-800 tracking-tight">{title}</h2>
            </div>
            <div className="text-slate-500 text-sm font-medium ml-8">{subtitle}</div>
        </div>
        <button 
            type="button" 
            onClick={onBack} 
            className="group bg-white border border-slate-200 text-slate-600 rounded-xl px-5 py-2.5 text-sm font-bold flex items-center gap-2 hover:bg-slate-50 hover:text-slate-900 transition-all duration-300 shadow-sm"
        >
            <ChevronLeft size={18} strokeWidth={2.5} className="transition-transform group-hover:-translate-x-1" />
            QUAY LẠI
        </button>
    </div>
);

// 3. Khung điều hướng ngang (Segmented Control Container)
export const TabGroup: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className="bg-slate-200/50 p-1.5 rounded-2xl inline-flex w-fit gap-1 shadow-inner select-none shrink-0">
        {children}
    </div>
);

// 4. Nút bấm Tab thành phần
interface TabButtonProps {
    active: boolean;
    onClick: () => void;
    label: string;
    icon: LucideIcon;
}
export const TabButton: React.FC<TabButtonProps> = ({ active, onClick, label, icon: Icon }) => (
    <button 
        type="button"
        onClick={onClick}
        className={`
            px-6 py-2.5 rounded-xl text-sm font-bold transition-all duration-300 flex items-center gap-2 cursor-pointer
            ${active 
                ? 'bg-white text-yellow-600 shadow-sm scale-100' 
                : 'text-slate-500 hover:text-slate-700 hover:bg-slate-200/30'
            }
        `}
    >
        <Icon size={16} className={active ? 'text-yellow-500' : 'text-slate-400'} />
        {label}
    </button>
);

// 5. Khối Card lớn duy nhất chứa nội dung Tab
export const DetailCard: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className="bg-white rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)] border border-slate-100 min-h-125 overflow-hidden">
        {children}
    </div>
);

// 6. Phân khu danh mục thông tin bên trong Tab (Ví dụ: Tổng Quan, Liên Hệ...)
export const DetailSection: React.FC<{ title: string; children: React.ReactNode; dotColor?: string }> = ({ title, children, dotColor = "bg-yellow-400" }) => (
    <div className="flex flex-col gap-5">
        <div className="border-b border-slate-100 pb-2.5">
            <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider flex items-center gap-2">
                <span className={`w-1.5 h-4 ${dotColor} rounded-full inline-block`}></span>
                {title}
            </h3>
        </div>
        {children}
    </div>
);

// 7. Ô hiển thị dữ liệu (Label nhỏ ở trên, Value lớn ở dưới)
export const InfoField: React.FC<{ label: string; value: React.ReactNode; className?: string }> = ({ label, value, className = "" }) => (
    <div className={`flex flex-col gap-1.5 ${className}`}>
        <span className="text-xs text-slate-400 font-bold uppercase tracking-wider select-none">
            {label}
        </span>
        <div className="text-[14px] font-semibold text-slate-800 wrap-break-word leading-snug">
            {value || <span className="text-slate-300 font-normal italic">Chưa cập nhật</span>}
        </div>
    </div>
);

// 8. Khối hồ sơ Avatar/Logo bên phải (Profile Sidebar)
interface ProfileCardProps {
    title: string;
    subTitle: string;
    logoPath?: string;
    fallbackIcon: LucideIcon;
    statusActive: boolean;
    activeLabel?: string;
    inactiveLabel?: string;
}
export const DetailProfileCard: React.FC<ProfileCardProps> = ({ 
    title, subTitle, logoPath, fallbackIcon: FallbackIcon, statusActive, activeLabel = "ĐANG HOẠT ĐỘNG", inactiveLabel = "NGỪNG GIAO DỊCH" 
}) => (
    <div className="bg-slate-50/60 rounded-2xl p-6 border border-slate-100/70 flex flex-col items-center text-center sticky top-0">
        <div className="w-40 h-40 rounded-full bg-white border-4 border-white shadow-md flex items-center justify-center overflow-hidden mb-5 group hover:shadow-lg transition-shadow duration-300">
            {logoPath ? (
                <img src={logoPath} alt="Profile Logo" className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105" />
            ) : (
                <FallbackIcon className="w-16 h-16 text-slate-300" />
            )}
        </div>
        
        <h2 className="text-xl font-extrabold text-slate-800 mb-1 tracking-tight">{title}</h2>
        <p className="text-sm text-yellow-600 font-black tracking-widest uppercase mb-6">{subTitle}</p>

        <div className={`
            w-full py-3.5 px-4 rounded-xl border flex items-center justify-center gap-2 font-bold text-xs tracking-wider transition-all duration-300
            ${statusActive 
                ? 'bg-emerald-50 border-emerald-200/60 text-emerald-600 shadow-sm shadow-emerald-500/5' 
                : 'bg-slate-100 border-slate-200 text-slate-400'
            }
        `}>
            <span className={`w-2 h-2 rounded-full ${statusActive ? 'bg-emerald-500 animate-pulse' : 'bg-slate-300'}`}></span>
            {statusActive ? activeLabel : inactiveLabel}
        </div>
    </div>
);

// 9. Màn hình báo rỗng cho các Tab chưa có danh sách (Tách biệt khỏi Table)
export const TabEmptyPlaceholder: React.FC<{ title: string; desc: string; icon: LucideIcon }> = ({ title, desc, icon: Icon }) => (
    <div className="flex flex-col items-center justify-center py-32 text-slate-400 animate-in fade-in zoom-in-95 duration-300">
        <div className="bg-slate-50 p-6 rounded-3xl border border-slate-100 mb-4 text-slate-300 shadow-inner">
            <Icon className="w-12 h-12" strokeWidth={1.5} />
        </div>
        <h3 className="text-base font-bold text-slate-700 mb-1">{title}</h3>
        <p className="text-sm font-medium text-slate-400">{desc}</p>
    </div>
);