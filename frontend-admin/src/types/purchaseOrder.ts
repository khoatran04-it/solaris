import { PaginationParams } from './common';

// =========================================================
// ENUMS
// =========================================================
export enum PurchaseOrderStatus {
  Draft = 1,
  Processing = 2,
  Approved = 3,
  PartiallyReceived = 4,
  Completed = 5,
  Cancelled = 6,
}

export const PurchaseOrderStatusLabels: Record<PurchaseOrderStatus, string> = {
  [PurchaseOrderStatus.Draft]: 'Nháp',
  [PurchaseOrderStatus.Processing]: 'Chờ duyệt',
  [PurchaseOrderStatus.Approved]: 'Đã duyệt',
  [PurchaseOrderStatus.PartiallyReceived]: 'Nhận một phần',
  [PurchaseOrderStatus.Completed]: 'Hoàn tất',
  [PurchaseOrderStatus.Cancelled]: 'Đã hủy',
};

export const PurchaseOrderStatusColors: Record<PurchaseOrderStatus, string> = {
  [PurchaseOrderStatus.Draft]: 'bg-slate-50 text-slate-600 border-slate-200',
  [PurchaseOrderStatus.Processing]: 'bg-blue-50 text-blue-700 border-blue-200',
  [PurchaseOrderStatus.Approved]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [PurchaseOrderStatus.PartiallyReceived]: 'bg-amber-50 text-amber-700 border-amber-200',
  [PurchaseOrderStatus.Completed]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [PurchaseOrderStatus.Cancelled]: 'bg-rose-50 text-rose-700 border-rose-200',
};

// =========================================================
// INTERFACES
// =========================================================

// Chi tiết đơn mua (Read)
export interface PurchaseOrderDetail {
  id: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  uoMId: number;
  uoMName: string;
  orderQuantity: number;
  unitPrice: number;
  totalPrice: number;
  receivedQuantity: number;
  rejectedQuantity?: number;
}

// Đơn mua hàng (Read)
export interface PurchaseOrder {
  id: number;
  orderCode: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  status: PurchaseOrderStatus;
  totalAmount: number;
  settledAmount?: number;
  note?: string;
  cancellationReason?: string;
  closureReason?: string;

  supplierId: number;
  supplierName: string;

  createdById: number;
  createdByName: string;

  warehouseId?: number;
  warehouseCode?: string;
  warehouseName?: string;

  createdAt: string;
  updatedAt: string;

  details: PurchaseOrderDetail[];
}

// Payload tạo chi tiết
export interface PurchaseOrderDetailCreatePayload {
  variantId: number;
  uoMId: number;
  orderQuantity: number;
  unitPrice: number;
}

// Payload tạo đơn mua
export interface PurchaseOrderCreatePayload {
  orderCode?: string; // Backend auto-gen, có thể bỏ qua
  orderDate: string;
  expectedDeliveryDate?: string;
  note?: string;
  supplierId: number;
  warehouseId?: number;
  createdById: number;
  details: PurchaseOrderDetailCreatePayload[];
}

// Payload cập nhật trạng thái
export interface PurchaseOrderUpdatePayload {
  expectedDeliveryDate?: string;
  note?: string;
  warehouseId?: number;
  status: PurchaseOrderStatus;
  cancellationReason?: string;
}

// Query Params
export interface PurchaseOrderQueryParams extends PaginationParams {
  supplierId?: number | string;
  status?: number;
  startDate?: string;
  endDate?: string;
}
