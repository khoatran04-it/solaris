import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore, UserInfo } from '../../../stores/useAuthStore';

/**
 * ============================================================================
 * 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
 * 🧪 UNIT TEST: useAuthStore (Quản lý State Xác thực & Bộ nhớ Trình duyệt)
 * ============================================================================
 */
describe('Module 01 - useAuthStore (State Management & LocalStorage)', () => {
  beforeEach(() => {
    // Dọn sạch LocalStorage và State trên RAM trước mỗi ca kiểm thử
    localStorage.clear();
    useAuthStore.getState().logout();
  });

  // #region TC01: KHỞI TẠO BAN ĐẦU
  /**
   * TC01: Khi chưa có dữ liệu lưu trữ, trạng thái ban đầu phải là Chưa xác thực.
   */
  it('TC01 - Khởi tạo trạng thái ban đầu: Chưa đăng nhập, token là null', () => {
    const state = useAuthStore.getState();
    expect(state.token).toBeNull();
    expect(state.userInfo).toBeNull();
    expect(state.isAuthenticated).toBe(false);
  });
  // #endregion

  // #region TC02: ĐĂNG NHẬP THÀNH CÔNG
  /**
   * TC02: Khi gọi hàm login(), Token và UserInfo phải được lưu đồng thời vào
   * Zustand State (RAM) và LocalStorage (ổ cứng trình duyệt) để F5 không bị mất phiên.
   */
  it('TC02 - Hàm login(): Lưu Token và UserInfo vào RAM và LocalStorage', () => {
    const mockUser: UserInfo = {
      id: 1,
      username: 'admin',
      fullName: 'Đặng Khoa',
      email: 'admin@solaris.vn',
      roles: ['SUPER_ADMIN'],
      warehouseIds: [1, 2],
      permissions: ['USER_VIEW', 'USER_MANAGE', 'ROLE_VIEW', 'ROLE_MANAGE'],
    };

    const mockToken = 'mock_jwt_token_header.payload.signature';

    // Act: Thực hiện đăng nhập
    useAuthStore.getState().login(mockToken, mockUser);

    // Assert trong Zustand State
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.token).toBe(mockToken);
    expect(state.userInfo?.username).toBe('admin');
    expect(state.userInfo?.roles).toContain('SUPER_ADMIN');
    expect(state.userInfo?.permissions).toContain('ROLE_MANAGE');

    // Assert trong LocalStorage
    expect(localStorage.getItem('token')).toBe(mockToken);
    expect(JSON.parse(localStorage.getItem('userInfo')!)).toEqual(mockUser);
  });
  // #endregion

  // #region TC03: ĐĂNG XUẤT
  /**
   * TC03: Khi gọi hàm logout(), toàn bộ Token và UserInfo phải bị xóa sạch
   * khỏi cả State và LocalStorage để bảo mật.
   */
  it('TC03 - Hàm logout(): Xóa sạch Token và UserInfo khỏi RAM và LocalStorage', () => {
    const mockUser: UserInfo = {
      id: 2,
      username: 'staff',
      fullName: 'Nhân Viên',
      email: 'staff@solaris.vn',
      roles: ['STAFF'],
      warehouseIds: [],
      permissions: ['USER_VIEW'],
    };

    // Đăng nhập trước
    useAuthStore.getState().login('token_abc', mockUser);

    // Act: Thực hiện đăng xuất
    useAuthStore.getState().logout();

    // Assert
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.token).toBeNull();
    expect(state.userInfo).toBeNull();

    expect(localStorage.getItem('token')).toBeNull();
    expect(localStorage.getItem('userInfo')).toBeNull();
  });
  // #endregion
});
