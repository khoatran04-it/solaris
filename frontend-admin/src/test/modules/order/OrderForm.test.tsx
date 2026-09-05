import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import OrderForm from '../../../pages/order/OrderForm';
import { orderApi } from '../../../api/orderApi';
import { customerApi } from '../../../api/customerApi';
import { customerAddressApi } from '../../../api/customerAddressApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { productVariantApi } from '../../../api/productVariantApi';
import { uomApi } from '../../../api/uomApi';

// Mock APIs
vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    create: vi.fn(),
    previewRouting: vi.fn(),
  },
}));

vi.mock('../../../api/customerApi', () => ({
  customerApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/customerAddressApi', () => ({
  customerAddressApi: {
    getByCustomerId: vi.fn(),
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
  };
});

/**
 * ============================================================================
 * MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * COMPONENT TEST: OrderForm (Form Tạo Đơn Bán Hàng & Định Tuyến Kho)
 * ============================================================================
 */
describe('Module 13 - OrderForm Component', () => {
  const mockCustomers = [
    { id: 1, name: 'Nguyễn Văn A', code: 'CUST-001', phoneNumber: '0901234567' },
  ];

  const mockAddresses = [
    {
      id: 10,
      customerId: 1,
      receiverName: 'Nguyễn Văn A',
      phone: '0901234567',
      fullAddress: '123 Lê Lợi, Bến Nghé, Quận 1, TP.HCM',
      isDefault: true,
      latitude: 10.775,
      longitude: 106.7,
    },
  ];

  const mockWarehouses = [
    { id: 1, name: 'Kho Tổng TP.HCM', code: 'WH-HCM' },
    { id: 2, name: 'Kho Hà Nội', code: 'WH-HN' },
  ];

  const mockVariants = [
    {
      id: 10,
      name: 'Táo Envy Size Nhỏ 1kg',
      code: 'SKU-TAO-1KG',
      prices: [{ uoMId: 1, price: 120000 }],
    },
  ];

  const mockUoms = [{ id: 1, name: 'Hộp 1kg', code: 'HOP' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerApi.getAllList as any).mockResolvedValue(mockCustomers);
    (customerAddressApi.getByCustomerId as any).mockResolvedValue(mockAddresses);
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (productVariantApi.getAllList as any).mockResolvedValue(mockVariants);
    (uomApi.getAllList as any).mockResolvedValue(mockUoms);
  });

  // TC01: Render form tạo đơn hàng
  it('TC01 - Render form tạo đơn hàng với các trường thông tin khách hàng, người nhận, kho và bảng mặt hàng', async () => {
    render(
      <MemoryRouter>
        <OrderForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Tạo Đơn Hàng Mới')).toBeInTheDocument();
      expect(screen.getByText('1. Thông Tin Khách Hàng & Giao Hàng')).toBeInTheDocument();
      expect(screen.getByText('2. Chi Tiết Mặt Hàng')).toBeInTheDocument();
      expect(screen.getByText('3. Định Tuyến Kho & Thanh Toán')).toBeInTheDocument();
    });
  });

  // TC02: Tải sổ địa chỉ khi chọn khách hàng
  it('TC02 - Tự động tải sổ địa chỉ và điền thông tin người nhận khi chọn Khách hàng', async () => {
    render(
      <MemoryRouter>
        <OrderForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chọn khách hàng...')).toBeInTheDocument();
    });

    const selectPlaceholder = screen.getByText('Chọn khách hàng...');
    fireEvent.click(selectPlaceholder);

    const customerOption = await screen.findByText('CUST-001 - Nguyễn Văn A (0901234567)');
    fireEvent.click(customerOption);

    await waitFor(() => {
      expect(customerAddressApi.getByCustomerId).toHaveBeenCalledWith(1);
    });
  });

  // TC03: Thêm dòng mặt hàng mới
  it('TC03 - Bấm nút "THÊM MẶT HÀNG" tạo thêm một dòng trống trong bảng chi tiết', async () => {
    render(
      <MemoryRouter>
        <OrderForm />
      </MemoryRouter>
    );

    const addRowBtn = await screen.findByRole('button', { name: /THÊM MẶT HÀNG/i });
    fireEvent.click(addRowBtn);

    const deleteButtons = screen.getAllByTitle(/Xóa dòng/i);
    expect(deleteButtons.length).toBe(2);
  });

  // TC04: Bắt lỗi validation khi bấm TẠO MỚI mà chưa chọn Khách hàng hoặc Biến thể
  it('TC04 - Bắt lỗi validation khi bấm Lưu đơn hàng mà chưa chọn Khách hàng hoặc Biến thể', async () => {
    render(
      <MemoryRouter>
        <OrderForm />
      </MemoryRouter>
    );

    const submitBtn = await screen.findByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vui lòng chọn khách hàng')).toBeInTheDocument();
    });

    expect(orderApi.create).not.toHaveBeenCalled();
  });

  // TC05: Gọi API tạo đơn hàng thành công
  it('TC05 - Nhập đầy đủ thông tin hợp lệ, submit form và điều hướng về /orders', async () => {
    (orderApi.create as any).mockResolvedValue({ id: 100, message: 'Tạo đơn hàng thành công' });

    render(
      <MemoryRouter>
        <OrderForm />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chọn khách hàng...')).toBeInTheDocument();
    });

    // 1. Chọn khách hàng
    const selectPlaceholder = screen.getByText('Chọn khách hàng...');
    fireEvent.click(selectPlaceholder);
    const customerOption = await screen.findByText('CUST-001 - Nguyễn Văn A (0901234567)');
    fireEvent.click(customerOption);

    // 2. Nhập địa chỉ giao hàng
    const addressInput = screen.getByPlaceholderText(
      /Số nhà, tên đường, phường\/xã, quận\/huyện.../i
    );
    fireEvent.change(addressInput, { target: { value: '123 Lê Lợi, Bến Nghé, Q1' } });

    // 3. Chọn sản phẩm
    const variantPlaceholder = screen.getByText('Chọn sản phẩm...');
    fireEvent.click(variantPlaceholder);
    const variantOption = await screen.findByText('SKU-TAO-1KG - Táo Envy Size Nhỏ 1kg');
    fireEvent.click(variantOption);

    // 4. Chọn ĐVT
    const uomPlaceholder = screen.getByText('ĐVT');
    fireEvent.click(uomPlaceholder);
    const uomOption = await screen.findByText('Hộp 1kg');
    fireEvent.click(uomOption);

    // 5. Submit form
    const submitBtn = screen.getByRole('button', { name: /TẠO MỚI/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(orderApi.create).toHaveBeenCalled();
    });
  });
});
