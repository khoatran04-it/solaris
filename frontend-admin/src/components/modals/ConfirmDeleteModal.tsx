import React, { useEffect } from 'react';
import { AlertTriangle, Trash2, Loader2 } from 'lucide-react';

interface ConfirmDeleteModalProps {
    isOpen: boolean;
    itemName?: string;
    onClose: () => void;
    onConfirm: () => void;
    
    // Các prop bổ sung để dùng linh hoạt cho nhiều trang
    loading?: boolean;
    title?: string;
    message?: string;
}

export const ConfirmDeleteModal: React.FC<ConfirmDeleteModalProps> = ({ 
    isOpen, 
    itemName, 
    onClose, 
    onConfirm,
    loading = false,
    title = "Xác nhận xóa dữ liệu",
    message
}) => {
    // UX: Cho phép nhấn nút ESC để đóng modal
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && isOpen && !loading) {
                onClose();
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isOpen, onClose, loading]);

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
                className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity animate-in fade-in duration-200"
                onClick={() => !loading && onClose()}
            ></div>

            {/* Nội dung Modal */}
            <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden animate-in fade-in zoom-in-95 duration-200">
                
                {/* Khu vực Icon & Text */}
                <div className="p-6 text-center sm:p-8">
                    <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-5 ring-8 ring-red-50">
                        <AlertTriangle className="text-red-600" size={32} strokeWidth={2.5} />
                    </div>
                    
                    <h3 className="text-xl font-bold text-slate-900 mb-2">
                        {title}
                    </h3>
                    
                    {/* Render message truyền vào, nếu không có thì xài câu mặc định */}
                    {message ? (
                        <p className="text-slate-500 text-sm md:text-base leading-relaxed">
                            {message}
                        </p>
                    ) : (
                        <p className="text-slate-500 text-sm md:text-base leading-relaxed">
                            Bạn có chắc chắn muốn xóa <br/>
                            {itemName && <span className="font-bold text-slate-800 text-lg">"{itemName}"</span>} không? <br/>
                            <span className="text-red-500 text-sm mt-1 block font-medium">Hành động này không thể hoàn tác.</span>
                        </p>
                    )}
                </div>

                {/* Khu vực Nút bấm */}
                <div className="flex gap-3 p-5 bg-slate-50 border-t border-slate-100 sm:px-8">
                    <button 
                        onClick={onClose} 
                        disabled={loading}
                        className="flex-1 px-4 py-2.5 rounded-xl border border-slate-300 text-slate-700 font-bold hover:bg-slate-200 hover:text-slate-900 transition-colors focus:ring-4 focus:ring-slate-100 outline-none disabled:opacity-50"
                    >
                        Hủy bỏ
                    </button>
                    <button 
                        onClick={onConfirm} 
                        disabled={loading}
                        className="flex-1 px-4 py-2.5 rounded-xl bg-red-600 text-white font-bold hover:bg-red-700 shadow-sm shadow-red-200 transition-colors flex items-center justify-center gap-2 focus:ring-4 focus:ring-red-100 outline-none disabled:opacity-60"
                    >
                        {loading ? (
                            <>
                                <Loader2 size={18} className="animate-spin" /> Đang xóa...
                            </>
                        ) : (
                            <>
                                <Trash2 size={18} strokeWidth={2.5} /> Xóa ngay
                            </>
                        )}
                    </button>
                </div>
            </div>
        </div>
    );
};