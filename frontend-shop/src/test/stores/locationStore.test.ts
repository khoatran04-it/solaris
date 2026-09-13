import { describe, it, expect } from "vitest";
import {
  normalizeDistrictName,
  mapLocationToWarehouse,
} from "@/stores/locationStore";
import { ShopWarehouse } from "@/types/warehouse";

const mockWarehouses: ShopWarehouse[] = [
  {
    id: 2,
    code: "WH-RETAIL01",
    name: "Kho Bán Lẻ Q7",
    warehouseType: "Kho Bán Lẻ",
    province: "Hồ Chí Minh",
    district: "Quận 7",
    ward: "Phường Tân Hưng",
    streetAddress: "123 Nguyễn Thị Thập",
    fullAddress: "123 Nguyễn Thị Thập, Phường Tân Hưng, Quận 7, Hồ Chí Minh",
    latitude: 10.732,
    longitude: 106.705,
    isActive: true,
  },
  {
    id: 3,
    code: "WH-RETAIL-02",
    name: "Kho Bán Lẻ Thủ Đức",
    warehouseType: "Kho Bán Lẻ",
    province: "Hồ Chí Minh",
    district: "Thành Phố Thủ Đức",
    ward: "Phường Linh Trung",
    streetAddress: "Khu Phố 6 Xa Lộ Hà Nội",
    fullAddress: "Khu Phố 6 Xa Lộ Hà Nội, Phường Linh Trung, Thành Phố Thủ Đức, Hồ Chí Minh",
    latitude: 10.878,
    longitude: 106.7766,
    isActive: true,
  },
  {
    id: 4,
    code: "WH-RETAIL-03",
    name: "Kho Bán Lẻ Quận 4",
    warehouseType: "Kho Bán Lẻ",
    province: "Hồ Chí Minh",
    district: "Quận 4",
    ward: "Phường 18",
    streetAddress: "300A Nguyễn Tất Thành",
    fullAddress: "300A Nguyễn Tất Thành, Phường 18, Quận 4, Hồ Chí Minh",
    latitude: 10.7584,
    longitude: 106.7118,
    isActive: true,
  },
];

describe("Location Store - Dynamic Warehouse Routing & Normalization", () => {
  describe("normalizeDistrictName", () => {
    it("TC01: Chuẩn hóa các biến thể của Thủ Đức về 'thu duc'", () => {
      expect(normalizeDistrictName("Thành Phố Thủ Đức")).toBe("thu duc");
      expect(normalizeDistrictName("Thủ Đức")).toBe("thu duc");
      expect(normalizeDistrictName("TP. Thủ Đức")).toBe("thu duc");
      expect(normalizeDistrictName("TP Thủ Đức")).toBe("thu duc");
      expect(normalizeDistrictName("Quận Thủ Đức")).toBe("thu duc");
      expect(normalizeDistrictName("Q. Thủ Đức")).toBe("thu duc");
    });

    it("TC02: Chuẩn hóa các quận đánh số", () => {
      expect(normalizeDistrictName("Quận 7")).toBe("7");
      expect(normalizeDistrictName("Q.7")).toBe("7");
      expect(normalizeDistrictName("Q7")).toBe("7");
      expect(normalizeDistrictName("Quận 4")).toBe("4");
      expect(normalizeDistrictName("Quận 1")).toBe("1");
      expect(normalizeDistrictName("Quận 10")).toBe("10");
      expect(normalizeDistrictName("Quận 12")).toBe("12");
    });

    it("TC03: Chuẩn hóa các huyện và quận ngoại thành", () => {
      expect(normalizeDistrictName("Quận Bình Thạnh")).toBe("binh thanh");
      expect(normalizeDistrictName("Huyện Bình Chánh")).toBe("binh chanh");
      expect(normalizeDistrictName("Huyện Nhà Bè")).toBe("nha be");
      expect(normalizeDistrictName("Huyện Cần Giờ")).toBe("can gio");
    });

    it("TC04: Xử lý chuỗi rỗng / null an toàn", () => {
      expect(normalizeDistrictName("")).toBe("");
      expect(normalizeDistrictName(null)).toBe("");
      expect(normalizeDistrictName(undefined)).toBe("");
    });
  });

  describe("mapLocationToWarehouse", () => {
    it("TC05: Khớp chính xác Kho Bán Lẻ Thủ Đức khi khách có địa chỉ Thành Phố Thủ Đức", () => {
      const wh = mapLocationToWarehouse(
        "Thành Phố Thủ Đức",
        "Hồ Chí Minh",
        undefined,
        undefined,
        mockWarehouses,
      );
      expect(wh.id).toBe(3);
      expect(wh.code).toBe("WH-RETAIL-02");
      expect(wh.name).toBe("Kho Bán Lẻ Thủ Đức");
    });

    it("TC06: Khớp Kho Bán Lẻ Thủ Đức khi khách nhập 'Thủ Đức' hoặc 'TP Thủ Đức'", () => {
      const wh1 = mapLocationToWarehouse("Thủ Đức", "TP.HCM", 0, 0, mockWarehouses);
      expect(wh1.id).toBe(3);

      const wh2 = mapLocationToWarehouse("TP. Thủ Đức", "TP.HCM", 0, 0, mockWarehouses);
      expect(wh2.id).toBe(3);
    });

    it("TC07: Khớp chính xác Kho Bán Lẻ Q7 khi khách ở Quận 7 hoặc Q7", () => {
      const wh1 = mapLocationToWarehouse("Quận 7", "Hồ Chí Minh", 0, 0, mockWarehouses);
      expect(wh1.id).toBe(2);

      const wh2 = mapLocationToWarehouse("Q7", "Hồ Chí Minh", 0, 0, mockWarehouses);
      expect(wh2.id).toBe(2);
    });

    it("TC08: Bỏ qua tọa độ giả Quận 1 (10.7769, 106.7009) nếu địa chỉ ở Thủ Đức", () => {
      const wh = mapLocationToWarehouse(
        "Thành Phố Thủ Đức",
        "TP. Hồ Chí Minh",
        10.7769,
        106.7009,
        mockWarehouses,
      );
      expect(wh.id).toBe(3);
      expect(wh.code).toBe("WH-RETAIL-02");
    });

    it("TC09: Sử dụng GPS thực tế của người dùng khi có tọa độ hợp lệ", () => {
      const wh = mapLocationToWarehouse(
        undefined,
        undefined,
        10.875,
        106.772,
        mockWarehouses,
      );
      expect(wh.id).toBe(3);
      expect(wh.code).toBe("WH-RETAIL-02");
    });

    it("TC10: Fallback an toàn về kho đầu tiên khi không có dữ liệu địa chỉ", () => {
      const wh = mapLocationToWarehouse(undefined, undefined, 0, 0, mockWarehouses);
      expect(wh.id).toBe(2);
    });
  });
});
