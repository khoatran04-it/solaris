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
  InfoField,
} from '../../components/commons/TabUI';
import { Toast } from '../../components/commons/Toast';

// API & Types
import { purchaseOrderApi } from '../../api/purchaseOrderApi';
import {
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseOrderStatusLabels,
  PurchaseOrderStatusColors,
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
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>(
    { show: false, type: 'success', message: '' }
  );
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
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleUpdateStatus = async (status: PurchaseOrderStatus, reason?: string) => {
    if (!po) return;

    try {
      setActionLoading(true);

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
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      value || 0
    );
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '---';
    try {
      const d = new Date(dateString);
      if (isNaN(d.getTime())) return dateString;
      return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return dateString.includes('T') ? dateString.split('T')[0] : dateString;
    }
  };

  const extractWarehouse = (note?: string) => {
    if (!note) return '---';
    const match = note.match(/\[Kho nhận:\s*([^\]]+)\]/i);
    return match ? match[1].trim() : '---';
  };

  const getCleanNote = (note?: string) => {
    if (!note) return '';
    return note.replace(/\[Kho nhận:\s*[^\]]+\]\s*/i, '').trim();
  };

  // --- RENDER ---
  if (loading)
    return <div className="p-12 text-center text-slate-500 font-medium">Đang tải dữ liệu...</div>;
  if (!po)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy Đơn mua hàng!</div>
    );

  const cleanNoteText = getCleanNote(po.note);

  return (
    <DetailPageContainer>
      <DetailHeader
        title="Chi Tiết Đơn Mua Hàng"
        subtitle={`Mã Đơn: ${po.orderCode}`}
        onBack={() => navigate('/purchase-orders')}
        icon={ShoppingCart}
      />

      {/* ================= THANH CÔNG CỤ (ACTION BUTTONS) ================= */}
      <div className="flex flex-wrap items-center justify-between gap-4 mb-2 p-4.5 bg-white border border-slate-100 rounded-3xl shadow-[0_2px_20px_-4px_rgba(0,0,0,0.05)]">
        <div className="flex items-center gap-3">
          <span className="text-sm font-bold text-slate-500">Trạng thái hiện tại:</span>
          <span
            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-extrabold border ${PurchaseOrderStatusColors[po.status]}`}
          >
            {PurchaseOrderStatusLabels[po.status]}
          </span>
        </div>

        <div className="flex items-center gap-3">
          {/* NÚT: DRAFT -> PROCESSING */}
          {po.status === PurchaseOrderStatus.Draft && (
            <button
              onClick={() => handleUpdateStatus(PurchaseOrderStatus.Processing)}
              disabled={actionLoading}
              className="flex items-center gap-2 px-5 py-2.5 bg-amber-400 text-slate-900 rounded-xl font-bold text-sm hover:bg-amber-300 transition-all shadow-xs disabled:opacity-50 cursor-pointer"
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
                className="flex items-center gap-2 px-5 py-2.5 bg-white text-rose-600 border border-rose-200 rounded-xl font-bold text-sm hover:bg-rose-50 transition-colors disabled:opacity-50 cursor-pointer"
              >
                Hủy Đơn
              </button>
              <button
                onClick={() => handleUpdateStatus(PurchaseOrderStatus.Approved)}
                disabled={actionLoading}
                className="flex items-center gap-2 px-5 py-2.5 bg-emerald-600 text-white rounded-xl font-bold text-sm hover:bg-emerald-700 transition-all shadow-xs disabled:opacity-50 cursor-pointer"
              >
                <CheckCircle size={16} /> Duyệt Đơn Hàng
              </button>
            </>
          )}

          {/* NÚT: APPROVED / PARTIALLY RECEIVED -> TẠO PHIẾU NHẬP */}
          {(po.status === PurchaseOrderStatus.Approved ||
            po.status === PurchaseOrderStatus.PartiallyReceived) && (
            <button
              onClick={() => navigate(`/inventory-receipts/create?poId=${po.id}`)}
              className="flex items-center gap-2 px-5 py-2.5 bg-indigo-600 text-white rounded-xl font-bold text-sm hover:bg-indigo-700 transition-all shadow-xs cursor-pointer"
            >
              <Package size={16} /> Nhận Hàng (Tạo Phiếu Nhập)
            </button>
          )}
        </div>
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
            <div className="p-8 flex flex-col gap-6">
              {/* Cảnh báo Hủy đơn */}
              {po.status === PurchaseOrderStatus.Cancelled && po.cancellationReason && (
                <div className="bg-rose-50 border border-rose-200 text-rose-700 p-4 rounded-2xl flex gap-3 items-start shadow-sm">
                  <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                  <div>
                    <h4 className="font-bold text-sm uppercase tracking-wider">Lý do hủy đơn:</h4>
                    <p className="text-sm mt-1 font-medium">{po.cancellationReason}</p>
                  </div>
                </div>
              )}

              <DetailSection title="Chứng Từ Giao Dịch">
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-x-8 gap-y-6">
                  <InfoField
                    label="Mã Đơn Hàng (PO)"
                    value={
                      <span className="font-mono font-bold text-amber-800 bg-amber-50 px-2.5 py-1 rounded-lg border border-amber-200/80">
                        {po.orderCode}
                      </span>
                    }
                  />
                  <InfoField
                    label="Nhà Cung Cấp"
                    value={<span className="font-bold text-slate-800">{po.supplierName}</span>}
                  />
                  <InfoField
                    label="Kho Nhận Hàng Dự Kiến"
                    value={
                      <span className="font-semibold text-slate-800 bg-slate-100 px-2.5 py-1 rounded-lg border border-slate-200">
                        {extractWarehouse(po.note)}
                      </span>
                    }
                  />
                  <InfoField
                    label="Trạng Thái Đơn"
                    value={
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-extrabold border ${PurchaseOrderStatusColors[po.status]}`}
                      >
                        {PurchaseOrderStatusLabels[po.status]}
                      </span>
                    }
                  />

                  <InfoField label="Người Lập Đơn" value={po.createdByName} />
                  <InfoField label="Ngày Lập Đơn" value={formatDate(po.orderDate)} />
                  <InfoField label="Ngày Giao Dự Kiến" value={formatDate(po.expectedDeliveryDate)} />
                  <InfoField
                    label="Tổng Tiền Thanh Toán"
                    value={
                      <span className="font-black text-amber-700 text-lg">
                        {formatCurrency(po.totalAmount)}
                      </span>
                    }
                  />
                </div>

                {cleanNoteText ? (
                  <div className="mt-4 pt-6 border-t border-slate-100">
                    <InfoField
                      label="Ghi Chú & Yêu Cầu Vận Chuyển"
                      value={<span className="text-slate-700 font-medium">{cleanNoteText}</span>}
                    />
                  </div>
                ) : null}
              </DetailSection>
            </div>
          </DetailCard>
        </div>
      )}

      {/* ================= TAB 2: CHI TIẾT MẶT HÀNG ================= */}
      {activeTab === 'items' && (
        <div className="animate-in fade-in duration-300">
          <DetailCard>
            <div className="p-8 flex flex-col gap-6">
              <DetailSection title="Danh Sách Hàng Hóa Cần Nhập">
                <div className="overflow-x-auto border border-slate-200 rounded-2xl shadow-2xs">
                  <table className="w-full text-left text-sm border-collapse">
                    <thead className="bg-slate-50/80 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                      <tr>
                        <th className="py-3.5 px-4 text-center w-12">#</th>
                        <th className="py-3.5 px-4 w-[18%]">Mã SKU</th>
                        <th className="py-3.5 px-4 w-[32%]">Tên Sản Phẩm</th>
                        <th className="py-3.5 px-4 w-[12%]">Đơn Vị Tính</th>
                        <th className="py-3.5 px-4 w-[14%] text-right">Giá Nhập</th>
                        <th className="py-3.5 px-4 w-[10%] text-center">SL Đặt</th>
                        <th className="py-3.5 px-4 w-[10%] text-center">Đã Nhận</th>
                        <th className="py-3.5 px-4 w-[14%] text-right">Thành Tiền</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {!po.details || po.details.length === 0 ? (
                        <tr>
                          <td colSpan={8} className="py-8 text-center text-slate-400 italic">
                            Không có dữ liệu mặt hàng
                          </td>
                        </tr>
                      ) : (
                        po.details.map((item, index) => {
                          const isFullyReceived = item.receivedQuantity >= item.orderQuantity;
                          const hasStartedReceiving = item.receivedQuantity > 0;

                          return (
                            <tr
                              key={item.id || index}
                              className="hover:bg-slate-50/60 transition-colors group"
                            >
                              <td className="py-3.5 px-4 text-slate-400 text-center font-medium">
                                {index + 1}
                              </td>
                              <td className="py-3.5 px-4 font-bold text-slate-700">
                                <span className="font-mono text-xs font-bold text-blue-700 bg-blue-50 px-2 py-0.5 rounded-md border border-blue-200/80">
                                  {item.variantCode}
                                </span>
                              </td>
                              <td className="py-3.5 px-4 font-semibold text-slate-900">
                                {item.variantName}
                              </td>
                              <td className="py-3.5 px-4 text-slate-600 font-medium">
                                {item.uoMName}
                              </td>
                              <td className="py-3.5 px-4 text-right text-slate-700 font-semibold">
                                {formatCurrency(item.unitPrice)}
                              </td>

                              {/* Cột SL Đặt */}
                              <td className="py-3.5 px-4 text-center">
                                <span className="font-black text-indigo-700 text-sm">
                                  {item.orderQuantity}
                                </span>
                              </td>

                              {/* Cột SL Đã Nhận (Tiến độ giao hàng) */}
                              <td className="py-3.5 px-4 text-center">
                                {isFullyReceived ? (
                                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-emerald-100 text-emerald-700 border border-emerald-200">
                                    ĐỦ ({item.receivedQuantity})
                                  </span>
                                ) : hasStartedReceiving ? (
                                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 text-amber-700 border border-amber-200">
                                    THIẾU ({item.receivedQuantity}/{item.orderQuantity})
                                  </span>
                                ) : (
                                  <span className="text-slate-400 font-medium text-xs">0</span>
                                )}
                              </td>

                              <td className="py-3.5 px-4 text-right font-extrabold text-slate-800">
                                {formatCurrency(item.totalPrice)}
                              </td>
                            </tr>
                          );
                        })
                      )}
                    </tbody>
                    {po.details && po.details.length > 0 && (
                      <tfoot className="bg-slate-50/80 border-t border-slate-200">
                        <tr>
                          <td
                            colSpan={7}
                            className="px-6 py-4 text-right text-slate-600 text-xs font-extrabold uppercase tracking-wider"
                          >
                            Tổng Giá Trị Đơn Hàng:
                          </td>
                          <td className="px-4 py-4 text-right text-lg font-black text-amber-700">
                            {formatCurrency(po.totalAmount)}
                          </td>
                        </tr>
                      </tfoot>
                    )}
                  </table>
                </div>
              </DetailSection>
            </div>
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
                Bạn đang thực hiện hủy đơn mua hàng{' '}
                <strong className="text-slate-900 bg-slate-100 px-1 rounded">{po.orderCode}</strong>
                . Lịch sử thao tác này sẽ được lưu lại hệ thống.
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
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
                disabled={actionLoading}
                className="px-5 py-2 border border-slate-300 rounded-lg text-sm font-bold text-slate-600 bg-white hover:bg-slate-100 transition-colors cursor-pointer"
              >
                Quay lại
              </button>
              <button
                onClick={() =>
                  handleUpdateStatus(PurchaseOrderStatus.Cancelled, cancelReason.trim())
                }
                disabled={actionLoading || !cancelReason.trim()}
                className="px-5 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 disabled:bg-slate-300 shadow-sm cursor-pointer"
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
