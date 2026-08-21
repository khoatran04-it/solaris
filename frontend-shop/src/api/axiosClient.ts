import axios, { AxiosRequestConfig } from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7070/api/shop';

// Cho phép bỏ qua chứng chỉ SSL tự ký (self-signed dev certs) khi chạy SSR trên Node.js
let httpsAgent: any = undefined;
if (typeof window === 'undefined') {
    try {
        const https = require('https');
        httpsAgent = new https.Agent({ rejectUnauthorized: false });
    } catch {
        // Ignore in browser bundle
    }
}

const axiosInstance = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    httpsAgent: httpsAgent,
});

// Interceptor: gắn JWT Token
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

// Interceptor: bóc tách response.data + xử lý 401
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

const axiosClient = {
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

export default axiosClient;
