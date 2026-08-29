import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter } from 'react-router-dom';
import CustomerList from '../../../pages/customer/CustomerList';
import { customerApi } from '../../../api/customerApi';
import { customerTypeApi } from '../../../api/customerTypeApi';
import { customerTierApi } from '../../../api/customerTierApi';
import { customerGroupApi } from '../../../api/customerGroupApi';

// Mock APIs
vi.mock('../../../api/customerApi', () => ({
  customerApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/customerTypeApi', () => ({
  customerTypeApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/customerTierApi', () => ({
  customerTierApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/customerGroupApi', () => ({
  customerGroupApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 6: CUSTOMER MASTER DATA
 * 🧪 COMPONENT TEST: CustomerList (Danh sách Hồ Sơ Khách Hàng)
 * ============================================================================
 */
describe('Module 06 - CustomerList Component', () => {
  const mockTypes = [{ id: 1, name: 'Khách sỉ' }];
  const mockTiers = [{ id: 1, name: 'Hạng Vàng' }];
  const mockGroups = [{ id: 1, name: 'Khách VIP' }];

  const mockCustomers = [
    {
      id: 1,
      code: 'KH001',
      name: 'Công ty Minh Long',
      phoneNumber: '0909123456',
      email: 'minhlong@gmail.com',
      customerTypeId: 1,
      customerTypeName: 'Khách sỉ',
      customerTierId: 1,
      customerTierName: 'Hạng Vàng',
      groups: ['Khách VIP'],
      isActive: true,
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'KH002',
      name: 'Nguyễn Văn Nam',
      phoneNumber: '0988776655',
      email: 'nam@gmail.com',
      customerTypeId: 1,
      customerTypeName: 'Khách sỉ',
      customerTierId: 1,
      customerTierName: 'Hạng Vàng',
      groups: [],
      isActive: false,
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (customerTypeApi.getAllList as any).mockResolvedValue(mockTypes);
    (customerTierApi.getAllList as any).mockResolvedValue(mockTiers);
    (customerGroupApi.getAllList as any).mockResolvedValue(mockGroups);
    (customerApi.getAll as any).mockResolvedValue({
      items: mockCustomers,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH KHÁCH HÀNG
  it('TC01 - Render danh sách khách hàng với đầy đủ thông tin hồ sơ', async () => {
    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Công ty Minh Long')).toBeInTheDocument();
      expect(screen.getByText('KH001')).toBeInTheDocument();
      expect(screen.getByText('0909123456')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn Nam')).toBeInTheDocument();
      expect(screen.getByText('KH002')).toBeInTheDocument();
    });

    expect(customerApi.getAll).toHaveBeenCalled();
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  it('TC02 - Tìm kiếm khách hàng theo mã, tên hoặc số điện thoại', async () => {
    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KH001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/Tìm kiếm theo mã, tên, số điện thoại.../i);
    fireEvent.change(searchInput, { target: { value: 'Minh Long' } });

    await waitFor(
      () => {
        expect(customerApi.getAll).toHaveBeenCalledWith(
          expect.objectContaining({
            search: 'Minh Long',
          })
        );
      },
      { timeout: 1000 }
    );
  });
  // #endregion

  // #region TC03: LỌC THEO PHÂN LOẠI & BẬC HẠNG
  it('TC03 - Mở bộ lọc phân loại khách hàng và tải danh sách types', async () => {
    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KH001')).toBeInTheDocument();
    });

    const filterButtons = screen.getAllByRole('button');
    const typeFilterBtn = filterButtons.find((btn) => btn.textContent?.includes('LOẠI KHÁCH HÀNG'));
    if (typeFilterBtn) {
      fireEvent.click(typeFilterBtn);
    }

    await waitFor(() => {
      expect(customerTypeApi.getAllList).toHaveBeenCalled();
    });
  });
  // #endregion

  // #region TC04: THAY ĐỔI TRẠNG THÁI (TOGGLE ACTIVE)
  it('TC04 - Đổi trạng thái hoạt động của tài khoản khách hàng', async () => {
    (customerApi.toggleActive as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KH001')).toBeInTheDocument();
    });

    const activeToggle = screen.getAllByRole('button', { name: /Hoạt động/i })[0];
    fireEvent.click(activeToggle);

    await waitFor(() => {
      expect(customerApi.toggleActive).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC05: XÓA KHÁCH HÀNG CÓ XÁC NHẬN MODAL
  it('TC05 - Mở modal xác nhận xóa và gọi API delete', async () => {
    (customerApi.delete as any).mockResolvedValue({});

    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('KH001')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });

    const confirmBtn = screen.getByRole('button', { name: /Xóa ngay/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(customerApi.delete).toHaveBeenCalledWith(1);
    });
  });
  // #endregion

  // #region TC06: PHÂN TRANG VÀ HIỂN THỊ TỔNG SỐ BẢN GHI
  it('TC06 - Hiển thị đúng số lượng tổng bản ghi và phân trang', async () => {
    render(
      <MemoryRouter>
        <CustomerList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText(/Hiển thị/i)).toHaveTextContent('2');
    });
  });
  // #endregion
});
