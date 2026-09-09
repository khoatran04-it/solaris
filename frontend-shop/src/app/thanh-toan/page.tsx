"use client";

import React, { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Truck,
  CheckCircle2,
  ShoppingBag,
  ArrowRight,
  AlertCircle,
  Sparkles,
  Gift,
  Banknote,
  QrCode,
  ShieldCheck,
  Lock,
  ThermometerSnowflake,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import shopOrderApi from "@/api/shopOrderApi";
import shopCustomerApi from "@/api/shopCustomerApi";
import shopShippingApi from "@/api/shopShippingApi";
import shopPaymentApi from "@/api/shopPaymentApi";
import { formatVND } from "@/lib/utils";
import { ShopAddress } from "@/types/customer";
import { ShopOrder, ShopCheckoutPayload } from "@/types/order";
import { GhnProvince, GhnDistrict, GhnWard } from "@/types/shipping";

export default function ThanhToanPage() {
  const router = useRouter();
  const { isAuthenticated, initAuth } = useAuthStore();
  const { cart, fetchCart, clearCart } = useCartStore();

  const [addresses, setAddresses] = useState<ShopAddress[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<number | null>(
    null,
  );

  // GHN Location Data
  const [provinces, setProvinces] = useState<GhnProvince[]>([]);
  const [districts, setDistricts] = useState<GhnDistrict[]>([]);
  const [wards, setWards] = useState<GhnWard[]>([]);

  // Address selection state
  const [useNewAddress, setUseNewAddress] = useState(false);
  const [receiverName, setReceiverName] = useState("");
  const [receiverPhone, setReceiverPhone] = useState("");
  const [selectedProvinceId, setSelectedProvinceId] = useState<number>(201); // Mặc định TP.HCM
  const [selectedDistrictId, setSelectedDistrictId] = useState<number | null>(
    null,
  );
  const [selectedWardCode, setSelectedWardCode] = useState<string>("");
  const [streetAddress, setStreetAddress] = useState("");
  const [note, setNote] = useState("");

  // Shipping Fee & Freeship State
  const [shippingFee, setShippingFee] = useState<number>(25000);
  const [isFreeShipping, setIsFreeShipping] = useState<boolean>(false);
  const [isCalculatingFee, setIsCalculatingFee] = useState<boolean>(false);
  const freeShippingThreshold = 300000;

  // Payment method: 1 = COD, 2 = BankTransfer, 3 = VNPay
  const [paymentMethod, setPaymentMethod] = useState<number>(3); // Mặc định VNPay Sandbox
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    initAuth();
    fetchCart();
  }, [initAuth, fetchCart]);

  // Load Provinces
  useEffect(() => {
    shopShippingApi
      .getProvinces()
      .then((data) => {
        setProvinces(data);
        if (data.length > 0) {
          const hcm =
            data.find((p) => p.provinceName.includes("Hồ Chí Minh")) || data[0];
          setSelectedProvinceId(hcm.provinceID);
        }
      })
      .catch(() => {});
  }, []);

  // Load Districts when Province changes
  useEffect(() => {
    if (selectedProvinceId) {
      shopShippingApi
        .getDistricts(selectedProvinceId)
        .then((data) => {
          setDistricts(data);
          if (data.length > 0) {
            setSelectedDistrictId(data[0].districtID);
          } else {
            setSelectedDistrictId(null);
          }
        })
        .catch(() => {});
    }
  }, [selectedProvinceId]);

  // Load Wards when District changes
  useEffect(() => {
    if (selectedDistrictId) {
      shopShippingApi
        .getWards(selectedDistrictId)
        .then((data) => {
          setWards(data);
          if (data.length > 0) {
            setSelectedWardCode(data[0].wardCode);
          } else {
            setSelectedWardCode("");
          }
        })
        .catch(() => {});
    }
  }, [selectedDistrictId]);

  // Calculate Realtime Shipping Fee via GHN
  const calculateFee = useCallback(async () => {
    if (!selectedDistrictId || !selectedWardCode || !cart) return;

    setIsCalculatingFee(true);
    try {
      const res = await shopShippingApi.calculateFee({
        toDistrictId: selectedDistrictId,
        toWardCode: selectedWardCode,
        subTotal: cart.subTotal - cart.totalDiscount,
        weightGram: cart.items.length * 500,
      });

      setShippingFee(res.totalFee);
      setIsFreeShipping(res.isFreeShipping);
    } catch {
      // Giữ giá trị dự phòng nếu có lỗi mạng
      const isEligibleFree =
        cart.subTotal - cart.totalDiscount >= freeShippingThreshold;
      setShippingFee(isEligibleFree ? 0 : 25000);
      setIsFreeShipping(isEligibleFree);
    } finally {
      setIsCalculatingFee(false);
    }
  }, [selectedDistrictId, selectedWardCode, cart]);

  useEffect(() => {
    calculateFee();
  }, [calculateFee]);

  // Load saved customer addresses if authenticated
  useEffect(() => {
    if (isAuthenticated) {
      shopCustomerApi
        .getAddresses()
        .then((addrs: ShopAddress[]) => {
          setAddresses(addrs);
          const defaultAddr =
            addrs.find((a: ShopAddress) => a.isDefault) || addrs[0];
          if (defaultAddr) {
            setSelectedAddressId(defaultAddr.id);
          } else {
            setUseNewAddress(true);
          }
        })
        .catch(() => {
          setUseNewAddress(true);
        });
    }
  }, [isAuthenticated]);

  // Khi đổi địa chỉ đã lưu -> Đồng bộ sang GHN District & Ward để tính phí ship chuẩn xác
  useEffect(() => {
    if (
      !useNewAddress &&
      selectedAddressId &&
      addresses.length > 0 &&
      provinces.length > 0
    ) {
      const addr = addresses.find((a) => a.id === selectedAddressId);
      if (addr) {
        const prov = provinces.find(
          (p) =>
            p.provinceName
              .toLowerCase()
              .includes(addr.province.toLowerCase()) ||
            addr.province.toLowerCase().includes(p.provinceName.toLowerCase()),
        );
        if (prov) {
          setSelectedProvinceId(prov.provinceID);
          shopShippingApi.getDistricts(prov.provinceID).then((distList) => {
            setDistricts(distList);
            const dist =
              distList.find(
                (d) =>
                  d.districtName
                    .toLowerCase()
                    .includes(addr.district.toLowerCase()) ||
                  addr.district
                    .toLowerCase()
                    .includes(d.districtName.toLowerCase()),
              ) || distList[0];
            if (dist) {
              setSelectedDistrictId(dist.districtID);
              shopShippingApi.getWards(dist.districtID).then((wardList) => {
                setWards(wardList);
                const ward =
                  wardList.find(
                    (w) =>
                      w.wardName
                        .toLowerCase()
                        .includes(addr.ward.toLowerCase()) ||
                      addr.ward
                        .toLowerCase()
                        .includes(w.wardName.toLowerCase()),
                  ) || wardList[0];
                if (ward) {
                  setSelectedWardCode(ward.wardCode);
                }
              });
            }
          });
        }
      }
    }
  }, [selectedAddressId, useNewAddress, addresses, provinces]);

  const handleCheckout = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage("");

    if (!cart || cart.items.length === 0) {
      setErrorMessage("Giỏ hàng của bạn đang trống.");
      return;
    }

    if (!useNewAddress && !selectedAddressId) {
      setErrorMessage("Vui lòng chọn hoặc thêm địa chỉ nhận hàng.");
      return;
    }

    if (useNewAddress) {
      if (
        !receiverName.trim() ||
        !receiverPhone.trim() ||
        !streetAddress.trim()
      ) {
        setErrorMessage(
          "Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ nhận hàng.",
        );
        return;
      }
    }

    setIsSubmitting(true);

    try {
      const currentProvince =
        provinces.find((p) => p.provinceID === selectedProvinceId)
          ?.provinceName || "TP. Hồ Chí Minh";
      const currentDistrict =
        districts.find((d) => d.districtID === selectedDistrictId)
          ?.districtName || "";
      const currentWard =
        wards.find((w) => w.wardCode === selectedWardCode)?.wardName || "";

      const payload: ShopCheckoutPayload = {
        customerAddressId: useNewAddress
          ? undefined
          : (selectedAddressId ?? undefined),
        receiverName: useNewAddress ? receiverName.trim() : undefined,
        receiverPhone: useNewAddress ? receiverPhone.trim() : undefined,
        province: useNewAddress ? currentProvince : undefined,
        district: useNewAddress ? currentDistrict : undefined,
        ward: useNewAddress ? currentWard : undefined,
        streetAddress: useNewAddress ? streetAddress.trim() : undefined,
        ghnDistrictId: selectedDistrictId ?? undefined,
        ghnWardCode: selectedWardCode || undefined,
        shippingFee: shippingFee,
        latitude: 10.7769,
        longitude: 106.7009,
        paymentMethod: paymentMethod,
        note: note.trim(),
      };

      // 1. Tạo đơn hàng trên backend
      const order: ShopOrder = await shopOrderApi.checkout(payload);

      // 2. Xóa giỏ hàng trên client
      await clearCart();

      // 3. Nếu chọn VNPay -> Tạo URL thanh toán và chuyển hướng
      if (paymentMethod === 3) {
        const vnPayRes = await shopPaymentApi.createVnPayUrl({
          orderCode: order.orderCode,
          orderDescription: `Thanh toán đơn hàng Solaris ${order.orderCode}`,
        });

        if (vnPayRes?.paymentUrl) {
          window.location.href = vnPayRes.paymentUrl;
          return;
        }
      }

      // Redirect tới chi tiết đơn hàng (COD / BankTransfer)
      router.push(`/tai-khoan/don-hang/${order.orderCode}`);
    } catch (error: any) {
      const msg =
        error?.message ||
        error?.Message ||
        error?.details ||
        error?.Details ||
        error?.title ||
        (typeof error === "string"
          ? error
          : "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.");
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!cart || cart.items.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center space-y-4">
        <div className="w-16 h-16 rounded-2xl bg-slate-50 text-slate-400 flex items-center justify-center mx-auto">
          <ShoppingBag className="w-8 h-8" />
        </div>
        <p className="text-sm font-bold text-slate-700">
          Giỏ hàng của bạn đang trống.
        </p>
        <Link
          href="/san-pham"
          className="inline-block px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-2xl transition-all shadow-md"
        >
          Tiếp tục mua hàng
        </Link>
      </div>
    );
  }

  const netSubTotal = cart.subTotal - cart.totalDiscount;
  const finalTotal = Math.max(0, netSubTotal + shippingFee);
  const amountMissingForFreeShip = Math.max(
    0,
    freeShippingThreshold - netSubTotal,
  );
  const hasColdChain = Boolean(
    cart.hasColdChain || cart.items.some((i) => i.requiresColdChain),
  );

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* 1. Header */}
      <div>
        <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight flex items-center gap-3">
          <span>Xác Nhận & Thanh Toán</span>
          {hasColdChain && (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-cyan-100 text-cyan-800 border border-cyan-300">
              <ThermometerSnowflake className="w-3.5 h-3.5 text-cyan-600" />
              Đơn hàng chuỗi lạnh (Cold-Chain)
            </span>
          )}
        </h1>
        <p className="text-xs text-slate-500 mt-1 font-medium">
          {hasColdChain
            ? "Vận chuyển bảo quản lạnh chuyên dụng 0-4°C • Đội xe máy thùng lạnh Solaris (≤ 15km) • Cổng thanh toán bảo mật"
            : "Giao hàng nhanh toàn quốc qua GHN Express • Cổng thanh toán bảo mật VNPay Sandbox"}
        </p>
      </div>

      {/* Cold Chain Notification Banner */}
      {hasColdChain && (
        <div className="p-4 rounded-2xl bg-cyan-50/90 border border-cyan-200 flex items-start sm:items-center gap-3 text-xs text-cyan-950 shadow-2xs">
          <ThermometerSnowflake className="w-5 h-5 text-cyan-600 shrink-0 mt-0.5 sm:mt-0" />
          <div className="space-y-0.5">
            <p className="font-bold text-cyan-900">
              Chính sách giao hàng chuỗi lạnh chuyên dụng:
            </p>
            <p className="text-cyan-800 leading-relaxed">
              Đơn hàng của bạn có thực phẩm tươi sống (thịt, cá, hải sản, rau
              củ). Solaris tự vận chuyển bằng{" "}
              <strong>Đội xe máy thùng lạnh chuyên dụng</strong> trong bán kính
              tối đa <strong>15 km</strong> từ kho xuất hàng để đảm bảo chuẩn
              tươi sống.
            </p>
          </div>
        </div>
      )}

      {/* 2. Freeship Alert Banner */}
      <div
        className={`p-4 rounded-2xl border transition-all flex items-center justify-between gap-3 text-xs ${
          isFreeShipping || netSubTotal >= freeShippingThreshold
            ? "bg-emerald-50 border-emerald-200 text-emerald-900 font-medium"
            : "bg-amber-50 border-amber-200 text-amber-900 font-medium"
        }`}
      >
        <div className="flex items-center gap-2.5">
          {isFreeShipping || netSubTotal >= freeShippingThreshold ? (
            <>
              <Sparkles className="w-5 h-5 text-emerald-600 shrink-0" />
              <span>
                🎉 Chúc mừng! Đơn hàng của bạn đã đạt điều kiện{" "}
                <strong>MIỄN PHÍ GIAO HÀNG</strong> toàn quốc.
              </span>
            </>
          ) : (
            <>
              <Gift className="w-5 h-5 text-amber-600 shrink-0" />
              <span>
                Mua thêm <strong>{formatVND(amountMissingForFreeShip)}</strong>{" "}
                để được <strong>FREESHIP 100%</strong> (Đơn từ 300.000₫).
              </span>
            </>
          )}
        </div>
        {amountMissingForFreeShip > 0 && (
          <Link
            href="/san-pham"
            className="text-xs font-bold text-amber-800 underline shrink-0 hover:text-amber-900"
          >
            Mua thêm ngay
          </Link>
        )}
      </div>

      {errorMessage && (
        <div
          className={`p-4 rounded-2xl border flex items-start gap-3 text-xs font-semibold animate-in fade-in ${
            errorMessage.toLowerCase().includes("bán kính") ||
            errorMessage.toLowerCase().includes("chuỗi lạnh")
              ? "bg-amber-50 border-amber-300 text-amber-900"
              : "bg-rose-50 border-rose-200 text-rose-700"
          }`}
        >
          {errorMessage.toLowerCase().includes("bán kính") ||
          errorMessage.toLowerCase().includes("chuỗi lạnh") ? (
            <ThermometerSnowflake className="w-5 h-5 shrink-0 text-amber-600 mt-0.5" />
          ) : (
            <AlertCircle className="w-5 h-5 shrink-0 text-rose-600 mt-0.5" />
          )}
          <div className="flex-1 space-y-1">
            <p>{errorMessage}</p>
            {(errorMessage.toLowerCase().includes("bán kính") ||
              errorMessage.toLowerCase().includes("chuỗi lạnh")) && (
              <div className="pt-1 flex items-center gap-3">
                <Link
                  href="/gio-hang"
                  className="inline-flex items-center gap-1 text-xs font-bold text-amber-800 underline hover:text-amber-950"
                >
                  Quay lại giỏ hàng để điều chỉnh sản phẩm &rarr;
                </Link>
              </div>
            )}
          </div>
        </div>
      )}

      <form
        onSubmit={handleCheckout}
        className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start"
      >
        {/* Left (7 Cols): Delivery Info & Payment */}
        <div className="lg:col-span-7 space-y-6">
          {/* 1. Sổ Địa Chỉ Giao Hàng & GHN Selector */}
          <div className="bg-white rounded-3xl border border-slate-200/80 p-6 sm:p-7 shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] space-y-5">
            <div className="flex items-center justify-between pb-3.5 border-b border-slate-100">
              <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider flex items-center gap-2">
                <span className="w-1.5 h-4 bg-emerald-600 rounded-full"></span>
                {hasColdChain
                  ? "1. Địa Chỉ Nhận Hàng (Đội Xe Lạnh ❄️ Solaris Cold-Express)"
                  : "1. Địa Chỉ Nhận Hàng (GHN Logistics)"}
              </h2>

              {addresses.length > 0 && (
                <button
                  type="button"
                  onClick={() => setUseNewAddress(!useNewAddress)}
                  className="text-xs font-bold text-emerald-700 hover:text-emerald-800 cursor-pointer"
                >
                  {useNewAddress ? "Chọn địa chỉ có sẵn" : "+ Thêm địa chỉ mới"}
                </button>
              )}
            </div>

            {/* List Saved Addresses */}
            {!useNewAddress && addresses.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {addresses.map((addr) => (
                  <div
                    key={addr.id}
                    onClick={() => setSelectedAddressId(addr.id)}
                    className={`p-4 rounded-2xl border cursor-pointer transition-all ${
                      selectedAddressId === addr.id
                        ? "border-emerald-600 bg-emerald-50/50 shadow-2xs ring-2 ring-emerald-500/20"
                        : "border-slate-200 hover:border-slate-300 bg-slate-50/40"
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="space-y-1">
                        <p className="font-bold text-xs text-slate-900 flex items-center gap-1.5">
                          {addr.receiverName} • {addr.phone}
                          {addr.isDefault && (
                            <span className="px-1.5 py-0.2 bg-emerald-100 text-emerald-800 text-[9px] font-bold rounded">
                              Mặc định
                            </span>
                          )}
                        </p>
                        <p className="text-xs text-slate-600 leading-relaxed">
                          {addr.streetAddress}, {addr.ward}, {addr.district},{" "}
                          {addr.province}
                        </p>
                      </div>
                      {selectedAddressId === addr.id && (
                        <CheckCircle2 className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
                      )}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              /* New Address Form with GHN 3-level selector */
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Họ tên người nhận *
                  </label>
                  <input
                    type="text"
                    placeholder="Nguyễn Văn A"
                    value={receiverName}
                    onChange={(e) => setReceiverName(e.target.value)}
                    className="w-full h-11 px-3.5 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                    required={useNewAddress}
                  />
                </div>

                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Số điện thoại nhận hàng *
                  </label>
                  <input
                    type="tel"
                    placeholder="0912345678"
                    value={receiverPhone}
                    onChange={(e) => setReceiverPhone(e.target.value)}
                    className="w-full h-11 px-3.5 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                    required={useNewAddress}
                  />
                </div>

                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Tỉnh / Thành phố (GHN) *
                  </label>
                  <select
                    value={selectedProvinceId}
                    onChange={(e) =>
                      setSelectedProvinceId(Number(e.target.value))
                    }
                    className="w-full h-11 px-3 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                  >
                    {provinces.map((p) => (
                      <option key={p.provinceID} value={p.provinceID}>
                        {p.provinceName}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Quận / Huyện (GHN) *
                  </label>
                  <select
                    value={selectedDistrictId || ""}
                    onChange={(e) =>
                      setSelectedDistrictId(Number(e.target.value))
                    }
                    className="w-full h-11 px-3 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                  >
                    {districts.map((d) => (
                      <option key={d.districtID} value={d.districtID}>
                        {d.districtName}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Phường / Xã (GHN) *
                  </label>
                  <select
                    value={selectedWardCode}
                    onChange={(e) => setSelectedWardCode(e.target.value)}
                    className="w-full h-11 px-3 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                  >
                    {wards.map((w) => (
                      <option key={w.wardCode} value={w.wardCode}>
                        {w.wardName}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                    Số nhà, tên đường *
                  </label>
                  <input
                    type="text"
                    placeholder="123 Đường Lê Lợi"
                    value={streetAddress}
                    onChange={(e) => setStreetAddress(e.target.value)}
                    className="w-full h-11 px-3.5 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
                    required={useNewAddress}
                  />
                </div>
              </div>
            )}

            <div className="space-y-1 pt-2">
              <label className="font-bold text-xs text-slate-700 uppercase tracking-wide block">
                Ghi chú giao hàng (Tùy chọn)
              </label>
              <input
                type="text"
                placeholder="Ví dụ: Giao vào giờ hành chính, gọi trước khi đến..."
                value={note}
                onChange={(e) => setNote(e.target.value)}
                className="w-full h-11 px-3.5 bg-slate-50/70 border border-slate-200 rounded-xl text-xs font-medium focus:outline-none focus:border-emerald-600 focus:bg-white focus:ring-4 focus:ring-emerald-500/15"
              />
            </div>
          </div>

          {/* 2. Phương Thức Thanh Toán */}
          <div className="bg-white rounded-3xl border border-slate-200/80 p-6 sm:p-7 shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] space-y-4">
            <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider flex items-center gap-2 pb-3.5 border-b border-slate-100">
              <span className="w-1.5 h-4 bg-emerald-600 rounded-full"></span>
              2. Phương Thức Thanh Toán
            </h2>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {/* VNPay */}
              <div
                onClick={() => setPaymentMethod(3)}
                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                  paymentMethod === 3
                    ? "border-emerald-600 bg-emerald-50/60 ring-2 ring-emerald-500/20 shadow-2xs"
                    : "border-slate-200 hover:border-slate-300 bg-white"
                }`}
              >
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="w-8 h-8 rounded-xl bg-blue-600 text-white flex items-center justify-center font-black text-xs shadow-xs">
                      VNP
                    </div>
                    {paymentMethod === 3 && (
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    )}
                  </div>
                  <div>
                    <h4 className="font-bold text-xs text-slate-900">
                      Cổng VNPay Sandbox
                    </h4>
                    <p className="text-[10px] text-slate-500">
                      Quét VNPAY-QR, Thẻ ATM/Visa
                    </p>
                  </div>
                </div>
              </div>

              {/* COD */}
              <div
                onClick={() => setPaymentMethod(1)}
                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                  paymentMethod === 1
                    ? "border-emerald-600 bg-emerald-50/60 ring-2 ring-emerald-500/20 shadow-2xs"
                    : "border-slate-200 hover:border-slate-300 bg-white"
                }`}
              >
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="w-8 h-8 rounded-xl bg-emerald-100 text-emerald-800 flex items-center justify-center font-bold text-xs">
                      <Banknote className="w-4 h-4 text-emerald-700" />
                    </div>
                    {paymentMethod === 1 && (
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    )}
                  </div>
                  <div>
                    <h4 className="font-bold text-xs text-slate-900">
                      Khi nhận hàng (COD)
                    </h4>
                    <p className="text-[10px] text-slate-500">
                      Kiểm tra trước khi trả tiền
                    </p>
                  </div>
                </div>
              </div>

              {/* BankTransfer */}
              <div
                onClick={() => setPaymentMethod(2)}
                className={`p-4 rounded-2xl border cursor-pointer transition-all flex flex-col justify-between ${
                  paymentMethod === 2
                    ? "border-emerald-600 bg-emerald-50/60 ring-2 ring-emerald-500/20 shadow-2xs"
                    : "border-slate-200 hover:border-slate-300 bg-white"
                }`}
              >
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="w-8 h-8 rounded-xl bg-purple-100 text-purple-800 flex items-center justify-center font-bold text-xs">
                      <QrCode className="w-4 h-4 text-purple-700" />
                    </div>
                    {paymentMethod === 2 && (
                      <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                    )}
                  </div>
                  <div>
                    <h4 className="font-bold text-xs text-slate-900">
                      Chuyển khoản VietQR
                    </h4>
                    <p className="text-[10px] text-slate-500">
                      Quét mã QR Mobile Banking
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {paymentMethod === 3 && (
              <div className="p-3.5 bg-blue-50/80 rounded-2xl border border-blue-100 text-xs text-blue-900 flex items-center gap-2">
                <Lock className="w-4 h-4 text-blue-600 shrink-0" />
                <span>
                  Sau khi bấm xác nhận, bạn sẽ được chuyển hướng an toàn sang
                  cổng thanh toán <strong>VNPay Sandbox</strong>.
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Right (5 Cols): Cart Summary & Submit CTA */}
        <div className="lg:col-span-5 space-y-6">
          <div className="bg-white rounded-3xl border border-slate-200/80 p-6 sm:p-7 shadow-[0_2px_20px_-4px_rgba(0,0,0,0.04)] space-y-5">
            <h3 className="font-extrabold text-sm text-slate-900 pb-3.5 border-b border-slate-100 flex items-center justify-between">
              <span>Đơn Hàng ({cart.items.length} món)</span>
              <Link
                href="/gio-hang"
                className="text-xs font-bold text-emerald-700 hover:text-emerald-800"
              >
                Sửa
              </Link>
            </h3>

            {/* List Items mini */}
            <div className="space-y-3.5 max-h-64 overflow-y-auto pr-1 divide-y divide-slate-100">
              {cart.items.map((item) => (
                <div
                  key={item.id}
                  className="pt-3.5 first:pt-0 flex items-center justify-between text-xs gap-3"
                >
                  <div className="flex-1 truncate">
                    <div className="flex items-center gap-1.5 truncate">
                      <p className="font-bold text-slate-900 truncate">
                        {item.variantName}
                      </p>
                      {item.requiresColdChain && (
                        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 bg-cyan-50 text-cyan-700 text-[9px] font-bold rounded border border-cyan-200 shrink-0">
                          ❄️ Lạnh
                        </span>
                      )}
                    </div>
                    <span className="text-[11px] text-slate-500 font-medium">
                      {item.quantity} x {formatVND(item.unitPrice)} (
                      {item.uoMName})
                    </span>
                  </div>
                  <span className="font-black text-slate-900 shrink-0">
                    {formatVND(item.totalPrice)}
                  </span>
                </div>
              ))}
            </div>

            <div className="pt-4 border-t border-slate-100 space-y-2.5 text-xs">
              <div className="flex justify-between text-slate-600">
                <span>Tạm tính tiền hàng:</span>
                <span className="font-bold text-slate-900">
                  {formatVND(cart.subTotal)}
                </span>
              </div>

              {cart.totalDiscount > 0 && (
                <div className="flex justify-between text-rose-600 font-semibold">
                  <span>Khuyến mãi & Chiết khấu VIP:</span>
                  <span className="font-bold">
                    -{formatVND(cart.totalDiscount)}
                  </span>
                </div>
              )}

              <div className="flex justify-between items-center text-slate-600">
                <span className="flex items-center gap-1">
                  {hasColdChain ? (
                    <ThermometerSnowflake className="w-3.5 h-3.5 text-cyan-600" />
                  ) : (
                    <Truck className="w-3.5 h-3.5 text-slate-400" />
                  )}
                  {hasColdChain
                    ? "Solaris Cold-Express (Xe lạnh):"
                    : "Phí giao hàng GHN:"}
                </span>
                {isCalculatingFee ? (
                  <span className="text-[11px] text-slate-400 italic">
                    Đang tính cước...
                  </span>
                ) : isFreeShipping || shippingFee === 0 ? (
                  <span className="font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md">
                    Miễn phí
                  </span>
                ) : (
                  <span className="font-bold text-slate-900">
                    {formatVND(shippingFee)}
                  </span>
                )}
              </div>

              <div className="pt-4 border-t border-slate-100 flex justify-between items-baseline">
                <span className="font-extrabold text-slate-900 text-sm">
                  Tổng thanh toán:
                </span>
                <span className="font-black text-2xl text-emerald-800">
                  {formatVND(finalTotal)}
                </span>
              </div>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full h-12 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl font-extrabold text-xs transition-all shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 disabled:opacity-50 active:scale-98 cursor-pointer"
            >
              <span>
                {isSubmitting
                  ? "Đang xử lý đơn hàng..."
                  : paymentMethod === 3
                    ? "Thanh Toán Qua VNPay"
                    : "Xác Nhận & Đặt Hàng"}
              </span>
              <ArrowRight className="w-4 h-4" />
            </button>
          </div>

          <div
            className={`p-5 rounded-3xl border text-xs space-y-1.5 ${
              hasColdChain
                ? "bg-cyan-50/80 border-cyan-200 text-cyan-950"
                : "bg-emerald-50/70 border-emerald-200/60 text-emerald-900"
            }`}
          >
            <div className="flex items-center gap-2 font-bold">
              {hasColdChain ? (
                <>
                  <ThermometerSnowflake className="w-4 h-4 text-cyan-600 shrink-0" />
                  <span>Bảo quản lạnh 0-4°C suốt hành trình giao nhận</span>
                </>
              ) : (
                <>
                  <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0" />
                  <span>Cam kết giao nông sản tươi mới 100%</span>
                </>
              )}
            </div>
            <p className="text-[11px] text-slate-600 leading-relaxed">
              {hasColdChain
                ? "Đơn hàng được bảo quản trong thùng lạnh chuyên dụng của đội xe Solaris, vận chuyển hỏa tốc trong bán kính 15km để giữ trọn chất lượng tươi sống."
                : "Đơn hàng được bàn giao ngay cho đơn vị vận chuyển GHN Express với bao bì đóng gói bảo quản nông sản chuyên dụng."}
            </p>
          </div>
        </div>
      </form>
    </div>
  );
}
