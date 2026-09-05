import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import SupplierTypeList from '../../../pages/supplierType/SupplierTypeList';
import { supplierTypeApi } from '../../../api/supplierTypeApi';

// Mock API
vi.mock('../../../api/supplierTypeApi', () => ({
  supplierTypeApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 3: SUPPLIER
 * COMPONENT TEST: SupplierTypeList (Danh sách Phân loại Nhà cung cấp)
 * ============================================================================
 */
describe('Module 03 - SupplierTypeList Component', () => {
  const mockSupplierTypes = [
    {
      id: 1,
      code: 'FARM',
      name: 'Nhà vườn / Nông trại',
      description: 'Cung cấp nông sản trực tiếp',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'IMPORT',
      name: 'Công ty Nhập khẩu',
      description: 'Nhập khẩu trái cây cao cấp',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (supplierTypeApi.getAll as any).mockResolvedValue({
      items: mockSupplierTypes,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH PHÂN LOẠI
  it('TC01 - Render danh sách phân loại nhà cung cấp với đầy đủ thông tin', async () => {
    render(
      <MemoryRouter>
        <SupplierTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Nhà vườn / Nông trại')).toBeInTheDocument();
      expect(screen.getByText('FARM')).toBeInTheDocument();
      expect(screen.getByText('Công ty Nhập khẩu')).toBeInTheDocument();
      expect(screen.getByText('IMPORT')).toBeInTheDocument();
    });

    expect(supplierTypeApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên phân loại', async () => {
    render(
      <MemoryRouter>
        <SupplierTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FARM')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên phân loại.../i);
    fireEvent.change(searchInput, { target: { value: 'Nhà vườn' } });

    await waitFor(
      () => {
        expect(supplierTypeApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Nhà vườn',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO TRẠNG THÁI HOẠT ĐỘNG
  it('TC03 - Lọc danh sách theo trạng thái Hoạt động / Tạm khóa', async () => {
    render(
      <MemoryRouter>
        <SupplierTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FARM')).toBeInTheDocument();
    });

    // Mở bộ lọc trạng thái và chọn "Hoạt động"
    const filterButtons = screen.getAllByRole('button');
    const statusFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('Trạng thái'));
    if (statusFilterBtn) {
      fireEvent.click(statusFilterBtn);
    }

    await waitFor(() => {
      expect(supplierTypeApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của phân loại NCC', async () => {
    (supplierTypeApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FARM')).toBeInTheDocument();
    });

    // Click toggle button của bản ghi đầu tiên
    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(supplierTypeApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA PHÂN LOẠI CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (supplierTypeApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <SupplierTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('FARM')).toBeInTheDocument();
    });

    // Click nút xóa bản ghi
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
      expect(supplierTypeApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
