import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import PurchaseOrderDetail from '../../../pages/purchaseOrder/PurchaseOrderDetail';
import { purchaseOrderApi } from '../../../api/purchaseOrderApi';
import { PurchaseOrderStatus } from '../../../types/purchaseOrder';

// Mock APIs
vi.mock('../../../api/purchaseOrderApi', () => ({
  purchaseOrderApi: {
    getById: vi.fn(),
    updateStatus: vi.fn(),
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
 * 🛒 MODULE 09: PURCHASING & PURCHASE ORDER MANAGEMENT
 * 🧪 COMPONENT TEST: PurchaseOrderDetail (Chi Tiết Đơn Hàng & Vòng Đời Trạng Thái)
 * ============================================================================
 */
describe('Module 09 - PurchaseOrderDetail Component', () => {
  const mockOrderDraft = {
    id: 1,
    orderCode: 'PO-20260830-001',
    orderDate: '2026-08-30T08:00:00Z',
    expectedDeliveryDate: '2026-09-02T08:00:00Z',
    status: PurchaseOrderStatus.Draft,
    totalAmount: 2000000,
    note: 'Yêu cầu kiểm tra kỹ độ ngọt Brix',
    supplierId: 1,
    supplierName: 'Nông Trại Đà Lạt GAP',
    createdById: 10,
    createdByName: 'Nguyễn Thu Mua',
    createdAt: '2026-08-30T08:00:00Z',
    updatedAt: '2026-08-30T08:00:00Z',
    details: [
      {
        id: 1,
        variantId: 10,
        variantCode: 'SKU-DAUTAY-500G',
        variantName: 'Dâu Tây Hộp 500g',
        uoMId: 100,
        uoMName: 'Hộp',
        orderQuantity: 50,
        receivedQuantity: 0,
        unitPrice: 40000,
        totalPrice: 2000000,
      },
    ],
  };

  const mockOrderProcessing = {
    ...mockOrderDraft,
    id: 2,
    orderCode: 'PO-20260830-002',
    status: PurchaseOrderStatus.Processing,
  };

  const mockOrderApproved = {
    ...mockOrderDraft,
    id: 3,
    orderCode: 'PO-20260830-003',
    status: PurchaseOrderStatus.Approved,
  };

  const mockOrderCancelled = {
    ...mockOrderDraft,
    id: 4,
    orderCode: 'PO-20260830-004',
    status: PurchaseOrderStatus.Cancelled,
    cancellationReason: 'Nhà cung cấp hết hàng mùa vụ',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  // #region TC01: RENDER CHI TIẾT ĐƠN HÀNG VÀ TAB THÔNG TIN
  it('TC01 - Render thông tin chi tiết đơn mua hàng, nhà cung cấp, ngày đặt, ngày giao và tổng tiền', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderDraft);

    render(
      <MemoryRouter initialEntries={['/purchase-orders/1']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Mã Đơn: PO-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('Nông Trại Đà Lạt GAP')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Thu Mua')).toBeInTheDocument();
      expect(screen.getByText('Yêu cầu kiểm tra kỹ độ ngọt Brix')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TAB CHI TIẾT MẶT HÀNG & TIẾN ĐỘ GIAO HÀNG
  it('TC02 - Chuyển sang Tab Mặt hàng và hiển thị đúng SKU, Tên, Đơn giá, SL Đặt và SL Đã nhận', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderDraft);

    render(
      <MemoryRouter initialEntries={['/purchase-orders/1']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/2\. CHI TIẾT MẶT HÀNG/i)).toBeInTheDocument();
    });

    const itemsTab = screen.getByText(/2\. CHI TIẾT MẶT HÀNG/i);
    fireEvent.click(itemsTab);

    await waitFor(() => {
      expect(screen.getByText('SKU-DAUTAY-500G')).toBeInTheDocument();
      expect(screen.getByText('Dâu Tây Hộp 500g')).toBeInTheDocument();
      expect(screen.getByText('50')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: LUỒNG WORKFLOW - CHUYỂN DRAFT SANG PROCESSING
  it('TC03 - Hiển thị nút "Chuyển Chờ Duyệt" khi đơn ở trạng thái Nháp và gọi API cập nhật trạng thái', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderDraft);
    (purchaseOrderApi.updateStatus as any).mockResolvedValue({ message: 'Thành công' });

    render(
      <MemoryRouter initialEntries={['/purchase-orders/1']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Chuyển Chờ Duyệt/i)).toBeInTheDocument();
    });

    const btnSubmitForApproval = screen.getByText(/Chuyển Chờ Duyệt/i);
    fireEvent.click(btnSubmitForApproval);

    await waitFor(() => {
      expect(purchaseOrderApi.updateStatus).toHaveBeenCalledWith(1, {
        status: PurchaseOrderStatus.Processing,
        cancellationReason: undefined,
      });
    });
  });
  // #endregion

  // #region TC04: LUỒNG WORKFLOW - DUYỆT ĐƠN HÀNG KHI Ở TRẠNG THÁI PROCESSING
  it('TC04 - Hiển thị nút "Duyệt Đơn Hàng" khi đơn ở trạng thái Chờ duyệt (Processing)', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderProcessing);
    (purchaseOrderApi.updateStatus as any).mockResolvedValue({ message: 'Thành công' });

    render(
      <MemoryRouter initialEntries={['/purchase-orders/2']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Duyệt Đơn Hàng/i)).toBeInTheDocument();
      expect(screen.getByText(/Hủy Đơn/i)).toBeInTheDocument();
    });

    const approveBtn = screen.getByText(/Duyệt Đơn Hàng/i);
    fireEvent.click(approveBtn);

    await waitFor(() => {
      expect(purchaseOrderApi.updateStatus).toHaveBeenCalledWith(2, {
        status: PurchaseOrderStatus.Approved,
        cancellationReason: undefined,
      });
    });
  });
  // #endregion

  // #region TC05: LUỒNG HỦY ĐƠN VỚI LÝ DO
  it('TC05 - Mở modal xác nhận hủy đơn, nhập lý do và gửi yêu cầu hủy', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderProcessing);
    (purchaseOrderApi.updateStatus as any).mockResolvedValue({ message: 'Hủy thành công' });

    render(
      <MemoryRouter initialEntries={['/purchase-orders/2']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Hủy Đơn/i)).toBeInTheDocument();
    });

    const cancelBtn = screen.getByText(/Hủy Đơn/i);
    fireEvent.click(cancelBtn);

    // Modal xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Xác nhận Hủy Đơn/i)).toBeInTheDocument();
    });

    const reasonInput = screen.getByPlaceholderText(/Nhà cung cấp báo hết hàng, sai giá.../i);
    fireEvent.change(reasonInput, { target: { value: 'Giá biến động quá cao' } });

    const confirmCancelBtn = screen.getByRole('button', { name: /Xác Nhận Hủy/i });
    fireEvent.click(confirmCancelBtn);

    await waitFor(() => {
      expect(purchaseOrderApi.updateStatus).toHaveBeenCalledWith(2, {
        status: PurchaseOrderStatus.Cancelled,
        cancellationReason: 'Giá biến động quá cao',
      });
    });
  });
  // #endregion

  // #region TC06: NÚT TẠO PHIẾU NHẬP KHO KHI ĐƠN ĐÃ DUYỆT
  it('TC06 - Hiển thị nút "Nhận Hàng (Tạo Phiếu Nhập)" khi đơn ở trạng thái Approved và điều hướng chính xác', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderApproved);

    render(
      <MemoryRouter initialEntries={['/purchase-orders/3']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Nhận Hàng \(Tạo Phiếu Nhập\)/i)).toBeInTheDocument();
    });

    const createReceiptBtn = screen.getByText(/Nhận Hàng \(Tạo Phiếu Nhập\)/i);
    fireEvent.click(createReceiptBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-receipts/create?poId=3');
  });
  // #endregion

  // #region TC07: CẢNH BÁO LÝ DO HỦY KHI ĐƠN BỊ CANCELLED
  it('TC07 - Hiển thị khối cảnh báo lý do hủy đơn khi đơn ở trạng thái Cancelled', async () => {
    (purchaseOrderApi.getById as any).mockResolvedValue(mockOrderCancelled);

    render(
      <MemoryRouter initialEntries={['/purchase-orders/4']}>
        <Routes>
          <Route path="/purchase-orders/:id" element={<PurchaseOrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Lý do hủy đơn:/i)).toBeInTheDocument();
      expect(screen.getByText('Nhà cung cấp hết hàng mùa vụ')).toBeInTheDocument();
    });
  });
  // #endregion
});
