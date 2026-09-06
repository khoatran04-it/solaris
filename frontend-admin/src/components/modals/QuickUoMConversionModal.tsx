import React, { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { X, Save, Repeat, ArrowRight, PackageOpen, Loader2 } from 'lucide-react';
import { FormInput, FormSelect } from '../commons/FormUI';
import { uomConversionApi } from '../../api/uomConversionApi';
import { UoMConversion } from '../../types/uomConversion';

interface QuickUoMConversionModalProps {
  isOpen: boolean;
  onClose: () => void;
  productId: number;
  productName?: string;
  productCode?: string;
  baseUoMId?: number;
  baseUoMName?: string;
  initialFromUoMId?: number;
  uomOptions: { label: string; value: number }[];
  onSuccess: (newConversion: UoMConversion) => void;
}

export const QuickUoMConversionModal: React.FC<QuickUoMConversionModalProps> = ({
  isOpen,
  onClose,
  productId,
  productName,
  productCode,
  baseUoMId,
  baseUoMName,
  initialFromUoMId,
  uomOptions,
  onSuccess,
}) => {
  const [fromUoMId, setFromUoMId] = useState<number>(initialFromUoMId || 0);
  const [toUoMId, setToUoMId] = useState<number>(baseUoMId || 0);
  const [conversionFactor, setConversionFactor] = useState<number>(1);
  const [error, setError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  useEffect(() => {
    if (isOpen) {
      setFromUoMId(initialFromUoMId || 0);
      setToUoMId(baseUoMId || 0);
      setConversionFactor(1);
      setError('');
    }
  }, [isOpen, initialFromUoMId, baseUoMId]);

  // UX: Đóng modal khi bấm phím Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen && !isSubmitting) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose, isSubmitting]);

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

  const selectedFrom = uomOptions.find((u) => u.value === fromUoMId);
  const selectedTo = uomOptions.find((u) => u.value === toUoMId);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!productId || productId <= 0) {
      setError('Thiếu thông tin sản phẩm áp dụng.');
      return;
    }
    if (!fromUoMId || fromUoMId <= 0) {
      setError('Vui lòng chọn đơn vị nguồn (quy cách đóng gói).');
      return;
    }
    if (!toUoMId || toUoMId <= 0) {
      setError('Vui lòng chọn đơn vị đích (đơn vị cơ sở).');
      return;
    }
    if (fromUoMId === toUoMId) {
      setError('Đơn vị đóng gói và đơn vị cơ sở không được trùng nhau!');
      return;
    }
    if (!conversionFactor || conversionFactor <= 0) {
      setError('Hệ số quy đổi phải lớn hơn 0.');
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');

      const res = await uomConversionApi.create({
        fromUoMId,
        toUoMId,
        conversionFactor,
        productId,
        isActive: true,
      });

      if (res) {
        onSuccess(res);
        onClose();
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          'Không thể lưu quy đổi đặc thù cho sản phẩm này. Vui lòng thử lại!'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const modalContent = (
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 animate-in fade-in"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isSubmitting) {
          onClose();
        }
      }}
    >
      <div className="bg-white rounded-3xl shadow-2xl w-full max-w-lg overflow-visible animate-in zoom-in-95 duration-200 border border-slate-100 relative">
        {/* MODAL HEADER */}
        <div className="flex justify-between items-center px-6 py-4.5 bg-amber-50/80 border-b border-amber-200/60 rounded-t-3xl">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-amber-400 text-slate-900 rounded-xl shadow-xs">
              <PackageOpen size={20} strokeWidth={2.5} />
            </div>
            <div>
              <h3 className="text-base font-extrabold text-slate-900 leading-tight">
                Cấu Hình Quy Đổi Nhanh
              </h3>
              <p className="text-xs text-slate-500 font-medium">
                Thiết lập tỷ lệ bao bì riêng cho sản phẩm này
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200/60 rounded-xl transition-colors cursor-pointer"
          >
            <X size={20} />
          </button>
        </div>

        {/* MODAL BODY */}
        <form onSubmit={handleSubmit}>
          <div className="p-6 flex flex-col gap-5">
            {/* THÔNG TIN SẢN PHẨM HIỆN TẠI */}
            <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-2xl flex items-center justify-between">
              <div className="flex flex-col">
                <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                  Sản phẩm áp dụng
                </span>
                <span className="text-sm font-extrabold text-slate-800 line-clamp-1">
                  {productName || 'Sản phẩm đang chọn'}
                </span>
                {productCode && (
                  <span className="text-xs font-semibold text-slate-500">{productCode}</span>
                )}
              </div>

              <div className="text-right">
                <span className="text-[11px] font-bold text-amber-700 uppercase tracking-wider block">
                  Đơn vị cơ sở
                </span>
                <span className="inline-flex px-2.5 py-0.5 rounded-lg bg-amber-100 text-amber-900 font-black text-xs border border-amber-300">
                  {baseUoMName || 'Chưa gán'}
                </span>
              </div>
            </div>

            {/* PREVIEW CÔNG THỨC TOÁN HỌC TRỰC QUAN */}
            {selectedFrom && selectedTo && conversionFactor > 0 && (
              <div className="p-3.5 bg-amber-50/70 border border-amber-200 rounded-2xl flex items-center justify-center gap-2.5 text-sm font-bold text-slate-800">
                <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">
                  Công thức:
                </span>
                <span className="px-2.5 py-1 bg-white text-slate-900 rounded-lg border border-slate-200 font-black text-xs">
                  1 {selectedFrom.label.split(' ')[0]}
                </span>
                <span className="text-amber-500 text-base font-bold">=</span>
                <span className="px-3 py-1 bg-amber-400 text-slate-900 rounded-lg font-black text-xs shadow-2xs">
                  {conversionFactor.toLocaleString('vi-VN')} {selectedTo.label.split(' ')[0]}
                </span>
              </div>
            )}

            {/* ERROR BANNER */}
            {error && (
              <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs font-bold text-rose-600 animate-in fade-in">
                {error}
              </div>
            )}

            {/* GRID INPUTS */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <FormSelect
                label="Từ đơn vị tính (Bao bì / Lớn)"
                required
                showSearch
                placeholder="Chọn ĐVT..."
                options={uomOptions}
                value={fromUoMId}
                onSelect={(val) => {
                  setFromUoMId(Number(val));
                  setError('');
                }}
              />

              <FormInput
                label="Hệ số quy đổi (Số lượng)"
                required
                type="number"
                placeholder="VD: 30"
                value={conversionFactor}
                onChange={(e) => {
                  setConversionFactor(Number(e.target.value));
                  setError('');
                }}
              />
            </div>

            <div>
              <FormSelect
                label="Đến đơn vị tính (Đơn vị cơ sở)"
                required
                showSearch
                placeholder="Chọn ĐVT đích..."
                options={uomOptions}
                value={toUoMId}
                onSelect={(val) => {
                  setToUoMId(Number(val));
                  setError('');
                }}
              />
              <p className="text-[11px] text-slate-400 mt-1 italic">
                * Mặc định là đơn vị cơ sở ({baseUoMName || 'Gốc'}) của sản phẩm này.
              </p>
            </div>
          </div>

          {/* MODAL FOOTER */}
          <div className="flex justify-end gap-3 px-6 py-4 bg-slate-50 border-t border-slate-100 rounded-b-3xl">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-5 py-2.5 rounded-xl border border-slate-200 text-slate-600 font-bold text-sm hover:bg-slate-100 transition-colors"
            >
              Hủy bỏ
            </button>

            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-900 bg-amber-400 hover:bg-amber-500 transition-all shadow-sm shadow-amber-200 active:scale-98 disabled:opacity-50 cursor-pointer"
            >
              {isSubmitting ? (
                <>
                  <Loader2 size={16} className="animate-spin" />
                  <span>Đang lưu...</span>
                </>
              ) : (
                <>
                  <Save size={16} strokeWidth={2.5} />
                  <span>Lưu Quy Đổi</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );

  if (typeof document !== 'undefined') {
    return createPortal(modalContent, document.body);
  }
  return modalContent;
};
