import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import ProductDetailClient from "@/components/product/ProductDetailClient";
import { useCartStore } from "@/stores/cartStore";

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
  useSearchParams: () => ({
    get: vi.fn().mockReturnValue(null),
  }),
}));

/**
 * ============================================================================
 * FRONTEND SHOP - MODULE 12: SHOPPING CART
 * COMPONENT TEST: ProductDetailClient (Thao Tác Thêm Vào Giỏ Hàng Từ Chi Tiết Sản Phẩm)
 * ============================================================================
 */
describe("Module 12 - ProductDetailClient Component", () => {
  const mockAddItem = vi.fn();

  const mockProduct = {
    id: 1,
    code: "PROD-BO",
    name: "Bơ Booth 7 Đắk Lắk",
    slug: "bo-booth-7-dak-lak",
    baseUoMId: 1,
    baseUoMName: "Kg",
    attributes: { "Xuất xứ": "Đắk Lắk" },
    activePromotions: [],
    variants: [
      {
        id: 10,
        code: "SKU-BO-KG",
        name: "Bơ Booth Loại 1 (Kg)",
        quantityAvailable: 50,
        isInStock: true,
        attributes: {},
        prices: [
          {
            priceId: 1,
            uoMId: 1,
            uoMName: "Kg",
            price: 100000,
            discountedPrice: 80000,
            discountPercent: 20,
            isDefault: true,
          },
          {
            priceId: 2,
            uoMId: 2,
            uoMName: "Hộp 2Kg",
            price: 190000,
            discountedPrice: 190000,
            discountPercent: 0,
            isDefault: false,
          },
        ],
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockAddItem.mockResolvedValue(undefined);
    useCartStore.setState({
      addItem: mockAddItem,
    });
  });

  // TC01: RENDER CHI TIẾT SẢN PHẨM & ĐƠN GIÁ
  it("TC01 - Render giá bán đã chiết khấu, badge giảm giá và ĐVT mặc định", () => {
    render(<ProductDetailClient product={mockProduct as any} />);

    expect(screen.getAllByText(/80\.000/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/100\.000/)).toBeInTheDocument();
    expect(screen.getByText("-20%")).toBeInTheDocument();
    expect(screen.getByText("/ Kg")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /Thêm Vào Giỏ Hàng/i }),
    ).toBeInTheDocument();
  });

  // TC02: THÊM SẢN PHẨM VÀO GIỎ HÀNG THÀNH CÔNG
  it('TC02 - Bấm "Thêm Vào Giỏ Hàng" gọi hàm addItem với VariantId, UoMId và số lượng', async () => {
    render(<ProductDetailClient product={mockProduct as any} />);

    const addToCartBtn = screen.getByRole("button", {
      name: /Thêm Vào Giỏ Hàng/i,
    });
    fireEvent.click(addToCartBtn);

    await waitFor(() => {
      expect(mockAddItem).toHaveBeenCalledWith(10, 1, 1);
    });

    expect(await screen.findByText(/Đã thêm vào giỏ/i)).toBeInTheDocument();
  });

  // TC03: ĐỒNG BỘ MÔ TẢ CHI TIẾT TỪ BIẾN THỂ (PRODUCT VARIANT DESCRIPTION)
  it("TC03 - Hiển thị mô tả riêng của biến thể khi admin nhập trong form biến thể", () => {
    const productWithVariantDesc = {
      ...mockProduct,
      description: "Thịt Heo nuôi chuẩn VietGAP",
      variants: [
        {
          ...mockProduct.variants[0],
          description:
            "Thịt heo ba chỉ là phần thịt nằm ở bụng heo, có lớp mỡ và nạc xen kẽ.",
        },
      ],
    };

    render(<ProductDetailClient product={productWithVariantDesc as any} />);

    expect(
      screen.getByText(
        /Thịt heo ba chỉ là phần thịt nằm ở bụng heo, có lớp mỡ và nạc xen kẽ./,
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Thịt Heo nuôi chuẩn VietGAP/),
    ).toBeInTheDocument();
  });
});
