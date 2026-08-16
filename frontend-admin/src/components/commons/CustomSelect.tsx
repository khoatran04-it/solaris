import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Search, X } from 'lucide-react';

export interface SelectOption {
    value: number | string;
    label: string;
}

interface CustomSelectProps {
    label: string;
    required?: boolean;
    placeholder?: string;
    options: SelectOption[];
    value: number | string | null;
    onChange: (value: number | string) => void;
}

export const CustomSelect: React.FC<CustomSelectProps> = ({ label, required, placeholder = "Chọn một tùy chọn...", options, value, onChange }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const wrapperRef = useRef<HTMLDivElement>(null);

    // Tự động đóng menu khi click ra ngoài
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    // Lọc danh sách dựa trên từ khóa search
    const filteredOptions = options.filter(opt => 
        opt.label.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const selectedOption = options.find(opt => opt.value === value);

    return (
        <div ref={wrapperRef} className="flex items-start w-full relative">
            
            {/* LABEL (Căn lề chuẩn mực đồng bộ 100% với GenericInput và DatePicker) */}
            <label className="font-bold text-sm text-slate-800 w-35 mt-2.5 flex items-center select-none">
                {label}: {required && <span className="text-red-500 ml-1 text-base">*</span>}
            </label>

            {/* VÙNG CHỌN (TRIGGER BOX) */}
            <div className="flex-1 relative">
                <div 
                    onClick={() => setIsOpen(!isOpen)}
                    className={`
                        flex justify-between items-center px-4 py-2.5 rounded-lg cursor-pointer text-sm transition-all duration-300 border
                        ${isOpen 
                            ? 'border-green-500 ring-2 ring-green-100 bg-white shadow-sm' 
                            : 'border-slate-200 bg-white hover:border-slate-300 shadow-sm/50'
                        }
                        ${selectedOption ? 'text-slate-800 font-medium' : 'text-slate-400 italic'}
                    `}
                >
                    <span className="overflow-hidden text-ellipsis whitespace-nowrap pr-2">
                        {selectedOption ? selectedOption.label : placeholder}
                    </span>
                    <ChevronDown 
                        size={18} 
                        className={`text-slate-400 transition-transform duration-300 ${
                            isOpen ? 'rotate-180 text-green-500' : 'rotate-0'
                        }`} 
                    />
                </div>

                {/* MENU DROPDOWN (Bóng đổ sâu, Bo góc lớn Premium) */}
                {isOpen && (
                    <div className="absolute top-[calc(100%+8px)] left-0 right-0 bg-white border border-slate-100 rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] z-100 overflow-hidden animate-in fade-in zoom-in-95 duration-200 flex flex-col">
                        
                        {/* Thanh tìm kiếm nội bộ */}
                        <div className="flex items-center px-3 py-2 border-b border-slate-100 bg-slate-50/50">
                            <Search size={16} className="text-slate-400" />
                            <input 
                                type="text" 
                                autoFocus
                                placeholder="Tìm kiếm nhanh..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                className="border-none outline-none w-full text-sm bg-transparent placeholder-slate-400 text-slate-700 font-medium pl-2.5 py-1"
                            />
                            {searchTerm && (
                                <X 
                                    size={16} 
                                    className="text-slate-400 hover:text-slate-600 cursor-pointer transition-colors" 
                                    onClick={() => setSearchTerm('')} 
                                />
                            )}
                        </div>

                        {/* Danh sách các Options */}
                        <div className="max-h-56 overflow-y-auto p-1.5 space-y-0.5 custom-scrollbar">
                            {filteredOptions.length > 0 ? (
                                filteredOptions.map((opt) => {
                                    const isSelected = value === opt.value;
                                    return (
                                        <div 
                                            key={opt.value}
                                            onClick={() => {
                                                onChange(opt.value);
                                                setIsOpen(false);
                                                setSearchTerm('');
                                            }}
                                            className={`
                                                px-3 py-2.5 cursor-pointer rounded-lg text-sm font-medium transition-colors duration-150
                                                ${isSelected 
                                                    ? 'bg-green-500 text-white font-semibold shadow-sm shadow-green-500/20' 
                                                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                                                }
                                            `}
                                        >
                                            {opt.label}
                                        </div>
                                    );
                                })
                            ) : (
                                <div className="py-6 text-center text-sm text-slate-400 italic bg-slate-50/20">
                                    Không tìm thấy kết quả phù hợp
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};