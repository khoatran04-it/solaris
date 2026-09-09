import React, { useState, useEffect, useMemo } from 'react';
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
  existingConversions?: UoMConversion[];
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
  existingConversions,
  onSuccess,
}) => {
  const [fromUoMId, setFromUoMId] = useState<number>(initialFromUoMId || 0);
  const [toUoMId, setToUoMId] = useState<number>(baseUoMId || 0);
  const [conversionFactor, setConversionFactor] = useState<number>(1);
  const [error, setError] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const effectiveToUoMId = baseUoMId || toUoMId;

  // Kiểm tra xem đã có quy tắc quy đổi đặc thù cho cặp [fromUoMId -> effectiveToUoMId] của sản phẩm này chưa
  const existingRule = useMemo(() => {
    if (!productId || !fromUoMId || !effectiveToUoMId) return undefined;
    return existingConversions?.find(
      (c) =>
        c.isActive &&
        c.productId === productId &&
        c.fromUoMId === fromUoMId &&
        c.toUoMId === effectiveToUoMId
    );
  }, [existingConversions, productId, fromUoMId, effectiveToUoMId]);

  useEffect(() => {
    if (isOpen) {
      const initFrom = initialFromUoMId || 0;
      setFromUoMId(initFrom);
      setToUoMId(baseUoMId || 0);
      const targetTo = baseUoMId || 0;
      const match = existingConversions?.find(
        (c) =>
          c.isActive &&
          c.productId === productId &&
          c.fromUoMId === initFrom &&
          c.toUoMId === targetTo
      );
      setConversionFactor(match ? match.conversionFactor : 1);
      setError('');
    }
  }, [isOpen, initialFromUoMId, baseUoMId, productId, existingConversions]);

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

  // Nghiệp vụ: Đơn vị lớn (Bao bì) loại trừ Đơn vị cơ sở ra (tránh đổi Gói -> Gói)
  const fromUoMOptions = useMemo(() => {
    if (!baseUoMId) return uomOptions;
    return uomOptions.filter((u) => u.value !== baseUoMId);
  }, [uomOptions, baseUoMId]);

  // Nghiệp vụ: Đơn vị đích BẮT BUỘC và DUY NHẤT là Đơn vị cơ sở của sản phẩm
  const toUoMOptions = useMemo(() => {
    if (!baseUoMId) return [];
    return [
      {
        label: `${baseUoMName || 'Đơn vị cơ sở'} (Đơn vị cơ sở của sản phẩm)`,
        value: baseUoMId,
      },
    ];
  }, [baseUoMId, baseUoMName]);

  if (!isOpen) return null;

  const selectedFrom =
    fromUoMOptions.find((u) => u.value === fromUoMId) ||
    uomOptions.find((u) => u.value === fromUoMId);
  const selectedTo = toUoMOptions.find((u) => u.value === (baseUoMId || toUoMId)) || {
    label: baseUoMName || 'Đơn vị cơ sở',
    value: baseUoMId || 0,
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!productId || productId <= 0) {
      setError('Thiếu thông tin sản phẩm áp dụng.');
      return;
    }
    if (!fromUoMId || fromUoMId <= 0) {
      setError('Vui lòng chọn đơn vị nguồn (quy cách đóng gói lớn: Thùng, Lốc, Hộp...).');
      return;
    }
    if (!effectiveToUoMId || effectiveToUoMId <= 0) {
      setError('Sản phẩm này chưa có Đơn vị tính cơ sở hợp lệ!');
      return;
    }
    if (fromUoMId === effectiveToUoMId) {
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

      if (existingRule) {
        // Cập nhật quy tắc quy đổi đã tồn tại của sản phẩm này (Upsert)
        await uomConversionApi.update(existingRule.id, {
          fromUoMId,
          toUoMId: effectiveToUoMId,
          conversionFactor,
          productId,
          isActive: true,
        });

        const updatedConv: UoMConversion = {
          ...existingRule,
          conversionFactor,
          fromUoMId,
          toUoMId: effectiveToUoMId,
          fromUoMName: selectedFrom?.label?.split(' ')[0] || existingRule.fromUoMName,
          toUoMName: selectedTo?.label?.split(' ')[0] || existingRule.toUoMName,
        };
        onSuccess(updatedConv);
        onClose();
      } else {
        // Tạo mới quy tắc quy đổi đặc thù
        const res = await uomConversionApi.create({
          fromUoMId,
          toUoMId: effectiveToUoMId,
          conversionFactor,
          productId,
          isActive: true,
        });

        if (res) {
          onSuccess(res);
          onClose();
        }
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
                {existingRule ? 'Cập Nhật Quy Đổi Sản Phẩm' : 'Cấu Hình Quy Đổi Nhanh'}
              </h3>
              <p className="text-xs text-slate-500 font-medium">
                {existingRule
                  ? 'Cập nhật hệ số bao bì đã có cho sản phẩm này'
                  : 'Thiết lập tỷ lệ bao bì riêng cho sản phẩm này'}
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

            {/* BADGE THÔNG BÁO ĐANG CẬP NHẬT QUY TẮC HIỆN CÓ */}
            {existingRule && selectedFrom && selectedTo && (
              <div className="p-3 bg-blue-50 border border-blue-200 rounded-2xl flex items-center gap-2.5 text-xs font-semibold text-blue-900 animate-in fade-in">
                <Repeat size={16} className="text-blue-600 shrink-0" />
                <span>
                  Sản phẩm này đã có quy tắc quy đổi:{' '}
                  <b>
                    1 {selectedFrom.label.split(' ')[0]} = {existingRule.conversionFactor}{' '}
                    {selectedTo.label.split(' ')[0]}
                  </b>
                  . Bạn có thể thay đổi hệ số và bấm <b>Cập Nhật Quy Đổi</b>.
                </span>
              </div>
            )}

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
                placeholder="Chọn ĐVT đóng gói..."
                options={fromUoMOptions}
                value={fromUoMId}
                onSelect={(val) => {
                  const newFromId = Number(val);
                  setFromUoMId(newFromId);
                  setError('');
                  const match = existingConversions?.find(
                    (c) =>
                      c.isActive &&
                      c.productId === productId &&
                      c.fromUoMId === newFromId &&
                      c.toUoMId === effectiveToUoMId
                  );
                  if (match) {
                    setConversionFactor(match.conversionFactor);
                  }
                }}
              />

              <FormInput
                label="Hệ số quy đổi (Số lượng)"
                required
                type="number"
                placeholder="VD: 24"
                value={conversionFactor}
                onChange={(e) => {
                  setConversionFactor(Number(e.target.value));
                  setError('');
                }}
              />
            </div>

            <div>
              <FormSelect
                label="Đến đơn vị tính (Đơn vị cơ sở của sản phẩm)"
                required
                disabled={true}
                placeholder="Đơn vị tính cơ sở..."
                options={toUoMOptions}
                value={baseUoMId || toUoMId}
                onSelect={() => {}}
              />
              <p className="text-[11px] text-amber-800 font-semibold mt-1.5 flex items-center gap-1.5 bg-amber-50/80 px-3 py-1.5 rounded-xl border border-amber-200">
                <span>
                  Cố định theo đơn vị cơ sở (<b>{baseUoMName || 'Gốc'}</b>). Quy cách đóng gói đặc
                  thù của sản phẩm chỉ được quy đổi về đơn vị cơ sở này.
                </span>
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
                  <span>{existingRule ? 'Cập Nhật Quy Đổi' : 'Lưu Quy Đổi'}</span>
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
