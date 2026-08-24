import React, { useState, useRef, useEffect } from 'react';
import { Filter, ChevronLeft, ChevronRight } from 'lucide-react';

interface CustomDateFilterProps {
  title: string;
  selectedDate: Date | null;
  onApply: (date: Date | null) => void;
}

export const CustomDateFilter: React.FC<CustomDateFilterProps> = ({
  title,
  selectedDate,
  onApply,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [tempDate, setTempDate] = useState<Date | null>(selectedDate);
  const [currentMonth, setCurrentMonth] = useState(selectedDate || new Date());
  const wrapperRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setTempDate(selectedDate);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [selectedDate]);

  // --- LOGIC VẼ TỜ LỊCH ---
  const daysInMonth = new Date(
    currentMonth.getFullYear(),
    currentMonth.getMonth() + 1,
    0
  ).getDate();
  const firstDayOfMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1).getDay();
  const startingEmptyCells = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;
  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1);
  const emptyCells = Array.from({ length: startingEmptyCells }, (_, i) => i);
  const dayNames = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

  const isFiltered = selectedDate !== null;

  return (
    <div ref={wrapperRef} className="relative inline-block">
      {/* THẺ TIÊU ĐỀ TRÊN BẢNG */}
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

      {/* BẢNG LỌC CALENDAR POPUP */}
      {isOpen && (
        <div className="absolute top-full right-0 mt-3 w-72 bg-white border border-slate-100 rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] z-50 p-5 font-normal animate-in fade-in zoom-in-95 duration-200">
          {/* Header Tháng / Năm */}
          <div className="flex justify-between items-center mb-5">
            <button
              onClick={() =>
                setCurrentMonth(
                  new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1)
                )
              }
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-800 transition-colors"
            >
              <ChevronLeft size={20} />
            </button>
            <span className="font-bold text-[15px] text-slate-800">
              Tháng {currentMonth.getMonth() + 1}, {currentMonth.getFullYear()}
            </span>
            <button
              onClick={() =>
                setCurrentMonth(
                  new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1)
                )
              }
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-800 transition-colors"
            >
              <ChevronRight size={20} />
            </button>
          </div>

          {/* Tiêu đề các ngày trong tuần */}
          <div className="grid grid-cols-7 gap-1 mb-2">
            {dayNames.map((day) => (
              <div
                key={day}
                className="text-center text-[11px] font-bold text-slate-400 uppercase tracking-wider"
              >
                {day}
              </div>
            ))}
          </div>

          {/* Lưới lịch */}
          <div className="grid grid-cols-7 gap-1 mb-5">
            {emptyCells.map((cell) => (
              <div key={`empty-${cell}`} />
            ))}

            {days.map((day) => {
              const isSelected =
                tempDate?.getDate() === day &&
                tempDate?.getMonth() === currentMonth.getMonth() &&
                tempDate?.getFullYear() === currentMonth.getFullYear();
              const isToday =
                new Date().getDate() === day &&
                new Date().getMonth() === currentMonth.getMonth() &&
                new Date().getFullYear() === currentMonth.getFullYear();

              return (
                <div
                  key={day}
                  onClick={() =>
                    setTempDate(new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day))
                  }
                  className={`
                                        h-8 flex items-center justify-center cursor-pointer text-[13px] rounded-md transition-all duration-200
                                        ${
                                          isSelected
                                            ? 'bg-green-500 text-white font-bold shadow-md shadow-green-500/30'
                                            : isToday
                                              ? 'border border-green-500 text-green-600 font-bold bg-green-50/50 hover:bg-green-100'
                                              : 'text-slate-700 font-medium hover:bg-slate-100 hover:text-slate-900'
                                        }
                                    `}
                >
                  {day}
                </div>
              );
            })}
          </div>

          {/* Dải nút hành động */}
          <div className="flex border-t border-slate-100 pt-4 gap-3">
            <button
              onClick={() => {
                setTempDate(null);
                onApply(null);
                setIsOpen(false);
              }}
              className="flex-1 py-2 bg-white border border-slate-200 rounded-lg text-xs font-bold text-slate-600 hover:bg-slate-50 hover:text-slate-900 transition-colors"
            >
              XÓA LỌC
            </button>
            <button
              onClick={() => {
                onApply(tempDate);
                setIsOpen(false);
              }}
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
