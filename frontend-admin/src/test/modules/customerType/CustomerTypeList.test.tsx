import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerTypeList from '../../../pages/customerType/CustomerTypeList';
import { customerTypeApi } from '../../../api/customerTypeApi';

// Mock API
vi.mock('../../../api/customerTypeApi', () => ({
  customerTypeApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 6: CUSTOMER MASTER DATA
 * 🧪 COMPONENT TEST: CustomerTypeList (Danh sách Phân loại Khách hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerTypeList Component', () => {
  const mockCustomerTypes = [
    {
      id: 1,
      code: 'SI',
      name: 'Khách sỉ',
      description: 'Bán buôn số lượng lớn',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'HORECA',
      name: 'Khách sạn & Nhà hàng',
      description: 'Khách tiêu thụ định kỳ',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerTypeApi.getAll as any).mockResolvedValue({
      items: mockCustomerTypes,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH PHÂN LOẠI
  it('TC01 - Render danh sách phân loại khách hàng với đầy đủ thông tin', async () => {
    render(
      <MemoryRouter>
        <CustomerTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Khách sỉ')).toBeInTheDocument();
      expect(screen.getByText('SI')).toBeInTheDocument();
      expect(screen.getByText('Khách sạn & Nhà hàng')).toBeInTheDocument();
      expect(screen.getByText('HORECA')).toBeInTheDocument();
    });

    expect(customerTypeApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên phân loại', async () => {
    render(
      <MemoryRouter>
        <CustomerTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SI')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên phân loại.../i);
    fireEvent.change(searchInput, { target: { value: 'Khách sỉ' } });

    await waitFor(
      () => {
        expect(customerTypeApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Khách sỉ',
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
        <CustomerTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SI')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const statusFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('TRẠNG THÁI'));
    if (statusFilterBtn) {
      fireEvent.click(statusFilterBtn);
    }

    await waitFor(() => {
      expect(customerTypeApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của phân loại khách hàng', async () => {
    (customerTypeApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SI')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(customerTypeApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA PHÂN LOẠI CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (customerTypeApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerTypeList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('SI')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(customerTypeApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
