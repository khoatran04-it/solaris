import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  PackageCheck,
  FileText,
  Package,
  CheckCircle,
  XCircle,
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

import { inventoryIssueApi } from '../../api/inventoryIssueApi';
import { orderApi } from '../../api/orderApi';
import { Order } from '../../types/order';
import {
  InventoryIssue,
  InventoryIssueStatus,
  InventoryIssueStatusLabels,
  InventoryIssueStatusColors,
} from '../../types/inventoryIssue';
import { DocumentPrintModal } from '../../components/commons/DocumentPrintModal';

const InventoryIssueDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [issue, setIssue] = useState<InventoryIssue | null>(null);
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'info' | 'items'>('info');

  // Actions
  const [actionLoading, setActionLoading] = useState(false);
  const [ghnLoading, setGhnLoading] = useState(false);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [printModalOpen, setPrintModalOpen] = useState(false);

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

  const fetchIssue = useCallback(async () => {
    if (!id) return;
    try {
      setLoading(true);
      const data = await inventoryIssueApi.getById(Number(id));
      setIssue(data);
      if (data.orderId) {
        try {
          const ord = await orderApi.getById(data.orderId);
          setOrder(ord);
        } catch (oErr) {
          console.error('Không thể tải đơn hàng liên kết:', oErr);
        }
      }
    } catch (error) {
      console.error('Error fetching issue:', error);
      showToast('error', 'KHÔNG THỂ TẢI DỮ LIỆU PHIẾU XUẤT KHO');
    } finally {
      setLoading(false);
    }
  }, [id]);

  const handleCreateGhn = async () => {
    if (!issue?.orderId) {
      showToast('warning', 'Phiếu xuất kho không liên kết với đơn bán hàng nào.');
      return;
    }
    try {
      setGhnLoading(true);
      const res = await orderApi.createGhnOrder(issue.orderId);
      showToast('success', `ĐÃ TẠO VẬN ĐƠN GHN: ${res.orderCode}`);
      fetchIssue();
    } catch (err: any) {
      showToast('error', err.response?.data?.message || 'Lỗi đẩy đơn sang GHN');
    } finally {
      setGhnLoading(false);
    }
  };

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

  if (!issue)
    return (
      <div className="p-12 text-center text-rose-500 font-bold">
        Không tìm thấy chứng từ xuất kho!
      </div>
    );

  const totalAmount =
    issue.details?.reduce((sum, item) => sum + (Number(item.totalPrice) || 0), 0) || 0;

  return (
    <DetailPageContainer>
      <Toast {...toast} />

      <DetailHeader
        title="Chi Tiết Phiếu Xuất Kho"
        subtitle={
          <>
            Mã chứng từ: <span className="font-bold text-slate-800">{issue.issueCode}</span>
          </>
        }
        onBack={() => navigate('/inventory-issues')}
        icon={PackageCheck}
      />

      {/* ================= THANH CÔNG CỤ (ACTION TOOLBAR) ================= */}
      <div className="flex flex-col gap-3 mb-6">
        <div className="flex flex-wrap items-center justify-between gap-3 p-4 bg-white border border-slate-200 rounded-xl shadow-xs">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <span className="text-[13px] font-bold text-slate-500 uppercase tracking-wide">
                Trạng thái:
              </span>
              <span
                className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${InventoryIssueStatusColors[issue.status]}`}
              >
                {InventoryIssueStatusLabels[issue.status]}
              </span>
            </div>

            {/* Vách ngăn nếu có nút tiếp theo */}
            <div className="h-6 w-px bg-slate-200 mx-2 hidden md:block"></div>

            {/* CÁC NÚT KHI ĐANG XỬ LÝ / CHỜ XUẤT */}
            {(issue.status === InventoryIssueStatus.Pending ||
              issue.status === InventoryIssueStatus.Picking) && (
              <button
                onClick={handleCompleteIssue}
                disabled={actionLoading}
                className="px-5 py-2 bg-emerald-600 text-white rounded-lg font-bold text-sm hover:bg-emerald-700 transition-all shadow-xs disabled:opacity-50"
              >
                {actionLoading ? 'Đang xử lý...' : 'Hoàn Tất Xuất Kho'}
              </button>
            )}

            {/* CÁC NÚT GIAO VẬN KHI ĐÃ HOÀN TẤT XUẤT KHO (3 NÚT TEXT THUẦN THEME SOLARIS) */}
            {issue.status === InventoryIssueStatus.Completed && (
              <div className="flex items-center gap-2.5 flex-wrap">
                {/* Nút 1: Tạo Đơn GHN (Khóa mờ nếu là hàng chuỗi lạnh) */}
                {order?.requiresColdChain ? (
                  <button
                    disabled
                    title="Đơn hàng có chứa đồ tươi sống/cấp đông, GHN không hỗ trợ bảo quản lạnh!"
                    className="px-4 py-2 bg-slate-100 text-slate-400 border border-slate-200 rounded-lg font-bold text-sm cursor-not-allowed opacity-60"
                  >
                    Tạo Đơn GHN (Không hỗ trợ hàng lạnh)
                  </button>
                ) : (
                  <button
                    onClick={handleCreateGhn}
                    disabled={ghnLoading || !!order?.trackingCode || !!order?.deliveryTripId}
                    className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-sm shadow-xs transition-all disabled:opacity-50"
                  >
                    {ghnLoading
                      ? 'Đang tạo đơn...'
                      : order?.deliveryTripId ||
                          order?.shippingProvider === 'Internal' ||
                          order?.trackingCode?.startsWith('SLR-EXP')
                        ? 'Đã Điều Phối Xe Nội Bộ'
                        : order?.trackingCode
                          ? `Đã Tạo GHN (${order.trackingCode})`
                          : 'Tạo Đơn GHN'}
                  </button>
                )}

                {/* Nút 2: Điều Phối Xe Nội Bộ (Chạy qua Tab B2C) */}
                <button
                  onClick={() => {
                    navigate(
                      `/transportation/dashboard?tab=b2c&highlightOrderId=${issue.orderId || ''}`
                    );
                  }}
                  className="px-4 py-2 bg-amber-400 hover:bg-amber-500 text-slate-900 rounded-lg font-bold text-sm shadow-xs transition-all"
                >
                  Điều Phối Xe Giao Hàng
                </button>

                {/* Nút 3: In Phiếu Xuất & Tem Kiện (Web Print Preview A5/A6) */}
                <button
                  onClick={() => setPrintModalOpen(true)}
                  className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-lg font-bold text-sm shadow-xs transition-all"
                >
                  In Phiếu Xuất & Tem Kiện
                </button>
              </div>
            )}
          </div>

          {/* Nút Hủy Phiếu: Nằm góc phải khi chưa hoàn tất */}
          {(issue.status === InventoryIssueStatus.Pending ||
            issue.status === InventoryIssueStatus.Picking) && (
            <button
              onClick={() => setCancelModalOpen(true)}
              disabled={actionLoading}
              className="px-4 py-2 bg-white text-rose-600 border border-rose-200 rounded-lg font-bold text-sm hover:bg-rose-50 transition-colors shadow-xs"
            >
              Hủy Phiếu Xuất
            </button>
          )}
        </div>

        {/* THÔNG BÁO BẢO QUẢN CHUỖI LẠNH (NẾU CÓ) */}
        {issue.status === InventoryIssueStatus.Completed && order?.requiresColdChain && (
          <div className="p-3.5 bg-amber-50/80 border border-amber-200 rounded-xl text-xs text-amber-900 font-medium flex items-center justify-between">
            <div>
              <span className="font-bold uppercase tracking-wide mr-2 text-amber-950">
                Lưu ý bảo quản chuỗi lạnh:
              </span>
              Đơn hàng có chứa mặt hàng thực phẩm tươi sống / cấp đông. Hệ thống đã tự động khóa
              cổng giao hàng GHN để đảm bảo an toàn thực phẩm. Vui lòng sử dụng Đội Xe Máy Thùng
              Lạnh Nội Bộ tại Trung Tâm Vận Tải.
            </div>
            <span className="px-2.5 py-1 bg-amber-200/80 text-amber-950 font-bold rounded-lg shrink-0">
              Bảo quản 0°C đến 4°C
            </span>
          </div>
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
                  <h4 className="font-bold text-sm text-rose-800 uppercase tracking-wider">
                    Lý do hủy phiếu:
                  </h4>
                  <p className="text-sm text-rose-700 font-medium">{issue.cancellationReason}</p>
                </div>
              </div>
            )}

            <DetailSection title="Thông Tin Xuất Kho" dotColor="bg-indigo-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Kho xuất hàng"
                  value={<span className="font-bold text-indigo-700">{issue.warehouseName}</span>}
                />
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
                  <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                    Ngày xuất hàng
                  </span>
                  <DateCell isoString={issue.issueDate} />
                </div>
              </div>
            </DetailSection>

            <DetailSection title="Thông Tin Người Nhận" dotColor="bg-emerald-400">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-y-6 gap-x-8">
                <InfoField
                  label="Tên người nhận"
                  value={
                    <span className="font-bold text-slate-800">{issue.receiverName || '---'}</span>
                  }
                />
                <InfoField label="Số điện thoại" value={issue.receiverPhone || '---'} />
                <div className="md:col-span-2 lg:col-span-4">
                  <InfoField label="Địa chỉ giao hàng" value={issue.deliveryAddress || '---'} />
                </div>
              </div>

              {issue.note && (
                <div className="mt-6 pt-6 border-t border-slate-100">
                  <InfoField
                    label="Ghi chú xuất kho"
                    value={<span className="italic text-slate-600 font-medium">{issue.note}</span>}
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
                    <th className="px-4 py-4 text-center bg-emerald-50/50 min-w-30">
                      SL Thực Xuất
                    </th>
                    <th className="px-4 py-4 text-right min-w-30">Đơn Giá</th>
                    <th className="px-4 py-4 text-right min-w-37.5">Thành Tiền</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {issue.details?.map((item, idx) => (
                    <tr key={item.id || idx} className="hover:bg-slate-50/80 transition-colors">
                      <td className="px-4 py-3 text-center text-slate-400 font-medium">
                        {idx + 1}
                      </td>
                      <td className="px-4 py-3 font-bold text-slate-500">{item.variantCode}</td>
                      <td className="px-4 py-3 font-bold text-slate-800">{item.variantName}</td>
                      <td className="px-4 py-3 font-black text-indigo-600">{item.batchCode}</td>
                      <td className="px-4 py-3 text-center text-slate-600">{item.uoMName}</td>

                      {/* Cột Số Lượng Thực Xuất nổi bật */}
                      <td className="px-4 py-3 text-center border-l border-emerald-100 bg-emerald-50/20">
                        <span className="font-black text-[15px] text-emerald-700">
                          {item.quantity}
                        </span>
                      </td>

                      <td className="px-4 py-3 text-right font-medium text-slate-600 border-l border-slate-100">
                        {formatCurrency(item.unitPrice)}
                      </td>
                      <td className="px-4 py-3 text-right font-black text-slate-800">
                        {formatCurrency(item.totalPrice)}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-slate-50/80 border-t border-slate-200 text-sm">
                  <tr className="border-t border-slate-200 bg-emerald-50/30">
                    <td
                      colSpan={7}
                      className="px-4 py-4 text-right text-emerald-800 uppercase tracking-wider text-xs font-black"
                    >
                      Tổng Giá Trị Xuất Kho:
                    </td>
                    <td className="px-4 py-4 text-right text-[17px] font-black text-emerald-600">
                      {formatCurrency(totalAmount)}
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
              <h3 className="text-lg font-black text-rose-700">Xác Nhận Hủy Phiếu Xuất Kho</h3>
            </div>
            <div className="p-6">
              <p className="text-sm text-slate-600 mb-5 leading-relaxed">
                Bạn đang thực hiện hủy phiếu xuất kho{' '}
                <strong className="text-slate-900 bg-slate-100 px-1.5 py-0.5 rounded">
                  {issue.issueCode}
                </strong>
                .
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
                onClick={() => {
                  setCancelModalOpen(false);
                  setCancelReason('');
                }}
                disabled={actionLoading}
                className="px-5 py-2 text-sm font-bold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors shadow-xs"
              >
                Đóng Lại
              </button>
              <button
                onClick={handleCancelIssue}
                disabled={actionLoading || !cancelReason.trim()}
                className="px-5 py-2 bg-rose-600 text-white rounded-lg text-sm font-bold hover:bg-rose-700 transition-colors disabled:opacity-50 shadow-xs"
              >
                {actionLoading ? 'Đang hủy...' : 'Xác Nhận Hủy'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ================= MODAL IN PHIẾU XUẤT KHO (REUSABLE COMPONENT) ================= */}
      {issue && (
        <DocumentPrintModal
          isOpen={printModalOpen}
          onClose={() => setPrintModalOpen(false)}
          documentTitle="PHIẾU XUẤT KHO KIÊM BIÊN BẢN BÀN GIAO"
          documentSubtitle="Hệ thống chuỗi thực phẩm sạch & bảo quản chuỗi lạnh Solaris"
          documentCode={issue.issueCode}
          documentDate={issue.issueDate}
          warehouseName={issue.warehouseName}
          creatorName={issue.issuedByName}
          partyTitle="Người nhận hàng / Đơn vị nhận"
          partyName={issue.receiverName || 'Khách hàng'}
          partyPhone={issue.receiverPhone}
          partyAddress={issue.deliveryAddress}
          referenceCode={issue.orderCode || 'Xuất nội bộ'}
          paymentMethodName={
            order
              ? order.paymentMethod === 1
                ? 'Tiền mặt / COD'
                : 'Chuyển khoản / Điện tử'
              : undefined
          }
          paymentStatusName={
            order
              ? order.paymentStatus === 3
                ? 'Đã thanh toán'
                : 'Chưa thanh toán / Thu COD'
              : undefined
          }
          codAmount={
            order && order.paymentStatus !== 3 && order.paymentMethod === 1 ? order.totalAmount : 0
          }
          totalAmount={order?.totalAmount}
          notes={issue.note || 'Hàng thực phẩm tươi sạch - Giao nhanh đúng dải nhiệt độ'}
          items={(issue.details || []).map((item) => ({
            skuCode: item.variantCode,
            productName: item.variantName,
            batchCode: item.batchCode || 'Lô mặc định',
            uoMName: item.uoMName,
            quantity: item.quantity,
          }))}
          signatures={[
            {
              title: 'Thủ Kho Xuất Hàng',
              subtitle: '(Ký, ghi rõ họ tên)',
              name: issue.issuedByName,
            },
            {
              title: 'Người Kiểm Đếm',
              subtitle: '(Ký xác nhận)',
              name: issue.warehouseName
                ? `Kho: ${issue.warehouseName}`
                : '........................',
            },
            {
              title: 'Tài Xế Giao Hàng',
              subtitle: '(Ký nhận kiện hàng)',
              name: '........................',
            },
            {
              title: 'Người Nhận Hàng',
              subtitle: '(Kiểm tra và ký nhận)',
              name: issue.receiverName || '........................',
            },
          ]}
        />
      )}
    </DetailPageContainer>
  );
};

export default InventoryIssueDetail;
