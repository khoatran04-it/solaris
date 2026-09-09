import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ClipboardCheck,
  FileText,
  Package,
  Loader2,
  CheckCircle,
  XCircle,
  AlertCircle,
  Trash2,
} from 'lucide-react';

// API & Types
import { inventoryReceiptApi } from '../../api/inventoryReceiptApi';
import {
  InventoryReceipt,
  InventoryReceiptStatus,
  InventoryReceiptStatusLabels,
  InventoryReceiptStatusColors,
} from '../../types/inventoryReceipt';

// Components
import { Toast } from '../../components/commons/Toast';
import {
  DetailPageContainer,
  DetailHeader,
  TabGroup,
  TabButton,
  DetailCard,
  DetailSection,
  InfoField,
} from '../../components/commons/TabUI';
import { DateCell, DateTimeCell, TableEmpty } from '../../components/commons/ListUI';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import { DocumentPrintModal } from '../../components/commons/DocumentPrintModal';

type TabType = 'info' | 'details';

const InventoryReceiptDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // --- STATES ---
  const [receipt, setReceipt] = useState<InventoryReceipt | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<TabType>('info');
  const [toast, setToast] = useState<{ show: boolean; type: 'error' | 'success'; message: string }>(
    { show: false, type: 'error', message: '' }
  );
  const [printModalOpen, setPrintModalOpen] = useState(false);

  // Cancel Modal States
  const [isCancelModalOpen, setIsCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [isCancelling, setIsCancelling] = useState(false);

  // Delete Modal States
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  // Complete Action State
  const [isCompleting, setIsCompleting] = useState(false);

  // --- EFFECTS ---
  const fetchReceipt = () => {
    setLoading(true);
    inventoryReceiptApi
      .getById(Number(id))
      .then((res) => setReceipt(res))
      .catch(() => showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU NHẬP KHO!'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    if (id) fetchReceipt();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  // --- HANDLERS ---
  const showToast = (type: 'success' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  const handleComplete = async () => {
    if (!id) return;
    setIsCompleting(true);
    try {
      await inventoryReceiptApi.complete(Number(id));
      showToast('success', 'HOÀN TẤT NHẬP KHO THÀNH CÔNG! ĐÃ CẬP NHẬT TỒN KHO & GHI SỔ CÁI.');
      fetchReceipt();
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI HOÀN TẤT NHẬP KHO.');
    } finally {
      setIsCompleting(false);
    }
  };

  const handleCancel = async () => {
    if (!id || !cancelReason.trim()) return;
    setIsCancelling(true);
    try {
      await inventoryReceiptApi.cancel(Number(id), cancelReason.trim());
      showToast('success', 'ĐÃ HỦY PHIẾU NHẬP KHO!');
      setIsCancelModalOpen(false);
      setCancelReason('');
      fetchReceipt();
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'CÓ LỖI XẢY RA KHI HỦY PHIẾU NHẬP KHO.');
    } finally {
      setIsCancelling(false);
    }
  };

  const handleDelete = async () => {
    if (!id) return;
    setIsDeleting(true);
    try {
      await inventoryReceiptApi.delete(Number(id));
      showToast('success', 'XÓA PHIẾU NHẬP KHO THÀNH CÔNG');
      setIsDeleteModalOpen(false);
      setTimeout(() => navigate('/inventory-receipts'), 1000);
    } catch (error: any) {
      showToast('error', error.response?.data?.message || 'Không thể xóa phiếu nhập kho!');
    } finally {
      setIsDeleting(false);
    }
  };

  // --- RENDER ---
  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-slate-50/30 p-12">
        <Loader2 className="w-10 h-10 animate-spin text-indigo-500" />
      </div>
    );
  }

  if (!receipt)
    return <div className="p-12 text-center text-rose-500 font-bold">Không tìm thấy chứng từ!</div>;

  // Calculate totals
  const totalExpected = receipt.details?.reduce((acc, d) => acc + d.expectedQuantity, 0) || 0;
  const totalAccepted = receipt.details?.reduce((acc, d) => acc + d.acceptedQuantity, 0) || 0;
  const totalRejected = receipt.details?.reduce((acc, d) => acc + d.rejectedQuantity, 0) || 0;

  return (
    <DetailPageContainer>
      <Toast type={toast.type} message={toast.message} show={toast.show} />

      <DetailHeader
        icon={ClipboardCheck}
        title="Chi Tiết Phiếu Nhập Kho"
        subtitle={`Mã chứng từ: ${receipt.receiptCode}`}
        onBack={() => navigate('/inventory-receipts')}
      />

      {/* ================= THANH CÔNG CỤ (ACTION BUTTONS) ================= */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6 p-4 bg-white border border-slate-200 rounded-xl shadow-sm">
        <div className="flex items-center gap-3">
          <span className="text-sm font-bold text-slate-500">Trạng thái hiện tại:</span>
          <span
            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${InventoryReceiptStatusColors[receipt.status]}`}
          >
            {InventoryReceiptStatusLabels[receipt.status]}
          </span>
        </div>

        <div className="flex items-center gap-2">
          {/* NÚT IN PHIẾU NHẬP KHO */}
          <button
            type="button"
            onClick={() => setPrintModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 bg-white text-slate-700 border border-slate-200 rounded-lg font-bold text-xs hover:bg-slate-50 transition-colors shadow-xs cursor-pointer"
            title="In Phiếu Nhập Kho Nông Sản (GRN)"
          >
            In Phiếu Nhập Kho
          </button>

          {/* Chờ Nhập Kho hoặc Đang Kiểm Đếm đều có quyền Hoàn tất hoặc Hủy */}
          {(receipt.status === InventoryReceiptStatus.Pending ||
            receipt.status === InventoryReceiptStatus.Inspecting) && (
            <>
              {receipt.status === InventoryReceiptStatus.Pending && (
                <button
                  onClick={() => setIsDeleteModalOpen(true)}
                  disabled={isCancelling || isCompleting || isDeleting}
                  className="flex items-center gap-1.5 px-3 py-2 bg-rose-50 text-rose-600 border border-rose-200/80 rounded-lg font-bold text-xs hover:bg-rose-100 transition-colors cursor-pointer"
                >
                  <Trash2 size={15} /> Xóa Phiếu
                </button>
              )}
              <button
                onClick={() => {
                  setCancelReason('');
                  setIsCancelModalOpen(true);
                }}
                disabled={isCancelling || isCompleting || isDeleting}
                className="flex items-center gap-1.5 px-4 py-2 bg-white text-slate-600 border border-slate-300 rounded-lg font-bold text-xs hover:bg-slate-50 transition-colors cursor-pointer"
              >
                <XCircle size={15} /> Hủy Phiếu
              </button>

              <button
                onClick={handleComplete}
                disabled={isCompleting || isCancelling || isDeleting}
                className="flex items-center gap-2 px-5 py-2 bg-emerald-600 text-white rounded-lg font-bold text-xs hover:bg-emerald-700 transition-all shadow-sm shadow-emerald-200 disabled:opacity-50 cursor-pointer"
              >
                {isCompleting ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <CheckCircle size={15} />
                )}
                Hoàn Tất Nhập Kho
              </button>
            </>
          )}
        </div>
      </div>

      {/* ================= TABS ĐIỀU HƯỚNG ================= */}
      <TabGroup>
        <TabButton
          active={activeTab === 'info'}
          onClick={() => setActiveTab('info')}
          label="1. THÔNG TIN PHIẾU"
          icon={FileText}
        />
        <TabButton
          active={activeTab === 'details'}
          onClick={() => setActiveTab('details')}
          label={`2. CHI TIẾT KIỂM ĐẾM (${receipt.details?.length || 0})`}
          icon={Package}
        />
      </TabGroup>

      {/* ================= TAB 1: THÔNG TIN PHIẾU ================= */}
      <DetailCard>
        {activeTab === 'info' && (
          <div className="p-8 flex flex-col gap-8 animate-in fade-in duration-300">
            {receipt.status === InventoryReceiptStatus.Cancelled && receipt.cancellationReason && (
              <div className="bg-rose-50 border border-rose-200 rounded-xl p-4 flex gap-3 items-start shadow-sm">
                <AlertCircle className="w-5 h-5 shrink-0 mt-0.5 text-rose-500" />
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-bold text-rose-800 uppercase tracking-wider">
                    Lý do hủy phiếu:
                  </span>
                  <span className="text-sm font-medium text-rose-700">
                    {receipt.cancellationReason}
                  </span>
                </div>
              </div>
            )}

            <DetailSection title="Thông Tin Chung" dotColor="bg-indigo-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Mã phiếu nhập"
                  value={<span className="font-bold text-indigo-700">{receipt.receiptCode}</span>}
                />
                <InfoField
                  label="Kho nhận hàng"
                  value={<span className="font-bold text-slate-800">{receipt.warehouseName}</span>}
                />
                <InfoField label="Nhà cung cấp" value={receipt.supplierName || '---'} />
                <InfoField label="Thủ kho kiểm đếm" value={receipt.receivedByName || '---'} />

                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Ngày Nhận Hàng
                  </span>
                  <DateCell isoString={receipt.receiptDate} />
                </div>
                <div className="flex flex-col items-start">
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Thời Gian Lập Phiếu
                  </span>
                  <DateTimeCell isoString={receipt.createdAt} />
                </div>
              </div>
            </DetailSection>

            <DetailSection title="Ghi Chú Kiểm Đếm" dotColor="bg-slate-400">
              <div className="text-sm text-slate-600 leading-relaxed bg-slate-50/70 p-5 rounded-2xl border border-slate-100 min-h-24">
                {receipt.note || (
                  <span className="italic text-slate-400 font-medium">
                    Không có ghi chú nào được ghi nhận.
                  </span>
                )}
              </div>
            </DetailSection>
          </div>
        )}

        {/* ================= TAB 2: CHI TIẾT KIỂM ĐẾM ================= */}
        {activeTab === 'details' && (
          <div className="p-8 flex flex-col gap-6 animate-in fade-in duration-300">
            <div className="overflow-x-auto border border-slate-200 rounded-xl shadow-sm">
              <table className="w-full text-left whitespace-nowrap min-w-[900px]">
                <thead>
                  <tr className="bg-slate-50/80 border-b border-slate-200">
                    <th className="w-12 py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                      #
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      Mã SKU
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      Tên SP
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      Mã Lô
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                      ĐVT
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right bg-slate-100/50">
                      SL Dự Kiến
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-emerald-600 uppercase tracking-wider text-right bg-emerald-50/50">
                      SL Đạt ✅
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-rose-600 uppercase tracking-wider text-right bg-rose-50/50">
                      Từ Chối ❌
                    </th>
                    <th className="py-4 px-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-left min-w-50">
                      Lý Do Từ Chối
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {receipt.details && receipt.details.length > 0 ? (
                    receipt.details.map((detail, index) => (
                      <tr
                        key={detail.id || index}
                        className="hover:bg-slate-50 transition-colors group"
                      >
                        <td className="py-4 px-4 text-sm text-slate-400 text-center font-medium">
                          {index + 1}
                        </td>
                        <td className="py-4 px-4 text-sm font-bold text-slate-700">
                          {detail.variantCode || '---'}
                        </td>
                        <td className="py-4 px-4 text-sm font-medium text-slate-900">
                          {detail.variantName}
                        </td>
                        <td className="py-4 px-4 text-sm font-bold text-indigo-600">
                          {detail.batchCode || '---'}
                        </td>
                        <td className="py-4 px-4 text-sm text-slate-600 text-center">
                          {detail.uoMName}
                        </td>

                        <td className="py-4 px-4 text-sm font-medium text-slate-700 text-right bg-slate-50/50 border-l border-slate-100">
                          {detail.expectedQuantity.toLocaleString('vi-VN')}
                        </td>
                        <td className="py-4 px-4 text-[15px] font-black text-emerald-600 text-right bg-emerald-50/30 border-l border-emerald-100">
                          {detail.acceptedQuantity.toLocaleString('vi-VN')}
                        </td>
                        <td className="py-4 px-4 text-[15px] font-black text-rose-600 text-right bg-rose-50/30 border-l border-rose-100">
                          {detail.rejectedQuantity.toLocaleString('vi-VN')}
                        </td>

                        <td
                          className="py-4 px-4 text-sm text-slate-600 truncate max-w-62.5 border-l border-slate-100"
                          title={detail.rejectReason}
                        >
                          {detail.rejectReason || (
                            <span className="italic text-slate-400">---</span>
                          )}
                        </td>
                      </tr>
                    ))
                  ) : (
                    <TableEmpty colSpan={9} message="Không có chi tiết sản phẩm nào." />
                  )}
                </tbody>
                {receipt.details && receipt.details.length > 0 && (
                  <tfoot className="bg-slate-50 border-t border-slate-200">
                    <tr>
                      <td
                        colSpan={5}
                        className="py-4 px-4 text-right text-xs font-bold text-slate-500 uppercase tracking-wider"
                      >
                        Tổng Số Lượng:
                      </td>
                      <td className="py-4 px-4 text-right text-[15px] font-black text-slate-700">
                        {totalExpected.toLocaleString('vi-VN')}
                      </td>
                      <td className="py-4 px-4 text-right text-[16px] font-black text-emerald-600">
                        {totalAccepted.toLocaleString('vi-VN')}
                      </td>
                      <td className="py-4 px-4 text-right text-[16px] font-black text-rose-600">
                        {totalRejected.toLocaleString('vi-VN')}
                      </td>
                      <td></td>
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>
          </div>
        )}
      </DetailCard>

      {/* ================= MODAL HỦY PHIẾU ================= */}
      {isCancelModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="px-6 py-4 border-b border-slate-100 bg-rose-50/30">
              <h3 className="text-lg font-black text-rose-700 flex items-center gap-2">
                <AlertCircle size={20} /> Xác Nhận Hủy Phiếu Nhập
              </h3>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                Hành động này sẽ đánh dấu phiếu nhập kho{' '}
                <strong className="text-slate-900 bg-slate-100 px-1 rounded">
                  {receipt.receiptCode}
                </strong>{' '}
                thành Đã Hủy. Hệ thống yêu cầu ghi rõ lý do.
              </p>
              <div>
                <label className="block text-sm font-bold text-slate-700 mb-2">
                  Lý do hủy <span className="text-rose-500">*</span>
                </label>
                <textarea
                  value={cancelReason}
                  onChange={(e) => setCancelReason(e.target.value)}
                  placeholder="Ví dụ: Xe quay đầu, sai lệch quá nhiều, lập nhầm phiếu..."
                  className="w-full border border-slate-300 rounded-xl p-3 text-sm focus:ring-2 focus:ring-rose-500 focus:border-rose-500 outline-none min-h-24 resize-none"
                  autoFocus
                />
              </div>
            </div>
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-100 flex justify-end gap-3">
              <button
                onClick={() => {
                  setIsCancelModalOpen(false);
                  setCancelReason('');
                }}
                className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors cursor-pointer"
                disabled={isCancelling}
              >
                Đóng Lại
              </button>
              <button
                onClick={handleCancel}
                disabled={!cancelReason.trim() || isCancelling}
                className="px-5 py-2 text-sm font-bold text-white bg-rose-600 rounded-lg hover:bg-rose-700 disabled:opacity-50 shadow-sm flex items-center gap-2 transition-colors cursor-pointer"
              >
                {isCancelling ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                Xác Nhận Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {receipt && (
        <DocumentPrintModal
          isOpen={printModalOpen}
          onClose={() => setPrintModalOpen(false)}
          documentTitle="PHIẾU NHẬP KHO NÔNG SẢN (GRN)"
          documentSubtitle="Hệ thống tiếp nhận, kiểm định chất lượng & nhập kho Solaris"
          documentCode={receipt.receiptCode}
          documentDate={receipt.receiptDate || receipt.createdAt}
          warehouseName={receipt.warehouseName}
          creatorName={receipt.receivedByName}
          partyTitle="Nhà cung cấp / Đối tác"
          partyName={receipt.supplierName || 'Nội bộ / Nhập kho'}
          referenceCode={receipt.poCode || receipt.receiptCode}
          notes={
            receipt.note ||
            'Hàng nhập đã qua kiểm đếm cân trọng lượng và kiểm tra tiêu chuẩn cảm quan.'
          }
          items={(receipt.details || []).map((d) => ({
            skuCode: d.variantCode,
            productName: d.variantName,
            batchCode: d.batchCode,
            uoMName: d.uoMName || 'Kg',
            quantity: d.acceptedQuantity > 0 ? d.acceptedQuantity : d.expectedQuantity,
            qcStatus:
              d.rejectedQuantity > 0
                ? `Nhận ${d.acceptedQuantity} / Loại ${d.rejectedQuantity}`
                : 'Đạt chuẩn 100%',
            note:
              d.rejectReason || (d.actualWeightKg ? `Cân nặng: ${d.actualWeightKg} kg` : undefined),
          }))}
          signatures={[
            {
              title: 'Thủ Kho Tiếp Nhận',
              subtitle: '(Ký, ghi rõ họ tên)',
              name: receipt.receivedByName,
            },
            { title: 'KCS / Kiểm Định Viên', subtitle: '(Ký, đánh giá cảm quan)' },
            { title: 'Người Giao Hàng', subtitle: '(Ký, xác nhận bàn giao)' },
          ]}
        />
      )}

      {/* ================= MODAL XÓA PHIẾU ================= */}
      <ConfirmDeleteModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={handleDelete}
        loading={isDeleting}
        title="Xóa Phiếu Nhập Kho"
        message={`Bạn có chắc chắn muốn xóa phiếu nhập kho "${receipt.receiptCode}" không? Thao tác này không thể hoàn tác.`}
      />
    </DetailPageContainer>
  );
};

export default InventoryReceiptDetail;
