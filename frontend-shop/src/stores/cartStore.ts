import { create } from "zustand";
import { ShopCart } from "@/types/cart";
import shopCartApi from "@/api/shopCartApi";

interface GuestItem {
  variantId: number;
  uoMId: number;
  quantity: number;
}

interface CartState {
  cart: ShopCart | null;
  guestItems: GuestItem[];
  totalCount: number;
  isLoading: boolean;
  fetchCart: () => Promise<void>;
  addItem: (
    variantId: number,
    uoMId: number,
    quantity: number,
  ) => Promise<void>;
  addMultipleItems: (items: GuestItem[]) => Promise<void>;
  updateQuantity: (cartItemId: number, quantity: number) => Promise<void>;
  removeItem: (cartItemId: number) => Promise<void>;
  clearCart: () => Promise<void>;
  syncGuestCartOnLogin: () => Promise<void>;
}

function getGuestCartFromStorage(): GuestItem[] {
  if (typeof window === "undefined") return [];
  try {
    const saved = localStorage.getItem("solaris_guest_cart");
    if (!saved) return [];
    const parsed = JSON.parse(saved);
    if (Array.isArray(parsed)) {
      return parsed.filter(
        (i): i is GuestItem =>
          i &&
          typeof i.variantId === "number" &&
          i.variantId > 0 &&
          typeof i.quantity === "number" &&
          i.quantity > 0 &&
          typeof i.uoMId === "number" &&
          i.uoMId > 0,
      );
    }
  } catch {
    try {
      localStorage.removeItem("solaris_guest_cart");
    } catch {
      // Ignore storage errors
    }
  }
  return [];
}

function saveGuestCartToStorage(items: GuestItem[]): void {
  if (typeof window === "undefined") return;
  try {
    if (items.length === 0) {
      localStorage.removeItem("solaris_guest_cart");
    } else {
      localStorage.setItem("solaris_guest_cart", JSON.stringify(items));
    }
  } catch (e) {
    console.error("Không thể ghi dữ liệu giỏ hàng vào LocalStorage:", e);
  }
}

export const useCartStore = create<CartState>((set, get) => ({
  cart: null,
  guestItems: [],
  totalCount: 0,
  isLoading: false,

  fetchCart: async () => {
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (!token) {
      const storageItems = getGuestCartFromStorage();
      const items =
        storageItems.length > 0 ? storageItems : [...get().guestItems];
      const initialCount = items.reduce((sum, i) => sum + i.quantity, 0);

      if (items.length === 0) {
        set({ cart: null, guestItems: [], totalCount: 0, isLoading: false });
        return;
      }

      set({ guestItems: items, totalCount: initialCount, isLoading: true });
      try {
        const preview = await shopCartApi.getGuestCartPreview({ items });
        if (preview && preview.items) {
          const validVariantIds = new Set(
            preview.items.map((i) => i.variantId),
          );
          const validItems = items.filter((i) =>
            validVariantIds.has(i.variantId),
          );
          saveGuestCartToStorage(validItems);

          const count = preview.items.reduce(
            (sum, item) => sum + item.quantity,
            0,
          );
          set({
            cart: preview,
            guestItems: validItems,
            totalCount: count,
            isLoading: false,
          });
        } else {
          set({ cart: preview, isLoading: false });
        }
      } catch (error) {
        console.error("Lỗi khi tải giỏ hàng vãng lai:", error);
        set({ isLoading: false });
      }
      return;
    }

    try {
      set({ isLoading: true });
      const cartData = await shopCartApi.getCart();
      const totalCount = cartData.items.reduce(
        (sum, item) => sum + item.quantity,
        0,
      );
      set({ cart: cartData, totalCount, isLoading: false });
    } catch (error) {
      console.error("Lỗi khi tải giỏ hàng người dùng:", error);
      set({ isLoading: false });
    }
  },

  addItem: async (variantId: number, uoMId: number, quantity: number) => {
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (!token) {
      const storageItems = getGuestCartFromStorage();
      const items =
        storageItems.length > 0 ? storageItems : [...get().guestItems];
      const existing = items.find(
        (i) => i.variantId === variantId && i.uoMId === uoMId,
      );
      if (existing) {
        existing.quantity += quantity;
      } else {
        items.push({ variantId, uoMId, quantity });
      }
      saveGuestCartToStorage(items);

      const initialCount = items.reduce((sum, i) => sum + i.quantity, 0);
      set({ guestItems: items, totalCount: initialCount, isLoading: true });

      try {
        const preview = await shopCartApi.getGuestCartPreview({ items });
        const count = preview?.items
          ? preview.items.reduce((sum, item) => sum + item.quantity, 0)
          : initialCount;
        set({
          cart: preview,
          guestItems: items,
          totalCount: count,
          isLoading: false,
        });
      } catch {
        set({ isLoading: false });
      }
      return;
    }

    try {
      set({ isLoading: true });
      const updatedCart = await shopCartApi.addItem({
        variantId,
        uoMId,
        quantity,
      });
      const totalCount = updatedCart.items.reduce(
        (sum, item) => sum + item.quantity,
        0,
      );
      set({ cart: updatedCart, totalCount, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  addMultipleItems: async (newItems: GuestItem[]) => {
    if (!newItems || newItems.length === 0) return;
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (!token) {
      const storageItems = getGuestCartFromStorage();
      const items =
        storageItems.length > 0 ? storageItems : [...get().guestItems];
      for (const item of newItems) {
        const existing = items.find(
          (i) => i.variantId === item.variantId && i.uoMId === item.uoMId,
        );
        if (existing) {
          existing.quantity += item.quantity;
        } else {
          items.push({ ...item });
        }
      }
      saveGuestCartToStorage(items);

      const initialCount = items.reduce((sum, i) => sum + i.quantity, 0);
      set({ guestItems: items, totalCount: initialCount, isLoading: true });

      try {
        const preview = await shopCartApi.getGuestCartPreview({ items });
        const count = preview?.items
          ? preview.items.reduce((sum, item) => sum + item.quantity, 0)
          : initialCount;
        set({
          cart: preview,
          guestItems: items,
          totalCount: count,
          isLoading: false,
        });
      } catch {
        set({ isLoading: false });
      }
      return;
    }

    try {
      set({ isLoading: true });
      const updatedCart = await shopCartApi.syncGuestCart({ items: newItems });
      const totalCount = updatedCart.items.reduce(
        (sum, item) => sum + item.quantity,
        0,
      );
      set({ cart: updatedCart, totalCount, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  updateQuantity: async (cartItemId: number, quantity: number) => {
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (token) {
      try {
        set({ isLoading: true });
        const updatedCart = await shopCartApi.updateItem(cartItemId, {
          quantity,
        });
        const totalCount = updatedCart.items.reduce(
          (sum, item) => sum + item.quantity,
          0,
        );
        set({ cart: updatedCart, totalCount, isLoading: false });
      } catch (error) {
        set({ isLoading: false });
        throw error;
      }
    } else {
      const currentCart = get().cart;
      const targetItem = currentCart?.items?.find((i) => i.id === cartItemId);
      if (!targetItem) return;

      const storageItems = getGuestCartFromStorage();
      const items =
        storageItems.length > 0 ? storageItems : [...get().guestItems];
      const existingIdx = items.findIndex(
        (i) =>
          i.variantId === targetItem.variantId && i.uoMId === targetItem.uoMId,
      );
      if (existingIdx !== -1) {
        if (quantity <= 0) {
          items.splice(existingIdx, 1);
        } else {
          items[existingIdx].quantity = quantity;
        }
      }
      saveGuestCartToStorage(items);

      if (items.length > 0) {
        const initialCount = items.reduce((sum, i) => sum + i.quantity, 0);
        set({ guestItems: items, totalCount: initialCount, isLoading: true });
        try {
          const preview = await shopCartApi.getGuestCartPreview({ items });
          const count = preview?.items
            ? preview.items.reduce((sum, item) => sum + item.quantity, 0)
            : initialCount;
          set({
            cart: preview,
            guestItems: items,
            totalCount: count,
            isLoading: false,
          });
        } catch {
          set({ isLoading: false });
        }
      } else {
        set({ cart: null, guestItems: [], totalCount: 0, isLoading: false });
      }
    }
  },

  removeItem: async (cartItemId: number) => {
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (token) {
      try {
        set({ isLoading: true });
        const updatedCart = await shopCartApi.removeItem(cartItemId);
        const totalCount = updatedCart.items.reduce(
          (sum, item) => sum + item.quantity,
          0,
        );
        set({ cart: updatedCart, totalCount, isLoading: false });
      } catch (error) {
        set({ isLoading: false });
        throw error;
      }
    } else {
      const currentCart = get().cart;
      const targetItem = currentCart?.items?.find((i) => i.id === cartItemId);
      if (!targetItem) return;

      const storageItems = getGuestCartFromStorage();
      const baseItems =
        storageItems.length > 0 ? storageItems : [...get().guestItems];
      const items = baseItems.filter(
        (i) =>
          !(
            i.variantId === targetItem.variantId && i.uoMId === targetItem.uoMId
          ),
      );
      saveGuestCartToStorage(items);

      if (items.length > 0) {
        const initialCount = items.reduce((sum, i) => sum + i.quantity, 0);
        set({ guestItems: items, totalCount: initialCount, isLoading: true });
        try {
          const preview = await shopCartApi.getGuestCartPreview({ items });
          const count = preview?.items
            ? preview.items.reduce((sum, item) => sum + item.quantity, 0)
            : initialCount;
          set({
            cart: preview,
            guestItems: items,
            totalCount: count,
            isLoading: false,
          });
        } catch {
          set({ isLoading: false });
        }
      } else {
        set({ cart: null, guestItems: [], totalCount: 0, isLoading: false });
      }
    }
  },

  clearCart: async () => {
    const token =
      typeof window !== "undefined"
        ? localStorage.getItem("solaris_shop_token")
        : null;
    if (token) {
      try {
        set({ isLoading: true });
        await shopCartApi.clearCart();
        set({ cart: null, totalCount: 0, isLoading: false });
      } catch (error) {
        set({ isLoading: false });
        throw error;
      }
    } else {
      saveGuestCartToStorage([]);
      set({ cart: null, guestItems: [], totalCount: 0, isLoading: false });
    }
  },

  syncGuestCartOnLogin: async () => {
    const guestItems = getGuestCartFromStorage();
    if (guestItems.length === 0) return;

    try {
      const updatedCart = await shopCartApi.syncGuestCart({
        items: guestItems,
      });
      saveGuestCartToStorage([]);
      const totalCount = updatedCart?.items
        ? updatedCart.items.reduce((sum, item) => sum + item.quantity, 0)
        : 0;
      set({ cart: updatedCart, guestItems: [], totalCount });
    } catch (error) {
      console.error(
        "Không thể đồng bộ giỏ hàng vãng lai khi đăng nhập:",
        error,
      );
    }
  },
}));
