import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryTransferList from '../../../pages/inventoryTransfer/InventoryTransferList';
import { inventoryTransferApi } from '../../../api/inventoryTransferApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { InventoryTransferStatus } from '../../../types/inventoryTransfer';

// Mock APIs
vi.mock('../../../api/inventoryTransferApi', () => ({
  inventoryTransferApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    dispatch: vi.fn(),
    receive: vi.fn(),
    cancel: vi.fn(),
  },
}));

vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
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
 * 🧪 COMPONENT TEST: InventoryTransferList (Danh Sách Phiếu Điều Chuyển Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryTransferList Component', () => {
  const mockWarehouses = [
    { id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' },
    { id: 2, name: 'Kho Nam Sài Gòn', code: 'WH-HCM-01' },
  ];

  const mockTransfers = [
    {
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
      details: [],
    },
    {
      id: 2,
      transferCode: 'TRF-20260830-002',
      fromWarehouseId: 1,
      fromWarehouseName: 'Tổng Kho Hà Nội',
      toWarehouseId: 2,
      toWarehouseName: 'Kho Nam Sài Gòn',
      status: InventoryTransferStatus.InTransit,
      createdById: 10,
      createdByName: 'Nguyễn Quản Trị',
      dispatchedById: 11,
      dispatchedByName: 'Trần Xuất Kho',
      dispatchedDate: '2026-08-30T09:00:00Z',
      createdAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryTransferApi.getAll as any).mockResolvedValue({
      items: mockTransfers,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER DANH SÁCH PHIẾU ĐIỀU CHUYỂN
  it('TC01 - Render danh sách phiếu điều chuyển với mã phiếu, kho nguồn, kho đích và trạng thái vòng đời', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryTransferApi.getAll).toHaveBeenCalled();
    });

    expect(await screen.findByText('TRF-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('TRF-20260830-002')).toBeInTheDocument();
    expect(screen.getAllByText('Tổng Kho Hà Nội').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Kho Nam Sài Gòn').length).toBeGreaterThan(0);
    expect(screen.getByText('Nháp')).toBeInTheDocument();
    expect(screen.getByText('Đang vận chuyển')).toBeInTheDocument();
  });

  // TC02: TÌM KIẾM THEO MÃ PHIẾU
  it('TC02 - Nhập mã phiếu kích hoạt debounce API call', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm mã phiếu.../i);
    fireEvent.change(searchInput, { target: { value: 'TRF-20260830-002' } });

    await waitFor(
      () => {
        expect(inventoryTransferApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({ search: 'TRF-20260830-002' })
        );
      },
      { timeout: 1500 }
    );
  });

  // TC03: ĐIỀU HƯỚNG SANG TRANG CHI TIẾT
  it('TC03 - Bấm nút xem chi tiết điều hướng sang trang chi tiết phiếu điều chuyển', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByTitle('Xem chi tiết');
    fireEvent.click(viewButtons[0]);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-transfers/1');
  });

  // TC04: ĐIỀU HƯỚNG TẠO MỚI PHIẾU CHUYỂN
  it('TC04 - Bấm nút tạo mới điều hướng sang form tạo phiếu chuyển', async () => {
    render(
      <MemoryRouter>
        <InventoryTransferList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('TRF-20260830-001')).toBeInTheDocument();
    });

    const createBtn = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(createBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-transfers/create');
  });
});
