import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerDetail from '../../../pages/customer/CustomerDetail';
import { customerApi } from '../../../api/customerApi';
import { customerAddressApi } from '../../../api/customerAddressApi';
import { orderApi } from '../../../api/orderApi';
import { OrderStatus, PaymentStatus, PaymentMethod } from '../../../types/order';

// Mock APIs
vi.mock('../../../api/customerApi', () => ({
  customerApi: {
    getById: vi.fn(),
  },
}));

vi.mock('../../../api/customerAddressApi', () => ({
  customerAddressApi: {
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    setDefault: vi.fn(),
  },
}));

vi.mock('../../../api/orderApi', () => ({
  orderApi: {
    getAll: vi.fn(),
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
 * 📦 MODULE 06: CUSTOMER
 * 🧪 COMPONENT TEST: CustomerDetail (Chi tiết Khách hàng, Sổ địa chỉ & Lịch sử mua hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerDetail Component', () => {
  const mockCustomer = {
    id: 10,
    code: 'CUST-001',
    name: 'Nguyễn Văn A',
    phoneNumber: '0901234567',
    email: 'nguyenvana@gmail.com',
    gender: true,
    birthday: '1995-05-15T00:00:00Z',
    taxCode: '0312345678',
    customerTypeId: 1,
    customerTypeName: 'Khách Lẻ B2C',
    customerTierId: 2,
    customerTierName: 'Hạng Bạc',
    nextTierName: 'Hạng Vàng',
    amountToNextTier: 500000,
    tierProgressPercent: 67,
    discountPercent: 5,
    totalSpent: 1500000,
    totalOrders: 3,
    isActive: true,
    groups: ['Khách Thân Thiết', 'Miền Nam'],
    note: 'Khách hàng VIP cần giao nhanh',
    createdAt: '2026-08-01T10:00:00Z',
    updatedAt: '2026-08-20T10:00:00Z',
    addresses: [
      {
        id: 101,
        customerId: 10,
        receiverName: 'Nguyễn Văn A',
        phone: '0901234567',
        streetAddress: '123 Lê Lợi',
        ward: 'Phường Bến Nghé',
        district: 'Quận 1',
        province: 'TP.HCM',
        isDefault: true,
      },
      {
        id: 102,
        customerId: 10,
        receiverName: 'Văn Phòng A',
        phone: '0909998888',
        streetAddress: '456 Nguyễn Huệ',
        ward: 'Phường Bến Thành',
        district: 'Quận 1',
        province: 'TP.HCM',
        isDefault: false,
      },
    ],
  };

  const mockOrders = [
    {
      id: 1001,
      orderCode: 'ORD-20260830-001',
      customerId: 10,
      customerName: 'Nguyễn Văn A',
      warehouseName: 'Tổng Kho TP.HCM',
      status: OrderStatus.Completed,
      paymentStatus: PaymentStatus.Paid,
      paymentMethod: PaymentMethod.COD,
      totalAmount: 500000,
      trackingCode: 'GHN-123456',
      orderDate: '2026-08-30T10:00:00Z',
      createdAt: '2026-08-30T10:00:00Z',
    },
    {
      id: 1002,
      orderCode: 'ORD-20260831-002',
      customerId: 10,
      customerName: 'Nguyễn Văn A',
      warehouseName: 'Tổng Kho TP.HCM',
      status: OrderStatus.Shipping,
      paymentStatus: PaymentStatus.Unpaid,
      paymentMethod: PaymentMethod.COD,
      totalAmount: 1000000,
      trackingCode: 'GHN-789012',
      orderDate: '2026-08-31T14:00:00Z',
      createdAt: '2026-08-31T14:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerApi.getById as any).mockResolvedValue(mockCustomer);
    (orderApi.getAll as any).mockResolvedValue({ items: mockOrders, totalRecords: 2 });
  });

  const renderComponent = () =>
    render(
      <MemoryRouter initialEntries={['/customers/10']}>
        <Routes>
          <Route path="/customers/:id" element={<CustomerDetail />} />
        </Routes>
      </MemoryRouter>
    );

  // #region TC01: RENDER THÔNG TIN HỒ SƠ KHÁCH HÀNG & TIẾN ĐỘ HẠNG
  it('TC01 - Render thông tin chi tiết khách hàng và Widget thăng hạng thành viên', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
      expect(screen.getAllByText('CUST-001').length).toBeGreaterThan(0);
      expect(screen.getAllByText('0901234567').length).toBeGreaterThan(0);
      expect(screen.getByText('nguyenvana@gmail.com')).toBeInTheDocument();
      expect(screen.getAllByText('Hạng Bạc').length).toBeGreaterThan(0);
      expect(screen.getByText(/Chiết khấu -5%/)).toBeInTheDocument();
      expect(screen.getByText(/3 đơn/)).toBeInTheDocument();
      expect(screen.getByText(/67%/)).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: CHUYỂN TAB SỔ ĐỊA CHỈ & HIỂN THỊ DANH SÁCH ĐỊA CHỈ
  it('TC02 - Chuyển sang Tab Sổ địa chỉ giao hàng và hiển thị đúng địa chỉ chi tiết', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    const addressTabBtn = screen.getByRole('button', { name: /SỔ ĐỊA CHỈ GIAO HÀNG/i });
    fireEvent.click(addressTabBtn);

    await waitFor(() => {
      expect(screen.getByText('123 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('456 Nguyễn Huệ, Phường Bến Thành, Quận 1, TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('Văn Phòng A')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: ĐẶT ĐỊA CHỈ LÀM MẶC ĐỊNH
  it('TC03 - Đặt một địa chỉ phụ làm địa chỉ giao hàng mặc định', async () => {
    (customerAddressApi.setDefault as any).mockResolvedValue({});

    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole('button', { name: /SỔ ĐỊA CHỈ GIAO HÀNG/i }));

    await waitFor(() => {
      expect(screen.getByTitle('Đặt làm mặc định')).toBeInTheDocument();
    });

    const setDefaultBtn = screen.getByTitle('Đặt làm mặc định');
    fireEvent.click(setDefaultBtn);

    await waitFor(() => {
      expect(customerAddressApi.setDefault).toHaveBeenCalledWith(102, 10);
    });
  });
  // #endregion

  // #region TC04: XÓA ĐỊA CHỈ VỚI MODAL XÁC NHẬN
  it('TC04 - Mở modal xác nhận và xóa địa chỉ giao hàng', async () => {
    (customerAddressApi.delete as any).mockResolvedValue({});

    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getByRole('button', { name: /SỔ ĐỊA CHỈ GIAO HÀNG/i }));

    await waitFor(() => {
      expect(screen.getAllByTitle('Xóa').length).toBeGreaterThan(0);
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    // Modal xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(customerAddressApi.delete).toHaveBeenCalledWith(101);
    });
  });
  // #endregion

  // #region TC05: CHUYỂN TAB LỊCH SỬ MUA HÀNG & HIỂN THỊ ĐƠN HÀNG
  it('TC05 - Chuyển sang Tab Lịch sử mua hàng và hiển thị danh sách đơn hàng đã đặt', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    const historyTabBtn = screen.getByRole('button', { name: /LỊCH SỬ MUA HÀNG/i });
    fireEvent.click(historyTabBtn);

    await waitFor(() => {
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260831-002')).toBeInTheDocument();
      expect(screen.getByText(/GHN-123456/)).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC06: ĐIỀU HƯỚNG QUAY LẠI DANH SÁCH KHÁCH HÀNG
  it('TC06 - Bấm nút Quay lại điều hướng về route /customers', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    const backBtn = screen.getByRole('button', { name: /QUAY LẠI/i });
    fireEvent.click(backBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/customers');
  });
  // #endregion
});
