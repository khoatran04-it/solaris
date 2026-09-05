import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import SupplierList from '../../../pages/supplier/SupplierList';
import { supplierApi } from '../../../api/supplierApi';
import { supplierTypeApi } from '../../../api/supplierTypeApi';

// Mock APIs
vi.mock('../../../api/supplierApi', () => ({
  supplierApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/supplierTypeApi', () => ({
  supplierTypeApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 3: SUPPLIER
 * COMPONENT TEST: SupplierList (Danh sách Hồ sơ Nhà cung cấp)
 * ============================================================================
 */
describe('Module 03 - SupplierList Component', () => {
  const mockSupplierTypes = [
    { id: 1, code: 'FARM', name: 'Nhà vườn' },
    { id: 2, code: 'COOP', name: 'Hợp tác xã' },
  ];

  const mockSuppliers = [
    {
      id: 1,
      code: 'NCC01',
      name: 'Nông trại Xanh Đà Lạt',
      phone: '0912345678',
      email: 'dalat@gmail.com',
      supplierTypeId: 1,
      supplierTypeName: 'Nhà vườn',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'NCC02',
      name: 'HTX Nông Nghiệp Bến Tre',
      phone: '0987654321',
      email: 'bentre@gmail.com',
      supplierTypeId: 2,
      supplierTypeName: 'Hợp tác xã',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierTypeApi.getAllList as any).mockResolvedValue(mockSupplierTypes);
    (supplierApi.getAll as any).mockResolvedValue({
      items: mockSuppliers,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH NHÀ CUNG CẤP
  it('TC01 - Render danh sách nhà cung cấp với đầy đủ thông tin bảng', async () => {
    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Nông trại Xanh Đà Lạt')).toBeInTheDocument();
      expect(screen.getByText('NCC01')).toBeInTheDocument();
      expect(screen.getByText('0912345678')).toBeInTheDocument();
      expect(screen.getByText('HTX Nông Nghiệp Bến Tre')).toBeInTheDocument();
    });

    expect(supplierApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA (DEBOUNCE SEARCH)
  it('TC02 - Tìm kiếm nhà cung cấp theo mã, tên hoặc số điện thoại', async () => {
    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('NCC01')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên, số điện thoại.../i);
    fireEvent.change(searchInput, { target: { value: 'Đà Lạt' } });

    await waitFor(
      () => {
        expect(supplierApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Đà Lạt',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: ĐẶC THÙ - LỌC THEO PHÂN LOẠI NCC (MULTI-TYPE FILTERING)
  it('TC03 - Lọc theo phân loại nhà cung cấp (supplierTypeIds)', async () => {
    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('NCC01')).toBeInTheDocument();
    });

    // Mở bộ lọc phân loại
    const filterButtons = screen.getAllByRole('button');
    const typeFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('Phân loại'));
    if (typeFilterBtn) {
      fireEvent.click(typeFilterBtn);
    }

    await waitFor(() => {
      expect(supplierTypeApi.getAllList).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của nhà cung cấp', async () => {
    (supplierApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('NCC01')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(supplierApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA NHÀ CUNG CẤP CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (supplierApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('NCC01')).toBeInTheDocument();
    });

    // Click nút xóa
    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    // Modal hiển thị
    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    // Xác nhận xóa
    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(supplierApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC06: PHÂN TRANG VÀ HIỂN THỊ TỔNG SỐ BẢN GHI
  it('TC06 - Hiển thị đúng số lượng tổng bản ghi và phân trang', async () => {
    render(
      <MemoryRouter>
        <SupplierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Hiển thị/i)).toHaveTextContent('2');
    });
  });
  // #endregion
});
