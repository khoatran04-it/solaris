import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryIssueList from '../../../pages/inventoryIssue/InventoryIssueList';
import { inventoryIssueApi } from '../../../api/inventoryIssueApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { InventoryIssueStatus } from '../../../types/inventoryIssue';

// Mock APIs
vi.mock('../../../api/inventoryIssueApi', () => ({
  inventoryIssueApi: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    complete: vi.fn(),
    cancel: vi.fn(),
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
 * MODULE 10: INVENTORY ISSUES & FEFO SMART PICKER
 * COMPONENT TEST: InventoryIssueList (Danh Sách Phiếu Xuất Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryIssueList Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' }];

  const mockIssues = [
    {
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
      issueDate: '2026-08-30T08:00:00Z',
      note: 'Xuất giao nội thành',
      createdAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      issueCode: 'ISS-20260830-002',
      orderId: 11,
      orderCode: 'ORD-20260830-002',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      status: InventoryIssueStatus.Completed,
      issuedById: 10,
      issuedByName: 'Trần Nhặt Hàng',
      receiverName: 'Lê Thị Mua',
      issueDate: '2026-08-30T09:00:00Z',
      note: 'Đã giao cho bưu tá',
      createdAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (inventoryIssueApi.getAll as any).mockResolvedValue({
      items: mockIssues,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER DANH SÁCH PHIẾU XUẤT
  it('TC01 - Render danh sách phiếu xuất kho với mã phiếu, đơn hàng, kho xuất, người nhận và trạng thái', async () => {
    render(
      <MemoryRouter>
        <InventoryIssueList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryIssueApi.getAll).toHaveBeenCalled();
    });

    expect(await screen.findByText('ISS-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('ISS-20260830-002')).toBeInTheDocument();
    expect(screen.getByText('ORD-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('Nguyễn Văn Khách')).toBeInTheDocument();
    expect(screen.getByText('Chờ xử lý')).toBeInTheDocument();
    expect(screen.getByText('Đã xuất kho')).toBeInTheDocument();
  });

  // TC02: TÌM KIẾM THEO MÃ HOẶC NGƯỜI NHẬN
  it('TC02 - Nhập từ khóa tìm kiếm kích hoạt debounce API call', async () => {
    render(
      <MemoryRouter>
        <InventoryIssueList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã phiếu, người nhận/i);
    fireEvent.change(searchInput, { target: { value: 'Khách' } });

    await waitFor(
      () => {
        expect(inventoryIssueApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({ search: 'Khách' })
        );
      },
      { timeout: 1500 }
    );
  });

  // TC03: ĐIỀU HƯỚNG XEM CHI TIẾT
  it('TC03 - Bấm nút xem chi tiết điều hướng đến trang chi tiết phiếu xuất kho', async () => {
    render(
      <MemoryRouter>
        <InventoryIssueList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByTitle('Xem chi tiết');
    fireEvent.click(viewButtons[0]);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-issues/1');
  });

  // TC04: MỞ MODAL XÓA VÀ GỌI API DELETE
  it('TC04 - Xác nhận xóa phiếu xuất kho chưa hoàn tất gọi API delete', async () => {
    (inventoryIssueApi.delete as any).mockResolvedValue({ message: 'Xóa thành công' });

    render(
      <MemoryRouter>
        <InventoryIssueList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('ISS-20260830-001')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa phiếu chờ');
    fireEvent.click(deleteButtons[0]);

    expect(screen.getByRole('heading', { name: /Xóa Phiếu Xuất Kho/i })).toBeInTheDocument();

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(inventoryIssueApi.delete).toHaveBeenCalledWith(1);
    });
  });
});
