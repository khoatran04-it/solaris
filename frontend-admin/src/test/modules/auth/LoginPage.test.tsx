import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../../../pages/login/LoginPage';
import axiosClient from '../../../api/axiosClient';

// Giả lập (Mock) axiosClient để kiểm soát dữ liệu trả về từ API
vi.mock('../../../api/axiosClient', () => ({
  default: {
    post: vi.fn(),
  },
}));

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 COMPONENT TEST: LoginPage (Giao diện Đăng nhập Hệ thống)
 * ============================================================================
 */
describe('Module 01 - LoginPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  // #region TC01: RENDER GIAO DIỆN
  /**
   * TC01: Giao diện phải hiển thị đầy đủ Logo thương hiệu SOLARIS,
   * tiêu đề hệ thống, trường nhập Tên tài khoản, Mật khẩu và Nút hành động Đăng nhập.
   */
  it('TC01 - Hiển thị đầy đủ giao diện: Logo SOLARIS, ô Tài khoản, Mật khẩu và Nút Đăng nhập', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('SOLARIS')).toBeInTheDocument();
    expect(screen.getByText('Hệ Thống Quản Trị Chuỗi Cung Ứng Nông Sản')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập tên tài khoản...')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập mật khẩu...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Đăng Nhập/i })).toBeInTheDocument();
  });
  // #endregion

  // #region TC02: NHẬP LIỆU TÀI KHOẢN & MẬT KHẨU
  /**
   * TC02: Người dùng có thể gõ thông tin tài khoản và mật khẩu vào các ô input.
   */
  it('TC02 - Cho phép người dùng gõ tài khoản và mật khẩu vào form', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const usernameInput = screen.getByPlaceholderText('Nhập tên tài khoản...') as HTMLInputElement;
    const passwordInput = screen.getByPlaceholderText('Nhập mật khẩu...') as HTMLInputElement;

    fireEvent.change(usernameInput, { target: { value: 'admin' } });
    fireEvent.change(passwordInput, { target: { value: 'Admin@123' } });

    expect(usernameInput.value).toBe('admin');
    expect(passwordInput.value).toBe('Admin@123');
  });
  // #endregion

  // #region TC03: XỬ LÝ LỖI ĐĂNG NHẬP (401 UNAUTHORIZED)
  /**
   * TC03: Khi API trả về mã lỗi 401/400 (Sai tài khoản hoặc mật khẩu),
   * giao diện phải hiển thị thông báo lỗi trực quan cho người dùng.
   */
  it('TC03 - Hiển thị thông báo lỗi khi Backend từ chối đăng nhập', async () => {
    (axiosClient.post as any).mockRejectedValueOnce({
      response: {
        data: {
          message: 'Tên đăng nhập hoặc mật khẩu không chính xác.',
        },
      },
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const usernameInput = screen.getByPlaceholderText('Nhập tên tài khoản...');
    const passwordInput = screen.getByPlaceholderText('Nhập mật khẩu...');
    const submitBtn = screen.getByRole('button', { name: /Đăng Nhập/i });

    fireEvent.change(usernameInput, { target: { value: 'wrong_user' } });
    fireEvent.change(passwordInput, { target: { value: 'wrong_pass' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Tên đăng nhập hoặc mật khẩu không chính xác.')).toBeInTheDocument();
    });
  });
  // #endregion
});
