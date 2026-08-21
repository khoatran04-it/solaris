import axiosClient from './axiosClient';
import { ShopCart, ShopCartAddPayload, ShopCartUpdatePayload, ShopSyncGuestCartPayload } from '@/types/cart';

const shopCartApi = {
    getCart: () =>
        axiosClient.get<ShopCart>('/cart'),

    addItem: (data: ShopCartAddPayload) =>
        axiosClient.post<ShopCart>('/cart/items', data),

    updateItem: (cartItemId: number, data: ShopCartUpdatePayload) =>
        axiosClient.put<ShopCart>(`/cart/items/${cartItemId}`, data),

    removeItem: (cartItemId: number) =>
        axiosClient.delete<ShopCart>(`/cart/items/${cartItemId}`),

    clearCart: () =>
        axiosClient.delete<ShopCart>('/cart'),

    syncGuestCart: (data: ShopSyncGuestCartPayload) =>
        axiosClient.post<ShopCart>('/cart/sync', data),
};

export default shopCartApi;
