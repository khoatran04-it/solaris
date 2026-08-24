import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import RoleList from '../../../pages/role/RoleList';
import { roleApi } from '../../../api/roleApi';

// Mock roleApi
vi.mock('../../../api/roleApi', () => ({
  roleApi: {
    getAll: vi.fn(),
    toggleActive: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 COMPONENT TEST: RoleList (Danh sách Vai trò & Phân quyền)
 * ============================================================================
 */
describe('Module 01 - RoleList Component', () => {
  const mockRoles = [
    {
      id: 1,
      code: 'ADMIN',
      name: 'Quản trị viên hệ thống',
      description: 'Toàn quyền quản trị toàn bộ hệ thống',
      isActive: true,
      permissionIds: [1, 2, 3],
      createdAt: '2026-08-20T10:00:00Z',
    },
    {
      id: 2,
      code: 'SALES_STAFF',
      name: 'Nhân viên kinh doanh',
      description: 'Tạo và theo dõi đơn đặt hàng',
      isActive: false,
      permissionIds: [10, 11],
      createdAt: '2026-08-21T10:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (roleApi.getAll as any).mockResolvedValue({
      items: mockRoles,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // #region TC01: RENDER DANH SÁCH VAI TRÒ
  /**
   * TC01: Khi trang được tải, hệ thống gọi API lấy danh sách vai trò
   * và render bảng dữ liệu với đầy đủ Mã vai trò, Tên vai trò, Trạng thái.
   */
  it('TC01 - Render tiêu đề trang và danh sách vai trò từ API', async () => {
    render(
      <MemoryRouter>
        <RoleList />
      </MemoryRouter>
    );

    expect(screen.getByText('Quản Lý Vai Trò & Phân Quyền')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('ADMIN')).toBeInTheDocument();
      expect(screen.getByText('Quản trị viên hệ thống')).toBeInTheDocument();
      expect(screen.getByText('SALES_STAFF')).toBeInTheDocument();
      expect(screen.getByText('Nhân viên kinh doanh')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC02: TÌM KIẾM THEO TỪ KHÓA
  /**
   * TC02: Cho phép người dùng gõ từ khóa tìm kiếm vào ô Search.
   */
  it('TC02 - Nhập từ khóa tìm kiếm vai trò', async () => {
    render(
      <MemoryRouter>
        <RoleList />
      </MemoryRouter>
    );

    const searchInput = screen.getByPlaceholderText('Tìm theo mã, tên vai trò...');
    fireEvent.change(searchInput, { target: { value: 'kinh doanh' } });

    expect((searchInput as HTMLInputElement).value).toBe('kinh doanh');
  });
  // #endregion

  // #region TC03: ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
  /**
   * TC03: Khi bấm vào badge trạng thái, hệ thống gọi API toggleActive để đổi trạng thái vai trò.
   */
  it('TC03 - Gọi API đổi trạng thái khi bấm vào badge Hoạt động/Tạm khóa', async () => {
    (roleApi.toggleActive as any).mockResolvedValue(true);

    render(
      <MemoryRouter>
        <RoleList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Quản trị viên hệ thống')).toBeInTheDocument();
    });

    const activeBadges = screen.getAllByRole('button');
    const toggleButton = activeBadges.find((btn) => btn.textContent?.includes('Hoạt động'));
    if (toggleButton) {
      fireEvent.click(toggleButton);
      await waitFor(() => {
        expect(roleApi.toggleActive).toHaveBeenCalledWith(1);
      });
    }
  });
  // #endregion

  // #region TC04: MỞ MODAL XÁC NHẬN XÓA
  /**
   * TC04: Khi click vào nút Xóa (icon Thùng rác), modal xác nhận xóa phải được mở ra.
   */
  it('TC04 - Mở modal xác nhận khi click nút Xóa vai trò', async () => {
    render(
      <MemoryRouter>
        <RoleList />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Quản trị viên hệ thống')).toBeInTheDocument();
    });

    const deleteButtons = screen.getAllByTitle('Xóa');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Bạn có chắc chắn muốn xóa/i)).toBeInTheDocument();
    });
  });
  // #endregion
});
