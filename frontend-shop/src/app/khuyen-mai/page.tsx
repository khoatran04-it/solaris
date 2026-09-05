import React from "react";
import { Metadata } from "next";
import Link from "next/link";
import { Sparkles, Clock, ArrowRight, Tag, Gift } from "lucide-react";
import EmptyState from "@/components/common/EmptyState";
import shopProductApi from "@/api/shopProductApi";
import { formatDate } from "@/lib/utils";
import { ShopPromotionBadge } from "@/types/product";

export const metadata: Metadata = {
  title: "Khuyến Mãi Nông Sản Sạch",
  description:
    "Tổng hợp các chương trình ưu đãi, flash sale nông sản sạch tươi ngon tại Solaris Farm.",
};

// Force dynamic rendering to avoid build-time static hang
export const dynamic = "force-dynamic";

export default async function KhuyenMaiPage() {
  let promotions: ShopPromotionBadge[] = [];
  try {
    promotions = await shopProductApi.getPromotions();
  } catch {
    promotions = [];
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-10">
      {/* Header Banner */}
      <div className="bg-gradient-to-r from-rose-700 via-pink-700 to-amber-700 text-white rounded-3xl p-8 sm:p-12 shadow-xl relative overflow-hidden">
        <div className="absolute right-0 top-0 w-96 h-96 bg-white/10 rounded-full blur-3xl pointer-events-none" />
        <div className="relative z-10 space-y-3 max-w-xl">
          <span className="inline-flex items-center gap-1.5 px-3.5 py-1 bg-white/20 backdrop-blur-xs rounded-full text-xs font-extrabold tracking-wide">
            <Sparkles className="w-3.5 h-3.5 text-amber-200 animate-pulse" />
            ĐẠI TIỆC NÔNG SẢN TƯƠI SẠCH
          </span>
          <h1 className="text-3xl sm:text-4xl font-black tracking-tight">
            Chương Trình Khuyến Mãi
          </h1>
          <p className="text-xs sm:text-sm text-pink-100/90 leading-relaxed font-medium">
            Săn nông sản sạch đạt chuẩn VietGAP & GlobalGAP với mức giá siêu ưu
            đãi trực tiếp từ các nông trường công nghệ cao.
          </p>
        </div>
      </div>

      {/* Promotions List */}
      {promotions.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {promotions.map((promo) => (
            <div
              key={promo.id}
              className="bg-white rounded-3xl border border-slate-200/80 p-6 sm:p-7 shadow-[0_2px_15px_-3px_rgba(0,0,0,0.03)] hover:shadow-xl hover:border-pink-300 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between space-y-5 group"
            >
              <div className="space-y-3.5">
                <div className="flex items-center justify-between">
                  <span className="px-3 py-1 bg-rose-50 text-rose-700 font-black text-xs rounded-full border border-rose-200">
                    Giảm{" "}
                    {promo.isPercentage
                      ? `${promo.discountValue}%`
                      : `${promo.discountValue}đ`}
                  </span>
                  <Tag className="w-4 h-4 text-slate-400 group-hover:text-rose-600 transition-colors" />
                </div>

                <h3 className="font-black text-base text-slate-900 group-hover:text-rose-600 transition-colors line-clamp-2">
                  {promo.name}
                </h3>

                <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
                  <Clock className="w-3.5 h-3.5 text-amber-500" />
                  <span>Hạn ưu đãi: {formatDate(promo.endDate)}</span>
                </div>
              </div>

              <Link
                href={`/khuyen-mai/${promo.slug}`}
                className="inline-flex items-center justify-center gap-2 w-full py-3 bg-slate-900 group-hover:bg-rose-600 text-white rounded-2xl text-xs font-extrabold transition-all shadow-md active:scale-98"
              >
                <span>Xem sản phẩm ưu đãi</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Gift className="w-7 h-7" />}
          title="Chưa có chương trình mới hôm nay"
          description="Vui lòng quay lại sau để đón nhận các đợt ưu đãi nông sản vụ mùa mới!"
          actionText="Xem tất cả sản phẩm"
          actionHref="/san-pham"
        />
      )}
    </div>
  );
}
