import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ThanhToanPage from "@/app/thanh-toan/page";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import shopOrderApi from "@/api/shopOrderApi";
import shopCustomerApi from "@/api/shopCustomerApi";
import shopShippingApi from "@/api/shopShippingApi";
import shopPaymentApi from "@/api/shopPaymentApi";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

vi.mock("@/api/shopOrderApi", () => ({
  default: {
    checkout: vi.fn(),
  },
}));

vi.mock("@/api/shopCustomerApi", () => ({
  default: {
    getAddresses: vi.fn(),
  },
}));

vi.mock("@/api/shopShippingApi", () => ({
  default: {
    getProvinces: vi.fn(),
    getDistricts: vi.fn(),
    getWards: vi.fn(),
    calculateFee: vi.fn(),
  },
}));

vi.mock("@/api/shopPaymentApi", () => ({
  default: {
    createVnPayUrl: vi.fn(),
  },
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * PAGE TEST: ThanhToanPage (Trang Xác Nhận & Thanh Toán Đơn Hàng)
 * ============================================================================
 */
describe("Module 13 - ThanhToanPage Component", () => {
  const mockClearCart = vi.fn();
  const mockFetchCart = vi.fn();

  const mockCart = {
    cartId: 1,
    items: [
      {
        id: 101,
        variantId: 10,
        variantName: "Bơ Booth 7 Đắk Lắk (Kg)",
        variantCode: "SKU-BO",
        uoMId: 1,
        uoMName: "Kg",
        quantity: 2,
        unitPrice: 80000,
        originalPrice: 100000,
        discountAmount: 20000,
        totalPrice: 160000,
        availableStock: 50,
        isOutOfStock: false,
      },
    ],
    totalItems: 1,
    subTotal: 200000,
    totalDiscount: 40000,
    estimatedTotal: 160000,
  };

  const mockAddresses = [
    {
      id: 1,
      receiverName: "Nguyễn Văn A",
      phone: "0901234567",
      province: "TP. Hồ Chí Minh",
      district: "Quận 1",
      ward: "Phường Bến Nghé",
      streetAddress: "123 Lê Lợi",
      fullAddress: "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      isDefault: true,
      latitude: 10.7769,
      longitude: 106.7009,
    },
  ];

  const mockProvinces = [
    { provinceID: 201, provinceName: "Hồ Chí Minh", code: "HCM" },
  ];

  const mockDistricts = [
    { districtID: 1442, provinceID: 201, districtName: "Quận 1", code: "Q1" },
  ];

  const mockWards = [
    { wardCode: "20101", districtID: 1442, wardName: "Phường Bến Nghé" },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      isAuthenticated: true,
      user: null,
      token: "mock-token",
    });
    useCartStore.setState({
      cart: mockCart,
      fetchCart: mockFetchCart,
      clearCart: mockClearCart,
    });

    (shopCustomerApi.getAddresses as any).mockResolvedValue(mockAddresses);
    (shopShippingApi.getProvinces as any).mockResolvedValue(mockProvinces);
    (shopShippingApi.getDistricts as any).mockResolvedValue(mockDistricts);
    (shopShippingApi.getWards as any).mockResolvedValue(mockWards);
    (shopShippingApi.calculateFee as any).mockResolvedValue({
      totalFee: 25000,
      isFreeShipping: false,
    });
  });

  // TC01: RENDER KHI GIỎ HÀNG TRỐNG
  it("TC01 - Hiển thị giao diện giỏ hàng trống khi không có sản phẩm nào", () => {
    useCartStore.setState({ cart: null });

    render(<ThanhToanPage />);

    expect(
      screen.getByText("Giỏ hàng của bạn đang trống."),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /Tiếp tục mua hàng/i }),
    ).toBeInTheDocument();
  });

  // TC02: RENDER THÔNG TIN ĐƠN HÀNG & ĐỊA CHỈ MẶC ĐỊNH
  it("TC02 - Render chi tiết sản phẩm, địa chỉ đã lưu và phương thức thanh toán VNPay", async () => {
    render(<ThanhToanPage />);

    await waitFor(() => {
      expect(screen.getByText(/Địa Chỉ Nhận Hàng/i)).toBeInTheDocument();
      expect(screen.getByText(/Phương Thức Thanh Toán/i)).toBeInTheDocument();
      expect(
        screen.getByText(/Bơ Booth 7 Đắk Lắk \(Kg\)/i),
      ).toBeInTheDocument();
      expect(
        screen.getByText(/Nguyễn Văn A • 0901234567/i),
      ).toBeInTheDocument();
      expect(screen.getByText("Mặc định")).toBeInTheDocument();
    });
  });

  // TC03: TÍNH PHÍ VẬN CHUYỂN GHN & HIỂN THỊ BANNER FREESHIP
  it("TC03 - Hiển thị gợi ý mua thêm để đạt Freeship khi đơn hàng dưới 300k", async () => {
    render(<ThanhToanPage />);

    await waitFor(() => {
      expect(screen.getByText(/FREESHIP 100%/i)).toBeInTheDocument();
      expect(screen.getByText("140.000 ₫")).toBeInTheDocument();
    });
  });

  // TC04: ĐẶT HÀNG THÀNH CÔNG VỚI TIỀN MẶT COD
  it("TC04 - Chọn COD và submit thành công, xóa giỏ hàng và điều hướng tới chi tiết đơn hàng", async () => {
    const mockOrderResponse = {
      id: 10,
      orderCode: "ORD-20260830-010",
      totalAmount: 185000,
    };

    (shopOrderApi.checkout as any).mockResolvedValue(mockOrderResponse);

    render(<ThanhToanPage />);

    await waitFor(() => {
      expect(screen.getByText(/Khi nhận hàng \(COD\)/i)).toBeInTheDocument();
    });

    // 1. Chọn phương thức COD
    const codOption = screen.getByText(/Khi nhận hàng \(COD\)/i);
    fireEvent.click(codOption);

    // 2. Submit form
    const submitBtn = screen.getByRole("button", {
      name: /Xác Nhận & Đặt Hàng/i,
    });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shopOrderApi.checkout).toHaveBeenCalledWith(
        expect.objectContaining({
          customerAddressId: 1,
          paymentMethod: 1,
        }),
      );
      expect(mockClearCart).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith(
        "/tai-khoan/don-hang/ORD-20260830-010",
      );
    });
  });

  // TC05: ĐẶT HÀNG QUA CỔNG VNPAY SANDBOX
  it("TC05 - Chọn VNPay và submit tạo đơn hàng cùng VNPay payment URL", async () => {
    const mockOrderResponse = {
      id: 20,
      orderCode: "ORD-20260830-020",
      totalAmount: 185000,
    };

    (shopOrderApi.checkout as any).mockResolvedValue(mockOrderResponse);
    (shopPaymentApi.createVnPayUrl as any).mockResolvedValue({
      paymentUrl:
        "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?token=123",
    });

    // Mock window.location.href
    delete (window as any).location;
    (window as any).location = { href: "" };

    render(<ThanhToanPage />);

    await waitFor(() => {
      expect(screen.getByText(/Cổng VNPay Sandbox/i)).toBeInTheDocument();
    });

    // VNPay là mặc định (method 3)
    const submitBtn = screen.getByRole("button", {
      name: /Thanh Toán Qua VNPay/i,
    });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shopOrderApi.checkout).toHaveBeenCalledWith(
        expect.objectContaining({
          paymentMethod: 3,
        }),
      );
      expect(shopPaymentApi.createVnPayUrl).toHaveBeenCalledWith({
        orderCode: "ORD-20260830-020",
        orderDescription: "Thanh toán đơn hàng Solaris ORD-20260830-020",
      });
      expect(window.location.href).toBe(
        "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?token=123",
      );
    });
  });

  // TC06: ĐẶT HÀNG THÀNH CÔNG VỚI ĐỊA CHỈ CÓ ID = 0
  it("TC06 - Cho phép checkout thành công khi địa chỉ nhận hàng có id = 0", async () => {
    const zeroIdAddress = [
      {
        id: 0,
        receiverName: "Nguyễn Văn A",
        phone: "0773259367",
        province: "TP. Hồ Chí Minh",
        district: "Thành Phố Thủ Đức",
        ward: "Phường Hiệp Phú",
        streetAddress: "123 Xa Lộ Hà Nội",
        fullAddress:
          "123 Xa Lộ Hà Nội, Phường Hiệp Phú, Thành Phố Thủ Đức, TP. Hồ Chí Minh",
        isDefault: true,
        latitude: 10.8494,
        longitude: 106.7537,
      },
    ];
    (shopCustomerApi.getAddresses as any).mockResolvedValue(zeroIdAddress);
    (shopOrderApi.checkout as any).mockResolvedValue({
      id: 30,
      orderCode: "ORD-20260913-030",
      totalAmount: 185000,
    });

    render(<ThanhToanPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/123 Xa Lộ Hà Nội, Phường Hiệp Phú/i),
      ).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole("button", {
      name: /Thanh Toán Qua VNPay/i,
    });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shopOrderApi.checkout).toHaveBeenCalledWith(
        expect.objectContaining({
          customerAddressId: 0,
        }),
      );
      expect(
        screen.queryByText("Vui lòng chọn hoặc thêm địa chỉ nhận hàng."),
      ).not.toBeInTheDocument();
    });
  });
});
