import React, { useEffect } from 'react';
import { AlertTriangle, Trash2 } from 'lucide-react';

interface ConfirmDeleteModalProps {
    isOpen: boolean;
    itemName: string;
    onClose: () => void;
    onConfirm: () => void;
}

export const ConfirmDeleteModal: React.FC<ConfirmDeleteModalProps> = ({ 
    isOpen, 
    itemName, 
    onClose, 
    onConfirm 
}) => {
    // UX: Cho phép nhấn nút ESC để đóng modal
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && isOpen) {
                onClose();
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isOpen, onClose]);

    // UX: Khóa cuộn trang (scroll) khi mở modal
    useEffect(() => {
        if (isOpen) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = 'unset';
        }
        return () => {
            document.body.style.overflow = 'unset';
        };
    }, [isOpen]);

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-1000 flex items-center justify-center px-4">
            {/* Lớp nền tối mờ (Backdrop) */}
            <div 
                className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity"
                onClick={onClose}
            ></div>

            {/* Nội dung Modal */}
            <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in duration-200">
                
                {/* Khu vực Icon & Text */}
                <div className="p-6 text-center sm:p-8">
                    <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-5 ring-8 ring-red-50">
                        <AlertTriangle className="text-red-600" size={32} strokeWidth={2.5} />
                    </div>
                    
                    <h3 className="text-xl font-bold text-slate-900 mb-2">
                        Xác nhận xóa dữ liệu
                    </h3>
                    
                    <p className="text-slate-500 text-sm md:text-base leading-relaxed">
                        Bạn có chắc chắn muốn xóa <br/>
                        <span className="font-bold text-slate-800 text-lg">"{itemName}"</span> không? <br/>
                        <span className="text-red-500 text-sm mt-1 block">Hành động này không thể hoàn tác.</span>
                    </p>
                </div>

                {/* Khu vực Nút bấm */}
                <div className="flex gap-3 p-5 bg-slate-50 border-t border-slate-100 sm:px-8">
                    <button 
                        onClick={onClose} 
                        className="flex-1 px-4 py-2.5 rounded-xl border border-slate-300 text-slate-700 font-semibold hover:bg-slate-200 hover:text-slate-900 transition-colors focus:ring-4 focus:ring-slate-100 outline-none"
                    >
                        Hủy bỏ
                    </button>
                    <button 
                        onClick={onConfirm} 
                        className="flex-1 px-4 py-2.5 rounded-xl bg-red-600 text-white font-semibold hover:bg-red-700 shadow-sm shadow-red-200 transition-colors flex items-center justify-center gap-2 focus:ring-4 focus:ring-red-100 outline-none"
                    >
                        <Trash2 size={18} />
                        Xóa ngay
                    </button>
                </div>
            </div>
        </div>
    );
};