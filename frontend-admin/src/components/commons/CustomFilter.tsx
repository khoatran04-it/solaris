import React, { useState, useRef, useEffect } from 'react';
import { Filter, Check } from 'lucide-react';

interface CustomFilterProps {
  title: string;
  options: { label: string; value: string | number }[];
  selectedValues: (string | number)[];
  onApply: (values: (string | number)[]) => void;
}

export const CustomFilter: React.FC<CustomFilterProps> = ({
  title,
  options,
  selectedValues,
  onApply,
}) => {
  const [isOpen, setIsOpen] = useState(false);

  // Lưu state tạm thời khi user đang click chọn, chỉ apply khi bấm nút
  const [tempSelected, setTempSelected] = useState<(string | number)[]>(selectedValues);
  const wrapperRef = useRef<HTMLDivElement>(null);

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setTempSelected(selectedValues); // Reset nếu đóng ngang
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [selectedValues]);

  const toggleSelection = (val: string | number) => {
    if (tempSelected.includes(val)) {
      setTempSelected(tempSelected.filter((v) => v !== val));
    } else {
      setTempSelected([...tempSelected, val]);
    }
  };

  const handleApply = () => {
    onApply(tempSelected);
    setIsOpen(false);
  };

  const isFiltered = selectedValues.length > 0;

  return (
    <div ref={wrapperRef} className="relative inline-block">
      {/* THẺ TIÊU ĐỀ CỘT TRÊN BẢNG */}
      <div
        onClick={() => setIsOpen(!isOpen)}
        className={`flex items-center gap-1.5 cursor-pointer select-none transition-colors duration-200 ${
          isFiltered ? 'text-green-500' : 'text-slate-500 hover:text-slate-800'
        }`}
      >
        {title}
        <Filter
          size={14}
          className={isFiltered ? 'fill-green-500 text-green-500' : 'text-slate-400'}
        />
      </div>

      {/* BẢNG LỌC POPUP EXCEL */}
      {isOpen && (
        <div className="absolute top-[calc(100%+12px)] left-0 w-56 bg-white border border-slate-100 rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] z-50 font-normal animate-in fade-in zoom-in-95 duration-200 overflow-hidden flex flex-col">
          {/* Danh sách lựa chọn (Có thanh cuộn nếu quá dài) */}
          <div className="max-h-60 overflow-y-auto p-2 space-y-0.5 custom-scrollbar">
            {options.map((opt) => {
              const isSelected = tempSelected.includes(opt.value);
              return (
                <div
                  key={opt.value}
                  onClick={() => toggleSelection(opt.value)}
                  className={`
                                        flex items-center gap-3 px-3 py-2 cursor-pointer rounded-lg transition-colors duration-200 text-[13px] font-medium
                                        ${isSelected ? 'bg-green-50/50 text-slate-900' : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'}
                                    `}
                >
                  {/* Checkbox Custom mượt mà */}
                  <div
                    className={`
                                        w-4 h-4 shrink-0 flex items-center justify-center rounded-sm border transition-all duration-200
                                        ${isSelected ? 'bg-green-500 border-green-500 shadow-sm shadow-green-500/30' : 'bg-white border-slate-300'}
                                    `}
                  >
                    {isSelected && <Check size={12} className="text-white" strokeWidth={3.5} />}
                  </div>
                  <span className="truncate">{opt.label}</span>
                </div>
              );
            })}
            {options.length === 0 && (
              <div className="py-4 text-center text-xs text-slate-400 italic">Không có dữ liệu</div>
            )}
          </div>

          {/* Dải nút hành động */}
          <div className="flex border-t border-slate-100 p-3 gap-2 bg-slate-50/50">
            <button
              onClick={() => {
                setTempSelected([]);
                onApply([]);
                setIsOpen(false);
              }}
              className="flex-1 py-2 bg-white border border-slate-200 rounded-lg text-xs font-bold text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition-colors shadow-sm"
            >
              XÓA LỌC
            </button>
            <button
              onClick={handleApply}
              className="flex-1 py-2 bg-yellow-400 rounded-lg text-xs font-bold text-slate-900 hover:bg-yellow-500 transition-colors shadow-sm"
            >
              ÁP DỤNG
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
