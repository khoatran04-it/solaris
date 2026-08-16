// Định nghĩa cấu trúc trả về cho mọi API có phân trang
export interface PagedResult<T> {
    items: T[]; 
    totalRecords: number; 
    totalPages: number; 
    currentPage: number; 
    pageSize: number; 
}

// Định nghĩa TẤT CẢ các tham số có thể gửi lên cho API phân trang
export interface PaginationParams {
    search?: string; 
    pageIndex?: number; 
    pageSize?: number; 
    names?: string;
    createdAt?: string; // Định dạng YYYY-MM-DD
    updatedAt?: string; // Định dạng YYYY-MM-DD
}
