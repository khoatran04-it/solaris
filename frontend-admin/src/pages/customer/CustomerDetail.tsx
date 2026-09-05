import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Users,
  MapPin,
  History,
  Hexagon,
  Loader2,
  User,
  Plus,
  Star,
  Edit3,
  Trash2,
  ShoppingBag,
  Award,
  Sparkles,
  TrendingUp,
  CreditCard,
  Eye,
} from 'lucide-react';

// API & Types
import { customerApi } from '../../api/customerApi';
import { customerAddressApi } from '../../api/customerAddressApi';
import { orderApi } from '../../api/orderApi';
import { Customer } from '../../types/customer';
import {
  Order,
  OrderStatus,
  OrderStatusLabels,
  OrderStatusColors,
  PaymentStatus,
  PaymentStatusLabels,
  PaymentStatusColors,
  PaymentMethod,
  PaymentMethodLabels,
} from '../../types/order';

// Components
import { Toast } from '../../components/commons/Toast';
import { ModalAddAddress } from '../../components/modals/ModalAddAddress';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';
import {
  DetailPageContainer,
  DetailHeader,
  TabGroup,
  TabButton,
  DetailCard,
  DetailSection,
  InfoField,
  DetailProfileCard,
  TabEmptyPlaceholder,
} from '../../components/commons/TabUI';
import { DateTimeCell, TableEmpty } from '../../components/commons/ListUI';

type TabType = 'detail' | 'addresses' | 'history';

const CustomerDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // --- STATES DỮ LIỆU ---
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingOrders, setLoadingOrders] = useState(false);
  const [activeTab, setActiveTab] = useState<TabType>('detail');
  const [toast, setToast] = useState<{ show: boolean; type: 'error' | 'success'; message: string }>(
    { show: false, type: 'error', message: '' }
  );

  // --- STATES CHO MODAL ĐỊA CHỈ ---
  const [isAddressModalOpen, setIsAddressModalOpen] = useState(false);
  const [editingAddress, setEditingAddress] = useState<CustomerAddressPayload | null>(null);
  const [editingAddressId, setEditingAddressId] = useState<number | null>(null);

  // --- STATES CHO MODAL XÓA ---
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deletingAddressId, setDeletingAddressId] = useState<number | null>(null);

  // --- EFFECT ---
  const fetchCustomer = () => {
    setLoading(true);
    customerApi
      .getById(Number(id))
      .then((res) => setCustomer(res))
      .catch(() => showToast('error', 'Không thể tải thông tin khách hàng!'))
      .finally(() => setLoading(false));
  };

  const fetchCustomerOrders = (customerId: number) => {
    setLoadingOrders(true);
    orderApi
      .getAll({ customerId, pageSize: 50 })
      .then((res) => setOrders(res.items || []))
      .catch(() => console.error('Không thể tải lịch sử đơn hàng!'))
      .finally(() => setLoadingOrders(false));
  };

  useEffect(() => {
    if (id) {
      fetchCustomer();
      fetchCustomerOrders(Number(id));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const showToast = (type: 'success' | 'error', message: string) => {
    setToast({ show: true, type, message });
    setTimeout(() => setToast((prev) => ({ ...prev, show: false })), 3000);
  };

  // --- HANDLERS: ĐỊA CHỈ ---
  const openAddAddressModal = () => {
    setEditingAddress(null);
    setEditingAddressId(null);
    setIsAddressModalOpen(true);
  };

  const openEditAddressModal = (address: CustomerAddress) => {
    setEditingAddressId(address.id);
    setEditingAddress({
      receiverName: address.receiverName,
      phone: address.phone,
      province: address.province,
      district: address.district || '',
      ward: address.ward || '',
      streetAddress: address.streetAddress,
      isDefault: address.isDefault,
    });
    setIsAddressModalOpen(true);
  };

  const handleSaveAddress = async (payload: CustomerAddressPayload) => {
    try {
      if (editingAddressId) {
        await customerAddressApi.update(editingAddressId, payload);
        showToast('success', 'Cập nhật địa chỉ thành công!');
      } else {
        await customerAddressApi.create(Number(id), payload);
        showToast('success', 'Thêm địa chỉ mới thành công!');
      }
      fetchCustomer(); // Reload lại danh sách sau khi lưu
    } catch (error) {
      showToast('error', 'Có lỗi xảy ra khi lưu địa chỉ.');
      throw error; // Ném lỗi lại cho Modal để nó không tự đóng
    }
  };

  const handleSetDefault = async (addressId: number) => {
    try {
      await customerAddressApi.setDefault(addressId, Number(id));
      showToast('success', 'Đã thay đổi địa chỉ mặc định!');
      fetchCustomer();
    } catch (error) {
      showToast('error', 'Lỗi khi thiết lập mặc định.');
    }
  };

  const confirmDeleteAddress = async () => {
    if (!deletingAddressId) return;
    try {
      await customerAddressApi.delete(deletingAddressId);
      setIsDeleteModalOpen(false);
      showToast('success', 'Đã xóa địa chỉ thành công!');
      fetchCustomer();
    } catch (error) {
      showToast('error', 'Không thể xóa địa chỉ này.');
    }
  };

  // --- RENDER ---
  if (loading) {
    return (
      <div className="h-full flex items-center justify-center bg-slate-50/30">
        <Loader2 className="w-10 h-10 animate-spin text-yellow-500" />
      </div>
    );
  }

  if (!customer) return null;

  return (
    <DetailPageContainer>
      <Toast {...toast} />
      <ModalAddAddress
        isOpen={isAddressModalOpen}
        onClose={() => setIsAddressModalOpen(false)}
        onSave={handleSaveAddress}
        initialData={editingAddress}
      />
      <ConfirmDeleteModal
        isOpen={isDeleteModalOpen}
        itemName="địa chỉ này"
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={confirmDeleteAddress}
      />

      <DetailHeader
        icon={Hexagon}
        title="Hồ Sơ Khách Hàng"
        subtitle={
          <>
            Mã hệ thống: <span className="text-slate-800 font-bold">{customer.code}</span>
          </>
        }
        onBack={() => navigate('/customers')}
      />

      <TabGroup>
        <TabButton
          active={activeTab === 'detail'}
          onClick={() => setActiveTab('detail')}
          label="THÔNG TIN HỒ SƠ"
          icon={Users}
        />
        <TabButton
          active={activeTab === 'addresses'}
          onClick={() => setActiveTab('addresses')}
          label="SỔ ĐỊA CHỈ GIAO HÀNG"
          icon={MapPin}
        />
        <TabButton
          active={activeTab === 'history'}
          onClick={() => setActiveTab('history')}
          label="LỊCH SỬ MUA HÀNG"
          icon={History}
        />
      </TabGroup>

      <DetailCard>
        {/* ================= TAB 1: THÔNG TIN CHI TIẾT (Giữ nguyên) ================= */}
        {activeTab === 'detail' && (
          <div className="p-8 grid grid-cols-1 lg:grid-cols-3 gap-10 items-start">
            <div className="lg:col-span-2 flex flex-col gap-9">
              <DetailSection title="Tổng Quan Cá Nhân" dotColor="bg-yellow-400">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-y-5 gap-x-8">
                  {/* Widget Tiến độ Hạng Thành viên & Tổng tiền đã thanh toán */}
                  <div className="md:col-span-2 bg-gradient-to-br from-amber-500/10 via-yellow-500/5 to-emerald-500/10 rounded-2xl p-5 border border-amber-200/60 shadow-xs space-y-4">
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                      <div className="flex items-center gap-3.5">
                        <div className="w-12 h-12 rounded-2xl bg-amber-500 text-white flex items-center justify-center shadow-md shadow-amber-500/20 shrink-0">
                          <Award size={24} />
                        </div>
                        <div>
                          <div className="flex items-center gap-2 flex-wrap">
                            <span className="text-xs font-bold uppercase tracking-wider text-amber-900">
                              Hạng Thành Viên:
                            </span>
                            <span className="px-2.5 py-0.5 rounded-lg bg-amber-500 text-white text-xs font-black shadow-xs">
                              {customer.customerTierName || 'Thành Viên'}
                            </span>
                            {customer.discountPercent && customer.discountPercent > 0 ? (
                              <span className="text-xs font-bold text-emerald-700 bg-emerald-100/80 px-2 py-0.5 rounded-md border border-emerald-200/50">
                                Chiết khấu -{customer.discountPercent}%
                              </span>
                            ) : null}
                          </div>
                          <p className="text-xs text-slate-600 mt-1.5 leading-relaxed">
                            {customer.nextTierName ? (
                              <>
                                Mua thêm{' '}
                                <strong className="text-amber-700 font-black">
                                  {(customer.amountToNextTier || 0).toLocaleString('vi-VN')} đ
                                </strong>{' '}
                                để thăng cấp lên bậc{' '}
                                <strong className="text-amber-800 font-black">
                                  {customer.nextTierName}
                                </strong>
                              </>
                            ) : (
                              <span className="text-emerald-700 font-bold flex items-center gap-1">
                                <Sparkles size={14} className="text-amber-500" /> Khách hàng đã đạt
                                thứ hạng thành viên cao nhất (VIP)!
                              </span>
                            )}
                          </p>
                        </div>
                      </div>

                      {/* Badge Tổng thanh toán */}
                      <div className="flex items-center gap-4 sm:border-l sm:border-amber-200 sm:pl-6 shrink-0">
                        <div>
                          <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500 block">
                            Tổng Đã Thanh Toán
                          </span>
                          <span className="text-xl font-black text-emerald-700">
                            {(customer.totalSpent || 0).toLocaleString('vi-VN')} đ
                          </span>
                        </div>
                        <div className="text-right">
                          <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500 block">
                            Đơn Hàng
                          </span>
                          <span className="text-base font-black text-slate-800">
                            {customer.totalOrders || 0} đơn
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Thanh tiến độ Loyalty Progress Bar */}
                    <div className="space-y-1.5 pt-1">
                      <div className="flex items-center justify-between text-xs font-bold text-slate-700">
                        <span className="text-amber-800 font-extrabold">
                          {customer.customerTierName || 'Thành Viên'}
                        </span>
                        <span className="text-emerald-700 font-black">
                          {customer.tierProgressPercent || 0}%
                        </span>
                        <span className="text-slate-500">
                          {customer.nextTierName
                            ? `Mục tiêu: ${customer.nextTierName}`
                            : '🌟 VIP Tối Đa'}
                        </span>
                      </div>
                      <div className="w-full h-3 bg-amber-200/50 rounded-full overflow-hidden p-0.5 border border-amber-200">
                        <div
                          className="h-full bg-gradient-to-r from-amber-500 to-emerald-500 rounded-full transition-all duration-500 shadow-xs"
                          style={{
                            width: `${Math.min(100, Math.max(0, customer.tierProgressPercent || 0))}%`,
                          }}
                        />
                      </div>
                    </div>
                  </div>

                  <InfoField label="Tên khách hàng" value={customer.name} />
                  <InfoField
                    label="Giới tính"
                    value={
                      customer.gender === true
                        ? 'Nam'
                        : customer.gender === false
                          ? 'Nữ'
                          : 'Chưa cập nhật'
                    }
                  />
                  <InfoField
                    label="Ngày sinh"
                    value={
                      customer.birthday
                        ? new Date(customer.birthday).toLocaleDateString('vi-VN')
                        : '---'
                    }
                  />
                  <InfoField label="Mã số thuế" value={customer.taxCode || '---'} />
                  <div className="md:col-span-2 grid grid-cols-2 border-l-2 border-slate-100 pl-4 py-0.5 gap-2 mt-2">
                    <div className="flex flex-col items-start">
                      <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                        Ngày Khởi Tạo
                      </span>
                      <DateTimeCell isoString={customer.createdAt} />
                    </div>
                    <div className="flex flex-col items-start">
                      <span className="text-[11px] font-bold text-slate-400 uppercase mb-1">
                        Cập Nhật Gần Nhất
                      </span>
                      <DateTimeCell isoString={customer.updatedAt} />
                    </div>
                  </div>
                </div>
              </DetailSection>

              <DetailSection title="Thông Tin Liên Hệ" dotColor="bg-blue-400">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-y-5 gap-x-8">
                  <InfoField label="Số điện thoại di động" value={customer.phoneNumber} />
                  <InfoField label="Địa chỉ thư điện tử (Email)" value={customer.email || '---'} />
                </div>
              </DetailSection>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                <DetailSection title="Định Danh Hệ Thống" dotColor="bg-purple-400">
                  <div className="space-y-4">
                    <InfoField
                      label="Loại Khách Hàng"
                      value={
                        <span className="inline-flex px-2.5 py-0.5 rounded-lg bg-indigo-50 text-indigo-700 text-xs font-bold border border-indigo-200/30">
                          {customer.customerTypeName}
                        </span>
                      }
                    />
                    <InfoField
                      label="Bậc Xếp Hạng"
                      value={
                        <span className="inline-flex px-2.5 py-0.5 rounded-lg bg-amber-50 text-amber-700 text-xs font-bold border border-amber-200/30">
                          {customer.customerTierName}
                        </span>
                      }
                    />
                    <div className="flex flex-col gap-1.5 pt-2">
                      <span className="text-[12px] font-bold text-slate-400 uppercase">
                        Nhóm Marketing
                      </span>
                      <div className="flex flex-wrap gap-1.5">
                        {customer.groups && customer.groups.length > 0 ? (
                          customer.groups.map((group, idx) => (
                            <span
                              key={idx}
                              className="inline-flex px-2.5 py-1 rounded-md text-[11px] font-semibold bg-blue-50 text-blue-600 border border-blue-100"
                            >
                              {group}
                            </span>
                          ))
                        ) : (
                          <span className="text-sm italic text-slate-400">Chưa gắn nhóm.</span>
                        )}
                      </div>
                    </div>
                  </div>
                </DetailSection>
                <DetailSection title="Ghi Chú Nội Bộ" dotColor="bg-slate-400">
                  <p className="text-sm text-slate-600 leading-relaxed bg-slate-50/70 p-4 rounded-2xl border border-slate-100 min-h-35">
                    {customer.note || (
                      <span className="italic text-slate-400 font-normal">
                        Không có ghi chú đặc biệt cho khách hàng này.
                      </span>
                    )}
                  </p>
                </DetailSection>
              </div>
            </div>

            <div className="lg:col-span-1">
              <DetailProfileCard
                title={customer.name}
                subTitle={customer.code}
                logoPath={customer.avatarPath}
                fallbackIcon={User}
                statusActive={customer.isActive}
              />
            </div>
          </div>
        )}

        {/* ================= TAB 2: SỔ ĐỊA CHỈ (Mới) ================= */}
        {activeTab === 'addresses' && (
          <div className="p-8 flex flex-col gap-6">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-slate-800">Sổ Địa Chỉ Giao Hàng</h3>
                <p className="text-sm text-slate-500 mt-1">
                  Quản lý các điểm nhận hàng của khách hàng
                </p>
              </div>
              <button
                onClick={openAddAddressModal}
                className="flex items-center gap-2 px-4 py-2.5 text-sm font-bold text-slate-900 bg-yellow-400 rounded-xl hover:bg-yellow-500 shadow-sm shadow-yellow-200 transition-all"
              >
                <Plus size={18} strokeWidth={2.5} /> Thêm Địa Chỉ
              </button>
            </div>

            <div className="overflow-x-auto border border-slate-100 rounded-xl">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/70 border-b border-slate-100">
                    <th className="w-[10%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                      Mặc Định
                    </th>
                    <th className="w-[20%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      Người Nhận
                    </th>
                    <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      SĐT
                    </th>
                    <th className="w-[40%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">
                      Địa Chỉ Chi Tiết
                    </th>
                    <th className="w-[15%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">
                      Thao Tác
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {customer.addresses && customer.addresses.length > 0 ? (
                    customer.addresses.map((addr) => (
                      <tr
                        key={addr.id}
                        className={`transition-colors duration-200 group ${addr.isDefault ? 'bg-yellow-50/30' : 'hover:bg-slate-50/80'}`}
                      >
                        {/* Icon Mặc định (Ngôi sao) */}
                        <td className="py-4 px-6 text-center">
                          <button
                            onClick={() => !addr.isDefault && handleSetDefault(addr.id)}
                            disabled={addr.isDefault}
                            className={`p-2 rounded-full transition-all ${
                              addr.isDefault
                                ? 'text-yellow-500 bg-yellow-100 cursor-default'
                                : 'text-slate-300 hover:text-yellow-500 hover:bg-yellow-50'
                            }`}
                            title={addr.isDefault ? 'Đang là mặc định' : 'Đặt làm mặc định'}
                          >
                            <Star size={20} className={addr.isDefault ? 'fill-current' : ''} />
                          </button>
                        </td>

                        <td className="py-4 px-6 font-bold text-slate-700">{addr.receiverName}</td>
                        <td className="py-4 px-6 font-medium text-slate-600">{addr.phone}</td>

                        {/* Gộp địa chỉ đầy đủ */}
                        <td className="py-4 px-6 text-sm text-slate-600">
                          {addr.streetAddress}, {addr.ward ? `${addr.ward}, ` : ''}
                          {addr.district ? `${addr.district}, ` : ''}
                          {addr.province}
                        </td>

                        {/* Thao tác */}
                        <td className="py-4 px-6">
                          <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                            <button
                              onClick={() => openEditAddressModal(addr)}
                              className="p-1.5 text-slate-400 hover:text-yellow-600 hover:bg-yellow-50 rounded-lg transition-colors"
                              title="Chỉnh sửa"
                            >
                              <Edit3 size={17} strokeWidth={2.5} />
                            </button>
                            <button
                              onClick={() => {
                                setDeletingAddressId(addr.id);
                                setIsDeleteModalOpen(true);
                              }}
                              className="p-1.5 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                              title="Xóa"
                            >
                              <Trash2 size={17} strokeWidth={2.5} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  ) : (
                    <TableEmpty colSpan={5} message="Khách hàng này chưa có sổ địa chỉ." />
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* ================= TAB 3: LỊCH SỬ MUA HÀNG (Tích hợp thật) ================= */}
        {activeTab === 'history' && (
          <div className="p-8 flex flex-col gap-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h3 className="text-lg font-bold text-slate-800">Lịch Sử Mua Hàng & Đơn Hàng</h3>
                <p className="text-sm text-slate-500 mt-1">
                  Theo dõi danh sách đơn hàng và tổng chi tiêu thực tế của khách hàng
                </p>
              </div>

              <div className="flex items-center gap-3">
                <div className="px-4 py-2 bg-emerald-50 text-emerald-800 rounded-xl border border-emerald-200 text-xs font-bold flex items-center gap-2">
                  <CreditCard size={16} />
                  <span>
                    Đã thanh toán:{' '}
                    <strong>{(customer.totalSpent || 0).toLocaleString('vi-VN')} đ</strong>
                  </span>
                </div>
                <div className="px-4 py-2 bg-blue-50 text-blue-800 rounded-xl border border-blue-200 text-xs font-bold flex items-center gap-2">
                  <ShoppingBag size={16} />
                  <span>
                    Tổng đơn: <strong>{orders.length} đơn</strong>
                  </span>
                </div>
              </div>
            </div>

            {loadingOrders ? (
              <div className="py-16 flex items-center justify-center">
                <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
              </div>
            ) : orders.length > 0 ? (
              <div className="overflow-x-auto border border-slate-100 rounded-2xl shadow-2xs">
                <table className="w-full text-left text-sm whitespace-nowrap min-w-[850px]">
                  <thead className="bg-slate-50/80 text-slate-600 font-bold text-xs uppercase tracking-wider border-b border-slate-200">
                    <tr>
                      <th className="px-4 py-3.5">Mã Đơn Hàng</th>
                      <th className="px-4 py-3.5">Ngày Đặt</th>
                      <th className="px-4 py-3.5">Kho Xuất</th>
                      <th className="px-4 py-3.5">Thanh Toán</th>
                      <th className="px-4 py-3.5 text-center">Trạng Thái Đơn</th>
                      <th className="px-4 py-3.5 text-right">Tổng Tiền</th>
                      <th className="px-4 py-3.5 text-center w-20">Thao Tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 bg-white">
                    {orders.map((ord) => (
                      <tr key={ord.id} className="hover:bg-slate-50/80 transition-colors">
                        <td className="px-4 py-4">
                          <button
                            onClick={() => navigate(`/orders/${ord.id}`)}
                            className="font-bold text-emerald-700 hover:text-emerald-900 hover:underline flex items-center gap-1.5"
                          >
                            <ShoppingBag size={15} className="text-emerald-600" />
                            <span>{ord.orderCode}</span>
                          </button>
                          {ord.trackingCode && (
                            <span className="text-[10px] text-slate-400 block mt-0.5">
                              GHN: {ord.trackingCode}
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-4 text-xs text-slate-600">
                          <DateTimeCell isoString={ord.orderDate || ord.createdAt} />
                        </td>
                        <td className="px-4 py-4 text-xs font-semibold text-slate-700">
                          {ord.warehouseName || 'Chưa chỉ định'}
                        </td>
                        <td className="px-4 py-4 text-xs">
                          <span
                            className={`inline-flex items-center px-2.5 py-1 rounded-md font-bold text-[11px] border ${
                              PaymentStatusColors[ord.paymentStatus as PaymentStatus] ||
                              'bg-slate-100 text-slate-700 border-slate-200'
                            }`}
                          >
                            {PaymentStatusLabels[ord.paymentStatus as PaymentStatus] ||
                              'Chưa thanh toán'}
                          </span>
                          <span className="block text-[11px] text-slate-400 mt-1">
                            {PaymentMethodLabels[ord.paymentMethod as PaymentMethod] || 'COD'}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-center">
                          <span
                            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-bold border ${
                              OrderStatusColors[ord.status as OrderStatus] ||
                              'bg-slate-100 text-slate-700 border-slate-200'
                            }`}
                          >
                            {OrderStatusLabels[ord.status as OrderStatus] || 'Chờ xử lý'}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-right font-black text-slate-900 text-sm">
                          {ord.totalAmount?.toLocaleString('vi-VN')} đ
                        </td>
                        <td className="px-4 py-4 text-center">
                          <button
                            onClick={() => navigate(`/orders/${ord.id}`)}
                            className="p-1.5 bg-slate-100 hover:bg-emerald-50 text-slate-600 hover:text-emerald-700 rounded-lg transition-colors inline-flex items-center justify-center"
                            title="Xem chi tiết đơn hàng"
                          >
                            <Eye size={16} />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <TabEmptyPlaceholder
                icon={History}
                title="Lịch sử mua hàng trống"
                desc="Khách hàng này chưa phát sinh bất kỳ đơn hàng hay giao dịch nào trên hệ thống."
              />
            )}
          </div>
        )}
      </DetailCard>
    </DetailPageContainer>
  );
};

export default CustomerDetail;
