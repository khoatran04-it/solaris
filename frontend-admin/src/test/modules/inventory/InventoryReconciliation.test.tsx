import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryReconciliation from '../../../pages/inventory/InventoryReconciliation';
import { warehouseApi } from '../../../api/warehouseApi';
import { inventoryReconciliationApi } from '../../../api/inventoryReconciliationApi';

// Mock APIs
vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/inventoryReconciliationApi', () => ({
  inventoryReconciliationApi: {
    getShiftClosing: vi.fn(),
    getStockLedger: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 10: INVENTORY
 * 🧪 COMPONENT TEST: InventoryReconciliation (Bảng Cân Đối Phát Sinh & Sổ Cái)
 * ============================================================================
 */
describe('Module 10 - InventoryReconciliation Component', () => {
  const mockWarehouses = [
    { id: 1, code: 'WH01', name: 'Tổng Kho TP.HCM' },
    { id: 2, code: 'WH02', name: 'Kho Hà Nội' },
  ];

  const mockShiftData = {
    warehouseId: 1,
    warehouseName: 'Tổng Kho TP.HCM',
    fromDate: '2026-08-25',
    toDate: '2026-09-01',
    totalOpeningItems: 500,
    totalInflowItems: 300,
    totalOutflowItems: 150,
    totalClosingItems: 650,
    items: [
      {
        variantId: 10,
        variantCode: 'SKU-XOAI-01',
        variantName: 'Xoài Cát Hòa Lộc Hộp 1kg',
        uomName: 'Hộp',
        openingStock: 200,
        totalReceipt: 100,
        totalTransferIn: 50,
        totalReturn: 10,
        totalIssue: 80,
        totalTransferOut: 20,
        totalAdjustment: 5,
        closingStock: 265,
        currentAvailable: 250,
        currentReserved: 10,
        currentDamaged: 5,
      },
      {
        variantId: 20,
        variantCode: 'SKU-BO-02',
        variantName: 'Bơ Sáp 034 Hộp 1kg',
        uomName: 'Hộp',
        openingStock: 300,
        totalReceipt: 140,
        totalTransferIn: 0,
        totalReturn: 0,
        totalIssue: 50,
        totalTransferOut: 0,
        totalAdjustment: -10,
        closingStock: 380,
        currentAvailable: 370,
        currentReserved: 5,
        currentDamaged: 5,
      },
    ],
  };

  const mockLedgerData = {
    items: [
      {
        transactionCode: 'TXN-20260901-001',
        transactionDate: '2026-09-01T08:30:00Z',
        transactionType: 'Receipt',
        referenceCode: 'PNK-20260901-01',
        variantId: 10,
        variantCode: 'SKU-XOAI-01',
        variantName: 'Xoài Cát Hòa Lộc Hộp 1kg',
        batchCode: 'BATCH-XOAI-01',
        quantity: 100,
        performedBy: 'Thủ Kho Test',
        note: 'Nhập hàng từ nhà vườn',
      },
      {
        transactionCode: 'TXN-20260901-002',
        transactionDate: '2026-09-01T10:00:00Z',
        transactionType: 'Issue',
        referenceCode: 'PXK-20260901-01',
        variantId: 10,
        variantCode: 'SKU-XOAI-01',
        variantName: 'Xoài Cát Hòa Lộc Hộp 1kg',
        batchCode: 'BATCH-XOAI-01',
        quantity: -50,
        performedBy: 'Thủ Kho Test',
        note: 'Xuất giao hàng cho khách',
      },
    ],
    totalRecords: 2,
    totalPages: 1,
    currentPage: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryReconciliationApi.getShiftClosing as any).mockResolvedValue(mockShiftData);
    (inventoryReconciliationApi.getStockLedger as any).mockResolvedValue(mockLedgerData);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter>
        <InventoryReconciliation />
      </MemoryRouter>
    );

  // #region TC01: RENDER BẢNG CÂN ĐỐI PHÁT SINH VÀ METRIC CARDS
  it('TC01 - Render Bảng cân đối phát sinh với đầy đủ Metric Cards (Tồn đầu, Nhập, Xuất, Tồn cuối)', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Cân Đối Phát Sinh & Sổ Cái (Reconciliation)')).toBeInTheDocument();
      expect(screen.getByText('500')).toBeInTheDocument(); // Tồn đầu kỳ
      expect(screen.getByText('+300')).toBeInTheDocument(); // Tổng nhập
      expect(screen.getByText('-150')).toBeInTheDocument(); // Tổng xuất
      expect(screen.getByText('650')).toBeInTheDocument(); // Tồn cuối kỳ lý thuyết
      expect(screen.getByText('SKU-XOAI-01')).toBeInTheDocument();
      expect(screen.getByText('Xoài Cát Hòa Lộc Hộp 1kg')).toBeInTheDocument();
      expect(screen.getByText('SKU-BO-02')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: CHUYỂN TAB SỔ CÁI GIAO DỊCH (AUDIT TRAIL)
  it('TC02 - Chuyển sang Tab Sổ cái giao dịch và hiển thị danh sách giao dịch bất biến', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('SKU-XOAI-01')).toBeInTheDocument();
    });

    const ledgerTabBtn = screen.getByRole('button', { name: /2\. SỔ CÁI GIAO DỊCH \(AUDIT TRAIL\)/i });
    fireEvent.click(ledgerTabBtn);

    await waitFor(() => {
      expect(screen.getByText('TXN-20260901-001')).toBeInTheDocument();
      expect(screen.getByText('TXN-20260901-002')).toBeInTheDocument();
      expect(screen.getByText('PNK-20260901-01')).toBeInTheDocument();
      expect(screen.getByText('PXK-20260901-01')).toBeInTheDocument();
      expect(screen.getByText('+100')).toBeInTheDocument();
      expect(screen.getByText('-50')).toBeInTheDocument();
      expect(screen.getByText('Nhập hàng từ nhà vườn')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: TẢI LẠI SỐ LIỆU KHI BẤM NÚT REFRESH
  it('TC03 - Bấm nút Tải Lại Số Liệu gọi lại API lấy dữ liệu mới', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('SKU-XOAI-01')).toBeInTheDocument();
    });

    const refreshBtn = screen.getByRole('button', { name: /Tải Lại Số Liệu/i });
    fireEvent.click(refreshBtn);

    await waitFor(() => {
      expect(inventoryReconciliationApi.getShiftClosing).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion
});
