import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../stores/useAuthStore';
import { ShieldAlert } from 'lucide-react';

interface ProtectedRouteProps {
  requiredPermission?: string;
  requiredPermissions?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  requiredPermission,
  requiredPermissions,
}) => {
  const { isAuthenticated, userInfo } = useAuthStore();
  const location = useLocation();

  // 1. Nếu chưa đăng nhập, chuyển hướng về /login
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // 2. Super Admin luôn có toàn quyền truy cập
  const isSuperAdmin = userInfo?.roles?.some(
    (r) => r.toUpperCase() === 'ADMIN' || r.toUpperCase() === 'SUPER_ADMIN'
  );
  if (isSuperAdmin) {
    return <Outlet />;
  }

  // 3. Kiểm tra danh sách quyền hạn
  const userPermissions = userInfo?.permissions || [];

  if (requiredPermission && !userPermissions.includes(requiredPermission)) {
    return <AccessDeniedPage requiredPermission={requiredPermission} />;
  }

  if (
    requiredPermissions &&
    requiredPermissions.length > 0 &&
    !requiredPermissions.some((p) => userPermissions.includes(p))
  ) {
    return <AccessDeniedPage requiredPermission={requiredPermissions.join(', ')} />;
  }

  return <Outlet />;
};

const AccessDeniedPage: React.FC<{ requiredPermission?: string }> = ({ requiredPermission }) => (
  <div className="min-h-[60vh] flex flex-col items-center justify-center p-8 text-center animate-in fade-in zoom-in-95 duration-200">
    <div className="w-16 h-16 bg-rose-50 text-rose-500 rounded-2xl flex items-center justify-center mb-4 shadow-xs border border-rose-100">
      <ShieldAlert size={36} />
    </div>
    <h2 className="text-2xl font-black text-slate-800 tracking-tight mb-2 uppercase">
      Truy Cập Bị Từ Chối (403 Forbidden)
    </h2>
    <p className="text-slate-500 max-w-md text-sm mb-6 leading-relaxed">
      Tài khoản của bạn không được cấp quyền hạn{' '}
      {requiredPermission && (
        <code className="bg-rose-50 text-rose-600 px-2 py-0.5 rounded-md font-mono text-xs font-bold border border-rose-200">
          {requiredPermission}
        </code>
      )}{' '}
      để thao tác hoặc xem dữ liệu tại phân hệ này.
    </p>
    <a
      href="/"
      className="bg-yellow-400 hover:bg-yellow-500 text-slate-900 font-bold px-6 py-2.5 rounded-xl transition-all shadow-xs hover:shadow-md"
    >
      Quay Về Trang Chủ
    </a>
  </div>
);

export default ProtectedRoute;
