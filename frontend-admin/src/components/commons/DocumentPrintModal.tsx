import React from 'react';

export interface DocumentPrintItem {
  skuCode?: string;
  productName: string;
  batchCode?: string;
  uoMName: string;
  quantity: number;
  unitPrice?: number;
  totalPrice?: number;
  qcStatus?: string;
  note?: string;
}

export interface DocumentSignature {
  title: string;
  subtitle?: string;
  name?: string;
}

export interface DocumentPrintProps {
  isOpen: boolean;
  onClose: () => void;
  documentTitle: string;
  documentSubtitle?: string;
  documentCode: string;
  documentDate: string | Date;
  warehouseName?: string;
  creatorName?: string;

  // Đối tác (Khách hàng, Nhà cung cấp...)
  partyTitle?: string;
  partyName: string;
  partyPhone?: string;
  partyAddress?: string;

  // Tham chiếu giao dịch
  referenceCode?: string;
  paymentMethodName?: string;
  paymentStatusName?: string;
  notes?: string;

  // Tài chính tổng hợp
  subTotal?: number;
  discountAmount?: number;
  shippingFee?: number;
  totalAmount?: number;
  codAmount?: number;

  // Danh sách mặt hàng
  items: DocumentPrintItem[];

  // Khối chữ ký (tuỳ biến, nếu không truyền sẽ dùng mặc định 4 bên)
  signatures?: DocumentSignature[];
}

export const DocumentPrintModal: React.FC<DocumentPrintProps> = ({
  isOpen,
  onClose,
  documentTitle,
  documentSubtitle = 'Hệ thống chuỗi thực phẩm sạch & bảo quản chuỗi lạnh Solaris',
  documentCode,
  documentDate,
  warehouseName,
  creatorName,
  partyTitle = 'Khách hàng / Người nhận',
  partyName,
  partyPhone,
  partyAddress,
  referenceCode,
  paymentMethodName,
  paymentStatusName,
  notes,
  subTotal,
  discountAmount,
  shippingFee,
  totalAmount,
  codAmount,
  items,
  signatures,
}) => {
  if (!isOpen) return null;

  const formattedDate =
    typeof documentDate === 'string'
      ? new Date(documentDate).toLocaleDateString('vi-VN', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
        })
      : documentDate.toLocaleDateString('vi-VN');

  const nowPrintTime = new Date().toLocaleString('vi-VN');

  // Chữ ký mặc định nếu không truyền
  const defaultSignatures: DocumentSignature[] = [
    {
      title: 'Người Lập Phiếu',
      subtitle: '(Ký, ghi rõ họ tên)',
      name: creatorName || 'Người lập',
    },
    {
      title: 'Thủ Kho / KCS',
      subtitle: '(Ký xác nhận kiểm đếm)',
      name: warehouseName ? `Kho: ${warehouseName}` : '........................',
    },
    {
      title: 'Người Giao / Tài Xế',
      subtitle: '(Ký nhận bàn giao)',
      name: '........................',
    },
    {
      title: 'Người Nhận Hàng',
      subtitle: '(Kiểm tra và ký nhận)',
      name: partyName || '........................',
    },
  ];

  const displaySignatures = signatures || defaultSignatures;

  // Kiểm tra xem có cần cột đơn giá / thành tiền không
  const hasPricing = items.some((it) => it.unitPrice !== undefined || it.totalPrice !== undefined);
  // Kiểm tra xem có cột QC không
  const hasQC = items.some((it) => it.qcStatus !== undefined);

  return (
    <>
      {/* Embedded CSS dành riêng cho in ấn khổ A4/A5 */}
      <style>{`
        @media print {
          body * {
            visibility: hidden !important;
          }
          #solaris-printable-document, #solaris-printable-document * {
            visibility: visible !important;
          }
          #solaris-printable-document {
            position: absolute !important;
            left: 0 !important;
            top: 0 !important;
            width: 100% !important;
            margin: 0 !important;
            padding: 16px !important;
            border: none !important;
            box-shadow: none !important;
            background: white !important;
          }
          .no-print {
            display: none !important;
          }
          @page {
            size: A4 portrait;
            margin: 10mm;
          }
        }
      `}</style>

      {/* Modal Container */}
      <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 overflow-y-auto no-print">
        <div className="bg-white rounded-2xl shadow-2xl w-full max-w-3xl overflow-hidden border border-slate-200 my-6 flex flex-col max-h-[90vh]">
          {/* Header Action Bar */}
          <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between shrink-0">
            <div>
              <span className="text-xs font-bold text-slate-500 uppercase tracking-wider block">
                Bản xem trước chứng từ
              </span>
              <h3 className="text-base font-black text-slate-900">{documentTitle}</h3>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => window.print()}
                className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-bold transition-all shadow-xs"
              >
                In Chứng Từ Ngay
              </button>
              <button
                type="button"
                onClick={onClose}
                className="w-8 h-8 rounded-xl hover:bg-slate-200 text-slate-500 flex items-center justify-center font-bold transition-colors"
                title="Đóng cửa sổ xem trước"
              >
                ✕
              </button>
            </div>
          </div>

          {/* VÙNG IN CHỨNG TỪ (PRINTABLE AREA) */}
          <div className="flex-1 overflow-y-auto p-6 bg-slate-100/50 flex justify-center">
            <div
              id="solaris-printable-document"
              className="w-full bg-white p-8 rounded-xl shadow-xs border border-slate-200 text-slate-900 font-sans space-y-6"
            >
              {/* 1. Header Thương Hiệu & Mã Chứng Từ */}
              <div className="flex justify-between items-start border-b-2 border-slate-900 pb-4">
                <div>
                  <h2 className="text-lg font-black uppercase text-slate-900 tracking-tight">
                    SOLARIS FRESH FOODS & SCM
                  </h2>
                  <p className="text-xs text-slate-500 font-medium">{documentSubtitle}</p>
                  {warehouseName && (
                    <p className="text-xs text-slate-700 font-bold mt-1">
                      Chi nhánh / Kho: <span className="text-slate-900">{warehouseName}</span>
                    </p>
                  )}
                  {creatorName && (
                    <p className="text-[11px] text-slate-500">Người lập phiếu: {creatorName}</p>
                  )}
                </div>
                <div className="text-right">
                  <span className="text-[11px] font-bold text-slate-500 uppercase block">
                    Mã chứng từ
                  </span>
                  <span className="text-base font-black font-mono text-slate-900 block tracking-wide">
                    {documentCode}
                  </span>
                  <span className="text-xs text-slate-500 block mt-0.5">
                    Ngày lập: {formattedDate}
                  </span>
                </div>
              </div>

              {/* 2. Tiêu Đề Chứng Từ */}
              <div className="text-center pt-2">
                <h1 className="text-xl font-black uppercase tracking-wider text-slate-900">
                  {documentTitle}
                </h1>
                {referenceCode && (
                  <p className="text-xs font-semibold text-slate-500 mt-1">
                    (Mã đơn / Tham chiếu gốc:{' '}
                    <span className="font-mono text-slate-800">{referenceCode}</span>)
                  </p>
                )}
              </div>

              {/* 3. Khối Thông Tin Giao Nhận & Đối Tác (2 Cột) */}
              <div className="grid grid-cols-2 gap-4 text-xs bg-slate-50 p-4 rounded-xl border border-slate-200">
                <div className="space-y-1">
                  <span className="font-bold text-slate-500 uppercase text-[10px] block tracking-wider">
                    {partyTitle}:
                  </span>
                  <p className="font-black text-sm text-slate-900">{partyName || '---'}</p>
                  {partyPhone && (
                    <p className="text-slate-700 font-semibold">SĐT liên hệ: {partyPhone}</p>
                  )}
                  {partyAddress && (
                    <p className="text-slate-600 leading-relaxed">Địa chỉ: {partyAddress}</p>
                  )}
                </div>

                <div className="space-y-1 border-l border-slate-200 pl-4">
                  <span className="font-bold text-slate-500 uppercase text-[10px] block tracking-wider">
                    Thông tin thanh toán & vận chuyển:
                  </span>
                  {paymentMethodName && (
                    <p className="text-slate-700">
                      Hình thức:{' '}
                      <span className="font-bold text-slate-900">{paymentMethodName}</span>
                    </p>
                  )}
                  {paymentStatusName && (
                    <p className="text-slate-700">
                      Trạng thái thanh toán:{' '}
                      <span className="font-bold text-slate-900">{paymentStatusName}</span>
                    </p>
                  )}
                  {codAmount !== undefined && codAmount > 0 && (
                    <p className="text-slate-900 font-black">
                      Tiền thu hộ (COD):{' '}
                      <span className="text-red-600 font-mono text-sm">
                        {codAmount.toLocaleString('vi-VN')} ₫
                      </span>
                    </p>
                  )}
                  {notes && <p className="text-slate-600 italic mt-1">Ghi chú: {notes}</p>}
                </div>
              </div>

              {/* 4. Bảng Chi Tiết Mặt Hàng */}
              <div>
                <table className="w-full text-xs text-left border-collapse border border-slate-300">
                  <thead className="bg-slate-100 text-slate-800 font-bold uppercase text-[10px]">
                    <tr>
                      <th className="p-2 border border-slate-300 w-8 text-center">STT</th>
                      <th className="p-2 border border-slate-300">Mặt hàng / Quy cách</th>
                      <th className="p-2 border border-slate-300">Số Lô (Batch)</th>
                      <th className="p-2 border border-slate-300 text-center">ĐVT</th>
                      <th className="p-2 border border-slate-300 text-right">Số Lượng</th>
                      {hasPricing && (
                        <>
                          <th className="p-2 border border-slate-300 text-right">Đơn Giá</th>
                          <th className="p-2 border border-slate-300 text-right">Thành Tiền</th>
                        </>
                      )}
                      {hasQC && (
                        <th className="p-2 border border-slate-300 text-center">Kiểm Định QC</th>
                      )}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200">
                    {items.map((item, idx) => (
                      <tr key={idx} className="hover:bg-slate-50/50">
                        <td className="p-2 border border-slate-300 text-center text-slate-500 font-semibold">
                          {idx + 1}
                        </td>
                        <td className="p-2 border border-slate-300">
                          <span className="font-bold text-slate-900">{item.productName}</span>
                          {item.skuCode && (
                            <span className="block text-[10px] text-slate-500 font-mono">
                              [{item.skuCode}]
                            </span>
                          )}
                        </td>
                        <td className="p-2 border border-slate-300 font-mono text-slate-700">
                          {item.batchCode || '---'}
                        </td>
                        <td className="p-2 border border-slate-300 text-center text-slate-700 font-medium">
                          {item.uoMName}
                        </td>
                        <td className="p-2 border border-slate-300 text-right font-black text-slate-900">
                          {item.quantity.toLocaleString('vi-VN')}
                        </td>
                        {hasPricing && (
                          <>
                            <td className="p-2 border border-slate-300 text-right font-mono text-slate-700">
                              {item.unitPrice !== undefined
                                ? `${item.unitPrice.toLocaleString('vi-VN')} ₫`
                                : '---'}
                            </td>
                            <td className="p-2 border border-slate-300 text-right font-mono font-bold text-slate-900">
                              {item.totalPrice !== undefined
                                ? `${item.totalPrice.toLocaleString('vi-VN')} ₫`
                                : '---'}
                            </td>
                          </>
                        )}
                        {hasQC && (
                          <td className="p-2 border border-slate-300 text-center font-bold text-[11px]">
                            {item.qcStatus ? (
                              <span
                                className={
                                  item.qcStatus.includes('chuẩn') || item.qcStatus.includes('nhập')
                                    ? 'text-emerald-700'
                                    : 'text-rose-700'
                                }
                              >
                                {item.qcStatus}
                              </span>
                            ) : (
                              '---'
                            )}
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* 5. Khối Tổng Kết Tài Chính (Nếu có) */}
              {(subTotal !== undefined || totalAmount !== undefined) && (
                <div className="flex justify-end pt-1">
                  <div className="w-72 space-y-1.5 text-xs text-slate-700">
                    {subTotal !== undefined && (
                      <div className="flex justify-between">
                        <span>Tiền hàng:</span>
                        <span className="font-mono font-semibold">
                          {subTotal.toLocaleString('vi-VN')} ₫
                        </span>
                      </div>
                    )}
                    {discountAmount !== undefined && discountAmount > 0 && (
                      <div className="flex justify-between text-slate-600">
                        <span>Chiết khấu / Giảm giá:</span>
                        <span className="font-mono font-semibold">
                          -{discountAmount.toLocaleString('vi-VN')} ₫
                        </span>
                      </div>
                    )}
                    {shippingFee !== undefined && shippingFee > 0 && (
                      <div className="flex justify-between text-slate-600">
                        <span>Phí vận chuyển:</span>
                        <span className="font-mono font-semibold">
                          +{shippingFee.toLocaleString('vi-VN')} ₫
                        </span>
                      </div>
                    )}
                    {totalAmount !== undefined && (
                      <div className="flex justify-between pt-2 border-t-2 border-slate-900 text-sm font-black text-slate-900">
                        <span>Tổng thanh toán:</span>
                        <span className="font-mono text-base">
                          {totalAmount.toLocaleString('vi-VN')} ₫
                        </span>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* 6. Khối Chữ Ký Đối Soát */}
              <div
                className={`grid grid-cols-${displaySignatures.length} gap-2 text-center text-xs pt-6`}
                style={{
                  gridTemplateColumns: `repeat(${displaySignatures.length}, minmax(0, 1fr))`,
                }}
              >
                {displaySignatures.map((sig, idx) => (
                  <div key={idx} className="flex flex-col items-center">
                    <span className="font-bold text-slate-900 block">{sig.title}</span>
                    {sig.subtitle && (
                      <span className="text-[10px] text-slate-500 italic block">
                        {sig.subtitle}
                      </span>
                    )}
                    <div className="h-16"></div>
                    <span className="font-semibold text-slate-800 text-[11px]">{sig.name}</span>
                  </div>
                ))}
              </div>

              {/* 7. Footer Bản In */}
              <div className="border-t border-slate-200 pt-3 flex justify-between text-[10px] text-slate-400">
                <span>In ngày: {nowPrintTime}</span>
                <span>Hệ thống Quản trị Chuỗi Cung ứng Solaris ERP - Bản in điện tử</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};
