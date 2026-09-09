import { describe, it, expect, vi, beforeEach } from "vitest";
import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import InteractiveOrderCard from "@/components/chat/InteractiveOrderCard";
import shopAiApi from "@/api/shopAiApi";
import shopCustomerApi from "@/api/shopCustomerApi";
import { useAuthStore } from "@/stores/authStore";

vi.mock("@/api/shopAiApi", () => ({
  default: {
    confirmOrder: vi.fn(),
  },
}));

vi.mock("@/api/shopCustomerApi", () => ({
  default: {
    getAddresses: vi.fn(),
  },
}));

describe("Module 15 - InteractiveOrderCard Component (UI Testing in RAM)", () => {
  const mockAddresses = [
    {
      id: 1,
      receiverName: "Khoa Tran",
      phone: "0912345678",
      province: "TP Hồ Chí Minh",
      district: "Quận 1",
      ward: "Phường Bến Nghé",
      streetAddress: "123 Le Loi",
      fullAddress: "123 Le Loi, Phường Bến Nghé, Quận 1, TP Hồ Chí Minh",
      isDefault: true,
      latitude: 10.7769,
      longitude: 106.7009,
    },
    {
      id: 2,
      receiverName: "Tran Van A",
      phone: "0987654321",
      province: "TP Hồ Chí Minh",
      district: "Quận 3",
      ward: "Phường 6",
      streetAddress: "456 Vo Van Tan",
      fullAddress: "456 Vo Van Tan, Phường 6, Quận 3, TP Hồ Chí Minh",
      isDefault: false,
      latitude: 10.778,
      longitude: 106.69,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    (shopCustomerApi.getAddresses as any).mockResolvedValue(mockAddresses);
    useAuthStore.setState({
      isAuthenticated: true,
      user: { id: 1, name: "Khoa Tran" } as any,
      token: "mock-token",
    });
  });

  const mockPayload = {
    title: "Đơn Hàng Đặt Lại Bơ Sáp",
    previousOrderCode: "ORD-OLD-123",
    items: [
      {
        variantId: 101,
        variantCode: "VAR-BO-01",
        variantName: "Bơ Sáp 034 Đặc Sản",
        uoMId: 1,
        uoMName: "Kg",
        unitPrice: 100000,
        quantity: 2,
        availableStock: 5,
        discountAmount: 0,
        totalPrice: 200000,
      },
    ],
    subTotal: 200000,
    totalDiscount: 0,
    shippingFee: 25000,
    totalAmount: 225000,
    isFreeShipping: false,
    freeShippingThreshold: 300000,
    suggestedReceiverName: "Khoa Tran",
    suggestedReceiverPhone: "0912345678",
    suggestedDeliveryAddress:
      "123 Le Loi, Phường Bến Nghé, Quận 1, TP Hồ Chí Minh",
  };

  it("TC01 - Render thông tin sản phẩm, tính toán tổng tiền và hiển thị dropdown sổ địa chỉ", async () => {
    render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

    expect(screen.getByText("Đơn Hàng Đặt Lại Bơ Sáp")).toBeInTheDocument();
    expect(screen.getByText("Bơ Sáp 034 Đặc Sản")).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.getByLabelText(/Chọn địa chỉ nhận hàng/i),
      ).toBeInTheDocument();
      expect(screen.getAllByText(/Khoa Tran/i).length).toBeGreaterThanOrEqual(
        1,
      );
      expect(screen.getAllByText(/0912345678/i).length).toBeGreaterThanOrEqual(
        1,
      );
      expect(screen.getAllByText(/123 Le Loi/i).length).toBeGreaterThanOrEqual(
        1,
      );
    });
  });

  it("TC02 - Tăng số lượng sản phẩm và tự động đạt Freeship khi >= 300k", async () => {
    render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

    // Ban đầu 2kg * 100k = 200k (< 300k -> có phí ship 25k)
    expect(screen.getByText(/FREESHIP XE LẠNH/i)).toBeInTheDocument();

    // Bấm nút '+' để tăng lên 3kg (3kg * 100k = 300k -> Đạt Freeship)
    const plusButton = screen.getByTitle("Tăng số lượng");
    fireEvent.click(plusButton);

    expect(screen.getByText("3")).toBeInTheDocument();
    expect(screen.getByText(/MIỄN PHÍ VẬN CHUYỂN/i)).toBeInTheDocument();
  });

  it("TC03 - Xác nhận đặt hàng thành công qua COD", async () => {
    (shopAiApi.confirmOrder as any).mockResolvedValue({
      orderId: 99,
      orderCode: "ORD-20260830-777",
      totalAmount: 225000,
      paymentMethodName: "Thanh toán khi nhận (COD)",
    });

    render(<InteractiveOrderCard sessionId={1} payload={mockPayload} />);

    await waitFor(() => {
      expect(
        screen.getByLabelText(/Chọn địa chỉ nhận hàng/i),
      ).toBeInTheDocument();
    });

    // Chọn COD
    const codRadio = screen.getByLabelText(/Khi nhận \(COD\)/i);
    fireEvent.click(codRadio);

    // Bấm Xác nhận
    const submitBtn = screen.getByRole("button", {
      name: /Xác Nhận Đặt Đơn Này/i,
    });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(shopAiApi.confirmOrder).toHaveBeenCalledWith(
        expect.objectContaining({
          sessionId: 1,
          paymentMethod: 1,
          customerAddressId: 1,
        }),
      );
      expect(
        screen.getByText("ĐÃ TẠO ĐƠN HÀNG THÀNH CÔNG!"),
      ).toBeInTheDocument();
      expect(screen.getByText("ORD-20260830-777")).toBeInTheDocument();
    });
  });

  it("TC04 - Cảnh báo khi khách hàng chưa cập nhật địa chỉ và disable nút đặt hàng", async () => {
    (shopCustomerApi.getAddresses as any).mockResolvedValue([]);

    const payloadWithoutAddr = {
      ...mockPayload,
      suggestedDeliveryAddress: undefined,
      suggestedReceiverName: undefined,
      suggestedReceiverPhone: undefined,
    };

    render(<InteractiveOrderCard sessionId={1} payload={payloadWithoutAddr} />);

    await waitFor(() => {
      expect(
        screen.getByText(
          "Bạn chưa cập nhật địa chỉ nhận hàng trong sổ địa chỉ.",
        ),
      ).toBeInTheDocument();
      expect(
        screen.getByRole("link", { name: /Cập nhật địa chỉ ngay/i }),
      ).toHaveAttribute("href", "/tai-khoan/dia-chi");
    });

    const submitBtn = screen.getByRole("button", {
      name: /Xác Nhận & Thanh Toán VNPay/i,
    });
    expect(submitBtn).toBeDisabled();
  });

  it("TC05 - Chặn tăng số lượng vượt quá tồn kho khả dụng", async () => {
    window.alert = vi.fn();
    const payloadLowStock = {
      ...mockPayload,
      items: [
        {
          ...mockPayload.items[0],
          quantity: 2,
          availableStock: 2, // Đã chạm giới hạn
        },
      ],
    };

    render(<InteractiveOrderCard sessionId={1} payload={payloadLowStock} />);

    const plusBtn = screen.getByTitle(/Đã đạt giới hạn tồn kho/i);
    expect(plusBtn).toBeDisabled();
  });
});
