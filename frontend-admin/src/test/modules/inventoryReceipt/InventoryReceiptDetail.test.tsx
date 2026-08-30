import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import InventoryReceiptDetail from '../../../pages/inventoryReceipt/InventoryReceiptDetail';
import { inventoryReceiptApi } from '../../../api/inventoryReceiptApi';
import { InventoryReceiptStatus } from '../../../types/inventoryReceipt';

// Mock APIs
vi.mock('../../../api/inventoryReceiptApi', () => ({
  inventoryReceiptApi: {
    getById: vi.fn(),
    complete: vi.fn(),
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
 * 📥 MODULE 10: INVENTORY RECEIPTS & QUALITY CONTROL (GRN)
 * 🧪 COMPONENT TEST: InventoryReceiptDetail (Chi Tiết & Xử Lý Phiếu Nhập Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryReceiptDetail Component', () => {
  const mockReceipt = {
    id: 1,
    receiptCode: 'IR-20260830-001',
    warehouseId: 1,
    warehouseName: 'Tổng Kho Hà Nội',
    supplierId: 1,
    supplierName: 'Nông Trại Đà Lạt GAP',
    status: InventoryReceiptStatus.Pending,
    receivedById: 10,
    receivedByName: 'Nguyễn Kiểm Đếm',
    receiptDate: '2026-08-30T08:00:00Z',
    note: 'Giao hàng đúng hẹn',
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
        uoMName: 'Hộp',
        expectedQuantity: 100,
        acceptedQuantity: 95,
        rejectedQuantity: 5,
        rejectReason: 'Héo cuống',
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (inventoryReceiptApi.getById as any).mockResolvedValue(mockReceipt);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/inventory-receipts/1']}>
        <Routes>
          <Route path="/inventory-receipts/:id" element={<InventoryReceiptDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: RENDER CHI TIẾT PHIẾU NHẬP KHO
  it('TC01 - Render thông tin phiếu nhập kho, nhà cung cấp, kho nhận và danh sách kiểm đếm', async () => {
    renderComponent();

    await waitFor(() => {
      expect(inventoryReceiptApi.getById).toHaveBeenCalledWith(1);
    });

    expect(await screen.findByText('IR-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    expect(screen.getByText('Nông Trại Đà Lạt GAP')).toBeInTheDocument();
    expect(screen.getByText('Nguyễn Kiểm Đếm')).toBeInTheDocument();
  });

  // TC02: CHUYỂN TAB XEM DANH SÁCH MẶT HÀNG
  it('TC02 - Chuyển sang tab danh sách mặt hàng và kiểm đếm hiển thị đầy đủ thông tin QC', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    // Bấm tab "2. CHI TIẾT KIỂM ĐẾM"
    const detailsTab = screen.getByRole('button', { name: /CHI TIẾT KIỂM ĐẾM/i });
    fireEvent.click(detailsTab);

    expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    expect(screen.getByText('BATCH-2026-001')).toBeInTheDocument();
    expect(screen.getAllByText('100').length).toBeGreaterThan(0);
    expect(screen.getAllByText('95').length).toBeGreaterThan(0);
    expect(screen.getAllByText('5').length).toBeGreaterThan(0);
  });

  // TC03: HOÀN TẤT NHẬP KHO
  it('TC03 - Bấm nút hoàn tất nhập kho gọi API complete và tải lại dữ liệu', async () => {
    (inventoryReceiptApi.complete as any).mockResolvedValue({ message: 'Hoàn tất thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    const completeBtn = screen.getByRole('button', { name: /Hoàn Tất Nhập Kho/i });
    fireEvent.click(completeBtn);

    await waitFor(() => {
      expect(inventoryReceiptApi.complete).toHaveBeenCalledWith(1);
    });
  });

  // TC04: HỦY PHIẾU NHẬP KHO VỚI LÝ DO
  it('TC04 - Mở modal hủy phiếu nhập kho, nhập lý do và gọi API cancel', async () => {
    (inventoryReceiptApi.cancel as any).mockResolvedValue({ message: 'Hủy thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    const cancelBtn = screen.getByRole('button', { name: /Hủy Phiếu/i });
    fireEvent.click(cancelBtn);

    // Modal hủy hiện ra
    expect(screen.getByText(/Xác Nhận Hủy Phiếu Nhập/i)).toBeInTheDocument();

    const reasonInput = screen.getByPlaceholderText(/Ví dụ: Xe quay đầu/i);
    fireEvent.change(reasonInput, { target: { value: 'Giao sai loại trái cây' } });

    const confirmCancelBtn = screen.getByRole('button', { name: /Xác Nhận Hủy/i });
    fireEvent.click(confirmCancelBtn);

    await waitFor(() => {
      expect(inventoryReceiptApi.cancel).toHaveBeenCalledWith(1, 'Giao sai loại trái cây');
    });
  });
});
