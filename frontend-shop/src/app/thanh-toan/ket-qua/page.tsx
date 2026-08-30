'use client';

import React, { useEffect, useState, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { 
    CheckCircle2, 
    XCircle, 
    ShoppingBag, 
    FileText,
    ShieldCheck
} from 'lucide-react';
import shopPaymentApi from '@/api/shopPaymentApi';
import { formatVND } from '@/lib/utils';
import { VnPayCallbackResult } from '@/types/payment';

function KetQuaContent() {
    const searchParams = useSearchParams();

    const [loading, setLoading] = useState(true);
    const [result, setResult] = useState<VnPayCallbackResult | null>(null);

    useEffect(() => {
        const queryStr = searchParams.toString();
        if (!queryStr) {
            setLoading(false);
            return;
        }

        // Gọi backend xác thực chữ ký số VNPay Callback
        shopPaymentApi.getVnPayCallback(queryStr)
            .then((res: VnPayCallbackResult) => {
                setResult(res);
            })
            .catch(() => {
                // Fallback nếu có lỗi mạng: parse trực tiếp từ searchParams
                const rspCode = searchParams.get('vnp_ResponseCode');
                const orderCode = searchParams.get('vnp_TxnRef');
                const amountRaw = Number(searchParams.get('vnp_Amount')) || 0;
                const txnNo = searchParams.get('vnp_TransactionNo');
                const bankCode = searchParams.get('vnp_BankCode');

                setResult({
                    isSuccess: rspCode === '00',
                    orderCode: orderCode || undefined,
                    transactionNo: txnNo || undefined,
                    responseCode: rspCode || undefined,
                    bankCode: bankCode || undefined,
                    amount: amountRaw / 100,
                    message: rspCode === '00' ? 'Thanh toán thành công' : 'Giao dịch không thành công'
                });
            })
            .finally(() => {
                setLoading(false);
            });
    }, [searchParams]);

    if (loading) {
        return (
            <div className="max-w-xl mx-auto py-20 px-4 text-center space-y-4">
                <div className="w-12 h-12 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin mx-auto" />
                <p className="text-xs font-bold text-slate-600">Đang đối soát giao dịch với VNPay Gateway...</p>
            </div>
        );
    }

    const isSuccess = result?.isSuccess ?? (searchParams.get('vnp_ResponseCode') === '00');
    const orderCode = result?.orderCode || searchParams.get('vnp_TxnRef');
    const amount = result?.amount || (Number(searchParams.get('vnp_Amount')) || 0) / 100;
    const transactionNo = result?.transactionNo || searchParams.get('vnp_TransactionNo');
    const bankCode = result?.bankCode || searchParams.get('vnp_BankCode');

    return (
        <div className="max-w-xl mx-auto py-12 px-4 sm:px-6">
            <div className="bg-white rounded-3xl border border-slate-200 p-6 sm:p-8 shadow-xs text-center space-y-6">
                
                {/* Icon Header */}
                <div className="flex justify-center">
                    {isSuccess ? (
                        <div className="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-600 shadow-lg shadow-emerald-600/20">
                            <CheckCircle2 className="w-10 h-10" />
                        </div>
                    ) : (
                        <div className="w-16 h-16 rounded-full bg-rose-100 flex items-center justify-center text-rose-600 shadow-lg shadow-rose-600/20">
                            <XCircle className="w-10 h-10" />
                        </div>
                    )}
                </div>

                {/* Status Message */}
                <div className="space-y-1">
                    <h1 className="text-xl sm:text-2xl font-black text-slate-900">
                        {isSuccess ? 'Thanh Toán Thành Công!' : 'Thanh Toán Không Thành Công'}
                    </h1>
                    <p className="text-xs text-slate-500">
                        {isSuccess 
                            ? 'Cảm ơn bạn đã tin chọn nông sản tươi sạch Solaris. Đơn hàng đã được xác nhận thanh toán.' 
                            : 'Giao dịch bị hủy hoặc xảy ra lỗi trong quá trình xử lý qua VNPay.'}
                    </p>
                </div>

                {/* Transaction Details Box */}
                <div className="p-4 bg-slate-50 rounded-2xl border border-slate-100 text-left space-y-2.5 text-xs">
                    {orderCode && (
                        <div className="flex justify-between text-slate-600">
                            <span>Mã đơn hàng:</span>
                            <span className="font-bold text-slate-900">{orderCode}</span>
                        </div>
                    )}
                    {amount > 0 && (
                        <div className="flex justify-between text-slate-600">
                            <span>Số tiền thanh toán:</span>
                            <span className="font-black text-emerald-700">{formatVND(amount)}</span>
                        </div>
                    )}
                    {transactionNo && (
                        <div className="flex justify-between text-slate-600">
                            <span>Mã giao dịch VNPay:</span>
                            <span className="font-mono text-slate-800">{transactionNo}</span>
                        </div>
                    )}
                    {bankCode && (
                        <div className="flex justify-between text-slate-600">
                            <span>Ngân hàng / Ví:</span>
                            <span className="font-semibold text-slate-800">{bankCode}</span>
                        </div>
                    )}
                    <div className="flex justify-between text-slate-600">
                        <span>Cổng thanh toán:</span>
                        <span className="font-semibold text-blue-700">VNPay Sandbox</span>
                    </div>
                </div>

                {/* CTAs */}
                <div className="space-y-3 pt-2">
                    {orderCode && (
                        <Link
                            href={`/tai-khoan/don-hang/${orderCode}`}
                            className="w-full py-3.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-2xl text-xs font-bold transition-all shadow-md flex items-center justify-center gap-2"
                        >
                            <FileText className="w-4 h-4" />
                            <span>Xem Chi Tiết Đơn Hàng</span>
                        </Link>
                    )}

                    <Link
                        href="/san-pham"
                        className="w-full py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-2xl text-xs font-bold transition-all flex items-center justify-center gap-2"
                    >
                        <ShoppingBag className="w-4 h-4" />
                        <span>Tiếp Tục Mua Sắm</span>
                    </Link>
                </div>

                <div className="flex items-center justify-center gap-1.5 text-[11px] text-slate-400">
                    <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
                    <span>Giao dịch được bảo mật bởi VNPay & Solaris Security</span>
                </div>

            </div>
        </div>
    );
}

export default function KetQuaThanhToanPage() {
    return (
        <Suspense fallback={
            <div className="max-w-xl mx-auto py-20 px-4 text-center">
                <div className="w-12 h-12 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin mx-auto" />
            </div>
        }>
            <KetQuaContent />
        </Suspense>
    );
}
