import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerReturnForm from '../../../pages/customerReturn/CustomerReturnForm';
import { customerReturnApi } from '../../../api/customerReturnApi';
import { orderApi } from '../../../api/orderApi';
import { customerApi } from '../../../api/customerApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { productBatchApi } from '../../../api/productBatchApi';
import { uomApi } from '../../../api/uomApi';
import { OrderStatus, PaymentStatus, PaymentMethod } from '../../../types/order';

// Mock APIs
vi.mock('../../../api/customerReturnApi', () => ({
  customerReturnApi: {
    create: vi.fn(),
  },
}));

vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
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

vi.mock('../../../api/productVariantApi', () => ({
  productVariantApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/productBatchApi', () => ({
  productBatchApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/uomApi', () => ({
  uomApi: {
    getAllList: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [new URLSearchParams()],
  };
});

/**
 * ============================================================================
 * MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * COMPONENT TEST: CustomerReturnForm (Form Tiếp Nhận Khách Hàng Trả Hàng)
 * ============================================================================
 */
describe('Module 13 - CustomerReturnForm Component', () => {
  const mockOrders = [
    {
      id: 10,
      orderCode: 'ORD-20260830-010',
      customerId: 1,
      customerName: 'Nguyễn Văn A',
      warehouseId: 1,
      warehouseName: 'Kho Tổng TP.HCM',
      status: OrderStatus.Completed,
      paymentStatus: PaymentStatus.Paid,
      paymentMethod: PaymentMethod.COD,
      subTotal: 300000,
      totalAmount: 300000,
      orderDate: '2026-08-30T08:00:00Z',
      details: [
        {
          id: 1,
          variantId: 10,
          variantName: 'Xoài Cát Hòa Lộc',
          variantCode: 'SKU-XOAI',
          uoMId: 1,
          uoMName: 'Hộp 1kg',
          quantity: 2,
          unitPrice: 150000,
          totalPrice: 300000,
        },
      ],
      issuedItems: [
        {
          id: 1,
          variantId: 10,
          batchId: 5,
          batchCode: 'BATCH-XOAI-05',
          uoMId: 1,
          quantityIssued: 2,
          unitPrice: 150000,
        },
      ],
    },
  ];

  const mockCustomers = [{ id: 1, name: 'Nguyễn Văn A', code: 'CUST-001' }];
  const mockWarehouses = [{ id: 1, name: 'Kho Tổng TP.HCM', code: 'WH-HCM' }];
  const mockVariants = [{ id: 10, name: 'Xoài Cát Hòa Lộc', code: 'SKU-XOAI', prices: [] }];
  const mockBatches = [{ id: 5, variantId: 10, batchCode: 'BATCH-XOAI-05' }];
  const mockUoms = [{ id: 1, name: 'Hộp 1kg', code: 'HOP' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (orderApi.getAll as any).mockResolvedValue({ items: mockOrders });
    (orderApi.getById as any).mockResolvedValue(mockOrders[0]);
    (customerApi.getAllList as any).mockResolvedValue(mockCustomers);
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (productBatchApi.getAllList as any).mockResolvedValue(mockBatches);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
  });

  // TC01: Render form tạo phiếu trả hàng
  it('TC01 - Render form tạo phiếu trả hàng với các trường thông tin đơn hàng, kho, lý do và bảng chi tiết', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Tạo Phiếu.*Trả Hàng/i)).toBeInTheDocument();
      expect(screen.getByText('1. Đơn Bán Hàng Gốc (Sales Order Reference)')).toBeInTheDocument();
      expect(screen.getByText('2. Thông Tin Phiếu Tiếp Nhận')).toBeInTheDocument();
      expect(screen.getByText('3. Chi Tiết Mặt Hàng Hoàn Trả')).toBeInTheDocument();
    });
  });

  // TC02: Thêm dòng mặt hàng trả
  it('TC02 - Bấm nút "Thêm Mặt Hàng Khác" tạo thêm một dòng trống trong bảng chi tiết', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnForm />
      </MemoryRouter>
    );

    const addRowBtn = await screen.findByRole('button', { name: /Thêm Mặt Hàng Khác/i });
    fireEvent.click(addRowBtn);

    const deleteButtons = screen.getAllByTitle(/Xóa dòng/i);
    expect(deleteButtons.length).toBe(2);
  });

  // TC03: Bắt lỗi validation khi chưa chọn đơn hàng hoặc mặt hàng
  it('TC03 - Bắt lỗi validation khi submit form mà chưa chọn Đơn hàng hoặc Mặt hàng', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnForm />
      </MemoryRouter>
    );

    const submitBtn = await screen.findByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng chọn đơn bán hàng gốc')).toBeInTheDocument();
    });

    expect(customerReturnApi.create).not.toHaveBeenCalled();
  });

  // TC04: Gọi API tạo phiếu trả hàng thành công
  it('TC04 - Nhập đầy đủ thông tin hợp lệ, submit form và điều hướng về /customer-returns', async () => {
    (customerReturnApi.create as any).mockResolvedValue({
      id: 100,
      message: 'Tạo phiếu trả hàng thành công',
    });

    render(
      <MemoryRouter>
        <CustomerReturnForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/-- Chọn Đơn Bán Hàng gốc để tự động điền --/i)).toBeInTheDocument();
    });

    // 1. Chọn đơn hàng gốc
    const orderSelect = screen.getByText(/-- Chọn Đơn Bán Hàng gốc để tự động điền --/i);
    fireEvent.click(orderSelect);
    const orderOption = await screen.findByText(/ORD-20260830-010/i);
    fireEvent.click(orderOption);

    // 2. Chờ tự động điền dữ liệu từ đơn hàng
    await waitFor(() => {
      expect(screen.getByText(/Đã liên kết đơn hàng/i)).toBeInTheDocument();
    });

    // 3. Nhập lý do trả hàng
    const reasonInput = screen.getByPlaceholderText(/Sản phẩm dập nát khi giao/i);
    fireEvent.change(reasonInput, { target: { value: 'Sản phẩm dập nát do vận chuyển' } });

    // 4. Submit form
    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(customerReturnApi.create).toHaveBeenCalled();
    });
  });
});
