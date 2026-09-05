import React, { useState, useEffect } from 'react';
import { X, Save, Sparkles, AlertCircle } from 'lucide-react';
import { FormInput } from '../commons/FormUI';
import DatePicker from '../commons/CustomDatePicker';
import { productBatchApi } from '../../api/productBatchApi';

interface ModalCreateBatchProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (newBatch: { id: number; variantId: number; batchCode: string }) => void;
  variantId: number;
  variantCode?: string;
  variantName?: string;
  supplierId: number;
}

export const ModalCreateBatch: React.FC<ModalCreateBatchProps> = ({
  isOpen,
  onClose,
  onSuccess,
  variantId,
  variantCode,
  variantName,
  supplierId,
}) => {
  const [batchCode, setBatchCode] = useState('');
  const [manufactureDate, setManufactureDate] = useState<Date | null>(new Date());
  const [expiryDate, setExpiryDate] = useState<Date | null>(new Date(Date.now() + 14 * 86400000));
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Sinh mã Lô hàng tự động chuẩn Việt Nam (YYYYMMDD)
  const generateBatchCode = () => {
    const codePrefix = variantCode
      ? variantCode
          .replace(/[^a-zA-Z0-9]/g, '')
          .slice(0, 8)
          .toUpperCase()
      : 'LOT';
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const todayStr = `${year}${month}${day}`;
    const randomNum = Math.floor(100 + Math.random() * 900);
    setBatchCode(`BATCH-${codePrefix}-${todayStr}-${randomNum}`);
    setError('');
  };

  useEffect(() => {
    if (isOpen) {
      generateBatchCode();
      setManufactureDate(new Date());
      setExpiryDate(new Date(Date.now() + 14 * 86400000));
      setError('');
    }
  }, [isOpen, variantId, variantCode]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!batchCode.trim()) {
      setError('Vui lòng nhập Mã Lô hàng');
      return;
    }
    if (!manufactureDate) {
      setError('Vui lòng chọn Ngày thu hoạch / sản xuất');
      return;
    }
    if (!expiryDate) {
      setError('Vui lòng chọn Hạn sử dụng (FEFO Expiry)');
      return;
    }
    if (expiryDate <= manufactureDate) {
      setError('Hạn sử dụng phải sau Ngày thu hoạch / sản xuất!');
      return;
    }
    if (!supplierId || supplierId <= 0) {
      setError('Bắt buộc chọn Nhà Cung Cấp trước khi tạo Lô!');
      return;
    }

    try {
      setIsSubmitting(true);
      setError('');

      const safeMfgDate = manufactureDate.toISOString();
      const safeExpDate = expiryDate.toISOString();

      const res = await productBatchApi.create({
        batchCode: batchCode.trim(),
        variantId: Number(variantId),
        supplierId: Number(supplierId),
        manufactureDate: safeMfgDate,
        expiryDate: safeExpDate,
        isActive: true,
      });

      if (res) {
        const newBatchId = (res.id || res.Id) as number;
        onSuccess({
          id: newBatchId,
          variantId: Number(variantId),
          batchCode: batchCode.trim(),
        });
        onClose();
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Không thể tạo Lô hàng. Vui lòng kiểm tra lại!');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4 animate-in fade-in">
      <div className="bg-white rounded-3xl shadow-xl w-full max-w-lg overflow-visible animate-in zoom-in-95 duration-200 border border-slate-100">
        {/* MODAL HEADER */}
        <div className="flex justify-between items-center px-6 py-4.5 bg-amber-50/80 border-b border-amber-200/60 rounded-t-3xl">
          <div>
            <h3 className="text-base font-extrabold text-amber-900 flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-amber-600" /> Khai Báo Lô Nông Sản Mới (FEFO)
            </h3>
            {variantName && (
              <p className="text-xs text-amber-800 font-semibold mt-0.5">
                Áp dụng cho: {variantName}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 bg-white rounded-full p-1.5 shadow-2xs border border-slate-200 cursor-pointer transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* MODAL BODY */}
        <form onSubmit={handleSubmit}>
          <div className="p-6 space-y-5">
            {error && (
              <div className="p-3 bg-rose-50 text-rose-600 text-xs font-bold rounded-xl border border-rose-200 flex items-center gap-2 animate-in fade-in">
                <AlertCircle className="w-4 h-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            {/* MÃ LÔ HÀNG */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label className="font-bold text-[13px] text-slate-700 uppercase tracking-wide">
                  Mã Lô Hàng (Batch Code) <span className="text-red-500">*</span>
                </label>
                <button
                  type="button"
                  onClick={generateBatchCode}
                  className="text-xs text-amber-700 font-bold hover:underline flex items-center gap-1 cursor-pointer"
                >
                  ⚡ Tự sinh mã
                </button>
              </div>
              <FormInput
                label=""
                value={batchCode}
                onChange={(e) => setBatchCode(e.target.value)}
                placeholder="VD: BATCH-BO034-20260901-001"
              />
            </div>

            {/* NGÀY SẢN XUẤT & HẠN DÙNG */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <DatePicker
                label="Ngày Thu Hoạch / Đóng Gói"
                required
                value={manufactureDate}
                onChange={(date) => setManufactureDate(date)}
                placeholder="Chọn ngày SX..."
              />
              <DatePicker
                label="Hạn Sử Dụng (FEFO Expiry)"
                required
                value={expiryDate}
                onChange={(date) => setExpiryDate(date)}
                placeholder="Chọn hạn dùng..."
                alignRight
              />
            </div>

            {/* GHI CHÚ QUY TẮC FEFO */}
            <div className="p-3.5 bg-amber-50/80 text-amber-900 text-xs rounded-2xl border border-amber-200/80 flex items-start gap-2.5 leading-relaxed">
              <AlertCircle className="w-4 h-4 text-amber-700 shrink-0 mt-0.5" />
              <span>
                <strong>Quản trị Lô theo FEFO (First Expired, First Out):</strong> Khi xuất kho giao
                hàng cho khách lẻ, hệ thống Solaris sẽ tự động đề xuất xuất Lô có Hạn sử dụng gần
                nhất trước để chống thối rữa và giảm hao hụt nông sản.
              </span>
            </div>
          </div>

          {/* MODAL FOOTER */}
          <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3 rounded-b-3xl">
            <button
              type="button"
              onClick={onClose}
              className="px-5 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-100 shadow-2xs transition-colors cursor-pointer"
            >
              Hủy Bỏ
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 px-6 py-2.5 text-sm font-extrabold text-slate-900 bg-amber-400 hover:bg-amber-300 rounded-xl shadow-xs transition-all cursor-pointer disabled:opacity-50"
            >
              <Save className="w-4 h-4" /> {isSubmitting ? 'Đang lưu...' : 'Lưu Lô Hàng Mới'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
