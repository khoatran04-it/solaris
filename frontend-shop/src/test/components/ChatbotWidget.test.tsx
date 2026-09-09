import { describe, it, expect, vi, beforeEach } from "vitest";
import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import ChatbotWidget from "@/components/chat/ChatbotWidget";
import shopAiApi from "@/api/shopAiApi";
import { useAuthStore } from "@/stores/authStore";

vi.mock("@/api/shopAiApi", () => ({
  default: {
    getSessions: vi.fn(),
    getSessionMessages: vi.fn(),
    createSession: vi.fn(),
    deleteSession: vi.fn(),
    sendMessage: vi.fn(),
    confirmOrder: vi.fn(),
  },
}));

describe("Module 15 - ChatbotWidget Component (UI Testing in RAM)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Mock scrollIntoView in jsdom
    window.HTMLElement.prototype.scrollIntoView = vi.fn();
    useAuthStore.setState({
      isAuthenticated: true,
      user: {
        id: 1,
        code: "CUST-01",
        name: "Khoa Tran",
        phoneNumber: "0901234567",
        discountPercent: 0,
      },
    });
  });

  it("TC01 - Render nút mở Chatbot dạng nổi (Floating Button)", () => {
    render(<ChatbotWidget />);

    const chatButton = screen.getByRole("button", {
      name: /Trợ Lý Nông Sản AI/i,
    });
    expect(chatButton).toBeInTheDocument();
    expect(screen.getByText(/Trợ Lý Nông Sản AI/i)).toBeInTheDocument();
    expect(screen.getByText(/3.5 Flash/i)).toBeInTheDocument();
  });

  it("TC02 - Mở khung Chat khi click vào nút và tải phiên hội thoại", async () => {
    (shopAiApi.getSessions as any).mockResolvedValue([
      {
        id: 1,
        title: "Tư vấn bơ 034",
        totalMessages: 2,
        updatedAt: "2026-08-30",
      },
    ]);
    (shopAiApi.getSessionMessages as any).mockResolvedValue([
      { id: 10, role: "user", content: "Bơ sáp có ngon không?" },
      { id: 11, role: "model", content: "Dạ bơ sáp 034 dẻo béo thơm ngon ạ!" },
    ]);

    render(<ChatbotWidget />);

    const openBtn = screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i });
    fireEvent.click(openBtn);

    await waitFor(() => {
      expect(screen.getByText("Solaris AI Commerce")).toBeInTheDocument();
      expect(
        screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i),
      ).toBeInTheDocument();
    });
  });

  it("TC03 - Gửi tin nhắn và render câu trả lời từ AI trong RAM", async () => {
    (shopAiApi.getSessions as any).mockResolvedValue([]);
    (shopAiApi.createSession as any).mockResolvedValue({
      id: 1,
      title: "Cuộc trò chuyện mới",
    });
    (shopAiApi.sendMessage as any).mockResolvedValue({
      sessionId: 1,
      messageId: 100,
      content: "Dạ Solaris có sầu riêng Ri6 tươi ngon!",
      payloadType: "none",
      createdAt: new Date().toISOString(),
    });

    render(<ChatbotWidget />);

    // Mở chat
    fireEvent.click(
      screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i }),
    );

    await waitFor(() => {
      expect(
        screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i),
      ).toBeInTheDocument();
    });

    const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
    fireEvent.change(input, { target: { value: "Có sầu riêng Ri6 không?" } });

    const form = input.closest("form")!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(shopAiApi.sendMessage).toHaveBeenCalledWith(
        expect.objectContaining({ message: "Có sầu riêng Ri6 không?" }),
      );
      expect(
        screen.getByText(/Dạ Solaris có sầu riêng Ri6 tươi ngon!/i),
      ).toBeInTheDocument();
    });
  });

  it("TC04 - Render Thẻ Đơn Hàng Tương Tác khi AI trả về interactive_order payload", async () => {
    (shopAiApi.getSessions as any).mockResolvedValue([]);
    (shopAiApi.createSession as any).mockResolvedValue({
      id: 1,
      title: "Cuộc trò chuyện mới",
    });
    (shopAiApi.sendMessage as any).mockResolvedValue({
      sessionId: 1,
      messageId: 101,
      content: "Em đã chuẩn bị sẵn đơn hàng cho bạn:",
      payloadType: "interactive_order",
      payload: {
        title: "Đơn Hàng Gợi Ý Bơ 034",
        items: [
          {
            variantId: 1,
            variantCode: "VAR-BO-01",
            variantName: "Bơ Sáp 034 VIP",
            uoMId: 1,
            uoMName: "Kg",
            unitPrice: 85000,
            quantity: 2,
            discountAmount: 0,
            totalPrice: 170000,
          },
        ],
        subTotal: 170000,
        totalDiscount: 0,
        shippingFee: 25000,
        totalAmount: 195000,
        isFreeShipping: false,
        freeShippingThreshold: 300000,
      },
    });

    render(<ChatbotWidget />);

    fireEvent.click(
      screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i }),
    );

    await waitFor(() => {
      expect(
        screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i),
      ).toBeInTheDocument();
    });

    const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
    fireEvent.change(input, { target: { value: "Lên đơn 2kg bơ 034" } });

    const form = input.closest("form")!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(screen.getByText("Đơn Hàng Gợi Ý Bơ 034")).toBeInTheDocument();
      expect(screen.getByText("Bơ Sáp 034 VIP")).toBeInTheDocument();
    });
  });

  it("TC05 - Render Thẻ Tra Cứu Đơn Hàng khi AI trả về order_tracking payload", async () => {
    (shopAiApi.getSessions as any).mockResolvedValue([]);
    (shopAiApi.createSession as any).mockResolvedValue({
      id: 1,
      title: "Cuộc trò chuyện mới",
    });
    (shopAiApi.sendMessage as any).mockResolvedValue({
      sessionId: 1,
      messageId: 102,
      content:
        "Dạ đơn hàng ORD-20260905-0001 đang được GHN Express vận chuyển:",
      payloadType: "order_tracking",
      payload: {
        orderId: 99,
        orderCode: "ORD-20260905-0001",
        orderDate: "2026-09-05T10:00:00Z",
        status: 3,
        statusName: "Đang giao hàng",
        paymentStatus: 1,
        paymentStatusName: "Đã thanh toán",
        paymentMethod: 3,
        paymentMethodName: "VNPay Sandbox",
        subTotal: 340000,
        shippingFee: 0,
        totalAmount: 340000,
        shippingProvider: "GHN Express (2H Nông Sản)",
        items: [
          {
            variantName: "Bơ Sáp 034 VIP",
            quantity: 4,
            uoMName: "Kg",
            totalPrice: 340000,
          },
        ],
      },
    });

    render(<ChatbotWidget />);

    fireEvent.click(
      screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i }),
    );

    await waitFor(() => {
      expect(screen.getByText(/Solaris AI xin chào bạn/i)).toBeInTheDocument();
    });

    const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
    fireEvent.change(input, {
      target: { value: "Kiểm tra đơn ORD-20260905-0001" },
    });

    const form = input.closest("form")!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(shopAiApi.sendMessage).toHaveBeenCalled();
      expect(screen.getByText("ORD-20260905-0001")).toBeInTheDocument();
      expect(screen.getByText("Đang giao hàng")).toBeInTheDocument();
      expect(screen.getAllByText(/GHN Express/i).length).toBeGreaterThanOrEqual(
        1,
      );
      expect(screen.getByText("Xem Chi Tiết Đơn Hàng")).toBeInTheDocument();
    });
  });

  it("TC06 - Render nút Thêm tất cả vào giỏ khi AI trả về danh sách product_cards", async () => {
    (shopAiApi.getSessions as any).mockResolvedValue([]);
    (shopAiApi.createSession as any).mockResolvedValue({
      id: 1,
      title: "Cuộc trò chuyện mới",
    });
    (shopAiApi.sendMessage as any).mockResolvedValue({
      sessionId: 1,
      messageId: 103,
      content: "Dưới đây là một số loại trái cây tươi ngon hôm nay:",
      payloadType: "product_cards",
      payload: [
        {
          id: 1,
          variantId: 10,
          uoMId: 1,
          name: "Bơ Sáp 034",
          slug: "bo-sap-034",
          price: 85000,
          discountedPrice: 85000,
          uoMName: "Kg",
          isInStock: true,
        },
        {
          id: 2,
          variantId: 20,
          uoMId: 1,
          name: "Sầu Riêng Ri6",
          slug: "sau-rieng-ri6",
          price: 150000,
          discountedPrice: 150000,
          uoMName: "Kg",
          isInStock: true,
        },
      ],
    });

    render(<ChatbotWidget />);

    fireEvent.click(
      screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i }),
    );

    await waitFor(() => {
      expect(screen.getByText(/Solaris AI xin chào bạn/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i)).not.toBeDisabled();
    });

    const input = screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i);
    fireEvent.change(input, { target: { value: "Tư vấn trái cây" } });

    const form = input.closest("form")!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(screen.getByText("Bơ Sáp 034")).toBeInTheDocument();
      expect(screen.getByText("Sầu Riêng Ri6")).toBeInTheDocument();
      expect(screen.getByText(/Thêm tất cả vào giỏ/i)).toBeInTheDocument();
    });
  });

  it("TC07 - Cho phép khách vãng lai (chưa đăng nhập) mở khung chat tư vấn và tải phiên chat của guest", async () => {
    useAuthStore.setState({
      isAuthenticated: false,
      user: null,
      token: null,
    });

    render(<ChatbotWidget />);

    const openBtn = screen.getByRole("button", { name: /Trợ Lý Nông Sản AI/i });
    fireEvent.click(openBtn);

    await waitFor(() => {
      expect(screen.getByText(/Solaris AI xin chào bạn/i)).toBeInTheDocument();
      expect(
        screen.getByPlaceholderText(/Hỏi AI hoặc gõ/i),
      ).toBeInTheDocument();
    });

    // Khách vãng lai vẫn được đồng bộ phiên chat qua guest session token
    expect(shopAiApi.getSessions).toHaveBeenCalled();
  });
});
