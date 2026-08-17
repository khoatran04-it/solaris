import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum InventoryAuditStatus {
    Draft = 1,
    InProgress = 2,
    PendingApproval = 3,
    Completed = 4,
    Cancelled = 5,
}

export const InventoryAuditStatusLabels: Record<InventoryAuditStatus, string> = {
    [InventoryAuditStatus.Draft]: 'Nháp',
    [InventoryAuditStatus.InProgress]: 'Đang kiểm đếm',
    [InventoryAuditStatus.PendingApproval]: 'Chờ duyệt',
    [InventoryAuditStatus.Completed]: 'Đã chốt sổ',
    [InventoryAuditStatus.Cancelled]: 'Đã hủy',
};

export const InventoryAuditStatusColors: Record<InventoryAuditStatus, string> = {
    [InventoryAuditStatus.Draft]: 'bg-slate-100 text-slate-600 border-slate-200',
    [InventoryAuditStatus.InProgress]: 'bg-indigo-100 text-indigo-700 border-indigo-200',
    [InventoryAuditStatus.PendingApproval]: 'bg-amber-100 text-amber-700 border-amber-200',
    [InventoryAuditStatus.Completed]: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    [InventoryAuditStatus.Cancelled]: 'bg-rose-100 text-rose-700 border-rose-200',
};

export enum InventoryAuditType {
    Full = 1,
    Cycle = 2,
    Spot = 3,
}

export const InventoryAuditTypeLabels: Record<InventoryAuditType, string> = {
    [InventoryAuditType.Full]: 'Kiểm kê toàn bộ',
    [InventoryAuditType.Cycle]: 'Kiểm kê cuốn chiếu',
    [InventoryAuditType.Spot]: 'Kiểm kê đột xuất',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface InventoryAuditDetail {
    id: number;
    variantId: number;
    variantName: string;
    variantCode: string;
    batchId: number;
    batchCode: string;
    expiryDate?: string;
    uoMId: number;
    uoMName: string;
    systemQuantity: number;
    actualQuantity: number;
    varianceQuantity: number;
    unitPrice: number;
    varianceAmount: number;
    reasonNote?: string;
}

export interface InventoryAudit {
    id: number;
    auditCode: string;

    warehouseId: number;
    warehouseName: string;

    auditType: InventoryAuditType;
    status: InventoryAuditStatus;

    auditorId: number;
    auditorName: string;

    approvedById?: number;
    approvedByName?: string;

    auditDate: string;
    completedDate?: string;

    totalSystemQty: number;
    totalActualQty: number;
    totalVarianceQty: number;
    totalVarianceAmount: number;

    note?: string;
    createdAt: string;
    updatedAt: string;

    details: InventoryAuditDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface InventoryAuditCreatePayload {
    warehouseId: number;
    auditType: InventoryAuditType;
    auditorId?: number;
    note?: string;
    specificItems?: { variantId: number; batchId: number; uoMId: number }[];
}

export interface InventoryAuditSubmitCountPayload {
    note?: string;
    items: {
        detailId: number;
        actualQuantity: number;
        reasonNote?: string;
    }[];
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface InventoryAuditQueryParams extends PaginationParams {
    warehouseId?: number | string;
    status?: number;
    auditType?: number;
    startDate?: string;
    endDate?: string;
}
