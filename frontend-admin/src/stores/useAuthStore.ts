import { create } from 'zustand';

// 1. Khai báo kiểu dữ liệu (Khớp 100% với DTO trả về từ Backend)
export interface UserInfo {
  id: number;
  username: string;
  fullName: string;
  email: string;
  avatarUrl?: string;
  roles: string[];
  warehouseIds: number[];
  permissions: string[];
}

interface AuthState {
  token: string | null;
  userInfo: UserInfo | null;
  isAuthenticated: boolean;

  // Các hàm hành động (Actions)
  login: (token: string, userInfo: UserInfo) => void;
  logout: () => void;
}

// 2. Lấy dữ liệu khởi tạo từ localStorage (Để F5 web không bị mất phiên đăng nhập)
const getInitialToken = () => localStorage.getItem('token');
const getInitialUserInfo = () => {
  const storedUser = localStorage.getItem('userInfo');
  return storedUser ? JSON.parse(storedUser) : null;
};

// 3. Khởi tạo Zustand Store
export const useAuthStore = create<AuthState>((set) => ({
  // Trạng thái ban đầu
  token: getInitialToken(),
  userInfo: getInitialUserInfo(),
  isAuthenticated: !!getInitialToken(), // Nếu có token thì trả về true

  // Hàm xử lý khi Đăng nhập thành công
  login: (token: string, userInfo: UserInfo) => {
    // Lưu vào ổ cứng trình duyệt để axiosClient và F5 web đọc được
    localStorage.setItem('token', token);
    localStorage.setItem('userInfo', JSON.stringify(userInfo));

    // Cập nhật State trên RAM của React
    set({ token, userInfo, isAuthenticated: true });
  },

  // Hàm xử lý khi Đăng xuất (Hoặc bị API 401 đá văng)
  logout: () => {
    // Quét sạch ổ cứng
    localStorage.removeItem('token');
    localStorage.removeItem('userInfo');

    // Xóa State trên RAM
    set({ token: null, userInfo: null, isAuthenticated: false });
  },
}));
