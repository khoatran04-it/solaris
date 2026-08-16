import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ShoppingCart, FileText, Package, AlertCircle, CheckCircle, Clock } from 'lucide-react';

// Common UI
import { 
    DetailPageContainer, 
    DetailHeader, 
    TabGroup, 
    TabButton, 
    DetailCard, 
    DetailSection, 
    InfoField 
} from '../../components/commons/TabUI'; // Giữ nguyên đường dẫn import của sếp
import { Toast } from '../../components/commons/Toast';

// API & Types
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import { 
    PurchaseOrder, 
    PurchaseOrderStatus,
    PurchaseOrderStatusLabels,
    PurchaseOrderStatusColors
} from '../../types/purchaseOrder';

const PurchaseOrderDetail: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    
    // --- STATES ---
    const [po, setPo] = useState<PurchaseOrder | null>(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');
    const [actionLoading, setActionLoading] = useState(false);
    
    // Modal & Toast
    const [toast, setToast] = useState<{ show: boolean, type: 'success' | 'error', message: string }>({ show: false, type: 'success', message: '' });
    const [cancelModalOpen, setCancelModalOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState('');

    // --- EFFECTS ---
    const fetchPo = useCallback(async () => {
        if (!id) return;
        try {
            setLoading(true);
            const data = await purchaseOrderApi.getById(Number(id));
            setPo(data);
        } catch (error) {
            console.error('Error fetching PO:', error);
            showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU ĐƠN MUA HÀNG');
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => {
        fetchPo();
    }, [fetchPo]);

    // --- HANDLERS ---
    const showToast = (type: 'success' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // Hàm gọi API Update Trạng Thái (Cần viết thêm hàm updateStatus trong purchaseOrderApi.ts)
    const handleUpdateStatus = async (status: PurchaseOrderStatus, reason?: string) => {
        if (!po) return;
        
        try {
            setActionLoading(true);
            
            // Giả định sếp có hàm PATCH cập nhật trạng thái riêng biệt để tối ưu
            await purchaseOrderApi.updateStatus(po.id, { 
                status, 
                cancellationReason: reason,
            });
            
            showToast('success', 'CẬP NHẬT TRẠNG THÁI THÀNH CÔNG');
            
            if (cancelModalOpen) {
                setCancelModalOpen(false);
                setCancelReason('');
            }
            
            fetchPo(); // Reload lại data mới nhất
        } catch (error) {
            console.error('Error updating status:', error);
            showToast('error', 'CẬP NHẬT TRẠNG THÁI THẤT BẠI');
        } finally {
            setActionLoading(false);
        }
    };

    // --- FORMATTERS ---
    const formatCurrency = (value: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0);
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return '---';
        return new Date(dateString).toLocaleDateString('vi-VN');
    };

    // --- RENDER ---
    if (loading) return <div className="p-12 text-center text-slate-500 font-medium">Đang tải dữ liệu...</div>;
    if (!po) return <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy Đơn mua hàng!</div>;

    return (
        <DetailPageContainer>
            <DetailHeader
                title="Chi Tiết Đơn Mua Hàng"
                subtitle={`Mã Đơn: ${po.orderCode}`}
                onBack={() => navigate('/purchase-orders')}
                icon={ShoppingCart}
            />

            {/* ================= THANH CÔNG CỤ (ACTION BUTTONS) ================= */}
            <div className="flex flex-wrap gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
                <div className="flex-1 flex items-center gap-3">
                    <span className="text-sm font-bold text-slate-500">Trạng thái hiện tại:</span>
                    <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${PurchaseOrderStatusColors[po.status]}`}>
                        {PurchaseOrderStatusLabels[po.status]}
                    </span>
                </div>

                {/* NÚT: DRAFT -> PROCESSING */}
                {po.status === PurchaseOrderStatus.Draft && (
                    <button
                        onClick={() => handleUpdateStatus(PurchaseOrderStatus.Processing)}
                        disabled={actionLoading}
                        className="flex items-center gap-2 px-5 py-2 bg-blue-600 text-white rounded-lg font-bold text-sm hover:bg-blue-700 transition-colors disabled:opacity-50 shadow-sm shadow-blue-200"
                    >
                        <Clock size={16} /> Chuyển Chờ Duyệt
                    </button>
                )}

                {/* NÚT: PROCESSING -> APPROVED / CANCELLED */}
                {po.status === PurchaseOrderStatus.Processing && (
                    <>
                        <button
                            onClick={() => setCancelModalOpen(true)}
                            disabled={actionLoading}
                            className="flex items-center gap-2 px-5 py-2 bg-white text-red-600 border border-red-200 rounded-lg font-bold text-sm hover:bg-red-50 transition-colors disabled:opacity-50"
                        >
                            Hủy Đơn
                        </button>
                        <button
                            onClick={() => handleUpdateStatus(PurchaseOrderStatus.Approved)}
                            disabled={actionLoading}
                            className="flex items-center gap-2 px-5 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-colors disabled:opacity-50 shadow-sm shadow-emerald-200"
                        >
                            <CheckCircle size={16} /> Duyệt Đơn Hàng
                        </button>
                    </>
                )}

                {/* NÚT: APPROVED / PARTIALLY RECEIVED -> TẠO PHIẾU NHẬP */}
                {(po.status === PurchaseOrderStatus.Approved || po.status === PurchaseOrderStatus.PartiallyReceived) && (
                    <button
                        onClick={() => navigate(`/inventory-receipts/create?poId=${po.id}`)}
                        className="flex items-center gap-2 px-5 py-2 bg-indigo-600 text-white rounded-lg font-bold text-sm hover:bg-indigo-700 transition-colors shadow-sm shadow-indigo-200"
                    >
                        <Package size={16} /> Nhận Hàng (Tạo Phiếu Nhập)
                    </button>
                )}
            </div>

            {/* ================= TABS ĐIỀU HƯỚNG ================= */}
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
                    label={`2. CHI TIẾT MẶT HÀNG (${po.details?.length || 0})`}
                />
            </TabGroup>

            {/* ================= TAB 1: THÔNG TIN ================= */}
            {activeTab === 'info' && (
                <div className="animate-in fade-in duration-300">
                    <DetailCard>
                        {/* Cảnh báo Hủy đơn */}
                        {po.status === PurchaseOrderStatus.Cancelled && po.cancellationReason && (
                            <div className="mb-6 bg-rose-50 border border-rose-200 text-rose-700 p-4 rounded-xl flex gap-3 items-start shadow-sm">
                                <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                                <div>
                                    <h4 className="font-bold text-sm uppercase tracking-wider">Lý do hủy đơn:</h4>
                                    <p className="text-sm mt-1 font-medium">{po.cancellationReason}</p>
                                </div>
                            </div>
                        )}
                        
                        <DetailSection title="Chứng Từ Giao Dịch">
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-x-8 gap-y-6">
                                <InfoField label="Mã Đơn Hàng (PO)" value={<span className="font-bold text-indigo-700">{po.orderCode}</span>} />
                                <InfoField label="Nhà Cung Cấp" value={<span className="font-bold text-slate-800">{po.supplierName}</span>} />
                                <InfoField label="Người Lập Đơn" value={po.createdByName} />
                                <InfoField label="Tổng Tiền Thanh Toán" value={<span className="font-black text-emerald-600 text-lg">{formatCurrency(po.totalAmount)}</span>} />
                                
                                <InfoField label="Ngày Lập Đơn" value={formatDate(po.orderDate)} />
                                <InfoField label="Ngày Giao Dự Kiến" value={formatDate(po.expectedDeliveryDate)} />
                            </div>
                            
                            {po.note && (
                                <div className="mt-6 pt-6 border-t border-slate-100">
                                    <InfoField label="Ghi Chú Chung" value={<span className="italic text-slate-600">{po.note}</span>} />
                                </div>
                            )}
                        </DetailSection>
                    </DetailCard>
                </div>
            )}

            {/* ================= TAB 2: CHI TIẾT MẶT HÀNG ================= */}
            {activeTab === 'items' && (
                <div className="animate-in fade-in duration-300">
                    <DetailCard>
                        <DetailSection title="Danh Sách Hàng Hóa Cần Nhập">
                            <div className="overflow-x-auto border border-slate-200 rounded-xl shadow-sm">
                                <table className="w-full text-left text-sm whitespace-nowrap">
                                    <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                                        <tr>
                                            <th className="px-4 py-4 text-center w-12">#</th>
                                            <th className="px-4 py-4">Mã SKU</th>
                                            <th className="px-4 py-4">Tên Sản Phẩm</th>
                                            <th className="px-4 py-4">ĐVT</th>
                                            <th className="px-4 py-4 text-right">Giá Nhập</th>
                                            <th className="px-4 py-4 text-center bg-indigo-50/50">SL Đặt</th>
                                            <th className="px-4 py-4 text-center bg-emerald-50/50">Đã Nhận</th>
                                            <th className="px-4 py-4 text-right">Thành Tiền</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {!po.details || po.details.length === 0 ? (
                                            <tr>
                                                <td colSpan={8} className="px-4 py-8 text-center text-slate-400 italic">
                                                    Không có dữ liệu mặt hàng
                                                </td>
                                            </tr>
                                        ) : (
                                            po.details.map((item, index) => {
                                                const isFullyReceived = item.receivedQuantity >= item.orderQuantity;
                                                // Tính tiến độ giao hàng
                                                const hasStartedReceiving = item.receivedQuantity > 0;
                                                
                                                return (
                                                    <tr key={item.id || index} className="hover:bg-slate-50/80 transition-colors group">
                                                        <td className="px-4 py-3 text-slate-400 text-center">{index + 1}</td>
                                                        <td className="px-4 py-3 font-bold text-slate-700">{item.variantCode}</td>
                                                        <td className="px-4 py-3 font-medium text-slate-900">{item.variantName}</td>
                                                        <td className="px-4 py-3 text-slate-500">{item.uoMName}</td>
                                                        <td className="px-4 py-3 text-right text-slate-600">{formatCurrency(item.unitPrice)}</td>
                                                        
                                                        {/* Cột SL Đặt */}
                                                        <td className="px-4 py-3 text-center bg-indigo-50/20 group-hover:bg-indigo-50/50">
                                                            <span className="font-bold text-indigo-700 text-[15px]">{item.orderQuantity}</span>
                                                        </td>
                                                        
                                                        {/* Cột SL Đã Nhận (Tiến độ giao hàng) */}
                                                        <td className="px-4 py-3 text-center bg-emerald-50/20 group-hover:bg-emerald-50/50">
                                                            {isFullyReceived ? (
                                                                <span className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-bold bg-emerald-100 text-emerald-700 border border-emerald-200">
                                                                    ĐỦ ({item.receivedQuantity})
                                                                </span>
                                                            ) : hasStartedReceiving ? (
                                                                <span className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-bold bg-amber-100 text-amber-700 border border-amber-200">
                                                                    THIẾU ({item.receivedQuantity}/{item.orderQuantity})
                                                                </span>
                                                            ) : (
                                                                <span className="text-slate-400 font-medium text-[13px]">0</span>
                                                            )}
                                                        </td>

                                                        <td className="px-4 py-3 text-right font-bold text-slate-800">{formatCurrency(item.totalPrice)}</td>
                                                    </tr>
                                                );
                                            })
                                        )}
                                    </tbody>
                                    {po.details && po.details.length > 0 && (
                                        <tfoot className="bg-slate-50/80 border-t border-slate-200">
                                            <tr>
                                                <td colSpan={7} className="px-4 py-4 text-right text-slate-600 text-xs font-bold uppercase tracking-wider">
                                                    Tổng Giá Trị Đơn Hàng:
                                                </td>
                                                <td className="px-4 py-4 text-right text-lg font-black text-emerald-600">
                                                    {formatCurrency(po.totalAmount)}
                                                </td>
                                            </tr>
                                        </tfoot>
                                    )}
                                </table>
                            </div>
                        </DetailSection>
                    </DetailCard>
                </div>
            )}

            {/* ================= MODAL HỦY ĐƠN ================= */}
            {cancelModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
                    <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
                        <div className="px-6 py-4 border-b border-slate-100 bg-rose-50/30">
                            <h3 className="text-lg font-black text-rose-700 flex items-center gap-2">
                                <AlertCircle size={20} /> Xác nhận Hủy Đơn
                            </h3>
                        </div>
                        <div className="p-6">
                            <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                                Bạn đang thực hiện hủy đơn mua hàng <strong className="text-slate-900 bg-slate-100 px-1 rounded">{po.orderCode}</strong>.
                                Lịch sử thao tác này sẽ được lưu lại hệ thống.
                            </p>
                            <div>
                                <label className="block text-sm font-bold text-slate-700 mb-2">
                                    Lý do hủy đơn <span className="text-red-500">*</span>
                                </label>
                                <textarea
                                    className="w-full px-3 py-2 border border-slate-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-rose-500 focus:border-rose-500 resize-none text-sm"
                                    rows={3}
                                    placeholder="Nhà cung cấp báo hết hàng, sai giá..."
                                    value={cancelReason}
                                    onChange={(e) => setCancelReason(e.target.value)}
                                    autoFocus
                                ></textarea>
                            </div>
                        </div>
                        <div className="px-6 py-4 bg-slate-50 flex justify-end gap-3 border-t border-slate-100">
                            <button
                                onClick={() => { setCancelModalOpen(false); setCancelReason(''); }}
                                disabled={actionLoading}
                                className="px-5 py-2 border border-slate-300 rounded-lg text-sm font-bold text-slate-600 bg-white hover:bg-slate-100 transition-colors"
                            >
                                Quay lại
                            </button>
                            <button
                                onClick={() => handleUpdateStatus(PurchaseOrderStatus.Cancelled, cancelReason.trim())}
                                disabled={actionLoading || !cancelReason.trim()}
                                className="px-5 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 disabled:bg-slate-300 shadow-sm"
                            >
                                {actionLoading ? 'Đang xử lý...' : 'Xác Nhận Hủy'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            <Toast {...toast} />
        </DetailPageContainer>
    );
};

export default PurchaseOrderDetail;