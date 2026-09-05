import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import InventoryAuditDetail from '../../../pages/inventoryAudit/InventoryAuditDetail';
import { inventoryAuditApi } from '../../../api/inventoryAuditApi';
import { InventoryAuditStatus, InventoryAuditType } from '../../../types/inventoryAudit';

// Mock APIs
vi.mock('../../../api/inventoryAuditApi', () => ({
  inventoryAuditApi: {
    getById: vi.fn(),
    submitCount: vi.fn(),
    approveAndReconcile: vi.fn(),
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
 * MODULE 11: INVENTORY AUDIT (STOCKTAKE)
 * COMPONENT TEST: InventoryAuditDetail (Chi Tiết & Đối Soát Kiểm Kê)
 * ============================================================================
 */
describe('Module 11 - InventoryAuditDetail Component', () => {
  const mockAudit = {
    id: 1,
    auditCode: 'AUD-20260830-001',
    warehouseId: 1,
    warehouseName: 'Tổng Kho Hà Nội',
    auditType: InventoryAuditType.Full,
    status: InventoryAuditStatus.InProgress,
    auditorId: 10,
    auditorName: 'Nguyễn Kiểm Kê',
    approvedById: 20,
    approvedByName: 'Trần Quản Lý Kho',
    auditDate: '2026-08-30T08:00:00Z',
    totalSystemQty: 100,
    totalActualQty: 95,
    totalVarianceQty: -5,
    totalVarianceAmount: -250000,
    note: 'Kiểm kê định kỳ',
    createdAt: '2026-08-30T08:00:00Z',
    details: [
      {
        id: 100,
        variantId: 1,
        variantName: 'Dâu Tây Đà Lạt Hộp 500g',
        variantCode: 'SKU-DAUTAY-500G',
        batchId: 50,
        batchCode: 'BATCH-2026-001',
        uoMId: 1,
        uoMName: 'Hộp 500g',
        systemQuantity: 100,
        actualQuantity: 95,
        varianceQuantity: -5,
        unitPrice: 50000,
        varianceAmount: -250000,
        reasonNote: 'Hao hụt 5 hộp',
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (inventoryAuditApi.getById as any).mockResolvedValue(mockAudit);
    (inventoryAuditApi.submitCount as any).mockResolvedValue(undefined);
    (inventoryAuditApi.approveAndReconcile as any).mockResolvedValue({
      adjustmentId: 88,
      message: 'Thành công',
    });
    (inventoryAuditApi.cancel as any).mockResolvedValue(undefined);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/inventory-audits/1']}>
        <Routes>
          <Route path="/inventory-audits/:id" element={<InventoryAuditDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: RENDER CHI TIẾT ĐỢT KIỂM KÊ
  it('TC01 - Render thông tin chi tiết đợt kiểm kê, kho, kiểm kê viên và danh sách dòng đối soát', async () => {
    renderComponent();

    await waitFor(() => {
      expect(inventoryAuditApi.getById).toHaveBeenCalledWith(1);
    });

    expect(await screen.findByText(/AUD-20260830-001/i)).toBeInTheDocument();
    expect(screen.getByText(/Tổng Kho Hà Nội/i)).toBeInTheDocument();
    expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    expect(screen.getByText('BATCH-2026-001')).toBeInTheDocument();
  });

  // TC02: NỘP SỐ LIỆU KIỂM ĐẾM THỰC TẾ
  it('TC02 - Nhấn nút Lưu số liệu gọi API submitCount', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText(/AUD-20260830-001/i)).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /Lưu Số Liệu/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(inventoryAuditApi.submitCount).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          items: expect.arrayContaining([
            expect.objectContaining({ detailId: 100, actualQuantity: 95 }),
          ]),
        })
      );
    });
  });

  // TC03: PHÊ DUYỆT VÀ CHỐT SỔ TỰ ĐỘNG CÂN BẰNG TỒN KHO
  it('TC03 - Nhấn nút Chốt Sổ & Cân Bằng Kho gọi API approveAndReconcile', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText(/AUD-20260830-001/i)).toBeInTheDocument();
    });

    const approveBtn = screen.getByRole('button', { name: /Chốt Sổ & Cân Bằng Kho/i });
    fireEvent.click(approveBtn);

    await waitFor(() => {
      expect(inventoryAuditApi.approveAndReconcile).toHaveBeenCalledWith(1);
    });
  });
});
