export interface UoM {
    id: number;
    code: string;
    name: string;
    categoryId: number;
    categoryName?: string; // Dữ liệu flatten (Join từ bảng Category) để UI dễ hiển thị
    synonyms?: string | null; // Từ đồng nghĩa (VD: kg, kí, kilogram)
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface UoMPayload {
    code: string;
    name: string;
    categoryId: number; // Bắt buộc phải chọn nhóm khi tạo
    synonyms?: string | null;
    isActive?: boolean;
}