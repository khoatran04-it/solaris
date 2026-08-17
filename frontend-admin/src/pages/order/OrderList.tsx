import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShoppingBag, Eye, Plus } from 'lucide-react';
import {
    ListPageContainer,
    ListHeader,
    ListCard,
    TableLoading,
    TableEmpty,
    ListPagination,
    DateTimeCell
} from '../../components/commons/ListUI';
import { CustomFilter } from '../../components/commons/CustomFilter';
import { CustomDateFilter } from '../../components/commons/CustomDateFilter';
import { Toast } from '../../components/commons/Toast';

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
                // Fail-safe chống sập chùm API
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

    // Đưa trang về 1 mỗi khi đổi filter
    useEffect(() => {
        setCurrentPage(1);
    }, [debouncedSearch, customerFilter, warehouseFilter, statusFilter, paymentStatusFilter, fromDateFilter, toDateFilter]);

    // --- FETCH DATA ---
    const fetchData = useCallback(async () => {
        try {
            setLoading(true);
            const response = await orderApi.getAll({
                search: debouncedSearch,
                pageIndex: currentPage,
                pageSize: pageSize,
                customerId: customerFilter.length > 0 ? Number(customerFilter[0]) : undefined,
                warehouseId: warehouseFilter.length > 0 ? Number(warehouseFilter[0]) : undefined,
                status: statusFilter.length > 0 ? Number(statusFilter[0]) : undefined,
                paymentStatus: paymentStatusFilter.length > 0 ? Number(paymentStatusFilter[0]) : undefined,
                startDate: fromDateFilter ? fromDateFilter.toLocaleDateString('en-CA') : undefined,
                endDate: toDateFilter ? toDateFilter.toLocaleDateString('en-CA') : undefined
            });

            setOrders(response.items || []);
            setTotalPages(response.totalPages || 0);
            setTotalItems(response.totalRecords || 0);
        } catch (error) {
            console.error('Lỗi tải dữ liệu đơn hàng:', error);
            showToast('error', 'CÓ LỖI XẢY RA KHI TẢI DANH SÁCH ĐƠN HÀNG!');
            setOrders([]);
        } finally {
            setLoading(false);
        }
    }, [debouncedSearch, currentPage, customerFilter, warehouseFilter, statusFilter, paymentStatusFilter, fromDateFilter, toDateFilter]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    const formatCurrency = (amount: number) => {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount || 0);
    };

    return (
        <ListPageContainer>
            <Toast {...toast} />

            <ListHeader
                title="Quản Lý Đơn Hàng"
                subtitle="Theo dõi và điều phối đơn đặt hàng của khách"
                icon={ShoppingBag}
                searchPlaceholder="Tìm mã đơn, tên, SĐT..."
                searchTerm={searchTerm}
                onSearchChange={setSearchTerm}
                onAdd={() => navigate('/orders/create')}
            />

            <ListCard>
                {/* --- BẢNG DỮ LIỆU CHUẨN CONVENTION --- */}
                <div className="overflow-x-auto flex-1 min-h-100 pb-24">
                    <table className="w-full text-left border-collapse min-w-300">
                        <thead>
                            <tr className="bg-slate-50/70 border-b border-slate-100">
                                <th className="w-[4%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">#</th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-left">Mã Đơn</th>
                                
                                <th className="w-[14%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHÁCH HÀNG" options={customerOptions} selectedValues={customerFilter} onApply={setCustomerFilter} />
                                </th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">Giao Hàng</th>
                                
                                <th className="w-[12%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <CustomFilter title="KHO XỬ LÝ" options={warehouseOptions} selectedValues={warehouseFilter} onApply={setWarehouseFilter} />
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
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="TRẠNG THÁI" options={statusOptions} selectedValues={statusFilter} onApply={setStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
                                    <div className="flex justify-center">
                                        <CustomFilter title="THANH TOÁN" options={paymentStatusOptions} selectedValues={paymentStatusFilter} onApply={setPaymentStatusFilter} />
                                    </div>
                                </th>
                                
                                <th className="w-[10%] py-4 px-2 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Tổng Tiền</th>
                                
                                <th className="w-[8%] py-4 px-6 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">Thao Tác</th>
                            </tr>
                        </thead>
                        
                        <tbody className="divide-y divide-slate-100">
                            {loading ? (
                                <TableLoading colSpan={11} />
                            ) : orders.length === 0 ? (
                                <TableEmpty colSpan={11} message="Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc." />
                            ) : (
                                orders.map((order, idx) => (
                                    <tr key={order.id} className="hover:bg-slate-50/80 transition-colors duration-200 group">
                                        {/* CELL 1: # */}
                                        <td className="py-3 px-6 text-center text-slate-400 text-xs">
                                            {(currentPage - 1) * pageSize + idx + 1}
                                        </td>
                                        
                                        {/* CELL 2: MÃ ĐƠN */}
                                        <td className="py-3 px-2">
                                            <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-[12px] font-bold bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-sm">
                                                {order.orderCode}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 3: KHÁCH HÀNG */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-bold text-slate-800 truncate max-w-45" title={order.customerName}>
                                                {order.customerName}
                                            </div>
                                        </td>
                                        
                                        {/* CELL 4: NGƯỜI NHẬN / SĐT */}
                                        <td className="py-3 px-2">
                                            <div className="text-[13px] font-medium text-slate-700">{order.receiverName || '---'}</div>
                                            <div className="text-[11px] font-bold text-slate-400">{order.receiverPhone}</div>
                                        </td>
                                        
                                        {/* CELL 5: KHO XỬ LÝ */}
                                        <td className="py-3 px-2">
                                            {order.warehouseName ? (
                                                <span className="text-[13px] font-bold text-slate-600">{order.warehouseName}</span>
                                            ) : (
                                                <span className="text-[12px] italic text-slate-400">Chưa gán</span>
                                            )}
                                        </td>
                                        
                                        {/* CELL 6 & 7: NGÀY ĐẶT (Gộp 2 cột Từ ngày - Đến ngày) */}
                                        <td className="py-3 px-2 text-center" colSpan={2}>
                                            <DateTimeCell isoString={order.orderDate} />
                                        </td>
                                        
                                        {/* CELL 8: TRẠNG THÁI ĐƠN */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${OrderStatusColors[order.status]}`}>
                                                    {OrderStatusLabels[order.status]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 9: THANH TOÁN */}
                                        <td className="py-3 px-2 text-center">
                                            <div className="flex justify-center">
                                                <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold border shadow-sm ${PaymentStatusColors[order.paymentStatus]}`}>
                                                    {PaymentStatusLabels[order.paymentStatus]}
                                                </span>
                                            </div>
                                        </td>
                                        
                                        {/* CELL 10: TỔNG TIỀN */}
                                        <td className="py-3 px-2 text-right">
                                            <span className="text-[14px] font-black text-emerald-600">
                                                {formatCurrency(order.totalAmount)}
                                            </span>
                                        </td>
                                        
                                        {/* CELL 11: THAO TÁC */}
                                        <td className="py-3 px-6">
                                            <div className="flex justify-center gap-1.5 opacity-40 group-hover:opacity-100 transition-all duration-300">
                                                <button
                                                    onClick={() => navigate(`/orders/${order.id}`)}
                                                    className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                                                    title="Xem chi tiết"
                                                >
                                                    <Eye size={17} strokeWidth={2.5} />
                                                </button>
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
        </ListPageContainer>
    );
};

export default OrderList;