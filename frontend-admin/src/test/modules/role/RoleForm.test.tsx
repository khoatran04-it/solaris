import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import RoleForm from '../../../pages/role/RoleForm';
import { roleApi } from '../../../api/roleApi';
import { permissionApi } from '../../../api/permissionApi';

// Mock roleApi & permissionApi
vi.mock('../../../api/roleApi', () => ({
  roleApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/permissionApi', () => ({
  permissionApi: {
    getAll: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 COMPONENT TEST: RoleForm (Form Tạo mới & Chỉnh sửa Vai trò)
 * ============================================================================
 */
describe('Module 01 - RoleForm Component', () => {
  const mockPermissions = [
    { id: 1, module: 'Hệ thống', code: 'USER_VIEW', name: 'Xem danh sách nhân viên' },
    { id: 2, module: 'Hệ thống', code: 'USER_MANAGE', name: 'Thêm/Sửa/Xóa nhân viên' },
    { id: 3, module: 'Đơn hàng', code: 'ORDER_VIEW', name: 'Xem danh sách đơn hàng' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (permissionApi.getAll as any).mockResolvedValue(mockPermissions);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  /**
   * TC01: Form tạo mới phải hiển thị tiêu đề "Tạo Mới Vai Trò",
   * 2 Tab chính ("1. THÔNG TIN CƠ BẢN" và "2. MA TRẬN PHÂN QUYỀN"), và các trường nhập liệu.
   */
  it('TC01 - Render đầy đủ các trường trong form tạo mới vai trò', async () => {
    render(
      <MemoryRouter initialEntries={['/roles/create']}>
        <Routes>
          <Route path="/roles/create" element={<RoleForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tạo Mới Vai Trò')).toBeInTheDocument();
    expect(screen.getByText('1. THÔNG TIN CƠ BẢN')).toBeInTheDocument();
    expect(screen.getByText(/2. MA TRẬN PHÂN QUYỀN/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: SALE_MANAGER')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: Trưởng phòng Kinh Doanh')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION FORM KHI BỎ TRỐNG TRƯỜNG BẮT BUỘC
  /**
   * TC02: Khi submit mà không nhập Mã hoặc Tên vai trò,
   * form phải hiển thị thông báo lỗi yêu cầu nhập liệu.
   */
  it('TC02 - Báo lỗi validation khi chưa nhập Mã vai trò hoặc Tên vai trò', async () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/roles/create']}>
        <Routes>
          <Route path="/roles/create" element={<RoleForm />} />
        </Routes>
      </MemoryRouter>
    );

    const form = container.querySelector('form');
    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập Mã Vai Trò.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Tên Vai Trò.')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: CHUYỂN ĐỔI GIỮA CÁC TAB
  /**
   * TC03: Người dùng có thể click chuyển sang Tab "MA TRẬN PHÂN QUYỀN" để xem ma trận quyền.
   */
  it('TC03 - Chuyển sang Tab Phân quyền chi tiết và hiển thị danh mục quyền', async () => {
    render(
      <MemoryRouter initialEntries={['/roles/create']}>
        <Routes>
          <Route path="/roles/create" element={<RoleForm />} />
        </Routes>
      </MemoryRouter>
    );

    const permTab = screen.getByText(/2. MA TRẬN PHÂN QUYỀN/i);
    fireEvent.click(permTab);

    await waitFor(() => {
      expect(screen.getByText('Xem danh sách nhân viên')).toBeInTheDocument();
      expect(screen.getByText('Xem danh sách đơn hàng')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: SUBMIT TẠO MỚI THÀNH CÔNG
  /**
   * TC04: Khi điền đầy đủ dữ liệu hợp lệ và submit, hệ thống gọi API `roleApi.create`.
   */
  it('TC04 - Submit tạo vai trò mới thành công', async () => {
    (roleApi.create as any).mockResolvedValue(10);

    const { container } = render(
      <MemoryRouter initialEntries={['/roles/create']}>
        <Routes>
          <Route path="/roles/create" element={<RoleForm />} />
        </Routes>
      </MemoryRouter>
    );

    const codeInput = screen.getByPlaceholderText('VD: SALE_MANAGER');
    const nameInput = screen.getByPlaceholderText('VD: Trưởng phòng Kinh Doanh');

    fireEvent.change(codeInput, { target: { value: 'ACCOUNTANT' } });
    fireEvent.change(nameInput, { target: { value: 'Kế toán trưởng' } });

    const form = container.querySelector('form');
    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(roleApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          code: 'ACCOUNTANT',
          name: 'Kế toán trưởng',
        })
      );
    });
  });
  // #endregion

  // #region TC05: CHẾ ĐỘ CHỈNH SỬA (EDIT MODE)
  /**
   * TC05: Khi truy cập với ID, form chuyển sang chế độ Sửa và tự động nạp dữ liệu cũ.
   */
  it('TC05 - Chế độ Edit: Nạp dữ liệu cũ của vai trò từ API theo ID', async () => {
    (roleApi.getById as any).mockResolvedValue({
      id: 5,
      code: 'DIRECTOR',
      name: 'Ban Giám Đốc',
      description: 'Xem toàn bộ báo cáo doanh thu',
      isActive: true,
      permissionIds: [1, 3],
    });

    render(
      <MemoryRouter initialEntries={['/roles/edit/5']}>
        <Routes>
          <Route path="/roles/edit/:id" element={<RoleForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Chỉnh Sửa & Phân Quyền')).toBeInTheDocument();
      expect((screen.getByPlaceholderText('VD: SALE_MANAGER') as HTMLInputElement).value).toBe(
        'DIRECTOR'
      );
      expect(
        (screen.getByPlaceholderText('VD: Trưởng phòng Kinh Doanh') as HTMLInputElement).value
      ).toBe('Ban Giám Đốc');
    });
  });
  // #endregion
});
