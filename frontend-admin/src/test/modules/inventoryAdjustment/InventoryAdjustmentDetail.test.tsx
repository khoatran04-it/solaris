import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import InventoryAdjustmentDetail from '../../../pages/inventoryAdjustment/InventoryAdjustmentDetail';
import { inventoryAdjustmentApi } from '../../../api/inventoryAdjustmentApi';
import {
  InventoryAdjustmentReason,
  InventoryAdjustmentStatus,
  InventoryAdjustmentType,
} from '../../../types/inventoryAdjustment';

// Mock APIs
vi.mock('../../../api/inventoryAdjustmentApi', () => ({
  inventoryAdjustmentApi: {
    getById: vi.fn(),
    approve: vi.fn(),
    cancel: vi.fn(),
    delete: vi.fn(),
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
 * ⚖️ MODULE 11: INVENTORY ADJUSTMENT & WRITE-OFF
 * 🧪 COMPONENT TEST: InventoryAdjustmentDetail (Chi Tiết & Phê Duyệt Điều Chỉnh)
 * ============================================================================
 */
describe('Module 11 - InventoryAdjustmentDetail Component', () => {
  const mockAdjustment = {
    id: 1,
    adjustmentCode: 'ADJ-20260830-001',
    warehouseId: 1,
    warehouseName: 'Tổng Kho Hà Nội',
    auditId: 5,
    auditCode: 'AUD-20260830-005',
    status: InventoryAdjustmentStatus.Draft,
    reason: InventoryAdjustmentReason.Spoilage,
    createdById: 10,
    createdByName: 'Nguyễn Lập Phiếu',
    approvedById: 20,
    approvedByName: 'Trần Kế Toán Trưởng',
    adjustmentDate: '2026-08-30T08:00:00Z',
    totalVarianceAmount: 500000,
    note: 'Hàng dập nát',
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
        adjustmentType: InventoryAdjustmentType.MoveToDamaged,
        quantity: 10,
        unitPrice: 50000,
        totalAmount: 500000,
        reasonDetail: 'Dập nát đáy thùng',
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (inventoryAdjustmentApi.getById as any).mockResolvedValue(mockAdjustment);
    (inventoryAdjustmentApi.approve as any).mockResolvedValue({ message: 'Duyệt thành công' });
    (inventoryAdjustmentApi.cancel as any).mockResolvedValue({ message: 'Hủy thành công' });
    (inventoryAdjustmentApi.delete as any).mockResolvedValue({ message: 'Xóa thành công' });
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/inventory-adjustments/1']}>
        <Routes>
          <Route path="/inventory-adjustments/:id" element={<InventoryAdjustmentDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: RENDER CHI TIẾT PHIẾU ĐIỀU CHỈNH KHO
  it('TC01 - Render thông tin phiếu điều chỉnh, mã chứng từ và danh sách mặt hàng', async () => {
    renderComponent();

    await waitFor(() => {
      expect(inventoryAdjustmentApi.getById).toHaveBeenCalledWith(1);
    });

    expect(await screen.findByText(/ADJ-20260830-001/i)).toBeInTheDocument();
    expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    expect(screen.getByText('BATCH-2026-001')).toBeInTheDocument();
    expect(screen.getByText('Nháp')).toBeInTheDocument();
  });

  // TC02: PHÊ DUYỆT ĐIỀU CHỈNH
  it('TC02 - Nhấn nút Duyệt & Cập Nhật Tồn Kho gọi API approve', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText(/ADJ-20260830-001/i)).toBeInTheDocument();
    });

    const approveBtn = screen.getByRole('button', { name: /Duyệt & Cập Nhật Tồn Kho/i });
    fireEvent.click(approveBtn);

    await waitFor(() => {
      expect(inventoryAdjustmentApi.approve).toHaveBeenCalledWith(1);
    });
  });
});
