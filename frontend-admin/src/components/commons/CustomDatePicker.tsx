import React, { useState, useRef, useEffect } from 'react';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight } from 'lucide-react';

interface DatePickerProps {
  label?: string;
  required?: boolean;
  error?: string;
  value?: Date | null;
  selectedDate?: Date | null;
  onChange: (date: Date) => void;
  placeholder?: string;
  alignRight?: boolean;
}

const DatePicker: React.FC<DatePickerProps> = ({
  label,
  required,
  error,
  value,
  selectedDate,
  onChange,
  placeholder = 'Chọn ngày/tháng/năm...',
  alignRight = false,
}) => {
  const activeDate = value !== undefined ? value : (selectedDate !== undefined ? selectedDate : null);
  const [isOpen, setIsOpen] = useState(false);
  const [currentMonth, setCurrentMonth] = useState(activeDate || new Date());
  const wrapperRef = useRef<HTMLDivElement>(null);

  // Cập nhật currentMonth khi activeDate thay đổi
  useEffect(() => {
    if (activeDate) {
      setCurrentMonth(activeDate);
    }
  }, [activeDate]);

  // Đóng lịch khi click ra ngoài
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Format ngày hiển thị ra ô Input (VD: 27/06/2026)
  const displayValue = activeDate
    ? activeDate.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
    : '';

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

  const handlePrevMonth = () =>
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  const handleNextMonth = () =>
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));

  const handleSelectDay = (day: number) => {
    const selectedDate = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    onChange(selectedDate);
    setIsOpen(false);
  };

  return (
    <div ref={wrapperRef} className="flex flex-col relative w-full">
      {/* LABEL (Đồng bộ chuẩn FormUI) */}
      {label && (
        <label className="font-bold text-[13px] text-slate-700 uppercase tracking-wide mb-2 block">
          {label} {required && <span className="text-red-500 ml-1">*</span>}
        </label>
      )}

      {/* VÙNG CHỌN (INPUT GIẢ) */}
      <div
        onClick={() => setIsOpen(!isOpen)}
        className={`
                    flex justify-between items-center px-4 h-11.5 rounded-xl cursor-pointer text-sm transition-all duration-300 border
                    ${
                      isOpen
                        ? 'border-yellow-400 ring-4 ring-yellow-400/20 bg-white'
                        : 'border-slate-200 bg-slate-50/50 hover:bg-white'
                    }
                    ${error && !isOpen ? 'border-red-400 focus:border-red-500 focus:ring-4 focus:ring-red-500/20' : ''}
                `}
      >
        <span className={value ? 'text-slate-800 font-medium' : 'text-slate-400 font-medium'}>
          {displayValue || placeholder}
        </span>
        <CalendarIcon
          size={18}
          className={`transition-colors duration-300 ${isOpen ? 'text-yellow-500' : 'text-slate-400'}`}
        />
      </div>

      {/* HIỂN THỊ LỖI */}
      {error && (
        <div className="text-red-500 text-xs font-medium mt-1 animate-in fade-in slide-in-from-top-1">
          {error}
        </div>
      )}

      {/* POPUP LỊCH (CALENDAR) */}
      {isOpen && (
        <div
          className={`absolute top-[calc(100%+8px)] ${
            alignRight ? 'right-0' : 'left-0'
          } w-70 bg-white border border-slate-100 rounded-2xl shadow-[0_10px_40px_-10px_rgba(0,0,0,0.15)] z-100 p-5 font-normal animate-in fade-in zoom-in-95 duration-200`}
        >
          {/* Header: Tháng / Năm */}
          <div className="flex justify-between items-center mb-5">
            <button
              onClick={(e) => {
                e.preventDefault();
                handlePrevMonth();
              }}
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-800 transition-colors"
            >
              <ChevronLeft size={20} />
            </button>
            <span className="font-extrabold text-[14px] text-slate-800 uppercase tracking-wide">
              Tháng {currentMonth.getMonth() + 1}, {currentMonth.getFullYear()}
            </span>
            <button
              onClick={(e) => {
                e.preventDefault();
                handleNextMonth();
              }}
              className="p-1.5 rounded-lg hover:bg-slate-100 text-slate-500 hover:text-slate-800 transition-colors"
            >
              <ChevronRight size={20} />
            </button>
          </div>

          {/* Tên thứ trong tuần */}
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

          {/* Lưới ngày tháng */}
          <div className="grid grid-cols-7 gap-1">
            {/* Ô trống đầu tháng */}
            {emptyCells.map((cell) => (
              <div key={`empty-${cell}`} />
            ))}

            {/* Các ngày trong tháng */}
            {days.map((day) => {
              const isSelected =
                value?.getDate() === day &&
                value?.getMonth() === currentMonth.getMonth() &&
                value?.getFullYear() === currentMonth.getFullYear();
              const isToday =
                new Date().getDate() === day &&
                new Date().getMonth() === currentMonth.getMonth() &&
                new Date().getFullYear() === currentMonth.getFullYear();

              return (
                <div
                  key={day}
                  onClick={() => handleSelectDay(day)}
                  className={`
                                        h-8 flex items-center justify-center cursor-pointer text-[13px] rounded-lg transition-all duration-200
                                        ${
                                          isSelected
                                            ? 'bg-yellow-400 text-slate-900 font-extrabold shadow-sm shadow-yellow-400/50'
                                            : isToday
                                              ? 'border border-yellow-400 text-yellow-600 font-bold bg-yellow-50 hover:bg-yellow-100'
                                              : 'text-slate-700 font-medium hover:bg-slate-100 hover:text-slate-900'
                                        }
                                    `}
                >
                  {day}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};

export default DatePicker;
