import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import InventoryReceiptList from '../../../pages/inventoryReceipt/InventoryReceiptList';
import { inventoryReceiptApi } from '../../../api/inventoryReceiptApi';
import { warehouseApi } from '../../../api/warehouseApi';
import { supplierApi } from '../../../api/supplierApi';
import { InventoryReceiptStatus } from '../../../types/inventoryReceipt';

// Mock APIs
vi.mock('../../../api/inventoryReceiptApi', () => ({
  inventoryReceiptApi: {
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

vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
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
 * MODULE 10: INVENTORY RECEIPTS & QUALITY CONTROL (GRN)
 * COMPONENT TEST: InventoryReceiptList (Danh Sách Phiếu Nhập Kho)
 * ============================================================================
 */
describe('Module 10 - InventoryReceiptList Component', () => {
  const mockWarehouses = [{ id: 1, name: 'Tổng Kho Hà Nội', code: 'WH-HN-01' }];
  const mockSuppliers = [{ id: 1, name: 'Nông Trại Đà Lạt GAP', code: 'SUP-DALAT' }];

  const mockReceipts = [
    {
      id: 1,
      receiptCode: 'IR-20260830-001',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      supplierId: 1,
      supplierName: 'Nông Trại Đà Lạt GAP',
      status: InventoryReceiptStatus.Pending,
      receivedById: 10,
      receivedByName: 'Nguyễn Kiểm Đếm',
      receiptDate: '2026-08-30T08:00:00Z',
      note: 'Nhập đợt 1',
      createdAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      receiptCode: 'IR-20260830-002',
      warehouseId: 1,
      warehouseName: 'Tổng Kho Hà Nội',
      supplierId: 1,
      supplierName: 'Nông Trại Đà Lạt GAP',
      status: InventoryReceiptStatus.Completed,
      receivedById: 10,
      receivedByName: 'Nguyễn Kiểm Đếm',
      receiptDate: '2026-08-30T09:00:00Z',
      note: 'Đã kiểm tra đạt chuẩn',
      createdAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (inventoryReceiptApi.getAll as any).mockResolvedValue({
      items: mockReceipts,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: RENDER DANH SÁCH PHIẾU NHẬP KHO
  it('TC01 - Render bảng danh sách phiếu nhập kho với mã IR, kho, nhà cung cấp, người nhận và trạng thái', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(inventoryReceiptApi.getAll).toHaveBeenCalled();
    });

    expect(await screen.findByText('IR-20260830-001')).toBeInTheDocument();
    expect(screen.getByText('IR-20260830-002')).toBeInTheDocument();
    expect(screen.getAllByText('Tổng Kho Hà Nội').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Nông Trại Đà Lạt GAP').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Nguyễn Kiểm Đếm').length).toBeGreaterThan(0);
    expect(screen.getByText('Chờ xử lý')).toBeInTheDocument();
    expect(screen.getByText('Hoàn tất')).toBeInTheDocument();
  });

  // TC02: TÌM KIẾM THEO MÃ PHIẾU NHẬP
  it('TC02 - Nhập mã phiếu nhập kho kích hoạt tìm kiếm', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã phiếu/i);
    fireEvent.change(searchInput, { target: { value: 'IR-20260830-002' } });

    await waitFor(
      () => {
        expect(inventoryReceiptApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({ search: 'IR-20260830-002' })
        );
      },
      { timeout: 1500 }
    );
  });

  // TC03: ĐIỀU HƯỚNG SANG XEM CHI TIẾT
  it('TC03 - Bấm nút xem chi tiết điều hướng đến trang chi tiết phiếu nhập kho', async () => {
    render(
      <MemoryRouter>
        <InventoryReceiptList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByTitle('Xem chi tiết');
    fireEvent.click(viewButtons[0]);

    expect(mockNavigate).toHaveBeenCalledWith('/inventory-receipts/1');
  });

  // TC04: MỞ MODAL XÓA VÀ GỌI API DELETE
  it('TC04 - Mở modal xác nhận xóa phiếu nhập kho chưa hoàn tất và gọi API xóa', async () => {
    (inventoryReceiptApi.delete as any).mockResolvedValue({ message: 'Xóa thành công' });

    render(
      <MemoryRouter>
        <InventoryReceiptList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('IR-20260830-001')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa phiếu chờ');
    fireEvent.click(deleteButtons[0]);

    // Modal hiển thị
    expect(screen.getByText(/Xóa Phiếu Nhập Kho Chờ Xử Lý/i)).toBeInTheDocument();

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(inventoryReceiptApi.delete).toHaveBeenCalledWith(1);
    });
  });
});
