import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import "@testing-library/jest-dom";
import DiaChiPage from "@/app/tai-khoan/dia-chi/page";
import { useAuthStore } from "@/stores/authStore";
import { useLocationStore } from "@/stores/locationStore";
import shopCustomerApi from "@/api/shopCustomerApi";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

vi.mock("@/api/shopCustomerApi", () => ({
  default: {
    getAddresses: vi.fn(),
    createAddress: vi.fn(),
    deleteAddress: vi.fn(),
    setDefaultAddress: vi.fn(),
  },
}));

vi.mock("@/components/address/GhnAddressSelect", () => ({
  default: ({ onChange }: any) => (
    <div data-testid="ghn-address-select">
      <button
        type="button"
        onClick={() =>
          onChange({
            province: "Hồ Chí Minh",
            district: "Quận Thủ Đức",
            ward: "Phường Linh Trung",
          })
        }
      >
        Select Location
      </button>
    </div>
  ),
}));

vi.mock("@/components/account/AccountSidebar", () => ({
  default: () => <div data-testid="account-sidebar" />,
}));

describe("DiaChiPage - Optional GPS Coordinates", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      isAuthenticated: true,
      token: "fake-jwt-token",
      customerInfo: {
        id: 1,
        code: "CUST-001",
        name: "Trần Đăng Khoa",
        phoneNumber: "0773259363",
        discountPercent: 0,
      },
    });

    // Giả lập LocationStore đã có GPS hiện tại của trình duyệt (ví dụ: Quận 1)
    useLocationStore.setState({
      userLatitude: 10.7616,
      userLongitude: 106.7058,
      isDetectingGps: false,
    });

    vi.mocked(shopCustomerApi.getAddresses).mockResolvedValue([
      {
        id: 1,
        receiverName: "Địa chỉ cũ",
        phone: "0901234567",
        province: "Hồ Chí Minh",
        district: "Quận 1",
        ward: "Phường Bến Nghé",
        streetAddress: "1 Công Xã Paris",
        fullAddress: "1 Công Xã Paris, Phường Bến Nghé, Quận 1, Hồ Chí Minh",
        isDefault: true,
        latitude: 10.7769,
        longitude: 106.7009,
      },
    ]);
  });

  it("khi mở form thêm địa chỉ, thanh GPS mặc định là TÙY CHỌN và KHÔNG bị tự động gán tọa độ hiện tại", async () => {
    render(<DiaChiPage />);

    await waitFor(() => {
      expect(screen.getByText("Thêm địa chỉ mới")).toBeInTheDocument();
    });

    // Mở form thêm địa chỉ
    fireEvent.click(screen.getByText("Thêm địa chỉ mới"));

    // Kiểm tra tiêu đề form
    expect(screen.getByText("Thêm Địa Chỉ Nhận Hàng Mới")).toBeInTheDocument();

    // Thanh GPS phải hiển thị là 'Tùy chọn' và 'Chưa gắn tọa độ'
    expect(screen.getByText(/Tọa độ GPS \(Tùy chọn\):/i)).toBeInTheDocument();
    expect(screen.getByText(/Chưa gắn tọa độ/i)).toBeInTheDocument();
    expect(screen.getByText("📍 Lấy GPS hiện tại")).toBeInTheDocument();

    // Tuyệt đối không tự gán tọa độ hiện tại 10.7616, 106.7058
    expect(screen.queryByText(/10.7616/)).not.toBeInTheDocument();
    expect(screen.queryByText("✕ Bỏ GPS")).not.toBeInTheDocument();
  });

  it("khi tạo địa chỉ mới không bấm lấy GPS, hệ thống gửi latitude = 0 và longitude = 0", async () => {
    vi.mocked(shopCustomerApi.createAddress).mockResolvedValue({} as any);

    render(<DiaChiPage />);

    await waitFor(() => {
      expect(screen.getByText("Thêm địa chỉ mới")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText("Thêm địa chỉ mới"));

    // Nhập thông tin địa chỉ ở Thủ Đức
    fireEvent.change(screen.getByRole("textbox", { name: /Họ tên người nhận \*/i }), {
      target: { value: "Trần Đăng Khoa" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: /Số điện thoại \*/i }), {
      target: { value: "0773259363" },
    });
    fireEvent.change(
      screen.getByRole("textbox", { name: /Địa chỉ cụ thể \(Số nhà, tên đường\) \*/i }),
      { target: { value: "123 Đường Lê Lợi" } }
    );

    // Bấm lưu địa chỉ
    fireEvent.click(screen.getByRole("button", { name: "Lưu địa chỉ" }));

    await waitFor(() => {
      expect(shopCustomerApi.createAddress).toHaveBeenCalledTimes(1);
    });

    // Kiểm tra payload gửi đi: latitude và longitude phải là 0, không bị gán GPS hiện tại
    const payload = vi.mocked(shopCustomerApi.createAddress).mock.calls[0][0];
    expect(payload.receiverName).toBe("Trần Đăng Khoa");
    expect(payload.latitude).toBe(0);
    expect(payload.longitude).toBe(0);
  });

  it("khi người dùng bấm 'Lấy GPS hiện tại', thanh GPS cập nhật tọa độ và cho phép 'Bỏ GPS'", async () => {
    vi.mocked(shopCustomerApi.createAddress).mockResolvedValue({} as any);

    render(<DiaChiPage />);

    await waitFor(() => {
      expect(screen.getByText("Thêm địa chỉ mới")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText("Thêm địa chỉ mới"));

    // Bấm lấy GPS hiện tại
    const gpsButton = screen.getByText("📍 Lấy GPS hiện tại");
    fireEvent.click(gpsButton);

    await waitFor(() => {
      expect(screen.getByText(/10.7616/)).toBeInTheDocument();
      expect(screen.getByText("Bỏ GPS")).toBeInTheDocument();
    });

    // Bây giờ bấm 'Bỏ GPS'
    fireEvent.click(screen.getByText("Bỏ GPS"));

    // Thanh GPS phải quay về trạng thái chưa gắn
    expect(screen.getByText(/Chưa gắn tọa độ/i)).toBeInTheDocument();
    expect(screen.queryByText("Bỏ GPS")).not.toBeInTheDocument();
    expect(screen.queryByText(/10.7616/)).not.toBeInTheDocument();
  });
});
