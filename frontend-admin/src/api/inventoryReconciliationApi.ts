import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { ShiftClosingReport, StockLedgerEntry } from '../types/inventoryReconciliation';

export const inventoryReconciliationApi = {
    getShiftClosing: (warehouseId: number, fromDate?: string, toDate?: string): Promise<ShiftClosingReport> => {
        return axiosClient.get('/InventoryReconciliations/shift-closing', {
            params: { warehouseId, fromDate, toDate }
        });
    },

    getStockLedger: (warehouseId: number, variantId?: number, fromDate?: string, toDate?: string, pageIndex = 1, pageSize = 15): Promise<PagedResult<StockLedgerEntry>> => {
        return axiosClient.get('/InventoryReconciliations/stock-ledger', {
            params: { warehouseId, variantId, fromDate, toDate, pageIndex, pageSize }
        });
    },
};
