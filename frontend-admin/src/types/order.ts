import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum OrderStatus {
    Draft = 1,
    Pending = 2,
    Confirmed = 3,
    Processing = 4,
    Shipping = 5,
    Completed = 6,
    Cancelled = 7,
}

export const OrderStatusLabels: Record<OrderStatus, string> = {
    [OrderStatus.Draft]: 'Nháp',
    [OrderStatus.Pending]: 'Chờ xử lý',
    [OrderStatus.Confirmed]: 'Đã xác nhận',
    [OrderStatus.Processing]: 'Đang đóng gói',
    [OrderStatus.Shipping]: 'Đang giao hàng',
    [OrderStatus.Completed]: 'Hoàn tất',
    [OrderStatus.Cancelled]: 'Đã hủy',
};

export const OrderStatusColors: Record<OrderStatus, string> = {
    [OrderStatus.Draft]: 'bg-slate-100 text-slate-600 border-slate-200',
    [OrderStatus.Pending]: 'bg-amber-100 text-amber-700 border-amber-200',
    [OrderStatus.Confirmed]: 'bg-blue-100 text-blue-700 border-blue-200',
    [OrderStatus.Processing]: 'bg-indigo-100 text-indigo-700 border-indigo-200',
    [OrderStatus.Shipping]: 'bg-purple-100 text-purple-700 border-purple-200',
    [OrderStatus.Completed]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    [OrderStatus.Cancelled]: 'bg-rose-100 text-rose-700 border-rose-200',
};

export enum PaymentStatus {
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4,
}

export const PaymentStatusLabels: Record<PaymentStatus, string> = {
    [PaymentStatus.Unpaid]: 'Chưa thanh toán',
    [PaymentStatus.PartiallyPaid]: 'Đã đặt cọc',
    [PaymentStatus.Paid]: 'Đã thanh toán',
    [PaymentStatus.Refunded]: 'Đã hoàn tiền',
};

export const PaymentStatusColors: Record<PaymentStatus, string> = {
    [PaymentStatus.Unpaid]: 'bg-slate-100 text-slate-600 border-slate-200',
    [PaymentStatus.PartiallyPaid]: 'bg-amber-100 text-amber-700 border-amber-200',
    [PaymentStatus.Paid]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    [PaymentStatus.Refunded]: 'bg-rose-100 text-rose-700 border-rose-200',
};

export enum PaymentMethod {
    COD = 1,
    BankTransfer = 2,
    EWallet = 3,
    CreditCard = 4,
}

export const PaymentMethodLabels: Record<PaymentMethod, string> = {
    [PaymentMethod.COD]: 'Thanh toán khi nhận hàng (COD)',
    [PaymentMethod.BankTransfer]: 'Chuyển khoản ngân hàng',
    [PaymentMethod.EWallet]: 'Ví điện tử',
    [PaymentMethod.CreditCard]: 'Thẻ tín dụng / Ghi nợ',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface OrderDetail {
    id: number;
    variantId: number;
    variantName: string;
    variantCode: string;
    uoMId: number;
    uoMName: string;
    quantity: number;
    baseQuantity: number;
    unitPrice: number;
    discountAmount: number;
    totalPrice: number;
    issuedQuantity: number;
}

export interface Order {
    id: number;
    orderCode: string;

    customerId: number;
    customerName: string;
    customerPhone: string;

    customerAddressId?: number;
    receiverName?: string;
    receiverPhone?: string;
    deliveryAddress?: string;

    warehouseId?: number;
    warehouseName?: string;

    status: OrderStatus;
    paymentStatus: PaymentStatus;
    paymentMethod: PaymentMethod;

    subTotal: number;
    discountAmount: number;
    shippingFee: number;
    totalAmount: number;

    note?: string;
    cancellationReason?: string;

    orderDate: string;
    createdAt: string;
    updatedAt: string;

    details: OrderDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface OrderDetailCreatePayload {
    variantId: number;
    uoMId: number;
    quantity: number;
    unitPrice?: number;
    discountAmount?: number;
}

export interface OrderCreatePayload {
    customerId: number;
    customerAddressId?: number;
    receiverName?: string;
    receiverPhone?: string;
    deliveryAddress?: string;
    warehouseId?: number;
    paymentMethod: PaymentMethod;
    shippingFee?: number;
    note?: string;
    details: OrderDetailCreatePayload[];
}

export interface OrderUpdatePayload {
    status?: OrderStatus;
    paymentStatus?: PaymentStatus;
    warehouseId?: number;
    note?: string;
    cancellationReason?: string;
}

export interface RoutingPreviewResult {
    optimalWarehouseId: number;
    warehouseName: string;
    distanceKm: number;
    isFullyStocked: boolean;
    missingItems: {
        variantId: number;
        variantName: string;
        requestedQuantity: number;
        availableQuantity: number;
        missingQuantity: number;
    }[];
    suggestedSourceWarehouseId?: number;
    suggestedSourceWarehouseName?: string;
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface OrderQueryParams extends PaginationParams {
    customerId?: number | string;
    warehouseId?: number | string;
    status?: number;
    paymentStatus?: number;
    startDate?: string;
    endDate?: string;
}
