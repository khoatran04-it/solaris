import React from 'react';
import { Loader2, LucideIcon, ChevronLeft, Search, ChevronDown } from 'lucide-react';

// 1. Container cho toàn bộ trang
export const PageContainer: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className="h-full flex flex-col p-6 bg-slate-50/30 overflow-y-auto font-sans">
        <div className="max-w-5xl w-full mx-auto flex flex-col gap-8 pb-12">
            {children}
        </div>
    </div>
);

// 2. Card bọc ngoài Form
export const FormCard: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <div className="bg-white p-8 rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)] border border-slate-100">
        {children}
    </div>
);

// 3. Label dùng chung
export const FormLabel: React.FC<{ label: string; required?: boolean }> = ({ label, required }) => (
    <label className="font-bold text-[13px] text-slate-700 uppercase tracking-wide mb-2 block">
        {label} {required && <span className="text-red-500 ml-1">*</span>}
    </label>
);

// 4. Input Field chuẩn (Đã fix chiều cao h-[46px])
interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
    label: string;
    error?: string;
    required?: boolean;
}
export const FormInput = React.forwardRef<HTMLInputElement, InputProps>(
    ({ label, error, required, ...props }, ref) => {
        const inputClass = `
            w-full px-4 h-[46px] rounded-xl border text-sm transition-all duration-300 outline-none bg-slate-50/50 hover:bg-white focus:bg-white
            ${error 
                ? 'border-red-400 focus:border-red-500 focus:ring-4 focus:ring-red-500/20' 
                : 'border-slate-200 focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20'
            }
            text-slate-800 placeholder-slate-400 font-medium disabled:opacity-60
        `;
        return (
            <div className="flex flex-col relative">
                <FormLabel label={label} required={required} />
                <input ref={ref} className={inputClass} {...props} />
                {error && <div className="text-red-500 text-xs font-medium mt-1 animate-in fade-in slide-in-from-top-1">{error}</div>}
            </div>
        );
    }
);

// 5. Nút bấm Submit (Tạo mới/Lưu)
interface SubmitButtonProps {
    loading: boolean;
    isEditMode: boolean;
    icon?: LucideIcon;
}
export const SubmitButton: React.FC<SubmitButtonProps> = ({ loading, isEditMode, icon: Icon }) => (
    <button 
        type="submit" 
        disabled={loading} 
        className="group bg-yellow-400 hover:bg-yellow-500 text-slate-900 border-none rounded-xl px-10 py-3 text-[15px] flex items-center gap-2.5 cursor-pointer font-bold transition-all duration-300 shadow-[0_4px_14px_0_rgba(250,204,21,0.39)] hover:shadow-[0_6px_20px_rgba(250,204,21,0.23)] hover:-translate-y-0.5 disabled:opacity-50 disabled:cursor-not-allowed"
    >
        {loading ? (
            <Loader2 size={20} className="animate-spin" />
        ) : (
            Icon && <Icon size={20} strokeWidth={2.5} className="transition-transform group-hover:scale-110" />
        )}
        {isEditMode ? 'LƯU THAY ĐỔI' : 'TẠO MỚI'}
    </button>
);

// 6. Section Header
export const FormSection: React.FC<{ title: string; children: React.ReactNode; className?: string }> = ({ title, children, className = "" }) => (
    <div className={`flex flex-col gap-6 ${className}`}>
        <div className="border-b border-slate-100 pb-3">
            <h3 className="text-sm font-extrabold text-slate-800 uppercase tracking-wider flex items-center gap-2">
                <span className="w-1.5 h-4 bg-yellow-400 rounded-full inline-block"></span>
                {title}
            </h3>
        </div>
        {children}
    </div>
);

// 7. Header của Form
export const FormHeader: React.FC<{ title: string; subtitle: string; onBack: () => void; icon?: any }> = ({ title, subtitle, onBack, icon: Icon }) => (
    <div className="flex justify-between items-end mb-2">
        <div>
            <div className="flex items-center gap-2 mb-1.5">
                {Icon && <Icon className="w-6 h-6 text-yellow-500 fill-yellow-100" />}
                <h2 className="m-0 font-bold text-3xl text-slate-800 tracking-tight">{title}</h2>
            </div>
            <p className="text-slate-500 text-sm font-medium ml-8">{subtitle}</p>
        </div>
        <button type="button" onClick={onBack} className="group bg-white border border-slate-200 text-slate-600 rounded-xl px-5 py-2.5 text-sm font-bold flex items-center gap-2 hover:bg-slate-50 hover:text-slate-900 transition-all duration-300">
            <ChevronLeft size={18} strokeWidth={2.5} className="transition-transform group-hover:-translate-x-1" />
            QUAY LẠI
        </button>
    </div>
);

// 8. Textarea Field chuẩn
interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
    label: string;
    error?: string;
    required?: boolean;
}
export const FormTextarea: React.FC<TextareaProps> = ({ label, required, error, ...props }) => (
    <div className="flex flex-col gap-2">
        <FormLabel label={label} required={required} />
        <textarea 
            {...props}
            className={`w-full px-4 py-2.5 rounded-xl border text-sm transition-all duration-300 outline-none bg-slate-50/50 hover:bg-white focus:bg-white resize-none 
                ${error 
                    ? 'border-red-400 focus:border-red-500 focus:ring-4 focus:ring-red-500/20' 
                    : 'border-slate-200 focus:border-yellow-400 focus:ring-4 focus:ring-yellow-400/20'
                } text-slate-800 placeholder-slate-400 font-medium disabled:opacity-60`}
        />
        {error && <div className="text-red-500 text-xs font-medium mt-1">{error}</div>}
    </div>
);

// 9. Select Field chuẩn (Đã fix chiều cao h-[46px] để đồng bộ với Input)
interface SelectOption {
    label: string;
    value: any;
}
interface FormSelectProps {
    label: string;
    required?: boolean;
    error?: string;
    options: SelectOption[];
    value: any;
    placeholder?: string;
    onSelect: (value: any) => void;
    showSearch?: boolean;
    searchPlaceholder?: string;
    disabled?: boolean;
}
export const FormSelect: React.FC<FormSelectProps> = ({ 
    label, required, error, options, value, placeholder, onSelect, showSearch, searchPlaceholder, 
    disabled // 🔥 BƯỚC 1: Thêm disabled vào destructuring
}) => {
    const [isOpen, setIsOpen] = React.useState(false);
    const [search, setSearch] = React.useState('');
    const containerRef = React.useRef<HTMLDivElement>(null);

    React.useEffect(() => {
        const handleClick = (e: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) setIsOpen(false);
        };
        document.addEventListener('mousedown', handleClick);
        return () => document.removeEventListener('mousedown', handleClick);
    }, []);

    const selectedOption = options.find(opt => opt.value === value);
    const filteredOptions = options.filter(opt => opt.label.toLowerCase().includes(search.toLowerCase()));

    return (
        <div className="flex flex-col relative" ref={containerRef}>
            <FormLabel label={label} required={required} />
            <div 
                // 🔥 BƯỚC 2: Chặn onClick nếu đang bị disabled
                onClick={() => {
                    if (!disabled) setIsOpen(!isOpen);
                }}
                // 🔥 BƯỚC 3: Cập nhật CSS hiển thị trạng thái Disabled
                className={`flex justify-between items-center px-4 h-11.5 rounded-xl text-sm transition-all duration-300 border 
                ${disabled 
                    ? 'bg-slate-100 border-slate-200 cursor-not-allowed opacity-70' // UI khi bị khóa
                    : `cursor-pointer ${isOpen ? 'border-yellow-400 ring-4 ring-yellow-400/20 bg-white' : 'border-slate-200 bg-slate-50/50 hover:bg-white'}`
                }
                ${error && !disabled ? 'border-red-400 focus:border-red-500 focus:ring-4 focus:ring-red-500/20' : ''}`}
            >
                <span className={`truncate ${selectedOption ? (disabled ? 'text-slate-500 font-medium' : 'text-slate-800 font-medium') : 'text-slate-400 italic'}`}>
                    {selectedOption ? selectedOption.label : placeholder}
                </span>
                <ChevronDown size={16} className={`shrink-0 transition-transform duration-300 ${disabled ? 'text-slate-300' : 'text-slate-400'} ${isOpen && !disabled ? 'rotate-180 text-yellow-500' : ''}`} />
            </div>

            {/* Menu Dropdown chỉ render khi isOpen = true (mà disabled thì không bao giờ isOpen được) */}
            {isOpen && !disabled && (
                <div className="absolute top-[calc(100%+6px)] left-0 right-0 bg-white border border-slate-100 rounded-xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] z-100 overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200">
                    {showSearch && (
                        <div className="flex items-center px-3 py-2 border-b border-slate-100 bg-slate-50/50">
                            <Search size={14} className="text-slate-400" />
                            <input 
                                type="text" placeholder={searchPlaceholder} value={search} 
                                onChange={e => setSearch(e.target.value)}
                                className="border-none outline-none w-full text-xs bg-transparent pl-2 py-1 font-medium"
                                onClick={e => e.stopPropagation()}
                            />
                        </div>
                    )}
                    <div className="max-h-44 overflow-y-auto p-1.5 space-y-0.5">
                        {filteredOptions.length > 0 ? filteredOptions.map(opt => (
                            <div 
                                key={opt.value} 
                                onClick={() => { onSelect(opt.value); setIsOpen(false); setSearch(''); }}
                                className={`px-3 py-2.5 rounded-lg text-[13px] font-semibold cursor-pointer transition-colors ${value === opt.value ? 'bg-yellow-400 text-slate-900' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'}`}
                            >
                                {opt.label}
                            </div>
                        )) : <div className="px-3 py-4 text-xs text-slate-400 text-center italic">Không tìm thấy kết quả</div>}
                    </div>
                </div>
            )}
            {error && <div className="text-red-500 text-xs font-medium mt-1 animate-in fade-in slide-in-from-top-1">{error}</div>}
        </div>
    );
};