import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
    ShoppingBag,
    FileText,
    Package,
    PackageCheck,
    Truck,
    RotateCcw,
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

import { orderApi } from '../../api/orderApi';
import {
    Order,
    OrderStatus,
    OrderStatusLabels,
    OrderStatusColors,
    PaymentStatusLabels,
    PaymentStatusColors,
    PaymentMethodLabels
} from '../../types/order';

const OrderDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [order, setOrder] = useState<Order | null>(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

    // Cancel modal
    const [cancelModalOpen, setCancelModalOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState('');
    const [actionLoading, setActionLoading] = useState(false);

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

    const fetchOrder = useCallback(async () => {
        if (!id) return;
        try {
            setLoading(true);
            const data = await orderApi.getById(Number(id));
            setOrder(data);
        } catch (error) {
            console.error('Lỗi tải đơn hàng:', error);
            showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU ĐƠN HÀNG');
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => {
        fetchOrder();
    }, [fetchOrder]);

    const handleCancelOrder = async () => {
        if (!order || !cancelReason.trim()) return;
        try {
            setActionLoading(true);
            await orderApi.cancel(order.id, cancelReason.trim());
            showToast('success', 'ĐÃ HỦY ĐƠN HÀNG VÀ HOÀN TRẢ TỒN KHO!');
            setCancelModalOpen(false);
            setCancelReason('');
            fetchOrder(); // Reload lại data
        } catch (err: any) {
            showToast('error', err.response?.data?.message || 'Lỗi server: Không thể hủy đơn hàng!');
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
    
    if (!order) return <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy đơn hàng!</div>;

    return (
        <DetailPageContainer>
            <Toast {...toast} />

            <DetailHeader
                title="Chi Tiết Đơn Bán Hàng"
                subtitle={<>Mã hệ thống: <span className="font-bold text-slate-800">{order.orderCode}</span></>}
                onBack={() => navigate('/orders')}
                icon={ShoppingBag}
            />

            {/* ================= THÀNH CÔNG CỤ (ACTION TOOLBAR) ================= */}
            <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
                
                <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                        <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">Trạng thái:</span>
                        <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${OrderStatusColors[order.status]}`}>
                            {OrderStatusLabels[order.status]}
                        </span>
                        <span className={`inline-flex items-center px-2.5 py-1 rounded text-[11px] font-bold border shadow-sm ${PaymentStatusColors[order.paymentStatus]}`}>
                            {PaymentStatusLabels[order.paymentStatus]}
                        </span>
                    </div>

                    <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>

                    {/* Nút luồng đi tiếp: Nằm bên trái */}
                    <div className="flex items-center gap-2">
                        {/* Nút Đẩy Đơn Sang GHN */}
                        {!order.trackingCode && order.status !== OrderStatus.Cancelled && order.status !== OrderStatus.Completed && (
                            <button
                                onClick={async () => {
                                    try {
                                        setActionLoading(true);
                                        const res = await orderApi.createGhnOrder(order.id);
                                        showToast('success', `ĐÃ TẠO VẬN ĐƠN GHN: ${res.orderCode}`);
                                        fetchOrder();
                                    } catch (err: any) {
                                        showToast('error', err.response?.data?.message || 'Lỗi đẩy đơn sang GHN');
                                    } finally {
                                        setActionLoading(false);
                                    }
                                }}
                                disabled={actionLoading}
                                className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg font-bold text-sm hover:bg-blue-700 transition-all shadow-sm shadow-blue-200 disabled:opacity-50"
                            >
                                <Truck size={16} /> Tạo Đơn GHN
                            </button>
                        )}

                        {(order.status === OrderStatus.Confirmed || order.status === OrderStatus.Processing) && (
                            <button
                                onClick={() => navigate(`/inventory-issues/create?orderId=${order.id}`)}
                                className="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg font-bold text-sm hover:bg-indigo-700 transition-all shadow-sm shadow-indigo-200"
                            >
                                <PackageCheck size={16} /> Xuất Kho Đơn Hàng
                            </button>
                        )}

                        {(order.status === OrderStatus.Confirmed || order.status === OrderStatus.Processing) && (
                            <button
                                onClick={() => navigate(`/inventory-transfers/create?orderId=${order.id}`)}
                                className="flex items-center gap-2 px-4 py-2 bg-amber-500 text-white rounded-lg font-bold text-sm hover:bg-amber-600 transition-all shadow-sm shadow-amber-200"
                            >
                                <Truck size={16} /> Chuyển Kho Bổ Sung
                            </button>
                        )}

                        {(order.status === OrderStatus.Shipping || order.status === OrderStatus.Completed) && (
                            <button
                                onClick={() => navigate(`/customer-returns/create?orderId=${order.id}`)}
                                className="flex items-center gap-2 px-4 py-2 bg-teal-600 text-white rounded-lg font-bold text-sm hover:bg-teal-700 transition-all shadow-sm shadow-teal-200"
                            >
                                <RotateCcw size={16} /> Tiếp Nhận Khách Trả
                            </button>
                        )}
                    </div>
                </div>

                {/* Nút Hủy Đơn: Nằm sát góc phải */}
                {(order.status === OrderStatus.Draft || order.status === OrderStatus.Pending || order.status === OrderStatus.Confirmed || order.status === OrderStatus.Processing) && (
                    <button
                        onClick={() => setCancelModalOpen(true)}
                        className="flex items-center gap-2 px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-50 transition-colors shadow-sm"
                    >
                        <XCircle size={16} strokeWidth={2.5} /> Hủy Đơn Này
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
                    label={`2. CHI TIẾT MẶT HÀNG (${order.details?.length || 0})`}
                />
            </TabGroup>

            {/* ================= TAB 1: THÔNG TIN ================= */}
            {activeTab === 'info' && (
                <DetailCard>
                    <div className="p-8 flex flex-col gap-8 animate-in fade-in duration-300">
                        {order.status === OrderStatus.Cancelled && order.cancellationReason && (
                            <div className="bg-rose-50 border border-rose-200 p-4 rounded-xl flex gap-3 items-start shadow-sm">
                                <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                                <div className="flex flex-col gap-1">
                                    <h4 className="font-bold text-sm text-rose-800 uppercase tracking-wider">Lý do hủy đơn hàng:</h4>
                                    <p className="text-sm text-rose-700 font-medium">{order.cancellationReason}</p>
                                </div>
                            </div>
                        )}

                        <DetailSection title="Thông Tin Khách Hàng & Giao Hàng" dotColor="bg-indigo-400">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                                <InfoField label="Khách hàng" value={<span className="font-bold text-slate-800">{order.customerName}</span>} />
                                <InfoField label="SĐT khách hàng" value={order.customerPhone || '---'} />
                                <InfoField label="Người nhận hàng" value={<span className="font-bold text-indigo-700">{order.receiverName || order.customerName}</span>} />
                                <InfoField label="SĐT nhận hàng" value={order.receiverPhone || order.customerPhone || '---'} />

                                <div className="md:col-span-2 lg:col-span-4">
                                    <InfoField label="Địa chỉ giao hàng" value={order.deliveryAddress || '---'} />
                                </div>
                            </div>
                        </DetailSection>

                        <DetailSection title="Điều Phối & Thanh Toán" dotColor="bg-emerald-400">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                                <InfoField label="Kho xử lý đơn" value={<span className="font-bold text-indigo-700">{order.warehouseName || 'Chưa gán kho'}</span>} />
                                <InfoField label="Phương thức thanh toán" value={<span className="font-medium text-slate-800">{PaymentMethodLabels[order.paymentMethod]}</span>} />
                                
                                <div className="flex flex-col items-start">
                                    <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">Ngày đặt hàng</span>
                                    <DateCell isoString={order.orderDate} />
                                </div>
                                
                                <InfoField label="Tổng tiền thanh toán" value={<span className="font-black text-emerald-600 text-[17px]">{formatCurrency(order.totalAmount)}</span>} />

                                {order.trackingCode && (
                                    <InfoField 
                                        label="Mã vận đơn GHN" 
                                        value={
                                            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-blue-50 text-blue-700 font-mono font-bold rounded-md border border-blue-200">
                                                🚚 {order.trackingCode}
                                            </span>
                                        } 
                                    />
                                )}

                                {order.paymentTransactionNo && (
                                    <InfoField 
                                        label="Mã GD VNPay" 
                                        value={<span className="font-mono text-xs font-semibold text-slate-700">{order.paymentTransactionNo}</span>} 
                                    />
                                )}
                            </div>

                            {order.note && (
                                <div className="mt-6 pt-6 border-t border-slate-100">
                                    <InfoField label="Ghi chú của khách" value={<span className="italic text-slate-600 font-medium">{order.note}</span>} />
                                </div>
                            )}
                        </DetailSection>
                    </div>
                </DetailCard>
            )}

            {/* ================= TAB 2: MẶT HÀNG ================= */}
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
                                        <th className="px-4 py-4 text-center min-w-20">ĐVT</th>
                                        <th className="px-4 py-4 text-right min-w-30">Đơn Giá</th>
                                        <th className="px-4 py-4 text-center bg-indigo-50/50 min-w-25">SL Đặt</th>
                                        <th className="px-4 py-4 text-center bg-emerald-50/50 min-w-25" title="Số lượng đã được xuất kho thành công">Đã Xuất Kho</th>
                                        <th className="px-4 py-4 text-right min-w-30">Chiết Khấu</th>
                                        <th className="px-4 py-4 text-right min-w-37.5">Thành Tiền</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100 text-sm">
                                    {order.details?.map((item, idx) => (
                                        <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                                            <td className="px-4 py-3 text-center text-slate-400 font-medium">{idx + 1}</td>
                                            <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                                            <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                                            <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>
                                            <td className="px-4 py-3 text-right font-medium text-slate-600">{formatCurrency(item.unitPrice)}</td>
                                            
                                            {/* Cột Số lượng Đặt */}
                                            <td className="px-4 py-3 text-center border-l border-indigo-100 bg-indigo-50/20">
                                                <span className="font-bold text-[15px] text-indigo-700">{item.quantity}</span>
                                            </td>
                                            
                                            {/* Cột Số lượng Đã Xuất */}
                                            <td className="px-4 py-3 text-center border-l border-emerald-100 bg-emerald-50/20">
                                                <span className={`font-black text-[15px] ${item.issuedQuantity < item.quantity ? 'text-amber-500' : 'text-emerald-600'}`}>
                                                    {item.issuedQuantity}
                                                </span>
                                            </td>
                                            
                                            <td className="px-4 py-3 text-right text-slate-500 border-l border-slate-100">{formatCurrency(item.discountAmount)}</td>
                                            <td className="px-4 py-3 text-right font-black text-slate-800">{formatCurrency(item.totalPrice)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                                <tfoot className="bg-slate-50/80 border-t border-slate-200 text-sm">
                                    <tr>
                                        <td colSpan={8} className="px-4 py-3 text-right font-bold text-slate-500 uppercase tracking-wider text-xs">Tiền hàng:</td>
                                        <td className="px-4 py-3 text-right font-bold text-slate-700">{formatCurrency(order.subTotal)}</td>
                                    </tr>
                                    <tr>
                                        <td colSpan={8} className="px-4 py-2 text-right font-bold text-slate-500 uppercase tracking-wider text-xs">Phí vận chuyển:</td>
                                        <td className="px-4 py-2 text-right font-bold text-slate-700">{formatCurrency(order.shippingFee)}</td>
                                    </tr>
                                    <tr className="border-t border-slate-200 bg-emerald-50/30">
                                        <td colSpan={8} className="px-4 py-4 text-right text-emerald-800 uppercase tracking-wider text-xs font-black">Tổng Thanh Toán:</td>
                                        <td className="px-4 py-4 text-right text-[17px] font-black text-emerald-600">{formatCurrency(order.totalAmount)}</td>
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
                                <AlertCircle size={20} strokeWidth={2.5} /> Xác Nhận Hủy Đơn Hàng
                            </h3>
                        </div>
                        <div className="p-6">
                            <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                                Bạn đang thực hiện hủy đơn <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">{order.orderCode}</strong>. 
                                Tồn kho đã giữ chỗ (Reserved) sẽ tự động được hệ thống hoàn trả lại thành tồn kho khả dụng (Available).
                            </p>
                            
                            <div>
                                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-2">
                                    Lý do hủy đơn <span className="text-rose-500">*</span>
                                </label>
                                <textarea
                                    className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-500 focus:border-rose-500 outline-none resize-none text-slate-800 font-medium"
                                    rows={3}
                                    placeholder="Khách đổi ý, đặt nhầm sản phẩm, kho hết hàng..."
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
                                onClick={handleCancelOrder}
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

export default OrderDetail;