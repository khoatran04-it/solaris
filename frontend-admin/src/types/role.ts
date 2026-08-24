import { PaginationParams } from './common';

// 1. Model chính (Dùng để hứng dữ liệu Read từ Backend)
export interface Role {
  id: number;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  permissionIds: number[];
}

// 2. Payload (Dùng chung cho cả form Create và Update)
export interface RolePayload {
  code?: string; // Bắt buộc nhập khi Create, có thể bỏ qua khi Update
  name: string;
  description?: string;
  isActive: boolean;
  permissionIds: number[];
}

// 3. Query Params (Dùng cho bộ lọc tìm kiếm trên Table)
export interface RoleQueryParams extends PaginationParams {
  isActive?: boolean;
}
