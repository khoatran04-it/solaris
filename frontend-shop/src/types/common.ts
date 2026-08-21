export interface PagedResult<T> {
    items: T[];
    totalRecords: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
}
