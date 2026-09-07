import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  RotateCcw,
  FileText,
  Package,
  ShieldCheck,
  XCircle,
  CheckCircle,
  AlertCircle,
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
import { Toast } from '../../components/commons/Toast';
import { DateCell, DateTimeCell } from '../../components/commons/ListUI';

import { customerReturnApi } from '../../api/customerReturnApi';
import {
  CustomerReturn,
  CustomerReturnStatus,
  CustomerReturnStatusLabels,
  CustomerReturnStatusColors,
  CustomerReturnInspectionPayload,
  CustomerReturnItemInspectionPayload,
} from '../../types/customerReturn';

const CustomerReturnDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [ret, setRet] = useState<CustomerReturn | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

  // QC Inspection Modal
  const [qcModalOpen, setQcModalOpen] = useState(false);
  const [inspectionItems, setInspectionItems] = useState<CustomerReturnItemInspectionPayload[]>([]);
  const [inspectionNotes, setInspectionNotes] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  // Reject Modal
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

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

  const fetchReturn = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await customerReturnApi.getById(Number(id));
      setRet(data);

      if (data.details) {
        setInspectionItems(
          data.details.map((d) => ({
            detailId: d.id,
            acceptedQuantity: d.acceptedQuantity > 0 ? d.acceptedQuantity : d.returnedQuantity,
            damagedQuantity: d.damagedQuantity,
            rejectReason: d.rejectReason || '',
          }))
        );
      }
    } catch (error) {
      console.error('Lỗi tải phiếu trả hàng:', error);
      showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU TRẢ HÀNG');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchReturn();
  }, [fetchReturn]);

  const handleInspectionChange = (
    detailId: number,
    field: keyof CustomerReturnItemInspectionPayload,
    val: any
  ) => {
    setInspectionItems((prev) =>
      prev.map((item) => {
        if (item.detailId === detailId) {
          return { ...item, [field]: val };
        }
        return item;
      })
    );
  };

  const handleApprove = async () => {
    if (!ret) return;
    try {
      setActionLoading(true);
      await customerReturnApi.approve(ret.id);
      showToast('success', 'ĐÃ DUYỆT YÊU CẦU TRẢ HÀNG THÀNH CÔNG! KHÁCH HÀNG SẼ GỬI HÀNG VỀ KHO.');
      fetchReturn();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể duyệt yêu cầu trả hàng!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleSaveQC = async () => {
    if (!ret) return;
    try {
      setActionLoading(true);
      const payload: CustomerReturnInspectionPayload = {
        inspectionNotes: inspectionNotes.trim(),
        items: inspectionItems,
      };

      await customerReturnApi.inspect(ret.id, payload);
      showToast(
        'success',
        'ĐÃ LƯU KẾT QUẢ KIỂM ĐỊNH QC! BẠN CÓ THỂ TẠO PHIẾU NHẬP KHO THU HỒI ĐỂ NHẬP LẠI HÀNG.'
      );
      setQcModalOpen(false);
      fetchReturn();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể lưu kết quả kiểm định QC!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleCompleteReturn = async () => {
    if (!ret) return;
    try {
      setActionLoading(true);
      await customerReturnApi.complete(ret.id);
      showToast('success', 'ĐÃ HOÀN TẤT PHIẾU TRẢ HÀNG & HẠCH TOÁN TỒN KHO THÀNH CÔNG!');
      fetchReturn();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể hoàn tất phiếu trả hàng!');
    } finally {
      setActionLoading(false);
    }
  };

  const handleReject = async () => {
    if (!ret || !rejectReason.trim()) return;
    try {
      setActionLoading(true);
      await customerReturnApi.reject(ret.id, rejectReason.trim());
      showToast('success', 'ĐÃ TỪ CHỐI NHẬN HÀNG HOÀN TRẢ!');
      setRejectModalOpen(false);
      setRejectReason('');
      fetchReturn();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Không thể từ chối trả hàng!');
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

  if (!ret)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">
        Không tìm thấy chứng từ trả hàng!
      </div>
    );

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Chi Tiết Phiếu Trả Hàng (RMA)"
        subtitle={
          <>
            Mã chứng từ: <span className="font-bold text-slate-800">{ret.returnCode}</span>
          </>
        }
        onBack={() => navigate('/customer-returns')}
        icon={RotateCcw}
      />

      {/* ================= THÀNH CÔNG CỤ (ACTION TOOLBAR) ================= */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">
              Trạng thái:
            </span>
            <span
              className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border shadow-sm ${CustomerReturnStatusColors[ret.status]}`}
            >
              {CustomerReturnStatusLabels[ret.status]}
            </span>
            {ret.status === CustomerReturnStatus.Completed && (
              <span className="text-xs font-bold text-emerald-700 bg-emerald-50 px-3 py-1 rounded-full border border-emerald-200 shadow-sm">
                Đã hoàn tiền: {formatCurrency(ret.refundAmount)}
              </span>
            )}
          </div>

          <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>

          {/* Nhóm nút theo từng trạng thái */}
          <div className="flex items-center gap-2">
            {/* 1. Trạng thái Pending: Nút Duyệt Yêu Cầu */}
            {ret.status === CustomerReturnStatus.Pending && (
              <button
                onClick={handleApprove}
                disabled={actionLoading}
                className="flex items-center gap-2 px-4 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-200 disabled:opacity-50"
              >
                <CheckCircle size={16} /> Duyệt Yêu Cầu
              </button>
            )}

            {/* 2. Trạng thái Approved: Nút Kiểm Định QC (khi hàng về kho) */}
            {ret.status === CustomerReturnStatus.Approved && (
              <button
                onClick={() => setQcModalOpen(true)}
                disabled={actionLoading}
                className="flex items-center gap-2 px-5 py-2 bg-indigo-600 text-white rounded-lg font-bold text-sm hover:bg-indigo-700 transition-all shadow-sm shadow-indigo-200 disabled:opacity-50"
              >
                <ShieldCheck size={16} /> Kiểm Định QC & Nghiệm Thu
              </button>
            )}

            {/* 3. Trạng thái Inspecting (Đang xử lý sau khi QC): Nút Tạo Phiếu Nhập Kho Thu Hồi + Hoàn tất */}
            {ret.status === CustomerReturnStatus.Inspecting && (
              <>
                <button
                  onClick={() => navigate(`/inventory-receipts/create?returnId=${ret.id}`)}
                  className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg font-bold text-sm hover:bg-blue-700 transition-all shadow-sm shadow-blue-200"
                >
                  <Package size={16} /> Tạo Phiếu Nhập Kho Thu Hồi
                </button>

                <button
                  onClick={handleCompleteReturn}
                  disabled={actionLoading}
                  className="flex items-center gap-2 px-4 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-200 disabled:opacity-50"
                >
                  <CheckCircle size={16} /> Hoàn Tất Trả Hàng
                </button>

                <button
                  onClick={() => setQcModalOpen(true)}
                  disabled={actionLoading}
                  className="flex items-center gap-2 px-3 py-2 bg-slate-100 text-slate-700 rounded-lg font-medium text-xs hover:bg-slate-200 transition-all"
                >
                  <ShieldCheck size={14} /> Sửa Kết Quả QC
                </button>
              </>
            )}
          </div>
        </div>

        {/* Nút Từ Chối (Hiện khi chưa hoàn tất hoặc từ chối) */}
        {(ret.status === CustomerReturnStatus.Pending ||
          ret.status === CustomerReturnStatus.Approved ||
          ret.status === CustomerReturnStatus.Inspecting) && (
          <button
            onClick={() => setRejectModalOpen(true)}
            disabled={actionLoading}
            className="flex items-center gap-2 px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-50 transition-colors shadow-sm"
          >
            <XCircle size={16} strokeWidth={2.5} /> Từ Chối Trả Hàng
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
          label={`2. CHI TIẾT KIỂM ĐỊNH QC (${ret.details?.length || 0})`}
        />
      </TabGroup>

      {/* ================= TAB 1: THÔNG TIN CHUNG ================= */}
      {activeTab === 'info' && (
        <DetailCard>
          <div className="p-8 flex flex-col gap-8 animate-in fade-in duration-300">
            <DetailSection title="Thông Tin Tiếp Nhận Hàng" dotColor="bg-blue-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Đơn hàng gốc"
                  value={
                    <button
                      onClick={() => navigate(`/orders/${ret.orderId}`)}
                      className="text-indigo-600 hover:underline font-bold"
                    >
                      {ret.orderCode}
                    </button>
                  }
                />
                <InfoField
                  label="Khách hàng"
                  value={<span className="font-bold text-slate-800">{ret.customerName}</span>}
                />
                <InfoField
                  label="Kho tiếp nhận"
                  value={<span className="font-bold text-indigo-700">{ret.warehouseName}</span>}
                />
                <InfoField label="Người tiếp nhận / QC" value={ret.receivedByName || '---'} />

                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Ngày tiếp nhận
                  </span>
                  <DateCell isoString={ret.returnDate} />
                </div>

                <InfoField
                  label="Số tiền hoàn lại"
                  value={
                    <span className="font-black text-rose-600 text-lg">
                      {formatCurrency(ret.refundAmount)}
                    </span>
                  }
                />
              </div>
            </DetailSection>

            <DetailSection title="Lý Do & Đánh Giá Chất Lượng" dotColor="bg-emerald-400">
              <div className="flex flex-col gap-4">
                <InfoField
                  label="Lý do khách trả hàng"
                  value={<span className="text-slate-800 font-medium">{ret.reason || '---'}</span>}
                />
                {ret.inspectionNotes && (
                  <div className="p-5 bg-slate-50 border border-slate-200 rounded-2xl">
                    <div className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                      Ghi chú kiểm định QC:
                    </div>
                    <div className="text-sm font-medium text-slate-700 leading-relaxed">
                      {ret.inspectionNotes}
                    </div>
                  </div>
                )}
              </div>
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
                    <th className="px-4 py-4 text-center min-w-25">SL Trả Về</th>
                    <th className="px-4 py-4 text-center bg-emerald-50/50 text-emerald-800 min-w-35">
                      SL Đạt (Nhập Tốt)
                    </th>
                    <th className="px-4 py-4 text-center bg-amber-50/50 text-amber-800 min-w-35">
                      SL Hỏng (Vào Kho Lỗi)
                    </th>
                    <th className="px-4 py-4 text-right min-w-30">Đơn Giá</th>
                    <th className="px-4 py-4 text-right min-w-37.5">Tiền Hoàn Lại</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {ret.details?.map((item, idx) => (
                    <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-4 py-3 text-center text-slate-400 font-medium">
                        {idx + 1}
                      </td>
                      <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                      <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                      <td className="px-4 py-3 font-black text-indigo-600">{item.batchCode}</td>
                      <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>
                      <td className="px-4 py-3 text-center font-bold text-slate-700">
                        {item.returnedQuantity}
                      </td>

                      {/* Cột SL Đạt */}
                      <td className="px-4 py-3 text-center font-black text-emerald-700 bg-emerald-50/20 border-l border-emerald-100">
                        {item.acceptedQuantity}
                      </td>

                      {/* Cột SL Hỏng */}
                      <td className="px-4 py-3 text-center font-black text-amber-700 bg-amber-50/20 border-l border-amber-100">
                        {item.damagedQuantity}
                      </td>

                      <td className="px-4 py-3 text-right font-medium text-slate-600 border-l border-slate-100">
                        {formatCurrency(item.unitPrice)}
                      </td>
                      <td className="px-4 py-3 text-right font-black text-rose-600">
                        {formatCurrency(item.refundAmount)}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-slate-50/80 border-t border-slate-200 text-sm">
                  <tr className="border-t border-slate-200 bg-rose-50/30">
                    <td
                      colSpan={9}
                      className="px-4 py-4 text-right text-rose-800 uppercase tracking-wider text-xs font-black"
                    >
                      Tổng Tiền Hoàn Khách Hàng:
                    </td>
                    <td className="px-4 py-4 text-right text-[17px] font-black text-rose-600">
                      {formatCurrency(ret.refundAmount)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </DetailCard>
      )}

      {/* ================= QC INSPECTION MODAL ================= */}
      {qcModalOpen && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 overflow-y-auto animate-in fade-in">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-4xl overflow-hidden my-8 animate-in zoom-in-95 duration-200">
            <div className="px-6 py-4 border-b border-indigo-100 bg-indigo-50/50 flex items-center justify-between">
              <h3 className="text-lg font-black text-indigo-900 flex items-center gap-2">
                <ShieldCheck size={20} className="text-indigo-600" /> Nghiệm Thu Kiểm Định QC
              </h3>
              <span className="text-xs font-bold text-slate-500 bg-white px-2.5 py-1 rounded-lg border border-slate-200">
                Mã: {ret.returnCode}
              </span>
            </div>
            <div className="p-6 max-h-[70vh] overflow-y-auto">
              <p className="text-xs text-slate-600 mb-5 leading-relaxed bg-indigo-50/30 p-3 rounded-xl border border-indigo-100">
                <strong>Hướng dẫn:</strong> Phân loại số lượng hàng đạt tiêu chuẩn (sẽ cộng lại
                vào <strong>Tồn kho Khả Dụng</strong>) và số lượng hàng hư hỏng (sẽ cộng vào{' '}
                <strong>Tồn kho Hàng Lỗi/Hỏng</strong>).
              </p>

              <div className="overflow-x-auto border border-slate-200 rounded-xl mb-6 shadow-sm">
                <table className="w-full text-left text-xs whitespace-nowrap">
                  <thead className="bg-slate-50 text-slate-600 font-bold uppercase tracking-wider border-b border-slate-200">
                    <tr>
                      <th className="px-4 py-3">Sản Phẩm / Lô</th>
                      <th className="px-4 py-3 text-center">SL Trả</th>
                      <th className="px-4 py-3 text-center text-emerald-700 bg-emerald-50/50">
                        SL Đạt (Tốt)
                      </th>
                      <th className="px-4 py-3 text-center text-amber-700 bg-amber-50/50">
                        SL Hỏng (Lỗi)
                      </th>
                      <th className="px-4 py-3 min-w-50">Lý do lỗi / Ghi chú item</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {ret.details?.map((d) => {
                      const insp = inspectionItems.find((i) => i.detailId === d.id);
                      return (
                        <tr key={d.id} className="hover:bg-slate-50 transition-colors">
                          <td className="px-4 py-3">
                            <div className="font-bold text-slate-800 text-sm">{d.variantName}</div>
                            <div className="text-[11px] font-semibold text-indigo-600">
                              Lô: {d.batchCode || '---'}
                            </div>
                          </td>
                          <td className="px-4 py-3 text-center font-bold text-slate-700 text-sm">
                            {d.returnedQuantity}
                          </td>
                          <td className="px-4 py-3 text-center bg-emerald-50/20 border-l border-emerald-100">
                            <input
                              type="number"
                              min="0"
                              max={d.returnedQuantity}
                              value={insp?.acceptedQuantity ?? d.returnedQuantity}
                              onChange={(e) =>
                                handleInspectionChange(
                                  d.id,
                                  'acceptedQuantity',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              className="w-24 px-2.5 py-1.5 border border-emerald-300 rounded-lg text-center font-bold text-emerald-700 text-sm focus:ring-2 focus:ring-emerald-400 outline-none bg-white shadow-sm"
                            />
                          </td>
                          <td className="px-4 py-3 text-center bg-amber-50/20 border-l border-amber-100">
                            <input
                              type="number"
                              min="0"
                              max={d.returnedQuantity}
                              value={insp?.damagedQuantity ?? 0}
                              onChange={(e) =>
                                handleInspectionChange(
                                  d.id,
                                  'damagedQuantity',
                                  parseFloat(e.target.value) || 0
                                )
                              }
                              className="w-24 px-2.5 py-1.5 border border-amber-300 rounded-lg text-center font-bold text-amber-700 text-sm focus:ring-2 focus:ring-amber-400 outline-none bg-white shadow-sm"
                            />
                          </td>
                          <td className="px-4 py-3 border-l border-slate-100">
                            <input
                              type="text"
                              placeholder="Ví dụ: Dập nát vỏ ngoài, lỗi nguồn..."
                              value={insp?.rejectReason ?? ''}
                              onChange={(e) =>
                                handleInspectionChange(d.id, 'rejectReason', e.target.value)
                              }
                              className="w-full px-3 py-1.5 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-indigo-400 outline-none bg-white"
                            />
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
                  Kết luận & Biên bản kiểm định chung
                </label>
                <textarea
                  className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none resize-none font-medium text-slate-800"
                  rows={3}
                  placeholder="Hàng đạt 80% chất lượng ban đầu, số còn lại cấn móp trong quá trình vận chuyển..."
                  value={inspectionNotes}
                  onChange={(e) => setInspectionNotes(e.target.value)}
                />
              </div>
            </div>
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3">
              <button
                onClick={() => setQcModalOpen(false)}
                disabled={actionLoading}
                className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors shadow-sm"
              >
                Hủy Bỏ
              </button>
              <button
                onClick={handleSaveQC}
                disabled={actionLoading}
                className="px-5 py-2 bg-indigo-600 text-white rounded-lg text-sm font-bold hover:bg-indigo-700 transition-all disabled:opacity-50 flex items-center gap-2 shadow-sm shadow-indigo-200"
              >
                {actionLoading ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <CheckCircle size={16} />
                )}
                Lưu Kết Quả Kiểm Định & Chuyển Sang Xử Lý
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ================= REJECT MODAL ================= */}
      {rejectModalOpen && (
        <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="px-6 py-4 border-b border-rose-100 bg-rose-50/50">
              <h3 className="text-lg font-black text-rose-700 flex items-center gap-2">
                <XCircle size={20} strokeWidth={2.5} /> Từ Chối Nhận Hàng Hoàn Trả
              </h3>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                Bạn đang từ chối tiếp nhận trả hàng cho phiếu{' '}
                <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">
                  {ret.returnCode}
                </strong>
                .
              </p>
              <div>
                <label className="block text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-2">
                  Lý do từ chối <span className="text-rose-500">*</span>
                </label>
                <textarea
                  className="w-full p-3 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-rose-500 focus:border-rose-500 outline-none resize-none text-slate-800 font-medium"
                  rows={3}
                  placeholder="Quá thời hạn đổi trả 7 ngày, lỗi do người dùng làm vỡ..."
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  autoFocus
                />
              </div>
            </div>
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3">
              <button
                onClick={() => {
                  setRejectModalOpen(false);
                  setRejectReason('');
                }}
                disabled={actionLoading}
                className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors shadow-sm"
              >
                Đóng Lại
              </button>
              <button
                onClick={handleReject}
                disabled={actionLoading || !rejectReason.trim()}
                className="px-5 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 shadow-sm flex items-center gap-2"
              >
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                Xác Nhận Từ Chối
              </button>
            </div>
          </div>
        </div>
      )}
    </DetailPageContainer>
  );
};

export default CustomerReturnDetail;
