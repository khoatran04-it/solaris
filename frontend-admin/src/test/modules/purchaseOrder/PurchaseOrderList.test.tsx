import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import PurchaseOrderList from '../../../pages/purchaseOrder/PurchaseOrderList';
import { purchaseOrderApi } from '../../../api/purchaseOrderApi';
import { supplierApi } from '../../../api/supplierApi';
import { PurchaseOrderStatus } from '../../../types/purchaseOrder';

// Mock APIs
vi.mock('../../../api/purchaseOrderApi', () => ({
  purchaseOrderApi: {
    getAll: vi.fn(),
    getAllList: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    updateStatus: vi.fn(),
    delete: vi.fn(),
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
 * MODULE 09: PURCHASING & PURCHASE ORDER MANAGEMENT
 * COMPONENT TEST: PurchaseOrderList (Danh Sách Đơn Đặt Mua Hàng)
 * ============================================================================
 */
describe('Module 09 - PurchaseOrderList Component', () => {
  const mockSuppliers = [
    { id: 1, name: 'Nông Trại Đà Lạt GAP', code: 'SUP-DALAT' },
    { id: 2, name: 'Hợp Tác Xã Mộc Châu', code: 'SUP-MOCCHAU' },
  ];

  const mockOrders = [
    {
      id: 1,
      orderCode: 'PO-20260830-001',
      orderDate: '2026-08-30T08:00:00Z',
      expectedDeliveryDate: '2026-09-02T08:00:00Z',
      status: PurchaseOrderStatus.Draft,
      totalAmount: 5000000,
      note: 'Giao hàng buổi sáng',
      supplierId: 1,
      supplierName: 'Nông Trại Đà Lạt GAP',
      createdById: 10,
      createdByName: 'Nguyễn Thu Mua',
      createdAt: '2026-08-30T08:00:00Z',
      updatedAt: '2026-08-30T08:00:00Z',
      details: [],
    },
    {
      id: 2,
      orderCode: 'PO-20260830-002',
      orderDate: '2026-08-30T09:00:00Z',
      expectedDeliveryDate: '2026-09-05T08:00:00Z',
      status: PurchaseOrderStatus.Approved,
      totalAmount: 12000000,
      note: 'Đã ký hợp đồng',
      supplierId: 2,
      supplierName: 'Hợp Tác Xã Mộc Châu',
      createdById: 10,
      createdByName: 'Nguyễn Thu Mua',
      createdAt: '2026-08-30T09:00:00Z',
      updatedAt: '2026-08-30T09:00:00Z',
      details: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierApi.getAllList as any).mockResolvedValue(mockSuppliers);
    (purchaseOrderApi.getAll as any).mockResolvedValue({
      items: mockOrders,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH ĐƠN MUA HÀNG
  it('TC01 - Render danh sách đơn mua hàng với mã PO, nhà cung cấp, trạng thái, tổng tiền và người tạo', async () => {
    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    expect(screen.getByText('Đơn Mua Hàng')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('PO-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('Nông Trại Đà Lạt GAP')).toBeInTheDocument();
      expect(screen.getByText('Nháp')).toBeInTheDocument();

      expect(screen.getByText('PO-20260830-002')).toBeInTheDocument();
      expect(screen.getByText('Hợp Tác Xã Mộc Châu')).toBeInTheDocument();
      expect(screen.getByText('Đã duyệt')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM ĐƠN HÀNG THEO TỪ KHÓA
  it('TC02 - Tìm kiếm đơn mua hàng theo từ khóa với Debounced search', async () => {
    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText(
      'Tìm kiếm theo mã đơn, nhà cung cấp, ghi chú...'
    );
    fireEvent.change(searchInput, { target: { value: 'PO-20260830-001' } });

    await waitFor(
      () => {
        expect(purchaseOrderApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'PO-20260830-001',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: XÓA ĐƠN HÀNG Ở TRẠNG THÁI DRAFT
  it('TC03 - Mở modal xác nhận và xóa thành công đơn hàng ở trạng thái Nháp', async () => {
    (purchaseOrderApi.delete as any).mockResolvedValue({ message: 'Xóa thành công' });

    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PO-20260830-001')).toBeInTheDocument();
    });

    const deleteBtn = screen.getByTitle('Xóa');
    fireEvent.click(deleteBtn);

    // Modal xác nhận xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(purchaseOrderApi.delete).toHaveBeenCalledWith(1);
      expect(purchaseOrderApi.getAll).toHaveBeenCalledTimes(2);
    });
  });
  // #endregion

  // #region TC04: KHÔNG HIỂN THỊ NÚT SỬA/XÓA VỚI ĐƠN HÀNG ĐÃ DUYỆT
  it('TC04 - Chỉ hiển thị nút Sửa/Xóa đối với đơn ở trạng thái Nháp (Draft)', async () => {
    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PO-20260830-001')).toBeInTheDocument();
      expect(screen.getByText('PO-20260830-002')).toBeInTheDocument();
    });

    // Chỉ có 1 nút Chỉnh sửa và 1 nút Xóa (cho đơn PO-20260830-001 vì đơn thứ 2 là Approved)
    const editButtons = screen.getAllByTitle('Chỉnh sửa');
    const deleteButtons = screen.getAllByTitle('Xóa');
    const viewButtons = screen.getAllByTitle('Xem chi tiết');

    expect(editButtons).toHaveLength(1);
    expect(deleteButtons).toHaveLength(1);
    expect(viewButtons).toHaveLength(2);
  });
  // #endregion

  // #region TC05: ĐIỀU HƯỚNG TẠO MỚI & XEM CHI TIẾT
  it('TC05 - Điều hướng chính xác khi nhấn Tạo đơn mới và Xem chi tiết', async () => {
    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('PO-20260830-001')).toBeInTheDocument();
    });

    // Nút Thêm mới
    const addBtn = screen.getByRole('button', { name: /THÊM/i });
    fireEvent.click(addBtn);
    expect(mockNavigate).toHaveBeenCalledWith('/purchase-orders/create');

    // Nút Xem chi tiết
    const viewButtons = screen.getAllByTitle('Xem chi tiết');
    fireEvent.click(viewButtons[0]);
    expect(mockNavigate).toHaveBeenCalledWith('/purchase-orders/1');
  });
  // #endregion

  // #region TC06: HIỂN THỊ KHI DANH SÁCH RỖNG
  it('TC06 - Hiển thị giao diện rỗng khi không tìm thấy đơn mua hàng nào', async () => {
    (purchaseOrderApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <PurchaseOrderList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(
        screen.getByText(/Thử thay đổi từ khóa tìm kiếm hoặc điều kiện lọc/i)
      ).toBeInTheDocument();
    });
  });
  // #endregion
});
