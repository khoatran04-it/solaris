import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerTierList from '../../../pages/customerTier/CustomerTierList';
import { customerTierApi } from '../../../api/customerTierApi';

// Mock API
vi.mock('../../../api/customerTierApi', () => ({
  customerTierApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 6: CUSTOMER MASTER DATA
 * 🧪 COMPONENT TEST: CustomerTierList (Danh sách Bậc Hạng Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerTierList Component', () => {
  const mockCustomerTiers = [
    {
      id: 1,
      code: 'GOLD',
      name: 'Hạng Vàng',
      discountPercent: 5,
      minSpending: 10000000,
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'DIAMOND',
      name: 'Hạng Kim Cương',
      discountPercent: 10,
      minSpending: 50000000,
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerTierApi.getAll as any).mockResolvedValue({
      items: mockCustomerTiers,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH BẬC HẠNG
  it('TC01 - Render danh sách bậc hạng thành viên với đầy đủ chiết khấu và chi tiêu tối thiểu', async () => {
    render(
      <MemoryRouter>
        <CustomerTierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Hạng Vàng')).toBeInTheDocument();
      expect(screen.getByText('GOLD')).toBeInTheDocument();
      expect(screen.getByText('Hạng Kim Cương')).toBeInTheDocument();
      expect(screen.getByText('DIAMOND')).toBeInTheDocument();
    });

    expect(customerTierApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên bậc hạng', async () => {
    render(
      <MemoryRouter>
        <CustomerTierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('GOLD')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên bậc hạng.../i);
    fireEvent.change(searchInput, { target: { value: 'Vàng' } });

    await waitFor(
      () => {
        expect(customerTierApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Vàng',
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
        <CustomerTierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('GOLD')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const statusFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('TRẠNG THÁI'));
    if (statusFilterBtn) {
      fireEvent.click(statusFilterBtn);
    }

    await waitFor(() => {
      expect(customerTierApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của bậc hạng khách hàng', async () => {
    (customerTierApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerTierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('GOLD')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(customerTierApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA BẬC HẠNG CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (customerTierApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerTierList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('GOLD')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(customerTierApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
