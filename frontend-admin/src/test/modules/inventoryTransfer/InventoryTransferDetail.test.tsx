import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import InventoryTransferDetail from '../../../pages/inventoryTransfer/InventoryTransferDetail';
import { inventoryTransferApi } from '../../../api/inventoryTransferApi';
import { InventoryTransferStatus } from '../../../types/inventoryTransfer';

// Mock APIs
vi.mock('../../../api/inventoryTransferApi', () => ({
  inventoryTransferApi: {
    getById: vi.fn(),
    dispatch: vi.fn(),
    receive: vi.fn(),
    cancel: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

/**
 * ============================================================================
 * 🚚 MODULE 10: INVENTORY TRANSFERS (2-STEP DISPATCH & RECEIVE)
 * 🧪 COMPONENT TEST: InventoryTransferDetail (Chi Tiết & Xử Lý Phiếu Điều Chuyển)
 * ============================================================================
 */
describe('Module 10 - InventoryTransferDetail Component', () => {
  const mockDraftTransfer = {
    id: 1,
    transferCode: 'TRF-20260830-001',
    fromWarehouseId: 1,
    fromWarehouseName: 'Tổng Kho Hà Nội',
    toWarehouseId: 2,
    toWarehouseName: 'Kho Nam Sài Gòn',
    status: InventoryTransferStatus.Draft,
    createdById: 10,
    createdByName: 'Nguyễn Quản Trị',
    createdAt: '2026-08-30T08:00:00Z',
    note: 'Chuyển cân đối tồn kho',
    details: [
      {
        id: 100,
        variantId: 1,
        variantName: 'Dâu Tây Đà Lạt Hộp 500g',
        variantCode: 'SKU-DAUTAY-500G',
        batchId: 50,
        batchCode: 'BATCH-2026-001',
        uoMId: 1,
        uoMName: 'Hộp',
        quantity: 50,
      },
    ],
  };

  const mockInTransitTransfer = {
    ...mockDraftTransfer,
    id: 2,
    status: InventoryTransferStatus.InTransit,
    dispatchedById: 11,
    dispatchedByName: 'Trần Xuất Kho',
    dispatchedDate: '2026-08-30T09:00:00Z',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  // TC01: RENDER CHI TIẾT PHIẾU ĐIỀU CHUYỂN DRAFT
  it('TC01 - Render thông tin phiếu điều chuyển, kho nguồn, kho đích và nút xuất hàng đi', async () => {
    (inventoryTransferApi.getById as any).mockResolvedValue(mockDraftTransfer);

    render(
      <MemoryRouter initialEntries={['/inventory-transfers/1']}>
        <Routes>
          <Route path="/inventory-transfers/:id" element={<InventoryTransferDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryTransferApi.getById).toHaveBeenCalledWith(1);
    });

    expect(await screen.findByText('TRF-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    expect(screen.getByText('Kho Nam Sài Gòn')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Xuất Hàng Đi \(In-Transit\)/i })).toBeInTheDocument();
  });

  // TC02: BƯỚC 1 - XUẤT HÀNG ĐI (DISPATCH)
  it('TC02 - Bấm nút Xuất hàng đi gọi API dispatch để chuyển sang trạng thái InTransit', async () => {
    (inventoryTransferApi.getById as any).mockResolvedValue(mockDraftTransfer);
    (inventoryTransferApi.dispatch as any).mockResolvedValue(undefined);

    render(
      <MemoryRouter initialEntries={['/inventory-transfers/1']}>
        <Routes>
          <Route path="/inventory-transfers/:id" element={<InventoryTransferDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const dispatchBtn = screen.getByRole('button', { name: /Xuất Hàng Đi \(In-Transit\)/i });
    fireEvent.click(dispatchBtn);

    await waitFor(() => {
      expect(inventoryTransferApi.dispatch).toHaveBeenCalledWith(1);
    });
  });

  // TC03: BƯỚC 2 - NHẬN HÀNG TẠI ĐÍCH (RECEIVE)
  it('TC03 - Bấm nút Nhận hàng tại đích khi hàng đang InTransit gọi API receive', async () => {
    (inventoryTransferApi.getById as any).mockResolvedValue(mockInTransitTransfer);
    (inventoryTransferApi.receive as any).mockResolvedValue(undefined);

    render(
      <MemoryRouter initialEntries={['/inventory-transfers/2']}>
        <Routes>
          <Route path="/inventory-transfers/:id" element={<InventoryTransferDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const receiveBtn = screen.getByRole('button', { name: /Nhận Hàng Vào Kho Đích/i });
    fireEvent.click(receiveBtn);

    await waitFor(() => {
      expect(inventoryTransferApi.receive).toHaveBeenCalledWith(2);
    });
  });

  // TC04: HỦY PHIẾU CHUYỂN KHO DRAFT
  it('TC04 - Hủy phiếu điều chuyển Draft với lý do gọi API cancel', async () => {
    (inventoryTransferApi.getById as any).mockResolvedValue(mockDraftTransfer);
    (inventoryTransferApi.cancel as any).mockResolvedValue(undefined);

    render(
      <MemoryRouter initialEntries={['/inventory-transfers/1']}>
        <Routes>
          <Route path="/inventory-transfers/:id" element={<InventoryTransferDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const cancelBtn = screen.getByRole('button', { name: /Hủy Lệnh/i });
    fireEvent.click(cancelBtn);

    expect(screen.getByText(/Xác Nhận Hủy Chuyển Kho/i)).toBeInTheDocument();

    const reasonInput = screen.getByPlaceholderText(/Thay đổi tuyến điều phối/i);
    fireEvent.change(reasonInput, { target: { value: 'Hủy kế hoạch chuyển' } });

    const confirmCancelBtn = screen.getByRole('button', { name: /Xác Nhận Hủy/i });
    fireEvent.click(confirmCancelBtn);

    await waitFor(() => {
      expect(inventoryTransferApi.cancel).toHaveBeenCalledWith(1, 'Hủy kế hoạch chuyển');
    });
  });
});
