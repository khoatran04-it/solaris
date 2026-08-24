import React, { useEffect, useState } from 'react';
import { CheckCircle, AlertTriangle, XCircle } from 'lucide-react';

interface ToastProps {
  show: boolean;
  type: 'success' | 'warning' | 'error';
  message: string;
}

export const Toast: React.FC<ToastProps> = ({ show, type, message }) => {
  // Giữ nguyên logic siêu việt: Lưu state cũ để chống giật UI khi fade out
  const [displayContent, setDisplayContent] = useState({ type, message });

  useEffect(() => {
    if (show) {
      setDisplayContent({ type, message });
    }
  }, [show, type, message]);

  const currentType = show ? type : displayContent.type;
  const currentMessage = show ? message : displayContent.message;

  // 💡 DICTIONARY PATTERN: Khai báo cấu hình giao diện cho từng loại Toast
  const styleConfig = {
    success: {
      wrapper: 'bg-emerald-500 text-white',
      title: 'THÀNH CÔNG',
      icon: <CheckCircle size={28} className="drop-shadow-sm text-white" />,
    },
    warning: {
      wrapper: 'bg-yellow-400 text-slate-900',
      title: 'CẢNH BÁO',
      icon: <AlertTriangle size={28} className="drop-shadow-sm text-slate-900" />,
    },
    error: {
      wrapper: 'bg-red-500 text-white',
      title: 'LỖI',
      icon: <XCircle size={28} className="drop-shadow-sm text-white" />,
    },
  };

  // Bốc cấu hình tương ứng ra xài
  const currentStyle = styleConfig[currentType];

  return (
    <div
      className={`
                fixed top-6 left-1/2 z-9999 min-w-350px max-w-md
                flex items-center gap-3.5 px-5 py-3.5 rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.12)]
                transition-all duration-400 ease-[cubic-bezier(0.175,0.885,0.32,1.275)]
                ${currentStyle.wrapper}
                ${
                  show
                    ? 'opacity-100 -translate-x-1/2 translate-y-0 scale-100'
                    : 'opacity-0 -translate-x-1/2 -translate-y-6 scale-95 pointer-events-none'
                }
            `}
    >
      {/* ICON */}
      <div className="shrink-0">{currentStyle.icon}</div>

      {/* NỘI DUNG */}
      <div className="flex flex-col pt-0.5">
        <span className="text-[14px] font-extrabold tracking-wider leading-none mb-1">
          {currentStyle.title}
        </span>
        <span className="text-[13px] font-medium opacity-90 leading-snug">{currentMessage}</span>
      </div>
    </div>
  );
};
