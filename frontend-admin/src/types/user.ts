import { PaginationParams } from './common';

// Type phụ trợ cho mảng phân quyền ngoại lệ
export interface CustomPermission {
  permissionId: number;
  isGranted: boolean;
}

// 1. Model chính (Dùng để hứng dữ liệu Read từ Backend)
export interface User {
  id: number;
  citizenId: string;
  username: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  avatarUrl?: string;
  lastLoginAt?: string;
  isActive: boolean;
  createdAt: string;
  roleIds: number[];
  warehouseIds: number[];
  customPermissions: CustomPermission[];
}

// 2. Payload (Dùng chung cho cả form Create và Update)
export interface UserPayload {
  citizenId: string;
  username?: string; // Bắt buộc khi Create, disable/không gửi lên khi Update
  password?: string; // Bắt buộc khi Create
  fullName: string;
  email: string;
  phoneNumber: string;
  avatarUrl?: string;
  isActive: boolean;
  roleIds: number[];
  warehouseIds: number[];
  customPermissions: CustomPermission[];
}

// Payload riêng cho tính năng đổi mật khẩu
export interface UserChangePasswordPayload {
  newPassword: string;
}

// 3. Query Params (Dùng cho bộ lọc tìm kiếm trên Table)
export interface UserQueryParams extends PaginationParams {
  roleId?: number | string;
  warehouseId?: number | string;
  isActive?: boolean;
}
