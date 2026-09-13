import { create } from "zustand";
import { ShopWarehouse } from "@/types/warehouse";
import { ShopAddress } from "@/types/customer";
import shopWarehouseApi from "@/api/shopWarehouseApi";
import shopCustomerApi from "@/api/shopCustomerApi";

// Hàm tính khoảng cách Haversine giữa 2 tọa độ GPS (km)
function calculateDistanceKm(
  lat1: number,
  lon1: number,
  lat2: number,
  lon2: number,
): number {
  const R = 6371; // Bán kính Trái Đất (km)
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLon = ((lon2 - lon1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos((lat1 * Math.PI) / 180) *
      Math.cos((lat2 * Math.PI) / 180) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return R * c;
}

interface LocationState {
  // Thông tin địa chỉ nhận hàng hiển thị cho khách
  deliveryAddress: string;
  deliveryDistrict: string;

  // Kho bán lẻ phụ trách phục vụ (Nội bộ hệ thống - Store Locking)
  selectedWarehouse: ShopWarehouse | null;
  warehouses: ShopWarehouse[];

  // Sổ địa chỉ của khách hàng (nếu đã đăng nhập)
  savedAddresses: ShopAddress[];

  isModalOpen: boolean;
  isDetectingGps: boolean;
  detectionMessage: string | null;
  isInitialized: boolean;

  initLocation: () => Promise<void>;
  fetchWarehouses: () => Promise<ShopWarehouse[]>;
  loadSavedAddresses: () => Promise<ShopAddress[]>;
  detectGps: (openModalOnFail?: boolean) => Promise<void>;
  selectSavedAddress: (addr: ShopAddress) => void;
  setDefaultAddress: (addressId: number) => Promise<void>;
  setCustomDeliveryAddress: (
    street: string,
    ward: string,
    district: string,
    province: string,
  ) => { success: boolean; message?: string };
  openModal: () => void;
  closeModal: () => void;
}

export const useLocationStore = create<LocationState>((set, get) => ({
  deliveryAddress: "Quận 4, TP. Hồ Chí Minh",
  deliveryDistrict: "Quận 4",
  selectedWarehouse: null,
  warehouses: [],
  savedAddresses: [],
  isModalOpen: false,
  isDetectingGps: false,
  detectionMessage: null,
  isInitialized: false,

  fetchWarehouses: async () => {
    try {
      const list = await shopWarehouseApi.getAll();
      set({ warehouses: list });
      return list;
    } catch {
      return [];
    }
  },

  loadSavedAddresses: async () => {
    if (typeof window === "undefined") return [];
    const token = localStorage.getItem("solaris_shop_token");
    if (!token) return [];
    try {
      const addrs = await shopCustomerApi.getAddresses();
      set({ savedAddresses: addrs });
      return addrs;
    } catch {
      return [];
    }
  },

  initLocation: async () => {
    if (get().isInitialized && get().selectedWarehouse) return;

    // 1. Tải danh sách kho bán lẻ từ server
    let whList = get().warehouses;
    if (!whList.length) {
      whList = await get().fetchWarehouses();
    }

    if (!whList.length) {
      set({ isInitialized: true });
      return;
    }

    // 2. Tải sổ địa chỉ của khách nếu đã đăng nhập -> Ưu tiên địa chỉ mặc định
    const addrs = await get().loadSavedAddresses();
    if (addrs.length > 0) {
      const defaultAddr = addrs.find((a) => a.isDefault) || addrs[0];
      if (defaultAddr) {
        const matchedWh = mapLocationToWarehouse(
          defaultAddr.district,
          defaultAddr.province,
          defaultAddr.latitude,
          defaultAddr.longitude,
          whList,
        );
        set({
          deliveryAddress:
            defaultAddr.fullAddress ||
            `${defaultAddr.district}, ${defaultAddr.province}`,
          deliveryDistrict: defaultAddr.district,
          selectedWarehouse: matchedWh,
          isInitialized: true,
        });
        if (typeof window !== "undefined") {
          localStorage.setItem(
            "solaris_delivery_address",
            defaultAddr.fullAddress,
          );
          localStorage.setItem(
            "solaris_delivery_district",
            defaultAddr.district,
          );
          localStorage.setItem(
            "solaris_selected_warehouse",
            JSON.stringify(matchedWh),
          );
        }
        return;
      }
    }

    // 3. Kiểm tra localStorage từ phiên truy cập trước
    if (typeof window !== "undefined") {
      const savedAddr = localStorage.getItem("solaris_delivery_address");
      const savedDistrict = localStorage.getItem("solaris_delivery_district");
      const savedWhRaw = localStorage.getItem("solaris_selected_warehouse");

      if (savedDistrict) {
        const remappedWh = mapLocationToWarehouse(
          savedDistrict,
          undefined,
          undefined,
          undefined,
          whList,
        );
        set({
          deliveryAddress: savedAddr || `${savedDistrict}, TP.HCM`,
          deliveryDistrict: savedDistrict,
          selectedWarehouse: remappedWh,
          isInitialized: true,
        });
        localStorage.setItem(
          "solaris_selected_warehouse",
          JSON.stringify(remappedWh),
        );
        return;
      }

      if (savedWhRaw) {
        try {
          const savedWh = JSON.parse(savedWhRaw) as ShopWarehouse;
          const stillExists = whList.find((w) => w.id === savedWh.id);
          if (stillExists) {
            set({
              deliveryAddress:
                savedAddr || `${stillExists.district || "TP.HCM"}`,
              deliveryDistrict:
                savedDistrict || stillExists.district || "",
              selectedWarehouse: stillExists,
              isInitialized: true,
            });
            return;
          }
        } catch {}
      }

      // 4. Nếu chưa có gì: Thử tự động định vị GPS (im lặng)
      await get().detectGps(true);
      set({ isInitialized: true });
    }
  },

  detectGps: async (openModalOnFail = true) => {
    let whList = get().warehouses;
    if (!whList.length) {
      whList = await get().fetchWarehouses();
    }
    if (!whList.length) return;

    const defaultWh =
      whList.find(
        (w) => w.district?.includes("4") || w.name.includes("Quận 4"),
      ) || whList[0];

    if (typeof window === "undefined" || !navigator.geolocation) {
      set({
        selectedWarehouse: get().selectedWarehouse || defaultWh,
        deliveryAddress: get().deliveryAddress || "Quận 4, TP. Hồ Chí Minh",
        deliveryDistrict: get().deliveryDistrict || "Quận 4",
        detectionMessage: "Trình duyệt không hỗ trợ định vị GPS.",
        isModalOpen: openModalOnFail,
      });
      return;
    }

    set({
      isDetectingGps: true,
      detectionMessage: "Đang xác định vị trí của bạn...",
    });

    try {
      const position = await new Promise<GeolocationPosition>(
        (resolve, reject) => {
          navigator.geolocation.getCurrentPosition(resolve, reject, {
            timeout: 8000,
            maximumAge: 60000,
            enableHighAccuracy: true,
          });
        },
      );

      const userLat = position.coords.latitude;
      const userLon = position.coords.longitude;
      const nearest = findNearestWarehouse(userLat, userLon, whList);

      // Kiểm tra bán kính phục vụ (ví dụ trong vòng 40km từ chi nhánh gần nhất)
      const distance = calculateDistanceKm(
        userLat,
        userLon,
        nearest.latitude || 0,
        nearest.longitude || 0,
      );
      if (distance > 40) {
        set({
          selectedWarehouse: defaultWh,
          isDetectingGps: false,
          detectionMessage: `Vị trí hiện tại cách chi nhánh gần nhất ${Math.round(distance)}km, vượt quá bán kính giao nhanh 2h. Vui lòng nhập địa chỉ nhận hàng trong TP.HCM.`,
          isModalOpen: true,
        });
        return;
      }

      const displayDistrict = nearest.district || "Hồ Chí Minh";
      const displayAddress = `Vị trí hiện tại (${displayDistrict}, TP.HCM)`;

      set({
        deliveryAddress: displayAddress,
        deliveryDistrict: displayDistrict,
        selectedWarehouse: nearest,
        isDetectingGps: false,
        detectionMessage: null,
        isModalOpen: false,
      });

      if (typeof window !== "undefined") {
        localStorage.setItem("solaris_delivery_address", displayAddress);
        localStorage.setItem("solaris_delivery_district", displayDistrict);
        localStorage.setItem(
          "solaris_selected_warehouse",
          JSON.stringify(nearest),
        );
      }
    } catch {
      const fallbackWh = get().selectedWarehouse || defaultWh;
      const fallbackDistrict = fallbackWh.district || "Quận 4";
      const fallbackAddress = `${fallbackDistrict}, TP. Hồ Chí Minh`;

      set({
        deliveryAddress: fallbackAddress,
        deliveryDistrict: fallbackDistrict,
        selectedWarehouse: fallbackWh,
        isDetectingGps: false,
        detectionMessage:
          "Không thể lấy GPS (Bạn đã từ chối hoặc thiết bị chưa bật định vị). Vui lòng điền địa chỉ nhận hàng bên dưới.",
        isModalOpen: openModalOnFail,
      });

      if (typeof window !== "undefined") {
        localStorage.setItem("solaris_delivery_address", fallbackAddress);
        localStorage.setItem("solaris_delivery_district", fallbackDistrict);
        localStorage.setItem(
          "solaris_selected_warehouse",
          JSON.stringify(fallbackWh),
        );
      }
    }
  },

  // Chọn từ sổ địa chỉ tài khoản
  selectSavedAddress: (addr: ShopAddress) => {
    const whList = get().warehouses;
    const matchedWh = mapLocationToWarehouse(
      addr.district,
      addr.province,
      addr.latitude,
      addr.longitude,
      whList,
    );

    set({
      deliveryAddress: addr.fullAddress,
      deliveryDistrict: addr.district,
      selectedWarehouse: matchedWh,
      isModalOpen: false,
      detectionMessage: null,
    });

    if (typeof window !== "undefined") {
      localStorage.setItem("solaris_delivery_address", addr.fullAddress);
      localStorage.setItem("solaris_delivery_district", addr.district);
      localStorage.setItem(
        "solaris_selected_warehouse",
        JSON.stringify(matchedWh),
      );
    }
  },

  // Đặt địa chỉ làm mặc định và đồng bộ kho ngay lập tức
  setDefaultAddress: async (addressId: number) => {
    try {
      await shopCustomerApi.setDefaultAddress(addressId);
      const addrs = await get().loadSavedAddresses();
      const target = addrs.find((a) => a.id === addressId);
      if (target) {
        get().selectSavedAddress(target);
      }
    } catch (error: any) {
      alert(error?.message || "Không thể thiết lập địa chỉ mặc định.");
    }
  },

  // Khách điền địa chỉ mới thủ công (form Tỉnh/Huyện/Xã/Đường)
  setCustomDeliveryAddress: (
    street: string,
    ward: string,
    district: string,
    province: string,
  ) => {
    const whList = get().warehouses;

    // Kiểm tra phạm vi phục vụ: Hiện tại Solaris có kho tại TP. Hồ Chí Minh
    const isSupportedProvince =
      province.toLowerCase().includes("hồ chí minh") ||
      province.toLowerCase().includes("hcm") ||
      province.toLowerCase().includes("sài gòn");

    if (!isSupportedProvince) {
      return {
        success: false,
        message: `Rất tiếc! Solaris hiện chưa có chi nhánh phục vụ tại ${province}. Chúng tôi hiện hỗ trợ giao hỏa tốc 2H tại TP. Hồ Chí Minh.`,
      };
    }

    const matchedWh = mapLocationToWarehouse(
      district,
      province,
      undefined,
      undefined,
      whList,
    );
    const parts = [street, ward, district, province].filter(Boolean);
    const fullAddress = parts.join(", ");

    set({
      deliveryAddress: fullAddress,
      deliveryDistrict: district,
      selectedWarehouse: matchedWh,
      isModalOpen: false,
      detectionMessage: null,
    });

    if (typeof window !== "undefined") {
      localStorage.setItem("solaris_delivery_address", fullAddress);
      localStorage.setItem("solaris_delivery_district", district);
      localStorage.setItem(
        "solaris_selected_warehouse",
        JSON.stringify(matchedWh),
      );
    }

    return { success: true };
  },

  openModal: () => {
    get().loadSavedAddresses();
    set({ isModalOpen: true });
  },

  closeModal: () => {
    set({ isModalOpen: false, detectionMessage: null });
  },
}));

// Chuẩn hóa tên quận/huyện để so khớp mềm dẻo (bỏ dấu tiếng Việt, bỏ tiền tố hành chính)
export function normalizeDistrictName(text?: string | null): string {
  if (!text) return "";
  let s = text.toLowerCase().trim();
  // Bỏ dấu tiếng Việt
  s = s
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "d");

  // Xóa các ký tự phân cách
  s = s.replace(/[\.,\-_/]/g, " ").trim();

  // Loại bỏ tiền tố hành chính phổ biến: thành phố, tp, thị xã, tx, quận, q, huyện, h, phường, p
  s = s.replace(/^(thanh\s+pho|tp|thi\s+xa|tx)\s+/g, "");
  s = s.replace(/^(quan|q|huyen|h)\s+/g, "");
  s = s.replace(/^q(\d+)$/g, "$1"); // q7 -> 7, q4 -> 4

  // Chuẩn hóa khoảng trắng
  s = s.replace(/\s+/g, " ").trim();
  return s;
}

// Hàm điều phối ngầm kho bán lẻ gần nhất dựa trên dữ liệu kho động từ Database (Store-Locking & Smart Routing)
export function mapLocationToWarehouse(
  district?: string,
  province?: string,
  lat?: number,
  lon?: number,
  warehouses: ShopWarehouse[] = [],
): ShopWarehouse {
  if (!warehouses.length) {
    return {
      id: 0,
      name: "Kho Mặc Định",
      code: "DEFAULT",
      warehouseType: "Kho Bán Lẻ",
      district: district || "",
      fullAddress: "",
      latitude: 0,
      longitude: 0,
      isActive: true,
    };
  }

  // 1. So khớp động theo Quận/Huyện từ danh sách kho của DB
  if (district) {
    const targetNorm = normalizeDistrictName(district);

    if (targetNorm) {
      // 1.1 Khớp chính xác tên quận đã chuẩn hóa giữa địa chỉ khách và địa chỉ kho trong DB
      const exactMatch = warehouses.find(
        (w) => w.district && normalizeDistrictName(w.district) === targetNorm,
      );
      if (exactMatch) return exactMatch;

      // 1.2 Khớp với tên kho (ví dụ kho tên "Kho Bán Lẻ Thủ Đức" hoặc "Kho Bán Lẻ Q7")
      const nameMatch = warehouses.find(
        (w) => w.name && normalizeDistrictName(w.name).includes(targetNorm),
      );
      if (nameMatch) return nameMatch;

      // 1.3 Khớp bao hàm hai chiều (chỉ áp dụng cho quận có tên chữ, không áp dụng cho số)
      const isNumeric = /^\d+$/.test(targetNorm);
      if (!isNumeric && targetNorm.length >= 3) {
        const subMatch = warehouses.find((w) => {
          if (!w.district) return false;
          const wNorm = normalizeDistrictName(w.district);
          return wNorm.includes(targetNorm) || targetNorm.includes(wNorm);
        });
        if (subMatch) return subMatch;
      }
    }
  }

  // 2. Nếu khách có tọa độ GPS thực tế từ thiết bị (khác 0 và không phải tọa độ giả Q1 10.7769, 106.7009)
  const isBogusQ1Coords =
    lat != null &&
    lon != null &&
    Math.abs(lat - 10.7769) < 0.001 &&
    Math.abs(lon - 106.7009) < 0.001 &&
    normalizeDistrictName(district) !== "1";

  if (
    lat != null &&
    lon != null &&
    lat !== 0 &&
    lon !== 0 &&
    !isBogusQ1Coords
  ) {
    return findNearestWarehouse(lat, lon, warehouses);
  }

  // 3. Khớp theo Tỉnh / Thành phố
  if (province) {
    const provNorm = normalizeDistrictName(province);
    if (provNorm) {
      const provMatch = warehouses.find(
        (w) =>
          w.province &&
          (normalizeDistrictName(w.province).includes(provNorm) ||
            provNorm.includes(normalizeDistrictName(w.province))),
      );
      if (provMatch) return provMatch;
    }
  }

  // 4. Fallback: Kho bán lẻ đang hoạt động đầu tiên
  return warehouses.find((w) => w.isActive) || warehouses[0];
}

function findNearestWarehouse(
  lat: number,
  lon: number,
  warehouses: ShopWarehouse[],
): ShopWarehouse {
  let nearest = warehouses[0];
  let minDistance = Infinity;

  for (const w of warehouses) {
    if (w.latitude != null && w.longitude != null) {
      const d = calculateDistanceKm(lat, lon, w.latitude, w.longitude);
      if (d < minDistance) {
        minDistance = d;
        nearest = w;
      }
    }
  }

  return nearest;
}
