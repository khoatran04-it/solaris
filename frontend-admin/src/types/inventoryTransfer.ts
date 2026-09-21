import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum InventoryTransferStatus {
  Draft = 1,
  Approved = 5,
  InTransit = 2,
  Completed = 3,
  Cancelled = 4,
}

export const InventoryTransferStatusLabels: Record<InventoryTransferStatus, string> = {
  [InventoryTransferStatus.Draft]: 'Nháp',
  [InventoryTransferStatus.Approved]: 'Đã duyệt',
  [InventoryTransferStatus.InTransit]: 'Đang vận chuyển',
  [InventoryTransferStatus.Completed]: 'Đã nhận hàng',
  [InventoryTransferStatus.Cancelled]: 'Đã hủy',
};

export const InventoryTransferStatusColors: Record<InventoryTransferStatus, string> = {
  [InventoryTransferStatus.Draft]: 'bg-slate-50 text-slate-600 border-slate-200',
  [InventoryTransferStatus.Approved]: 'bg-blue-50 text-blue-700 border-blue-200',
  [InventoryTransferStatus.InTransit]: 'bg-amber-50 text-amber-700 border-amber-200',
  [InventoryTransferStatus.Completed]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [InventoryTransferStatus.Cancelled]: 'bg-rose-50 text-rose-700 border-rose-200',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface InventoryTransferDetail {
  id: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  batchId: number;
  batchCode: string;
  uoMId: number;
  uoMName: string;
  quantity: number;
  actualReceivedQuantity?: number;
  damagedQuantity?: number;
}

export interface InventoryTransfer {
  id: number;
  transferCode: string;

  fromWarehouseId: number;
  fromWarehouseName: string;

  toWarehouseId: number;
  toWarehouseName: string;

  orderId?: number;
  orderCode?: string;

  status: InventoryTransferStatus;

  createdById: number;
  createdByName: string;

  approvedById?: number;
  approvedByName?: string;
  approvedDate?: string;
  approvalNote?: string;

  dispatchedById?: number;
  dispatchedByName?: string;
  dispatchedDate?: string;

  receivedById?: number;
  receivedByName?: string;
  receivedDate?: string;

  driverName?: string;
  driverPhone?: string;
  licensePlate?: string;
  inspectedById?: number;
  inspectedByName?: string;
  inspectedDate?: string;

  note?: string;
  cancellationReason?: string;

  createdAt: string;
  updatedAt: string;

  details: InventoryTransferDetail[];
}

export interface InventoryTransferInspectReceivePayload {
  items: {
    detailId: number;
    actualReceivedQuantity: number;
    damagedQuantity: number;
  }[];
  note?: string;
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface InventoryTransferDetailCreatePayload {
  variantId: number;
  batchId: number;
  uoMId: number;
  quantity: number;
}

export interface InventoryTransferCreatePayload {
  fromWarehouseId: number;
  toWarehouseId: number;
  orderId?: number;
  createdById?: number;
  note?: string;
  details: InventoryTransferDetailCreatePayload[];
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface InventoryTransferQueryParams extends PaginationParams {
  fromWarehouseId?: number | string;
  toWarehouseId?: number | string;
  status?: number;
  startDate?: string;
  endDate?: string;
}
