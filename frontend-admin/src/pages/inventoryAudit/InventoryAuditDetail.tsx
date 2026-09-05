import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ClipboardList,
  FileText,
  CheckCircle,
  XCircle,
  AlertCircle,
  Eye,
  EyeOff,
  Save,
  Scale,
  Loader2,
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
import { FormSelect } from '../../components/commons/FormUI';
import { Toast } from '../../components/commons/Toast';
import { DateTimeCell } from '../../components/commons/ListUI';

import { inventoryAuditApi } from '../../api/inventoryAuditApi';
import {
  InventoryAudit,
  InventoryAuditStatus,
  InventoryAuditStatusLabels,
  InventoryAuditStatusColors,
  InventoryAuditTypeLabels,
  InventoryAuditSubmitCountPayload,
} from '../../types/inventoryAudit';

const AUDIT_REASON_OPTIONS = [
  { value: 'Hao hụt tự nhiên (-)', label: 'Hao hụt tự nhiên (-)' },
  { value: 'Dập nát / Vỡ bao bì (-)', label: 'Dập nát / Vỡ bao bì (-)' },
  { value: 'Hư hỏng biến đổi chất lượng (-)', label: 'Hư hỏng biến đổi chất lượng (-)' },
  { value: 'Hết hạn sử dụng (-)', label: 'Hết hạn sử dụng (-)' },
  { value: 'Thất thoát / Mất cắp (-)', label: 'Thất thoát / Mất cắp (-)' },
  { value: 'Thừa do kiểm kê (+)', label: 'Thừa do kiểm kê (+)' },
  { value: 'Sai lệch nhập liệu (±)', label: 'Sai lệch nhập liệu (±)' },
  { value: 'Khác', label: 'Lý do khác (Nhập tay...)' },
];

const InventoryAuditDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [audit, setAudit] = useState<InventoryAudit | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'count' | 'info'>('count');

  // Blind Count Toggle
  const [isBlindCount, setIsBlindCount] = useState(false);

  // Count Inputs
  const [countInputs, setCountInputs] = useState<
    Record<number, { actualQuantity: number; reasonNote: string }>
  >({});
  const [actionLoading, setActionLoading] = useState(false);

  // Cancel modal
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');

  // Toast
  const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>(
    {
      show: false,
      type: 'success',
      message: '',
    }
  );

  const showToast = (type: 'success' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const fetchAudit = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await inventoryAuditApi.getById(Number(id));
      setAudit(data);

      const initialCounts: Record<number, { actualQuantity: number; reasonNote: string }> = {};
      data.details?.forEach((d) => {
        initialCounts[d.id] = {
          actualQuantity: d.actualQuantity,
          reasonNote: d.reasonNote || '',
        };
      });
      setCountInputs(initialCounts);
    } catch (error) {
      console.error('Error fetching audit:', error);
      showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU ĐỢT KIỂM KÊ');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchAudit();
  }, [fetchAudit]);

  const handleCountChange = (
    detailId: number,
    field: 'actualQuantity' | 'reasonNote',
    value: any
  ) => {
    setCountInputs((prev) => ({
      ...prev,
      [detailId]: {
        ...prev[detailId],
        [field]: value,
      },
    }));
  };

  // Save & Submit Count
  const handleSubmitCount = async () => {
    if (!audit) return;
    try {
      setActionLoading(true);
      const payload: InventoryAuditSubmitCountPayload = {
        items: Object.keys(countInputs).map((key) => ({
          detailId: Number(key),
          actualQuantity: Number(countInputs[Number(key)].actualQuantity || 0),
          reasonNote: countInputs[Number(key)].reasonNote,
        })),
      };

      await inventoryAuditApi.submitCount(audit.id, payload);
      showToast('success', 'ĐÃ NỘP SỐ LIỆU KIỂM ĐẾM THÀNH CÔNG!');
      fetchAudit();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể lưu số liệu kiểm đếm!');
    } finally {
      setActionLoading(false);
    }
  };

  // Approve & Reconcile (Auto Adjustment)
  const handleApprove = async () => {
    if (!audit) return;
    try {
      setActionLoading(true);
      const res = await inventoryAuditApi.approveAndReconcile(audit.id);
      showToast('success', 'CHỐT SỔ KIỂM KÊ & TỰ ĐỘNG CÂN BẰNG TỒN KHO THÀNH CÔNG!');
      fetchAudit();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể duyệt chốt kiểm kê!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = async () => {
    if (!audit || !cancelReason.trim()) return;
    try {
      setActionLoading(true);
      await inventoryAuditApi.cancel(audit.id, cancelReason.trim());
      showToast('success', 'ĐÃ HỦY ĐỢT KIỂM KÊ!');
      setCancelModalOpen(false);
      setCancelReason('');
      fetchAudit();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể hủy phiếu kiểm kê!');
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
      <div className="p-12 text-center text-slate-500 font-medium">Đang tải dữ liệu kiểm kê...</div>
    );
  if (!audit)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy đợt kiểm kê!</div>
    );

  const isEditable =
    audit.status === InventoryAuditStatus.Draft ||
    audit.status === InventoryAuditStatus.InProgress ||
    audit.status === InventoryAuditStatus.PendingApproval;

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Bảng Kiểm Kê Kho Hàng"
        subtitle={`Mã đợt: ${audit.auditCode} - Kho: ${audit.warehouseName}`}
        onBack={() => navigate('/inventory-audits')}
        icon={ClipboardList}
      />

      {/* ACTION TOOLBAR */}
      <div className="flex flex-wrap items-center justify-between gap-4 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex items-center gap-3">
          <span className="text-sm font-bold text-slate-500">Trạng thái:</span>
          <span
            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${InventoryAuditStatusColors[audit.status]}`}
          >
            {InventoryAuditStatusLabels[audit.status]}
          </span>
          <span className="text-xs font-medium text-slate-600 bg-slate-100 px-2.5 py-1 rounded-lg">
            {InventoryAuditTypeLabels[audit.auditType]}
          </span>
        </div>

        <div className="flex items-center gap-3">
          {/* Blind Count Switch */}
          <button
            type="button"
            onClick={() => setIsBlindCount(!isBlindCount)}
            className={`flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold border transition-colors cursor-pointer ${
              isBlindCount
                ? 'bg-amber-100 text-amber-950 border-amber-300 shadow-2xs'
                : 'bg-white text-slate-700 border-slate-300 hover:bg-slate-50'
            }`}
          >
            {isBlindCount ? <EyeOff size={16} /> : <Eye size={16} />}
            {isBlindCount ? 'Chế độ Đếm Mù: ĐANG BẬT' : 'Đếm Mù (Blind Count)'}
          </button>

          {isEditable && (
            <>
              <button
                onClick={handleSubmitCount}
                disabled={actionLoading}
                className="flex items-center gap-2 px-4 py-2 bg-amber-50 text-amber-950 border border-amber-300 hover:bg-amber-100 rounded-xl text-sm font-bold transition-colors disabled:opacity-50 cursor-pointer shadow-2xs"
              >
                <Save size={16} /> Lưu Số Liệu
              </button>

              <button
                onClick={handleApprove}
                disabled={actionLoading}
                className="flex items-center gap-2 px-5 py-2 bg-emerald-600 text-white hover:bg-emerald-700 rounded-xl text-sm font-bold transition-all shadow-sm shadow-emerald-200 disabled:opacity-50 cursor-pointer"
              >
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Scale size={16} />}
                Chốt Sổ & Cân Bằng Kho
              </button>

              <button
                onClick={() => setCancelModalOpen(true)}
                disabled={actionLoading}
                className="flex items-center gap-2 px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-xl text-sm font-bold hover:bg-rose-50 transition-colors cursor-pointer"
              >
                <XCircle size={16} /> Hủy Phiếu
              </button>
            </>
          )}
        </div>
      </div>

      {/* SUMMARY METRIC CARDS */}
      {!isBlindCount && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <div className="p-4 bg-white border border-slate-200 rounded-2xl shadow-xs">
            <div className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Tổng Tồn Hệ Thống
            </div>
            <div className="text-2xl font-black text-slate-800 mt-1">{audit.totalSystemQty}</div>
          </div>
          <div className="p-4 bg-white border border-slate-200 rounded-2xl shadow-xs">
            <div className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Tổng Thực Tế Đếm
            </div>
            <div className="text-2xl font-black text-amber-900 mt-1">{audit.totalActualQty}</div>
          </div>
          <div className="p-4 bg-white border border-slate-200 rounded-2xl shadow-xs">
            <div className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Chênh Lệch Số Lượng
            </div>
            <div
              className={`text-2xl font-black mt-1 ${audit.totalVarianceQty < 0 ? 'text-rose-600' : audit.totalVarianceQty > 0 ? 'text-emerald-600' : 'text-slate-800'}`}
            >
              {audit.totalVarianceQty > 0 ? `+${audit.totalVarianceQty}` : audit.totalVarianceQty}
            </div>
          </div>
          <div className="p-4 bg-white border border-slate-200 rounded-2xl shadow-xs">
            <div className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              Giá Trị Chênh Lệch
            </div>
            <div
              className={`text-2xl font-black mt-1 ${audit.totalVarianceAmount < 0 ? 'text-rose-600' : audit.totalVarianceAmount > 0 ? 'text-emerald-600' : 'text-slate-800'}`}
            >
              {formatCurrency(audit.totalVarianceAmount)}
            </div>
          </div>
        </div>
      )}

      {/* TABS */}
      <TabGroup>
        <TabButton
          active={activeTab === 'count'}
          onClick={() => setActiveTab('count')}
          icon={ClipboardList}
          label={`1. BẢNG KIỂM ĐẾM THỰC TẾ (${audit.details?.length || 0} DÒNG)`}
        />
        <TabButton
          active={activeTab === 'info'}
          onClick={() => setActiveTab('info')}
          icon={FileText}
          label="2. THÔNG TIN ĐỢT KIỂM KÊ"
        />
      </TabGroup>

      {/* TAB 1: BẢNG KIỂM ĐẾM */}
      {activeTab === 'count' && (
        <DetailCard>
          <div className="p-6">
            <div className="overflow-x-auto border border-slate-200 rounded-2xl shadow-2xs min-h-[380px] pb-28">
              <table className="w-full text-left whitespace-nowrap text-sm min-w-[900px]">
                <thead className="bg-slate-50 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                  <tr>
                    <th className="px-3 py-3.5 text-center w-10">#</th>
                    <th className="px-4 py-3.5 min-w-[200px]">Mặt Hàng (SKU)</th>
                    <th className="px-4 py-3.5 min-w-[160px]">Lô Hàng</th>
                    <th className="px-3 py-3.5 text-center w-20">ĐVT</th>
                    {!isBlindCount && (
                      <th className="px-3 py-3.5 text-center bg-slate-100/70 w-24">Tồn Hệ Thống</th>
                    )}
                    <th className="px-3 py-3.5 text-center bg-amber-50/70 text-amber-950 w-28">
                      Thực Tế Đếm
                    </th>
                    {!isBlindCount && <th className="px-3 py-3.5 text-center w-24">Chênh Lệch</th>}
                    {!isBlindCount && <th className="px-3 py-3.5 text-right w-24">Đơn Giá</th>}
                    {!isBlindCount && <th className="px-3 py-3.5 text-right w-28">Giá Trị Lệch</th>}
                    <th className="px-4 py-3.5 min-w-[220px]">Giải trình / Ghi chú</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {audit.details?.map((detail, idx) => {
                    const currentVal = countInputs[detail.id] || {
                      actualQuantity: detail.actualQuantity,
                      reasonNote: '',
                    };
                    const variance = Number(currentVal.actualQuantity || 0) - detail.systemQuantity;
                    const varAmount = variance * detail.unitPrice;

                    return (
                      <tr
                        key={detail.id}
                        className="hover:bg-slate-50/60 transition-colors"
                        style={{ zIndex: 50 - idx }}
                      >
                        <td className="px-3 py-3 text-center text-slate-400">{idx + 1}</td>
                        <td className="px-4 py-3">
                          <div className="font-bold text-slate-900 text-[13px]">{detail.variantName}</div>
                          <div className="text-[11px] font-semibold text-slate-400">{detail.variantCode}</div>
                        </td>
                        <td className="px-4 py-3 font-bold text-amber-800 text-[12px]">{detail.batchCode}</td>
                        <td className="px-3 py-3 text-center text-slate-600 font-medium">{detail.uoMName}</td>

                        {/* Tồn hệ thống */}
                        {!isBlindCount && (
                          <td className="px-3 py-3 text-center font-bold text-slate-700 bg-slate-50/40">
                            {detail.systemQuantity}
                          </td>
                        )}

                        {/* Số đếm thực tế (Input editable) */}
                        <td className="px-3 py-3 text-center bg-amber-50/20">
                          {isEditable ? (
                            <input
                              type="number"
                              min="0"
                              value={currentVal.actualQuantity}
                              onChange={(e) =>
                                handleCountChange(
                                  detail.id,
                                  'actualQuantity',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              className="w-20 px-2 py-1.5 border border-amber-300 rounded-lg text-center font-black text-amber-950 focus:ring-2 focus:ring-amber-500 outline-none shadow-2xs"
                            />
                          ) : (
                            <span className="font-black text-amber-950">
                              {detail.actualQuantity}
                            </span>
                          )}
                        </td>

                        {/* Chênh lệch */}
                        {!isBlindCount && (
                          <td className="px-3 py-3 text-center font-bold">
                            <span
                              className={
                                variance < 0
                                  ? 'text-rose-600'
                                  : variance > 0
                                    ? 'text-emerald-600'
                                    : 'text-slate-400'
                              }
                            >
                              {variance > 0 ? `+${variance}` : variance}
                            </span>
                          </td>
                        )}

                        {!isBlindCount && (
                          <td className="px-3 py-3 text-right text-slate-600">
                            {formatCurrency(detail.unitPrice)}
                          </td>
                        )}

                        {!isBlindCount && (
                          <td className="px-3 py-3 text-right font-bold">
                            <span
                              className={
                                varAmount < 0
                                  ? 'text-rose-600'
                                  : varAmount > 0
                                    ? 'text-emerald-600'
                                    : 'text-slate-400'
                              }
                            >
                              {formatCurrency(varAmount)}
                            </span>
                          </td>
                        )}

                        {/* Ghi chú lý do */}
                        <td className="p-2 min-w-[220px]">
                          {isEditable ? (
                            <div className="flex flex-col gap-1.5">
                              <FormSelect
                                label=""
                                placeholder="-- Chọn lý do giải trình --"
                                options={AUDIT_REASON_OPTIONS}
                                value={
                                  AUDIT_REASON_OPTIONS.slice(0, 7).some(
                                    (o) => o.value === currentVal.reasonNote
                                  )
                                    ? currentVal.reasonNote
                                    : currentVal.reasonNote
                                      ? 'Khác'
                                      : ''
                                }
                                onSelect={(val) => {
                                  if (val === 'Khác') {
                                    handleCountChange(detail.id, 'reasonNote', 'Khác: ');
                                  } else {
                                    handleCountChange(detail.id, 'reasonNote', val);
                                  }
                                }}
                              />

                              {(!AUDIT_REASON_OPTIONS.slice(0, 7).some(
                                (o) => o.value === currentVal.reasonNote
                              ) &&
                                currentVal.reasonNote !== '') && (
                                <input
                                  type="text"
                                  placeholder="Nhập chi tiết lý do..."
                                  value={currentVal.reasonNote}
                                  onChange={(e) =>
                                    handleCountChange(detail.id, 'reasonNote', e.target.value)
                                  }
                                  className="w-full px-3 py-1.5 border border-amber-300 rounded-xl text-xs focus:ring-2 focus:ring-amber-400 outline-none bg-amber-50/40 text-amber-950 font-medium"
                                  autoFocus
                                />
                              )}
                            </div>
                          ) : (
                            <span className="inline-flex text-xs font-medium text-slate-700 bg-slate-100 px-2.5 py-1 rounded-lg border border-slate-200">
                              {detail.reasonNote || '---'}
                            </span>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </DetailCard>
      )}

      {/* TAB 2: THÔNG TIN ĐỢT KIỂM KÊ */}
      {activeTab === 'info' && (
        <DetailCard>
          <div className="p-8 flex flex-col gap-8">
            <DetailSection title="Thông Tin Quản Trị">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Kho hàng"
                  value={<span className="font-bold text-indigo-700">{audit.warehouseName}</span>}
                />
                <InfoField
                  label="Hình thức kiểm kê"
                  value={InventoryAuditTypeLabels[audit.auditType]}
                />
                <InfoField label="Nhân viên kiểm đếm" value={audit.auditorName} />
                <InfoField label="Người duyệt chốt" value={audit.approvedByName || 'Chưa duyệt'} />
                <InfoField
                  label="Ngày bắt đầu"
                  value={<DateTimeCell isoString={audit.auditDate} />}
                />
                <InfoField
                  label="Ngày hoàn tất"
                  value={
                    audit.completedDate ? <DateTimeCell isoString={audit.completedDate} /> : '---'
                  }
                />
              </div>

              {audit.note && (
                <div className="mt-6 pt-6 border-t border-slate-100">
                  <InfoField
                    label="Ghi chú & Chỉ đạo"
                    value={<span className="italic text-slate-600">{audit.note}</span>}
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
                <AlertCircle size={20} /> Xác Nhận Hủy Đợt Kiểm Kê
              </h3>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-4">
                Bạn đang thực hiện hủy đợt kiểm kê{' '}
                <strong className="text-slate-900">{audit.auditCode}</strong>.
              </p>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
                Lý do hủy <span className="text-red-500">*</span>
              </label>
              <textarea
                className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-500 outline-none resize-none"
                rows={3}
                placeholder="Kiểm kê nhầm kho, hoãn kế hoạch..."
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
                className="px-4 py-2 border border-slate-300 rounded-lg text-sm font-bold text-slate-600 bg-white hover:bg-slate-50"
              >
                Đóng
              </button>
              <button
                onClick={handleCancel}
                disabled={actionLoading || !cancelReason.trim()}
                className="px-4 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 disabled:opacity-50 flex items-center gap-2"
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

export default InventoryAuditDetail;
