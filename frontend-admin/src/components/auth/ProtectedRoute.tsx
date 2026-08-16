import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../../stores/useAuthStore';

const ProtectedRoute = () => {
  // Lấy trạng thái đăng nhập từ Zustand
  const { isAuthenticated } = useAuthStore();

  // Nếu chưa đăng nhập, đá về trang /login ngay lập tức
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Nếu đã đăng nhập, cho phép đi tiếp vào các component con (Outlet)
  return <Outlet />;
};

export default ProtectedRoute;