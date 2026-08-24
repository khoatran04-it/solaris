import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum InventoryAdjustmentStatus {
  Draft = 1,
  Approved = 2,
  Cancelled = 3,
}

export const InventoryAdjustmentStatusLabels: Record<InventoryAdjustmentStatus, string> = {
  [InventoryAdjustmentStatus.Draft]: 'Nháp',
  [InventoryAdjustmentStatus.Approved]: 'Đã duyệt',
  [InventoryAdjustmentStatus.Cancelled]: 'Đã hủy',
};

export const InventoryAdjustmentStatusColors: Record<InventoryAdjustmentStatus, string> = {
  [InventoryAdjustmentStatus.Draft]: 'bg-slate-100 text-slate-600 border-slate-200',
  [InventoryAdjustmentStatus.Approved]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  [InventoryAdjustmentStatus.Cancelled]: 'bg-rose-100 text-rose-700 border-rose-200',
};

export enum InventoryAdjustmentReason {
  Surplus = 1,
  LossTheft = 2,
  Spoilage = 3,
  Damage = 4,
  Expiry = 5,
  Shrinkage = 6,
  DataCorrection = 7,
}

export const InventoryAdjustmentReasonLabels: Record<InventoryAdjustmentReason, string> = {
  [InventoryAdjustmentReason.Surplus]: 'Thừa do kiểm kê (+)',
  [InventoryAdjustmentReason.LossTheft]: 'Thất thoát / Mất cắp (-)',
  [InventoryAdjustmentReason.Spoilage]: 'Hư hỏng biến đổi sinh hóa (-)',
  [InventoryAdjustmentReason.Damage]: 'Dập nát / Vỡ bao bì (-)',
  [InventoryAdjustmentReason.Expiry]: 'Hết hạn sử dụng (-)',
  [InventoryAdjustmentReason.Shrinkage]: 'Hao hụt tự nhiên (-)',
  [InventoryAdjustmentReason.DataCorrection]: 'Sai lệch nhập liệu (±)',
};

export enum InventoryAdjustmentType {
  IncreaseAvailable = 1,
  DecreaseAvailable = 2,
  MoveToDamaged = 3,
  DisposeDamaged = 4,
}

export const InventoryAdjustmentTypeLabels: Record<InventoryAdjustmentType, string> = {
  [InventoryAdjustmentType.IncreaseAvailable]: 'Tăng tồn khả dụng (+ Available)',
  [InventoryAdjustmentType.DecreaseAvailable]: 'Giảm tồn khả dụng (- Available)',
  [InventoryAdjustmentType.MoveToDamaged]: 'Chuyển sang hàng hỏng (Available -> Damaged)',
  [InventoryAdjustmentType.DisposeDamaged]: 'Xuất hủy hàng hỏng (- Damaged)',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface InventoryAdjustmentDetail {
  id: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  batchId: number;
  batchCode: string;
  expiryDate?: string;
  uoMId: number;
  uoMName: string;
  adjustmentType: InventoryAdjustmentType;
  quantity: number;
  unitPrice: number;
  totalAmount: number;
  reasonDetail?: string;
}

export interface InventoryAdjustment {
  id: number;
  adjustmentCode: string;

  warehouseId: number;
  warehouseName: string;

  auditId?: number;
  auditCode?: string;

  status: InventoryAdjustmentStatus;
  reason: InventoryAdjustmentReason;

  createdById: number;
  createdByName: string;

  approvedById?: number;
  approvedByName?: string;

  adjustmentDate: string;
  approvedDate?: string;

  totalVarianceAmount: number;
  note?: string;

  createdAt: string;
  updatedAt: string;

  details: InventoryAdjustmentDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface InventoryAdjustmentDetailCreatePayload {
  variantId: number;
  batchId: number;
  uoMId: number;
  adjustmentType: InventoryAdjustmentType;
  quantity: number;
  unitPrice: number;
  reasonDetail?: string;
}

export interface InventoryAdjustmentCreatePayload {
  warehouseId: number;
  auditId?: number;
  reason: InventoryAdjustmentReason;
  createdById?: number;
  note?: string;
  details: InventoryAdjustmentDetailCreatePayload[];
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface InventoryAdjustmentQueryParams extends PaginationParams {
  warehouseId?: number | string;
  status?: number;
  reason?: number;
  startDate?: string;
  endDate?: string;
}
