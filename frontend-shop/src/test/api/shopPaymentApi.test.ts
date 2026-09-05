import { describe, it, expect, vi, beforeEach } from "vitest";
import axiosClient from "@/api/axiosClient";
import shopPaymentApi from "@/api/shopPaymentApi";

vi.mock("@/api/axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe("Module 14 - Shop Payment API Client (VNPay)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("TC01 - createVnPayUrl gửi POST /payment/vnpay/create-url và nhận paymentUrl", async () => {
    const mockPayload = {
      orderCode: "ORD-20260830-001",
      orderDescription: "Thanh toan don hang ORD-20260830-001",
    };
    const mockResponse = {
      orderCode: "ORD-20260830-001",
      paymentUrl:
        "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_TmnCode=VVRIW1BA",
    };
    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const res = await shopPaymentApi.createVnPayUrl(mockPayload);

    expect(axiosClient.post).toHaveBeenCalledWith(
      "/payment/vnpay/create-url",
      mockPayload,
    );
    expect(res.paymentUrl).toContain("sandbox.vnpayment.vn");
    expect(res.orderCode).toBe("ORD-20260830-001");
  });

  it("TC02 - getVnPayCallback gửi GET /payment/vnpay/callback kèm query string", async () => {
    const queryStr =
      "?vnp_Amount=25000000&vnp_ResponseCode=00&vnp_TxnRef=ORD-20260830-001";
    const mockResponse = {
      isSuccess: true,
      orderCode: "ORD-20260830-001",
      amount: 250000,
      transactionNo: "14567890",
      bankCode: "NCB",
      message: "Giao dịch thành công",
    };
    (axiosClient.get as any).mockResolvedValue(mockResponse);

    const res = await shopPaymentApi.getVnPayCallback(queryStr);

    expect(axiosClient.get).toHaveBeenCalledWith(
      `/payment/vnpay/callback${queryStr}`,
    );
    expect(res.isSuccess).toBe(true);
    expect(res.amount).toBe(250000);
    expect(res.transactionNo).toBe("14567890");
  });
});
