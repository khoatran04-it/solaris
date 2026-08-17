import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    PackageCheck,
    FileText,
    Package,
    CheckCircle,
    XCircle,
    AlertCircle,
    Loader2
} from 'lucide-react';

import {
    DetailPageContainer,
    DetailHeader,
    TabGroup,
    TabButton,
    DetailCard,
    DetailSection,
    InfoField
} from '../../components/commons/TabUI';
import { Toast } from '../../components/commons/Toast';
import { DateCell, DateTimeCell } from '../../components/commons/ListUI';

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import {
    InventoryIssue,
    InventoryIssueStatus,
    InventoryIssueStatusLabels,
    InventoryIssueStatusColors
} from '../../types/inventoryIssue';

const InventoryIssueDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [issue, setIssue] = useState<InventoryIssue | null>(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

    // Actions
    const [actionLoading, setActionLoading] = useState(false);
    const [cancelModalOpen, setCancelModalOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState('');

    // Toast
    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error' | 'warning'; message: string }>({
        show: false,
        type: 'success',
        message: '',
    });

    const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    const fetchIssue = useCallback(async () => {
        if (!id) return;
        try {
            setLoading(true);
            const data = await inventoryIssueApi.getById(Number(id));
            setIssue(data);
        } catch (error) {
            console.error('Error fetching issue:', error);
            showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU XUẤT KHO');
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => {
        fetchIssue();
    }, [fetchIssue]);

    const handleCompleteIssue = async () => {
        if (!issue) return;
        try {
            setActionLoading(true);
            await inventoryIssueApi.complete(issue.id);
            showToast('success', 'XUẤT KHO THÀNH CÔNG! ĐÃ GHI SỔ CÁI VÀ TRỪ TỒN KHO.');
            fetchIssue(); // Reload lại data
        } catch (err: any) {
            showToast('error', err.response?.data?.message || 'Không thể hoàn tất xuất kho!');
        } finally {
            setActionLoading(false);
        }
    };

    const handleCancelIssue = async () => {
        if (!issue || !cancelReason.trim()) return;
        try {
            setActionLoading(true);
            await inventoryIssueApi.cancel(issue.id, cancelReason.trim());
            showToast('success', 'ĐÃ HỦY PHIẾU XUẤT KHO!');
            setCancelModalOpen(false);
            setCancelReason('');
            fetchIssue(); // Reload lại data
        } catch (err: any) {
            showToast('error', err.response?.data?.message || 'Không thể hủy phiếu xuất!');
        } finally {
            setActionLoading(false);
        }
    };

    const formatCurrency = (amount: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount || 0);
    };

    if (loading) {
        return (
            <div className="h-full flex items-center justify-center bg-slate-50/30 p-12">
                <Loader2 className="w-10 h-10 animate-spin text-indigo-500" />
            </div>
        );
    }

    if (!issue) return <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy chứng từ xuất kho!</div>;

    const totalAmount = issue.details?.reduce((sum, item) => sum + (Number(item.totalPrice) || 0), 0) || 0;

    return (
        <DetailPageContainer>
            <Toast {...toast} />

            <DetailHeader
                title="Chi Tiết Phiếu Xuất Kho"
                subtitle={<>Mã chứng từ: <span className="font-bold text-slate-800">{issue.issueCode}</span></>}
                onBack={() => navigate('/inventory-issues')}
                icon={PackageCheck}
            />

            {/* ================= THÀNH CÔNG CỤ (ACTION TOOLBAR) ================= */}
            <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
                
                <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                        <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">Trạng thái:</span>
                        <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${InventoryIssueStatusColors[issue.status]}`}>
                            {InventoryIssueStatusLabels[issue.status]}
                        </span>
                    </div>

                    {/* Vách ngăn nếu có nút tiếp theo */}
                    {(issue.status === InventoryIssueStatus.Pending || issue.status === InventoryIssueStatus.Picking) && (
                        <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>
                    )}

                    {/* Nút Hoàn tất (Nằm bên trái) */}
                    {(issue.status === InventoryIssueStatus.Pending || issue.status === InventoryIssueStatus.Picking) && (
                        <button
                            onClick={handleCompleteIssue}
                            disabled={actionLoading}
                            className="flex items-center gap-2 px-5 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-200 disabled:opacity-50"
                        >
                            {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle size={16} />}
                            Hoàn Tất Xuất Kho
                        </button>
                    )}
                </div>

                {/* Nút Hủy Phiếu: Nằm góc phải */}
                {(issue.status === InventoryIssueStatus.Pending || issue.status === InventoryIssueStatus.Picking) && (
                    <button
                        onClick={() => setCancelModalOpen(true)}
                        disabled={actionLoading}
                        className="flex items-center gap-2 px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-50 transition-colors shadow-sm"
                    >
                        <XCircle size={16} strokeWidth={2.5} /> Hủy Phiếu Xuất
                    </button>
                )}
            </div>

            {/* ================= TABS ================= */}
            <TabGroup>
                <TabButton
                    active={activeTab === 'info'}
                    onClick={() => setActiveTab('info')}
                    icon={FileText}
                    label="1. THÔNG TIN CHUNG"
                />
                <TabButton
                    active={activeTab === 'items'}
                    onClick={() => setActiveTab('items')}
                    icon={Package}
                    label={`2. CHI TIẾT MẶT HÀNG & LÔ HÀNG (${issue.details?.length || 0})`}
                />
            </TabGroup>

            {/* ================= TAB 1: THÔNG TIN CHUNG ================= */}
            {activeTab === 'info' && (
                <DetailCard>
                    <div className="p-8 flex flex-col gap-8 animate-in fade-in duration-300">
                        {issue.status === InventoryIssueStatus.Cancelled && issue.cancellationReason && (
                            <div className="bg-rose-50 border border-rose-200 p-4 rounded-xl flex gap-3 items-start shadow-sm">
                                <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                                <div className="flex flex-col gap-1">
                                    <h4 className="font-bold text-sm text-rose-800 uppercase tracking-wider">Lý do hủy phiếu:</h4>
                                    <p className="text-sm text-rose-700 font-medium">{issue.cancellationReason}</p>
                                </div>
                            </div>
                        )}

                        <DetailSection title="Thông Tin Xuất Kho" dotColor="bg-indigo-400">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                                <InfoField label="Kho xuất hàng" value={<span className="font-bold text-indigo-700">{issue.warehouseName}</span>} />
                                <InfoField
                                    label="Đơn hàng gốc"
                                    value={
                                        issue.orderCode ? (
                                            <button
                                                onClick={() => navigate(`/orders/${issue.orderId}`)}
                                                className="text-indigo-600 hover:underline font-bold"
                                            >
                                                {issue.orderCode}
                                            </button>
                                        ) : (
                                            <span className="italic text-slate-500">Xuất nội bộ</span>
                                        )
                                    }
                                />
                                <InfoField label="Thủ kho thực hiện" value={issue.issuedByName} />
                                
                                <div className="flex flex-col items-start">
                                    <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">Ngày xuất hàng</span>
                                    <DateCell isoString={issue.issueDate} />
                                </div>
                            </div>
                        </DetailSection>

                        <DetailSection title="Thông Tin Người Nhận" dotColor="bg-emerald-400">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                                <InfoField label="Tên người nhận" value={<span className="font-bold text-slate-800">{issue.receiverName || '---'}</span>} />
                                <InfoField label="Số điện thoại" value={issue.receiverPhone || '---'} />
                                <div className="md:col-span-2 lg:col-span-4">
                                    <InfoField label="Địa chỉ giao hàng" value={issue.deliveryAddress || '---'} />
                                </div>
                            </div>

                            {issue.note && (
                                <div className="mt-6 pt-6 border-t border-slate-100">
                                    <InfoField label="Ghi chú xuất kho" value={<span className="italic text-slate-600 font-medium">{issue.note}</span>} />
                                </div>
                            )}
                        </DetailSection>
                    </div>
                </DetailCard>
            )}

            {/* ================= TAB 2: CHI TIẾT MẶT HÀNG ================= */}
            {activeTab === 'items' && (
                <DetailCard>
                    <div className="p-8 flex flex-col gap-6 animate-in fade-in duration-300">
                        <div className="overflow-x-auto border border-slate-200 rounded-xl shadow-sm">
                            <table className="w-full text-left whitespace-nowrap">
                                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                                    <tr>
                                        <th className="px-4 py-4 text-center w-12">#</th>
                                        <th className="px-4 py-4 min-w-30">Mã SKU</th>
                                        <th className="px-4 py-4 min-w-50">Tên Sản Phẩm</th>
                                        <th className="px-4 py-4 min-w-37.5">Mã Lô (BatchCode)</th>
                                        <th className="px-4 py-4 text-center min-w-20">ĐVT</th>
                                        <th className="px-4 py-4 text-center bg-emerald-50/50 min-w-30">SL Thực Xuất</th>
                                        <th className="px-4 py-4 text-right min-w-30">Đơn Giá</th>
                                        <th className="px-4 py-4 text-right min-w-37.5">Thành Tiền</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 text-sm">
                                    {issue.details?.map((item, idx) => (
                                        <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                                            <td className="px-4 py-3 text-center text-slate-400 font-medium">{idx + 1}</td>
                                            <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                                            <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                                            <td className="px-4 py-3 font-black text-indigo-600">{item.batchCode}</td>
                                            <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>
                                            
                                            {/* Cột Số Lượng Thực Xuất nổi bật */}
                                            <td className="px-4 py-3 text-center border-l border-emerald-100 bg-emerald-50/20">
                                                <span className="font-black text-[15px] text-emerald-700">{item.quantity}</span>
                                            </td>
                                            
                                            <td className="px-4 py-3 text-right font-medium text-slate-600 border-l border-slate-100">{formatCurrency(item.unitPrice)}</td>
                                            <td className="px-4 py-3 text-right font-black text-slate-800">{formatCurrency(item.totalPrice)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                                <tfoot className="bg-slate-50/80 border-t border-slate-200 text-sm">
                                    <tr className="border-t border-slate-200 bg-emerald-50/30">
                                        <td colSpan={7} className="px-4 py-4 text-right text-emerald-800 uppercase tracking-wider text-xs font-black">Tổng Giá Trị Xuất Kho:</td>
                                        <td className="px-4 py-4 text-right text-[17px] font-black text-emerald-600">{formatCurrency(totalAmount)}</td>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>
                </DetailCard>
            )}

            {/* ================= CANCEL MODAL ================= */}
            {cancelModalOpen && (
                <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
                    <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
                        <div className="px-6 py-4 border-b border-rose-100 bg-rose-50/50">
                            <h3 className="text-lg font-black text-rose-700 flex items-center gap-2">
                                <AlertCircle size={20} strokeWidth={2.5} /> Xác Nhận Hủy Phiếu Xuất
                            </h3>
                        </div>
                        <div className="p-6">
                            <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                                Bạn đang thực hiện hủy phiếu xuất kho <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">{issue.issueCode}</strong>.
                            </p>
                            
                            <div>
                                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-2">
                                    Lý do hủy <span className="text-rose-500">*</span>
                                </label>
                                <textarea
                                    className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-500 focus:border-rose-500 outline-none resize-none text-slate-800 font-medium"
                                    rows={3}
                                    placeholder="Giao sai địa chỉ, hủy theo yêu cầu khách..."
                                    value={cancelReason}
                                    onChange={(e) => setCancelReason(e.target.value)}
                                    autoFocus
                                />
                            </div>
                        </div>
                        <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3">
                            <button
                                onClick={() => { setCancelModalOpen(false); setCancelReason(''); }}
                                disabled={actionLoading}
                                className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors shadow-sm"
                            >
                                Đóng Lại
                            </button>
                            <button
                                onClick={handleCancelIssue}
                                disabled={actionLoading || !cancelReason.trim()}
                                className="px-5 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 shadow-sm flex items-center gap-2"
                            >
                                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                                Xác Nhận Hủy
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </DetailPageContainer>
    );
};

export default InventoryIssueDetail;