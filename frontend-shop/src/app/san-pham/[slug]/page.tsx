import React from 'react';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { MapPin, Award, CheckCircle2, FileText, Info } from 'lucide-react';
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
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-12">
            
            {/* JSON-LD Schema Structured Data */}
            <JsonLdProduct product={product} />

            {/* Breadcrumb Navigation */}
            <nav className="flex items-center gap-2 text-xs text-slate-500">
                <Link href="/" className="hover:text-emerald-600">Trang chủ</Link>
                <span>/</span>
                <Link href="/san-pham" className="hover:text-emerald-600">Sản phẩm</Link>
                {product.categoryName && (
                    <>
                        <span>/</span>
                        <Link href={`/danh-muc/${product.categorySlug}`} className="hover:text-emerald-600">
                            {product.categoryName}
                        </Link>
                    </>
                )}
                <span>/</span>
                <span className="font-semibold text-slate-800 truncate max-w-xs">{product.name}</span>
            </nav>

            {/* Main Product Showcase Area */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-start">
                
                {/* Left: Product Image */}
                <div className="space-y-4">
                    <div className="relative aspect-square bg-white rounded-3xl border border-slate-200/80 shadow-xs overflow-hidden flex items-center justify-center">
                        {product.imagePath ? (
                            <img
                                src={product.imagePath}
                                alt={product.name}
                                className="w-full h-full object-cover"
                            />
                        ) : (
                            <div className="text-8xl select-none">🥑</div>
                        )}
                    </div>
                </div>

                {/* Right: Product Info & Purchase Controls */}
                <div className="space-y-6">
                    
                    <div className="space-y-2">
                        {/* SKU Code */}
                        <span className="text-[10px] font-mono font-bold tracking-wider text-slate-400 uppercase">
                            MÃ SP: {product.code}
                        </span>

                        {/* H1 Title (Mandatory for SEOQuake 100%) */}
                        <h1 className="text-2xl sm:text-3xl font-black text-slate-900 leading-tight">
                            {product.name}
                        </h1>

                        {/* Category & Status */}
                        <div className="flex items-center gap-2 pt-1">
                            <span className="px-2.5 py-0.5 bg-emerald-50 text-emerald-700 text-xs font-bold rounded-md">
                                {product.categoryName || 'Nông sản tươi'}
                            </span>
                            <span className="text-xs text-slate-400">•</span>
                            <span className="text-xs font-semibold text-emerald-600 flex items-center gap-1">
                                <CheckCircle2 className="w-3.5 h-3.5" />
                                Thu hoạch hôm nay
                            </span>
                        </div>
                    </div>

                    {/* Interactive Variant Selection & Add to Cart Client Component */}
                    <ProductDetailClient product={product} />

                </div>

            </div>

            {/* Specifications & Agricultural EAV Attributes Table (Nghị định 15/2018/NĐ-CP) */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 pt-8 border-t border-slate-200">
                
                {/* Left: Description */}
                <div className="lg:col-span-2 space-y-6 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs">
                    <div className="flex items-center gap-2 pb-4 border-b border-slate-100">
                        <FileText className="w-5 h-5 text-emerald-600" />
                        <h2 className="text-lg font-bold text-slate-900">Mô Tả & Thông Tin Chi Tiết</h2>
                    </div>

                    <div className="text-sm text-slate-600 leading-relaxed space-y-4">
                        <p>
                            {product.description || `${product.name} được nuôi trồng và thu hoạch theo quy chuẩn an toàn sinh học, bảo đảm độ tươi giòn tự nhiên và giữ trọn hàm lượng vitamin khoáng chất.`}
                        </p>
                    </div>

                    {/* Quality Badges */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-4 border-t border-slate-100">
                        <div className="flex items-start gap-3 p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100">
                            <Award className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
                            <div>
                                <h4 className="font-bold text-xs text-slate-900">Chuẩn An Toàn Thực Phẩm</h4>
                                <p className="text-[11px] text-slate-500 mt-0.5">Kiểm định dư lượng thuốc bảo vệ thực vật 0%.</p>
                            </div>
                        </div>

                        <div className="flex items-start gap-3 p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100">
                            <MapPin className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
                            <div>
                                <h4 className="font-bold text-xs text-slate-900">Nguồn Gốc Xuất Xứ Rõ Ràng</h4>
                                <p className="text-[11px] text-slate-500 mt-0.5">Truy xuất được lô hàng và nông trường thu hoạch.</p>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Right: EAV Nutrition & Storage Table */}
                <div className="space-y-4 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs">
                    <div className="flex items-center gap-2 pb-4 border-b border-slate-100">
                        <Info className="w-5 h-5 text-emerald-600" />
                        <h2 className="text-lg font-bold text-slate-900">Thông Số Nông Sản</h2>
                    </div>

                    <div className="divide-y divide-slate-100 text-xs">
                        {Object.entries(product.attributes).length > 0 ? (
                            Object.entries(product.attributes).map(([key, val]) => (
                                <div key={key} className="py-2.5 flex justify-between gap-4">
                                    <span className="font-medium text-slate-500">{key}</span>
                                    <span className="font-bold text-slate-800 text-right">{val}</span>
                                </div>
                            ))
                        ) : (
                            <>
                                <div className="py-2.5 flex justify-between gap-4">
                                    <span className="font-medium text-slate-500">Xuất xứ</span>
                                    <span className="font-bold text-slate-800">Đà Lạt, Lâm Đồng</span>
                                </div>
                                <div className="py-2.5 flex justify-between gap-4">
                                    <span className="font-medium text-slate-500">Tiêu chuẩn</span>
                                    <span className="font-bold text-emerald-700">VietGAP</span>
                                </div>
                                <div className="py-2.5 flex justify-between gap-4">
                                    <span className="font-medium text-slate-500">Bảo quản</span>
                                    <span className="font-bold text-slate-800">Ngăn mát 4-8°C</span>
                                </div>
                            </>
                        )}
                        <div className="py-2.5 flex justify-between gap-4">
                            <span className="font-medium text-slate-500">Đơn vị cơ sở</span>
                            <span className="font-bold text-slate-800">{product.baseUoMName}</span>
                        </div>
                    </div>
                </div>

            </div>

        </div>
    );
}


