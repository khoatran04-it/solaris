import React from 'react';
import Link from 'next/link';
import { ArrowRight, Flame, Leaf, CheckCircle2 } from 'lucide-react';
import PromoBannerSlider from '@/components/promotion/PromoBannerSlider';
import ProductCard from '@/components/product/ProductCard';
import shopProductApi from '@/api/shopProductApi';

// Force dynamic rendering to avoid build-time static hang
export const dynamic = 'force-dynamic';

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

            {/* 2. Category Quick Access Circles */}
            <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="text-center max-w-2xl mx-auto space-y-2 mb-8">
                    <h2 className="text-xl sm:text-2xl font-black tracking-tight text-slate-900">
                        Danh Mục Nông Sản Nổi Bật
                    </h2>
                    <p className="text-xs text-slate-500">
                        Lựa chọn tươi ngon từ vườn mỗi ngày, phân loại theo tiêu chuẩn chất lượng cao
                    </p>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
                    {categories.map((group) => (
                        <Link
                            key={group.groupId}
                            href={`/danh-muc/${group.groupSlug}`}
                            className="group p-4 bg-white hover:bg-emerald-50/60 rounded-2xl border border-slate-200/70 hover:border-emerald-300 shadow-xs hover:shadow-md transition-all text-center flex flex-col items-center justify-center space-y-2.5"
                        >
                            <div className="w-14 h-14 rounded-2xl bg-emerald-100/50 flex items-center justify-center text-3xl group-hover:scale-110 transition-transform">
                                {group.groupSlug.includes('trai-cay') ? '🍎' : 
                                 group.groupSlug.includes('rau') ? '🥦' : 
                                 group.groupSlug.includes('thit') ? '🥩' : '🌿'}
                            </div>
                            <div>
                                <h4 className="font-bold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors">
                                    {group.groupName}
                                </h4>
                                <span className="text-[10px] text-slate-400 font-medium">
                                    {group.categories.reduce((sum, c) => sum + c.productCount, 0)} sản phẩm
                                </span>
                            </div>
                        </Link>
                    ))}
                </div>
            </section>

            {/* 3. Featured / Flash Sale Products */}
            {featured.length > 0 && (
                <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex items-center justify-between mb-8 pb-3 border-b border-slate-200">
                        <div className="flex items-center gap-2">
                            <div className="p-2 bg-rose-100 text-rose-600 rounded-xl">
                                <Flame className="w-5 h-5 animate-bounce" />
                            </div>
                            <div>
                                <h2 className="text-xl font-black text-slate-900">Ưu Đãi Hôm Nay</h2>
                                <p className="text-xs text-slate-500">Giảm giá hấp dẫn các mặt hàng vụ mùa thu hoạch</p>
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
                    <div className="flex items-center justify-between mb-8 pb-3 border-b border-slate-200">
                        <div className="flex items-center gap-2">
                            <div className="p-2 bg-emerald-100 text-emerald-700 rounded-xl">
                                <Leaf className="w-5 h-5" />
                            </div>
                            <div>
                                <h2 className="text-xl font-black text-slate-900">Mới Về Hôm Nay</h2>
                                <p className="text-xs text-slate-500">Thu hoạch sáng sớm từ các nông trường Đà Lạt</p>
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
                <div className="bg-gradient-to-br from-emerald-900 via-teal-900 to-slate-900 text-white rounded-3xl p-8 sm:p-12 shadow-xl border border-emerald-500/20 grid grid-cols-1 lg:grid-cols-2 gap-8 items-center">
                    <div className="space-y-4">
                        <span className="px-3 py-1 bg-emerald-400/20 border border-emerald-300/30 text-emerald-300 rounded-full text-xs font-bold">
                            CAM KẾT CHẤT LƯỢNG SOLARIS
                        </span>
                        <h2 className="text-2xl sm:text-3xl font-black leading-tight">
                            Kỷ Luật Thép Của Ngành Nông Sản & Thực Phẩm
                        </h2>
                        <p className="text-xs text-emerald-100/80 leading-relaxed">
                            Mỗi sản phẩm tại Solaris đều tuân thủ nghiêm ngặt quy trình kiểm soát FEFO (Hết hạn trước - Xuất trước), độ ngọt Brix đo đạc minh bạch và truy xuất xuất xứ chuẩn Nghị định 15/2018/NĐ-CP.
                        </p>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
                            <div className="flex items-center gap-2 text-xs font-semibold text-emerald-200">
                                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>100% kiểm định an toàn QC</span>
                            </div>
                            <div className="flex items-center gap-2 text-xs font-semibold text-emerald-200">
                                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Giao hàng tươi sống 2h</span>
                            </div>
                            <div className="flex items-center gap-2 text-xs font-semibold text-emerald-200">
                                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Bảo quản nhiệt độ 2-8°C</span>
                            </div>
                            <div className="flex items-center gap-2 text-xs font-semibold text-emerald-200">
                                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                                <span>Đổi trả miễn phí 24h</span>
                            </div>
                        </div>
                    </div>

                    <div className="bg-white/5 border border-white/10 backdrop-blur-md rounded-2xl p-6 space-y-4 text-center">
                        <div className="text-5xl">🚜</div>
                        <h3 className="text-lg font-bold text-white">Bạn là Nông Hộ hoặc Khách B2B?</h3>
                        <p className="text-xs text-slate-300">
                            Hợp tác cùng chuỗi cung ứng Solaris để đưa nông sản chất lượng cao đến hàng triệu gia đình.
                        </p>
                        <Link
                            href="/san-pham"
                            className="inline-block px-6 py-2.5 bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold text-xs rounded-full shadow-lg transition-all"
                        >
                            Xem Thêm Danh Mục
                        </Link>
                    </div>
                </div>
            </section>

        </div>
    );
}

