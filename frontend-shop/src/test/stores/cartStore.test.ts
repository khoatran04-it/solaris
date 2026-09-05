import { describe, it, expect, vi, beforeEach } from "vitest";
import { useCartStore } from "@/stores/cartStore";
import axiosClient from "@/api/axiosClient";

vi.mock("@/api/axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 12: SHOPPING CART
 * STORE TEST: useCartStore (Zustand Cart State & Guest Sync)
 * ============================================================================
 */
describe("Module 12 - useCartStore Zustand Store", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    useCartStore.setState({
      cart: null,
      guestItems: [],
      totalCount: 0,
      isLoading: false,
    });
  });

  // TC01: KHỞI TẠO STATE BAN ĐẦU
  it("TC01 - Trạng thái ban đầu rỗng khi chưa có sản phẩm", () => {
    const state = useCartStore.getState();
    expect(state.cart).toBeNull();
    expect(state.guestItems).toEqual([]);
    expect(state.totalCount).toBe(0);
  });

  // TC02: GUEST MODE - THÊM SẢN PHẨM MỚI VÀO LOCALSTORAGE
  it("TC02 - Khách vãng lai thêm sản phẩm mới vào LocalStorage và cập nhật totalCount", async () => {
    await useCartStore.getState().addItem(10, 1, 2);

    const state = useCartStore.getState();
    expect(state.guestItems).toHaveLength(1);
    expect(state.guestItems[0]).toEqual({
      variantId: 10,
      uoMId: 1,
      quantity: 2,
    });
    expect(state.totalCount).toBe(2);

    const stored = JSON.parse(
      localStorage.getItem("solaris_guest_cart") || "[]",
    );
    expect(stored).toEqual([{ variantId: 10, uoMId: 1, quantity: 2 }]);
  });

  // TC03: GUEST MODE - CỘNG DỒN SỐ LƯỢNG KHI THÊM SẢN PHẨM ĐÃ CÓ
  it("TC03 - Khách vãng lai thêm trùng sản phẩm và ĐVT sẽ tự động cộng dồn số lượng", async () => {
    await useCartStore.getState().addItem(10, 1, 2);
    await useCartStore.getState().addItem(10, 1, 3);

    const state = useCartStore.getState();
    expect(state.guestItems).toHaveLength(1);
    expect(state.guestItems[0].quantity).toBe(5);
    expect(state.totalCount).toBe(5);
  });

  // TC04: AUTH MODE - FETCH CART TỪ SERVER
  it("TC04 - Đã đăng nhập: fetchCart gọi API /cart và lưu cart vào store", async () => {
    localStorage.setItem("solaris_shop_token", "mock_jwt_token");

    const mockCart = {
      cartId: 1,
      items: [
        {
          id: 101,
          variantId: 10,
          variantName: "Bơ Booth 7",
          variantCode: "SKU-BO",
          uoMId: 1,
          uoMName: "Kg",
          quantity: 3,
          unitPrice: 80000,
          originalPrice: 100000,
          discountAmount: 20000,
          totalPrice: 240000,
          availableStock: 50,
          isOutOfStock: false,
        },
      ],
      totalItems: 1,
      subTotal: 300000,
      totalDiscount: 60000,
      estimatedTotal: 240000,
    };

    (axiosClient.get as any).mockResolvedValue(mockCart);

    await useCartStore.getState().fetchCart();

    const state = useCartStore.getState();
    expect(axiosClient.get).toHaveBeenCalledWith("/cart");
    expect(state.cart).toEqual(mockCart);
    expect(state.totalCount).toBe(3);
  });

  // TC05: AUTH MODE - ADD ITEM GỌI API /cart/items
  it("TC05 - Đã đăng nhập: addItem gọi API POST /cart/items", async () => {
    localStorage.setItem("solaris_shop_token", "mock_jwt_token");

    const mockUpdatedCart = {
      cartId: 1,
      items: [
        {
          id: 102,
          variantId: 20,
          variantName: "Dâu Tây Đà Lạt",
          variantCode: "SKU-DAU",
          uoMId: 1,
          uoMName: "Hộp 500g",
          quantity: 2,
          unitPrice: 120000,
          originalPrice: 120000,
          discountAmount: 0,
          totalPrice: 240000,
          availableStock: 10,
          isOutOfStock: false,
        },
      ],
      totalItems: 1,
      subTotal: 240000,
      totalDiscount: 0,
      estimatedTotal: 240000,
    };

    (axiosClient.post as any).mockResolvedValue(mockUpdatedCart);

    await useCartStore.getState().addItem(20, 1, 2);

    expect(axiosClient.post).toHaveBeenCalledWith("/cart/items", {
      variantId: 20,
      uoMId: 1,
      quantity: 2,
    });
    expect(useCartStore.getState().cart).toEqual(mockUpdatedCart);
    expect(useCartStore.getState().totalCount).toBe(2);
  });

  // TC06: AUTH MODE - UPDATE QUANTITY GỌI API PUT /cart/items/:id
  it("TC06 - Đã đăng nhập: updateQuantity gọi API PUT /cart/items/:id", async () => {
    localStorage.setItem("solaris_shop_token", "mock_jwt_token");

    const mockCart = {
      cartId: 1,
      items: [{ id: 102, quantity: 5 }],
      totalItems: 1,
    };
    (axiosClient.put as any).mockResolvedValue(mockCart);

    await useCartStore.getState().updateQuantity(102, 5);

    expect(axiosClient.put).toHaveBeenCalledWith("/cart/items/102", {
      quantity: 5,
    });
    expect(useCartStore.getState().totalCount).toBe(5);
  });

  // TC07: AUTH MODE - REMOVE ITEM GỌI API DELETE /cart/items/:id
  it("TC07 - Đã đăng nhập: removeItem gọi API DELETE /cart/items/:id", async () => {
    localStorage.setItem("solaris_shop_token", "mock_jwt_token");

    const mockCart = {
      cartId: 1,
      items: [],
      totalItems: 0,
    };
    (axiosClient.delete as any).mockResolvedValue(mockCart);

    await useCartStore.getState().removeItem(102);

    expect(axiosClient.delete).toHaveBeenCalledWith("/cart/items/102");
    expect(useCartStore.getState().totalCount).toBe(0);
  });

  // TC08: AUTH MODE - CLEAR CART GỌI API DELETE /cart
  it("TC08 - Đã đăng nhập: clearCart gọi API DELETE /cart và đặt cart về null", async () => {
    localStorage.setItem("solaris_shop_token", "mock_jwt_token");

    (axiosClient.delete as any).mockResolvedValue(undefined);

    await useCartStore.getState().clearCart();

    expect(axiosClient.delete).toHaveBeenCalledWith("/cart");
    expect(useCartStore.getState().cart).toBeNull();
    expect(useCartStore.getState().totalCount).toBe(0);
  });

  // TC09: ĐỒNG BỘ GIỎ HÀNG KHÁCH VÃNG LAI KHI ĐĂNG NHẬP
  it("TC09 - syncGuestCartOnLogin gộp giỏ hàng LocalStorage qua API /cart/sync và xóa key tạm", async () => {
    const guestItems = [
      { variantId: 10, uoMId: 1, quantity: 2 },
      { variantId: 20, uoMId: 1, quantity: 1 },
    ];
    localStorage.setItem("solaris_guest_cart", JSON.stringify(guestItems));

    const mockMergedCart = {
      cartId: 1,
      items: [
        { id: 1, variantId: 10, quantity: 2 },
        { id: 2, variantId: 20, quantity: 1 },
      ],
      totalItems: 2,
    };

    (axiosClient.post as any).mockResolvedValue(mockMergedCart);

    await useCartStore.getState().syncGuestCartOnLogin();

    expect(axiosClient.post).toHaveBeenCalledWith("/cart/sync", {
      items: guestItems,
    });
    expect(localStorage.getItem("solaris_guest_cart")).toBeNull();
    expect(useCartStore.getState().guestItems).toEqual([]);
    expect(useCartStore.getState().totalCount).toBe(3);
  });

  // TC10: GUEST MODE - FETCH CART CALLS GUEST PREVIEW API
  it("TC10 - fetchCart của khách vãng lai gọi POST /cart/guest-preview và nạp cart preview", async () => {
    const guestItems = [{ variantId: 10, uoMId: 1, quantity: 2 }];
    localStorage.setItem("solaris_guest_cart", JSON.stringify(guestItems));

    const mockPreviewCart = {
      cartId: 0,
      items: [
        {
          id: 1,
          variantId: 10,
          variantName: "Bơ Sáp 034",
          uoMId: 1,
          uoMName: "Kg",
          quantity: 2,
          unitPrice: 85000,
          totalPrice: 170000,
          isOutOfStock: false,
        },
      ],
      totalItems: 2,
      subTotal: 170000,
      totalDiscount: 0,
      estimatedTotal: 170000,
    };

    (axiosClient.post as any).mockResolvedValue(mockPreviewCart);

    await useCartStore.getState().fetchCart();

    expect(axiosClient.post).toHaveBeenCalledWith("/cart/guest-preview", {
      items: guestItems,
    });
    expect(useCartStore.getState().cart).toEqual(mockPreviewCart);
    expect(useCartStore.getState().totalCount).toBe(2);
  });

  // TC11: GUEST MODE - ADD MULTIPLE ITEMS (1-CLICK ADD ALL)
  it("TC11 - Khách vãng lai addMultipleItems lưu vào LocalStorage và gọi guest-preview", async () => {
    const mockPreviewCart = {
      cartId: 0,
      items: [
        { id: 1, variantId: 10, uoMId: 1, quantity: 2 },
        { id: 2, variantId: 20, uoMId: 1, quantity: 3 },
      ],
      totalItems: 5,
      subTotal: 400000,
      totalDiscount: 0,
      estimatedTotal: 400000,
    };

    (axiosClient.post as any).mockResolvedValue(mockPreviewCart);

    await useCartStore.getState().addMultipleItems([
      { variantId: 10, uoMId: 1, quantity: 2 },
      { variantId: 20, uoMId: 1, quantity: 3 },
    ]);

    expect(axiosClient.post).toHaveBeenCalledWith("/cart/guest-preview", {
      items: [
        { variantId: 10, uoMId: 1, quantity: 2 },
        { variantId: 20, uoMId: 1, quantity: 3 },
      ],
    });
    expect(useCartStore.getState().totalCount).toBe(5);
    const stored = JSON.parse(
      localStorage.getItem("solaris_guest_cart") || "[]",
    );
    expect(stored).toHaveLength(2);
  });

  // TC12: GUEST MODE - UPDATE QUANTITY AND REMOVE ITEM
  it("TC12 - Khách vãng lai updateQuantity và removeItem cập nhật LocalStorage và gọi preview", async () => {
    const initialCart = {
      cartId: 0,
      items: [
        { id: 1, variantId: 10, uoMId: 1, quantity: 2 },
        { id: 2, variantId: 20, uoMId: 1, quantity: 1 },
      ],
      totalItems: 3,
      subTotal: 250000,
      totalDiscount: 0,
      estimatedTotal: 250000,
    };
    useCartStore.setState({
      cart: initialCart as any,
      guestItems: [
        { variantId: 10, uoMId: 1, quantity: 2 },
        { variantId: 20, uoMId: 1, quantity: 1 },
      ],
      totalCount: 3,
    });

    const updatedCart = {
      ...initialCart,
      items: [{ id: 1, variantId: 10, uoMId: 1, quantity: 5 }],
      totalItems: 5,
    };
    (axiosClient.post as any).mockResolvedValue(updatedCart);

    await useCartStore.getState().updateQuantity(1, 5);
    expect(useCartStore.getState().totalCount).toBe(5);

    // Remove item 1
    const emptyCart = {
      cartId: 0,
      items: [],
      totalItems: 0,
      subTotal: 0,
      totalDiscount: 0,
      estimatedTotal: 0,
    };
    (axiosClient.post as any).mockResolvedValue(emptyCart);

    await useCartStore.getState().removeItem(1);
    const stored = JSON.parse(
      localStorage.getItem("solaris_guest_cart") || "[]",
    );
    expect(stored.find((i: any) => i.variantId === 10)).toBeUndefined();
  });
});
