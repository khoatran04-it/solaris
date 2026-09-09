import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeftRight,
  FileText,
  Package,
  Truck,
  CheckCircle,
  XCircle,
  AlertCircle,
  Loader2,
  ClipboardCheck,
  X,
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
import { DateTimeCell } from '../../components/commons/ListUI';

import { inventoryTransferApi } from '../../api/inventoryTransferApi';
import {
  InventoryTransfer,
  InventoryTransferStatus,
  InventoryTransferStatusLabels,
  InventoryTransferStatusColors,
} from '../../types/inventoryTransfer';
import { DocumentPrintModal } from '../../components/commons/DocumentPrintModal';

const InventoryTransferDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [transfer, setTransfer] = useState<InventoryTransfer | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

  // Actions
  const [actionLoading, setActionLoading] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [printModalOpen, setPrintModalOpen] = useState(false);

  // Inspection Modal (Kiểm đếm nhận hàng chuỗi lạnh)
  const [inspectModalOpen, setInspectModalOpen] = useState(false);
  const [inspectItems, setInspectItems] = useState<
    {
      detailId: number;
      variantName: string;
      variantCode: string;
      uoMName: string;
      dispatchedQty: number;
      actualReceivedQuantity: number;
      damagedQuantity: number;
    }[]
  >([]);
  const [inspectNote, setInspectNote] = useState('');
  const [inspectLoading, setInspectLoading] = useState(false);

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

  const fetchTransfer = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await inventoryTransferApi.getById(Number(id));
      setTransfer(data);
    } catch (error) {
      console.error('Lỗi tải dữ liệu chuyển kho:', error);
      showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU CHUYỂN KHO');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchTransfer();
  }, [fetchTransfer]);

  const handleDispatch = () => {
    if (!transfer) return;
    showToast(
      'success',
      'Đã ghi nhận yêu cầu điều phối xe. Đang chuyển hướng sang Module Vận Tải...'
    );
    setTimeout(() => {
      navigate('/transportation/dashboard?tab=b2b');
    }, 400);
  };

  const handleReceive = async () => {
    if (!transfer) return;
    try {
      setActionLoading(true);
      await inventoryTransferApi.receive(transfer.id);
      showToast('success', 'ĐÃ TIẾP NHẬN HÀNG VÀO KHO ĐÍCH! HOÀN TẤT CHUYỂN KHO.');
      fetchTransfer(); // Reload lại data
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể nhận hàng!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleOpenInspect = () => {
    if (!transfer) return;
    const items = transfer.details.map((d) => ({
      detailId: d.id,
      variantName: d.variantName,
      variantCode: d.variantCode,
      uoMName: d.uoMName,
      dispatchedQty: d.quantity,
      actualReceivedQuantity: d.quantity,
      damagedQuantity: 0,
    }));
    setInspectItems(items);
    setInspectNote('');
    setInspectModalOpen(true);
  };

  const handleConfirmInspect = async () => {
    if (!transfer) return;
    try {
      setInspectLoading(true);
      const payload = {
        items: inspectItems.map((item) => ({
          detailId: item.detailId,
          actualReceivedQuantity: Number(item.actualReceivedQuantity) || 0,
          damagedQuantity: Number(item.damagedQuantity) || 0,
        })),
        note: inspectNote.trim() || undefined,
      };
      await inventoryTransferApi.inspectAndReceive(transfer.id, payload);
      showToast(
        'success',
        'KIỂM ĐẾM & NHẬN HÀNG THÀNH CÔNG! Đã tự động phân tách số lượng nguyên vẹn và hàng dập hỏng vào sổ cái tồn kho.'
      );
      setInspectModalOpen(false);
      fetchTransfer();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi kiểm đếm nhận hàng!');
    } finally {
      setInspectLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!transfer || !cancelReason.trim()) return;
    try {
      setActionLoading(true);
      await inventoryTransferApi.cancel(transfer.id, cancelReason.trim());
      showToast('success', 'ĐÃ HỦY LỆNH CHUYỂN KHO!');
      setCancelModalOpen(false);
      setCancelReason('');
      fetchTransfer(); // Reload lại data
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể hủy phiếu chuyển!');
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-slate-50/30 p-12">
        <Loader2 className="w-10 h-10 animate-spin text-indigo-500" />
      </div>
    );
  }

  if (!transfer)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">
        Không tìm thấy chứng từ chuyển kho!
      </div>
    );

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Chi Tiết Lệnh Chuyển Kho"
        subtitle={
          <>
            Mã chứng từ: <span className="font-bold text-slate-800">{transfer.transferCode}</span>
          </>
        }
        onBack={() => navigate('/inventory-transfers')}
        icon={ArrowLeftRight}
      />

      {/* ================= THÀNH CÔNG CỤ (ACTION TOOLBAR) ================= */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">
              Trạng thái:
            </span>
            <span
              className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${InventoryTransferStatusColors[transfer.status]}`}
            >
              {InventoryTransferStatusLabels[transfer.status]}
            </span>
          </div>

          {/* Vách ngăn nếu có nút tiếp theo */}
          {(transfer.status === InventoryTransferStatus.Draft ||
            transfer.status === InventoryTransferStatus.InTransit) && (
            <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>
          )}

          {/* Nút Điều phối (Nằm bên trái) */}
          <div className="flex items-center gap-2">
            {/* Nếu Nháp: Nút Gửi Yêu Cầu Điều Phối Xe */}
            {transfer.status === InventoryTransferStatus.Draft && (
              <button
                onClick={handleDispatch}
                disabled={actionLoading}
                className="flex items-center gap-2 px-5 py-2.5 bg-amber-400 hover:bg-amber-300 text-slate-900 rounded-xl font-bold text-sm transition-all shadow-xs disabled:opacity-50 cursor-pointer"
              >
                <Truck size={16} />
                Gửi Yêu Cầu Điều Phối Xe
              </button>
            )}

            {/* Nếu Đang đi đường: Nút Kiểm đếm & Tiếp nhận vào kho đích */}
            {transfer.status === InventoryTransferStatus.InTransit && (
              <>
                <button
                  onClick={handleOpenInspect}
                  disabled={actionLoading}
                  className="flex items-center gap-2 px-5 py-2.5 bg-amber-400 hover:bg-amber-300 text-slate-900 rounded-xl font-bold text-sm transition-all shadow-xs disabled:opacity-50 cursor-pointer"
                >
                  <ClipboardCheck size={16} strokeWidth={2.5} />
                  Kiểm Đếm & Tiếp Nhận
                </button>
                <button
                  onClick={handleReceive}
                  disabled={actionLoading}
                  className="flex items-center gap-2 px-4 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl font-bold text-sm transition-all disabled:opacity-50 cursor-pointer"
                  title="Nhận nhanh 100% nguyên vẹn vào kho đích"
                >
                  {actionLoading ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <CheckCircle size={16} />
                  )}
                  Nhận Hàng Vào Kho Đích
                </button>
              </>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          {/* NÚT IN CHỨNG TỪ */}
          <button
            type="button"
            onClick={() => setPrintModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 bg-white text-slate-700 border border-slate-200 rounded-xl font-bold text-xs hover:bg-slate-50 transition-colors shadow-xs cursor-pointer"
            title="In Lệnh Điều Chuyển Kho Nội Bộ"
          >
            In Lệnh Chuyển Kho
          </button>

          {/* Nút Hủy Lệnh: Nằm góc phải */}
          {(transfer.status === InventoryTransferStatus.Draft ||
            transfer.status === InventoryTransferStatus.InTransit) && (
            <button
              onClick={() => setCancelModalOpen(true)}
              disabled={actionLoading}
              className="flex items-center gap-2 px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-xl font-bold text-xs hover:bg-rose-50 transition-colors shadow-xs cursor-pointer"
            >
              <XCircle size={15} strokeWidth={2.5} />
              {transfer.status === InventoryTransferStatus.InTransit
                ? 'Hủy & Hoàn Về Kho Nguồn'
                : 'Hủy Lệnh'}
            </button>
          )}
        </div>
      </div>

      {/* ================= TABS ================= */}
      <TabGroup>
        <TabButton
          active={activeTab === 'info'}
          onClick={() => setActiveTab('info')}
          icon={FileText}
          label="1. THÔNG TIN TUYẾN CHUYỂN KHO"
        />
        <TabButton
          active={activeTab === 'items'}
          onClick={() => setActiveTab('items')}
          icon={Package}
          label={`2. CHI TIẾT MẶT HÀNG (${transfer.details?.length || 0})`}
        />
      </TabGroup>

      {/* ================= TAB 1: THÔNG TIN CHUNG ================= */}
      {activeTab === 'info' && (
        <DetailCard>
          <div className="p-8 flex flex-col gap-8 animate-in fade-in duration-300">
            {transfer.status === InventoryTransferStatus.Cancelled &&
              transfer.cancellationReason && (
                <div className="bg-rose-50 border border-rose-200 p-4 rounded-xl flex gap-3 items-start shadow-sm">
                  <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                  <div className="flex flex-col gap-1">
                    <h4 className="font-bold text-sm text-rose-800 uppercase tracking-wider">
                      Lý do hủy lệnh:
                    </h4>
                    <p className="text-sm text-rose-700 font-medium">
                      {transfer.cancellationReason}
                    </p>
                  </div>
                </div>
              )}

            {transfer.status === InventoryTransferStatus.InTransit && (
              <div className="p-6 bg-slate-50/80 rounded-2xl border border-slate-200">
                <div className="flex items-center justify-between mb-4">
                  <span className="text-xs font-black text-slate-700 uppercase tracking-wider">
                    Mốc Lộ Trình Vận Chuyển Liên Kho
                  </span>
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-amber-100 text-amber-900 border border-amber-300">
                    <span className="w-2 h-2 rounded-full bg-amber-500 animate-pulse" />
                    Đang Vận Chuyển Trên Đường
                  </span>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Mốc 1: Xuất bến kho nguồn */}
                  <div className="flex items-start gap-3 p-4 bg-white rounded-xl border border-slate-200">
                    <div className="w-8 h-8 rounded-full bg-emerald-100 text-emerald-700 font-black text-xs flex items-center justify-center shrink-0">
                      1
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase block">
                        1. Xuất Bến Kho Nguồn
                      </span>
                      <span className="font-bold text-slate-800 text-sm block">
                        {transfer.fromWarehouseName}
                      </span>
                      <span className="text-xs text-slate-500 mt-0.5 block">
                        {transfer.dispatchedDate
                          ? new Date(transfer.dispatchedDate).toLocaleString('vi-VN', {
                              hour: '2-digit',
                              minute: '2-digit',
                              day: '2-digit',
                              month: '2-digit',
                              year: 'numeric',
                            })
                          : 'Đã xuất kho'}
                      </span>
                    </div>
                  </div>

                  {/* Mốc 2: Đang trên đường */}
                  <div className="flex items-start gap-3 p-4 bg-white rounded-xl border-2 border-amber-400 shadow-xs">
                    <div className="w-8 h-8 rounded-full bg-amber-400 text-slate-900 font-black text-xs flex items-center justify-center shrink-0">
                      2
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-amber-700 uppercase block">
                        2. Đang Trên Đường
                      </span>
                      <span className="font-mono font-extrabold text-slate-900 text-sm block">
                        {transfer.licensePlate || 'Xe Tải Lạnh'}
                      </span>
                      <span className="text-xs text-slate-600 mt-0.5 block">
                        {transfer.driverName
                          ? `${transfer.driverName} • ${transfer.driverPhone || '---'}`
                          : 'Đội xe nội bộ'}
                      </span>
                    </div>
                  </div>

                  {/* Mốc 3: Chờ tiếp nhận */}
                  <div className="flex items-start gap-3 p-4 bg-white rounded-xl border border-slate-200 opacity-75">
                    <div className="w-8 h-8 rounded-full bg-slate-100 text-slate-500 font-black text-xs flex items-center justify-center shrink-0">
                      3
                    </div>
                    <div>
                      <span className="text-[11px] font-bold text-slate-400 uppercase block">
                        3. Chờ Tiếp Nhận Tại Đích
                      </span>
                      <span className="font-bold text-slate-800 text-sm block">
                        {transfer.toWarehouseName}
                      </span>
                      <span className="text-xs text-slate-400 mt-0.5 block">
                        Chờ kiểm đếm nhập kho
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <DetailSection title="Hành Trình Chuyển Kho" dotColor="bg-blue-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Kho nguồn (Xuất phát)"
                  value={
                    <span className="font-bold text-amber-700">{transfer.fromWarehouseName}</span>
                  }
                />
                <InfoField
                  label="Kho đích (Tiếp nhận)"
                  value={
                    <span className="font-bold text-emerald-700">{transfer.toWarehouseName}</span>
                  }
                />
                <InfoField
                  label="Đơn hàng liên quan"
                  value={
                    transfer.orderCode ? (
                      <button
                        onClick={() => navigate(`/orders/${transfer.orderId}`)}
                        className="text-indigo-600 hover:underline font-bold"
                      >
                        {transfer.orderCode}
                      </button>
                    ) : (
                      <span className="italic text-slate-500">Điều phối nội bộ</span>
                    )
                  }
                />
                <InfoField label="Người lập lệnh" value={transfer.createdByName} />
              </div>
            </DetailSection>

            <DetailSection title="Tiến Độ Vận Chuyển" dotColor="bg-indigo-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField label="Thủ kho xuất hàng" value={transfer.dispatchedByName || '---'} />
                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Thời gian xuất phát
                  </span>
                  {transfer.dispatchedDate ? (
                    <DateTimeCell isoString={transfer.dispatchedDate} />
                  ) : (
                    <span className="font-medium text-slate-800">---</span>
                  )}
                </div>

                <InfoField label="Thủ kho nhận hàng" value={transfer.receivedByName || '---'} />
                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Thời gian tiếp nhận
                  </span>
                  {transfer.receivedDate ? (
                    <DateTimeCell isoString={transfer.receivedDate} />
                  ) : (
                    <span className="font-medium text-slate-800">---</span>
                  )}
                </div>

                {transfer.driverName && (
                  <div className="col-span-full p-4 bg-blue-50/70 rounded-xl border border-blue-200 flex flex-col md:flex-row md:items-center justify-between gap-3">
                    <div className="flex items-center gap-3">
                      <span className="p-2 bg-blue-100 text-blue-700 rounded-xl">
                        <Truck className="w-5 h-5" />
                      </span>
                      <div>
                        <span className="font-extrabold text-blue-900 text-xs block">
                          Phương Tiện Vận Chuyển Liên Kho (Xe Tải Lạnh B2B)
                        </span>
                        <div className="text-xs text-slate-700 flex flex-wrap gap-4 mt-0.5">
                          <span>
                            Tài xế: <strong>{transfer.driverName}</strong>
                          </span>
                          <span>
                            SĐT: <strong>{transfer.driverPhone || '---'}</strong>
                          </span>
                          <span>
                            Biển số xe:{' '}
                            <strong className="font-mono">{transfer.licensePlate}</strong>
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {transfer.note && (
                <div className="mt-6 pt-6 border-t border-slate-100">
                  <InfoField
                    label="Ghi chú điều phối"
                    value={
                      <span className="italic text-slate-600 font-medium">{transfer.note}</span>
                    }
                  />
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
                    <th className="px-4 py-4 text-center bg-indigo-50/50 min-w-30">SL Chuyển</th>
                    {transfer.status === InventoryTransferStatus.Completed && (
                      <>
                        <th className="px-4 py-4 text-center bg-emerald-50/50 min-w-30 text-emerald-700">
                          SL Nguyên Vẹn
                        </th>
                        <th className="px-4 py-4 text-center bg-rose-50/50 min-w-30 text-rose-700">
                          SL Hỏng/Dập
                        </th>
                      </>
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {transfer.details?.map((item, idx) => (
                    <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-4 py-3 text-center text-slate-400 font-medium">
                        {idx + 1}
                      </td>
                      <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                      <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                      <td className="px-4 py-3 font-black text-indigo-600">{item.batchCode}</td>
                      <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>

                      {/* Cột Số lượng Chuyển kho nổi bật */}
                      <td className="px-4 py-3 text-center border-l border-indigo-100 bg-indigo-50/20">
                        <span className="font-black text-[15px] text-indigo-700">
                          {item.quantity}
                        </span>
                      </td>

                      {/* Cột Kiểm đếm thực tế */}
                      {transfer.status === InventoryTransferStatus.Completed && (
                        <>
                          <td className="px-4 py-3 text-center border-l border-emerald-100 bg-emerald-50/20">
                            <span className="font-black text-[15px] text-emerald-700">
                              {item.actualReceivedQuantity ?? item.quantity}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-center border-l border-rose-100 bg-rose-50/20">
                            <span
                              className={`font-black text-[15px] ${
                                (item.damagedQuantity || 0) > 0 ? 'text-rose-600' : 'text-slate-400'
                              }`}
                            >
                              {item.damagedQuantity || 0}
                            </span>
                          </td>
                        </>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </DetailCard>
      )}

      {/* ================= CANCEL MODAL ================= */}
      {cancelModalOpen && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200 border border-slate-100">
            <div className="flex justify-between items-center px-6 py-4.5 border-b border-rose-100 bg-rose-50/60 rounded-t-3xl">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-rose-100 text-rose-700 rounded-xl shadow-xs">
                  <AlertCircle size={20} strokeWidth={2.5} />
                </div>
                <h3 className="text-base font-extrabold text-slate-900 leading-tight">
                  Xác Nhận Hủy Chuyển Kho
                </h3>
              </div>
              <button
                type="button"
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
                className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200/60 rounded-xl transition-colors cursor-pointer"
              >
                <X size={20} />
              </button>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-5 leading-relaxed font-medium">
                Bạn đang thực hiện hủy lệnh chuyển kho{' '}
                <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded font-mono">
                  {transfer.transferCode}
                </strong>
                . Nếu hàng đang đi đường (InTransit), hệ thống sẽ tự động hoàn trả số lượng lại cho
                kho nguồn.
              </p>
              <div>
                <label className="block text-[11px] font-bold text-slate-600 uppercase tracking-wider mb-2">
                  Lý do hủy <span className="text-rose-500">*</span>
                </label>
                <textarea
                  className="w-full p-3 border border-slate-200 rounded-xl text-sm focus:ring-2 focus:ring-rose-400 focus:border-rose-400 outline-none resize-none text-slate-800 font-medium placeholder:text-slate-400"
                  rows={3}
                  placeholder="Thay đổi tuyến điều phối, hủy đơn hàng liên quan..."
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  autoFocus
                />
              </div>
            </div>
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3 rounded-b-3xl">
              <button
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
                disabled={actionLoading}
                className="px-5 py-2.5 text-sm font-bold text-slate-600 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shadow-xs cursor-pointer"
              >
                Đóng Lại
              </button>
              <button
                onClick={handleCancel}
                disabled={actionLoading || !cancelReason.trim()}
                className="flex items-center gap-2 px-5 py-2.5 bg-rose-600 text-white rounded-xl text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 shadow-xs cursor-pointer"
              >
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                Xác Nhận Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ================= INSPECTION & RECEIVE MODAL (KIỂM ĐẾM TIẾP NHẬN) ================= */}
      {inspectModalOpen && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="bg-white rounded-3xl shadow-2xl w-full max-w-3xl overflow-hidden animate-in zoom-in-95 duration-200 border border-slate-100 flex flex-col max-h-[90vh]">
            <div className="flex justify-between items-center px-6 py-4.5 bg-amber-50/80 border-b border-amber-200/60 rounded-t-3xl">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-amber-400 text-slate-900 rounded-xl shadow-xs">
                  <ClipboardCheck size={20} strokeWidth={2.5} />
                </div>
                <div>
                  <h3 className="text-base font-extrabold text-slate-900 leading-tight">
                    Kiểm Đếm Tiếp Nhận Chuyển Kho
                  </h3>
                  <p className="text-xs text-slate-500 font-medium">
                    Mã chứng từ:{' '}
                    <span className="font-bold text-slate-800 font-mono">
                      {transfer.transferCode}
                    </span>
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setInspectModalOpen(false)}
                className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200/60 rounded-xl transition-colors cursor-pointer"
              >
                <X size={20} />
              </button>
            </div>

            <div className="p-6 overflow-y-auto space-y-4">
              <div className="border border-slate-200/80 rounded-2xl overflow-hidden shadow-xs bg-white">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-700 font-extrabold uppercase border-b border-slate-200 text-[11px] tracking-wider">
                    <tr>
                      <th className="py-3.5 px-4">Sản Phẩm / SKU</th>
                      <th className="py-3.5 px-3 text-center">ĐVT</th>
                      <th className="py-3.5 px-3 text-center bg-slate-100/70 text-slate-700">
                        SL Xuất
                      </th>
                      <th className="py-3.5 px-3 text-center bg-amber-50/70 w-36 text-amber-900 border-x border-amber-200/60">
                        SL Nguyên Vẹn *
                      </th>
                      <th className="py-3.5 px-3 text-center bg-rose-50/60 w-36 text-rose-800">
                        SL Hỏng / Dập
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {inspectItems.map((item, index) => (
                      <tr key={item.detailId} className="hover:bg-amber-50/20 transition-colors">
                        <td className="py-3 px-4">
                          <span className="font-extrabold text-slate-800 text-xs block">
                            {item.variantName}
                          </span>
                          <span className="text-[11px] text-slate-400 font-mono">
                            {item.variantCode}
                          </span>
                        </td>
                        <td className="py-3 px-3 text-center text-slate-600 font-bold text-xs">
                          {item.uoMName}
                        </td>
                        <td className="py-3 px-3 text-center font-black text-slate-800 bg-slate-50/50 text-xs">
                          {item.dispatchedQty}
                        </td>
                        <td className="py-2.5 px-3 bg-amber-50/20 border-x border-amber-100/50">
                          <input
                            type="number"
                            min={0}
                            max={item.dispatchedQty}
                            value={item.actualReceivedQuantity}
                            onChange={(e) => {
                              const val = Math.max(0, Number(e.target.value) || 0);
                              const newItems = [...inspectItems];
                              newItems[index].actualReceivedQuantity = val;
                              newItems[index].damagedQuantity = Math.max(
                                0,
                                item.dispatchedQty - val
                              );
                              setInspectItems(newItems);
                            }}
                            className="w-full px-3 py-1.5 border border-amber-300 rounded-xl text-xs font-black text-slate-900 text-center outline-none focus:ring-2 focus:ring-amber-400 focus:border-amber-400 bg-white shadow-xs"
                          />
                        </td>
                        <td className="py-2.5 px-3 bg-rose-50/20">
                          <input
                            type="number"
                            min={0}
                            max={item.dispatchedQty}
                            value={item.damagedQuantity}
                            onChange={(e) => {
                              const val = Math.max(0, Number(e.target.value) || 0);
                              const newItems = [...inspectItems];
                              newItems[index].damagedQuantity = val;
                              newItems[index].actualReceivedQuantity = Math.max(
                                0,
                                item.dispatchedQty - val
                              );
                              setInspectItems(newItems);
                            }}
                            className="w-full px-3 py-1.5 border border-rose-300 rounded-xl text-xs font-black text-rose-700 text-center outline-none focus:ring-2 focus:ring-rose-400 focus:border-rose-400 bg-white shadow-xs"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div>
                <label className="block text-xs font-extrabold text-slate-700 uppercase tracking-wide mb-1.5">
                  Ghi chú biên bản kiểm đếm:
                </label>
                <input
                  type="text"
                  placeholder="Ví dụ: Nhiệt độ thùng xe 4°C, 2 vỉ dập màng trong lúc bốc dỡ..."
                  value={inspectNote}
                  onChange={(e) => setInspectNote(e.target.value)}
                  className="w-full px-3.5 py-2.5 border border-slate-200 rounded-xl text-xs outline-none focus:ring-2 focus:ring-amber-400 focus:border-amber-400 font-medium text-slate-800 placeholder:text-slate-400 bg-white shadow-xs"
                />
              </div>
            </div>

            <div className="flex justify-end gap-3 px-6 py-4 bg-slate-50 border-t border-slate-100 shrink-0 rounded-b-3xl">
              <button
                type="button"
                onClick={() => setInspectModalOpen(false)}
                disabled={inspectLoading}
                className="px-5 py-2.5 rounded-xl border border-slate-200 text-slate-600 font-bold text-sm hover:bg-slate-100 transition-colors cursor-pointer"
              >
                Đóng
              </button>
              <button
                type="button"
                onClick={handleConfirmInspect}
                disabled={inspectLoading}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl font-bold text-sm text-slate-900 bg-amber-400 hover:bg-amber-500 transition-all shadow-sm shadow-amber-200 active:scale-98 disabled:opacity-50 cursor-pointer"
              >
                {inspectLoading ? (
                  <>
                    <Loader2 size={16} className="animate-spin" />
                    <span>Đang xử lý...</span>
                  </>
                ) : (
                  <>
                    <CheckCircle size={16} strokeWidth={2.5} />
                    <span>Xác Nhận Nhập Kho & Chốt Sổ</span>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      {transfer && (
        <DocumentPrintModal
          isOpen={printModalOpen}
          onClose={() => setPrintModalOpen(false)}
          documentTitle="LỆNH ĐIỀU CHUYỂN KHO NỘI BỘ"
          documentSubtitle="Vận chuyển nông sản an toàn & duy trì chuỗi lạnh giữa các chi nhánh Solaris"
          documentCode={transfer.transferCode}
          documentDate={transfer.createdAt}
          warehouseName={`${transfer.sourceWarehouseName} ➔ ${transfer.destinationWarehouseName}`}
          creatorName="Bộ Phận Điều Phối Chuỗi Cung Ứng"
          partyTitle="Chi Nhánh Tiếp Nhận"
          partyName={transfer.destinationWarehouseName}
          partyAddress={`Kho xuất phát: ${transfer.sourceWarehouseName}`}
          referenceCode={transfer.transferCode}
          notes={
            transfer.note ||
            (transfer.vehicleNumber
              ? `Phương tiện: ${transfer.vehicleNumber} | Tài xế: ${transfer.driverName || 'N/A'}`
              : 'Đảm bảo điều kiện nhiệt độ mát/lạnh theo tiêu chuẩn bảo quản nông sản.')
          }
          items={(transfer.details || []).map((d) => ({
            skuCode: d.variantCode,
            productName: d.variantName,
            batchCode: d.batchCode,
            uoMName: d.uoMName || 'Kg',
            quantity: d.dispatchedQuantity,
            qcStatus:
              d.damagedQuantity && d.damagedQuantity > 0
                ? `Hao hụt: ${d.damagedQuantity} / Nhận: ${d.actualReceivedQuantity}`
                : 'Nguyên vẹn',
            note:
              d.actualReceivedQuantity !== undefined && d.actualReceivedQuantity !== null
                ? `Thực nhận kho đích: ${d.actualReceivedQuantity}`
                : undefined,
          }))}
          signatures={[
            {
              title: 'Thủ Kho Xuất (Nguồn)',
              subtitle: '(Ký, giao hàng)',
              name: transfer.sourceWarehouseName,
            },
            {
              title: 'Tài Xế Vận Chuyển',
              subtitle: '(Ký, nhận hàng & cam kết nhiệt độ)',
              name: transfer.driverName,
            },
            {
              title: 'Thủ Kho Tiếp Nhận (Đích)',
              subtitle: '(Ký, nhận hàng)',
              name: transfer.destinationWarehouseName,
            },
          ]}
        />
      )}
    </DetailPageContainer>
  );
};

export default InventoryTransferDetail;
