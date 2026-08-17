import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingBag, Eye, Trash2 } from 'lucide-react';
import {
    ListPageContainer,
    ListHeader,
    ListCard,
    TableLoading,
    TableEmpty,
    ListPagination,
    DateCell,
    DateTimeCell
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';
import { ConfirmDeleteModal } from '../../components/modals/ConfirmDeleteModal';

import { orderApi } from '../../api/orderApi';
import { customerApi } from '../../api/customerApi';
import { warehouseApi } from '../../api/warehouseApi';
import {
    Order,
    OrderStatus,
    OrderStatusLabels,
    OrderStatusColors,
    PaymentStatus,
    PaymentStatusLabels,
    PaymentStatusColors
} from '../../types/order';

const OrderList: React.FC = () => {
    const navigate = useNavigate();

    // --- STATES DỮ LIỆU ---
    const [orders, setOrders] = useState<Order[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [currentPage, setCurrentPage] = useState<number>(1);
    const [totalPages, setTotalPages] = useState<number>(0);
    const [totalItems, setTotalItems] = useState<number>(0);
    const pageSize = 10;

    // --- BỘ LỌC (FILTERS) ---
    const [searchTerm, setSearchTerm] = useState<string>('');
    const [debouncedSearch, setDebouncedSearch] = useState<string>('');
    const [customerFilter, setCustomerFilter] = useState<(string | number)[]>([]);
    const [warehouseFilter, setWarehouseFilter] = useState<(string | number)[]>([]);
    const [statusFilter, setStatusFilter] = useState<(string | number)[]>([]);
    const [paymentStatusFilter, setPaymentStatusFilter] = useState<(string | number)[]>([]);
    const [fromDateFilter, setFromDateFilter] = useState<Date | null>(null);
    const [toDateFilter, setToDateFilter] = useState<Date | null>(null);

    // --- STATE DELETE MODAL ---
    const [deleteId, setDeleteId] = useState<number | null>(null);
    const [deleteCode, setDeleteCode] = useState('');
    const [isDeleting, setIsDeleting] = useState(false);

    // --- OPTIONS CHO DROPDOWN ---
    const [customerOptions, setCustomerOptions] = useState<{ label: string; value: number }[]>([]);
    const [warehouseOptions, setWarehouseOptions] = useState<{ label: string; value: number }[]>([]);

    const statusOptions = Object.keys(OrderStatusLabels).map(key => ({
        label: OrderStatusLabels[Number(key) as OrderStatus],
        value: Number(key)
    }));

    const paymentStatusOptions = Object.keys(PaymentStatusLabels).map(key => ({
        label: PaymentStatusLabels[Number(key) as PaymentStatus],
        value: Number(key)
    }));

    // --- TOAST ---
    const [toast, setToast] = useState<{ show: boolean; type: 'success' | 'error'; message: string }>({
        show: false,
        type: 'success',
        message: '',
    });

    const showToast = (type: 'success' | 'error', message: string) => {
        setToast({ show: true, type, message });
        setTimeout(() => setToast(prev => ({ ...prev, show: false })), 3000);
    };

    // --- EFFECTS ---
    useEffect(() => {
        const loadOptions = async () => {
            try {
                const [custRes, whRes] = await Promise.all([
                    customerApi.getAllList().catch(() => []),
                    warehouseApi.getAllList().catch(() => []),
                ]);
                setCustomerOptions(custRes.map((c: any) => ({ label: `${c.code} - ${c.name}`, value: c.id })));
                setWarehouseOptions(whRes.map((w: any) => ({ label: w.name, value: w.id })));
            } catch (err) {
                console.error('Lỗi tải danh mục bổ trợ:', err);
            }
        };
        loadOptions();
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearch(searchTerm);
        }, 500);
        return () => clearTimeout(timer);
    }, [searchTerm]);

    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, customerFilter, warehouseFilter, statusFilter, paymentStatusFilter, fromDateFilter, toDateFilter]);

    const fetchOrders = useCallback(async () => {
        setLoading(true);
        try {
            const res = await orderApi.getAll({
                search: debouncedSearch || undefined,
                customerId: customerFilter.length > 0 ? Number(customerFilter[0]) : undefined,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                paymentStatus: paymentStatusFilter.length > 0 ? Number(paymentStatusFilter[0]) : undefined,
                startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined,
                pageIndex: currentPage,
                pageSize: pageSize,
            });

            setOrders(res.items || []);
            setTotalPages(res.totalPages || 0);
            setTotalItems(res.totalRecords || 0);
        } catch (err) {
            console.error('Lỗi tải danh sách đơn hàng:', err);
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DỮ LIỆU ĐƠN HÀNG');
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, customerFilter, warehouseFilter, statusFilter, paymentStatusFilter, fromDateFilter, toDateFilter, currentPage]);

    useEffect(() => {
        fetchOrders();
    }, [fetchOrders]);

    const handleDelete = async () => {
        if (!deleteId) return;
        try {
            setIsDeleting(true);
            await orderApi.delete(deleteId);
            showToast('success', 'XÓA ĐƠN HÀNG THÀNH CÔNG');
            setDeleteId(null);
            fetchOrders();
        } catch (error: any) {
            showToast('error', error.response?.data?.message || 'Không thể xóa đơn hàng!');
        } finally {
            setIsDeleting(false);
        }
    };

    const formatCurrency = (val: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val || 0);
    };

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                title="Đơn Bán Hàng"
                subtitle="Quản lý đơn đặt hàng từ khách hàng & Theo dõi tiến độ giao"
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/orders/create')}
                icon={ShoppingBag}
                searchPlaceholder="Tìm kiếm theo mã đơn, người nhận, SĐT..."
            />

            <ListCard>
                <div className="overflow-x-auto flex-1 min-h-[400px] pb-24">
                    <table className="w-full text-left border-collapse min-w-[1050px]">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[12%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã Đơn</th>
                                
                                <th className="w-[15%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHÁCH HÀNG" options={customerOptions} selectedValues={customerFilter} onApply={setCustomerFilter} />
                                </th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">Người Nhận</th>

                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHO XUẤT" options={warehouseOptions} selectedValues={warehouseFilter} onApply={setWarehouseFilter} />
                                </th>

                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="TỪ NGÀY" selectedDate={fromDateFilter} onApply={setFromDateFilter} />
                                    </div>
                                </th>

                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomDateFilter title="ĐẾN NGÀY" selectedDate={toDateFilter} onApply={setToDateFilter} />
                                    </div>
                                </th>

                                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>

                                <th className="w-[11%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="THANH TOÁN" options={paymentStatusOptions} selectedValues={paymentStatusFilter} onApply={setPaymentStatusFilter} />
                                    </div>
                                </th>

                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Tổng Tiền</th>

                                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>

                        <tbody className="divide-y divide-slate-100">
                            {loading ? (
                                <TableLoading colSpan={10} />
                            ) : orders.length === 0 ? (
                                <TableEmpty colSpan={10} message="Không tìm thấy đơn hàng nào phù hợp." />
                            ) : (
                                orders.map((order) => (
                                    <tr key={order.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        
                                        {/* CELL 1: MÃ ĐƠN */}
                                        <td className="py-3 px-6">
                                            <span className="inline-block px-2.5 py-1 bg-slate-100 text-slate-700 rounded-md font-bold text-[13px] border border-slate-200 shadow-sm">
                                                {order.orderCode}
                                            </span>
                                        </td>

                                        {/* CELL 2: KHÁCH HÀNG */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-bold text-slate-800">{order.customerName}</div>
                                            <div className="text-[11px] text-slate-400">{order.customerPhone}</div>
                                        </td>

                                        {/* CELL 3: NGƯỜI NHẬN */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-medium text-slate-700">{order.receiverName || '---'}</div>
                                            <div className="text-[11px] text-slate-400">{order.receiverPhone}</div>
                                        </td>

                                        {/* CELL 4: KHO XỬ LÝ */}
                                        <td className="py-3 px-2">
                                            {order.warehouseName ? (
                                                <span className="text-[13px] font-bold text-slate-600">{order.warehouseName}</span>
                                            ) : (
                                                <span className="text-[12px] italic text-slate-400">Chưa gán</span>
                                            )}
                                        </td>
                                        
                                        {/* CELL 5 & 6: NGÀY ĐẶT */}
                                        <td className="py-3 px-2 text-center" colSpan={2}>
                                            <DateCell isoString={order.orderDate} />
                                        </td>
                                        
                                        {/* CELL 7: TRẠNG THÁI ĐƠN */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${OrderStatusColors[order.status]}`}>
                                                    {OrderStatusLabels[order.status]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 8: THANH TOÁN */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${PaymentStatusColors[order.paymentStatus]}`}>
                                                    {PaymentStatusLabels[order.paymentStatus]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 9: TỔNG TIỀN */}
                                        <td className="py-3 px-2 text-right">
                                            <span className="text-[14px] font-black text-emerald-600">
                                                {formatCurrency(order.totalAmount)}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 10: THAO TÁC */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-60 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/orders/${order.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors cursor-pointer"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
                                                {(order.status === OrderStatus.Confirmed || order.status === OrderStatus.Cancelled) && (
                                                    <button
                                                        onClick={() => {
                                                            setDeleteId(order.id);
                                                            setDeleteCode(order.orderCode);
                                                        }}
                                                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors cursor-pointer"
                                                        title="Xóa đơn hàng"
                                                    >
                                                        <Trash2 size={17} strokeWidth={2.5} />
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                <ListPagination
                    currentPage={currentPage}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    onPageChange={setCurrentPage}
                    isLoading={loading}
                />
            </ListCard>

            <ConfirmDeleteModal
                isOpen={Boolean(deleteId)}
                onClose={() => setDeleteId(null)}
                onConfirm={handleDelete}
                loading={isDeleting}
                title="Xóa Đơn Bán Hàng"
                message={`Bạn có chắc chắn muốn xóa đơn hàng "${deleteCode}" không? Thao tác này không thể hoàn tác.`}
            />
        </ListPageContainer>
    );
};

export default OrderList;