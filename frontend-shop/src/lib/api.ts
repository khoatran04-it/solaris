import axios, { AxiosRequestConfig } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7070/api/shop';

const axiosInstance = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Interceptor gắn JWT Token nếu có
axiosInstance.interceptors.request.use(
    (config) => {
        if (typeof window !== 'undefined') {
            const token = localStorage.getItem('solaris_shop_token');
            if (token && config.headers) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Interceptor bóc tách response.data
axiosInstance.interceptors.response.use(
    (response) => response.data,
    (error) => {
        if (error.response?.status === 401 && typeof window !== 'undefined') {
            localStorage.removeItem('solaris_shop_token');
            localStorage.removeItem('solaris_shop_user');
        }
        return Promise.reject(error.response?.data || error);
    }
);

// Wrapper chuẩn hóa Type-Safe trả về data trực tiếp
export const apiClient = {
    get: async <T>(url: string, config?: AxiosRequestConfig): Promise<T> => {
        return axiosInstance.get(url, config) as unknown as Promise<T>;
    },
    post: async <T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
        return axiosInstance.post(url, data, config) as unknown as Promise<T>;
    },
    put: async <T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
        return axiosInstance.put(url, data, config) as unknown as Promise<T>;
    },
    delete: async <T>(url: string, config?: AxiosRequestConfig): Promise<T> => {
        return axiosInstance.delete(url, config) as unknown as Promise<T>;
    }
};

export function formatVND(amount: number | undefined | null): string {
    if (amount === undefined || amount === null || isNaN(amount)) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

export function formatDate(dateStr: string | undefined | null): string {
    if (!dateStr) return '';
    try {
        const d = new Date(dateStr);
        return d.toLocaleDateString('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch {
        return dateStr;
    }
}
