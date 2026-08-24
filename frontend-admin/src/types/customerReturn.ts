import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum CustomerReturnStatus {
  Pending = 1,
  Inspecting = 2,
  Completed = 3,
  Rejected = 4,
}

export const CustomerReturnStatusLabels: Record<CustomerReturnStatus, string> = {
  [CustomerReturnStatus.Pending]: 'Chờ tiếp nhận',
  [CustomerReturnStatus.Inspecting]: 'Đang kiểm định QC',
  [CustomerReturnStatus.Completed]: 'Đã hoàn tất',
  [CustomerReturnStatus.Rejected]: 'Từ chối trả hàng',
};

export const CustomerReturnStatusColors: Record<CustomerReturnStatus, string> = {
  [CustomerReturnStatus.Pending]: 'bg-slate-100 text-slate-600 border-slate-200',
  [CustomerReturnStatus.Inspecting]: 'bg-amber-100 text-amber-700 border-amber-200',
  [CustomerReturnStatus.Completed]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  [CustomerReturnStatus.Rejected]: 'bg-rose-100 text-rose-700 border-rose-200',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface CustomerReturnDetail {
  id: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  batchId: number;
  batchCode: string;
  uoMId: number;
  uoMName: string;
  returnedQuantity: number;
  acceptedQuantity: number;
  damagedQuantity: number;
  unitPrice: number;
  refundAmount: number;
  rejectReason?: string;
}

export interface CustomerReturn {
  id: number;
  returnCode: string;

  orderId: number;
  orderCode: string;

  customerId: number;
  customerName: string;

  warehouseId: number;
  warehouseName: string;

  receivedById?: number;
  receivedByName?: string;

  returnDate: string;
  status: CustomerReturnStatus;

  refundAmount: number;
  reason?: string;
  inspectionNotes?: string;

  createdAt: string;
  updatedAt: string;

  details: CustomerReturnDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface CustomerReturnDetailCreatePayload {
  variantId: number;
  batchId: number;
  uoMId: number;
  returnedQuantity: number;
  unitPrice?: number;
}

export interface CustomerReturnCreatePayload {
  orderId: number;
  customerId: number;
  warehouseId: number;
  receivedById?: number;
  returnDate?: string;
  reason?: string;
  details: CustomerReturnDetailCreatePayload[];
}

export interface CustomerReturnItemInspectionPayload {
  detailId: number;
  acceptedQuantity: number;
  damagedQuantity: number;
  rejectReason?: string;
}

export interface CustomerReturnInspectionPayload {
  inspectionNotes?: string;
  items: CustomerReturnItemInspectionPayload[];
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface CustomerReturnQueryParams extends PaginationParams {
  warehouseId?: number | string;
  status?: number;
  startDate?: string;
  endDate?: string;
}
