import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerReturnList from '../../../pages/customerReturn/CustomerReturnList';
import { customerReturnApi } from '../../../api/customerReturnApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { CustomerReturnStatus } from '../../../types/customerReturn';

// Mock APIs
vi.mock('../../../api/customerReturnApi', () => ({
  customerReturnApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    inspectAndComplete: vi.fn(),
    reject: vi.fn(),
    delete: vi.fn(),
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
 * 🧪 COMPONENT TEST: CustomerReturnList (Danh Sách Phiếu Khách Hàng Trả Hàng)
 * ============================================================================
 */
describe('Module 13 - CustomerReturnList Component', () => {
  const mockWarehouses = [
    { id: 1, name: 'Kho Tổng TP.HCM', code: 'WH-HCM' },
    { id: 2, name: 'Kho Hà Nội', code: 'WH-HN' },
  ];

  const mockReturns = [
    {
      id: 1,
      returnCode: 'RET-20260830-001',
      orderId: 10,
      orderCode: 'ORD-20260830-001',
      customerId: 1,
      customerName: 'Nguyễn Văn A',
      warehouseId: 1,
      warehouseName: 'Kho Tổng TP.HCM',
      status: CustomerReturnStatus.Pending,
      refundAmount: 0,
      reason: 'Sản phẩm không đạt độ ngọt yêu cầu',
      returnDate: '2026-08-30T08:00:00Z',
      createdAt: '2026-08-30T08:00:00Z',
      updatedAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      returnCode: 'RET-20260830-002',
      orderId: 20,
      orderCode: 'ORD-20260830-002',
      customerId: 2,
      customerName: 'Trần Thị B',
      warehouseId: 2,
      warehouseName: 'Kho Hà Nội',
      status: CustomerReturnStatus.Completed,
      refundAmount: 180000,
      reason: 'Dập nát khi vận chuyển',
      returnDate: '2026-08-30T09:00:00Z',
      createdAt: '2026-08-30T09:00:00Z',
      updatedAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (customerReturnApi.getAll as any).mockResolvedValue({
      items: mockReturns,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: Render danh sách phiếu trả hàng
  it('TC01 - Render danh sách phiếu trả hàng với mã phiếu, đơn gốc, khách hàng, kho và trạng thái', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('RET-20260830-002')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('ORD-20260830-002')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
      expect(screen.getByText('Kho Tổng TP.HCM')).toBeInTheDocument();
      expect(screen.getByText('Kho Hà Nội')).toBeInTheDocument();
      expect(screen.getByText('Chờ tiếp nhận')).toBeInTheDocument();
      expect(screen.getByText('Đã hoàn tất')).toBeInTheDocument();
    });
  });

  // TC02: Hiển thị trạng thái rỗng
  it('TC02 - Hiển thị trạng thái rỗng khi không có phiếu trả hàng nào', async () => {
    (customerReturnApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Không tìm thấy dữ liệu/i)).toBeInTheDocument();
    });
  });

  // TC03: Tìm kiếm theo từ khóa
  it('TC03 - Tìm kiếm theo từ khóa và gọi API với tham số search', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã phiếu/i);
    fireEvent.change(searchInput, { target: { value: '001' } });

    await waitFor(
      () => {
        expect(customerReturnApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: '001',
          })
        );
      },
      { timeout: 1000 }
    );
  });

  // TC04: Điều hướng tạo phiếu trả hàng mới
  it('TC04 - Bấm nút "THÊM" điều hướng sang route /customer-returns/create', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
    });

    const createBtn = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(createBtn);

    expect(mockNavigate).toHaveBeenCalledWith('/customer-returns/create');
  });

  // TC05: Điều hướng xem chi tiết phiếu trả hàng
  it('TC05 - Bấm nút xem chi tiết điều hướng sang route /customer-returns/:id', async () => {
    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByTitle(/Xem chi tiết/i);
    fireEvent.click(viewButtons[0]);

    expect(mockNavigate).toHaveBeenCalledWith('/customer-returns/1');
  });

  // TC06: Mở modal xóa và xác nhận xóa phiếu trả hàng
  it('TC06 - Mở modal xác nhận xóa và gọi API delete khi xác nhận', async () => {
    (customerReturnApi.delete as any).mockResolvedValue({ message: 'Xóa phiếu trả hàng thành công' });

    render(
      <MemoryRouter>
        <CustomerReturnList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('RET-20260830-001')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle(/Xóa phiếu trả/i);
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Xóa Phiếu Trả Hàng/i)).toBeInTheDocument();
    });

    const confirmDeleteBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmDeleteBtn);

    await waitFor(() => {
      expect(customerReturnApi.delete).toHaveBeenCalledWith(1);
    });
  });
});
