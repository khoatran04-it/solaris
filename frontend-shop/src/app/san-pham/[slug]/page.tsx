import React from 'react';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { MapPin, Award, CheckCircle2, FileText, Info, ShieldCheck, Sparkles } from 'lucide-react';
import ProductDetailClient from '@/components/product/ProductDetailClient';
import JsonLdProduct from '@/components/seo/JsonLdProduct';
import shopProductApi from '@/api/shopProductApi';
import { ShopProductDetail } from '@/types/product';

interface ProductDetailPageProps {
    params: Promise<{
        slug: string;
    }>;
}

// Generate dynamic SEO Metadata for SEOQuake
export async function generateMetadata({ params }: ProductDetailPageProps): Promise<Metadata> {
    const { slug } = await params;
    try {
        const product = await shopProductApi.getBySlug(slug);
        if (!product) return { title: 'Sản phẩm không tồn tại' };

        const origin = product.attributes['Xuất xứ / Vùng trồng'] || product.attributes['Xuất xứ'] || 'Việt Nam';
        const cert = product.attributes['Chứng nhận chất lượng'] || product.attributes['Chứng nhận'] || 'VietGAP';

        return {
            title: `${product.name} - Chuẩn ${cert}, Xuất xứ ${origin}`,
            description: product.description || `Mua ${product.name} tươi ngon tại Solaris Farm. Đạt chuẩn ${cert}, nguồn gốc ${origin}, giao hàng nhanh 2h.`,
            openGraph: {
                title: `${product.name} | Solaris Nông Sản Sạch`,
                description: `Mua ${product.name} chuẩn VietGAP/GlobalGAP giao tươi 2h.`,
                images: product.imagePath ? [product.imagePath] : [],
                type: 'article',
            },
            alternates: {
                canonical: `https://solaris.vn/san-pham/${product.slug}`
            }
        };
    } catch {
        return { title: 'Chi tiết sản phẩm' };
    }
}

export default async function ProductDetailPage({ params }: ProductDetailPageProps) {
    const { slug } = await params;

    let product: ShopProductDetail | null = null;
    try {
        product = await shopProductApi.getBySlug(slug);
    } catch {
        notFound();
    }

    if (!product) {
        notFound();
    }

    return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-10">
            
            {/* JSON-LD Schema Structured Data */}
            <JsonLdProduct product={product} />

            {/* Breadcrumb Navigation */}
            <nav className="flex items-center gap-2 text-xs text-slate-500 font-medium">
                <Link href="/" className="hover:text-emerald-700 transition-colors">Trang chủ</Link>
                <span>/</span>
                <Link href="/san-pham" className="hover:text-emerald-700 transition-colors">Sản phẩm</Link>
                {product.categoryName && (
                    <>
                        <span>/</span>
                        <Link href={`/danh-muc/${product.categorySlug}`} className="hover:text-emerald-700 transition-colors">
                            {product.categoryName}
                        </Link>
                    </>
                )}
                <span>/</span>
                <span className="font-bold text-slate-900 truncate max-w-xs">{product.name}</span>
            </nav>

            {/* Main Product Showcase Area */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-12 items-start">
                
                {/* Left: Product Image Gallery */}
                <div className="lg:col-span-6 space-y-4">
                    <div className="relative aspect-square bg-white rounded-3xl border border-slate-200/80 shadow-[0_4px_25px_-5px_rgba(0,0,0,0.06)] overflow-hidden flex items-center justify-center group">
                        {product.imagePath ? (
                            <img
                                src={product.imagePath}
                                alt={product.name}
                                className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500 ease-out"
                            />
                        ) : (
                            <div className="w-full h-full flex flex-col items-center justify-center text-slate-300">
                                <svg viewBox="0 0 24 24" className="w-24 h-24 fill-none stroke-current stroke-1.5">
                                    <path d="M12 2a9 9 0 0 1 9 9c0 4.97-4.03 9-9 9a9 9 0 0 1-9-9c0-4.97 4.03-9 9-9z" />
                                    <path d="M12 7v5l3 3" />
                                    <path d="M9 12a3 3 0 1 0 6 0 3 3 0 0 0-6 0z" />
                                </svg>
                                <span className="text-xs font-bold uppercase tracking-wider mt-2 text-slate-400">Solaris Farm</span>
                            </div>
                        )}

                        {/* Top Certification Badge */}
                        <div className="absolute top-4 left-4 flex flex-col gap-1.5 items-start">
                            <span className="bg-emerald-600 text-white text-[11px] font-extrabold px-3 py-1 rounded-full shadow-md flex items-center gap-1">
                                <CheckCircle2 className="w-3.5 h-3.5" />
                                100% Nông Sản Sạch
                            </span>
                        </div>
                    </div>
                </div>

                {/* Right: Product Info & Purchase Controls */}
                <div className="lg:col-span-6 bg-white rounded-3xl shadow-[0_4px_25px_-5px_rgba(0,0,0,0.05)] border border-slate-200/80 p-6 sm:p-8 space-y-6">
                    
                    <div className="space-y-2.5 pb-4 border-b border-slate-100">
                        {/* SKU Code */}
                        <div className="flex items-center justify-between">
                            <span className="text-[11px] font-mono font-bold tracking-wider text-slate-400 uppercase">
                                SKU: {product.code}
                            </span>
                            <span className="text-xs font-semibold text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200/60 flex items-center gap-1">
                                <Sparkles className="w-3 h-3 text-emerald-600" />
                                Thu hoạch trong ngày
                            </span>
                        </div>

                        {/* H1 Title */}
                        <h1 className="text-2xl sm:text-3xl font-black text-slate-900 leading-snug">
                            {product.name}
                        </h1>

                        {/* Category & Attributes Preview */}
                        <div className="flex flex-wrap items-center gap-2 pt-1 text-xs">
                            <span className="px-3 py-1 bg-slate-100 text-slate-700 font-bold rounded-xl">
                                {product.categoryName || 'Nông sản tươi'}
                            </span>
                            {product.attributes['Xuất xứ / Vùng trồng'] && (
                                <span className="px-3 py-1 bg-slate-100 text-slate-700 font-bold rounded-xl flex items-center gap-1">
                                    <MapPin className="w-3.5 h-3.5 text-emerald-600" />
                                    {product.attributes['Xuất xứ / Vùng trồng']}
                                </span>
                            )}
                            {product.attributes['Chứng nhận chất lượng'] && (
                                <span className="px-3 py-1 bg-emerald-50 text-emerald-800 font-bold rounded-xl border border-emerald-200/60 flex items-center gap-1">
                                    <Award className="w-3.5 h-3.5 text-emerald-600" />
                                    {product.attributes['Chứng nhận chất lượng']}
                                </span>
                            )}
                        </div>
                    </div>

                    {/* Interactive Variant Selection & Add to Cart Client Component */}
                    <ProductDetailClient product={product} />

                </div>

            </div>

            {/* Specifications & Agricultural EAV Attributes Table (Nghị định 15/2018/NĐ-CP) */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 pt-6 border-t border-slate-200">
                
                {/* Left: Description */}
                <div className="lg:col-span-2 space-y-6 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-2xs">
                    <div className="flex items-center gap-2.5 pb-4 border-b border-slate-100">
                        <div className="p-2 rounded-xl bg-emerald-50 text-emerald-700">
                            <FileText className="w-5 h-5" />
                        </div>
                        <h2 className="text-lg font-black text-slate-900">Mô Tả & Thông Tin Chi Tiết</h2>
                    </div>

                    <div className="text-sm text-slate-600 leading-relaxed space-y-4">
                        <p>
                            {product.description || `${product.name} được nuôi trồng và thu hoạch theo quy chuẩn nông nghiệp sạch, bảo đảm độ tươi giòn tự nhiên và giữ trọn hàm lượng vitamin khoáng chất thiết yếu.`}
                        </p>
                    </div>

                    {/* Quality Badges */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-4 border-t border-slate-100">
                        <div className="p-4 rounded-2xl bg-slate-50/80 border border-slate-100 flex items-start gap-3">
                            <ShieldCheck className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
                            <div>
                                <h4 className="text-xs font-bold text-slate-800">Kiểm Soát FEFO Nghiêm Ngặt</h4>
                                <p className="text-[11px] text-slate-500 mt-0.5">Xuất kho ưu tiên theo hạn sử dụng, đảm bảo độ tươi mới tối đa.</p>
                            </div>
                        </div>

                        <div className="p-4 rounded-2xl bg-slate-50/80 border border-slate-100 flex items-start gap-3">
                            <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
                            <div>
                                <h4 className="text-xs font-bold text-slate-800">Nghị Định 15/2018/NĐ-CP</h4>
                                <p className="text-[11px] text-slate-500 mt-0.5">Đầy đủ hồ sơ tự công bố chất lượng và kiểm nghiệm an toàn thực phẩm.</p>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Right: Technical Attributes Sidebar */}
                <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-2xs space-y-5">
                    <div className="flex items-center gap-2.5 pb-4 border-b border-slate-100">
                        <div className="p-2 rounded-xl bg-emerald-50 text-emerald-700">
                            <Info className="w-5 h-5" />
                        </div>
                        <h2 className="text-base font-black text-slate-900">Thông Số Nông Sản</h2>
                    </div>

                    <dl className="space-y-3.5 text-xs">
                        <div className="flex justify-between pb-2.5 border-b border-slate-100">
                            <dt className="text-slate-400 font-semibold">Đơn vị cơ sở:</dt>
                            <dd className="font-bold text-slate-800">{product.baseUoMName || 'Kg'}</dd>
                        </div>

                        <div className="flex justify-between pb-2.5 border-b border-slate-100">
                            <dt className="text-slate-400 font-semibold">Danh mục:</dt>
                            <dd className="font-bold text-slate-800">{product.categoryName || 'Nông sản'}</dd>
                        </div>

                        {Object.entries(product.attributes).map(([key, value]) => (
                            <div key={key} className="flex justify-between pb-2.5 border-b border-slate-100">
                                <dt className="text-slate-400 font-semibold">{key}:</dt>
                                <dd className="font-bold text-emerald-800 text-right max-w-[160px] truncate">{value}</dd>
                            </div>
                        ))}

                        <div className="flex justify-between pt-1">
                            <dt className="text-slate-400 font-semibold">Bảo quản đề xuất:</dt>
                            <dd className="font-bold text-slate-800">Nhiệt độ 2°C - 8°C</dd>
                        </div>
                    </dl>
                </div>

            </div>

        </div>
    );
}
