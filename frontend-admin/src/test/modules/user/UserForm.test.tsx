import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import UserForm from '../../../pages/user/UserForm';
import { userApi } from '../../../api/userApi';
import { roleApi } from '../../../api/roleApi';
import { permissionApi } from '../../../api/permissionApi';
import { warehouseApi } from '../../../api/warehouseApi';

// Mock các API liên quan
vi.mock('../../../api/userApi', () => ({
  userApi: {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}));

vi.mock('../../../api/roleApi', () => ({
  roleApi: {
    getAllList: vi.fn(),
  },
}));

vi.mock('../../../api/permissionApi', () => ({
  permissionApi: {
    getAll: vi.fn(),
  },
}));

vi.mock('../../../api/warehouseApi', () => ({
  warehouseApi: {
    getAllList: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 COMPONENT TEST: UserForm (Form Tạo mới & Chỉnh sửa Nhân viên)
 * ============================================================================
 */
describe('Module 01 - UserForm Component', () => {
  const mockRoles = [
    { id: 1, code: 'ADMIN', name: 'Quản trị viên', isActive: true },
    { id: 2, code: 'SALES', name: 'Nhân viên kinh doanh', isActive: true },
  ];

  const mockPermissions = [
    { id: 1, module: 'Hệ thống', code: 'USER_VIEW', name: 'Xem danh sách nhân viên' },
  ];

  const mockWarehouses = [
    { id: 1, code: 'WH-MAIN', name: 'Kho Tổng TP.HCM', isActive: true },
    { id: 2, code: 'WH-HN', name: 'Kho Hà Nội', isActive: true },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (roleApi.getAllList as any).mockResolvedValue(mockRoles);
    (permissionApi.getAll as any).mockResolvedValue(mockPermissions);
    (warehouseApi.getAllList as any).mockResolvedValue(mockWarehouses);
  });

  // #region TC01: RENDER FORM TẠO MỚI
  /**
   * TC01: Render tiêu đề form "Tiếp Nhận Nhân Viên Mới", 3 Tab chính
   * (1. THÔNG TIN CÁ NHÂN, 2. VAI TRÒ & KHO, 3. QUYỀN NGOẠI LỆ) và các trường nhập liệu.
   */
  it('TC01 - Render đầy đủ các trường trong form tạo mới nhân viên', async () => {
    render(
      <MemoryRouter initialEntries={['/users/create']}>
        <Routes>
          <Route path="/users/create" element={<UserForm />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Tiếp Nhận Nhân Viên Mới')).toBeInTheDocument();
    expect(screen.getByText('1. THÔNG TIN CÁ NHÂN')).toBeInTheDocument();
    expect(screen.getByText(/2. VAI TRÒ & KHO/i)).toBeInTheDocument();
    expect(screen.getByText(/3. QUYỀN NGOẠI LỆ/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập 12 số CCCD...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('VD: nguyenvan_a')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập mật khẩu...')).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: VALIDATION BÁO LỖI KHI THIẾU TRƯỜNG BẮT BUỘC
  /**
   * TC02: Khi submit form mà chưa nhập thông tin bắt buộc, form phải hiển thị thông báo lỗi.
   */
  it('TC02 - Báo lỗi validation khi chưa nhập CCCD, Tên đăng nhập, Mật khẩu, Họ tên, Email', async () => {
    const { container } = render(
      <MemoryRouter initialEntries={['/users/create']}>
        <Routes>
          <Route path="/users/create" element={<UserForm />} />
        </Routes>
      </MemoryRouter>
    );

    const form = container.querySelector('form');
    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(screen.getByText('Vui lòng nhập CCCD.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Username.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng cấp mật khẩu khởi tạo.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Họ tên.')).toBeInTheDocument();
      expect(screen.getByText('Vui lòng nhập Email.')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC03: CHUYỂN TAB VAI TRÒ & KHO HÀNG
  /**
   * TC03: Chuyển sang Tab "2. VAI TRÒ & KHO" hiển thị danh sách vai trò và kho hàng để tích chọn.
   */
  it('TC03 - Chuyển sang Tab Vai trò & Kho và hiển thị danh sách để phân quyền', async () => {
    render(
      <MemoryRouter initialEntries={['/users/create']}>
        <Routes>
          <Route path="/users/create" element={<UserForm />} />
        </Routes>
      </MemoryRouter>
    );

    const roleTab = screen.getByText(/2. VAI TRÒ & KHO/i);
    fireEvent.click(roleTab);

    await waitFor(() => {
      expect(screen.getByText('Quản trị viên')).toBeInTheDocument();
      expect(screen.getByText('Nhân viên kinh doanh')).toBeInTheDocument();
      expect(screen.getByText('Kho Tổng TP.HCM')).toBeInTheDocument();
    });
  });
  // #endregion

  // #region TC04: SUBMIT FORM TẠO MỚI THÀNH CÔNG
  /**
   * TC04: Nhập đầy đủ dữ liệu hợp lệ và submit, hệ thống gọi API `userApi.create`.
   */
  it('TC04 - Submit tạo mới nhân viên thành công', async () => {
    (userApi.create as any).mockResolvedValue(100);

    const { container } = render(
      <MemoryRouter initialEntries={['/users/create']}>
        <Routes>
          <Route path="/users/create" element={<UserForm />} />
        </Routes>
      </MemoryRouter>
    );

    fireEvent.change(screen.getByPlaceholderText('Nhập 12 số CCCD...'), {
      target: { value: '079200123456' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: nguyenvan_a'), {
      target: { value: 'khoatran' },
    });
    fireEvent.change(screen.getByPlaceholderText('Nhập mật khẩu...'), {
      target: { value: 'SecurePass@123' },
    });
    fireEvent.change(screen.getByPlaceholderText('VD: Nguyễn Văn A'), {
      target: { value: 'Trần Khoa' },
    });
    fireEvent.change(screen.getByPlaceholderText('Email công ty hoặc cá nhân...'), {
      target: { value: 'khoa@solaris.vn' },
    });
    fireEvent.change(screen.getByPlaceholderText('Nhập số điện thoại...'), {
      target: { value: '0901234567' },
    });

    const form = container.querySelector('form');
    if (form) {
      fireEvent.submit(form);
    }

    await waitFor(() => {
      expect(userApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          citizenId: '079200123456',
          username: 'khoatran',
          fullName: 'Trần Khoa',
          email: 'khoa@solaris.vn',
          phoneNumber: '0901234567',
        })
      );
    });
  });
  // #endregion

  // #region TC05: CHẾ ĐỘ CHỈNH SỬA (EDIT MODE)
  /**
   * TC05: Khi có param ID, nạp dữ liệu cũ của nhân viên từ `userApi.getById`.
   */
  it('TC05 - Chế độ Edit: Nạp thông tin nhân viên cũ theo ID', async () => {
    (userApi.getById as any).mockResolvedValue({
      id: 20,
      citizenId: '079200999888',
      username: 'sales_lead',
      fullName: 'Trưởng Nhóm Kinh Doanh',
      email: 'lead@solaris.vn',
      phoneNumber: '0988776655',
      isActive: true,
      roleIds: [2],
      warehouseIds: [1],
      customPermissions: [],
    });

    render(
      <MemoryRouter initialEntries={['/users/edit/20']}>
        <Routes>
          <Route path="/users/edit/:id" element={<UserForm />} />
        </Routes>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Cập Nhật Hồ Sơ Nhân Viên')).toBeInTheDocument();
      expect((screen.getByPlaceholderText('Nhập 12 số CCCD...') as HTMLInputElement).value).toBe(
        '079200999888'
      );
      expect((screen.getByPlaceholderText('VD: nguyenvan_a') as HTMLInputElement).value).toBe(
        'sales_lead'
      );
      expect((screen.getByPlaceholderText('VD: Nguyễn Văn A') as HTMLInputElement).value).toBe(
        'Trưởng Nhóm Kinh Doanh'
      );
    });
  });
  // #endregion
});
