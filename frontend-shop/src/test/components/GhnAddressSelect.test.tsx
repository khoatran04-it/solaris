import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom/vitest";
import GhnAddressSelect from "@/components/address/GhnAddressSelect";
import shopShippingApi from "@/api/shopShippingApi";

vi.mock("@/api/shopShippingApi", () => ({
  default: {
    getProvinces: vi.fn(),
    getDistricts: vi.fn(),
    getWards: vi.fn(),
  },
}));

describe("Module 14 - GhnAddressSelect Component (Shop)", () => {
  const mockProvinces = [
    { provinceID: 201, provinceName: "Hồ Chí Minh", code: "HCM" },
    { provinceID: 202, provinceName: "Hà Nội", code: "HN" },
  ];

  const mockDistricts = [
    { districtID: 1442, provinceID: 201, districtName: "Quận 1", code: "Q1" },
  ];

  const mockWards = [
    { wardCode: "20101", districtID: 1442, wardName: "Phường Bến Nghé" },
  ];

  const mockOnChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (shopShippingApi.getProvinces as any).mockResolvedValue(mockProvinces);
    (shopShippingApi.getDistricts as any).mockResolvedValue(mockDistricts);
    (shopShippingApi.getWards as any).mockResolvedValue(mockWards);
  });

  it("TC01 - Render 3 dropdowns và load danh sách Tỉnh/Thành", async () => {
    render(
      <GhnAddressSelect
        province=""
        district=""
        ward=""
        onChange={mockOnChange}
      />,
    );

    expect(screen.getByText(/Tỉnh \/ Thành phố/i)).toBeInTheDocument();
    expect(screen.getByText(/Quận \/ Huyện/i)).toBeInTheDocument();
    expect(screen.getByText(/Phường \/ Xã/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(shopShippingApi.getProvinces).toHaveBeenCalled();
      expect(screen.getByText("Hồ Chí Minh")).toBeInTheDocument();
    });
  });

  it("TC02 - Chọn Tỉnh -> Gọi onChange và kích hoạt load Quận/Huyện", async () => {
    render(
      <GhnAddressSelect
        province=""
        district=""
        ward=""
        onChange={mockOnChange}
      />,
    );

    await waitFor(() => {
      expect(screen.getByText("Hồ Chí Minh")).toBeInTheDocument();
    });

    const select = screen.getAllByRole("combobox")[0];
    fireEvent.change(select, { target: { value: "201" } });

    expect(mockOnChange).toHaveBeenCalledWith(
      expect.objectContaining({
        province: "Hồ Chí Minh",
        ghnProvinceId: 201,
      }),
    );
  });
});
