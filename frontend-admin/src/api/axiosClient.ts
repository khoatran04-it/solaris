// File này tạo ra 1 đại diện (instance) của axios để sử dụng trong toàn bộ dự án, 
// giúp quản lý các request và response một cách tập trung và dễ dàng hơn. 

import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7070/api';

const axiosClient = axios.create({
  baseURL: API_BASE_URL, 
  headers: {
    'Content-Type': 'application/json',
  },
});

// 🔥 BỔ SUNG 1: REQUEST INTERCEPTOR (Tự động kẹp Token vào mọi API gửi đi)
axiosClient.interceptors.request.use(
  (config) => {
    // Lấy token từ localStorage (Nơi chúng ta sẽ lưu token khi Login thành công)
    const token = localStorage.getItem('token');
    
    // Nếu có token, nhét nó vào Header theo chuẩn Bearer Token
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 🔥 CẬP NHẬT 2: RESPONSE INTERCEPTOR (Xử lý Data và bắt lỗi 401)
axiosClient.interceptors.response.use(
  (response) => {
    return response.data; // Chỉ nhận phần data của response
  },
  (error) => {
    // 1. Xử lý lỗi đặc biệt: 401 Unauthorized (Chưa đăng nhập hoặc Token hết hạn)
    if (error.response?.status === 401) {
      console.warn('Phiên đăng nhập hết hạn hoặc bị từ chối truy cập!');
      
      // Xóa dữ liệu cũ cho sạch sẽ
      localStorage.removeItem('token');
      localStorage.removeItem('userInfo');
      
      // Đá văng về trang Đăng nhập (Kiểm tra điều kiện để tránh bị lặp vô hạn nếu đang ở sẵn trang login)
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'; 
      }
    }
    
    // 2. Log lỗi ra console để Dev dễ debug
    console.error('API Error:', error.response?.data || error.message);
    
    return Promise.reject(error);
  }
);

export default axiosClient;