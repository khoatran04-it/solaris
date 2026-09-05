import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import InventoryIssueDetail from '../../../pages/inventoryIssue/InventoryIssueDetail';
import { inventoryIssueApi } from '../../../api/inventoryIssueApi';
import { InventoryIssueStatus } from '../../../types/inventoryIssue';

// Mock APIs
vi.mock('../../../api/inventoryIssueApi', () => ({
  inventoryIssueApi: {
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
 * MODULE 10: INVENTORY ISSUES & FEFO SMART PICKER
 * COMPONENT TEST: InventoryIssueDetail (Chi Tiết & Xử Lý Phiếu Xuất Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryIssueDetail Component', () => {
  const mockIssue = {
    id: 1,
    issueCode: 'ISS-20260830-001',
    orderId: 10,
    orderCode: 'ORD-20260830-001',
    warehouseId: 1,
    warehouseName: 'Tổng Kho Hà Nội',
    status: InventoryIssueStatus.Pending,
    issuedById: 10,
    issuedByName: 'Trần Nhặt Hàng',
    receiverName: 'Nguyễn Văn Khách',
    receiverPhone: '0988888888',
    deliveryAddress: 'Số 10 Phố Huế, Hoàn Kiếm, Hà Nội',
    issueDate: '2026-08-30T08:00:00Z',
    note: 'Giao giờ hành chính',
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
        quantity: 30,
        unitPrice: 50000,
        totalPrice: 1500000,
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (inventoryIssueApi.getById as any).mockResolvedValue(mockIssue);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/inventory-issues/1']}>
        <Routes>
          <Route path="/inventory-issues/:id" element={<InventoryIssueDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: RENDER CHI TIẾT PHIẾU XUẤT KHO
  it('TC01 - Render thông tin phiếu xuất, đơn hàng liên kết, người nhận và địa chỉ giao', async () => {
    renderComponent();

    await waitFor(() => {
      expect(inventoryIssueApi.getById).toHaveBeenCalledWith(1);
    });

    expect(await screen.findByText('ISS-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('Tổng Kho Hà Nội')).toBeInTheDocument();
    expect(screen.getByText('Nguyễn Văn Khách')).toBeInTheDocument();
    expect(screen.getByText(/Số 10 Phố Huế/i)).toBeInTheDocument();
  });

  // TC02: CHUYỂN TAB XEM DANH SÁCH MẶT HÀNG XUẤT
  it('TC02 - Chuyển sang tab danh sách mặt hàng hiển thị chi tiết mã SKU, Lô và thành tiền', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const detailsTab = screen.getByRole('button', { name: /CHI TIẾT MẶT HÀNG & LÔ HÀNG/i });
    fireEvent.click(detailsTab);

    expect(screen.getByText('Dâu Tây Đà Lạt Hộp 500g')).toBeInTheDocument();
    expect(screen.getByText('BATCH-2026-001')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
  });

  // TC03: HOÀN TẤT XUẤT KHO
  it('TC03 - Bấm nút hoàn tất xuất kho gọi API complete', async () => {
    (inventoryIssueApi.complete as any).mockResolvedValue({ message: 'Hoàn tất thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const completeBtn = screen.getByRole('button', { name: /Hoàn Tất Xuất Kho/i });
    fireEvent.click(completeBtn);

    await waitFor(() => {
      expect(inventoryIssueApi.complete).toHaveBeenCalledWith(1);
    });
  });

  // TC04: HỦY PHIẾU XUẤT KHO VỚI LÝ DO
  it('TC04 - Mở modal hủy phiếu xuất kho, nhập lý do và gọi API cancel', async () => {
    (inventoryIssueApi.cancel as any).mockResolvedValue({ message: 'Hủy thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const cancelBtn = screen.getByRole('button', { name: /Hủy Phiếu Xuất/i });
    fireEvent.click(cancelBtn);

    expect(screen.getByText(/Xác Nhận Hủy Phiếu Xuất/i)).toBeInTheDocument();

    const reasonInput = screen.getByPlaceholderText(/Giao sai địa chỉ, hủy theo yêu cầu/i);
    fireEvent.change(reasonInput, { target: { value: 'Khách đổi ý hủy đơn' } });

    const confirmCancelBtn = screen.getByRole('button', { name: /Xác Nhận Hủy/i });
    fireEvent.click(confirmCancelBtn);

    await waitFor(() => {
      expect(inventoryIssueApi.cancel).toHaveBeenCalledWith(1, 'Khách đổi ý hủy đơn');
    });
  });
});
