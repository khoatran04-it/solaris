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
  Loader2,
  CheckCircle,
  AlertTriangle,
  Boxes,
  ArrowRight,
  Bike,
} from 'lucide-react';

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
import { DateCell, DateTimeCell } from '../../components/commons/ListUI';

import { orderApi } from '../../api/orderApi';
import { vehicleApi } from '../../api/vehicleApi';
import { DeliveryVehicle } from '../../types/vehicle';
import {
  Order,
  OrderStatus,
  OrderStatusLabels,
  OrderStatusColors,
  PaymentStatusLabels,
  PaymentStatusColors,
  PaymentMethodLabels,
  RoutingPreviewResult,
} from '../../types/order';
import { DocumentPrintModal } from '../../components/commons/DocumentPrintModal';

const OrderDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [order, setOrder] = useState<Order | null>(null);
  const [routingAnalysis, setRoutingAnalysis] = useState<RoutingPreviewResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingRouting, setLoadingRouting] = useState(false);
  const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

  // Cancel modal
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [printModalOpen, setPrintModalOpen] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  // Toast
  const [toast, setToast] = useState<{
    show: boolean;
    type: 'success' | 'error' | 'warning';
    message: string;
  }>({
    show: false,
    type: 'success',
    message: '',
  });

  const showToast = (type: 'success' | 'error' | 'warning', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const fetchOrder = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await orderApi.getById(Number(id));
      setOrder(data);

      const isPendingOrConfirmed =
        data.status === OrderStatus.Draft ||
        data.status === OrderStatus.Pending ||
        data.status === OrderStatus.Confirmed ||
        data.status === OrderStatus.Processing;

      const hasUnissuedItems =
        data.details && data.details.some((d: any) => (d.issuedQuantity || 0) < d.quantity);

      // Phân tích tình trạng tồn kho đa kho cho đơn hàng CHỈ KHI đơn đang chờ xuất và chưa xuất đủ
      if (
        data &&
        data.details &&
        data.details.length > 0 &&
        isPendingOrConfirmed &&
        hasUnissuedItems
      ) {
        try {
          setLoadingRouting(true);
          const routing = await orderApi.previewRouting({
            customerId: data.customerId,
            customerAddressId: data.customerAddressId,
            deliveryAddress: data.deliveryAddress || '',
            warehouseId: data.warehouseId,
            paymentMethod: data.paymentMethod,
            details: data.details.map((d) => ({
              variantId: d.variantId,
              uoMId: d.uoMId,
              quantity: d.quantity,
            })),
          });
          setRoutingAnalysis(routing);
        } catch (rErr) {
          console.error('Không thể phân tích định tuyến tồn kho:', rErr);
        } finally {
          setLoadingRouting(false);
        }
      } else {
        setRoutingAnalysis(null);
      }
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
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      amount || 0
    );
  };

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-slate-50/30 p-12">
        <Loader2 className="w-10 h-10 animate-spin text-indigo-500" />
      </div>
    );
  }

  if (!order)
    return <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy đơn hàng!</div>;

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Chi Tiết Đơn Bán Hàng"
        subtitle={
          <>
            Mã hệ thống: <span className="font-bold text-slate-800">{order.orderCode}</span>
          </>
        }
        onBack={() => navigate('/orders')}
        icon={ShoppingBag}
      />

      {/* ================= THÀNH CÔNG CỤ (ACTION TOOLBAR) ================= */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">
              Trạng thái:
            </span>
            <span
              className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${OrderStatusColors[order.status]}`}
            >
              {OrderStatusLabels[order.status]}
            </span>
            <span
              className={`inline-flex items-center px-2.5 py-1 rounded text-[11px] font-bold border shadow-sm ${PaymentStatusColors[order.paymentStatus]}`}
            >
              {PaymentStatusLabels[order.paymentStatus]}
            </span>
          </div>

          <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>

          {/* Nút luồng đi tiếp: Nằm bên trái (100% Text - Không Icon) */}
          <div className="flex items-center gap-2">
            {/* 1. Nút Duyệt Đơn Hàng (Pending -> Confirmed) */}
            {order.status === OrderStatus.Pending && (
              <button
                onClick={async () => {
                  try {
                    setActionLoading(true);
                    await orderApi.updateStatus(order.id, {
                      status: OrderStatus.Confirmed,
                    });
                    showToast('success', 'ĐÃ DUYỆT ĐƠN HÀNG THÀNH CÔNG!');
                    fetchOrder();
                  } catch (err: any) {
                    showToast('error', err.response?.data?.message || 'Lỗi duyệt đơn hàng');
                  } finally {
                    setActionLoading(false);
                  }
                }}
                disabled={actionLoading}
                className="px-4 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-xs disabled:opacity-50"
              >
                Duyệt Đơn Hàng
              </button>
            )}

            {/* 2. Nút Tạo Phiếu Xuất Kho: Khi đơn đã Confirmed hoặc Processing và còn hàng cần xuất */}
            {(order.status === OrderStatus.Confirmed || order.status === OrderStatus.Processing) &&
              order.details?.some((d) => (d.issuedQuantity || 0) < d.quantity) && (
                <button
                  onClick={() => navigate(`/inventory-issues/create?orderId=${order.id}`)}
                  className="px-4 py-2 bg-indigo-600 text-white rounded-lg font-bold text-sm hover:bg-indigo-700 transition-all shadow-xs"
                >
                  Tạo Phiếu Xuất Kho
                </button>
              )}

            {/* 3. Khi đơn đang giao hàng: Hiển thị badge khóa hủy và nút xác nhận đã giao */}
            {order.status === OrderStatus.Shipping && (
              <>
                <span className="px-3 py-1.5 bg-amber-50 text-amber-800 border border-amber-200 rounded-lg text-xs font-bold">
                  Đang giao hàng (Không thể hủy đơn)
                </span>
                <button
                  onClick={async () => {
                    try {
                      setActionLoading(true);
                      await orderApi.updateStatus(order.id, {
                        status: OrderStatus.Completed,
                        paymentStatus: 3, // Paid
                      });
                      showToast(
                        'success',
                        'ĐÃ CẬP NHẬT ĐƠN HÀNG: GIAO THÀNH CÔNG & ĐÃ THANH TOÁN!'
                      );
                      fetchOrder();
                    } catch (err: any) {
                      showToast(
                        'error',
                        err.response?.data?.message || 'Lỗi cập nhật trạng thái đơn'
                      );
                    } finally {
                      setActionLoading(false);
                    }
                  }}
                  disabled={actionLoading}
                  className="px-4 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-xs disabled:opacity-50"
                >
                  Xác Nhận Đã Giao Hàng
                </button>
                <button
                  onClick={async () => {
                    const reason = window.prompt(
                      'Nhập lý do khách từ chối nhận hàng (ví dụ: Hàng dập nát, Giao trễ, Đổi ý...):',
                      'Khách từ chối nhận hàng'
                    );
                    if (!reason || !reason.trim()) return;

                    try {
                      setActionLoading(true);
                      await orderApi.updateStatus(order.id, {
                        status: OrderStatus.Cancelled,
                        cancellationReason: `Khách từ chối nhận: ${reason.trim()}`,
                      });
                      showToast('warning', 'ĐÃ GHI NHẬN KHÁCH TỪ CHỐI NHẬN - ĐANG HOÀN VỀ KHO!');
                      fetchOrder();
                    } catch (err: any) {
                      showToast(
                        'error',
                        err.response?.data?.message || 'Lỗi cập nhật trạng thái đơn'
                      );
                    } finally {
                      setActionLoading(false);
                    }
                  }}
                  disabled={actionLoading}
                  className="px-4 py-2 bg-rose-50 text-rose-700 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-100 transition-all shadow-xs disabled:opacity-50"
                >
                  Khách Từ Chối Nhận (Hoàn Về)
                </button>
              </>
            )}

            {order.status === OrderStatus.Completed && (
              <button
                onClick={() => navigate(`/customer-returns/create?orderId=${order.id}`)}
                className="px-4 py-2 bg-teal-600 text-white rounded-lg font-bold text-sm hover:bg-teal-700 transition-all shadow-xs"
              >
                Tiếp Nhận Khách Trả
              </button>
            )}

            {/* Nút In Hóa Đơn & Phiếu Giao Hàng */}
            <button
              onClick={() => setPrintModalOpen(true)}
              className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-800 rounded-lg font-bold text-sm transition-all shadow-xs"
              title="In hóa đơn bán hàng kiêm phiếu giao hàng"
            >
              In Phiếu Giao Hàng
            </button>
          </div>
        </div>

        {/* Nút Hủy Đơn: Nằm sát góc phải (CHỈ KHI CHƯA GIAO HÀNG) */}
        {(order.status === OrderStatus.Draft ||
          order.status === OrderStatus.Pending ||
          order.status === OrderStatus.Confirmed ||
          order.status === OrderStatus.Processing) && (
          <button
            onClick={() => setCancelModalOpen(true)}
            className="px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-50 transition-colors shadow-xs"
          >
            Hủy Đơn Này
          </button>
        )}
      </div>

      {/* ================= SMART FULFILLMENT ALERT BANNER (KHI ĐANG CHỜ XUẤT VÀ THIẾU HÀNG) ================= */}
      {(order.status === OrderStatus.Draft ||
        order.status === OrderStatus.Pending ||
        order.status === OrderStatus.Confirmed ||
        order.status === OrderStatus.Processing) &&
        order.details?.some((d) => (d.issuedQuantity || 0) < d.quantity) &&
        routingAnalysis &&
        !routingAnalysis.isFullyStocked &&
        routingAnalysis.missingItems &&
        routingAnalysis.missingItems.length > 0 && (
          <div className="bg-gradient-to-r from-amber-500/15 via-orange-500/10 to-amber-500/5 border-2 border-amber-300/90 rounded-3xl p-6 shadow-sm mb-6 flex flex-col lg:flex-row lg:items-center justify-between gap-6 animate-in fade-in duration-300">
            <div className="flex items-start gap-4">
              <div className="w-13 h-13 rounded-2xl bg-gradient-to-br from-amber-500 to-orange-500 text-white flex items-center justify-center shadow-lg shadow-amber-500/30 shrink-0">
                <AlertTriangle size={28} />
              </div>
              <div className="space-y-2">
                <div className="flex items-center gap-2.5 flex-wrap">
                  <span className="px-3 py-0.5 rounded-full bg-amber-500 text-white text-[11px] font-black uppercase tracking-wider shadow-xs">
                    ⚠️ Cảnh Báo Thiếu Hàng Xuất
                  </span>
                  <span className="text-sm font-bold text-amber-950">
                    Kho xuất hiện tại (
                    <strong className="text-indigo-800 font-extrabold">
                      {order.warehouseName || 'Chưa gán'}
                    </strong>
                    ) đang thiếu {routingAnalysis.missingItems.length} mặt hàng để xuất đơn!
                  </span>
                </div>

                {/* Danh sách mặt hàng thiếu */}
                <div className="flex flex-wrap gap-2 pt-1">
                  {routingAnalysis.missingItems.map((item, idx) => (
                    <span
                      key={idx}
                      className="inline-flex items-center gap-2 px-3.5 py-1.5 bg-white/95 border border-amber-300 rounded-xl text-xs text-amber-950 font-bold shadow-2xs"
                    >
                      <span className="w-2 h-2 rounded-full bg-rose-500 animate-pulse"></span>
                      <span>{item.variantName}:</span>
                      <span className="text-rose-600 font-black">Thiếu {item.missingQuantity}</span>
                      <span className="text-slate-400 font-medium">
                        (Cần {item.requestedQuantity}, Có {item.availableQuantity})
                      </span>
                    </span>
                  ))}
                </div>

                {/* Gợi ý kho nguồn */}
                {routingAnalysis.suggestedSourceWarehouseName && (
                  <p className="text-xs text-slate-700 pt-0.5 font-medium flex items-center gap-2 flex-wrap">
                    <span className="text-indigo-700 font-bold">Gợi ý điều phối:</span>
                    <span>Kho nguồn có sẵn hàng khả dụng là</span>
                    <strong className="text-indigo-950 font-black bg-indigo-50 px-2.5 py-1 rounded-lg border border-indigo-200">
                      {routingAnalysis.suggestedSourceWarehouseName}
                    </strong>
                  </p>
                )}
              </div>
            </div>

            {/* Nút Lập Lệnh Chuyển Kho Hàng Thiếu */}
            <button
              onClick={() =>
                navigate(
                  `/inventory-transfers/create?orderId=${order.id}&fromWarehouseId=${
                    routingAnalysis.suggestedSourceWarehouseId || ''
                  }&toWarehouseId=${order.warehouseId || ''}&missingOnly=true`
                )
              }
              className="px-5 py-3.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-slate-950 font-black text-xs uppercase tracking-wider rounded-2xl transition-all shadow-md shadow-amber-500/25 hover:scale-[1.02] active:scale-[0.98] flex items-center justify-center gap-2.5 shrink-0 border border-amber-400"
            >
              <Truck size={18} />
              <span>
                Lập Lệnh Chuyển Kho Bổ Sung ({routingAnalysis.missingItems.length} SP Thiếu)
              </span>
              <ArrowRight size={16} />
            </button>
          </div>
        )}

      {/* ================= SẴN SÀNG XUẤT HÀNG BANNER (KHI ĐANG CHỜ XUẤT VÀ ĐỦ HÀNG) ================= */}
      {(order.status === OrderStatus.Draft ||
        order.status === OrderStatus.Pending ||
        order.status === OrderStatus.Confirmed ||
        order.status === OrderStatus.Processing) &&
        order.details?.some((d) => (d.issuedQuantity || 0) < d.quantity) &&
        routingAnalysis &&
        routingAnalysis.isFullyStocked && (
          <div className="bg-emerald-50/90 border border-emerald-200 rounded-2xl px-5 py-3.5 mb-6 flex items-center justify-between text-xs text-emerald-900 font-bold shadow-2xs">
            <div className="flex items-center gap-2.5">
              <CheckCircle size={18} className="text-emerald-600 shrink-0" />
              <span>
                Kho xuất <strong>{order.warehouseName}</strong> có đủ 100% tồn kho khả dụng để thực
                hiện xuất kho cho đơn hàng này.
              </span>
            </div>
            <span className="px-2.5 py-1 bg-emerald-600 text-white rounded-lg text-[10px] font-black uppercase tracking-wider shrink-0">
              Sẵn sàng xuất hàng
            </span>
          </div>
        )}

      {/* ================= ĐANG GIAO HÀNG BANNER ================= */}
      {order.status === OrderStatus.Shipping && (
        <div className="bg-gradient-to-r from-blue-500/10 via-indigo-500/10 to-blue-500/5 border-2 border-blue-200 rounded-3xl p-5 shadow-sm mb-6 flex items-center justify-between gap-4 animate-in fade-in">
          <div className="flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-blue-600 text-white flex items-center justify-center shadow-md shadow-blue-500/20 shrink-0">
              <Truck size={24} />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="px-2.5 py-0.5 rounded-full bg-blue-600 text-white text-[11px] font-black uppercase tracking-wider">
                  Đang Vận Chuyển
                </span>
                <span className="text-sm font-bold text-blue-950">
                  Đơn hàng đã được xuất kho và đang giao đến khách hàng
                </span>
              </div>
              <p className="text-xs text-slate-600 pt-1 font-medium">
                Đơn vị vận chuyển:{' '}
                <strong className="text-blue-900 font-black">
                  {order.shippingProvider === 'Internal' || order.deliveryTripId
                    ? 'Đội Xe Thùng Lạnh Solaris'
                    : order.shippingProvider === 'GHN'
                      ? 'Giao Hàng Nhanh (GHN)'
                      : order.shippingProvider || 'Đội Xe Thùng Lạnh Solaris'}
                </strong>
                {order.trackingCode && (
                  <>
                    {' '}
                    | Mã vận đơn:{' '}
                    <strong className="font-mono text-indigo-700 bg-white px-2 py-0.5 rounded border border-blue-200 font-bold">
                      {order.trackingCode}
                    </strong>
                  </>
                )}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* ================= HOÀN TẤT BANNER ================= */}
      {order.status === OrderStatus.Completed && (
        <div className="bg-emerald-50/90 border border-emerald-300 rounded-3xl p-5 shadow-sm mb-6 flex items-center gap-4 animate-in fade-in">
          <div className="w-12 h-12 rounded-2xl bg-emerald-600 text-white flex items-center justify-center shadow-md shadow-emerald-500/20 shrink-0">
            <CheckCircle size={24} />
          </div>
          <div>
            <span className="px-2.5 py-0.5 rounded-full bg-emerald-600 text-white text-[11px] font-black uppercase tracking-wider">
              Hoàn Tất
            </span>
            <p className="text-sm font-bold text-emerald-950 pt-1">
              Đơn hàng đã được giao thành công và hoàn tất toàn bộ chu trình!
            </p>
          </div>
        </div>
      )}

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
                  <h4 className="font-bold text-sm text-rose-800 uppercase tracking-wider">
                    Lý do hủy đơn hàng:
                  </h4>
                  <p className="text-sm text-rose-700 font-medium">{order.cancellationReason}</p>
                </div>
              </div>
            )}

            <DetailSection title="Thông Tin Khách Hàng & Giao Hàng" dotColor="bg-indigo-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Khách hàng"
                  value={<span className="font-bold text-slate-800">{order.customerName}</span>}
                />
                <InfoField label="SĐT khách hàng" value={order.customerPhone || '---'} />
                <InfoField
                  label="Người nhận hàng"
                  value={
                    <span className="font-bold text-indigo-700">
                      {order.receiverName || order.customerName}
                    </span>
                  }
                />
                <InfoField
                  label="SĐT nhận hàng"
                  value={order.receiverPhone || order.customerPhone || '---'}
                />

                <div className="md:col-span-2 lg:col-span-4">
                  <InfoField label="Địa chỉ giao hàng" value={order.deliveryAddress || '---'} />
                </div>
              </div>
            </DetailSection>

            <DetailSection title="Điều Phối & Thanh Toán" dotColor="bg-emerald-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Kho xử lý đơn"
                  value={
                    <span className="font-bold text-indigo-700">
                      {order.warehouseName || 'Chưa gán kho'}
                    </span>
                  }
                />
                <InfoField
                  label="Phương thức thanh toán"
                  value={
                    <span className="font-medium text-slate-800">
                      {PaymentMethodLabels[order.paymentMethod]}
                    </span>
                  }
                />

                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Ngày đặt hàng
                  </span>
                  <DateCell isoString={order.orderDate} />
                </div>

                <InfoField
                  label="Tổng tiền thanh toán"
                  value={
                    <span className="font-black text-emerald-600 text-[17px]">
                      {formatCurrency(order.totalAmount)}
                    </span>
                  }
                />

                {order.trackingCode && (
                  <InfoField
                    label={
                      order.shippingProvider === 'Internal' ||
                      order.deliveryTripId ||
                      order.trackingCode.startsWith('SLR-EXP')
                        ? 'Mã vận đơn nội bộ'
                        : order.shippingProvider === 'GHN'
                          ? 'Mã vận đơn GHN'
                          : 'Mã vận đơn'
                    }
                    value={
                      <span className="inline-flex items-center px-2.5 py-1 bg-blue-50 text-blue-700 font-mono font-bold rounded-md border border-blue-200">
                        {order.trackingCode}
                      </span>
                    }
                  />
                )}

                {order.paymentTransactionNo && (
                  <InfoField
                    label="Mã GD VNPay"
                    value={
                      <span className="font-mono text-xs font-semibold text-slate-700">
                        {order.paymentTransactionNo}
                      </span>
                    }
                  />
                )}

                {order.deliveryTripId && (
                  <div className="col-span-full p-4 bg-emerald-50/80 rounded-xl border border-emerald-200 flex flex-col md:flex-row md:items-center justify-between gap-3">
                    <div>
                      <span className="font-extrabold text-emerald-800 text-xs flex items-center gap-1.5 mb-1">
                        Đội Xe Thùng Lạnh Solaris (Chuyến xe #{order.deliveryTripId})
                      </span>
                      <div className="text-xs text-slate-700 flex flex-wrap gap-4">
                        <span>
                          Tài xế: <strong>{order.driverName || 'Nhân viên giao nhận'}</strong>
                        </span>
                        <span>
                          SĐT: <strong>{order.driverPhone || '---'}</strong>
                        </span>
                        <span>
                          Biển số xe: <strong className="font-mono">{order.licensePlate}</strong>
                        </span>
                      </div>
                    </div>
                    {order.dispatchedAt && (
                      <span className="text-[11px] text-slate-500 font-medium">
                        Xuất phát:{' '}
                        {new Date(order.dispatchedAt).toLocaleTimeString('vi-VN', {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </span>
                    )}
                  </div>
                )}

                {order.requiresColdChain && (
                  <div className="col-span-full p-3 bg-blue-50/70 border border-blue-200 rounded-xl flex items-center gap-2 text-xs text-blue-800 font-bold">
                    <span>❄️</span>
                    <span>
                      Đơn hàng có sản phẩm chuỗi lạnh (thịt, cá, hải sản, rau củ tươi sống) — Được
                      vận chuyển bằng xe máy trang bị thùng giữ nhiệt lạnh Solaris Cold-Express.
                    </span>
                  </div>
                )}
              </div>

              {order.note && (
                <div className="mt-6 pt-6 border-t border-slate-100">
                  <InfoField
                    label="Ghi chú của khách"
                    value={<span className="italic text-slate-600 font-medium">{order.note}</span>}
                  />
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
                    <th
                      className="px-4 py-4 text-center bg-emerald-50/50 min-w-25"
                      title="Số lượng đã được xuất kho thành công"
                    >
                      Đã Xuất Kho
                    </th>
                    <th className="px-4 py-4 text-center min-w-36">Tình Trạng Kho Xuất</th>
                    <th className="px-4 py-4 text-right min-w-30">Chiết Khấu</th>
                    <th className="px-4 py-4 text-right min-w-37.5">Thành Tiền</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {order.details?.map((item, idx) => {
                    const missing = routingAnalysis?.missingItems?.find(
                      (m) => m.variantId === item.variantId
                    );
                    return (
                      <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                        <td className="px-4 py-3 text-center text-slate-400 font-medium">
                          {idx + 1}
                        </td>
                        <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                        <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                        <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>
                        <td className="px-4 py-3 text-right font-medium text-slate-600">
                          {formatCurrency(item.unitPrice)}
                        </td>

                        {/* Cột Số lượng Đặt */}
                        <td className="px-4 py-3 text-center border-l border-indigo-100 bg-indigo-50/20">
                          <span className="font-bold text-[15px] text-indigo-700">
                            {item.quantity}
                          </span>
                        </td>

                        {/* Cột Số lượng Đã Xuất */}
                        <td className="px-4 py-3 text-center border-l border-emerald-100 bg-emerald-50/20">
                          <span
                            className={`font-black text-[15px] ${item.issuedQuantity < item.quantity ? 'text-amber-500' : 'text-emerald-600'}`}
                          >
                            {item.issuedQuantity}
                          </span>
                        </td>

                        {/* Cột Tình trạng kho xuất */}
                        <td className="px-4 py-3 text-center border-l border-slate-100">
                          {(() => {
                            if (
                              order.status === OrderStatus.Shipping ||
                              order.status === OrderStatus.Completed
                            ) {
                              if (item.issuedQuantity >= item.quantity) {
                                return (
                                  <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                                    <CheckCircle size={13} /> Đã xuất đủ ({item.issuedQuantity}/
                                    {item.quantity})
                                  </span>
                                );
                              }
                              return (
                                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-amber-50 text-amber-700 border border-amber-200">
                                  <AlertCircle size={13} /> Đã xuất {item.issuedQuantity}/
                                  {item.quantity}
                                </span>
                              );
                            }

                            if (order.status === OrderStatus.Cancelled) {
                              return (
                                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-slate-100 text-slate-500 border border-slate-200">
                                  Đã hủy đơn
                                </span>
                              );
                            }

                            if (missing && missing.missingQuantity > 0) {
                              return (
                                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200">
                                  <AlertCircle size={13} /> Thiếu {missing.missingQuantity} (Có{' '}
                                  {missing.availableQuantity})
                                </span>
                              );
                            }

                            return (
                              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                                <CheckCircle size={13} /> Đủ hàng xuất
                              </span>
                            );
                          })()}
                        </td>

                        <td className="px-4 py-3 text-right text-slate-500 border-l border-slate-100">
                          {formatCurrency(item.discountAmount)}
                        </td>
                        <td className="px-4 py-3 text-right font-black text-slate-800">
                          {formatCurrency(item.totalPrice)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
                <tfoot className="bg-slate-50/80 border-t border-slate-200 text-sm">
                  <tr>
                    <td
                      colSpan={9}
                      className="px-4 py-3 text-right font-bold text-slate-500 uppercase tracking-wider text-xs"
                    >
                      Tiền hàng:
                    </td>
                    <td className="px-4 py-3 text-right font-bold text-slate-700">
                      {formatCurrency(order.subTotal)}
                    </td>
                  </tr>
                  <tr>
                    <td
                      colSpan={9}
                      className="px-4 py-2 text-right font-bold text-slate-500 uppercase tracking-wider text-xs"
                    >
                      Phí vận chuyển:
                    </td>
                    <td className="px-4 py-2 text-right font-bold text-slate-700">
                      {formatCurrency(order.shippingFee)}
                    </td>
                  </tr>
                  <tr className="border-t border-slate-200 bg-emerald-50/30">
                    <td
                      colSpan={9}
                      className="px-4 py-4 text-right text-emerald-800 uppercase tracking-wider text-xs font-black"
                    >
                      Tổng Thanh Toán:
                    </td>
                    <td className="px-4 py-4 text-right text-[17px] font-black text-emerald-600">
                      {formatCurrency(order.totalAmount)}
                    </td>
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
                Bạn đang thực hiện hủy đơn{' '}
                <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">
                  {order.orderCode}
                </strong>
                . Tồn kho đã giữ chỗ (Reserved) sẽ tự động được hệ thống hoàn trả lại thành tồn kho
                khả dụng (Available).
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
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
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

      {order && (
        <DocumentPrintModal
          isOpen={printModalOpen}
          onClose={() => setPrintModalOpen(false)}
          documentTitle="HÓA ĐƠN BÁN HÀNG KIÊM PHIẾU GIAO HÀNG"
          documentSubtitle="Hệ thống chuỗi thực phẩm sạch & bảo quản chuỗi lạnh Solaris"
          documentCode={order.orderCode}
          documentDate={order.orderDate}
          warehouseName={order.warehouseName}
          creatorName={order.createdByName}
          partyTitle="Khách hàng / Người nhận"
          partyName={order.receiverName || order.customerName || 'Khách lẻ'}
          partyPhone={order.receiverPhone || order.customerPhone}
          partyAddress={order.deliveryAddress}
          referenceCode={order.orderCode}
          paymentMethodName={PaymentMethodLabels[order.paymentMethod] || 'Tiền mặt / COD'}
          paymentStatusName={PaymentStatusLabels[order.paymentStatus]}
          codAmount={order.paymentStatus !== 3 && order.paymentMethod === 1 ? order.totalAmount : 0}
          shippingFee={order.shippingFee}
          discountAmount={order.discountAmount}
          subTotal={order.subTotal}
          totalAmount={order.totalAmount}
          notes={order.note || 'Giao hàng thực phẩm tươi sạch - Giữ nhiệt độ mát'}
          items={(order.details || []).map((d: any) => ({
            skuCode: d.variantCode,
            productName: d.variantName,
            batchCode: d.batchCode,
            uoMName: d.uoMName || 'Kg',
            quantity: d.quantity,
            unitPrice: d.unitPrice,
            totalPrice: d.totalPrice,
          }))}
        />
      )}
    </DetailPageContainer>
  );
};

export default OrderDetail;
