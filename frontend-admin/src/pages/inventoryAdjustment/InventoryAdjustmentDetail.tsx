import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  SlidersHorizontal,
  FileText,
  Package,
  CheckCircle,
  XCircle,
  AlertCircle,
  Loader2,
  Trash2,
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
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';

import { inventoryAdjustmentApi } from '../../api/inventoryAdjustmentApi';
import {
  InventoryAdjustment,
  InventoryAdjustmentStatus,
  InventoryAdjustmentStatusLabels,
  InventoryAdjustmentStatusColors,
  InventoryAdjustmentReasonLabels,
  InventoryAdjustmentType,
  InventoryAdjustmentTypeLabels,
} from '../../types/inventoryAdjustment';

const InventoryAdjustmentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [adj, setAdj] = useState<InventoryAdjustment | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'items' | 'info'>('items');

  // Actions
  const [actionLoading, setActionLoading] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  // Delete Modal
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);

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

  const fetchAdjustment = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await inventoryAdjustmentApi.getById(Number(id));
      setAdj(data);
    } catch (error) {
      console.error('Error fetching adjustment:', error);
      showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU ĐIỀU CHỈNH');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchAdjustment();
  }, [fetchAdjustment]);

  const handleApprove = async () => {
    if (!adj) return;
    try {
      setActionLoading(true);
      await inventoryAdjustmentApi.approve(adj.id);
      showToast('success', 'DUYỆT PHIẾU ĐIỀU CHỈNH THÀNH CÔNG! ĐÃ CẬP NHẬT TỒN KHO & GHI SỔ CÁI.');
      fetchAdjustment();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể duyệt phiếu điều chỉnh!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!adj || !cancelReason.trim()) return;
    try {
      setActionLoading(true);
      await inventoryAdjustmentApi.cancel(adj.id, cancelReason.trim());
      showToast('success', 'ĐÃ HỦY PHIẾU ĐIỀU CHỈNH!');
      setCancelModalOpen(false);
      setCancelReason('');
      fetchAdjustment();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể hủy phiếu điều chỉnh!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!adj) return;
    try {
      setActionLoading(true);
      await inventoryAdjustmentApi.delete(adj.id);
      showToast('success', 'XÓA PHIẾU ĐIỀU CHỈNH NHÁP THÀNH CÔNG');
      setDeleteModalOpen(false);
      setTimeout(() => navigate('/inventory-adjustments'), 1000);
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể xóa phiếu điều chỉnh!');
    } finally {
      setActionLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      amount || 0
    );
  };

  if (loading)
    return (
      <div className="p-12 text-center text-slate-500 font-medium">
        Đang tải dữ liệu phiếu điều chỉnh...
      </div>
    );
  if (!adj)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">
        Không tìm thấy chứng từ điều chỉnh!
      </div>
    );

  const totalAmount =
    adj.details?.reduce((sum, item) => sum + (Number(item.totalAmount) || 0), 0) || 0;

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Chi Tiết Phiếu Điều Chỉnh Tồn Kho"
        subtitle={`Mã chứng từ: ${adj.adjustmentCode}`}
        onBack={() => navigate('/inventory-adjustments')}
        icon={SlidersHorizontal}
      />

      {/* ACTION TOOLBAR */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex flex-wrap items-center gap-3">
          <span className="text-sm font-bold text-slate-500">Trạng thái:</span>
          <span
            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${InventoryAdjustmentStatusColors[adj.status]}`}
          >
            {InventoryAdjustmentStatusLabels[adj.status]}
          </span>
          <span className="text-xs font-medium text-slate-700 bg-slate-100 px-2.5 py-1 rounded-lg border border-slate-200">
            Lý do: {InventoryAdjustmentReasonLabels[adj.reason]}
          </span>
        </div>

        {adj.status === InventoryAdjustmentStatus.Draft && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => setDeleteModalOpen(true)}
              disabled={actionLoading}
              className="flex items-center gap-1.5 px-3 py-2 bg-rose-50 text-rose-600 border border-rose-200/80 rounded-lg font-bold text-xs hover:bg-rose-100 transition-colors cursor-pointer"
            >
              <Trash2 size={15} /> Xóa Nháp
            </button>
            <button
              onClick={() => setCancelModalOpen(true)}
              disabled={actionLoading}
              className="flex items-center gap-1.5 px-3 py-2 bg-slate-100 text-slate-700 border border-slate-200 rounded-lg font-bold text-xs hover:bg-slate-200 transition-colors cursor-pointer"
            >
              <XCircle size={15} /> Hủy Phiếu
            </button>
            <button
              onClick={handleApprove}
              disabled={actionLoading}
              className="flex items-center gap-2 px-5 py-2 bg-emerald-600 text-white rounded-lg font-bold text-xs hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-200 disabled:opacity-50 cursor-pointer"
            >
              {actionLoading ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <CheckCircle size={15} />
              )}
              Duyệt & Cập Nhật Tồn Kho
            </button>
          </div>
        )}
      </div>

      {/* TABS */}
      <TabGroup>
        <TabButton
          active={activeTab === 'items'}
          onClick={() => setActiveTab('items')}
          icon={Package}
          label={`1. CHI TIẾT ĐIỀU CHỈNH (${adj.details?.length || 0})`}
        />
        <TabButton
          active={activeTab === 'info'}
          onClick={() => setActiveTab('info')}
          icon={FileText}
          label="2. THÔNG TIN CHỨNG TỪ"
        />
      </TabGroup>

      {/* TAB 1: CHI TIẾT MẶT HÀNG */}
      {activeTab === 'items' && (
        <DetailCard>
          <div className="p-6 flex flex-col gap-6">
            <div className="overflow-x-auto border border-slate-200 rounded-xl shadow-sm">
              <table className="w-full text-left whitespace-nowrap min-w-225">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3.5 text-center w-12">#</th>
                    <th className="px-4 py-3.5">Mã SKU</th>
                    <th className="px-4 py-3.5">Tên Sản Phẩm</th>
                    <th className="px-4 py-3.5">Lô Hàng</th>
                    <th className="px-4 py-3.5">Loại Điều Chỉnh</th>
                    <th className="px-4 py-3.5 text-center">ĐVT</th>
                    <th className="px-4 py-3.5 text-center font-bold text-slate-800">Số Lượng</th>
                    <th className="px-4 py-3.5 text-right">Đơn Giá</th>
                    <th className="px-4 py-3.5 text-right">Thành Tiền</th>
                    <th className="px-4 py-3.5">Lý do chi tiết</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {adj.details?.map((item, idx) => {
                    const isIncrease =
                      item.adjustmentType === InventoryAdjustmentType.IncreaseAvailable;
                    return (
                      <tr key={item.id || idx} className="hover:bg-slate-50">
                        <td className="px-4 py-3 text-center text-slate-400">{idx + 1}</td>
                        <td className="px-4 py-3 font-bold text-slate-700">{item.variantCode}</td>
                        <td className="px-4 py-3 font-medium text-slate-900">{item.variantName}</td>
                        <td className="px-4 py-3 font-bold text-indigo-700">{item.batchCode}</td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-bold ${
                              isIncrease
                                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                                : 'bg-rose-50 text-rose-700 border border-rose-200'
                            }`}
                          >
                            {InventoryAdjustmentTypeLabels[item.adjustmentType]}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>
                        <td className="px-4 py-3 text-center font-black text-slate-900">
                          {item.quantity}
                        </td>
                        <td className="px-4 py-3 text-right text-slate-600">
                          {formatCurrency(item.unitPrice)}
                        </td>
                        <td className="px-4 py-3 text-right font-bold text-slate-900">
                          {formatCurrency(item.totalAmount)}
                        </td>
                        <td className="px-4 py-3 text-xs italic text-slate-600">
                          {item.reasonDetail || '---'}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
                <tfoot className="bg-slate-50/80 border-t border-slate-200 font-bold text-sm">
                  <tr>
                    <td
                      colSpan={8}
                      className="px-4 py-4 text-right text-slate-800 uppercase tracking-wider text-xs font-bold"
                    >
                      Tổng Giá Trị Điều Chỉnh:
                    </td>
                    <td
                      colSpan={2}
                      className="px-4 py-4 text-left text-lg font-black text-slate-900"
                    >
                      {formatCurrency(totalAmount)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </DetailCard>
      )}

      {/* TAB 2: THÔNG TIN CHỨNG TỪ */}
      {activeTab === 'info' && (
        <DetailCard>
          <div className="p-6 flex flex-col gap-6">
            <DetailSection title="Thông Tin Quản Trị">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Kho hàng"
                  value={<span className="font-bold text-indigo-700">{adj.warehouseName}</span>}
                />
                <InfoField
                  label="Đợt kiểm kê gốc"
                  value={
                    adj.auditCode ? (
                      <button
                        onClick={() => navigate(`/inventory-audits/${adj.auditId}`)}
                        className="text-indigo-600 hover:underline font-bold"
                      >
                        {adj.auditCode}
                      </button>
                    ) : (
                      'Điều chỉnh thủ công'
                    )
                  }
                />
                <InfoField label="Người lập phiếu" value={adj.createdByName} />
                <InfoField label="Người duyệt" value={adj.approvedByName || 'Chưa duyệt'} />
                <InfoField label="Ngày lập" value={<DateCell isoString={adj.adjustmentDate} />} />
                <InfoField
                  label="Ngày duyệt"
                  value={adj.approvedDate ? <DateTimeCell isoString={adj.approvedDate} /> : '---'}
                />
              </div>

              {adj.note && (
                <div className="mt-6 pt-6 border-t border-slate-100">
                  <InfoField
                    label="Ghi chú & Biên bản giải trình"
                    value={<span className="italic text-slate-600">{adj.note}</span>}
                  />
                </div>
              )}
            </DetailSection>
          </div>
        </DetailCard>
      )}

      {/* CANCEL MODAL */}
      {cancelModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 bg-rose-50/30">
              <h3 className="text-lg font-black text-rose-700 flex items-center gap-2">
                <AlertCircle size={20} /> Xác Nhận Hủy Phiếu Điều Chỉnh
              </h3>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-4">
                Bạn đang thực hiện hủy phiếu điều chỉnh{' '}
                <strong className="text-slate-900">{adj.adjustmentCode}</strong>.
              </p>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
                Lý do hủy <span className="text-red-500">*</span>
              </label>
              <textarea
                className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-500 outline-none resize-none"
                rows={3}
                placeholder="Nhập nhầm số liệu, hủy theo lệnh cấp trên..."
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                autoFocus
              />
            </div>
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3">
              <button
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
                disabled={actionLoading}
                className="px-4 py-2 border border-slate-300 rounded-lg text-sm font-bold text-slate-600 bg-white hover:bg-slate-50 cursor-pointer"
              >
                Đóng
              </button>
              <button
                onClick={handleCancel}
                disabled={actionLoading || !cancelReason.trim()}
                className="px-4 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 disabled:opacity-50 flex items-center gap-2 cursor-pointer"
              >
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                Xác Nhận Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {/* DELETE MODAL */}
      <ConfirmDeleteModal
        isOpen={deleteModalOpen}
        onClose={() => setDeleteModalOpen(false)}
        onConfirm={handleDelete}
        loading={actionLoading}
        title="Xóa Phiếu Điều Chỉnh Nháp"
        message={`Bạn có chắc chắn muốn xóa phiếu điều chỉnh "${adj.adjustmentCode}" không? Thao tác này không thể hoàn tác.`}
      />
    </DetailPageContainer>
  );
};

export default InventoryAdjustmentDetail;
