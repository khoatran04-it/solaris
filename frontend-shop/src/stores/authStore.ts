import { create } from 'zustand';
import { ShopCustomerInfo, ShopAuthResponse } from '@/types/shop';
import { apiClient } from '@/lib/api';

interface AuthState {
    token: string | null;
    user: ShopCustomerInfo | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    initAuth: () => void;
    login: (token: string, user: ShopCustomerInfo) => void;
    logout: () => void;
    updateUser: (user: ShopCustomerInfo) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    token: null,
    user: null,
    isAuthenticated: false,
    isLoading: true,

    initAuth: () => {
        if (typeof window !== 'undefined') {
            const token = localStorage.getItem('solaris_shop_token');
            const userStr = localStorage.getItem('solaris_shop_user');
            if (token && userStr) {
                try {
                    const user = JSON.parse(userStr);
                    set({ token, user, isAuthenticated: true, isLoading: false });
                    return;
                } catch {
                    localStorage.removeItem('solaris_shop_token');
                    localStorage.removeItem('solaris_shop_user');
                }
            }
            set({ token: null, user: null, isAuthenticated: false, isLoading: false });
        }
    },

    login: (token: string, user: ShopCustomerInfo) => {
        if (typeof window !== 'undefined') {
            localStorage.setItem('solaris_shop_token', token);
            localStorage.setItem('solaris_shop_user', JSON.stringify(user));
        }
        set({ token, user, isAuthenticated: true });
    },

    logout: () => {
        if (typeof window !== 'undefined') {
            localStorage.removeItem('solaris_shop_token');
            localStorage.removeItem('solaris_shop_user');
        }
        set({ token: null, user: null, isAuthenticated: false });
    },

    updateUser: (user: ShopCustomerInfo) => {
        if (typeof window !== 'undefined') {
            localStorage.setItem('solaris_shop_user', JSON.stringify(user));
        }
        set({ user });
    }
}));
