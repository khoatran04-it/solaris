"use client";

import React, { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { MapPin, Plus, Trash2, CheckCircle2 } from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import shopCustomerApi from "@/api/shopCustomerApi";
import { ShopAddress, ShopAddressPayload } from "@/types/customer";
import GhnAddressSelect from "@/components/address/GhnAddressSelect";
import AccountSidebar from "@/components/account/AccountSidebar";
import EmptyState from "@/components/common/EmptyState";

export default function DiaChiPage() {
  const router = useRouter();
  const { isAuthenticated, initAuth } = useAuthStore();
  const [addresses, setAddresses] = useState<ShopAddress[]>([]);
  const [showAddForm, setShowAddForm] = useState(false);

  // Form
  const [receiverName, setReceiverName] = useState("");
  const [phone, setPhone] = useState("");
  const [province, setProvince] = useState("TP. Hồ Chí Minh");
  const [district, setDistrict] = useState("");
  const [ward, setWard] = useState("");
  const [streetAddress, setStreetAddress] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    initAuth();
  }, [initAuth]);

  const loadAddresses = () => {
    shopCustomerApi
      .getAddresses()
      .then(setAddresses)
      .catch((err: any) => {
        if (
          err?.status === 401 ||
          err?.response?.status === 401 ||
          err?.message?.includes("401") ||
          (typeof err === "string" && err.includes("401"))
        ) {
          alert("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
          router.push("/dang-nhap?redirect=/tai-khoan/dia-chi");
        }
      });
  };

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/dang-nhap?redirect=/tai-khoan/dia-chi");
      return;
    }
    loadAddresses();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, router]);

  const handleAddAddress = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);

    try {
      await shopCustomerApi.createAddress({
        receiverName: receiverName.trim(),
        phone: phone.trim(),
        province: province.trim(),
        district: district.trim(),
        ward: ward.trim(),
        streetAddress: streetAddress.trim(),
        isDefault,
        latitude: 0,
        longitude: 0,
      } as ShopAddressPayload);

      setShowAddForm(false);
      setReceiverName("");
      setPhone("");
      setDistrict("");
      setWard("");
      setStreetAddress("");
      setIsDefault(false);
      loadAddresses();
    } catch (error: any) {
      if (
        error?.status === 401 ||
        error?.response?.status === 401 ||
        error?.message?.includes("401") ||
        (typeof error === "string" && error.includes("401"))
      ) {
        alert("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        router.push("/dang-nhap?redirect=/tai-khoan/dia-chi");
        return;
      }
      alert(error?.message || "Không thể thêm địa chỉ.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Bạn có chắc muốn xóa địa chỉ này?")) return;
    try {
      await shopCustomerApi.deleteAddress(id);
      loadAddresses();
    } catch (error: any) {
      alert(error?.message || "Không thể xóa địa chỉ.");
    }
  };

  const handleSetDefault = async (id: number) => {
    try {
      await shopCustomerApi.setDefaultAddress(id);
      loadAddresses();
    } catch (error: any) {
      alert(error?.message || "Không thể thiết lập địa chỉ mặc định.");
    }
  };

  return (
    <div className="min-h-[85vh] bg-slate-50/40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
            Sổ Địa Chỉ Nhận Hàng
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Quản lý danh sách địa chỉ giao hàng để đặt mua nông sản nhanh chóng
            hơn
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          {/* Sidebar */}
          <AccountSidebar activeTab="addresses" className="lg:col-span-4" />

          {/* Content */}
          <div className="lg:col-span-8 space-y-6">
            <div className="flex items-center justify-between">
              <h2 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider">
                Danh Sách Địa Chỉ Đã Lưu ({addresses.length})
              </h2>
              {!showAddForm && (
                <button
                  onClick={() => setShowAddForm(true)}
                  className="inline-flex items-center gap-1.5 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-2xl text-xs font-extrabold transition-all shadow-md shadow-emerald-600/20 active:scale-95 cursor-pointer"
                >
                  <Plus className="w-4 h-4" />
                  <span>Thêm địa chỉ mới</span>
                </button>
              )}
            </div>

            {showAddForm && (
              <div className="bg-white rounded-3xl shadow-[0_4px_25px_-5px_rgba(0,0,0,0.05)] border border-slate-200/80 p-6 sm:p-8 space-y-5 animate-in fade-in">
                <h3 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider pb-3 border-b border-slate-100 flex items-center gap-2">
                  <span className="w-1.5 h-4 bg-emerald-600 rounded-full"></span>
                  Thêm Địa Chỉ Nhận Hàng Mới
                </h3>

                <form
                  onSubmit={handleAddAddress}
                  className="grid grid-cols-1 sm:grid-cols-2 gap-4"
                >
                  <div className="space-y-1">
                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
                      Họ tên người nhận *
                    </label>
                    <input
                      type="text"
                      value={receiverName}
                      onChange={(e) => setReceiverName(e.target.value)}
                      className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                      required
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
                      Số điện thoại *
                    </label>
                    <input
                      type="tel"
                      value={phone}
                      onChange={(e) => setPhone(e.target.value)}
                      className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                      required
                    />
                  </div>

                  <div className="sm:col-span-2">
                    <GhnAddressSelect
                      province={province}
                      district={district}
                      ward={ward}
                      onChange={(p) => {
                        setProvince(p.province);
                        setDistrict(p.district);
                        setWard(p.ward);
                      }}
                    />
                  </div>

                  <div className="space-y-1 sm:col-span-2">
                    <label className="font-extrabold text-xs text-slate-700 uppercase tracking-wide block">
                      Địa chỉ cụ thể (Số nhà, tên đường) *
                    </label>
                    <input
                      type="text"
                      placeholder="123 Đường Lê Lợi"
                      value={streetAddress}
                      onChange={(e) => setStreetAddress(e.target.value)}
                      className="w-full h-11 px-3.5 rounded-xl border border-slate-200 text-xs bg-slate-50/70 hover:bg-white focus:bg-white focus:border-emerald-600 focus:ring-4 focus:ring-emerald-500/15 focus:outline-none transition-all text-slate-800 font-medium"
                      required
                    />
                  </div>

                  <div className="sm:col-span-2 flex items-center gap-2 pt-1">
                    <input
                      type="checkbox"
                      id="isDefault"
                      checked={isDefault}
                      onChange={(e) => setIsDefault(e.target.checked)}
                      className="w-4 h-4 text-emerald-600 rounded border-slate-300 focus:ring-emerald-500 cursor-pointer"
                    />
                    <label
                      htmlFor="isDefault"
                      className="text-xs text-slate-700 font-bold cursor-pointer"
                    >
                      Đặt làm địa chỉ nhận hàng mặc định
                    </label>
                  </div>

                  <div className="sm:col-span-2 flex items-center gap-3 pt-3">
                    <button
                      type="submit"
                      disabled={isSaving}
                      className="px-6 h-11 bg-emerald-600 hover:bg-emerald-700 text-white rounded-2xl text-xs font-extrabold transition-all shadow-md shadow-emerald-600/20 active:scale-98 cursor-pointer disabled:opacity-50"
                    >
                      {isSaving ? "Đang lưu..." : "Lưu địa chỉ"}
                    </button>
                    <button
                      type="button"
                      onClick={() => setShowAddForm(false)}
                      className="px-5 h-11 bg-slate-100 text-slate-700 rounded-2xl text-xs font-bold hover:bg-slate-200 transition-colors cursor-pointer"
                    >
                      Hủy
                    </button>
                  </div>
                </form>
              </div>
            )}

            {addresses.length > 0 ? (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {addresses.map((addr) => (
                  <div
                    key={addr.id}
                    className={`bg-white rounded-3xl border p-5 sm:p-6 transition-all hover:border-emerald-300 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] flex flex-col justify-between space-y-4 ${
                      addr.isDefault
                        ? "border-emerald-500 ring-2 ring-emerald-500/20 bg-emerald-50/20"
                        : "border-slate-200/80"
                    }`}
                  >
                    <div className="space-y-2">
                      <div className="flex items-center justify-between">
                        <h4 className="font-bold text-xs text-slate-900 flex items-center gap-1.5">
                          <span>{addr.receiverName}</span>
                        </h4>
                        {addr.isDefault && (
                          <span className="px-2.5 py-0.5 bg-emerald-100 text-emerald-800 text-[10px] font-extrabold rounded-full flex items-center gap-1">
                            <CheckCircle2 className="w-3 h-3" />
                            Mặc định
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-slate-600 font-semibold">
                        {addr.phone}
                      </p>
                      <p className="text-xs text-slate-500 leading-relaxed font-medium">
                        {addr.streetAddress}, {addr.ward}, {addr.district},{" "}
                        {addr.province}
                      </p>
                    </div>

                    <div className="pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
                      {!addr.isDefault ? (
                        <button
                          onClick={() => handleSetDefault(addr.id)}
                          className="text-emerald-700 hover:text-emerald-800 font-bold text-xs cursor-pointer"
                        >
                          Đặt làm mặc định
                        </button>
                      ) : (
                        <div />
                      )}

                      <button
                        onClick={() => handleDelete(addr.id)}
                        className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-xl transition-colors cursor-pointer"
                        title="Xóa địa chỉ"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ) : !showAddForm ? (
              <EmptyState
                icon={<MapPin className="w-8 h-8" />}
                title="Chưa có địa chỉ nào được lưu"
                description="Thêm địa chỉ giao hàng để đặt mua nông sản nhanh chóng và thuận tiện hơn."
                actionText="Thêm địa chỉ mới"
                onAction={() => setShowAddForm(true)}
              />
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}
