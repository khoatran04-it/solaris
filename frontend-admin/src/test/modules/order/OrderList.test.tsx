import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import OrderList from '../../../pages/order/OrderList';
import { orderApi } from '../../../api/orderApi';
import { customerApi } from '../../../api/customerApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { OrderStatus, PaymentStatus, PaymentMethod } from '../../../types/order';

// Mock APIs
vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    previewRouting: vi.fn(),
    updateStatus: vi.fn(),
    cancel: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/customerApi', () => ({
  customerApi: {
    getAllList: vi.fn(),
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
 * 📦 MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * 🧪 COMPONENT TEST: OrderList (Danh Sách Đơn Bán Hàng)
 * ============================================================================
 */
describe('Module 13 - OrderList Component', () => {
  const mockCustomers = [
    { id: 1, name: 'Nguyễn Văn A', code: 'CUST-001', phoneNumber: '0901234567' },
    { id: 2, name: 'Trần Thị B', code: 'CUST-002', phoneNumber: '0987654321' },
  ];

  const mockWarehouses = [
    { id: 1, name: 'Kho Tổng TP.HCM', code: 'WH-HCM' },
    { id: 2, name: 'Kho Hà Nội', code: 'WH-HN' },
  ];

  const mockOrders = [
    {
      id: 1,
      orderCode: 'ORD-20260830-001',
      customerId: 1,
      customerName: 'Nguyễn Văn A',
      customerPhone: '0901234567',
      warehouseId: 1,
      warehouseName: 'Kho Tổng TP.HCM',
      status: OrderStatus.Confirmed,
      paymentStatus: PaymentStatus.Paid,
      paymentMethod: PaymentMethod.COD,
      subTotal: 250000,
      discountAmount: 20000,
      shippingFee: 30000,
      totalAmount: 260000,
      orderDate: '2026-08-30T08:00:00Z',
      createdAt: '2026-08-30T08:00:00Z',
      updatedAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      orderCode: 'ORD-20260830-002',
      customerId: 2,
      customerName: 'Trần Thị B',
      customerPhone: '0987654321',
      warehouseId: 2,
      warehouseName: 'Kho Hà Nội',
      status: OrderStatus.Cancelled,
      paymentStatus: PaymentStatus.Unpaid,
      paymentMethod: PaymentMethod.BankTransfer,
      subTotal: 500000,
      discountAmount: 0,
      shippingFee: 0,
      totalAmount: 500000,
      orderDate: '2026-08-30T09:00:00Z',
      createdAt: '2026-08-30T09:00:00Z',
      updatedAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerApi.getAllList as any).mockResolvedValue(mockCustomers);
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (orderApi.getAll as any).mockResolvedValue({
      items: mockOrders,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: Render danh sách đơn hàng
  it('TC01 - Render danh sách đơn hàng với đầy đủ mã đơn, khách hàng, kho xuất, trạng thái và tổng tiền', async () => {
    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260830-002')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
      expect(screen.getByText('Kho Tổng TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('Kho Hà Nội')).toBeInTheDocument();
      expect(screen.getByText('Đã xác nhận')).toBeInTheDocument();
      expect(screen.getByText('Đã hủy')).toBeInTheDocument();
      expect(screen.getByText('Đã thanh toán')).toBeInTheDocument();
      expect(screen.getByText('Chưa thanh toán')).toBeInTheDocument();
    });
  });

  // TC02: Hiển thị trạng thái rỗng
  it('TC02 - Hiển thị trạng thái rỗng (Empty State) khi danh sách trả về 0 bản ghi', async () => {
    (orderApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Không tìm thấy dữ liệu/i)).toBeInTheDocument();
    });
  });

  // TC03: Tìm kiếm theo từ khóa
  it('TC03 - Nhập từ khóa tìm kiếm và gọi API với tham số search', async () => {
    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã đơn/i);
    fireEvent.change(searchInput, { target: { value: '001' } });

    await waitFor(
      () => {
        expect(orderApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: '001',
          })
        );
      },
      { timeout: 1000 }
    );
  });

  // TC04: Điều hướng tạo đơn hàng mới
  it('TC04 - Bấm nút "THÊM" điều hướng sang route /orders/create', async () => {
    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    });

    const createButton = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(createButton);

    expect(mockNavigate).toHaveBeenCalledWith('/orders/create');
  });

  // TC05: Điều hướng xem chi tiết đơn hàng
  it('TC05 - Bấm nút xem chi tiết điều hướng sang route /orders/:id', async () => {
    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByTitle(/Xem chi tiết/i);
    fireEvent.click(viewButtons[0]);

    expect(mockNavigate).toHaveBeenCalledWith('/orders/1');
  });

  // TC06: Mở modal xóa và xác nhận xóa đơn hàng
  it('TC06 - Mở modal xác nhận xóa và gọi API delete thành công khi xác nhận', async () => {
    (orderApi.delete as any).mockResolvedValue({ message: 'Xóa đơn hàng thành công' });

    render(
      <MemoryRouter>
        <OrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle(/Xóa đơn hàng/i);
    fireEvent.click(deleteButtons[0]);

    // Modal xóa xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Xóa Đơn Bán Hàng/i)).toBeInTheDocument();
    });

    // Bấm xác nhận xóa trong modal
    const confirmDeleteBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmDeleteBtn);

    await waitFor(() => {
      expect(orderApi.delete).toHaveBeenCalledWith(1);
    });
  });
});
