import { describe, it, expect, vi, beforeEach } from "vitest";
import shopOrderApi from "@/api/shopOrderApi";
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
 * FRONTEND SHOP - MODULE 13: SALES ORDERS & CUSTOMER RETURNS
 * API TEST: shopOrderApi (Shop Order Client API)
 * ============================================================================
 */
describe("Module 13 - shopOrderApi Client", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // TC01: CHECKOUT TẠO ĐƠN HÀNG
  it("TC01 - checkout gửi POST request tới /orders/checkout với payload đặt hàng", async () => {
    const payload = {
      customerAddressId: 1,
      paymentMethod: 1,
      shippingFee: 25000,
      note: "Giao giờ hành chính",
    };

    const mockResponse = {
      id: 10,
      orderCode: "ORD-20260830-010",
      totalAmount: 250000,
    };

    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const result = await shopOrderApi.checkout(payload);

    expect(axiosClient.post).toHaveBeenCalledWith("/orders/checkout", payload);
    expect(result).toEqual(mockResponse);
  });

  // TC02: LẤY DANH SÁCH ĐƠN HÀNG PHÂN TRANG
  it("TC02 - getAll gửi GET request tới /orders với params phân trang", async () => {
    const mockPagedResult = {
      items: [{ id: 1, orderCode: "ORD-001" }],
      totalRecords: 1,
      totalPages: 1,
      currentPage: 1,
      pageSize: 10,
    };

    (axiosClient.get as any).mockResolvedValue(mockPagedResult);

    const result = await shopOrderApi.getAll(1, 10);

    expect(axiosClient.get).toHaveBeenCalledWith("/orders", {
      params: { pageIndex: 1, pageSize: 10 },
    });
    expect(result).toEqual(mockPagedResult);
  });

  // TC03: LẤY CHI TIẾT ĐƠN HÀNG THEO MÃ
  it("TC03 - getByCode gửi GET request tới /orders/:orderCode", async () => {
    const mockOrder = {
      id: 10,
      orderCode: "ORD-20260830-010",
      statusName: "Đã xác nhận",
      items: [],
    };

    (axiosClient.get as any).mockResolvedValue(mockOrder);

    const result = await shopOrderApi.getByCode("ORD-20260830-010");

    expect(axiosClient.get).toHaveBeenCalledWith("/orders/ORD-20260830-010");
    expect(result).toEqual(mockOrder);
  });

  // TC04: HỦY ĐƠN HÀNG
  it("TC04 - cancel gửi POST request tới /orders/:orderCode/cancel với lý do hủy", async () => {
    const cancelPayload = {
      reason: "Khách hàng đổi ý muốn chọn sản phẩm khác",
    };
    const mockResponse = { message: "Hủy đơn hàng thành công" };

    (axiosClient.post as any).mockResolvedValue(mockResponse);

    const result = await shopOrderApi.cancel("ORD-20260830-010", cancelPayload);

    expect(axiosClient.post).toHaveBeenCalledWith(
      "/orders/ORD-20260830-010/cancel",
      cancelPayload,
    );
    expect(result).toEqual(mockResponse);
  });
});
