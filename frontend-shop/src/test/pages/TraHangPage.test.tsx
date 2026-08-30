import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import TraHangPage from '@/app/tai-khoan/tra-hang/page';
import { useAuthStore } from '@/stores/authStore';
import shopOrderApi from '@/api/shopOrderApi';
import shopReturnApi from '@/api/shopReturnApi';

const mockPush = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useSearchParams: () => ({
    get: () => '',
  }),
}));

vi.mock('@/api/shopOrderApi', () => ({
  default: {
    getByCode: vi.fn(),
  },
}));

vi.mock('@/api/shopReturnApi', () => ({
  default: {
    getAll: vi.fn(),
    create: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 🛒 FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * 🧪 PAGE TEST: TraHangPage (Đổi Trả Hàng & Hoàn Tiền RMA)
 * ============================================================================
 */
describe('Module 13 - TraHangPage Component', () => {
  const mockReturns = [
    {
      id: 1,
      returnCode: 'RET-20260830-001',
      orderCode: 'ORD-20260830-001',
      returnDate: '2026-08-30T09:00:00Z',
      status: 1,
      statusName: 'Chờ tiếp nhận',
      refundAmount: 150000,
      reason: 'Trái cây bị dập khi vận chuyển',
      inspectionNotes: '',
      details: [],
    },
  ];

  const mockOrder = {
    id: 1,
    orderCode: 'ORD-20260830-001',
    orderDate: '2026-08-30T08:00:00Z',
    status: 5,
    statusName: 'Giao thành công',
    totalAmount: 300000,
    items: [
      {
        detailId: 1,
        variantId: 10,
        variantName: 'Xoài Cát Hòa Lộc',
        uoMId: 1,
        uoMName: 'Hộp 1kg',
        quantity: 2,
        unitPrice: 150000,
        totalPrice: 300000,
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ isAuthenticated: true, user: null, token: 'mock-token' });
    (shopReturnApi.getAll as any).mockResolvedValue({
      items: mockReturns,
      totalRecords: 1,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
    (shopOrderApi.getByCode as any).mockResolvedValue(mockOrder);
    (shopReturnApi.create as any).mockResolvedValue({ id: 10, returnCode: 'RET-010' });
  });

  // TC01: CHUYỂN HƯỚNG NẾU CHƯA ĐĂNG NHẬP
  it('TC01 - Chưa đăng nhập: chuyển hướng sang /dang-nhap?redirect=/tai-khoan/tra-hang', async () => {
    useAuthStore.setState({ isAuthenticated: false });

    render(<TraHangPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/dang-nhap?redirect=/tai-khoan/tra-hang');
    });
  });

  // TC02: RENDER DANH SÁCH YÊU CẦU ĐỔI TRẢ
  it('TC02 - Render danh sách yêu cầu đổi trả với mã phiếu, đơn gốc, badge trạng thái và số tiền hoàn', async () => {
    render(<TraHangPage />);

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('Chờ tiếp nhận')).toBeInTheDocument();
      expect(screen.getByText('150.000 ₫')).toBeInTheDocument();
    });
  });

  // TC03: HIỂN THỊ THÔNG BÁO RỖNG KHI CHƯA CÓ YÊU CẦU NÀO
  it('TC03 - Hiển thị thông báo khi chưa có yêu cầu đổi trả nào', async () => {
    (shopReturnApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(<TraHangPage />);

    await waitFor(() => {
      expect(screen.getByText('Bạn chưa có yêu cầu đổi trả hàng nào.')).toBeInTheDocument();
    });
  });

  // TC04: MỞ FORM TẠO YÊU CẦU MỚI, TÌM ĐƠN GỐC VÀ GỬI YÊU CẦU ĐỔI TRẢ
  it('TC04 - Bấm "Tạo yêu cầu mới", tìm kiếm đơn gốc, nhập lý do và submit shopReturnApi.create', async () => {
    render(<TraHangPage />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Tạo yêu cầu mới/i })).toBeInTheDocument();
    });

    // 1. Mở form
    const createBtn = screen.getByRole('button', { name: /Tạo yêu cầu mới/i });
    fireEvent.click(createBtn);

    expect(screen.getByText('Tạo Phiếu Đổi/Trả Hàng Nông Sản')).toBeInTheDocument();

    // 2. Nhập mã đơn hàng và bấm kiểm tra
    const orderInput = screen.getByPlaceholderText(/Nhập mã đơn hàng/i);
    fireEvent.change(orderInput, { target: { value: 'ORD-20260830-001' } });

    const checkOrderBtn = screen.getByRole('button', { name: /Kiểm tra đơn/i });
    fireEvent.click(checkOrderBtn);

    await waitFor(() => {
      expect(shopOrderApi.getByCode).toHaveBeenCalledWith('ORD-20260830-001');
      expect(screen.getByText('Xoài Cát Hòa Lộc')).toBeInTheDocument();
    });

    // 3. Nhập lý do chung
    const reasonTextarea = screen.getByPlaceholderText(/Mô tả cụ thể tình trạng hàng hóa/i);
    fireEvent.change(reasonTextarea, { target: { value: 'Xoài bị dập và chín quá mức' } });

    // 4. Submit form
    const submitBtn = screen.getByRole('button', { name: /Gửi yêu cầu trả hàng/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shopReturnApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          orderCode: 'ORD-20260830-001',
          reason: 'Xoài bị dập và chín quá mức',
          items: expect.arrayContaining([
            expect.objectContaining({
              variantId: 10,
              returnedQuantity: 2,
            }),
          ]),
        })
      );
    });
  });
});
