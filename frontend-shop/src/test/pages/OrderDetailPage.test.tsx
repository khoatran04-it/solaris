import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import OrderDetailPage from "@/app/tai-khoan/don-hang/[orderCode]/page";
import { useAuthStore } from "@/stores/authStore";
import shopOrderApi from "@/api/shopOrderApi";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useParams: () => ({
    orderCode: "ORD-20260830-100",
  }),
}));

vi.mock("@/api/shopOrderApi", () => ({
  default: {
    getByCode: vi.fn(),
    cancel: vi.fn(),
  },
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * PAGE TEST: OrderDetailPage (Chi Tiết Đơn Hàng & Hủy Đơn)
 * ============================================================================
 */
describe("Module 13 - OrderDetailPage Component", () => {
  const mockOrder = {
    id: 100,
    orderCode: "ORD-20260830-100",
    orderDate: "2026-08-30T08:00:00Z",
    status: 2,
    statusName: "Đã xác nhận",
    paymentStatus: 2,
    paymentStatusName: "Đã thanh toán",
    paymentMethod: 3,
    paymentMethodName: "VNPay Sandbox",
    subTotal: 300000,
    discountAmount: 30000,
    shippingFee: 25000,
    totalAmount: 295000,
    receiverName: "Nguyễn Văn A",
    receiverPhone: "0901234567",
    deliveryAddress: "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
    note: "Giao trong giờ hành chính",
    trackingCode: "GHN-SOLARIS-999",
    expectedDeliveryDate: "2026-09-01",
    cancellationReason: "",
    items: [
      {
        detailId: 1,
        variantId: 10,
        variantName: "Xoài Cát Hòa Lộc Hộp 1kg",
        variantCode: "SKU-XOAI",
        imagePath: "/images/xoai.jpg",
        uoMId: 1,
        uoMName: "Hộp 1kg",
        quantity: 2,
        unitPrice: 150000,
        discountAmount: 30000,
        totalPrice: 270000,
        issuedQuantity: 0,
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      isAuthenticated: true,
      user: null,
      token: "mock-token",
    });
    (shopOrderApi.getByCode as any).mockResolvedValue(mockOrder);
    (shopOrderApi.cancel as any).mockResolvedValue({
      message: "Hủy đơn hàng thành công",
    });
  });

  // TC01: CHUYỂN HƯỚNG NẾU CHƯA ĐĂNG NHẬP
  it("TC01 - Chưa đăng nhập: chuyển hướng sang /dang-nhap?redirect=/tai-khoan/don-hang/:orderCode", async () => {
    useAuthStore.setState({ isAuthenticated: false });

    render(<OrderDetailPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith(
        "/dang-nhap?redirect=/tai-khoan/don-hang/ORD-20260830-100",
      );
    });
  });

  // TC02: RENDER CHI TIẾT ĐƠN HÀNG, SẢN PHẨM & MÃ VẬN ĐƠN GHN
  it("TC02 - Render chi tiết đơn hàng, danh sách sản phẩm, địa chỉ nhận và mã vận đơn GHN", async () => {
    render(<OrderDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/Đơn Hàng\s*#?ORD-20260830-100/i),
      ).toBeInTheDocument();
      expect(screen.getByText("Đã xác nhận")).toBeInTheDocument();
      expect(screen.getByText("Xoài Cát Hòa Lộc Hộp 1kg")).toBeInTheDocument();
      expect(
        screen.getByText(/123 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM/i),
      ).toBeInTheDocument();
      expect(screen.getByText(/295\.000/)).toBeInTheDocument();
    });
  });

  // TC03: HIỂN THỊ NÚT HỦY ĐƠN VỚI TRẠNG THÁI ĐÃ XÁC NHẬN
  it('TC03 - Hiển thị nút Hủy đơn hàng khi trạng thái là "Đã xác nhận"', async () => {
    render(<OrderDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /Hủy đơn hàng/i }),
      ).toBeInTheDocument();
    });
  });

  // TC04: MỞ MODAL HỦY ĐƠN VÀ GỌI API CANCEL
  it("TC04 - Mở modal hủy đơn, nhập lý do và gọi shopOrderApi.cancel thành công", async () => {
    render(<OrderDetailPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /Hủy đơn hàng/i }),
      ).toBeInTheDocument();
    });

    const cancelBtn = screen.getByRole("button", { name: /Hủy đơn hàng/i });
    fireEvent.click(cancelBtn);

    // Modal xuất hiện
    expect(screen.getByText("Xác Nhận Hủy Đơn Hàng")).toBeInTheDocument();

    const textarea = screen.getByPlaceholderText(/Nhập lý do hủy đơn/i);
    fireEvent.change(textarea, {
      target: { value: "Khách hàng đặt nhầm số lượng" },
    });

    const submitCancelBtn = screen.getByRole("button", {
      name: /Xác Nhận Hủy Đơn/i,
    });
    fireEvent.click(submitCancelBtn);

    await waitFor(() => {
      expect(shopOrderApi.cancel).toHaveBeenCalledWith("ORD-20260830-100", {
        reason: "Khách hàng đặt nhầm số lượng",
      });
    });
  });

  // TC05: HIỂN THỊ THÔNG BÁO KHÔNG TÌM THẤY ĐƠN HÀNG
  it("TC05 - Hiển thị thông báo khi không tìm thấy đơn hàng tương ứng", async () => {
    (shopOrderApi.getByCode as any).mockResolvedValue(null);

    render(<OrderDetailPage />);

    await waitFor(() => {
      expect(screen.getByText("Không tìm thấy đơn hàng")).toBeInTheDocument();
    });
  });
});
