import React from "react";
import Link from "next/link";
import {
  ArrowRight,
  Flame,
  Leaf,
  CheckCircle2,
  Layers,
  Building2,
  Sparkles,
} from "lucide-react";
import PromoBannerSlider from "@/components/promotion/PromoBannerSlider";
import ProductCard from "@/components/product/ProductCard";
import shopProductApi from "@/api/shopProductApi";

// Force dynamic rendering to avoid build-time static hang
export const dynamic = "force-dynamic";

async function getHomeData() {
  try {
    const [promotions, categories, featured, newArrivals] = await Promise.all([
      shopProductApi.getPromotions().catch(() => []),
      shopProductApi.getCategories().catch(() => []),
      shopProductApi.getFeatured(8).catch(() => []),
      shopProductApi.getNewArrivals(8).catch(() => []),
    ]);

    return { promotions, categories, featured, newArrivals };
  } catch {
    return { promotions: [], categories: [], featured: [], newArrivals: [] };
  }
}

export default async function HomePage() {
  const { promotions, categories, featured, newArrivals } = await getHomeData();

  return (
    <div className="space-y-16 pb-20">
      {/* 1. Hero Promo Banner Slider */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-6">
        <PromoBannerSlider promotions={promotions} />
      </section>

      {/* 2. Category Quick Access Cards */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="text-center max-w-2xl mx-auto space-y-2 mb-10">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-emerald-50 border border-emerald-200 text-emerald-800 text-[11px] font-bold rounded-full">
            <Sparkles className="w-3 h-3 text-amber-500" />
            <span>TUYỂN CHỌN NÔNG SẢN TƯƠI MỖI NGÀY</span>
          </div>
          <h2 className="text-2xl sm:text-3xl font-black tracking-tight text-slate-900 font-sans">
            Danh Mục Nông Sản Nổi Bật
          </h2>
          <p className="text-xs sm:text-sm text-slate-500">
            Phân loại rõ ràng theo chuẩn an toàn thực phẩm VietGAP, GlobalGAP &
            nguồn gốc vùng trồng
          </p>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 sm:gap-5">
          {categories.map((group) => {
            const count = group.categories.reduce(
              (sum, c) => sum + c.productCount,
              0,
            );

            return (
              <Link
                key={group.groupId}
                href={`/danh-muc/${group.groupSlug}`}
                className="group p-5 bg-white hover:bg-emerald-50/50 rounded-3xl border border-slate-200/80 hover:border-emerald-300 shadow-[0_2px_12px_-3px_rgba(0,0,0,0.03)] hover:shadow-lg hover:-translate-y-1 transition-all duration-300 text-center flex flex-col items-center justify-center space-y-3"
              >
                <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-emerald-50 to-teal-100/60 text-emerald-700 flex items-center justify-center group-hover:scale-110 group-hover:bg-emerald-600 group-hover:text-white transition-all duration-300 shadow-2xs">
                  <Layers className="w-6 h-6" />
                </div>
                <div className="space-y-0.5">
                  <h3 className="font-extrabold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors line-clamp-1">
                    {group.groupName}
                  </h3>
                  <span className="text-[10px] text-slate-400 font-semibold block">
                    {count} sản phẩm
                  </span>
                </div>
              </Link>
            );
          })}
        </div>
      </section>

      {/* 3. Featured / Flash Sale Products */}
      {featured.length > 0 && (
        <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-8 pb-4 border-b border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-rose-100 text-rose-600 rounded-2xl">
                <Flame className="w-5 h-5 animate-bounce" />
              </div>
              <div>
                <h2 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                  Ưu Đãi Hôm Nay
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Giảm giá hấp dẫn các mặt hàng vụ mùa thu hoạch tươi mới
                </p>
              </div>
            </div>

            <Link
              href="/khuyen-mai"
              className="text-xs font-bold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
            >
              <span>Xem tất cả</span>
              <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
            </Link>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {featured.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* 4. Fresh Harvest / New Arrivals */}
      {newArrivals.length > 0 && (
        <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-8 pb-4 border-b border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-emerald-100 text-emerald-700 rounded-2xl">
                <Leaf className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                  Mới Về Hôm Nay
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Thu hoạch sáng sớm từ các nông trường công nghệ cao Đà Lạt
                </p>
              </div>
            </div>

            <Link
              href="/san-pham?sort=newest"
              className="text-xs font-bold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
            >
              <span>Xem tất cả</span>
              <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
            </Link>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {newArrivals.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* 5. Trust & Quality Banner (Farm to Table) */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-gradient-to-br from-emerald-900 via-emerald-800 to-teal-900 rounded-3xl p-8 sm:p-12 text-white shadow-xl grid grid-cols-1 lg:grid-cols-12 gap-10 items-center relative overflow-hidden">
          <div className="absolute right-0 bottom-0 w-96 h-96 bg-emerald-400/10 rounded-full blur-3xl pointer-events-none" />

          <div className="lg:col-span-7 space-y-5 relative z-10">
            <span className="px-3.5 py-1 bg-white/15 border border-white/20 text-emerald-100 rounded-full text-[11px] font-extrabold tracking-wide backdrop-blur-xs">
              KỶ LUẬT THÉP CỦA NGÀNH NÔNG SẢN SẠCH
            </span>
            <h2 className="text-2xl sm:text-3xl font-black leading-tight tracking-tight">
              Cam Kết Chất Lượng Vượt Trội Từ Solaris Farm
            </h2>
            <p className="text-xs sm:text-sm text-emerald-100/90 leading-relaxed font-medium">
              Mỗi sản phẩm tại Solaris đều tuân thủ nghiêm ngặt quy trình kiểm
              soát FEFO (Hết hạn trước - Xuất trước), độ ngọt Brix đo đạc minh
              bạch và truy xuất xuất xứ chuẩn Nghị định 15/2018/NĐ-CP.
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5 pt-2 text-xs font-semibold text-emerald-100">
              <div className="flex items-center gap-2.5">
                <CheckCircle2 className="w-4 h-4 text-amber-300 shrink-0" />
                <span>100% kiểm định an toàn QC trước khi xuất kho</span>
              </div>
              <div className="flex items-center gap-2.5">
                <CheckCircle2 className="w-4 h-4 text-amber-300 shrink-0" />
                <span>Giao hàng tươi lạnh 2H nội thành</span>
              </div>
              <div className="flex items-center gap-2.5">
                <CheckCircle2 className="w-4 h-4 text-amber-300 shrink-0" />
                <span>Bảo quản chuỗi lạnh nhiệt độ 2°C - 8°C</span>
              </div>
              <div className="flex items-center gap-2.5">
                <CheckCircle2 className="w-4 h-4 text-amber-300 shrink-0" />
                <span>Đổi trả / Hoàn tiền 100% trong 24h</span>
              </div>
            </div>
          </div>

          <div className="lg:col-span-5 bg-white text-slate-900 rounded-3xl p-7 sm:p-8 space-y-5 text-center shadow-xl relative z-10">
            <div className="w-16 h-16 rounded-2xl bg-emerald-50 text-emerald-700 flex items-center justify-center mx-auto shadow-2xs">
              <Building2 className="w-8 h-8" />
            </div>
            <div className="space-y-1.5">
              <h3 className="text-lg font-black text-slate-900">
                Bạn là Nông Hộ hoặc Khách Hàng B2B?
              </h3>
              <p className="text-xs text-slate-500 leading-relaxed">
                Hợp tác cùng chuỗi cung ứng Solaris để đưa nông sản sạch chất
                lượng cao đến hàng triệu gia đình.
              </p>
            </div>
            <Link
              href="/san-pham"
              className="inline-block w-full py-3.5 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-extrabold text-xs rounded-2xl shadow-md transition-all active:scale-98"
            >
              Khám Phá Danh Mục Nông Sản
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
