import { PaginationParams, PagedResult } from './common';

export interface ShiftClosingItem {
    variantId: number;
    variantName: string;
    variantCode: string;
    uomName: string;
    openingStock: number;
    totalReceipt: number;
    totalTransferIn: number;
    totalReturn: number;
    totalIssue: number;
    totalTransferOut: number;
    totalAdjustment: number;
    closingStock: number;
    currentAvailable: number;
    currentReserved: number;
    currentDamaged: number;
}

export interface ShiftClosingReport {
    warehouseId: number;
    warehouseName: string;
    fromDate: string;
    toDate: string;
    totalOpeningItems: number;
    totalInflowItems: number;
    totalOutflowItems: number;
    totalClosingItems: number;
    items: ShiftClosingItem[];
}

export interface StockLedgerEntry {
    transactionDate: string;
    transactionCode: string;
    transactionType: string;
    referenceCode: string;
    variantName: string;
    batchCode: string;
    quantity: number;
    note: string;
    performedBy: string;
}
