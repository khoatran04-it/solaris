import { describe, it, expect } from "vitest";
import React from "react";
import { render, screen } from "@testing-library/react";
import ProductCardMini from "@/components/chat/ProductCardMini";
import OrderSuccessCard from "@/components/chat/OrderSuccessCard";

describe("Module 15 - ProductCardMini & OrderSuccessCard (UI Testing in RAM)", () => {
  it("TC01 - Render ProductCardMini với thông tin nông sản, tiêu chuẩn và giá", () => {
    const mockProduct = {
      id: 1,
      variantId: 101,
      name: "Bơ Sáp 034 Lâm Đồng",
      slug: "bo-sap-034",
      imagePath: "/images/bo.jpg",
      price: 80000,
      discountedPrice: 80000,
      uoMName: "Kg",
      origin: "Lâm Đồng",
      certification: "VietGAP",
      brixLevel: "14°Bx",
      isInStock: true,
    };

    render(<ProductCardMini product={mockProduct} />);

    expect(screen.getByText("Bơ Sáp 034 Lâm Đồng")).toBeInTheDocument();
    expect(screen.getByText("Lâm Đồng")).toBeInTheDocument();
    expect(screen.getByText("VietGAP")).toBeInTheDocument();
    expect(screen.getByText(/80.000/)).toBeInTheDocument();
  });

  it("TC02 - Render OrderSuccessCard với mã đơn hàng và link chuyển hướng", () => {
    const mockPayload = {
      orderId: 55,
      orderCode: "ORD-SUCCESS-888",
      totalAmount: 350000,
      paymentMethodName: "Cổng VNPay Sandbox",
      paymentUrl:
        "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=35000000",
    };

    render(<OrderSuccessCard payload={mockPayload} />);

    expect(
      screen.getByText("Đặt hàng thành công qua AI Chatbot!"),
    ).toBeInTheDocument();
    expect(screen.getByText("ORD-SUCCESS-888")).toBeInTheDocument();
    expect(screen.getByText(/350.000/)).toBeInTheDocument();
    expect(screen.getByText("Cổng VNPay Sandbox")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Cổng VNPay/i })).toHaveAttribute(
      "href",
      mockPayload.paymentUrl,
    );
    expect(screen.getByRole("link", { name: /Xem đơn hàng/i })).toHaveAttribute(
      "href",
      "/tai-khoan/don-hang/ORD-SUCCESS-888",
    );
  });
});
