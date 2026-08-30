import { create } from 'zustand';
import { ShopCart } from '@/types/cart';
import axiosClient from '@/api/axiosClient';

interface CartState {
    cart: ShopCart | null;
    guestItems: { variantId: number; uoMId: number; quantity: number }[];
    totalCount: number;
    isLoading: boolean;
    fetchCart: () => Promise<void>;
    addItem: (variantId: number, uoMId: number, quantity: number) => Promise<void>;
    updateQuantity: (cartItemId: number, quantity: number) => Promise<void>;
    removeItem: (cartItemId: number) => Promise<void>;
    clearCart: () => Promise<void>;
    syncGuestCartOnLogin: () => Promise<void>;
}

export const useCartStore = create<CartState>((set, get) => ({
    cart: null,
    guestItems: [],
    totalCount: 0,
    isLoading: false,

    fetchCart: async () => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('solaris_shop_token') : null;
        if (!token) {
            // Khách vãng lai: đọc từ localStorage
            if (typeof window !== 'undefined') {
                const saved = localStorage.getItem('solaris_guest_cart');
                const items = saved ? JSON.parse(saved) : [];
                const totalCount = items.reduce((sum: number, i: any) => sum + (i.quantity || 1), 0);
                set({ guestItems: items, totalCount });
            }
            return;
        }

        try {
            set({ isLoading: true });
            const cartData: ShopCart = await axiosClient.get('/cart');
            const totalCount = cartData.items.reduce((sum, item) => sum + item.quantity, 0);
            set({ cart: cartData, totalCount, isLoading: false });
        } catch {
            set({ isLoading: false });
        }
    },

    addItem: async (variantId: number, uoMId: number, quantity: number) => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('solaris_shop_token') : null;
        if (!token) {
            // Guest mode
            const items = [...get().guestItems];
            const existing = items.find(i => i.variantId === variantId && i.uoMId === uoMId);
            if (existing) {
                existing.quantity += quantity;
            } else {
                items.push({ variantId, uoMId, quantity });
            }
            if (typeof window !== 'undefined') {
                localStorage.setItem('solaris_guest_cart', JSON.stringify(items));
            }
            const totalCount = items.reduce((sum, i) => sum + i.quantity, 0);
            set({ guestItems: items, totalCount });
            return;
        }

        try {
            const updatedCart: ShopCart = await axiosClient.post('/cart/items', { variantId, uoMId, quantity });
            const totalCount = updatedCart.items.reduce((sum, item) => sum + item.quantity, 0);
            set({ cart: updatedCart, totalCount });
        } catch (error) {
            throw error;
        }
    },

    updateQuantity: async (cartItemId: number, quantity: number) => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('solaris_shop_token') : null;
        if (token) {
            try {
                const updatedCart: ShopCart = await axiosClient.put(`/cart/items/${cartItemId}`, { quantity });
                const totalCount = updatedCart.items.reduce((sum, item) => sum + item.quantity, 0);
                set({ cart: updatedCart, totalCount });
            } catch (error) {
                throw error;
            }
        }
    },

    removeItem: async (cartItemId: number) => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('solaris_shop_token') : null;
        if (token) {
            try {
                const updatedCart: ShopCart = await axiosClient.delete(`/cart/items/${cartItemId}`);
                const totalCount = updatedCart.items.reduce((sum, item) => sum + item.quantity, 0);
                set({ cart: updatedCart, totalCount });
            } catch (error) {
                throw error;
            }
        }
    },

    clearCart: async () => {
        const token = typeof window !== 'undefined' ? localStorage.getItem('solaris_shop_token') : null;
        if (token) {
            await axiosClient.delete('/cart');
            set({ cart: null, totalCount: 0 });
        } else {
            if (typeof window !== 'undefined') {
                localStorage.removeItem('solaris_guest_cart');
            }
            set({ guestItems: [], totalCount: 0 });
        }
    },

    syncGuestCartOnLogin: async () => {
        if (typeof window === 'undefined') return;
        const saved = localStorage.getItem('solaris_guest_cart');
        if (!saved) return;

        try {
            const guestItems = JSON.parse(saved);
            if (guestItems && guestItems.length > 0) {
                const updatedCart: ShopCart = await axiosClient.post('/cart/sync', { items: guestItems });
                localStorage.removeItem('solaris_guest_cart');
                const totalCount = updatedCart.items.reduce((sum, item) => sum + item.quantity, 0);
                set({ cart: updatedCart, guestItems: [], totalCount });
            }
        } catch {
            // Ignore if sync error
        }
    }
}));
