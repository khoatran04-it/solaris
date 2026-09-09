"use client";

import React, { useState } from "react";
import { useLocationStore } from "@/stores/locationStore";
import { useAuthStore } from "@/stores/authStore";
import { ShopAddress } from "@/types/customer";
import GhnAddressSelect, {
  GhnAddressChangePayload,
} from "@/components/address/GhnAddressSelect";

export default function LocationModal() {
  const {
    isModalOpen,
    deliveryAddress,
    savedAddresses,
    isDetectingGps,
    detectionMessage,
    detectGps,
    selectSavedAddress,
    setDefaultAddress,
    setCustomDeliveryAddress,
    closeModal,
  } = useLocationStore();

  const { isAuthenticated } = useAuthStore();

  // State cho Form điền địa chỉ nhận hàng
  const [province, setProvince] = useState("Hồ Chí Minh");
  const [district, setDistrict] = useState("");
  const [ward, setWard] = useState("");
  const [streetAddress, setStreetAddress] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  // Toggle hiển thị form nếu khách đã có địa chỉ trong tài khoản
  const [showManualForm, setShowManualForm] = useState(false);

  if (!isModalOpen) return null;

  const handleAddressChange = (payload: GhnAddressChangePayload) => {
    setProvince(payload.province);
    setDistrict(payload.district);
    setWard(payload.ward);
    setFormError(null);
  };

  const handleConfirmAddress = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    if (!district) {
      setFormError("Vui lòng chọn Quận/Huyện nhận hàng.");
      return;
    }

    const res = setCustomDeliveryAddress(
      streetAddress.trim(),
      ward.trim(),
      district.trim(),
      province.trim(),
    );

    if (!res.success) {
      setFormError(
        res.message || "Khu vực này hiện chưa được hỗ trợ giao hàng.",
      );
      return;
    }

    // Thành công: Reset form & đóng modal
    setShowManualForm(false);
  };

  const hasSavedAddresses = isAuthenticated && savedAddresses.length > 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs transition-opacity animate-in fade-in duration-200">
      {/* Modal Card */}
      <div className="relative w-full max-w-lg bg-white rounded-3xl border border-slate-200/90 shadow-2xl overflow-hidden flex flex-col max-h-[92vh]">
        {/* 1. Modal Header */}
        <div className="px-6 pt-6 pb-4 border-b border-slate-100 flex items-start justify-between">
          <div>
            <div className="inline-block text-[10px] font-black uppercase tracking-widest text-emerald-800 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200 mb-1.5">
              ĐỊA CHỈ GIAO HÀNG
            </div>
            <h2 className="text-lg font-black text-slate-900 leading-tight">
              CHỌN ĐỊA CHỈ NHẬN HÀNG
            </h2>
            <p className="text-xs text-slate-500 mt-1 leading-relaxed">
              Solaris tự động kiểm tra hàng có sẵn tại chi nhánh gần bạn nhất để
              cam kết giao tươi 2 giờ.
            </p>
          </div>

          <button
            type="button"
            onClick={closeModal}
            className="px-3 py-1.5 text-xs font-bold text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-all cursor-pointer"
            title="Đóng cửa sổ"
          >
            ĐÓNG
          </button>
        </div>

        {/* 2. Modal Body */}
        <div className="p-6 overflow-y-auto space-y-5">
          {/* LUỒNG 1: Nút bấm Định vị tự động GPS (1 chạm) */}
          <div className="p-4 bg-emerald-50/70 rounded-2xl border border-emerald-200/80 space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-emerald-950 uppercase tracking-wide">
                VỊ TRÍ HIỆN TẠI CỦA BẠN
              </span>
              <span className="text-[10px] font-bold text-emerald-700 bg-emerald-100/80 px-2 py-0.5 rounded">
                GPS 1 CHẠM
              </span>
            </div>
            <p className="text-xs text-slate-600">
              Tự động xác định vị trí của bạn để tìm sản phẩm tươi ngon sẵn có
              gần nhất.
            </p>

            <button
              type="button"
              onClick={() => detectGps(false)}
              disabled={isDetectingGps}
              className="w-full mt-1.5 py-2.5 px-4 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 disabled:bg-emerald-400 text-white rounded-xl text-xs font-bold transition-all cursor-pointer shadow-xs text-center block"
            >
              {isDetectingGps
                ? "ĐANG XÁC ĐỊNH VỊ TRÍ..."
                : "SỬ DỤNG VỊ TRÍ HIỆN TẠI (GPS)"}
            </button>

            {detectionMessage && (
              <p className="text-[11px] font-medium text-amber-800 bg-amber-50 p-2.5 rounded-xl border border-amber-200/70 mt-2 leading-relaxed">
                {detectionMessage}
              </p>
            )}
          </div>

          {/* Phân cách */}
          <div className="relative flex items-center justify-center">
            <div className="border-t border-slate-200 w-full" />
            <span className="bg-white px-3 text-[10px] font-extrabold uppercase tracking-widest text-slate-400 absolute">
              HOẶC NHẬP ĐỊA CHỈ NHẬN HÀNG
            </span>
          </div>

          {/* LUỒNG 2: Sổ địa chỉ đã lưu (Dành cho khách đã đăng nhập) */}
          {hasSavedAddresses && !showManualForm && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-extrabold text-slate-700 uppercase tracking-wider block">
                  ĐỊA CHỈ ĐÃ LƯU TRONG TÀI KHOẢN:
                </label>
                <button
                  type="button"
                  onClick={() => setShowManualForm(true)}
                  className="text-[11px] font-bold text-emerald-700 hover:text-emerald-900 hover:underline cursor-pointer"
                >
                  + Điền địa chỉ khác
                </button>
              </div>

              <div className="space-y-2.5">
                {savedAddresses.map((addr: ShopAddress) => {
                  const isSelected = deliveryAddress === addr.fullAddress;
                  return (
                    <div
                      key={addr.id}
                      className={`p-3.5 rounded-2xl border transition-all flex flex-col justify-between gap-2.5 ${
                        isSelected
                          ? "bg-emerald-50/80 border-emerald-600 ring-2 ring-emerald-500/20 shadow-xs"
                          : "bg-white border-slate-200 hover:border-emerald-300"
                      }`}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="space-y-0.5">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-slate-900">
                              {addr.receiverName}
                            </span>
                            <span className="text-[10px] text-slate-400">
                              ({addr.phone})
                            </span>
                            {addr.isDefault && (
                              <span className="text-[9px] font-black bg-emerald-600 text-white px-1.5 py-0.2 rounded">
                                MẶC ĐỊNH
                              </span>
                            )}
                          </div>
                          <p className="text-[11px] text-slate-600 leading-snug">
                            {addr.fullAddress}
                          </p>
                        </div>

                        {isSelected && (
                          <span className="text-[10px] font-black tracking-wider uppercase px-2 py-0.5 bg-emerald-700 text-white rounded-md whitespace-nowrap">
                            ĐANG CHỌN
                          </span>
                        )}
                      </div>

                      {/* Action Buttons: Đặt làm mặc định & Giao đến đây */}
                      <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100 text-[11px]">
                        {!addr.isDefault && (
                          <button
                            type="button"
                            onClick={() => setDefaultAddress(addr.id)}
                            className="font-semibold text-slate-500 hover:text-emerald-700 underline cursor-pointer"
                          >
                            Đặt làm mặc định
                          </button>
                        )}

                        <button
                          type="button"
                          onClick={() => selectSavedAddress(addr)}
                          className={`px-3 py-1 rounded-lg font-bold transition-all cursor-pointer ${
                            isSelected
                              ? "bg-emerald-700 text-white"
                              : "bg-slate-100 hover:bg-emerald-600 text-slate-700 hover:text-white"
                          }`}
                        >
                          {isSelected
                            ? "Đang giao đến đây"
                            : "Giao đến địa chỉ này"}
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* LUỒNG 3: Form điền địa chỉ nhận hàng (Khách vãng lai hoặc bấm điền mới) */}
          {(!hasSavedAddresses || showManualForm) && (
            <form onSubmit={handleConfirmAddress} className="space-y-3.5">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-extrabold text-slate-700 uppercase tracking-wider block">
                  ĐIỀN ĐỊA CHỈ NHẬN HÀNG:
                </label>
                {hasSavedAddresses && (
                  <button
                    type="button"
                    onClick={() => setShowManualForm(false)}
                    className="text-[11px] font-bold text-slate-500 hover:text-slate-800 underline cursor-pointer"
                  >
                    Quay lại sổ địa chỉ
                  </button>
                )}
              </div>

              {/* Dropdown 3 cấp GHN: Tỉnh / Quận / Phường */}
              <div className="bg-slate-50 p-3.5 rounded-2xl border border-slate-200/80">
                <GhnAddressSelect
                  province={province}
                  district={district}
                  ward={ward}
                  onChange={handleAddressChange}
                />
              </div>

              {/* Số nhà, tên đường */}
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1">
                  Số nhà, tên đường (Tùy chọn):
                </label>
                <input
                  type="text"
                  placeholder="Ví dụ: 123 Nguyễn Thị Minh Khai"
                  value={streetAddress}
                  onChange={(e) => setStreetAddress(e.target.value)}
                  className="w-full px-3.5 py-2.5 bg-white border border-slate-200 rounded-xl text-xs font-medium text-slate-800 placeholder-slate-400 focus:outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/15"
                />
              </div>

              {/* Báo lỗi ngoài phạm vi phục vụ */}
              {formError && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs font-bold leading-relaxed">
                  {formError}
                </div>
              )}

              {/* Nút Xác nhận địa chỉ */}
              <button
                type="submit"
                className="w-full py-2.5 px-4 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-extrabold uppercase tracking-wide transition-all shadow-xs cursor-pointer text-center"
              >
                Xác Nhận Địa Chỉ Này
              </button>
            </form>
          )}
        </div>

        {/* 3. Modal Footer */}
        <div className="p-4 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
          <span className="truncate max-w-[320px]">
            Địa chỉ hiện tại:{" "}
            <strong className="text-emerald-800 font-bold">
              {deliveryAddress}
            </strong>
          </span>
          <button
            type="button"
            onClick={closeModal}
            className="px-3.5 py-1.5 bg-white border border-slate-200 hover:bg-slate-100 text-slate-700 font-bold rounded-xl transition-all cursor-pointer text-xs"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}
