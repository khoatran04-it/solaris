import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import "@testing-library/jest-dom";
import GioHangPage from "@/app/gio-hang/page";
import { useCartStore } from "@/stores/cartStore";
import { useAuthStore } from "@/stores/authStore";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 12: SHOPPING CART
 * PAGE TEST: GioHangPage (Trang Giỏ Hàng Mua Sắm)
 * ============================================================================
 */
describe("Module 12 - GioHangPage Component", () => {
  const mockFetchCart = vi.fn();
  const mockUpdateQuantity = vi.fn();
  const mockRemoveItem = vi.fn();
  const mockClearCart = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      isAuthenticated: true,
      user: null,
      token: "mock-token",
    });
    useCartStore.setState({
      cart: null,
      guestItems: [],
      totalCount: 0,
      isLoading: false,
      fetchCart: mockFetchCart,
      updateQuantity: mockUpdateQuantity,
      removeItem: mockRemoveItem,
      clearCart: mockClearCart,
    });
  });

  // TC01: RENDER GIỎ HÀNG RỖNG
  it("TC01 - Hiển thị giao diện giỏ hàng trống và nút khám phá khi không có sản phẩm", () => {
    render(<GioHangPage />);

    expect(screen.getByText("Giỏ hàng của bạn đang trống")).toBeInTheDocument();
    expect(screen.getByText("Khám phá sản phẩm ngay")).toBeInTheDocument();
    expect(mockFetchCart).toHaveBeenCalled();
  });

  // TC02: RENDER DANH SÁCH MẶT HÀNG, GIÁ VÀ TỔNG TIỀN
  it("TC02 - Render chi tiết sản phẩm, xuất xứ, tạm tính, giảm giá và tổng thanh toán", () => {
    useCartStore.setState({
      cart: {
        cartId: 1,
        items: [
          {
            id: 101,
            variantId: 10,
            variantName: "Bơ Booth 7 Đắk Lắk",
            variantCode: "SKU-BO-KG",
            uoMId: 1,
            uoMName: "Kg",
            origin: "Đắk Lắk",
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
      },
      totalCount: 3,
    });

    render(<GioHangPage />);

    expect(screen.getByText("Bơ Booth 7 Đắk Lắk")).toBeInTheDocument();
    expect(screen.getByText("Kg")).toBeInTheDocument();
    expect(screen.getByText(/Vùng trồng: Đắk Lắk/i)).toBeInTheDocument();
    expect(screen.getByText(/3\s*món hàng/i)).toBeInTheDocument();
    expect(screen.getAllByText(/240\.000/)).toHaveLength(2); // Line total & Estimated total
    expect(screen.getByText(/300\.000/)).toBeInTheDocument(); // SubTotal
    expect(screen.getByText(/-.*60\.000/)).toBeInTheDocument(); // Total discount
  });

  // TC03: THAO TÁC TĂNG GIẢM SỐ LƯỢNG
  it("TC03 - Bấm nút cộng hoặc trừ gọi hàm updateQuantity với số lượng tương ứng", () => {
    useCartStore.setState({
      cart: {
        cartId: 1,
        items: [
          {
            id: 101,
            variantId: 10,
            variantName: "Dâu Tây Đà Lạt",
            variantCode: "SKU-DAU",
            uoMId: 1,
            uoMName: "Hộp",
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
      },
      totalCount: 2,
    });

    render(<GioHangPage />);

    const plusBtn = screen.getByText("+");
    fireEvent.click(plusBtn);
    expect(mockUpdateQuantity).toHaveBeenCalledWith(101, 3);

    const minusBtn = screen.getByText("-");
    fireEvent.click(minusBtn);
    expect(mockUpdateQuantity).toHaveBeenCalledWith(101, 1);
  });

  // TC04: THAO TÁC XÓA SẢN PHẨM KHỎI GIỎ
  it("TC04 - Bấm nút xóa gọi hàm removeItem với ID của dòng sản phẩm", () => {
    useCartStore.setState({
      cart: {
        cartId: 1,
        items: [
          {
            id: 105,
            variantId: 10,
            variantName: "Xoài Cát Hòa Lộc",
            variantCode: "SKU-XOAI",
            uoMId: 1,
            uoMName: "Kg",
            quantity: 1,
            unitPrice: 75000,
            originalPrice: 75000,
            discountAmount: 0,
            totalPrice: 75000,
            availableStock: 20,
            isOutOfStock: false,
          },
        ],
        totalItems: 1,
        subTotal: 75000,
        totalDiscount: 0,
        estimatedTotal: 75000,
      },
      totalCount: 1,
    });

    render(<GioHangPage />);

    const deleteBtn = screen.getByTitle("Xóa khỏi giỏ hàng");
    fireEvent.click(deleteBtn);

    expect(mockRemoveItem).toHaveBeenCalledWith(105);
  });

  // TC05: TIẾN HÀNH ĐẶT HÀNG KHI ĐÃ ĐĂNG NHẬP
  it('TC05 - Đã đăng nhập: bấm "Tiến Hành Đặt Hàng" chuyển hướng đến /thanh-toan', () => {
    useAuthStore.setState({ isAuthenticated: true });
    useCartStore.setState({
      cart: {
        cartId: 1,
        items: [
          {
            id: 101,
            variantId: 10,
            variantName: "Cam Sành",
            variantCode: "SKU-CAM",
            uoMId: 1,
            uoMName: "Kg",
            quantity: 2,
            unitPrice: 40000,
            originalPrice: 40000,
            discountAmount: 0,
            totalPrice: 80000,
            availableStock: 30,
            isOutOfStock: false,
          },
        ],
        totalItems: 1,
        subTotal: 80000,
        totalDiscount: 0,
        estimatedTotal: 80000,
      },
      totalCount: 2,
    });

    render(<GioHangPage />);

    const checkoutBtn = screen.getByRole("button", {
      name: /Tiến Hành Đặt Hàng/i,
    });
    fireEvent.click(checkoutBtn);

    expect(mockPush).toHaveBeenCalledWith("/thanh-toan");
  });

  // TC06: TIẾN HÀNH ĐẶT HÀNG KHI CHƯA ĐĂNG NHẬP
  it('TC06 - Chưa đăng nhập: bấm "Tiến Hành Đặt Hàng" chuyển hướng đến /dang-nhap?redirect=/thanh-toan', () => {
    useAuthStore.setState({ isAuthenticated: false });
    useCartStore.setState({
      cart: {
        cartId: 1,
        items: [
          {
            id: 101,
            variantId: 10,
            variantName: "Cam Sành",
            variantCode: "SKU-CAM",
            uoMId: 1,
            uoMName: "Kg",
            quantity: 2,
            unitPrice: 40000,
            originalPrice: 40000,
            discountAmount: 0,
            totalPrice: 80000,
            availableStock: 30,
            isOutOfStock: false,
          },
        ],
        totalItems: 1,
        subTotal: 80000,
        totalDiscount: 0,
        estimatedTotal: 80000,
      },
      totalCount: 2,
    });

    render(<GioHangPage />);

    const checkoutBtn = screen.getByRole("button", {
      name: /Tiến Hành Đặt Hàng/i,
    });
    fireEvent.click(checkoutBtn);

    expect(mockPush).toHaveBeenCalledWith("/dang-nhap?redirect=/thanh-toan");
  });
});
