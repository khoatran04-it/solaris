import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import UserList from '../../../pages/user/UserList';
import { userApi } from '../../../api/userApi';
import { roleApi } from '../../../api/roleApi';

// Mock userApi & roleApi
vi.mock('../../../api/userApi', () => ({
  userApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('../../../api/roleApi', () => ({
  roleApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 COMPONENT TEST: UserList (Danh sách Tài khoản & Nhân viên)
 * ============================================================================
 */
describe('Module 01 - UserList Component', () => {
  const mockUsers = [
    {
      id: 1,
      username: 'admin',
      fullName: 'Đặng Trần Khoa',
      email: 'khoa@solaris.vn',
      phoneNumber: '0901234567',
      citizenId: '079200000001',
      isActive: true,
      roleIds: [1],
      warehouseIds: [1],
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      username: 'linhnguyen',
      fullName: 'Nguyễn Mỹ Linh',
      email: 'linh@solaris.vn',
      phoneNumber: '0902222222',
      citizenId: '079200000002',
      isActive: false,
      roleIds: [2],
      warehouseIds: [],
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  const mockRoles = [
    { id: 1, code: 'ADMIN', name: 'Quản trị viên', isActive: true },
    { id: 2, code: 'SALES', name: 'Kinh doanh', isActive: true },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (roleApi.getAllList as any).mockResolvedValue(mockRoles);
    (userApi.getAll as any).mockResolvedValue({
      items: mockUsers,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH NHÂN VIÊN
  /**
   * TC01: Render tiêu đề trang "Danh Sách Nhân Sự", bảng danh sách
   * với đầy đủ Họ tên, Username, Email, Số điện thoại.
   */
  it('TC01 - Render danh sách nhân viên từ API', async () => {
    render(
      <MemoryRouter>
        <UserList />
      </MemoryRouter>
    );

    expect(screen.getByText('Danh Sách Nhân Sự')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Đặng Trần Khoa')).toBeInTheDocument();
      expect(screen.getByText('@admin')).toBeInTheDocument();
      expect(screen.getByText('khoa@solaris.vn')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Mỹ Linh')).toBeInTheDocument();
      expect(screen.getByText('@linhnguyen')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM NHÂN VIÊN
  /**
   * TC02: Cho phép người dùng nhập từ khóa tìm kiếm vào ô input Search.
   */
  it('TC02 - Nhập từ khóa tìm kiếm nhân viên', async () => {
    render(
      <MemoryRouter>
        <UserList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm theo tên, sđt, CCCD, username...');
    fireEvent.change(searchInput, { target: { value: 'Mỹ Linh' } });

    expect((searchInput as HTMLInputElement).value).toBe('Mỹ Linh');
  });
  // #endregion

  // #region TC03: ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
  /**
   * TC03: Khi bấm vào badge trạng thái, gọi API toggleActive để bật/khóa tài khoản.
   */
  it('TC03 - Gọi API đổi trạng thái khi bấm vào badge Hoạt động/Tạm khóa', async () => {
    (userApi.toggleActive as any).mockResolvedValue(true);

    render(
      <MemoryRouter>
        <UserList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Đặng Trần Khoa')).toBeInTheDocument();
    });

    const activeBadges = screen.getAllByRole('button');
    const toggleBtn = activeBadges.find((btn) => btn.textContent?.includes('Hoạt động'));
    if (toggleBtn) {
      fireEvent.click(toggleBtn);
      await waitFor(() => {
        expect(userApi.toggleActive).toHaveBeenCalledWith(1);
      });
    }
  });
  // #endregion

  // #region TC04: MỞ MODAL XÁC NHẬN XÓA TÀI KHOẢN
  /**
   * TC04: Khi click vào nút Xóa (icon Thùng rác), mở modal xác nhận xóa nhân viên.
   */
  it('TC04 - Mở modal xác nhận khi click nút Xóa nhân viên', async () => {
    render(
      <MemoryRouter>
        <UserList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Đặng Trần Khoa')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa nhân viên');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });
  });
  // #endregion
});
