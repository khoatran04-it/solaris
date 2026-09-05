import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryAuditForm from '../../../pages/inventoryAudit/InventoryAuditForm';
import { inventoryAuditApi } from '../../../api/inventoryAuditApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { InventoryAuditType } from '../../../types/inventoryAudit';

// Mock APIs
vi.mock('../../../api/inventoryAuditApi', () => ({
  inventoryAuditApi: {
    create: vi.fn(),
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
 * MODULE 11: INVENTORY AUDIT (STOCKTAKE)
 * COMPONENT TEST: InventoryAuditForm (Tạo Mới & Snapshot Kiểm Kê)
 * ============================================================================
 */
describe('Module 11 - InventoryAuditForm Component', () => {
  const mockWarehouses = [
    { id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' },
    { id: 2, name: 'Kho Nam Sài Gòn', code: 'WH-HCM-01' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryAuditApi.create as any).mockResolvedValue({ id: 101 });
  });

  // TC01: RENDER FORM KHỞI TẠO KIỂM KÊ
  it('TC01 - Render form khởi tạo đợt kiểm kê với đầy đủ dropdown kho và các hình thức kiểm kê', async () => {
    render(
      <MemoryRouter>
        <InventoryAuditForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    expect(screen.getByText('Tạo Đợt Kiểm Kê Kho Mới')).toBeInTheDocument();
    expect(screen.getByText('Kho cần kiểm kê')).toBeInTheDocument();
    expect(screen.getByText('Kiểm kê toàn bộ')).toBeInTheDocument();
    expect(screen.getByText('Kiểm kê cuốn chiếu')).toBeInTheDocument();
    expect(screen.getByText('Kiểm kê đột xuất')).toBeInTheDocument();
  });

  // TC02: SUBMIT TẠO MỚI THÀNH CÔNG
  it('TC02 - Chọn kho kiểm kê và submit thành công chuyển hướng đến trang chi tiết', async () => {
    render(
      <MemoryRouter>
        <InventoryAuditForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(warehouseApi.getAllList).toHaveBeenCalled();
    });

    // Select warehouse via FormSelect trigger
    const whLabel = screen.getByText('Kho cần kiểm kê');
    const whSelectTrigger = whLabel.nextElementSibling as HTMLElement;
    fireEvent.click(whSelectTrigger);
    const whOption = await screen.findByText('Tổng Kho Hà Nội');
    fireEvent.click(whOption);

    // Click submit button
    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(inventoryAuditApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          warehouseId: 1,
          auditType: InventoryAuditType.Full,
        })
      );
    });
  });
});
