import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryAuditList from '../../../pages/inventoryAudit/InventoryAuditList';
import { inventoryAuditApi } from '../../../api/inventoryAuditApi';
import { warehouseApi } from '../../../api/warehouseApi';
import {
  InventoryAuditStatus,
  InventoryAuditType,
} from '../../../types/inventoryAudit';

// Mock APIs
vi.mock('../../../api/inventoryAuditApi', () => ({
  inventoryAuditApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    submitCount: vi.fn(),
    approveAndReconcile: vi.fn(),
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
 * 📋 MODULE 11: INVENTORY AUDIT (STOCKTAKE)
 * 🧪 COMPONENT TEST: InventoryAuditList (Danh Sách Đợt Kiểm Kê Kho)
 * ============================================================================
 */
describe('Module 11 - InventoryAuditList Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' }];

  const mockAudits = [
    {
      id: 1,
      auditCode: 'AUD-20260830-001',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      auditType: InventoryAuditType.Full,
      status: InventoryAuditStatus.InProgress,
      auditorId: 10,
      auditorName: 'Nguyễn Kiểm Kê',
      auditDate: '2026-08-30T08:00:00Z',
      totalSystemQty: 100,
      totalActualQty: 0,
      totalVarianceQty: -100,
      totalVarianceAmount: -5000000,
      note: 'Kiểm kê định kỳ',
      createdAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      auditCode: 'AUD-20260830-002',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      auditType: InventoryAuditType.Spot,
      status: InventoryAuditStatus.Completed,
      auditorId: 10,
      auditorName: 'Nguyễn Kiểm Kê',
      auditDate: '2026-08-30T09:00:00Z',
      totalSystemQty: 50,
      totalActualQty: 48,
      totalVarianceQty: -2,
      totalVarianceAmount: -100000,
      note: 'Đã hoàn tất chốt sổ',
      createdAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryAuditApi.getAll as any).mockResolvedValue({
      items: mockAudits,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER DANH SÁCH ĐỢT KIỂM KÊ
  it('TC01 - Render danh sách đợt kiểm kê thành công với đầy đủ mã đợt, kho và trạng thái', async () => {
    render(
      <MemoryRouter>
        <InventoryAuditList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryAuditApi.getAll).toHaveBeenCalled();
    });

    expect(await screen.findByText('AUD-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('AUD-20260830-002')).toBeInTheDocument();
    expect(screen.getAllByText('Tổng Kho Hà Nội')).toHaveLength(2);
    expect(screen.getByText('Đang kiểm đếm')).toBeInTheDocument();
    expect(screen.getByText('Đã chốt sổ')).toBeInTheDocument();
  });

  // TC02: HIỂN THỊ KHI DANH SÁCH RỖNG
  it('TC02 - Hiển thị trạng thái rỗng khi không có đợt kiểm kê nào', async () => {
    (inventoryAuditApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <InventoryAuditList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryAuditApi.getAll).toHaveBeenCalled();
    });

    expect(
      await screen.findByText('Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc.')
    ).toBeInTheDocument();
  });
});
