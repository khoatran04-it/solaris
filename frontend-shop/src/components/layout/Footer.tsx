import React from "react";
import Link from "next/link";
import {
  ShieldCheck,
  Truck,
  RotateCcw,
  Award,
  Phone,
  Mail,
  MapPin,
  CheckCircle2,
} from "lucide-react";

export default function Footer() {
  return (
    <footer className="bg-white text-slate-600 pt-16 pb-10 border-t border-slate-200/80">
      {/* 1. Value Proposition Features Bar */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-14 border-b border-slate-100">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
          <div className="flex items-start gap-4 p-5 rounded-2xl bg-slate-50/70 border border-slate-100 hover:border-emerald-200 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-100/70 text-emerald-700 flex items-center justify-center shrink-0">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-extrabold text-slate-900 text-xs uppercase tracking-wide">
                100% Nông Sản Sạch
              </h4>
              <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                Chuẩn VietGAP, GlobalGAP & minh bạch nguồn gốc vùng trồng.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 p-5 rounded-2xl bg-slate-50/70 border border-slate-100 hover:border-emerald-200 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-100/70 text-emerald-700 flex items-center justify-center shrink-0">
              <Truck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-extrabold text-slate-900 text-xs uppercase tracking-wide">
                Giao Hàng Tươi Lạnh 2H
              </h4>
              <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                Chuỗi cung ứng lạnh bảo toàn độ tươi giòn và dưỡng chất.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 p-5 rounded-2xl bg-slate-50/70 border border-slate-100 hover:border-emerald-200 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-100/70 text-emerald-700 flex items-center justify-center shrink-0">
              <RotateCcw className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-extrabold text-slate-900 text-xs uppercase tracking-wide">
                Đổi Trả Trong 24H
              </h4>
              <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                Bảo hành 1 đổi 1 hoặc hoàn tiền nếu sản phẩm dập nát, lỗi QC.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4 p-5 rounded-2xl bg-slate-50/70 border border-slate-100 hover:border-emerald-200 transition-colors">
            <div className="w-12 h-12 rounded-xl bg-emerald-100/70 text-emerald-700 flex items-center justify-center shrink-0">
              <Award className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-extrabold text-slate-900 text-xs uppercase tracking-wide">
                Giá Trực Tiếp Tại Vườn
              </h4>
              <p className="text-xs text-slate-500 mt-1 leading-relaxed">
                Liên kết chặt chẽ với hợp tác xã nông sản sạch Đà Lạt.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Main Navigation Links */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-10">
          {/* Brand Overview */}
          <div className="lg:col-span-2 space-y-4">
            <div className="flex items-center gap-2.5">
              <div className="w-8 h-8 rounded-xl bg-emerald-600 flex items-center justify-center text-white font-black text-sm shadow-xs">
                <svg
                  viewBox="0 0 24 24"
                  className="w-5 h-5 fill-none stroke-current stroke-2"
                >
                  <circle
                    cx="12"
                    cy="12"
                    r="5"
                    className="fill-amber-400 stroke-amber-400"
                  />
                  <path d="M12 2v2" strokeLinecap="round" />
                  <path d="M12 20v2" strokeLinecap="round" />
                </svg>
              </div>
              <span className="text-lg font-black tracking-tight text-slate-900">
                SOLARIS ERP FARM
              </span>
            </div>
            <p className="text-xs text-slate-500 leading-relaxed max-w-sm">
              Hệ sinh thái phân phối nông sản công nghệ cao, kết nối trực tiếp
              từ nông trại đến bàn ăn gia đình Việt với kỷ luật quản trị kho
              FEFO chuẩn quốc tế.
            </p>
            <div className="space-y-2.5 text-xs text-slate-600 pt-2 font-medium">
              <div className="flex items-start gap-2.5">
                <MapPin className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
                <span>
                  Trụ sở chính: Tầng 8, Tòa nhà Solaris Plaza, Quận 1, TP. Hồ
                  Chí Minh
                </span>
              </div>
              <div className="flex items-center gap-2.5">
                <Phone className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>
                  Tổng đài tư vấn: <strong>1900 8888</strong> (7:30 - 21:30 hàng
                  ngày)
                </span>
              </div>
              <div className="flex items-center gap-2.5">
                <Mail className="w-4 h-4 text-emerald-600 shrink-0" />
                <span>Email liên hệ: hotro@solaris.vn</span>
              </div>
            </div>
          </div>

          {/* Về Solaris */}
          <div>
            <h4 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider mb-4 flex items-center gap-2">
              <span className="w-1.5 h-4 bg-emerald-500 rounded-full inline-block"></span>
              Về Solaris
            </h4>
            <ul className="space-y-2.5 text-xs text-slate-600">
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Giới thiệu nông trại
                </Link>
              </li>
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Tiêu chuẩn VietGAP / GlobalGAP
                </Link>
              </li>
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Quy trình kiểm soát chất lượng QC
                </Link>
              </li>
              <li>
                <Link
                  href="/khuyen-mai"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Chương trình khuyến mãi vụ mùa
                </Link>
              </li>
            </ul>
          </div>

          {/* Chính sách */}
          <div>
            <h4 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider mb-4 flex items-center gap-2">
              <span className="w-1.5 h-4 bg-emerald-500 rounded-full inline-block"></span>
              Chính Sách
            </h4>
            <ul className="space-y-2.5 text-xs text-slate-600">
              <li>
                <Link
                  href="/tai-khoan/tra-hang"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Chính sách đổi trả trong 24h
                </Link>
              </li>
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Chính sách giao hàng lạnh 2h
                </Link>
              </li>
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Chính sách bảo mật thông tin
                </Link>
              </li>
              <li>
                <Link
                  href="/san-pham"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Công bố ATTP Nghị định 15/2018
                </Link>
              </li>
            </ul>
          </div>

          {/* Khách hàng */}
          <div>
            <h4 className="text-xs font-extrabold text-slate-900 uppercase tracking-wider mb-4 flex items-center gap-2">
              <span className="w-1.5 h-4 bg-emerald-500 rounded-full inline-block"></span>
              Khách Hàng
            </h4>
            <ul className="space-y-2.5 text-xs text-slate-600">
              <li>
                <Link
                  href="/dang-nhap"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Đăng nhập tài khoản
                </Link>
              </li>
              <li>
                <Link
                  href="/tai-khoan/don-hang"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Tra cứu lịch sử đơn hàng
                </Link>
              </li>
              <li>
                <Link
                  href="/gio-hang"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Giỏ hàng của bạn
                </Link>
              </li>
              <li>
                <Link
                  href="/tai-khoan"
                  className="hover:text-emerald-700 transition-colors font-medium"
                >
                  Hạng thành viên & Chiết khấu VIP
                </Link>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* 3. Copyright & Certifications */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-400">
        <p>
          © 2026 Solaris Farm E-Commerce. Nền tảng phân phối nông sản công nghệ
          cao.
        </p>
        <div className="flex items-center gap-4 text-[11px] font-semibold text-slate-500">
          <span className="flex items-center gap-1">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" /> Chuẩn
            VietGAP
          </span>
          <span className="flex items-center gap-1">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" /> Chuẩn
            GlobalGAP
          </span>
          <span className="flex items-center gap-1">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" /> Quản lý
            kho FEFO
          </span>
        </div>
      </div>
    </footer>
  );
}
