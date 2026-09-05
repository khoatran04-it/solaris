import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import DonHangListPage from "@/app/tai-khoan/don-hang/page";
import { useAuthStore } from "@/stores/authStore";
import shopOrderApi from "@/api/shopOrderApi";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

vi.mock("@/api/shopOrderApi", () => ({
  default: {
    getAll: vi.fn(),
  },
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * PAGE TEST: DonHangListPage (Lịch Sử Đơn Hàng Của Khách)
 * ============================================================================
 */
describe("Module 13 - DonHangListPage Component", () => {
  const mockOrders = [
    {
      id: 1,
      orderCode: "ORD-20260830-001",
      orderDate: "2026-08-30T08:00:00Z",
      status: 2,
      statusName: "Đã xác nhận",
      paymentStatus: 2,
      paymentStatusName: "Đã thanh toán",
      paymentMethod: 1,
      paymentMethodName: "Tiền mặt (COD)",
      subTotal: 200000,
      discountAmount: 20000,
      shippingFee: 25000,
      totalAmount: 205000,
      items: [
        {
          detailId: 1,
          variantId: 10,
          variantName: "Bơ Booth 7 Đắk Lắk",
          variantCode: "SKU-BO-KG",
          uoMId: 1,
          uoMName: "Kg",
          quantity: 2,
          unitPrice: 100000,
          discountAmount: 20000,
          totalPrice: 180000,
          issuedQuantity: 0,
        },
      ],
    },
    {
      id: 2,
      orderCode: "ORD-20260830-002",
      orderDate: "2026-08-30T09:00:00Z",
      status: 5,
      statusName: "Giao thành công",
      paymentStatus: 2,
      paymentStatusName: "Đã thanh toán",
      paymentMethod: 3,
      paymentMethodName: "VNPay Sandbox",
      subTotal: 500000,
      discountAmount: 0,
      shippingFee: 0,
      totalAmount: 500000,
      items: [
        {
          detailId: 2,
          variantId: 20,
          variantName: "Xoài Cát Hòa Lộc",
          variantCode: "SKU-XOAI",
          uoMId: 1,
          uoMName: "Hộp 1kg",
          quantity: 3,
          unitPrice: 150000,
          discountAmount: 0,
          totalPrice: 450000,
          issuedQuantity: 3,
        },
      ],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      isAuthenticated: true,
      user: null,
      token: "mock-token",
    });
    (shopOrderApi.getAll as any).mockResolvedValue({
      items: mockOrders,
      totalRecords: 2,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    });
  });

  // TC01: CHUYỂN HƯỚNG NẾU CHƯA ĐĂNG NHẬP
  it("TC01 - Chưa đăng nhập: chuyển hướng sang /dang-nhap?redirect=/tai-khoan/don-hang", async () => {
    useAuthStore.setState({ isAuthenticated: false });

    render(<DonHangListPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith(
        "/dang-nhap?redirect=/tai-khoan/don-hang",
      );
    });
  });

  // TC02: RENDER DANH SÁCH ĐƠN HÀNG
  it("TC02 - Đã đăng nhập: render danh sách đơn hàng với mã đơn, badge trạng thái và tổng tiền", async () => {
    render(<DonHangListPage />);

    await waitFor(() => {
      expect(screen.getByText("ORD-20260830-001")).toBeInTheDocument();
      expect(screen.getByText("ORD-20260830-002")).toBeInTheDocument();
      expect(screen.getByText("Đã xác nhận")).toBeInTheDocument();
      expect(screen.getByText("Giao thành công")).toBeInTheDocument();
      expect(
        screen.getByText("2 x Bơ Booth 7 Đắk Lắk (Kg)"),
      ).toBeInTheDocument();
      expect(
        screen.getByText("3 x Xoài Cát Hòa Lộc (Hộp 1kg)"),
      ).toBeInTheDocument();
      expect(screen.getByText("205.000 ₫")).toBeInTheDocument();
      expect(screen.getByText("500.000 ₫")).toBeInTheDocument();
    });
  });

  // TC03: HIỂN THỊ TRẠNG THÁI RỖNG
  it("TC03 - Hiển thị thông báo rỗng khi khách hàng chưa có đơn hàng nào", async () => {
    (shopOrderApi.getAll as any).mockResolvedValue({
      items: [],
      totalRecords: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
    });

    render(<DonHangListPage />);

    await waitFor(() => {
      expect(screen.getByText("Bạn chưa có đơn hàng nào")).toBeInTheDocument();
      expect(screen.getByText("Mua sắm ngay")).toBeInTheDocument();
    });
  });

  // TC04: LIÊN KẾT XEM CHI TIẾT ĐƠN HÀNG
  it("TC04 - Mỗi đơn hàng có liên kết điều hướng xem chi tiết", async () => {
    render(<DonHangListPage />);

    await waitFor(() => {
      const viewLinks = screen.getAllByRole("link", { name: /Xem chi tiết/i });
      expect(viewLinks).toHaveLength(2);
      expect(viewLinks[0]).toHaveAttribute(
        "href",
        "/tai-khoan/don-hang/ORD-20260830-001",
      );
      expect(viewLinks[1]).toHaveAttribute(
        "href",
        "/tai-khoan/don-hang/ORD-20260830-002",
      );
    });
  });
});
