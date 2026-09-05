import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryAdjustmentList from '../../../pages/inventoryAdjustment/InventoryAdjustmentList';
import { inventoryAdjustmentApi } from '../../../api/inventoryAdjustmentApi';
import { warehouseApi } from '../../../api/warehouseApi';
import {
  InventoryAdjustmentReason,
  InventoryAdjustmentStatus,
} from '../../../types/inventoryAdjustment';

// Mock APIs
vi.mock('../../../api/inventoryAdjustmentApi', () => ({
  inventoryAdjustmentApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    approve: vi.fn(),
    cancel: vi.fn(),
    delete: vi.fn(),
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
 * ️ MODULE 11: INVENTORY ADJUSTMENT & WRITE-OFF
 * COMPONENT TEST: InventoryAdjustmentList (Danh Sách Phiếu Điều Chỉnh Kho)
 * ============================================================================
 */
describe('Module 11 - InventoryAdjustmentList Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' }];

  const mockAdjustments = [
    {
      id: 1,
      adjustmentCode: 'ADJ-20260830-001',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      status: InventoryAdjustmentStatus.Draft,
      reason: InventoryAdjustmentReason.Spoilage,
      createdById: 10,
      createdByName: 'Nguyễn Lập Phiếu',
      adjustmentDate: '2026-08-30T08:00:00Z',
      totalVarianceAmount: 500000,
      note: 'Dập nát',
      createdAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      adjustmentCode: 'ADJ-20260830-002',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      status: InventoryAdjustmentStatus.Approved,
      reason: InventoryAdjustmentReason.Surplus,
      createdById: 10,
      createdByName: 'Nguyễn Lập Phiếu',
      adjustmentDate: '2026-08-30T09:00:00Z',
      totalVarianceAmount: 200000,
      note: 'Thừa kiểm kê',
      createdAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryAdjustmentApi.getAll as any).mockResolvedValue({
      items: mockAdjustments,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER DANH SÁCH PHIẾU ĐIỀU CHỈNH KHO
  it('TC01 - Render danh sách phiếu điều chỉnh kho thành công với đầy đủ mã phiếu, kho và trạng thái', async () => {
    render(
      <MemoryRouter>
        <InventoryAdjustmentList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryAdjustmentApi.getAll).toHaveBeenCalled();
    });

    expect(await screen.findByText('ADJ-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('ADJ-20260830-002')).toBeInTheDocument();
    expect(screen.getAllByText('Tổng Kho Hà Nội')).toHaveLength(2);
    expect(screen.getByText('Nháp')).toBeInTheDocument();
    expect(screen.getByText('Đã duyệt')).toBeInTheDocument();
  });

  // TC02: HIỂN THỊ KHI DANH SÁCH RỖNG
  it('TC02 - Hiển thị trạng thái rỗng khi không có phiếu điều chỉnh nào', async () => {
    (inventoryAdjustmentApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <InventoryAdjustmentList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryAdjustmentApi.getAll).toHaveBeenCalled();
    });

    expect(
      await screen.findByText('Không có phiếu điều chỉnh nào phù hợp điều kiện tìm kiếm.')
    ).toBeInTheDocument();
  });
});
