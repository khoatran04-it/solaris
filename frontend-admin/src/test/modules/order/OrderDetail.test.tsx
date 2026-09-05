import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import OrderDetail from '../../../pages/order/OrderDetail';
import { orderApi } from '../../../api/orderApi';
import { OrderStatus, PaymentStatus, PaymentMethod } from '../../../types/order';

// Mock APIs
vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    getById: vi.fn(),
    cancel: vi.fn(),
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
 * MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * COMPONENT TEST: OrderDetail (Chi Tiết Đơn Bán Hàng)
 * ============================================================================
 */
describe('Module 13 - OrderDetail Component', () => {
  const mockOrder = {
    id: 100,
    orderCode: 'ORD-20260830-100',
    customerId: 1,
    customerName: 'Nguyễn Văn A',
    customerPhone: '0901234567',
    receiverName: 'Nguyễn Văn A',
    receiverPhone: '0901234567',
    deliveryAddress: '123 Lê Lợi, Bến Nghé, Quận 1, TP.HCM',
    warehouseId: 1,
    warehouseName: 'Kho Tổng TP.HCM',
    status: OrderStatus.Confirmed,
    paymentStatus: PaymentStatus.Paid,
    paymentMethod: PaymentMethod.COD,
    subTotal: 300000,
    discountAmount: 20000,
    shippingFee: 25000,
    totalAmount: 305000,
    note: 'Giao hàng giờ hành chính',
    orderDate: '2026-08-30T08:00:00Z',
    createdAt: '2026-08-30T08:00:00Z',
    updatedAt: '2026-08-30T08:00:00Z',
    details: [
      {
        id: 1,
        variantId: 10,
        variantName: 'Xoài Cát Hòa Lộc Hộp 1kg',
        variantCode: 'SKU-XOAI-1KG',
        uoMId: 1,
        uoMName: 'Hộp 1kg',
        quantity: 2,
        baseQuantity: 2,
        unitPrice: 150000,
        discountAmount: 20000,
        totalPrice: 280000,
        issuedQuantity: 0,
      },
    ],
    issuedItems: [],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (orderApi.getById as any).mockResolvedValue(mockOrder);
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/orders/100']}>
        <Routes>
          <Route path="/orders/:id" element={<OrderDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // TC01: Render chi tiết đơn hàng
  it('TC01 - Render chi tiết đơn hàng với đầy đủ mã đơn, người nhận, địa chỉ, trạng thái, và tổng tiền', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-100')).toBeInTheDocument();
      expect(screen.getByText(/123 Lê Lợi, Bến Nghé, Quận 1, TP.HCM/i)).toBeInTheDocument();
      expect(screen.getByText('Kho Tổng TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('Đã xác nhận')).toBeInTheDocument();
      expect(screen.getByText('Đã thanh toán')).toBeInTheDocument();
    });
  });

  // TC02: Chuyển đổi tab sang danh sách mặt hàng
  it('TC02 - Chuyển sang tab Mặt hàng đặt mua và hiển thị đầy đủ danh sách sản phẩm', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-100')).toBeInTheDocument();
    });

    const itemsTab = screen.getByRole('button', { name: /2\. CHI TIẾT MẶT HÀNG/i });
    fireEvent.click(itemsTab);

    await waitFor(() => {
      expect(screen.getByText('Xoài Cát Hòa Lộc Hộp 1kg')).toBeInTheDocument();
      expect(screen.getByText('SKU-XOAI-1KG')).toBeInTheDocument();
    });
  });

  // TC03: Hủy đơn hàng và gọi API cancel
  it('TC03 - Mở modal hủy đơn, nhập lý do hủy và gọi API orderApi.cancel', async () => {
    (orderApi.cancel as any).mockResolvedValue({ message: 'Hủy đơn hàng thành công' });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-100')).toBeInTheDocument();
    });

    const cancelBtn = screen.getByRole('button', { name: /Hủy Đơn Này/i });
    fireEvent.click(cancelBtn);

    // Modal hủy xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Xác Nhận Hủy Đơn Hàng/i)).toBeInTheDocument();
    });

    const reasonInput = screen.getByPlaceholderText(
      /Khách đổi ý, đặt nhầm sản phẩm, kho hết hàng.../i
    );
    fireEvent.change(reasonInput, { target: { value: 'Khách hàng đổi ý muốn đặt lại đơn khác' } });

    const confirmCancelBtn = screen.getByRole('button', { name: /Xác Nhận Hủy/i });
    fireEvent.click(confirmCancelBtn);

    await waitFor(() => {
      expect(orderApi.cancel).toHaveBeenCalledWith(100, 'Khách hàng đổi ý muốn đặt lại đơn khác');
    });
  });

  // TC04: Điều hướng quay lại danh sách đơn hàng
  it('TC04 - Bấm nút Quay lại điều hướng về route /orders', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-100')).toBeInTheDocument();
    });

    const backBtn = screen.getByRole('button', { name: /QUAY LẠI/i });
    fireEvent.click(backBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/orders');
  });
});
