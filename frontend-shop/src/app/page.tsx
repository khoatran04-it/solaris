import React from "react";
import Link from "next/link";
import { ArrowRight, Flame, Leaf, Sparkles } from "lucide-react";
import PromoBannerSlider from "@/components/promotion/PromoBannerSlider";
import ProductCard from "@/components/product/ProductCard";
import shopProductApi from "@/api/shopProductApi";

// Force dynamic rendering to avoid build-time static hang
export const dynamic = "force-dynamic";

async function getHomeData() {
  try {
    const [promotions, featured, newArrivals, deals] = await Promise.all([
      shopProductApi.getPromotions().catch(() => []),
      shopProductApi.getFeatured(20).catch(() => []),
      shopProductApi.getNewArrivals(20).catch(() => []),
      shopProductApi.getDeals(20).catch(() => []),
    ]);

    return { promotions, featured, newArrivals, deals };
  } catch {
    return { promotions: [], featured: [], newArrivals: [], deals: [] };
  }
}

export default async function HomePage() {
  const { promotions, featured, newArrivals, deals } = await getHomeData();

  return (
    <div className="space-y-16 pb-20">
      {/* 1. Hero Promo Banner Slider */}
      <section className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8 pt-6">
        <PromoBannerSlider promotions={promotions} />
      </section>

      {/* 2. Sản Phẩm Nổi Bật (Lọc sản phẩm được mua nhiều nhất từ database) */}
      {featured.length > 0 && (
        <section className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-8 pb-4 border-b border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-amber-100 text-amber-700 rounded-2xl shadow-2xs">
                <Sparkles className="w-5 h-5 text-amber-600" />
              </div>
              <div>
                <h2 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                  Sản Phẩm Nổi Bật
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Top nông sản sạch được khách hàng tin tưởng và chọn mua nhiều nhất
                </p>
              </div>
            </div>

            <Link
              href="/san-pham"
              className="text-xs font-bold text-emerald-700 hover:text-emerald-800 flex items-center gap-1 group"
            >
              <span>Xem tất cả</span>
              <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
            </Link>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-7 gap-3 sm:gap-3.5">
            {featured.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* 3. Ưu Đãi Hôm Nay (Chỉ hiển thị các sản phẩm thật sự có chiến dịch khuyến mãi đang diễn ra) */}
      {deals.length > 0 && (
        <section className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-8 pb-4 border-b border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-rose-100 text-rose-600 rounded-2xl shadow-2xs">
                <Flame className="w-5 h-5 animate-bounce" />
              </div>
              <div>
                <h2 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                  Ưu Đãi Hôm Nay
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Giảm giá hấp dẫn các mặt hàng trong chiến dịch khuyến mãi đang diễn ra
                </p>
              </div>
            </div>

            <Link
              href="/khuyen-mai"
              className="text-xs font-bold text-rose-700 hover:text-rose-800 flex items-center gap-1 group"
            >
              <span>Xem tất cả ưu đãi</span>
              <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
            </Link>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-7 gap-3 sm:gap-3.5">
            {deals.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* 4. Nông Sản Mới Về (20 sản phẩm mới nhất) */}
      {newArrivals.length > 0 && (
        <section className="max-w-screen-2xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between mb-8 pb-4 border-b border-slate-200">
            <div className="flex items-center gap-3">
              <div className="p-2.5 bg-emerald-100 text-emerald-700 rounded-2xl shadow-2xs">
                <Leaf className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-xl sm:text-2xl font-black text-slate-900 tracking-tight">
                  Nông Sản Mới Nhập
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

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-7 gap-3 sm:gap-3.5">
            {newArrivals.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
