import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerGroupList from '../../../pages/customerGroup/CustomerGroupList';
import { customerGroupApi } from '../../../api/customerGroupApi';

// Mock API
vi.mock('../../../api/customerGroupApi', () => ({
  customerGroupApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * MODULE 6: CUSTOMER MASTER DATA
 * COMPONENT TEST: CustomerGroupList (Danh sách Nhóm Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerGroupList Component', () => {
  const mockCustomerGroups = [
    {
      id: 1,
      code: 'VIP',
      name: 'Khách hàng VIP',
      description: 'Top 10% doanh số',
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'NEW',
      name: 'Khách hàng mới',
      description: 'Chưa phát sinh đơn hàng thứ 2',
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerGroupApi.getAll as any).mockResolvedValue({
      items: mockCustomerGroups,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH NHÓM
  it('TC01 - Render danh sách nhóm khách hàng với đầy đủ thông tin', async () => {
    render(
      <MemoryRouter>
        <CustomerGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Khách hàng VIP')).toBeInTheDocument();
      expect(screen.getByText('VIP')).toBeInTheDocument();
      expect(screen.getByText('Khách hàng mới')).toBeInTheDocument();
      expect(screen.getByText('NEW')).toBeInTheDocument();
    });

    expect(customerGroupApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm theo từ khóa mã hoặc tên nhóm khách hàng', async () => {
    render(
      <MemoryRouter>
        <CustomerGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VIP')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên nhóm.../i);
    fireEvent.change(searchInput, { target: { value: 'VIP' } });

    await waitFor(
      () => {
        expect(customerGroupApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'VIP',
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
        <CustomerGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VIP')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const statusFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('TRẠNG THÁI'));
    if (statusFilterBtn) {
      fireEvent.click(statusFilterBtn);
    }

    await waitFor(() => {
      expect(customerGroupApi.getAll).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của nhóm khách hàng', async () => {
    (customerGroupApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VIP')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(customerGroupApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA NHÓM CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (customerGroupApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerGroupList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('VIP')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(customerGroupApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion
});
